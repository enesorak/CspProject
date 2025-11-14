using System.Windows;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services;
using DevExpress.Mvvm;
using Microsoft.EntityFrameworkCore;


namespace CspProject.Views
{
    public partial class MyDocumentsView : UserControl
    {
        public event EventHandler<int>? RequestOpenDocument;
        private readonly ApplicationDbContext _dbContext ;

        public MyDocumentsView(ApplicationDbContext dbContext)
        {
            InitializeComponent();
            _dbContext = dbContext;
            LoadAllDocuments();
            
            // --- YENİ BÖLÜM: EKRAN YÜKLENDİĞİNDE ÇALIŞACAK KOD ---
            // Bu, ekran her görünür olduğunda etiketin güncellenmesini sağlar.
            this.Loaded += MyDocumentsView_Loaded;
        }
        private void MyDocumentsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Merkezi servisten en son kontrol zamanını oku ve etiketi güncelle.
            if (AppStateService.LastCheckTime.HasValue)
            {
                LastCheckedLabel.Text = $"Last checked at: {AppStateService.LastCheckTime.Value:g}";
            }
            else
            {
                LastCheckedLabel.Text = "Last checked at: Never";
            }
        }
        // Metod artık arama metnini parametre olarak alıyor
        private async void LoadAllDocuments(string searchText = "")
        {
            using (var dbContext = new ApplicationDbContext())
            {
                var query = dbContext.Documents.AsQueryable();

                // Arama metni varsa, doküman adına göre filtrele
                if (!string.IsNullOrEmpty(searchText))
                {
                    query = query.Where(d => d.DocumentName.ToLower().Contains(searchText.ToLower()));
                }

                var documents = await query
                    .OrderByDescending(d => d.ModifiedDate)
                    .ToListAsync();
                AllDocumentsGrid.ItemsSource = documents;
            }
        }

        private void AllDocumentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = (DevExpress.Xpf.Grid.GridControl)sender;
            if (grid.SelectedItem is Document selectedDoc)
            {
                RequestOpenDocument?.Invoke(this, selectedDoc.Id);
            }
        }

        // --- YENİ EKLENEN METODLAR ---

        // Arama çubuğundaki metin her değiştiğinde listeyi yeniden yükler
        private void SearchBox_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            LoadAllDocuments(SearchBox.Text);
        }

        // "Check for Approvals" butonuna tıklandığında çalışır
        private async void CheckNowButton_Click(object sender, RoutedEventArgs e)
        {
            CheckNowButton.IsEnabled = false;
            LastCheckedLabel.Text = "Checking...";
            await CheckForApprovals();
           
        }

        // E-postaları kontrol eden ve listeyi yenileyen ana mantık
        // Views/MyDocumentsView.xaml.cs

// ... (dosyanın geri kalanı aynı)

        private async Task CheckForApprovals()
        {
            var receiverService = new EmailReceiverService(_dbContext);
            string resultMessage = string.Empty;
            try
            {
                resultMessage = await receiverService.CheckForApprovalEmailsAsync();
                LoadAllDocuments(SearchBox.Text);
            }
            catch (Exception ex)
            {
                resultMessage = $"Error: {ex.GetType().Name}";
            }
            finally
            {
                
                var now = DateTime.Now;
                AppStateService.LastCheckTime = now;
                CheckNowButton.IsEnabled = true;
                // GÜNCELLENMİŞ LABEL METNİ
                LastCheckedLabel.Text = $"Last checked at: {now:g}";
                if (resultMessage.Contains("processed"))
                {
                    var notificationService = ServiceContainer.Default.GetService<INotificationService>();
            
                    if (notificationService != null)
                    {
                        var notification = notificationService.CreatePredefinedNotification("Approvals Processed", resultMessage, "");
                        await notification.ShowAsync();
                    }

                }
            }
        }
    }
}