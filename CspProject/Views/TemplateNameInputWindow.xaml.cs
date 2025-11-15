using System.Windows;

namespace CspProject.Views
{
    public partial class TemplateNameInputWindow : Window
    {
        public string TemplateName => TemplateNameTextBox.Text;

        public TemplateNameInputWindow()
        {
            InitializeComponent();
            TemplateNameTextBox.Focus();
            TemplateNameTextBox.SelectAll();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TemplateNameTextBox.Text))
            {
                MessageBox.Show("Please enter a template name.", 
                    "Validation Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }
    }
}