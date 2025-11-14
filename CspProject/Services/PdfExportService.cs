// Services/PdfExportService.cs
using System.IO;
using DevExpress.Spreadsheet;
using DevExpress.Xpf.Spreadsheet;

// Bu using ifadesini ekleyin

namespace CspProject.Services
{
    public static class PdfExportService
    {
        public static byte[] ExportToPdfBytes(SpreadsheetControl spreadsheetControl)
        {
            // Aktif çalışma sayfasını al
            var worksheet = spreadsheetControl.Document.Worksheets.ActiveWorksheet;

            
            CellRange usedRange = worksheet.GetUsedRange();
            
            // 2. Baskı alanını, A1 hücresinden bulunan bu son hücreye kadar ayarla.
            worksheet.SetPrintRange(usedRange); 
            worksheet.ActiveView.Orientation = PageOrientation.Portrait;
            worksheet.ActiveView.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;

            // --- YENİ EKLENEN PDF AYARLARI ---

            WorksheetPrintOptions printOptions = worksheet.PrintOptions;
            printOptions.PrintGridlines = false;
// Scale the worksheet to fit within the width of two pages.
            printOptions.FitToPage = true;
            printOptions.FitToWidth = 1;
            printOptions.FitToHeight = 1;
// Print in black and white.
            
// Print a dash instead of a cell error message.
            printOptions.ErrorsPrintMode = ErrorsPrintMode.Dash;
            
           
    

            // --- AYARLARIN SONU ---

            using (var memoryStream = new MemoryStream())
            {
                // Spreadsheet'in içeriğini bu yeni ayarlarla PDF formatında MemoryStream'e aktar
                spreadsheetControl.ExportToPdf(memoryStream);
                
                return memoryStream.ToArray();
            }
        }
    }
}