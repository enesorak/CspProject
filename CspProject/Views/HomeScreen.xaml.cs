using System.Windows;
using CspProject.Data.Entities;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace CspProject.Views;

    public partial class HomeScreen : UserControl
    {
        public event EventHandler? RequestNewDocument;
        public event EventHandler<int>? RequestOpenDocument;
        public event EventHandler<string>? RequestNavigate; // YENİ

        public HomeScreen(User currentUser)
        {
            InitializeComponent(); 
            CurrentUserTextBlock.Text = currentUser.Name;
            VersionTextBlock.Text = $"Version: {App.AppVersion}"; // Versiyonu ayarla

            NavigateTo("Home");
        }
        
        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string page)
            {
                NavigateTo(page);

            }
        }
         

        private void NavigateTo(string pageTag)
        {
            switch (pageTag)
            {
                case "Home":
                    var homeContent = new HomeContentView();
                    homeContent.RequestNewDocument += (s, e) => RequestNewDocument?.Invoke(s, e);
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
                    var templatesView = new TemplatesView();
                    templatesView.RequestNewFmeaDocument += (s, e) => RequestNewDocument?.Invoke(s, e);
                    PageContentControl.Content = templatesView;
                    break;
                case "ChangeLog":
                    PageContentControl.Content = new ChangeLogView();
                    break;
                case "Settings":
                    RequestNavigate?.Invoke(this, pageTag);
                    break;
            }
        }
         }