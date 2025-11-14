using System.Windows;
using CspProject.Data;
using CspProject.Data.Entities;
 

namespace CspProject.Views;

public partial class WelcomeView : UserControl
{
    public event EventHandler<User> UserCreated;

    public WelcomeView()
    {
        InitializeComponent();
        
    }

    private async void GetStarted_Click(object sender, RoutedEventArgs e)
    {
        // Basit doğrulama kontrolleri
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Please enter your name.", "Validation Error");
            return;
        }
        if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || !EmailTextBox.Text.Contains("@"))
        {
            MessageBox.Show("Please enter a valid email address.", "Validation Error");
            return;
        }

        // DEĞİŞİKLİK: Yeni kullanıcı 'Role' yerine 'Email' ile oluşturuluyor.
        var newUser = new User
        {
            Name = NameTextBox.Text,
            Email = EmailTextBox.Text 
        };

        using (var dbContext = new ApplicationDbContext())
        {
            dbContext.Users.Add(newUser);
            await dbContext.SaveChangesAsync();
        }
            
        UserCreated?.Invoke(this, newUser);
    }
}