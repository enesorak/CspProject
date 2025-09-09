using CspProject.Data;
using CspProject.Data.Entities;
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
        var selectedDoc = grid.SelectedItem as Document;
        if (selectedDoc == null) return; 
        RequestOpenDocument?.Invoke(this, selectedDoc.Id);
    }
}