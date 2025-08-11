using System.Windows;
using CspProject.Data;
 
using Microsoft.EntityFrameworkCore;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace CspProject.Views;

    public partial class HomeScreen : UserControl
    {
        public event EventHandler? RequestNewDocument;
        public event EventHandler<int>? RequestOpenDocument;

        public HomeScreen()
        {
            InitializeComponent();
            LoadRecentDocuments();
        }

        private async void LoadRecentDocuments(string statusFilter = "All", string searchText = "")
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            RecentDocumentsGrid.IsEnabled = false;
            CreateNewButton.IsEnabled = false;
            await Task.Delay(1);
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

                    var documents = await query
                        .OrderByDescending(d => d.ModifiedDate)
                        .Take(10) 
                        .ToListAsync();
                    
                    RecentDocumentsGrid.ItemsSource = documents;
                }
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                RecentDocumentsGrid.IsEnabled = true;
                CreateNewButton.IsEnabled = true;
            }
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
            if (grid.SelectedItem is Data.Entities.Document selectedDoc)
            {
             
                RequestOpenDocument?.Invoke(this, selectedDoc.Id);
            }
        }
    }