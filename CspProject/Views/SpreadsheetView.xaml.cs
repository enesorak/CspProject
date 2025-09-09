using System.IO;
using System.Windows;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services;
using DevExpress.Spreadsheet;
using DevExpress.Xpf.Core;
using Microsoft.EntityFrameworkCore;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;


namespace CspProject.Views;

public partial class SpreadsheetView : UserControl
{
    public event EventHandler? RequestGoToHome;
    public event Action<string>? DocumentInfoChanged;

    private readonly User? _currentUser;
    private Document? _currentDocument;
    private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();


    public SpreadsheetView(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;


        StatusLabel.Content =
            $"Status: {(string.IsNullOrWhiteSpace(_currentDocument?.Status) ? "New" : _currentDocument?.Status)}";

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
                MessageBox.Show(
                    $"Document '{documentToOpen.DocumentName}' (Version: {documentToOpen.Version}) has been loaded.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                spreadsheetControl.Modified = false;
                UpdateUiForDocumentStatus();
            }
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
            MessageBox.Show("Please save the document first before renaming.", "Cannot Rename", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var saveWindow = new SaveDocumentWindow(_currentDocument.DocumentName) { Owner = Window.GetWindow(this) };
        if (saveWindow.ShowDialog() == true)
        {
            _currentDocument.DocumentName = saveWindow.DocumentName;
            await _dbContext.SaveChangesAsync();
            MessageBox.Show($"Document successfully renamed to '{_currentDocument.DocumentName}'.", "Rename Successful",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
            // DEĞİŞİKLİK: Arayüz çakışmasını önlemek için BeginUpdate/EndUpdate eklendi.
            try
            {
                spreadsheetControl.BeginUpdate();
                spreadsheetControl.SaveDocument(saveFileDialog.FileName, DocumentFormat.Xlsx);
            }
            finally
            {
                spreadsheetControl.EndUpdate();
            }

            MessageBox.Show($"Document successfully exported to:\n{saveFileDialog.FileName}", "Export Successful",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    private void BackToHome_Click(object sender, RoutedEventArgs e)
    {
        RequestGoToHome?.Invoke(this, EventArgs.Empty);
    }
    private async Task UpdateDocumentFromSpreadsheet_c(Document doc)
    {
        // DEĞİŞİKLİK: NullReferenceException hatasını çözmek için tüm metod BeginUpdate/EndUpdate bloğuna alındı.
        try
        {
            spreadsheetControl.BeginUpdate();

            var worksheet = spreadsheetControl.Document.Worksheets[0];

            // 1. ADIM: Spreadsheet'ten gerekli tüm bilgileri oku.
            doc.ProductPart = worksheet.Cells["B4"].Value.ToString();
            doc.FmeaId = worksheet.Cells["G4"].Value.ToString();
            doc.ProjectName = worksheet.Cells["B6"].Value.ToString();
            doc.ResponsibleParty = worksheet.Cells["G6"].Value.ToString();
            doc.ApprovedBy = worksheet.Cells["I6"].Value.ToString();
            doc.Team = worksheet.Cells["B8"].Value.ToString();

            doc.ModifiedDate = DateTime.Now;
            doc.Version = _currentDocument?.Version ?? "0.0.1";

            // 2. ADIM: Gerekli tüm güncellemeleri spreadsheet'e yaz.
            // DEĞİŞİKLİK: Versiyon numarasını kaydetmeden ÖNCE hücreye yazıyoruz.
            // Bu, hatanın ana nedenini ortadan kaldırır.
            worksheet.Cells["I4"].Value = doc.Version;

            await Task.Delay(1);

            // 3. ADIM: Tüm değişiklikler yapıldıktan sonra, dokümanın son halini kaydet.
            using var memoryStream = new MemoryStream();
            spreadsheetControl.SaveDocument(memoryStream, DocumentFormat.Xlsx);
            doc.Content = memoryStream.ToArray();
        }
        finally
        {
            // Hata olsa bile arayüz güncellemelerini tekrar açarak kontrolün kilitlenmesini engelliyoruz.
            spreadsheetControl.EndUpdate();
        }
    }

    // Metodun imzasından "async Task" kaldırıldı, artık "void".
    private void UpdateDocumentFromSpreadsheet(Document doc)
    {
        spreadsheetControl.BeginUpdate();
        try
        {
            // Asıl kaydetme mantığı
            var worksheet = spreadsheetControl.Document.Worksheets[0];

            doc.ProductPart = worksheet.Cells["B4"].Value.ToString();
            doc.FmeaId = worksheet.Cells["G4"].Value.ToString();
            doc.ProjectName = worksheet.Cells["B6"].Value.ToString();
            doc.ResponsibleParty = worksheet.Cells["G6"].Value.ToString();
            doc.ApprovedBy = worksheet.Cells["I6"].Value.ToString();
            doc.Team = worksheet.Cells["B8"].Value.ToString();
          
            doc.ModifiedDate = DateTime.Now;
            doc.Version = _currentDocument?.Version ?? "0.0.1";

            // Versiyonu hücreye yazıyoruz
            worksheet.Cells["I4"].Value = doc.Version;

            // Son halini belleğe kaydedip Document nesnesine atıyoruz.
            using (var finalContentStream = new MemoryStream())
            {
                spreadsheetControl.SaveDocument(finalContentStream, DocumentFormat.Xlsx);
                doc.Content = finalContentStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            // Beklenmedik bir hata olursa kullanıcıyı bilgilendir.
            MessageBox.Show($"An error occurred while preparing the document for saving: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            spreadsheetControl.EndUpdate();
        }
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
        catch
        {
            /* Fallback for invalid format */
        }

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
        catch
        {
            /* Fallback for invalid format */
        }

        return "0.1.0";
    }


    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        if (await PerformSaveAsync(false))
        {
            if (_currentDocument == null) return;

            // Rastgele approver seçiyoruz bunu workflow a göre düzenlicez.
            var approver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Approver");
            if (approver == null)
            {
                MessageBox.Show("No approver found in the system.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            _currentDocument.ApproverId = approver.Id;
            _currentDocument.Status = "Under Review";
            _currentDocument.Version = IncrementMinorVersion(_currentDocument.Version); // YENİ: Alt versiyonu artır
            await SaveAndRefreshUi();
        }
    }


    private async Task<bool> PerformSaveAsync(bool showNoChangesWarning)
    {
        if (_currentDocument == null || _currentDocument.Id == 0)
        {
            var worksheet = spreadsheetControl.Document.Worksheets[0];
            string defaultName = worksheet.Cells["G4"].Value.ToString() ?? "New Document";
            var saveWindow = new SaveDocumentWindow(defaultName) { Owner = Window.GetWindow(this) };

            if (saveWindow.ShowDialog() == true)
            {
                if (_currentDocument == null)
                {
                    if (_currentUser != null) _currentDocument = new Document { AuthorId = _currentUser.Id };
                }

                _currentDocument.DocumentName = saveWindow.DocumentName;
                _currentDocument.Version = "0.0.1";
                _dbContext.Documents.Add(_currentDocument);
            }
            else
            {
                return false;
            }
        }
        else if (!spreadsheetControl.Modified && showNoChangesWarning)
        {
            MessageBox.Show("No changes to save.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }
        else
        {
            _currentDocument.Version = IncrementPatchVersion(_currentDocument.Version);
        }


        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateDocumentFromSpreadsheet(_currentDocument); // Artık async değil
        });


        await _dbContext.SaveChangesAsync();


        Application.Current.Dispatcher.Invoke(() =>
        {
            spreadsheetControl.Modified = false;
            
            UpdateUiForDocumentStatus();
            MessageBox.Show(
                $"Document '{_currentDocument.DocumentName}' saved successfully as version {_currentDocument.Version}!",
                "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        return true;
    }


    private async Task<bool> PerformSaveAsyncxx(bool showNoChangesWarning)
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
        else if (!spreadsheetControl.Modified && showNoChangesWarning)
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
        MessageBox.Show(
            $"Document '{_currentDocument.DocumentName}' saved successfully as version {_currentDocument.Version}!",
            "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        UpdateUiForDocumentStatus();
        return true;
    }

    private async void ApproveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentDocument == null || _currentUser == null) return;
        if (_currentDocument.AuthorId == _currentUser.Id)
        {
            MessageBox.Show("Authors cannot approve their own documents.", "Authorization Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        _currentDocument.Status = "Approved";
        _currentDocument.Version = IncrementMajorVersion(_currentDocument.Version); // YENİ: Ana versiyonu artır

        _currentDocument.ApprovedBy = _currentUser.Name;
        _currentDocument.DateCompleted = DateTime.Now;

        // YENİ: Onaylayan bilgilerini spreadsheet'e yaz
        var worksheet = spreadsheetControl.Document.Worksheets[0];
        worksheet.Cells["I6"].Value = _currentUser.Name;
        worksheet.Cells["K6"].Value = DateTime.Now;

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
        catch
        {
            /* Hatalı format için geri dönüş */
        }

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
        // 1. Değişiklikleri veritabanına kaydet.
        // Bu işlem bittiğinde _currentDocument nesnesi en güncel veriyi içerir.
        await _dbContext.SaveChangesAsync();

        // --- İSTEĞİN ÜZERİNE GÜNCELLEME ---
        // 2. Belgeyi yeniden yüklemek yerine, SADECE I4 hücresindeki versiyon bilgisini güncelle.
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
        // --- GÜNCELLEME SONA ERDİ ---

        // 3. Butonların durumunu ve diğer etiketleri güncelle.
        UpdateUiForDocumentStatus();
        MessageBox.Show($"Document status changed to: {_currentDocument?.Status}");
    }

    private async Task SaveAndRefreshUix()
    {
        await _dbContext.SaveChangesAsync();
        // --- EKLENEN ÇÖZÜM ---
        // 2. Veritabanına kaydettiğimiz dokümanın son halini (Content)
        //    spreadsheet kontrolüne yeniden yükleyerek arayüzü yenile.
        //    Bu satır, I4 hücresindeki versiyonun güncellenmesini sağlar.
        if (_currentDocument?.Content != null)
        {
            spreadsheetControl.LoadDocument(_currentDocument.Content, DocumentFormat.Xlsx);
            spreadsheetControl.Modified = false; // Yeniden yüklendiği için "değiştirildi" bayrağını sıfırla.
        }
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

        var worksheet = spreadsheetControl.Document.Worksheets[0];
        worksheet.Cells["I4"].Value = _currentDocument.Version;
        
        // Control button visibility based on status
        SubmitButton.IsEnabled = (status == "Draft" && isAuthor);
        ApproveButton.IsEnabled = (status == "Under Review" && isApprover);
        RejectButton.IsEnabled = (status == "Under Review" && isApprover);
    }
}