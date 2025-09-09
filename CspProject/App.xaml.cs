// ***********************************************************************************
// File: CspProject/App.xaml.cs
// Description: The code-behind for the application entry point. This is the
//              perfect place to set the global application culture.
// Author: Enes Orak
// ***********************************************************************************


using System.Globalization;
using System.Windows;
using CspProject.Data;
using Application = System.Windows.Application;

namespace CspProject;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    
    public static string AppVersion { get; } = "3.1.3";

    public App()
    {
        // FIX: Set the culture to be invariant at the very start of the application.
        // This ensures all components, including DevExpress, use the same universal
        // settings for things like formula separators (,) and decimal points (.).
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            
            

    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // FIX: Set the culture to be invariant at the very start of the application.
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        // DÜZELTME: Veritabanı oluşturma/güncelleme işlemi uygulama başlarken bir kez yapılır.
        using (var dbContext = new ApplicationDbContext())
        {
            dbContext.Database.EnsureCreated();
        }
    }
        
        
}