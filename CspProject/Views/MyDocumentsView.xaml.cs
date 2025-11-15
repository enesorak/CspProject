// Views/MyDocumentsView.xaml.cs - GÜNCELLENECEK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services;
using DevExpress.Mvvm;
using Microsoft.EntityFrameworkCore;

namespace CspProject.Views
{
    public partial class MyDocumentsView : ViewBase // ✅ UserControl → ViewBase
    {
        public event EventHandler<int>? RequestOpenDocument;

        // ❌ KALDIR
        // private readonly ApplicationDbContext _dbContext;

        public MyDocumentsView()
        {
            InitializeComponent();
            
            // ❌ KALDIR - ViewBase constructor'ında hallediyor
            // _dbContext = dbContext;
            
            // ❌ KALDIR - Çok erken, DbContext henüz hazır değil
            // LoadAllDocuments();
            
            // ❌ KALDIR
            // this.Loaded += MyDocumentsView_Loaded;
        }

        // ✅ EKLE - ViewBase lifecycle
        protected override void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            base.OnViewLoaded(sender, e);
            
            // DbContext artık hazır
            LoadAllDocuments();
            
            // Last check time'ı göster
            UpdateLastCheckLabel();
        }

        private void UpdateLastCheckLabel()
        {
            if (AppStateService.LastCheckTime.HasValue)
            {
                LastCheckedLabel.Text = $"Last checked at: {AppStateService.LastCheckTime.Value:g}";
            }
            else
            {
                LastCheckedLabel.Text = "Last checked at: Never";
            }
        }

        private async void LoadAllDocuments(string searchText = "")
        {
            // ✅ DbContext null check
            if (DbContext == null) return;

            // ✅ Yeni DbContext instance kullanmak yerine mevcut olanı kullan
            var query = DbContext.Documents.AsQueryable();

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(d => d.DocumentName.ToLower().Contains(searchText.ToLower()));
            }

            var documents = await query
                .OrderByDescending(d => d.ModifiedDate)
                .ToListAsync();
            
            AllDocumentsGrid.ItemsSource = documents;
        }

        private void AllDocumentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = (DevExpress.Xpf.Grid.GridControl)sender;
            if (grid.SelectedItem is Document selectedDoc)
            {
                RequestOpenDocument?.Invoke(this, selectedDoc.Id);
            }
        }

        private void SearchBox_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            LoadAllDocuments(SearchBox.Text);
        }

        private async void CheckNowButton_Click(object sender, RoutedEventArgs e)
        {
            CheckNowButton.IsEnabled = false;
            LastCheckedLabel.Text = "Checking...";
            await CheckForApprovals();
        }

        private async Task CheckForApprovals()
        {
            // ✅ DbContext null check
            if (DbContext == null) return;

            // ✅ Yeni instance yerine mevcut DbContext'i kullan
            var receiverService = new EmailReceiverService(DbContext);
            string resultMessage = string.Empty;
            
            try
            {
                resultMessage = await receiverService.CheckForApprovalEmailsAsync();
                LoadAllDocuments(SearchBox.Text);
            }
            catch (Exception ex)
            {
                resultMessage = $"Error: {ex.GetType().Name}";
                SentrySdk.CaptureException(ex);
            }
            finally
            {
                var now = DateTime.Now;
                AppStateService.LastCheckTime = now;
                CheckNowButton.IsEnabled = true;
                LastCheckedLabel.Text = $"Last checked at: {now:g}";
                
                if (resultMessage.Contains("processed"))
                {
                    var notificationService = ServiceContainer.Default.GetService<INotificationService>();

                    if (notificationService != null)
                    {
                        var notification = notificationService.CreatePredefinedNotification(
                            "Approvals Processed", 
                            resultMessage, 
                            "");
                        await notification.ShowAsync();
                    }
                }
            }
        }

        // ✅ EKLE - Custom cleanup
        protected override void OnDisposing()
        {
            base.OnDisposing();
            
            // Event cleanup
            RequestOpenDocument = null;
        }
    }
}