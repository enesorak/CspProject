using System.Windows;
using DevExpress.Xpf.Core;
 

namespace CspProject;

public partial class SaveDocumentWindow : ThemedWindow
{
    public string DocumentName { get; private set; }
    public SaveDocumentWindow(string defaultName)
    {
        InitializeComponent();
        DocumentNameTextBox.Text = defaultName;
        DocumentName = defaultName;
    }
    
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DocumentNameTextBox.Text))
        {
            MessageBox.Show("Document name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DocumentName = DocumentNameTextBox.Text;
        DialogResult = true;
    }
}