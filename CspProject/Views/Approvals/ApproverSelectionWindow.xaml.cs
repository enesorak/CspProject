using System.Windows;
using CspProject.Data;
using CspProject.Data.Entities;

namespace CspProject.Views.Approvals;

public partial class ApproverSelectionWindow : Window
{
    private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
    public int? SelectedApproverId { get; private set; }

    public ApproverSelectionWindow(int currentUserId)
    {
        InitializeComponent();
        LoadUsers(currentUserId);
    }

    private void LoadUsers(int currentUserId)
    {
        // Kullanıcının kendisi hariç diğer tüm kullanıcıları listele
        UsersListBox.ItemsSource = _dbContext.Users.Where(u => u.Id != currentUserId).ToList();
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (UsersListBox.SelectedItem is User selectedUser)
        {
            SelectedApproverId = selectedUser.Id;
            this.DialogResult = true;
            this.Close();
        }
        else
        {
            MessageBox.Show("Please select a user from the list.", "No Selection");
        }
    }

    private async void AddAndSelectButton_Click(object sender, RoutedEventArgs e)
    {
        var newName = NewNameTextBox.Text;
        var newEmail = NewEmailTextBox.Text;

        if (string.IsNullOrWhiteSpace(newName) || string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains("@"))
        {
            MessageBox.Show("Please enter a valid name and email address for the new approver.", "Validation Error");
            return;
        }

        var newUser = new User { Name = newName, Email = newEmail };
        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync();

        SelectedApproverId = newUser.Id;
        this.DialogResult = true;
        this.Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }
}