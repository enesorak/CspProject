using System.Windows;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services.Email;
using CspProject.Views.Base;
using Microsoft.EntityFrameworkCore;

namespace CspProject.Views.Settings
{
    public partial class SettingsView : ViewBase
    {
        // RequestGoToHome event'i ve BackButton_Click metodu buradan kaldırıldı.

      
        private EmailSetting? _currentEmailSettings;
        private readonly User _currentUser;

        public SettingsView(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
           
        }
        
        /// <summary>
        /// Called when the view is loaded and DbContext is ready.
        /// </summary>
        protected override void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            base.OnViewLoaded(sender, e);
            
            LoadEmailSettings();
            LoadSystemInfo();
        }
         
        
        /// <summary>
        /// Loads email settings from database and populates UI controls.
        /// </summary>
        private async void LoadEmailSettings()
        {
            if (DbContext == null) return;

            _currentEmailSettings = await DbContext.EmailSettings.FirstOrDefaultAsync();

            if (_currentEmailSettings == null)
            {
                _currentEmailSettings = new EmailSetting { Id = 1 };
                DbContext.EmailSettings.Add(_currentEmailSettings);
            }
            
            // Populate UI controls
            SmtpServerTextEdit.Text = _currentEmailSettings.SmtpServer;
            SmtpPortTextEdit.Text = _currentEmailSettings.SmtpPort.ToString();
            ImapServerTextEdit.Text = _currentEmailSettings.ImapServer;
            ImapPortTextEdit.Text = _currentEmailSettings.ImapPort.ToString();
            SenderNameTextEdit.Text = _currentEmailSettings.SenderName;
            PasswordBoxEdit.Password = _currentEmailSettings.Password;
            EnableSslCheckEdit.IsChecked = _currentEmailSettings.EnableSsl;
            
            // Use current user's email as default if no email is configured
            SenderEmailTextEdit.Text = string.IsNullOrWhiteSpace(_currentEmailSettings.SenderEmail)
                ? _currentUser.Email
                : _currentEmailSettings.SenderEmail;
        }

        
        
        /// <summary>
        /// Loads and displays system information (OS, database size, document count).
        /// </summary>
        private async void LoadSystemInfo()
        {
            if (DbContext == null) return;

            try
            {
                // Set OS information
                OsTextBlock.Text = Environment.OSVersion.ToString();

                // Get database file size
                string dbPath = "csp_database.db";
                if (System.IO.File.Exists(dbPath))
                {
                    var fileInfo = new System.IO.FileInfo(dbPath);
                    double sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                    DbSizeTextBlock.Text = $"{sizeInMB:F2} MB";
                }

                // Get total document count
                int docCount = await DbContext.Documents.CountAsync();
                DocCountTextBlock.Text = docCount.ToString();
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }

        
        
        
        /// <summary>
        /// Saves email settings to the database after validation.
        /// </summary>
        private async void SaveEmailSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEmailSettings == null || DbContext == null)
            {
                MessageBox.Show("An error occurred and settings cannot be saved.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(SenderEmailTextEdit.Text) || !SenderEmailTextEdit.Text.Contains("@"))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _currentEmailSettings.SmtpServer = SmtpServerTextEdit.Text;
            _currentEmailSettings.ImapServer = ImapServerTextEdit.Text;
            _currentEmailSettings.SenderName = SenderNameTextEdit.Text;
            _currentEmailSettings.SenderEmail = SenderEmailTextEdit.Text;
            _currentEmailSettings.Password = PasswordBoxEdit.Password;
            _currentEmailSettings.EnableSsl = EnableSslCheckEdit.IsChecked ?? true;

            int.TryParse(SmtpPortTextEdit.Text, out int smtpPort);
            _currentEmailSettings.SmtpPort = smtpPort;

            int.TryParse(ImapPortTextEdit.Text, out int imapPort);
            _currentEmailSettings.ImapPort = imapPort;
            
            await DbContext.SaveChangesAsync();

            MessageBox.Show("Email settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        
        
        /// <summary>
        /// Tests the email connection using current settings.
        /// </summary>
        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (DbContext == null) return;

            TestResultLabel.Text = "Testing connection...";
            TestResultLabel.Foreground = Brushes.Orange;

            var settingsToTest = new EmailSetting
            {
                SmtpServer = SmtpServerTextEdit.Text,
                SmtpPort = int.TryParse(SmtpPortTextEdit.Text, out var smtpPort) ? smtpPort : 0,
                ImapServer = ImapServerTextEdit.Text,
                ImapPort = int.TryParse(ImapPortTextEdit.Text, out var imapPort) ? imapPort : 0,
                SenderEmail = SenderEmailTextEdit.Text,
                SenderName = SenderNameTextEdit.Text,
                Password = PasswordBoxEdit.Password,
                EnableSsl = EnableSslCheckEdit.IsChecked ?? true
            };

            var emailService = new EmailService(DbContext);
            var (isSuccess, message) = await emailService.TestConnectionAsync(settingsToTest);

            if (isSuccess)
            {
                TestResultLabel.Text = message;
                TestResultLabel.Foreground = Brushes.Green;
            }
            else
            {
                TestResultLabel.Text = $"Test Failed: {message}";
                TestResultLabel.Foreground = Brushes.Red;
            }
        }
        
        
        
        
        /// <summary>
        /// Sends a test message to Sentry for monitoring verification.
        /// </summary>
        private void TestSentry_Click(object sender, RoutedEventArgs e)
        {
            SentrySdk.AddBreadcrumb("User clicked test button", "user_action");
            SentrySdk.CaptureMessage("This is a test message from CspProject", SentryLevel.Info);
    
            MessageBox.Show("Test message sent to Sentry!", 
                "Sentry Test", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Triggers a test exception and sends it to Sentry for error tracking verification.
        /// </summary>
        /// <summary>
        /// Triggers a test exception and sends it to Sentry for error tracking verification.
        /// </summary>
        private void TestError_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                throw new InvalidOperationException("This is a test exception for Sentry");
            }
            catch (Exception ex)
            {
                // Configure scope with additional context
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("test", "manual_error");
                    scope.SetExtra("button_clicked", "TestError");
                    scope.SetExtra("user", _currentUser.Name);
                });
        
                SentrySdk.CaptureException(ex);
                
                MessageBox.Show("Test error sent to Sentry!", 
                    "Error Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        /// <summary>
        /// Custom cleanup when view is being disposed.
        /// </summary>
        protected override void OnDisposing()
        {
            base.OnDisposing();
            
            // Clear sensitive data
            _currentEmailSettings = null;
            
            // Additional cleanup if needed
        }


    }
}