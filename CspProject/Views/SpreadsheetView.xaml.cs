using System.IO;
using System.Windows;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services;
using DevExpress.Spreadsheet;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace CspProject.Views;

public partial class SpreadsheetView : UserControl
{
    public event EventHandler? RequestGoToHome;

    private Document? _currentDocument;
    private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();

    public SpreadsheetView()
    {
        InitializeComponent();
    }

    public void CreateNewFmeaDocument()
    {
        _currentDocument = null;
        spreadsheetControl.CreateNewDocument();
        FmeaTemplateGenerator.Apply(spreadsheetControl.Document);
        spreadsheetControl.Modified = false;
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
            }
        }
    }
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentDocument == null) // New document
        {
            var worksheet = spreadsheetControl.Document.Worksheets[0];
            string defaultName = worksheet.Cells["G4"].Value.ToString() ?? "New Document";

            var saveWindow = new SaveDocumentWindow(defaultName) { Owner = Window.GetWindow(this) };
            if (saveWindow.ShowDialog() == true)
            {
                _currentDocument = new Document { Version = "0.0.1" };
                _currentDocument.DocumentName = saveWindow.DocumentName;
                _dbContext.Documents.Add(_currentDocument);
                UpdateDocumentFromSpreadsheet(_currentDocument);
                await _dbContext.SaveChangesAsync();
                spreadsheetControl.Modified = false;
                MessageBox.Show($"Document '{_currentDocument.DocumentName}' saved successfully as version {_currentDocument.Version}!", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        else // Existing document
        {
            if (!spreadsheetControl.Modified)
            {
                MessageBox.Show("No changes to save.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _currentDocument.Version = IncrementPatchVersion(_currentDocument.Version);
            UpdateDocumentFromSpreadsheet(_currentDocument);
            await _dbContext.SaveChangesAsync();
            spreadsheetControl.Modified = false;
            MessageBox.Show($"Document '{_currentDocument.DocumentName}' saved successfully as version {_currentDocument.Version}!", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
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
}