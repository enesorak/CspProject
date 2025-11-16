using System.Windows;
using System.Windows.Threading;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services.Email;
using CspProject.Views.Approvals;
using CspProject.Views.Audit;
using CspProject.Views.Documents;
using CspProject.Views.Settings;
using CspProject.Views.Templates;
using DevExpress.Mvvm;
using Button = System.Windows.Controls.Button;

namespace CspProject.Views.Home
{
    public partial class HomeScreen : UserControl
    {
        private readonly User _currentUser;
        private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
        private readonly DispatcherTimer _backgroundTimer;
        
        // ✅ Cache views to prevent multiple instances
        private readonly Dictionary<string, UserControl> _cachedViews = new();
        
        public event EventHandler? RequestBlankSpreadsheet;
        public event EventHandler? RequestNewFmeaDocument;
        public event EventHandler<string>? RequestNewDocumentFromFile;
        public event EventHandler<string>? RequestNewDocumentFromTemplate;
        public event EventHandler? RequestNewDocument;
        public event EventHandler<int>? RequestOpenDocument;
        public event EventHandler<string>? RequestNavigate;

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
            
            this.Unloaded += (s, e) =>
            {
                _backgroundTimer.Stop();
                CleanupViews();
            };
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
                    // ✅ Refresh current view instead of navigating
                    RefreshCurrentView();
                }
            }
            catch (Exception ex)
            {
                resultMessage = $"Error: {ex.GetType().Name}";
                SentrySdk.CaptureException(ex);
            }
            finally
            { 
                if (!isSilent && resultMessage.Contains("processed"))
                {
                    var notificationService = ServiceContainer.Default.GetService<INotificationService>();
            
                    if (notificationService != null)
                    {
                        var notification = notificationService.CreatePredefinedNotification(
                            "Approvals Processed", resultMessage, "");
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
            // ✅ Get or create cached view
            if (!_cachedViews.TryGetValue(pageTag, out var view))
            {
                view = CreateView(pageTag);
                if (view != null)
                {
                    _cachedViews[pageTag] = view;
                }
            }
            
            if (view != null)
            {
                PageContentControl.Content = view;
            }
        }

        private UserControl? CreateView(string pageTag)
        {
            switch (pageTag)
            {
                case "Home":
                    var homeContent = new HomeContentView();
                    homeContent.RequestNewDocument += (s, e) => RequestNewFmeaDocument?.Invoke(s, e);
                    homeContent.RequestOpenDocument += (s, id) => RequestOpenDocument?.Invoke(s, id);
                    return homeContent;
                    
                case "MyDocuments":
                    var myDocsView = new MyDocumentsView();
                    myDocsView.RequestOpenDocument += (s, id) => RequestOpenDocument?.Invoke(s, id);
                    return myDocsView;
                    
                case "Approvals":
                    var approvalsView = new ApprovalsView();
                    approvalsView.RequestOpenDocument += (s, id) => RequestOpenDocument?.Invoke(s, id);
                    return approvalsView;
                    
                case "Templates":
                    var templatesView = new TemplatesView();
                    templatesView.RequestNewFmeaDocument += (s, e) => 
                        RequestNewFmeaDocument?.Invoke(this, e);
                    templatesView.RequestNewDocumentFromFile += (s, filePath) => 
                        RequestNewDocumentFromFile?.Invoke(this, filePath);
                    templatesView.RequestBlankSpreadsheet += (s, e) =>
                        RequestBlankSpreadsheet?.Invoke(this, e);
                    return templatesView;
                    
                case "ChangeLog":
                    return new ChangeLogView();
                    
                case "Settings":
                    return new SettingsView(_currentUser);
                    
                default:
                    return null;
            }
        }

        /// <summary>
        /// Refreshes the currently displayed view
        /// </summary>
        private void RefreshCurrentView()
        {
            if (PageContentControl.Content is HomeContentView homeView)
            {
                // HomeContentView will auto-refresh via its Loaded event
                _cachedViews.Remove("Home");
                NavigateTo("Home");
            }
            else if (PageContentControl.Content is MyDocumentsView docsView)
            {
                _cachedViews.Remove("MyDocuments");
                NavigateTo("MyDocuments");
            }
        }

        /// <summary>
        /// Cleanup all cached views
        /// </summary>
        private void CleanupViews()
        {
            foreach (var view in _cachedViews.Values)
            {
                if (view is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _cachedViews.Clear();
            _dbContext?.Dispose();
        }
    }
}