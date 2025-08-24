using System.Windows;
using CspProject.Data;
using CspProject.Data.Entities;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace CspProject.Views;

public partial class SettingsView : UserControl
{
    public event EventHandler<User>? UserSwitched;
    private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
    
    public SettingsView(User currentUser)
    {
        InitializeComponent();
        LoadUsers(currentUser);
    }
    
    private void LoadUsers(User currentUser)
    {
        UserComboBox.ItemsSource = _dbContext.Users.ToList();
        UserComboBox.SelectedItem = currentUser;
    }

    private void SwitchUser_Click(object sender, RoutedEventArgs e)
    {
        if (UserComboBox.SelectedItem is User selectedUser)
        {
            UserSwitched?.Invoke(this, selectedUser);
            MessageBox.Show($"Switched to user: {selectedUser.Name}", "User Switched", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}