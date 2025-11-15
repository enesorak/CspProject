using System.IO;
using CspProject.Data;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;

// --- YENİ VE ÜCRETSİZ KÜTÜPHANE ---
using ClosedXML.Excel; // EPPlus yerine ClosedXML kullanmak için eklendi.

namespace CspProject.Services
{
    public class EmailReceiverService
    {
        private readonly ApplicationDbContext _dbContext;

        public EmailReceiverService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> CheckForApprovalEmailsAsync()
{
    var transaction = SentryService.StartPerformanceTracking("check-approval-emails", "email");
    
    var settings = await _dbContext.EmailSettings.FirstOrDefaultAsync();
    if (settings == null || string.IsNullOrWhiteSpace(settings.ImapServer) ||
        string.IsNullOrWhiteSpace(settings.Password))
    {
        transaction.Finish(SpanStatus.FailedPrecondition);
        return "Email receiving is not configured.";
    }

    if (!settings.EnableSsl)
    {
        SentrySdk.CaptureMessage("Attempted to check emails with SSL/TLS disabled.", 
            SentryLevel.Warning);
        transaction.Finish(SpanStatus.InvalidArgument);
        return "SSL/TLS must be enabled to check for emails.";
    }

    int processedCount = 0;

    try
    {
        var connectSpan = transaction.StartChild("imap.connect");
        using (var client = new ImapClient())
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await client.ConnectAsync(settings.ImapServer, settings.ImapPort, settings.EnableSsl);
            await client.AuthenticateAsync(settings.SenderEmail, settings.Password);
            connectSpan.Finish(SpanStatus.Ok);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);
            
            var searchSpan = transaction.StartChild("imap.search");
            var uids = await inbox.SearchAsync(SearchQuery.NotSeen.And(SearchQuery.SubjectContains("RE:")));
            searchSpan.SetExtra("unread_count", uids.Count);
            searchSpan.Finish(SpanStatus.Ok);

            foreach (var uid in uids)
            {
                try
                {
                    var message = await inbox.GetMessageAsync(uid);
                    var subject = message.Subject.Replace("RE:", "").Trim();

                    if (Guid.TryParse(subject, out var tokenGuid))
                    {
                        var token = await _dbContext.ApprovalTokens
                            .Include(t => t.Document).ThenInclude(d => d.Approver)
                            .FirstOrDefaultAsync(t => t.Token == tokenGuid && !t.IsUsed);

                        if (token != null)
                        {
                            var document = token.Document;
                            
                            var processSpan = transaction.StartChild("approval.process");
                            processSpan.SetExtra("action", token.Action);
                            processSpan.SetExtra("document_id", document.Id);
                            
                            if (token.Action == "Approve")
                            {
                                document.Status = "Approved";
                                document.Version = VersioningService.IncrementMajorVersion(document.Version);
                                document.DateCompleted = DateTime.Now;
                                document.ApprovedBy = document.Approver?.Name ?? "N/A";

                                if (document.Content != null)
                                {
                                    using (var stream = new MemoryStream(document.Content))
                                    using (var workbook = new XLWorkbook(stream))
                                    {
                                        var worksheet = workbook.Worksheet(1);
                                        worksheet.Cell("I6").Value = document.ApprovedBy;
                                        worksheet.Cell("K6").Value = document.DateCompleted.Value;
                                        worksheet.Cell("K6").Style.NumberFormat.Format = "yyyy-mm-dd hh:mm";

                                        using (var saveStream = new MemoryStream())
                                        {
                                            workbook.SaveAs(saveStream);
                                            document.Content = saveStream.ToArray();
                                        }
                                    }
                                }
                            }
                            else if (token.Action == "Reject")
                            {
                                document.Status = "Draft";
                            }

                            token.IsUsed = true;
                            await _dbContext.SaveChangesAsync();
                            processedCount++;
                            await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                            processSpan.Finish(SpanStatus.Ok);

                            DocumentUpdateService.Instance.NotifyDocumentUpdated(document.Id);
                            
                            SentryService.TrackUserAction($"Document {token.Action.ToLower()}ed via email", 
                                "workflow", 
                                new Dictionary<string, object>
                                {
                                    { "document_id", document.Id },
                                    { "document_name", document.DocumentName },
                                    { "action", token.Action }
                                });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ✅ Tek bir email hatası tüm işlemi durdurmasın
                    SentryService.CaptureSilentException(ex, $"Processing email UID: {uid}");
                }
            }

            await client.DisconnectAsync(true);
        }
        
        transaction.Finish(SpanStatus.Ok);
    }
    catch (Exception ex)
    {
        transaction.Finish(ex);
        SentrySdk.CaptureException(ex);
        return $"Error checking emails: {ex.Message}";
    }

    return $"Check complete. {processedCount} new approval(s) processed.";
}
    }
}