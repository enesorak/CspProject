using System;
using System.Collections.Generic; // List için eklendi
using System.IO; // Path ve Directory için eklendi
using System.Linq; // LINQ için eklendi
using System.Windows.Controls;
using CspProject.Models; // Yeni TemplateInfo sınıfı için eklendi
using Sentry; // Sentry için eklendi

namespace CspProject.Views
{
    public partial class TemplatesView : UserControl
    {
        // İki farklı sinyalimiz olacak:
        // 1. Dahili FMEA şablonu için (eskisi gibi)
        public event EventHandler? RequestNewFmeaDocument;
        // 2. Dosyadan yüklenen şablonlar için (yeni)
        public event EventHandler<string>? RequestNewDocumentFromFile;
        
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
                // 4. Akıllı sinyal gönderme:
                // Seçilen şablon "Dahili" ise eski sinyali gönder.
                if (selectedTemplate.TemplateType == "BuiltIn")
                {
                    RequestNewFmeaDocument?.Invoke(this, EventArgs.Empty);
                }
                // Seçilen şablon "Dosya" ise, yeni sinyali dosya yoluyla gönder.
                else if (selectedTemplate.TemplateType == "File")
                {
                    RequestNewDocumentFromFile?.Invoke(this, selectedTemplate.Identifier);
                }
            }
        }
    }
}