using CspProject.Data;
using DevExpress.Xpf.Core;
using Microsoft.EntityFrameworkCore;

namespace CspProject.Views
{
    public partial class DocumentChangeLogWindow : ThemedWindow
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly int _documentId;

        public DocumentChangeLogWindow(ApplicationDbContext dbContext, int documentId)
        {
            InitializeComponent();
            _dbContext = dbContext;
            _documentId = documentId;
            LoadDocumentChangeLogs();
        }

        private async void LoadDocumentChangeLogs()
        {
            var document = await _dbContext.Documents.FindAsync(_documentId);
            if (document != null)
            {
                TitleTextBlock.Text = $"Change Log for '{document.DocumentName}'";
            }

            var logs = await _dbContext.AuditLogs
                .Where(log => log.DocumentId == _documentId) // THE CRITICAL FILTER
                .Include(log => log.User)
                .OrderByDescending(log => log.Timestamp)
                .Select(log => new
                {
                    log.Timestamp,
                    UserName = log.User.Name,
                    log.FieldChanged,
                    log.OldValue,
                    log.NewValue,
                    log.Revision
                })
                .ToListAsync();

            ChangeLogGrid.ItemsSource = logs;
        }
    }
}