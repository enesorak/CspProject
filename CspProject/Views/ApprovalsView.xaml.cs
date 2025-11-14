using CspProject.Data;
using Microsoft.EntityFrameworkCore;
 

namespace CspProject.Views;
 
 
 
public partial class ApprovalsView : UserControl
{
    public event EventHandler<int>? RequestOpenDocument;

    public ApprovalsView()
    {
        InitializeComponent();
        LoadApprovalDocuments();
    }

    private async void LoadApprovalDocuments()
    {
        using (var dbContext = new ApplicationDbContext())
        {
            var documentsFromDb = await dbContext.Documents
                .Where(d => d.Status == "Under Review")
                .OrderByDescending(d => d.ModifiedDate).Include(document => document.Author)
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
            // DÜZELTME: SelectedItem'ı cast etmek yerine, Id'yi doğrudan satırdan oku.
            int docId = (int)grid.GetCellValue(grid.View.FocusedRowHandle, "Id");
            RequestOpenDocument?.Invoke(this, docId);
        }

    }
}