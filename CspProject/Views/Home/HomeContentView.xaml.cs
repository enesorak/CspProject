// Views/HomeContentView.xaml.cs - GÜNCELLENECEK

using System.Windows;
using CspProject.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ViewBase = CspProject.Views.Base.ViewBase;

namespace CspProject.Views.Home
{
    public partial class HomeContentView : ViewBase // ✅ UserControl → ViewBase
    {
        public event EventHandler? RequestNewDocument;
        public event EventHandler<int>? RequestOpenDocument;

        // ❌ KALDIR - artık ViewBase'den geliyor
        // private readonly ApplicationDbContext _dbContext;

        public HomeContentView()
        {
            InitializeComponent();

            // ❌ KALDIR - ViewBase otomatik hallediyor
            // using (var scope = App.ServiceProvider.CreateScope())
            // {
            //     _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // }
        }

        // ✅ EKLE - ViewBase'in lifecycle metodunu override et
        protected override void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            base.OnViewLoaded(sender, e); // ✅ ÖNEMLİ: base.OnViewLoaded() çağır
            
            // DbContext artık hazır, veriyi yükleyebiliriz
            LoadRecentDocuments();
        }

        private async void LoadRecentDocuments(string statusFilter = "All", string searchText = "")
        {
            // ✅ DbContext null check
            if (DbContext == null) return;

            LoadingIndicator.Visibility = Visibility.Visible;
            RecentDocumentsGrid.IsEnabled = false;
            CreateNewButton.IsEnabled = false;

            var transaction = SentryService.StartPerformanceTracking("load-recent-documents", "ui");
            transaction.SetExtra("status_filter", statusFilter);
            transaction.SetExtra("search_text", searchText);

            try
            {
                var span = transaction.StartChild("db.query");

                // ✅ _dbContext → DbContext
                var query = DbContext.Documents.AsQueryable();

                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                {
                    query = query.Where(d => d.Status == statusFilter);
                }

                if (!string.IsNullOrEmpty(searchText))
                {
                    query = query.Where(d => d.DocumentName.ToLower().Contains(searchText.ToLower()));
                }

                var documentsFromDb = await query
                    .Include(document => document.Author)
                    .Include(document => document.Approver)
                    .AsNoTracking()
                    .OrderByDescending(d => d.ModifiedDate)
                    .Take(10)
                    .ToListAsync();

                span.SetExtra("document_count", documentsFromDb.Count);
                span.Finish(SpanStatus.Ok);

                var documentsForDisplay = documentsFromDb.Select(d => new
                {
                    d.Id,
                    d.DocumentName,
                    AuthorName = d.Author?.Name,
                    ApproverName = d.Approver?.Name,
                    d.Status,
                    ModifiedDate = FormatDate(d.ModifiedDate),
                    d.Version
                }).ToList();

                RecentDocumentsGrid.ItemsSource = documentsForDisplay;

                transaction.Finish(SpanStatus.Ok);
            }
            catch (Exception ex)
            {
                transaction.Finish(ex);
                SentryService.CaptureExceptionWithContext(ex,
                    extras: new Dictionary<string, object>
                    {
                        { "filter", statusFilter },
                        { "search", searchText }
                    });
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                RecentDocumentsGrid.IsEnabled = true;
                CreateNewButton.IsEnabled = true;
            }
        }

        private string FormatDate(DateTime date)
        {
            if (date.Date == DateTime.Today) return "Today";
            if (date.Date == DateTime.Today.AddDays(-1)) return "Yesterday";
            return date.ToString("d");
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            string status = (StatusComboBox.EditValue as string) ?? "All";
            string search = SearchBox.Text ?? "";

            LoadRecentDocuments(status, search);
        }

        private void CreateNewButton_Click(object sender, RoutedEventArgs e)
        {
            RequestNewDocument?.Invoke(this, EventArgs.Empty);
        }

        private void RecentDocumentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = (DevExpress.Xpf.Grid.GridControl)sender;
            if (grid.SelectedItem != null)
            {
                int docId = (int)grid.GetCellValue(grid.View.FocusedRowHandle, "Id");
                RequestOpenDocument?.Invoke(this, docId);
            }
        }

        // ✅ EKLE - Custom cleanup
        protected override void OnDisposing()
        {
            base.OnDisposing();
            
            // Event subscriptions'ları temizle
            RequestNewDocument = null;
            RequestOpenDocument = null;
        }
    }
}