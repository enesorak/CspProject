// Views/ChangeLogView.xaml.cs - GÜNCELLENECEK

using System.Windows;
using CspProject.Services.Export;
using Microsoft.EntityFrameworkCore;
using ViewBase = CspProject.Views.Base.ViewBase;

namespace CspProject.Views.Audit
{
    public partial class ChangeLogView : ViewBase // ✅ UserControl → ViewBase
    {
        // ❌ KALDIR
        // private readonly ApplicationDbContext _dbContext;

        // ❌ ÖNCE
        // public ChangeLogView(ApplicationDbContext dbContext)
        // {
        //     InitializeComponent();
        //     _dbContext = dbContext;
        //     LoadFilterDropDowns();
        //     LoadChangeLogs();
        // }

        // ✅ SONRA
        public ChangeLogView()
        {
            InitializeComponent();
        }

        // ✅ EKLE
        protected override void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            base.OnViewLoaded(sender, e);
            
            LoadFilterDropDowns();
            LoadChangeLogs();
        }

        private async void LoadFilterDropDowns()
        {
            if (DbContext == null) return;

            var users = await DbContext.Users.OrderBy(u => u.Name).ToListAsync();
            UserComboBox.ItemsSource = users;

            var documents = await DbContext.Documents.OrderBy(d => d.DocumentName).ToListAsync();
            DocumentComboBox.ItemsSource = documents;
        }

        private async void LoadChangeLogs()
        {
            if (DbContext == null) return;

            var query = DbContext.AuditLogs
                .Include(log => log.User)
                .Include(log => log.Document)
                .AsQueryable();

            // Tarih filtresi
            if (StartDateEdit.DateTime != DateTime.MinValue)
            {
                query = query.Where(log => log.Timestamp >= StartDateEdit.DateTime);
            }
            if (EndDateEdit.DateTime != DateTime.MinValue)
            {
                var endDate = EndDateEdit.DateTime.Date.AddDays(1);
                query = query.Where(log => log.Timestamp < endDate);
            }

            // Kullanıcı filtresi
            if (UserComboBox.SelectedItem is Data.Entities.User selectedUser)
            {
                query = query.Where(log => log.UserId == selectedUser.Id);
            }

            // Doküman filtresi
            if (DocumentComboBox.SelectedItem is Data.Entities.Document selectedDocument)
            {
                query = query.Where(log => log.DocumentId == selectedDocument.Id);
            }

            var logs = await query
                .OrderByDescending(log => log.Timestamp)
                .Select(log => new
                {
                    log.Timestamp,
                    UserName = log.User.Name,
                    DocumentName = log.Document.DocumentName,
                    log.FieldChanged,
                    log.OldValue,
                    log.NewValue,
                    log.Revision
                })
                .ToListAsync();

            ChangeLogGrid.ItemsSource = logs;
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            LoadChangeLogs();
        }

        private async void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var logs = await GetFilteredLogs();

            if (!logs.Any())
            {
                MessageBox.Show("No change history available to export.", "No Data", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"ChangeLog_{DateTime.Now:yyyy-MM-dd}.xlsx",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ChangeLogExportService.ExportToExcel(logs, saveFileDialog.FileName);
                    MessageBox.Show($"Log successfully exported to:\n{saveFileDialog.FileName}", 
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export log.\n\nError: {ex.Message}", 
                        "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            var logs = await GetFilteredLogs();

            if (!logs.Any())
            {
                MessageBox.Show("No change history available to export.", "No Data", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"ChangeLog_{DateTime.Now:yyyy-MM-dd}.pdf",
                Filter = "PDF Document (*.pdf)|*.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ChangeLogExportService.ExportToPdf(logs, saveFileDialog.FileName);
                    MessageBox.Show($"Log successfully exported to:\n{saveFileDialog.FileName}", 
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export log.\n\nError: {ex.Message}", 
                        "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task<List<Data.Entities.AuditLog>> GetFilteredLogs()
        {
            if (DbContext == null) return new List<Data.Entities.AuditLog>();

            var query = DbContext.AuditLogs
                .Include(log => log.User)
                .Include(log => log.Document)
                .AsQueryable();

            // Tarih filtresi
            if (StartDateEdit.DateTime != DateTime.MinValue)
            {
                query = query.Where(log => log.Timestamp >= StartDateEdit.DateTime);
            }
            if (EndDateEdit.DateTime != DateTime.MinValue)
            {
                var endDate = EndDateEdit.DateTime.Date.AddDays(1);
                query = query.Where(log => log.Timestamp < endDate);
            }

            // Kullanıcı filtresi
            if (UserComboBox.SelectedItem is Data.Entities.User selectedUser)
            {
                query = query.Where(log => log.UserId == selectedUser.Id);
            }

            // Doküman filtresi
            if (DocumentComboBox.SelectedItem is Data.Entities.Document selectedDocument)
            {
                query = query.Where(log => log.DocumentId == selectedDocument.Id);
            }

            return await query.OrderByDescending(log => log.Timestamp).ToListAsync();
        }
    }
}