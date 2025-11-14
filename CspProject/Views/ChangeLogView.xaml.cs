using System.Windows;
using System.Windows.Controls;
using CspProject.Data;
using CspProject.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CspProject.Views
{
    public partial class ChangeLogView : UserControl
    {
        private readonly ApplicationDbContext _dbContext;

        public ChangeLogView(ApplicationDbContext dbContext)
        {
            InitializeComponent();
            _dbContext = dbContext;
            
            LoadFilterDropDowns();
            LoadChangeLogs();
        }

        private async void LoadFilterDropDowns()
        {
            // Kullanıcı filtresini doldur
            var users = await _dbContext.Users.OrderBy(u => u.Name).ToListAsync();
            UserComboBox.ItemsSource = users;

            // Doküman filtresini doldur
            var documents = await _dbContext.Documents.OrderBy(d => d.DocumentName).ToListAsync();
            DocumentComboBox.ItemsSource = documents;
        }

        private async void LoadChangeLogs()
        {
            var query = _dbContext.AuditLogs
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
            // Filtrelerden herhangi biri değiştiğinde listeyi yeniden yükle
            LoadChangeLogs();
        }

        // ==================== EXPORT METHODS ====================

        private async void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var logs = await GetFilteredLogs();

            if (!logs.Any())
            {
                MessageBox.Show("No change history available to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    // System-wide export - Document kolonu ile
                    ChangeLogExportService.ExportToExcel(logs, saveFileDialog.FileName);
                    MessageBox.Show($"Log successfully exported to:\n{saveFileDialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export log.\n\nError: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            var logs = await GetFilteredLogs();

            if (!logs.Any())
            {
                MessageBox.Show("No change history available to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    // System-wide export - Document kolonu ile
                    ChangeLogExportService.ExportToPdf(logs, saveFileDialog.FileName);
                    MessageBox.Show($"Log successfully exported to:\n{saveFileDialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export log.\n\nError: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ==================== HELPER METHOD ====================

        private async Task<List<Data.Entities.AuditLog>> GetFilteredLogs()
        {
            var query = _dbContext.AuditLogs
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