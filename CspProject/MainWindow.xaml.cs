// ***********************************************************************************
// File: CspProject/MainWindow.xaml.cs
// Description: Manages UI events. Professional versioning logic added.
// Author: Enes Orak
// ***********************************************************************************
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services;
using DevExpress.Spreadsheet;
using DevExpress.Xpf.Core;
using System.IO;
using System.Windows;
using CspProject.Views;
using MessageBox = System.Windows.MessageBox;

namespace CspProject;
 
public partial class MainWindow : ThemedWindow
{
    public MainWindow()
    {
        InitializeComponent();
        GoToHomeScreen();
    }

    private void GoToHomeScreen()
    {
        var homeScreen = new HomeScreen();
        homeScreen.RequestNewDocument += (s, e) => _ = GoToSpreadsheetScreen(null);
        homeScreen.RequestOpenDocument += async (s, docId) => await GoToSpreadsheetScreen(docId);
        MainContentControl.Content = homeScreen;
    }

    private async Task GoToSpreadsheetScreen(int? documentId)
    {
        DXSplashScreen.Show<LoadingSplashScreen>();
        string message = documentId.HasValue ? "Loading document, please wait..." : "Creating new template, please wait...";
        DXSplashScreen.SetState(message);

        await Task.Delay(50);

        try
        {
            var spreadsheetView = new SpreadsheetView();
            spreadsheetView.RequestGoToHome += (s, e) => GoToHomeScreen();

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
}