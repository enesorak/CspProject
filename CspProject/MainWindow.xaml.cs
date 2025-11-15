﻿using System.ComponentModel;
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

        public MainWindow(User currentUser,ApplicationDbContext dbContext)
        {
             
            InitializeComponent();
            this.DataContext = this;
       
            
            _currentUser = currentUser;
            _dbContext = dbContext;
            
            ShowHomeScreen();
        }

       /* private async void ThemedWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // `App.xaml.cs` zaten kullanıcıyı kontrol ettiği için,
            // burada sadece mevcut kullanıcıyı alıp HomeScreen'i yüklüyoruz.
            _currentUser = await _dataContext.Users.FirstOrDefaultAsync();
            if (_currentUser != null)
            {
                ShowHomeScreen();
            }
        }
        */

        private void ShowHomeScreen()
        {
        
            var homeScreen = new HomeScreen(_currentUser);
            homeScreen.RequestNewDocument += (_, _) => ShowSpreadsheetScreen(null);
            homeScreen.RequestOpenDocument += (_, docId) => ShowSpreadsheetScreen(docId);
            
            homeScreen.RequestNewDocumentFromFile += (_, filePath) => 
                ShowSpreadsheetScreenFromTemplate(filePath);
            
            homeScreen.RequestBlankSpreadsheet += (_, _) => ShowBlankSpreadsheet();

            
            
            MainContentControl.Content = homeScreen;
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

                    // Template'i yükle
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
            // 1. Arayüz İşlemi: WaitIndicator'ı göster (Ana İş Parçacığı)
            WaitIndicatorText = documentId.HasValue ? "Loading document..." : "Creating new document template...";
            IsWaitIndicatorVisible = true;

            // 2. Ağır işi, arayüz güncellendikten ve animasyon başladıktan sonra çalışacak şekilde,
            //    daha düşük bir öncelikle sıraya koy.
            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var spreadsheetView = new SpreadsheetView(_currentUser);
                    spreadsheetView.RequestGoToHome += (_, _) => ShowHomeScreen();

                    if (documentId.HasValue)
                    {
                        await spreadsheetView.LoadDocument(documentId.Value);
                    }
                    else
                    {
                        await spreadsheetView.CreateNewFmeaDocumentAsync();
                    }

                    MainContentControl.Content = spreadsheetView;
                }
                finally
                {
                    // 3. Ağır iş bittiğinde (veya hata oluştuğunda),
                    //    göstergeyi gizle.
                    IsWaitIndicatorVisible = false;
                }
            }, DispatcherPriority.Background); // İşi 'Background' önceliğiyle sıraya al
        }
 
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
    
            // ✅ Cleanup
            _dbContext?.Dispose();
    
            // ✅ Sentry flush
            SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).Wait();
    
            SentrySdk.AddBreadcrumb("Application closing", "app.lifecycle");
        }
    }
}