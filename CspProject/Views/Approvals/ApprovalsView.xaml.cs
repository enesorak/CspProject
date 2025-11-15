// Views/ApprovalsView.xaml.cs - GÜNCELLENECEK

using System.Windows;
using CspProject.Views.Base;
using Microsoft.EntityFrameworkCore;

namespace CspProject.Views.Approvals
{
    public partial class ApprovalsView : ViewBase // ✅ UserControl → ViewBase
    {
        public event EventHandler<int>? RequestOpenDocument;

        public ApprovalsView()
        {
            InitializeComponent();
            
            // ❌ KALDIR - Çok erken
            // LoadApprovalDocuments();
        }

        // ✅ EKLE - ViewBase lifecycle
        protected override void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            base.OnViewLoaded(sender, e);
            
            // DbContext artık hazır
            LoadApprovalDocuments();
        }

        private async void LoadApprovalDocuments()
        {
            // ✅ DbContext null check
            if (DbContext == null) return;

            // ✅ Yeni instance yerine mevcut DbContext kullan
            var documentsFromDb = await DbContext.Documents
                .Where(d => d.Status == "Under Review")
                .OrderByDescending(d => d.ModifiedDate)
                .Include(document => document.Author)
                .ToListAsync();

            var documentsForDisplay = documentsFromDb.Select(d => new
            {
                d.Id,
                d.DocumentName,
                AuthorName = d.Author?.Name,
                d.Version,
                ModifiedDate = FormatDate(d.ModifiedDate)
            }).ToList();

            ApprovalsGrid.ItemsSource = documentsForDisplay;
        }

        private string FormatDate(DateTime date)
        {
            if (date.Date == DateTime.Today) return "Today";
            if (date.Date == DateTime.Today.AddDays(-1)) return "Yesterday";
            return date.ToString("d");
        }

        private void ApprovalsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
            
            // Event cleanup
            RequestOpenDocument = null;
        }
    }
}