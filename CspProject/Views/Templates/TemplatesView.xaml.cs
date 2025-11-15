using System.IO;
using System.Windows;
using CspProject.Models;
// List için eklendi
// Path ve Directory için eklendi
// LINQ için eklendi
// Yeni TemplateInfo sınıfı için eklendi

// Sentry için eklendi

namespace CspProject.Views.Templates
{
    public partial class TemplatesView : UserControl
    {
        // İki farklı sinyalimiz olacak:
        // 1. Dahili FMEA şablonu için (eskisi gibi)
        public event EventHandler? RequestNewFmeaDocument;
        // 2. Dosyadan yüklenen şablonlar için (yeni)
        public event EventHandler<string>? RequestNewDocumentFromFile;
        
        public event EventHandler? RequestBlankSpreadsheet; // YENİ

        
        private readonly string _templateDirectory;

        public TemplatesView()
        {
            InitializeComponent();
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            _templateDirectory = Path.Combine(exePath, "Templates");
            LoadTemplates();
        }
        
        private void LoadTemplates()
        {
            var templateList = new List<TemplateInfo>();

            // 1. Dahili (hard-coded) FMEA şablonunu listeye ekle
            templateList.Add(new TemplateInfo 
            { 
                Name = "DFMEA Template (Built-in)", 
                Description = "A standard template for Design Failure Mode and Effects Analysis.",
                TemplateType = "BuiltIn",
                Identifier = "BUILTIN_FMEA"
            });

            // 2. Templates klasörünün var olduğundan emin ol
            if (!Directory.Exists(_templateDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_templateDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not create templates directory: {ex.Message}", "Error");
                    SentrySdk.CaptureException(ex);
                }
            }

            // 3. Klasördeki tüm .xlsx dosyalarını bul ve listeye ekle
            try
            {
                var templateFiles = Directory.GetFiles(_templateDirectory, "*.xlsx")
                    .Select(filePath => new TemplateInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        Description = $"Loads the custom template '{Path.GetFileName(filePath)}'.",
                        TemplateType = "File",
                        Identifier = filePath // Dosya yolunu Identifier olarak sakla
                    })
                    .ToList();
                
                templateList.AddRange(templateFiles);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not read templates directory: {ex.Message}", "Error");
                SentrySdk.CaptureException(ex);
            }

            TemplatesGrid.ItemsSource = templateList;
        }

        private void TemplatesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = (DevExpress.Xpf.Grid.GridControl)sender;
    
            if (grid.SelectedItem is TemplateInfo selectedTemplate)
            {
                // DEBUG LOG
                SentrySdk.AddBreadcrumb($"Template clicked: {selectedTemplate.Name}");
                SentrySdk.AddBreadcrumb($"Template type: {selectedTemplate.TemplateType}");
                SentrySdk.AddBreadcrumb($"Template identifier: {selectedTemplate.Identifier}");
        
                if (selectedTemplate.TemplateType == "BuiltIn")
                {
                    MessageBox.Show("Built-in event tetiklendi"); // TEST
                    RequestNewFmeaDocument?.Invoke(this, EventArgs.Empty);
                }
                else if (selectedTemplate.TemplateType == "File")
                {
                    MessageBox.Show($"File event tetiklendi: {selectedTemplate.Identifier}"); // TEST
                    RequestNewDocumentFromFile?.Invoke(this, selectedTemplate.Identifier);
                }
            }
            else
            {
                MessageBox.Show("SelectedItem null!"); // TEST
            }
        }
        
        private void BlankSheetButton_Click(object sender, RoutedEventArgs e)
        {
            RequestBlankSpreadsheet?.Invoke(this, EventArgs.Empty);
        }
        
        private async void ImportTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Excel Template",
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string sourceFile = openFileDialog.FileName;
                    string fileName = Path.GetFileName(sourceFile);
                    string destFile = Path.Combine(_templateDirectory, fileName);

                    // Aynı isimde dosya var mı kontrol et
                    if (File.Exists(destFile))
                    {
                        var result = MessageBox.Show(
                            $"A template named '{fileName}' already exists. Do you want to replace it?",
                            "Confirm Replace",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                            return;
                    }

                    // Dosyayı kopyala
                    await Task.Run(() => File.Copy(sourceFile, destFile, overwrite: true));

                    MessageBox.Show(
                        $"Template '{fileName}' imported successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Listeyi yenile
                    LoadTemplates();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to import template.\n\nError: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    SentrySdk.CaptureException(ex);
                }
            }
        }
    }
}