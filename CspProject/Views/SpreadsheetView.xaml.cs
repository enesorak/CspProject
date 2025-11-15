// Views/SpreadsheetView.xaml.cs - GÜNCELLENECEK
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services;
using DevExpress.Mvvm;
using DevExpress.XtraSpreadsheet;
using Microsoft.EntityFrameworkCore;

namespace CspProject.Views
{
    public partial class SpreadsheetView : ViewBase // ✅ UserControl → ViewBase (zaten IDisposable vardı)
    {
        public event EventHandler<string> ProcessingStarted;
        public event EventHandler ProcessingFinished;
        public event EventHandler? RequestGoToHome;
        public event Action<string>? DocumentInfoChanged;

        private readonly User? _currentUser;
        private Document? _currentDocument;
        
        // ❌ KALDIR - ViewBase'den gelecek
        // private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
        
        private bool _isAuditPanelOpen = false;

        // ❌ KALDIR - ViewBase'de zaten var
        // private bool _disposed = false;

        private INotificationService? NotificationService => 
            ServiceContainer.Default.GetService<INotificationService>();

        public SpreadsheetView(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            StatusLabel.Content = $"Status: {(string.IsNullOrWhiteSpace(_currentDocument?.Status) ? "New" : _currentDocument?.Status)}";

            // ✅ Loaded/Unloaded event'leri ViewBase hallediyor, kaldırabiliriz
            // this.Loaded += SpreadsheetView_Loaded;
            // this.Unloaded += SpreadsheetView_Unloaded;

            spreadsheetControl.CellValueChanged += SpreadsheetControl_CellValueChanged;
        }

        // ✅ EKLE - ViewBase lifecycle override
        protected override void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            base.OnViewLoaded(sender, e);
            
            // Document update service subscription
            DocumentUpdateService.Instance.DocumentUpdated += OnDocumentUpdatedInBackground;
        }

        private async void SpreadsheetControl_CellValueChanged(object sender, SpreadsheetCellEventArgs e)
        {
            if (_currentDocument == null || _currentDocument.Id == 0 || _currentUser == null) return;
            if (e.RowIndex < 8) return;
            if (DbContext == null) return; // ✅ Null check

            var log = new AuditLog
            {
                DocumentId = _currentDocument.Id,
                UserId = _currentUser.Id,
                Timestamp = DateTime.Now,
                FieldChanged = $"Cell: {e.Cell.GetReferenceA1()}",
                OldValue = e.OldValue.ToString(),
                NewValue = e.Value.ToString(),
                Revision = _currentDocument.Version,
                Rationale = "N/A"
            };

            DbContext.AuditLogs.Add(log);
            await DbContext.SaveChangesAsync();
        }

