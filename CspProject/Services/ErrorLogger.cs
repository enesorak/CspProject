using System.IO;

namespace CspProject.Services;

public static class ErrorLogger
{
    public static void Log(Exception ex)
    {
        try
        {
            // Get the user's desktop path
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string logFilePath = Path.Combine(desktopPath, "CspProject_ErrorLog.txt");

            // Format the error message
            string errorMessage = $@"
--------------------------------------------------
Date: {DateTime.Now}
Version: {App.AppVersion}
--------------------------------------------------
Error: {ex.Message}
Stack Trace:
{ex.StackTrace}
--------------------------------------------------

";
            // Append the error to the log file
            File.AppendAllText(logFilePath, errorMessage);
        }
        catch
        {
            // If logging itself fails, do nothing to avoid a crash loop.
        }
    }
}