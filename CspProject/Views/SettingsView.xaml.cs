using System.Windows;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services;

 

namespace CspProject.Views
{
    public partial class SettingsView : UserControl
    {
        // RequestGoToHome event'i ve BackButton_Click metodu buradan kaldırıldı.

        private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
        private EmailSetting? _currentEmailSettings;
        private readonly User _currentUser;

        public SettingsView(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            LoadEmailSettings();
        }
        private void TestSentry_Click(object sender, RoutedEventArgs e)
        {
            SentrySdk.AddBreadcrumb("User clicked test button", "user_action");
            SentrySdk.CaptureMessage("This is a test message from CspProject", SentryLevel.Info);
    
            MessageBox.Show("Test message sent to Sentry!");
        }

        private void TestError_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                throw new InvalidOperationException("This is a test exception for Sentry");
            }
            catch (Exception ex)
            {
                // ConfigureScope kullan
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("test", "manual_error");
                    scope.SetExtra("button_clicked", "TestError");
                });
        
                SentrySdk.CaptureException(ex);
                MessageBox.Show("Test error sent to Sentry!");
            }
        }
        private void LoadEmailSettings()
        {
            _currentEmailSettings = _dbContext.EmailSettings.FirstOrDefault();

            if (_currentEmailSettings == null)
            {
                _currentEmailSettings = new EmailSetting { Id = 1 };
                _dbContext.EmailSettings.Add(_currentEmailSettings);
            }
            
            SmtpServerTextEdit.Text = _currentEmailSettings.SmtpServer;
            SmtpPortTextEdit.Text = _currentEmailSettings.SmtpPort.ToString();
            ImapServerTextEdit.Text = _currentEmailSettings.ImapServer;
            ImapPortTextEdit.Text = _currentEmailSettings.ImapPort.ToString();
            SenderNameTextEdit.Text = _currentEmailSettings.SenderName;
            PasswordBoxEdit.Password = _currentEmailSettings.Password;
            EnableSslCheckEdit.IsChecked = _currentEmailSettings.EnableSsl;
            
            SenderEmailTextEdit.Text = string.IsNullOrWhiteSpace(_currentEmailSettings.SenderEmail)
                ? _currentUser.Email
                : _currentEmailSettings.SenderEmail;
        }

        private async void SaveEmailSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEmailSettings == null)
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
            
            await _dbContext.SaveChangesAsync();

            MessageBox.Show("Email settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
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

            var emailService = new EmailService(_dbContext);
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
    }
}