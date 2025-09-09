using System.Windows;
using CspProject.Data;
using Microsoft.EntityFrameworkCore;
using UserControl = System.Windows.Controls.UserControl;

namespace CspProject.Views;

public partial class HomeContentView : UserControl
{
    
    public event EventHandler? RequestNewDocument;
    public event EventHandler<int>? RequestOpenDocument;
    public HomeContentView()
    {
        InitializeComponent(); 
        LoadRecentDocuments();
    }
    
    
       private async void LoadRecentDocuments(string statusFilter = "All", string searchText = "")
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            RecentDocumentsGrid.IsEnabled = false;
            CreateNewButton.IsEnabled = false;
            //await Task.Delay(1);
            try
            {
                using (var dbContext = new ApplicationDbContext())
                {
                    var query = dbContext.Documents.AsQueryable();

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
                        .OrderByDescending(d => d.ModifiedDate)
                        .Take(10) 
                        .ToListAsync();
                    
                    var documentsForDisplay = documentsFromDb.Select(d => new 
                    {
                        d.Id,
                        d.DocumentName,
                        AuthorName = d.Author?.Name, // Null kontrolü ile yazar adını al
                        ApproverName = d.Approver?.Name, // Null kontrolü ile onaycı adını al
                        d.Status,
                        ModifiedDate = FormatDate(d.ModifiedDate),
                        d.Version
                    }).ToList();

                    RecentDocumentsGrid.ItemsSource = documentsForDisplay;
                }
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
                // DÜZELTME: SelectedItem'ı cast etmek yerine, Id'yi doğrudan satırdan oku.
                int docId = (int)grid.GetCellValue(grid.View.FocusedRowHandle, "Id");
                RequestOpenDocument?.Invoke(this, docId);
            }
            
           
        }
        
        

}