using CspProject.Data;
using Microsoft.EntityFrameworkCore;
using UserControl = System.Windows.Controls.UserControl;

namespace CspProject.Views;

public partial class MyDocumentsView : UserControl
{
    public event EventHandler<int>? RequestOpenDocument;

    public MyDocumentsView()
    {
        InitializeComponent();
        LoadAllDocuments();
    }

    private async void LoadAllDocuments()
    {
        using (var dbContext = new ApplicationDbContext())
        {
            var documents = await dbContext.Documents
                .OrderByDescending(d => d.ModifiedDate)
                .ToListAsync();
            AllDocumentsGrid.ItemsSource = documents;
        }
    }

    private void AllDocumentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var grid = (DevExpress.Xpf.Grid.GridControl)sender;
        if (grid.SelectedItem is Data.Entities.Document selectedDoc)
        {
            RequestOpenDocument?.Invoke(this, selectedDoc.Id);
        }
    }
}