        private async void OnDocumentUpdatedInBackground(object? sender, int updatedDocumentId)
        {
            if (_currentDocument != null && _currentDocument.Id == updatedDocumentId)
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await LoadDocument(_currentDocument.Id);
                    MessageBox.Show("The status of the current document was updated in the background.", 
                        "Document Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
        }

        public async Task CreateNewFmeaDocumentAsync()
        {
            if (_currentUser == null) return;

            ITransactionTracer transaction = SentrySdk.StartTransaction("create-fmea-document", "document");

            try
            {
                _currentDocument = new Document { AuthorId = _currentUser.Id };

                var templateSpan = transaction.StartChild("template.load");
                byte[] bytes = await LoadTemplateWithCache();
                templateSpan.Finish(SpanStatus.Ok);

                var uiSpan = transaction.StartChild("ui.load");
                using var ms = new MemoryStream(bytes);
                spreadsheetControl.LoadDocument(ms, DevExpress.Spreadsheet.DocumentFormat.Xlsx);
                uiSpan.Finish(SpanStatus.Ok);

                spreadsheetControl.Modified = false;
                UpdateUiForDocumentStatus();

                transaction.Finish(SpanStatus.Ok);
                SentrySdk.AddBreadcrumb("FMEA document created", "document");
            }
            catch (Exception ex)
            {
                transaction.Finish(ex);
                SentrySdk.CaptureException(ex);
                await ShowNotification("Error", $"Failed to create document: {ex.Message}");
            }
        }

        private static byte[]? _cachedFmeaTemplate;
        private static readonly object _cacheLock = new object();

        private async Task<byte[]> LoadTemplateWithCache()
        {
            if (_cachedFmeaTemplate != null)
            {
                SentrySdk.AddBreadcrumb("Template loaded from cache", "performance");
                return _cachedFmeaTemplate;
            }

            return await Task.Run(() =>
            {
                lock (_cacheLock)
                {
                    if (_cachedFmeaTemplate != null)
                        return _cachedFmeaTemplate;

                    var prevCulture = Thread.CurrentThread.CurrentCulture;
                    try
                    {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                        var workbook = new DevExpress.Spreadsheet.Workbook();
                        FmeaTemplateGenerator.Apply(workbook);

                        using var ms = new MemoryStream();
                        workbook.SaveDocument(ms, DevExpress.Spreadsheet.DocumentFormat.Xlsx);
                        _cachedFmeaTemplate = ms.ToArray();

                        SentrySdk.AddBreadcrumb(
                            $"Template cached ({_cachedFmeaTemplate.Length / 1024} KB)",
                            "performance");

                        return _cachedFmeaTemplate;
                    }
                    finally
                    {
                        Thread.CurrentThread.CurrentCulture = prevCulture;
                    }
                }
            });
        }

        public async Task LoadDocument(int documentId)
        {
            if (DbContext == null) return; // ✅ Null check

            ITransactionTracer transaction = SentrySdk.StartTransaction("load-document", "document");
            transaction.SetExtra("document_id", documentId);

            try
            {
                var loadSpan = transaction.StartChild("db.query");
                _currentDocument = await DbContext.Documents
                    .Include(d => d.Author)
                    .Include(d => d.Approver)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == documentId);
                loadSpan.Finish(SpanStatus.Ok);

                if (_currentDocument?.Content != null)
                {
                    var renderSpan = transaction.StartChild("spreadsheet.render");
                    spreadsheetControl.LoadDocument(_currentDocument.Content,
                        DevExpress.Spreadsheet.DocumentFormat.Xlsx);
                    spreadsheetControl.Modified = false;
                    renderSpan.Finish(SpanStatus.Ok);

                    UpdateUiForDocumentStatus();
                }

                transaction.Finish(SpanStatus.Ok);
            }
            catch (Exception ex)
            {
                transaction.Finish(ex);
                SentrySdk.CaptureException(ex);
                await ShowNotification("Error", $"Failed to load document: {ex.Message}");
            }
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (DbContext == null) return; // ✅ Null check

            var openWindow = new OpenDocumentWindow(DbContext);

            if (openWindow.ShowDialog() == true)
            {
                int docId = openWindow.SelectedDocumentId;
                await LoadDocument(docId);
                await ShowNotification("Document Loaded",
                    $"Document '{_currentDocument?.DocumentName}' (Version: {_currentDocument?.Version}) loaded successfully.");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformSaveAsync(true);
        }

        private async void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null)
            {
                await ShowNotification("Cannot Rename", "Please save the document first before renaming.");
                return;
            }

            if (DbContext == null) return; // ✅ Null check

            var saveWindow = new SaveDocumentWindow(_currentDocument.DocumentName) 
                { Owner = Window.GetWindow(this) };
            
            if (saveWindow.ShowDialog() == true)
            {
                _currentDocument.DocumentName = saveWindow.DocumentName;
                await DbContext.SaveChangesAsync();
                await ShowNotification("Rename Successful",
                    $"Document successfully renamed to '{_currentDocument.DocumentName}'.");
            }

            UpdateUiForDocumentStatus();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var activeSheet = spreadsheetControl.Document.Worksheets.ActiveWorksheet;
            var usedRange = activeSheet.GetUsedRange();

            bool isSheetEmpty = usedRange.RowCount == 1 &&
                                usedRange.ColumnCount == 1 &&
                                usedRange[0, 0].Value.IsEmpty;

            if (isSheetEmpty)
            {
                await ShowNotification("Warning", "There is no document to export.");
                return;
            }

            var worksheet = spreadsheetControl.Document.Worksheets[0];
            string fmeaId = worksheet.Cells["G4"].Value.ToString() ?? "Untitled";
            string version = worksheet.Cells["I4"].Value.ToString() ?? "0.0.0";

