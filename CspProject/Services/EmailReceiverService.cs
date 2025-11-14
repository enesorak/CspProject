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
            var settings = await _dbContext.EmailSettings.FirstOrDefaultAsync();
            if (settings == null || string.IsNullOrWhiteSpace(settings.ImapServer) ||
                string.IsNullOrWhiteSpace(settings.Password))
            {
                return "Email receiving is not configured.";
            }

            if (!settings.EnableSsl)
            {
                // Bu durum Sentry'e bildirilebilir çünkü ayarların yanlış olduğunu gösterir.
                SentrySdk.CaptureMessage("Attempted to check emails with SSL/TLS disabled.", SentryLevel.Warning);
                return "SSL/TLS must be enabled to check for emails.";
            }


            int processedCount = 0;

            try
            {
                using (var client = new ImapClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    await client.ConnectAsync(settings.ImapServer, settings.ImapPort, settings.EnableSsl);
                    await client.AuthenticateAsync(settings.SenderEmail, settings.Password);

                    var inbox = client.Inbox;
                    await inbox.OpenAsync(FolderAccess.ReadWrite);
                    var uids = await inbox.SearchAsync(SearchQuery.NotSeen.And(SearchQuery.SubjectContains("RE:")));

                    foreach (var uid in uids)
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
                                if (token.Action == "Approve")
                                {
                                    document.Status = "Approved";
                                    document.Version = VersioningService.IncrementMajorVersion(document.Version);
                                    document.DateCompleted = DateTime.Now;
                                    document.ApprovedBy = document.Approver?.Name ?? "N/A";

                                    if (document.Content != null)
                                    {
                                        // --- CLOSEDXML İLE EXCEL GÜNCELLEME ---
                                        using (var stream = new MemoryStream(document.Content))
                                        {
                                            // 1. Workbook'u stream'den yükle.
                                            using (var workbook = new XLWorkbook(stream))
                                            {
                                                // 2. İlk çalışma sayfasını al.
                                                var worksheet = workbook.Worksheet(1);

                                                // 3. İlgili hücreleri doldur.
                                                worksheet.Cell("I6").Value = document.ApprovedBy;
                                                worksheet.Cell("K6").Value = document.DateCompleted.Value;
                                                worksheet.Cell("K6").Style.NumberFormat.Format = "yyyy-mm-dd hh:mm";

                                                // 4. Değişiklikleri yeni bir stream'e kaydet.
                                                using (var saveStream = new MemoryStream())
                                                {
                                                    workbook.SaveAs(saveStream);
                                                    // 5. Yeni stream'in içeriğini byte dizisi olarak al.
                                                    document.Content = saveStream.ToArray();
                                                }
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

                                DocumentUpdateService.Instance.NotifyDocumentUpdated(document.Id);
                            }
                        }
                    }

                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                return $"Error checking emails: {ex.Message}";
            }

            return $"Check complete. {processedCount} new approval(s) processed.";
        }
    }
}