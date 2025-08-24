using System.IO;
using System.Windows;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services;
using DevExpress.Spreadsheet;
using DevExpress.Xpf.Bars;
using Microsoft.EntityFrameworkCore;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace CspProject.Views;

public partial class SpreadsheetView : UserControl
{
    public event EventHandler? RequestGoToHome;
    public event Action<string>? DocumentInfoChanged;

    private User? _currentUser; // YENİ: Mevcut kullanıcıyı tutar


    private Document? _currentDocument;
    private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
 
    
    public SpreadsheetView(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;

        
        StatusLabel.Content = $"Status: {(string.IsNullOrWhiteSpace(_currentDocument?.Status) ? "New" : _currentDocument?.Status)}"; 
        
        //VersionLabel.Content = $"Version: {_currentDocument?.Version}";
     }

    public void CreateNewFmeaDocument()
    {
        if (_currentUser == null) return;
        _currentDocument = new Document { AuthorId = _currentUser.Id };
        spreadsheetControl.CreateNewDocument();
        FmeaTemplateGenerator.Apply(spreadsheetControl.Document);
        spreadsheetControl.Modified = false;
        UpdateUiForDocumentStatus();


    }

    public async Task LoadDocument(int documentId)
    {
        _currentDocument = await _dbContext.Documents.FindAsync(documentId);
        if (_currentDocument?.Content != null)
        {
            // FIX: The LoadDocument method must be called on the UI thread.
            // The Task.Run was causing the cross-thread exception.
            spreadsheetControl.LoadDocument(_currentDocument.Content, DocumentFormat.Xlsx);
            spreadsheetControl.Modified = false;
            UpdateUiForDocumentStatus();

        }
    }
   
    
    
    
    
    
    
    
    
    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        // This logic should also check for unsaved changes, will add later.
        var openWindow = new OpenDocumentWindow(_dbContext);

