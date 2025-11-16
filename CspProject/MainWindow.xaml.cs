using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Views;
using CspProject.Views.Documents;
using CspProject.Views.Home;
using DevExpress.Xpf.Core;

namespace CspProject
{
    public partial class MainWindow : ThemedWindow, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly ApplicationDbContext _dbContext;
        private bool _isWaitIndicatorVisible;
        private HomeScreen? _currentHomeScreen;

        public bool IsWaitIndicatorVisible
        {
            get => _isWaitIndicatorVisible;
            set
            {
                _isWaitIndicatorVisible = value;
                OnPropertyChanged();
            }
        }

        private string _waitIndicatorText = "Processing...";

        public string WaitIndicatorText
        {
            get => _waitIndicatorText;
            set
            {
                _waitIndicatorText = value;
                OnPropertyChanged();
            }
        }

        public MainWindow(User currentUser, ApplicationDbContext dbContext)
        {
            InitializeComponent();
            this.DataContext = this;
       
            _currentUser = currentUser;
            _dbContext = dbContext;
            
            ShowHomeScreen();
        }

        private void ShowHomeScreen()
        {
            var homeScreen = new HomeScreen(_currentUser);
            
            // ✅ DEBUG: Log event subscription
            SentrySdk.AddBreadcrumb("Subscribing to HomeScreen events", "navigation");
            
            // FMEA Template
            homeScreen.RequestNewFmeaDocument += (_, _) =>
            {
                SentrySdk.AddBreadcrumb("RequestNewFmeaDocument triggered", "navigation");
                ShowSpreadsheetScreen(null);
            };
            
            // Open existing document
            homeScreen.RequestOpenDocument += (_, docId) =>
            {
                SentrySdk.AddBreadcrumb($"RequestOpenDocument triggered for ID: {docId}", "navigation");
                ShowSpreadsheetScreen(docId);
            };
            
            // Template from file
            homeScreen.RequestNewDocumentFromFile += (_, filePath) =>
            {
                SentrySdk.AddBreadcrumb($"RequestNewDocumentFromFile triggered: {filePath}", "navigation");
                ShowSpreadsheetScreenFromTemplate(filePath);
            };
            
            // Blank spreadsheet
            homeScreen.RequestBlankSpreadsheet += (_, _) =>
            {
                SentrySdk.AddBreadcrumb("RequestBlankSpreadsheet triggered", "navigation");
                ShowBlankSpreadsheet();
            };
            
            _currentHomeScreen = homeScreen;
            MainContentControl.Content = homeScreen;
            
            SentrySdk.AddBreadcrumb("HomeScreen displayed", "navigation");
        }
        
        private async void ShowBlankSpreadsheet()
        {
            WaitIndicatorText = "Creating blank spreadsheet...";
            IsWaitIndicatorVisible = true;

            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var spreadsheetView = new SpreadsheetView(_currentUser);
                    spreadsheetView.RequestGoToHome += (_, _) => ShowHomeScreen();

                    await spreadsheetView.CreateBlankDocument();

                    MainContentControl.Content = spreadsheetView;
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    MessageBox.Show($"Error creating blank spreadsheet: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsWaitIndicatorVisible = false;
                }
            }, DispatcherPriority.Background);
        }

        private async void ShowSpreadsheetScreenFromTemplate(string templateFilePath)
        {
            WaitIndicatorText = "Loading template...";
            IsWaitIndicatorVisible = true;

            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var spreadsheetView = new SpreadsheetView(_currentUser);
                    spreadsheetView.RequestGoToHome += (_, _) => ShowHomeScreen();

                    await spreadsheetView.LoadTemplateFromFile(templateFilePath);

                    MainContentControl.Content = spreadsheetView;
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    MessageBox.Show($"Failed to load template: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsWaitIndicatorVisible = false;
                    WaitIndicatorText = "";
                }
            }, DispatcherPriority.Background);
        }

        private async void ShowSpreadsheetScreen(int? documentId)
        {
            // ✅ DEBUG: Log the call
            SentrySdk.AddBreadcrumb($"ShowSpreadsheetScreen called with documentId: {documentId}", "navigation");
            
            WaitIndicatorText = documentId.HasValue ? "Loading document..." : "Creating new document template...";
            IsWaitIndicatorVisible = true;

            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var spreadsheetView = new SpreadsheetView(_currentUser);
                    spreadsheetView.RequestGoToHome += (_, _) => ShowHomeScreen();

                    if (documentId.HasValue)
                    {
                        SentrySdk.AddBreadcrumb($"Loading document ID: {documentId.Value}", "document");
                        await spreadsheetView.LoadDocument(documentId.Value);
                    }
                    else
                    {
                        SentrySdk.AddBreadcrumb("Creating new FMEA document", "document");
                        await spreadsheetView.CreateNewFmeaDocumentAsync();
                    }

                    MainContentControl.Content = spreadsheetView;
                    SentrySdk.AddBreadcrumb("SpreadsheetView displayed", "navigation");
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    MessageBox.Show($"Error loading document: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsWaitIndicatorVisible = false;
                }
            }, DispatcherPriority.Background);
        }
 
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
    
            // Cleanup
            _dbContext?.Dispose();
    
            // Sentry flush
            SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).Wait();
    
            SentrySdk.AddBreadcrumb("Application closing", "app.lifecycle");
        }
    }
}