using UserControl = System.Windows.Controls.UserControl;

namespace CspProject.Views;

public partial class TemplatesView : UserControl
{
    
    public event EventHandler? RequestNewFmeaDocument;

    public TemplatesView()
    {
        InitializeComponent();
        LoadTemplates();

    }
    
    
    private void LoadTemplates()
    {
        var templates = new List<object>
        {
            new { Name = "DFMEA Template", Description = "A standard template for Design Failure Mode and Effects Analysis." }
        };
        TemplatesGrid.ItemsSource = templates;
    }

    private void TemplatesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var grid = (DevExpress.Xpf.Grid.GridControl)sender;
        if (grid.SelectedItem != null)
        {
            // For now, we only have one template, so we fire the specific event.
            RequestNewFmeaDocument?.Invoke(this, EventArgs.Empty);
        }
    }
}