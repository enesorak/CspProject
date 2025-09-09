// ***********************************************************************************
// File: CspProject/MainWindow.xaml.cs
// Description: Manages UI events. Professional versioning logic added.
// Author: Enes Orak
// ***********************************************************************************
using CspProject.Data;
using CspProject.Data.Entities;
using DevExpress.Xpf.Core;
using System.Windows;
using CspProject.Views;
using Microsoft.EntityFrameworkCore;
using MessageBox = System.Windows.MessageBox;

namespace CspProject;
 
public partial class MainWindow : ThemedWindow
{
    
    private const string DefaultTitle = "CSP Project";
    private User? _currentUser; // YENİ

    public MainWindow()
    {
        InitializeComponent();
    
   
    }
    
    private async void ThemedWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Veritabanını ve test kullanıcılarını oluştur
        using (var dbContext = new ApplicationDbContext())
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

         // Start as the "Author" user by default
        _currentUser = await new ApplicationDbContext().Users.FirstAsync(u => u.Role == "Author");
        this.Title = DefaultTitle;
        GoToHomeScreen();
    }

    private void GoToHomeScreen()
    {            this.Title = DefaultTitle; // Ana ekrana dönerken başlığı sıfırla

        var homeScreen = new HomeScreen(_currentUser!);
        homeScreen.RequestNewDocument += (s, e) => _ = GoToSpreadsheetScreen(null);
        homeScreen.RequestOpenDocument += async (s, docId) => await GoToSpreadsheetScreen(docId);
        homeScreen.RequestNavigate += (s, page) => Navigate(page);

        MainContentControl.Content = homeScreen;
    }

    private async Task GoToSpreadsheetScreen(int? documentId)
    {
        if (_currentUser == null) return;
        DXSplashScreen.Show<LoadingSplashScreen>();
        string message = documentId.HasValue ? "Loading document, please wait..." : "Creating new template, please wait...";
        DXSplashScreen.SetState(message);

        await Task.Delay(50);

        try
        {
            var spreadsheetView = new SpreadsheetView(_currentUser);
            spreadsheetView.RequestGoToHome += (s, e) => GoToHomeScreen();
            spreadsheetView.DocumentInfoChanged += (info) => {
                this.Title = $"{DefaultTitle} - {info}";
            };

            if (documentId.HasValue)
            {
                await spreadsheetView.LoadDocument(documentId.Value);
            }
            else
            {
                spreadsheetView.CreateNewFmeaDocument();
            }

            MainContentControl.Content = spreadsheetView;
        }
        finally
        {
            DXSplashScreen.Close();
        }
    }
    
    private void Navigate(string pageTag)
    {
        switch(pageTag)
        {
            case "Home":
                GoToHomeScreen();
                break;
            case "Settings":
                GoToSettingsScreen();
                break;
            default:
                MessageBox.Show($"The '{pageTag}' feature is under construction.", "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }
    private void GoToSettingsScreen()
    {
        if (_currentUser == null) return;
            
        var settingsView = new SettingsView(_currentUser);
        settingsView.UserSwitched += (s, newUser) => {
            _currentUser = newUser;
            GoToHomeScreen(); 
        };
        MainContentControl.Content = settingsView;
    }
    
    private void GoToApprovalsScreen()
    {
        if (_currentUser == null) return;
            
        var settingsView = new SettingsView(_currentUser);
        settingsView.UserSwitched += (s, newUser) => {
            _currentUser = newUser;
            GoToHomeScreen(); 
        };
        MainContentControl.Content = settingsView;
    }
    
 
}