            string safeFmeaId = string.Join("_", fmeaId.Split(Path.GetInvalidFileNameChars()));
            string safeVersion = string.Join("_", version.Split(Path.GetInvalidFileNameChars()));

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{safeFmeaId}-Rev{safeVersion}.xlsx",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    spreadsheetControl.BeginUpdate();
                    spreadsheetControl.SaveDocument(saveFileDialog.FileName, 
                        DevExpress.Spreadsheet.DocumentFormat.Xlsx);
                    await ShowNotification("Export Successful",
                        $"Document successfully exported to:\n{saveFileDialog.FileName}");
                }
                finally
                {
                    spreadsheetControl.EndUpdate();
                }
            }
        }

        private void BackToHome_Click(object sender, RoutedEventArgs e)
        {
            RequestGoToHome?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateDocumentFromSpreadsheet(Document doc)
        {
            spreadsheetControl.BeginUpdate();
            try
            {
                var worksheet = spreadsheetControl.Document.Worksheets[0];

                doc.ProductPart = worksheet.Cells["B4"].Value.ToString();
                doc.FmeaId = worksheet.Cells["G4"].Value.ToString();
                doc.ProjectName = worksheet.Cells["B6"].Value.ToString();
                doc.ResponsibleParty = worksheet.Cells["G6"].Value.ToString();
                doc.ApprovedBy = worksheet.Cells["I6"].Value.ToString();
                doc.Team = worksheet.Cells["B8"].Value.ToString();

                doc.ModifiedDate = DateTime.Now;
                doc.Version = _currentDocument?.Version ?? "0.0.1";

                worksheet.Cells["I4"].Value = doc.Version;

                using (var finalContentStream = new MemoryStream())
                {
                    spreadsheetControl.SaveDocument(finalContentStream, 
                        DevExpress.Spreadsheet.DocumentFormat.Xlsx);
                    doc.Content = finalContentStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while preparing the document for saving: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                spreadsheetControl.EndUpdate();
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (DbContext == null) return; // ✅ Null check

            ITransactionTracer transaction = SentrySdk.StartTransaction("submit-for-review", "workflow");

            try
            {
                var saveResult = await PerformSaveAsync(showSuccessNotification: false);

                if (!saveResult)
                {
                    transaction.Finish(SpanStatus.Cancelled);
                    return;
                }

                if (_currentDocument == null || _currentUser == null)
                {
                    transaction.Finish(SpanStatus.InvalidArgument);
                    return;
                }

                var selectionWindow = new ApproverSelectionWindow(_currentUser.Id) 
                    { Owner = Window.GetWindow(this) };
                
                if (selectionWindow.ShowDialog() != true)
                {
                    transaction.Finish(SpanStatus.Cancelled);
                    return;
                }

                int? approverId = selectionWindow.SelectedApproverId;
                if (!approverId.HasValue)
                {
                    transaction.Finish(SpanStatus.InvalidArgument);
                    return;
                }

                var approver = await DbContext.Users.FindAsync(approverId.Value);
                if (approver == null)
                {
                    await ShowNotification("Error", "Could not find the selected approver.");
                    transaction.Finish(SpanStatus.NotFound);
                    return;
                }

                ProcessingStarted?.Invoke(this, "Sending approval email, please wait...");

                var oldStatus = _currentDocument.Status;
                _currentDocument.ApproverId = approverId.Value;
                _currentDocument.Status = "Under Review";
                _currentDocument.Version = VersioningService.IncrementMinorVersion(_currentDocument.Version);

                DbContext.AuditLogs.Add(new AuditLog
                {
                    DocumentId = _currentDocument.Id,
                    UserId = _currentUser.Id,
                    FieldChanged = "Status",
                    OldValue = oldStatus,
                    NewValue = "Under Review",
                    Revision = _currentDocument.Version,
                    Rationale = $"Submitted to {approver.Name}"
                });

                var emailSpan = transaction.StartChild("email.send");
                byte[] pdfBytes = PdfExportService.ExportToPdfBytes(spreadsheetControl);
                var emailService = new EmailService(DbContext);
                await emailService.SendApprovalRequestEmailAsync(approver, _currentDocument, pdfBytes);
                emailSpan.Finish(SpanStatus.Ok);

                await SaveAndRefreshUi();

                ProcessingFinished?.Invoke(this, EventArgs.Empty);
                transaction.Finish(SpanStatus.Ok);

                await ShowNotification("Submission Successful",
                    $"Document has been submitted and an email was sent to {approver.Email}.");
            }
            catch (Exception ex)
            {
                ProcessingFinished?.Invoke(this, EventArgs.Empty);
                transaction.Finish(ex);
                SentrySdk.CaptureException(ex);

                await ShowNotification("Email Sending Failed",
                    $"The document was NOT submitted. Please check your email settings.\n\nError: {ex.Message}");
            }
        }

        private async Task<bool> PerformSaveAsync(bool showSuccessNotification)
        {
            if (DbContext == null) return false; // ✅ Null check

            bool isNewDocument = _currentDocument == null || _currentDocument.Id == 0;

            if (!isNewDocument && !spreadsheetControl.Modified)
            {
                if (showSuccessNotification)
                {
                    await ShowNotification("Information", "No changes to save.");
                }

                return true;
            }

            if (isNewDocument)
            {
                var worksheet = spreadsheetControl.Document.Worksheets[0];
                string defaultName = worksheet.Cells["G4"].Value.ToString() ?? "New Document";
                var saveWindow = new SaveDocumentWindow(defaultName) 
                    { Owner = Window.GetWindow(this) };

                if (saveWindow.ShowDialog() != true) return false;

                _currentDocument ??= new Document { AuthorId = _currentUser.Id };
                _currentDocument.DocumentName = saveWindow.DocumentName;
                _currentDocument.Version = "0.0.1";
                DbContext.Documents.Add(_currentDocument);
            }
            else
            {
                _currentDocument.Version = VersioningService.IncrementPatchVersion(_currentDocument.Version);
            }

            UpdateDocumentFromSpreadsheet(_currentDocument);
            await DbContext.SaveChangesAsync();
            spreadsheetControl.Modified = false;

            UpdateUiForDocumentStatus();

            if (showSuccessNotification)
            {
                await ShowNotification("Save Successful",
                    $"Document '{_currentDocument.DocumentName}' saved as version {_currentDocument.Version}!");
            }

            return true;
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null || _currentUser == null || DbContext == null) return;
            
            if (_currentDocument.AuthorId == _currentUser.Id)
            {
                await ShowNotification("Authorization Error", 
                    "Authors cannot approve their own documents.");
                return;
            }

            var oldStatus = _currentDocument.Status;
            _currentDocument.Status = "Approved";
            _currentDocument.Version = VersioningService.IncrementMajorVersion(_currentDocument.Version);
            _currentDocument.ApprovedBy = _currentUser.Name;
            _currentDocument.DateCompleted = DateTime.Now;

            DbContext.AuditLogs.Add(new AuditLog
            {
                DocumentId = _currentDocument.Id,
                UserId = _currentUser.Id,
                FieldChanged = "Status",
                OldValue = oldStatus,
                NewValue = "Approved",
                Revision = _currentDocument.Version,
                Rationale = "Approved via application UI"
            });

            var worksheet = spreadsheetControl.Document.Worksheets[0];
            worksheet.Cells["I6"].Value = _currentUser.Name;
            worksheet.Cells["K6"].Value = DateTime.Now;

            await SaveAndRefreshUi();

            await ShowNotification("Document Approved", 
                $"Document status changed to: {_currentDocument.Status}");
        }

        private async void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null || DbContext == null) return;

            var oldStatus = _currentDocument.Status;
            _currentDocument.Status = "Draft";

            DbContext.AuditLogs.Add(new AuditLog
            {
                DocumentId = _currentDocument.Id,
                UserId = _currentUser.Id,
                FieldChanged = "Status",
                OldValue = oldStatus,
                NewValue = "Draft",
                Revision = _currentDocument.Version,
                Rationale = "Rejected via application UI"
            });

            await SaveAndRefreshUi();
            await ShowNotification("Document Rejected", 
                $"Document status changed to: {_currentDocument.Status}");
        }

        private async Task SaveAndRefreshUi()
        {
            if (DbContext == null) return;

            await DbContext.SaveChangesAsync();

            spreadsheetControl.BeginUpdate();
            try
            {
                if (_currentDocument != null)
                {
                    var worksheet = spreadsheetControl.Document.Worksheets[0];
                    worksheet.Cells["I4"].Value = _currentDocument.Version;
                    spreadsheetControl.Document.CalculateFull();
                }
            }
            finally
            {
                spreadsheetControl.EndUpdate();
            }

            UpdateUiForDocumentStatus();
        }

        private void UpdateUiForDocumentStatus()
        {
            if (_currentDocument == null || _currentUser == null) return;

            string status = _currentDocument.Status ?? "Draft";
            DocumentInfoChanged?.Invoke($"Status: {status} | Version: {_currentDocument.Version}");

            StatusLabel.Content = $"Status: {status}";
            VersionLabel.Content = $"Version: {_currentDocument.Version}";

            bool isCurrentUserTheAuthor = _currentDocument.AuthorId == _currentUser.Id;
            bool isCurrentUserTheApprover =
                _currentDocument.ApproverId.HasValue && 
                _currentDocument.ApproverId.Value == _currentUser.Id;

            if (status == "Approved")
            {
                spreadsheetControl.ReadOnly = true;
                SubmitButton.IsEnabled = false;
                ApproveButton.IsEnabled = false;
                RejectButton.IsEnabled = false;
                CheckApprovalsButton.IsEnabled = false;

                ApprovalStatusLabel.IsVisible = true;
                string approverName = _currentDocument.ApprovedBy ?? 
                    _currentDocument.Approver?.Name ?? "N/A";
                string approvalDate = _currentDocument.DateCompleted?.ToString("d") ?? "N/A";
                ApprovalStatusLabel.Content = $"Approved by {approverName} on {approvalDate}";
            }
            else
            {
                bool isEditable = (status == "Draft" && isCurrentUserTheAuthor);
                spreadsheetControl.ReadOnly = !isEditable;

                SubmitButton.IsEnabled = (status == "Draft" && isCurrentUserTheAuthor);
                ApproveButton.IsEnabled = (status == "Under Review" && isCurrentUserTheApprover);
                RejectButton.IsEnabled = (status == "Under Review" && isCurrentUserTheApprover);
                CheckApprovalsButton.IsEnabled = true;

                ApprovalStatusLabel.IsVisible = false;
            }

            var worksheet = spreadsheetControl.Document.Worksheets[0];
            worksheet.Cells["I4"].Value = _currentDocument.Version;
            worksheet.Cells["K4"].Value = _currentDocument.ModifiedDate;
        }

        private async void CheckApprovalsButton_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (DbContext == null) return;

            CheckApprovalsButton.IsEnabled = false;
            await ShowNotification("Checking Approvals", 
                "Checking for new approvals in the background...");

            var receiverService = new EmailReceiverService(DbContext);
            try
            {
                string result = await receiverService.CheckForApprovalEmailsAsync();

                if (_currentDocument != null)
                {
                    await DbContext.Entry(_currentDocument).ReloadAsync();
                    UpdateUiForDocumentStatus();
                }
            }
            catch (Exception ex)
            {
                await ShowNotification("Error",
                    $"Failed to check for approvals. Please verify your email settings.\n\nError: {ex.Message}");
            }
            finally
            {
                CheckApprovalsButton.IsEnabled = true;
            }
        }

        private async Task ShowNotification(string title, string message)
        {
            if (NotificationService != null)
            {
                var notification = NotificationService.CreatePredefinedNotification(title, message, "");
                await notification.ShowAsync();
            }
            else
            {
                MessageBox.Show(message, title);
            }
        }

        private async void DocumentChangeLogButton_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (_isAuditPanelOpen)
            {
                CloseAuditPanel();
            }
            else
            {
                if (_currentDocument == null || _currentDocument.Id == 0)
                {
                    await ShowNotification("Information", 
                        "Please save the document first to view its history.");
                    return;
                }

                if (DbContext == null) return;

                var logs = await DbContext.AuditLogs
                    .Where(log => log.DocumentId == _currentDocument.Id)
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

                if (!logs.Any())
                {
                    await ShowNotification("No History", 
                        "No change history available for this document.");
                    return;
                }

                AuditLogItemsControl.ItemsSource = logs;
                OpenAuditPanel();
            }
        }

        private void OpenAuditPanel()
        {
            AuditLogPanel.Visibility = Visibility.Visible;
            _isAuditPanelOpen = true;

            var animation = new DoubleAnimation
            {
                From = 450,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            PanelTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private void CloseAuditPanel()
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 450,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            animation.Completed += (s, args) =>
            {
                AuditLogPanel.Visibility = Visibility.Collapsed;
                _isAuditPanelOpen = false;
            };

            PanelTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private void CloseAuditPanel_Click(object sender, RoutedEventArgs e)
        {
            CloseAuditPanel();
        }

        private async void FlyoutExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null || _currentDocument.Id == 0 || DbContext == null)
            {
                await ShowNotification("Information", 
                    "Please save the document first to export its history.");
                return;
            }

            var logs = await DbContext.AuditLogs
                .Where(log => log.DocumentId == _currentDocument.Id)
                .Include(log => log.User)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            if (!logs.Any())
            {
                await ShowNotification("No History", "No change history available to export.");
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"AuditLog_{_currentDocument.DocumentName}_{DateTime.Now:yyyy-MM-dd}.xlsx",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    AuditLogExportService.ExportToExcel(logs, saveFileDialog.FileName, 
                        _currentDocument.DocumentName);
                    await ShowNotification("Export Successful",
                        $"Log successfully exported to:\n{saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    await ShowNotification("Export Failed", 
                        $"Failed to export audit log.\n\nError: {ex.Message}");
                }
            }
        }

        private async void FlyoutExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null || _currentDocument.Id == 0 || DbContext == null)
            {
                await ShowNotification("Information", 
                    "Please save the document first to export its history.");
                return;
            }

            var logs = await DbContext.AuditLogs
                .Where(log => log.DocumentId == _currentDocument.Id)
                .Include(log => log.User)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            if (!logs.Any())
            {
                await ShowNotification("No History", "No change history available to export.");
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"AuditLog_{_currentDocument.DocumentName}_{DateTime.Now:yyyy-MM-dd}.pdf",
                Filter = "PDF Document (*.pdf)|*.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    AuditLogExportService.ExportToPdf(logs, saveFileDialog.FileName, 
                        _currentDocument.DocumentName);
                    await ShowNotification("Export Successful",
                        $"Log successfully exported to:\n{saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    await ShowNotification("Export Failed", 
                        $"Failed to export audit log.\n\nError: {ex.Message}");
                }
            }
        }

        public async Task LoadTemplateFromFile(string filePath)
        {
            if (_currentUser == null) return;

            ITransactionTracer transaction = SentrySdk.StartTransaction("load-template-file", "document");
            transaction.SetExtra("file_path", filePath);

            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Template file not found: {filePath}");
                }

                _currentDocument = new Document
                {
                    AuthorId = _currentUser.Id,
                    DocumentName = Path.GetFileNameWithoutExtension(filePath),
                    Status = "Draft",
                    Version = "0.0.1"
                };

                var readSpan = transaction.StartChild("file.read");
                byte[] templateBytes = await File.ReadAllBytesAsync(filePath);
                readSpan.SetExtra("file_size", templateBytes.Length);
                readSpan.Finish(SpanStatus.Ok);

                var loadSpan = transaction.StartChild("spreadsheet.load");
                using (var ms = new MemoryStream(templateBytes))
                {
                    spreadsheetControl.LoadDocument(ms, DevExpress.Spreadsheet.DocumentFormat.Xlsx);
                }
                loadSpan.Finish(SpanStatus.Ok);

                spreadsheetControl.Modified = false;
                UpdateUiForDocumentStatus();

                transaction.Finish(SpanStatus.Ok);

                await ShowNotification("Template Loaded",
                    $"Template '{Path.GetFileName(filePath)}' loaded successfully.");
            }
            catch (Exception ex)
            {
                transaction.Finish(ex);
                SentrySdk.CaptureException(ex);
                await ShowNotification("Error", $"Failed to load template: {ex.Message}");
            }
        }

        public async Task CreateBlankDocument()
        {
            if (_currentUser == null) return;

            ITransactionTracer transaction = SentrySdk.StartTransaction("create-blank-document", "document");

            try
            {
                _currentDocument = new Document
                {
                    AuthorId = _currentUser.Id,
                    DocumentName = "Blank Template",
                    Status = "Draft",
                    Version = "0.0.1"
                };

                await Task.Run(() =>
                {
                    var prevCulture = Thread.CurrentThread.CurrentCulture;
                    try
                    {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                        var workbook = new DevExpress.Spreadsheet.Workbook();
                        FmeaTemplateGenerator.ApplyTitleBlockOnly(workbook);

                        using (var ms = new MemoryStream())
                        {
                            workbook.SaveDocument(ms, DevExpress.Spreadsheet.DocumentFormat.Xlsx);
                            ms.Position = 0;

                            Dispatcher.Invoke(() =>
                            {
                                spreadsheetControl.LoadDocument(ms, 
                                    DevExpress.Spreadsheet.DocumentFormat.Xlsx);
                            });
                        }
                    }
                    finally
                    {
                        Thread.CurrentThread.CurrentCulture = prevCulture;
                    }
                });

                spreadsheetControl.Modified = false;
                UpdateUiForDocumentStatus();

                transaction.Finish(SpanStatus.Ok);

                await ShowNotification("Template Base Created",
                    "Template with standard title block created. Add your custom content and save as template.");
            }
            catch (Exception ex)
            {
                transaction.Finish(ex);
                SentrySdk.CaptureException(ex);
                await ShowNotification("Error", $"Failed to create blank document: {ex.Message}");
            }
        }

        private async void SaveAsTemplateButton_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var activeSheet = spreadsheetControl.Document.Worksheets.ActiveWorksheet;
            var usedRange = activeSheet.GetUsedRange();

            bool isSheetEmpty = usedRange.RowCount == 1 &&
                                usedRange.ColumnCount == 1 &&
                                usedRange[0, 0].Value.IsEmpty;

            if (isSheetEmpty)
            {
                await ShowNotification("Cannot Save Template",
                    "The spreadsheet is empty. Please add some content first.");
                return;
            }

            var inputWindow = new TemplateNameInputWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (inputWindow.ShowDialog() != true)
                return;

            string templateName = inputWindow.TemplateName;

            if (string.IsNullOrWhiteSpace(templateName))
            {
                await ShowNotification("Invalid Name", "Please enter a valid template name.");
                return;
            }

            try
            {
                string templatesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");

                if (!Directory.Exists(templatesDir))
                    Directory.CreateDirectory(templatesDir);

                if (!templateName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    templateName += ".xlsx";

                string templatePath = Path.Combine(templatesDir, templateName);

                if (File.Exists(templatePath))
                {
                    var result = MessageBox.Show(
                        $"A template named '{templateName}' already exists. Do you want to replace it?",
                        "Confirm Replace",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                spreadsheetControl.SaveDocument(templatePath, 
                    DevExpress.Spreadsheet.DocumentFormat.Xlsx);

                await ShowNotification("Template Saved",
                    $"Template '{templateName}' saved successfully!\n\n" +
                    $"You can now find it in the Templates screen.");
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                await ShowNotification("Error",
                    $"Failed to save template.\n\nError: {ex.Message}");
            }
        }

        private async void TemplateInfoButton_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var worksheet = spreadsheetControl.Document.Worksheets.ActiveWorksheet;

            int totalSheets = spreadsheetControl.Document.Worksheets.Count;
            var usedRange = worksheet.GetUsedRange();

            string info = $"Spreadsheet Information\n\n" +
                          $"Active Sheet: {worksheet.Name}\n" +
                          $"Total Sheets: {totalSheets}\n" +
                          $"Used Range: {usedRange.GetReferenceA1()}\n" +
                          $"Rows: {usedRange.RowCount}\n" +
                          $"Columns: {usedRange.ColumnCount}";

            await ShowNotification("Template Info", info);
        }

        // ✅ GÜNCELLE - ViewBase'in OnDisposing metodunu override et
        protected override void OnDisposing()
        {
            base.OnDisposing();

            // Event subscriptions cleanup
            DocumentUpdateService.Instance.DocumentUpdated -= OnDocumentUpdatedInBackground;
            spreadsheetControl.CellValueChanged -= SpreadsheetControl_CellValueChanged;

            // Clear events
            ProcessingStarted = null;
            ProcessingFinished = null;
            RequestGoToHome = null;
            DocumentInfoChanged = null;
        }

        // ❌ KALDIR - ViewBase hallediyor
        // public void Dispose()
        // {
        //     Dispose(true);
        //     GC.SuppressFinalize(this);
        // }
        //
        // protected virtual void Dispose(bool disposing)
        // {
        //     if (!_disposed)
        //     {
        //         if (disposing)
        //         {
        //             _dbContext?.Dispose();
        //         }
        //         _disposed = true;
        //     }
        // }
        //
        // private void SpreadsheetView_Unloaded(object sender, RoutedEventArgs e)
        // {
        //     DocumentUpdateService.Instance.DocumentUpdated -= OnDocumentUpdatedInBackground;
        //     Dispose();
        // }
    }
}