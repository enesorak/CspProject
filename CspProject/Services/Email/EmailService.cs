using System.Web;
using CspProject.Data;
using CspProject.Data.Entities;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
// --- YENİ EKLENEN USING İFADELERİ ---

// --- EKLENEN İFADELERİN SONU ---

namespace CspProject.Services.Email
{
    public class EmailService
    {
        private readonly ApplicationDbContext _dbContext;

        public EmailService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<(bool Success, string Message)> TestConnectionAsync(EmailSetting settingsToTest)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new SentryUser
                {
                    Username = Environment.UserName,
                    Id = Environment.MachineName,
                };
                scope.SetTag("feature", "email_test");
            });


            // IMAP (Gelen Posta) Testi
            try
            {
                SentrySdk.AddBreadcrumb("Starting IMAP connection test...");

                
                using var client = new ImapClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await client.ConnectAsync(settingsToTest.ImapServer, settingsToTest.ImapPort,
                    settingsToTest.EnableSsl);
                await client.AuthenticateAsync(settingsToTest.SenderEmail, settingsToTest.Password);
                await client.DisconnectAsync(true);
                
                SentrySdk.CaptureMessage("IMAP connection test successful", SentryLevel.Info); // Başarıyı Sentry'e bildir

            }
            catch (Exception ex)
            {
                return (false, $"IMAP (Incoming Mail) Error: {ex.Message}");
            }

            // SMTP (Giden Posta) Testi
            try
            {
                SentrySdk.AddBreadcrumb("Starting SMTP connection test...");

                var secureSocketOptions = SecureSocketOptions.Auto; // Varsayılan olarak otomatikte başla
                if (settingsToTest.EnableSsl)
                {
                    // Eğer SSL etkinse, porta göre doğru yöntemi seç
                    secureSocketOptions = settingsToTest.SmtpPort == 465 
                        ? SecureSocketOptions.SslOnConnect 
                        : SecureSocketOptions.StartTls;
                }
                
                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await client.ConnectAsync(settingsToTest.SmtpServer, settingsToTest.SmtpPort,
                    secureSocketOptions);
                await client.AuthenticateAsync(settingsToTest.SenderEmail, settingsToTest.Password);
                await client.DisconnectAsync(true);
                
                SentrySdk.CaptureMessage("SMTP connection test successful", SentryLevel.Info); // Başarıyı Sentry'e bildir

            }
            catch (Exception ex)
            {
                return (false, $"SMTP (Outgoing Mail) Error: {ex.Message}");
            }

            return (true, "Connection successful for both IMAP and SMTP!");
        }

        public async Task SendApprovalRequestEmailAsync(User approver, Data.Entities.Document document, byte[] pdfAttachment)
        {
            var settings = await _dbContext.EmailSettings.FirstOrDefaultAsync();

            // 1. ADIM: Ayarları ve E-posta Adreslerini Doğrula
            if (settings == null || string.IsNullOrWhiteSpace(settings.Password) ||
                string.IsNullOrWhiteSpace(settings.SenderEmail) || !settings.SenderEmail.Contains("@"))
            {
                throw new InvalidOperationException(
                    "Email settings are not fully configured. Please check the Sender Email and App Password in the Settings screen.");
            }

            if (approver == null || string.IsNullOrWhiteSpace(approver.Email) || !approver.Email.Contains("@"))
            {
                throw new InvalidOperationException(
                    $"The selected approver '{approver?.Name}' does not have a valid email address. The process cannot continue.");
            }

            // ... (Token ve Mesaj oluşturma kodları aynı)
            var approveToken = new ApprovalToken
                { Token = Guid.NewGuid(), DocumentId = document.Id, Action = "Approve" };
            var rejectToken = new ApprovalToken { Token = Guid.NewGuid(), DocumentId = document.Id, Action = "Reject" };
            _dbContext.ApprovalTokens.Add(approveToken);
            _dbContext.ApprovalTokens.Add(rejectToken);
            await _dbContext.SaveChangesAsync();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
            message.To.Add(new MailboxAddress(approver.Name, approver.Email));
            message.Subject = $"ACTION REQUIRED: Approve '{document.DocumentName}' (v{document.Version})";

            var builder = new BodyBuilder();
            string approveMailto =
                $"mailto:{settings.SenderEmail}?subject=RE: {HttpUtility.UrlEncode(approveToken.Token.ToString())}&body={HttpUtility.UrlEncode("This document is approved.")}";
            string rejectMailto =
                $"mailto:{settings.SenderEmail}?subject=RE: {HttpUtility.UrlEncode(rejectToken.Token.ToString())}&body={HttpUtility.UrlEncode("This document is rejected. Reason: ")}";

            // HTML içeriği (kısaltıldı, kodda tam hali var)
            builder.HtmlBody = $@"
            <html>
            <body style='font-family: sans-serif;'>
                <p>Hello {approver.Name},</p>
                <p>The document '<b>{document.DocumentName}</b>' has been submitted for your approval.</p>
                <p>Please find the document attached as a PDF for your review.</p>
                <p>Once you have reviewed the document, please use the buttons below to process your decision:</p>
                <br>
                <a href='{approveMailto}' style='background-color: #4CAF50; color: white; padding: 14px 25px; text-align: center; text-decoration: none; display: inline-block; border-radius: 5px; margin-right: 10px;'>
                    <strong>APPROVE DOCUMENT</strong>
                </a>
                <a href='{rejectMailto}' style='background-color: #f44336; color: white; padding: 14px 25px; text-align: center; text-decoration: none; display: inline-block; border-radius: 5px;'>
                    <strong>REJECT DOCUMENT</strong>
                </a>
                <br><br>
                <p><i>This is an automated message from the CSP System. Clicking a button will open a new email draft for you to send.</i></p>
            </body>
            </html>";
            builder.Attachments.Add($"{document.DocumentName}.pdf", pdfAttachment,
                ContentType.Parse("application/pdf"));
            message.Body = builder.ToMessageBody();

            // 2. ADIM: E-postayı Göndermeyi Dene ve Hataları Yakala
            using (var smtpClient = new SmtpClient())
            {
                // --- YENİ BÖLÜM: ZAMAN AŞIMI (TIMEOUT) EKLEME ---
                // Bağlantı veya gönderme işlemi 15 saniyeden uzun sürerse hata ver.
                smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;

                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                {
                    try
                    {
                        var secureSocketOptions = SecureSocketOptions.Auto; // Varsayılan olarak otomatikte başla
                        if (settings.EnableSsl)
                        {
                            // Eğer SSL etkinse, porta göre doğru yöntemi seç
                            secureSocketOptions = settings.SmtpPort == 465 
                                ? SecureSocketOptions.SslOnConnect 
                                : SecureSocketOptions.StartTls;
                        }

                        await smtpClient.ConnectAsync(settings.SmtpServer, settings.SmtpPort, secureSocketOptions, cancellationTokenSource.Token);
                        await smtpClient.AuthenticateAsync(settings.SenderEmail, settings.Password, cancellationTokenSource.Token);
                        await smtpClient.SendAsync(message, cancellationTokenSource.Token);
                    }
            
                    
                    catch (Exception ex)
                    {
                        // Provide a more generic but helpful message
                        throw new InvalidOperationException(
                            $"Failed to send email. Please double-check all settings (Server, Port, Email, App Password) and your network connection. Error: {ex.Message}",
                            ex);
                    }
                    finally
                    {
                        await smtpClient.DisconnectAsync(true, CancellationToken.None);
                    }
                }
            }
        }
    }
}