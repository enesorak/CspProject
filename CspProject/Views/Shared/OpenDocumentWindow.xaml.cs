// ***********************************************************************************
// File: CspProject/OpenDocumentWindow.xaml.cs
// Description: Code-behind for the document selection window.
// Author: Enes Orak
// ***********************************************************************************

using System.Windows;
using CspProject.Data;
using DevExpress.Xpf.Core;
using Microsoft.EntityFrameworkCore;

namespace CspProject.Views.Shared;

public partial class OpenDocumentWindow : ThemedWindow
{
    private readonly ApplicationDbContext _dbContext;
    public int SelectedDocumentId { get; private set; } = -1;

    public OpenDocumentWindow(ApplicationDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        LoadDocuments();
    }

    private async void LoadDocuments()
    {
        var documents = await _dbContext.Documents
            .OrderByDescending(d => d.ModifiedDate)
            .Select(d => new { d.Id, d.DocumentName,d.Status, d.Version, d.ModifiedDate })
            .ToListAsync();
            
        DocumentsListView.ItemsSource = documents;
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentsListView.SelectedItem != null)
        {
            // Use dynamic to access properties of the anonymous type
            dynamic selectedDoc = DocumentsListView.SelectedItem;
            SelectedDocumentId = selectedDoc.Id;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please select a document to open.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}