using UserControl = System.Windows.Controls.UserControl;

namespace CspProject.Views;

public partial class LoadingSplashScreen : UserControl
{
    public LoadingSplashScreen()
    {
        InitializeComponent();
    }
    
    // This method is called by the DXSplashScreen to update the message.
    public void Progress(object state)
    {
        if (state is string message)
        {
            LoadingMessageTextBlock.Text = message;
        }
    }

    public void CloseSplashScreen()
    {
        // This is handled automatically by the DXSplashScreen service.
    }
}