// Views/MyDocumentsView.xaml.cs - FIXED VERSION

using System.Windows;
using CspProject.Data.Entities;
using CspProject.Services;
using CspProject.Services.Email;
using CspProject.Views.Base;
using DevExpress.Mvvm;
using Microsoft.EntityFrameworkCore;

namespace CspProject.Views.Documents
{
    public partial class MyDocumentsView : ViewBase
    {
        public event EventHandler<int>? RequestOpenDocument;

        public MyDocumentsView()
        {
            InitializeComponent();
        }

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
            if (DbContext == null) return;

            try
            {
                var query = DbContext.Documents.AsQueryable();

                if (!string.IsNullOrEmpty(searchText))
                {
                    query = query.Where(d => d.DocumentName.ToLower().Contains(searchText.ToLower()));
                }

                var documents = await query
                    .OrderByDescending(d => d.ModifiedDate)
                    .AsNoTracking()
                    .ToListAsync();
                
                AllDocumentsGrid.ItemsSource = documents;
                
                SentrySdk.AddBreadcrumb($"Loaded {documents.Count} documents", "data");
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                MessageBox.Show($"Error loading documents: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AllDocumentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var grid = (DevExpress.Xpf.Grid.GridControl)sender;
                
                // ✅ METHOD 1: Direct cast (if ItemsSource is List<Document>)
                if (grid.SelectedItem is Document selectedDoc)
                {
                    SentrySdk.AddBreadcrumb($"Opening document: {selectedDoc.DocumentName} (ID: {selectedDoc.Id})", "user_action");
                    RequestOpenDocument?.Invoke(this, selectedDoc.Id);
                    return;
                }
                
                // ✅ METHOD 2: Get ID from grid (if ItemsSource is anonymous type)
                if (grid.SelectedItem != null && grid.View.FocusedRowHandle >= 0)
                {
                    var idValue = grid.GetCellValue(grid.View.FocusedRowHandle, "Id");
                    if (idValue != null && int.TryParse(idValue.ToString(), out int docId))
                    {
                        SentrySdk.AddBreadcrumb($"Opening document ID: {docId}", "user_action");
                        RequestOpenDocument?.Invoke(this, docId);
                        return;
                    }
                }
                
                // ✅ If nothing worked, log it
                SentrySdk.AddBreadcrumb("Failed to open document - no valid selection", "error");
                MessageBox.Show("Please select a document to open.", 
                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                MessageBox.Show($"Error opening document: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (DbContext == null) return;

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

        protected override void OnDisposing()
        {
            base.OnDisposing();
            
            // Event cleanup
            RequestOpenDocument = null;
        }
    }
}