        // FIX: The ShowDialog() method on a WPF window returns a nullable boolean (bool?).
        // Comparing it directly to 'true' is the correct way to check if the user clicked "Open".
        if (openWindow.ShowDialog() == true)
        {
            int docId = openWindow.SelectedDocumentId;
            var documentToOpen = await _dbContext.Documents.FindAsync(docId);

            if (documentToOpen != null && documentToOpen.Content != null)
            {
                _currentDocument = documentToOpen;
                spreadsheetControl.LoadDocument(documentToOpen.Content, DocumentFormat.Xlsx);
                MessageBox.Show($"Document '{documentToOpen.DocumentName}' (Version: {documentToOpen.Version}) has been loaded.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                spreadsheetControl.Modified = false;
                UpdateUiForDocumentStatus();
            }
        }
    }
    
    
        
     
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        
        await PerformSaveAsync();
    }
    private async void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentDocument == null)
        {
            MessageBox.Show("Please save the document first before renaming.", "Cannot Rename", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveWindow = new SaveDocumentWindow(_currentDocument.DocumentName) { Owner = Window.GetWindow(this) };
        if (saveWindow.ShowDialog() == true)
        {
            _currentDocument.DocumentName = saveWindow.DocumentName;
            await _dbContext.SaveChangesAsync();
            MessageBox.Show($"Document successfully renamed to '{_currentDocument.DocumentName}'.", "Rename Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        UpdateUiForDocumentStatus();
    }
    private async Task SaveCurrentDocument(bool asNewMinorVersion)
    {
        if (!asNewMinorVersion && _currentDocument != null && !spreadsheetControl.Modified)
        {
            MessageBox.Show("No changes to save.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var worksheet = spreadsheetControl.Document.Worksheets[0];
        string defaultName = worksheet.Cells["G4"].Value.ToString() ?? "New Document";

        var saveWindow = new SaveDocumentWindow(defaultName);

        if (saveWindow.ShowDialog() == true)
        {
            Document documentToSave;

            if (asNewMinorVersion || _currentDocument == null)
            {
                documentToSave = new Document();
                _dbContext.Documents.Add(documentToSave);
                documentToSave.Version = _currentDocument != null ? IncrementMinorVersion(_currentDocument.Version) : "1.0.0";
            }
            else
            {
                documentToSave = _currentDocument;
                documentToSave.Version = IncrementPatchVersion(documentToSave.Version);
            }

            documentToSave.DocumentName = saveWindow.DocumentName;
            UpdateDocumentFromSpreadsheet(documentToSave);

            await _dbContext.SaveChangesAsync();
            _currentDocument = documentToSave;
            spreadsheetControl.Modified = false;
            MessageBox.Show($"Document '{documentToSave.DocumentName}' saved successfully as version {documentToSave.Version}!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        UpdateUiForDocumentStatus();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var activeSheet = spreadsheetControl.Document.Worksheets.ActiveWorksheet;
        var usedRange = activeSheet.GetUsedRange();

 
        bool isSheetEmpty = usedRange.RowCount == 1 &&
                            usedRange.ColumnCount == 1 &&
                            usedRange[0, 0].Value.IsEmpty;

        if (isSheetEmpty)
        {
            MessageBox.Show("There is no document to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            spreadsheetControl.SaveDocument(saveFileDialog.FileName, DocumentFormat.Xlsx);
            MessageBox.Show($"Document successfully exported to:\n{saveFileDialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BackToHome_Click(object sender, RoutedEventArgs e)
    {
        RequestGoToHome?.Invoke(this, EventArgs.Empty);
    }
    
    private void UpdateDocumentFromSpreadsheet(Document doc)
        {
            var worksheet = spreadsheetControl.Document.Worksheets[0];
            
            doc.ProductPart = worksheet.Cells["B4"].Value.ToString();
            doc.FmeaId = worksheet.Cells["G4"].Value.ToString();
            doc.ProjectName = worksheet.Cells["B6"].Value.ToString();
            doc.ResponsibleParty = worksheet.Cells["G6"].Value.ToString();
            doc.ApprovedBy = worksheet.Cells["I6"].Value.ToString();
            doc.Team = worksheet.Cells["B8"].Value.ToString();
         
            //doc.DocumentName = string.IsNullOrEmpty(doc.FmeaId) ? "Unnamed Document" : doc.FmeaId;
            doc.Content = spreadsheetControl.SaveDocument(DocumentFormat.Xlsx);
            doc.ModifiedDate = DateTime.Now;
            doc.Version = _currentDocument?.Version ?? "0.0.1"; // Ensure version is set
            worksheet.Cells["I4"].Value = doc.Version;
        }

        private string IncrementPatchVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "0.0.1";
            try
            {
                var parts = version.Split('.').Select(int.Parse).ToList();
                if (parts.Count == 3)
                {
                    parts[2]++; // Increment Patch
                    return string.Join(".", parts);
                }
            }
            catch { /* Fallback for invalid format */ }
            return "0.0.1";
        }

        private string IncrementMinorVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "0.1.0";
            try
            {
                var parts = version.Split('.').Select(int.Parse).ToList();
                if (parts.Count == 3)
                {
                    parts[1]++; // Increment Minor
                    parts[2] = 0; // Reset Patch
                    return string.Join(".", parts);
                }
            }
            catch { /* Fallback for invalid format */ }
            return "0.1.0";
        }
        
        
        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            /*if (_currentDocument == null) return;
            _currentDocument.Status = "Under Review";
            await SaveAndRefreshUi();*/
            
            if (await PerformSaveAsync())
            {
                if (_currentDocument == null) return;
                
                var approver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Approver");
                if (approver == null)
                {
                    MessageBox.Show("No approver found in the system.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _currentDocument.ApproverId = approver.Id;
                _currentDocument.Status = "Under Review";
                await SaveAndRefreshUi();
            }
        }
        
        
private async Task<bool> PerformSaveAsync()
        {
            // FIX: A new document is identified by having an Id of 0.
            if (_currentDocument == null || _currentDocument.Id == 0)
            {
                var worksheet = spreadsheetControl.Document.Worksheets[0];
                string defaultName = worksheet.Cells["G4"].Value.ToString() ?? "New Document";
                var saveWindow = new SaveDocumentWindow(defaultName) { Owner = Window.GetWindow(this) };
                
                if (saveWindow.ShowDialog() == true)
                {
                    if (_currentDocument == null) // Should not happen, but as a safeguard
                    {
                         if (_currentUser != null) _currentDocument = new Document { AuthorId = _currentUser.Id };
                    }
                    _currentDocument.DocumentName = saveWindow.DocumentName;
                    _currentDocument.Version = "0.0.1";
                    _dbContext.Documents.Add(_currentDocument);
                }
                else
                {
                    return false; // User cancelled the save dialog
                }
            }
            else if (!spreadsheetControl.Modified)
            {
                MessageBox.Show("No changes to save.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return true; // No changes, but operation is considered successful
            }
            else
            {
                _currentDocument.Version = IncrementPatchVersion(_currentDocument.Version);
            }

            UpdateDocumentFromSpreadsheet(_currentDocument);
            await _dbContext.SaveChangesAsync();
            spreadsheetControl.Modified = false;
            MessageBox.Show($"Document '{_currentDocument.DocumentName}' saved successfully as version {_currentDocument.Version}!", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateUiForDocumentStatus();
            return true;
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null || _currentUser == null) return;
            if (_currentDocument.AuthorId == _currentUser.Id)
            {
                MessageBox.Show("Authors cannot approve their own documents.", "Authorization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _currentDocument.Status = "Approved";
            _currentDocument.Version = IncrementMajorVersion(_currentDocument.Version); // YENİ: Ana versiyonu artır

            await SaveAndRefreshUi();
        }
        
        private string IncrementMajorVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "1.0.0";
            try
            {
                var parts = version.Split('.').Select(int.Parse).ToList();
                if (parts.Count == 3)
                {
                    parts[0]++; // Ana versiyonu artır
                    parts[1] = 0; // Alt versiyonu sıfırla
                    parts[2] = 0; // Yama versiyonunu sıfırla
                    return string.Join(".", parts);
                }
            }
            catch { /* Hatalı format için geri dönüş */ }
            return "1.0.0"; // Güvenli bir varsayılana geri dön
        }

        private async void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null) return;
            _currentDocument.Status = "Draft";
            await SaveAndRefreshUi();
        }
        
        
        private async Task SaveAndRefreshUi()
        {
            await _dbContext.SaveChangesAsync();
            UpdateUiForDocumentStatus();
            MessageBox.Show($"Document status changed to: {_currentDocument?.Status}");
        }

        // --- YENİ: Arayüz Güncelleme Mantığı ---
        private void UpdateUiForDocumentStatus()
        {
            if (_currentDocument == null || _currentUser == null) return;


            string status = _currentDocument.Status;
            DocumentInfoChanged?.Invoke($"Status: {status} | Version: {_currentDocument.Version}");

            StatusLabel.Content = $"Status: {(string.IsNullOrWhiteSpace(status) ? "New" : status)}";
            VersionLabel.Content = $"Version: {_currentDocument.Version}";

            bool isAuthor = _currentDocument.AuthorId == _currentUser.Id;
            bool isApprover = _currentUser.Role == "Approver";

            bool isEditable = (status == "Draft");
            spreadsheetControl.ReadOnly = !isEditable;

            // Control button visibility based on status
            SubmitButton.IsVisible = (status == "Draft" && isAuthor);
            ApproveButton.IsVisible = (status == "Under Review" && isApprover);
            RejectButton.IsVisible = (status == "Under Review" && isApprover);
        }
}