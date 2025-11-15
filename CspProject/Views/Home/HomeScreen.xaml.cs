using System.Windows;
using System.Windows.Threading;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services.Email;
using CspProject.Services.Template;
using CspProject.Views.Approvals;
using CspProject.Views.Audit;
using CspProject.Views.Documents;
using CspProject.Views.Settings;
using CspProject.Views.Templates;
using DevExpress.Mvvm;
// EKLE: UserControl için
using Button = System.Windows.Controls.Button;

namespace CspProject.Views.Home
{
    public partial class HomeScreen : UserControl
    {
        private readonly User _currentUser;
        private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
        private readonly DispatcherTimer _backgroundTimer;
        public event EventHandler? RequestBlankSpreadsheet; // YENİ

        public event EventHandler? RequestNewFmeaDocument;
        
        // --- YENİ EKLENEN OLAY ---
        // Dosyadan şablon yükleme isteğini MainWindow'a iletmek için.
        public event EventHandler<string>? RequestNewDocumentFromFile;
        
        
 
        // --- YENİ OLAY SONU ---
        
        
        // --- Event'ler ---
        public event EventHandler<string>? RequestNewDocumentFromTemplate;
        public event EventHandler? RequestNewDocument;
        public event EventHandler<int>? RequestOpenDocument;
        public event EventHandler<string>? RequestNavigate;
        
        private readonly TemplateService _templateService;
        private readonly string _templateDirectory;

        public HomeScreen(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            CurrentUserTextBlock.Text = _currentUser.Name;
            VersionTextBlock.Text = $"Version: {App.AppVersion}";

        
            
            NavigateTo("Home");

            _backgroundTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _backgroundTimer.Tick += BackgroundTimer_Tick;
            _backgroundTimer.Start();
            this.Unloaded += (s, e) => _backgroundTimer.Stop();
        }

        private async void BackgroundTimer_Tick(object? sender, EventArgs e)
        {
            await CheckForApprovals(isSilent: true);
        }

        private async Task CheckForApprovals(bool isSilent)
        {
            var receiverService = new EmailReceiverService(_dbContext);
            string resultMessage = string.Empty;
            try
            {
                resultMessage = await receiverService.CheckForApprovalEmailsAsync();
                if (resultMessage.Contains("processed"))
                {
                    NavigateTo("Home");
                }
            }
            catch (Exception ex)
            {
                resultMessage = $"Error: {ex.GetType().Name}";
            }
            finally
            { 
                if (!isSilent && resultMessage.Contains("processed"))
                {
                    var notificationService = ServiceContainer.Default.GetService<INotificationService>();
            
                    if (notificationService != null)
                    {
                        var notification = notificationService.CreatePredefinedNotification("Approvals Processed", resultMessage, "");
                        await notification.ShowAsync();
                    }
                }
            }
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pageTag)
            {
                NavigateTo(pageTag);
            }
        }

  private void NavigateTo(string pageTag)
        {
            switch (pageTag)
            {
                case "Home":
                    var homeContent = new HomeContentView();
                    // Home ekranındaki "Create New" butonu, mevcut FMEA olayını tetikler.
                    homeContent.RequestNewDocument += (s, e) => RequestNewFmeaDocument?.Invoke(s, e);
                    homeContent.RequestOpenDocument += (s, id) => RequestOpenDocument?.Invoke(s, id);
                    PageContentControl.Content = homeContent;
                    break;
                case "MyDocuments":
                    var myDocsView = new MyDocumentsView();
                    myDocsView.RequestOpenDocument += (s, id) => RequestOpenDocument?.Invoke(s, id);
                    PageContentControl.Content = myDocsView;
                    break;
                case "Approvals":
                    var approvalsView = new ApprovalsView();
                    approvalsView.RequestOpenDocument += (s, id) => RequestOpenDocument?.Invoke(s, id);
                    PageContentControl.Content = approvalsView;
                    break;
                case "Templates":
                    // --- GÜNCELLENMİŞ BÖLÜM ---
                    var templatesView = new TemplatesView(); 
                    
                    // 1. Dahili FMEA şablonu isteğini dinle
                    templatesView.RequestNewFmeaDocument += (s, e) => 
                        RequestNewFmeaDocument?.Invoke(this, e);
                    
                    // 2. Dosyadan şablon isteğini dinle
                    templatesView.RequestNewDocumentFromFile += (s, filePath) => 
                        RequestNewDocumentFromFile?.Invoke(this, filePath);
                    
                    templatesView.RequestBlankSpreadsheet += (s, e) =>
                        RequestBlankSpreadsheet?.Invoke(this, e);
                    
                    PageContentControl.Content = templatesView;
                    break;
                    // --- GÜNCELLEME SONU ---
                case "ChangeLog":
                    PageContentControl.Content = new ChangeLogView();
                    break;
                case "Settings":
                    PageContentControl.Content = new SettingsView(_currentUser);
                    break;
            }
        }
    }
}