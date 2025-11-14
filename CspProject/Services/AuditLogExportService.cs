using System.IO;
using CspProject.Data.Entities;
using DevExpress.Spreadsheet;
using DevExpress.XtraPrinting;
using DevExpress.XtraSpreadsheet;
using ClosedXML.Excel;
using DevExpress.Xpf.Spreadsheet;

namespace CspProject.Services
{
    public static class AuditLogExportService
    {
        /// <summary>
        /// Exports audit logs to Excel format with landscape orientation and auto-fit
        /// </summary>
        public static void ExportToExcel(IEnumerable<AuditLog> logs, string filePath, string documentName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Audit Log");

            // Set page orientation to Landscape
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 0); // Fit to 1 page wide, unlimited pages tall
            worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;

            // Header styling
            worksheet.Cell(1, 1).Value = $"Change History - {documentName}";
            worksheet.Range(1, 1, 1, 6).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            worksheet.Range(2, 1, 2, 6).Merge();
            worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, 1).Style.Font.FontSize = 10;

            // Column headers
            int headerRow = 4;
            worksheet.Cell(headerRow, 1).Value = "Timestamp";
            worksheet.Cell(headerRow, 2).Value = "User";
            worksheet.Cell(headerRow, 3).Value = "Field Changed";
            worksheet.Cell(headerRow, 4).Value = "Old Value";
            worksheet.Cell(headerRow, 5).Value = "New Value";
            worksheet.Cell(headerRow, 6).Value = "Revision";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Data rows
            int currentRow = headerRow + 1;
            foreach (var log in logs)
            {
                worksheet.Cell(currentRow, 1).Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(currentRow, 2).Value = log.User?.Name ?? "Unknown";
                worksheet.Cell(currentRow, 3).Value = log.FieldChanged;
                worksheet.Cell(currentRow, 4).Value = log.OldValue ?? "";
                worksheet.Cell(currentRow, 5).Value = log.NewValue ?? "";
                worksheet.Cell(currentRow, 6).Value = log.Revision;

                // Apply borders
                var dataRange = worksheet.Range(currentRow, 1, currentRow, 6);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                currentRow++;
            }

            // Set column widths - proportional for landscape
            worksheet.Column(1).Width = 18;  // Timestamp
            worksheet.Column(2).Width = 15;  // User
            worksheet.Column(3).Width = 20;  // Field Changed
            worksheet.Column(4).Width = 35;  // Old Value
            worksheet.Column(5).Width = 35;  // New Value
            worksheet.Column(6).Width = 10;  // Revision

            // Enable text wrapping for all data columns
            for (int col = 1; col <= 6; col++)
            {
                worksheet.Column(col).Style.Alignment.WrapText = true;
            }

            // Auto-adjust row heights based on content
            worksheet.Rows().AdjustToContents();

            workbook.SaveAs(filePath);
        }

        /// <summary>
        /// Exports audit logs to PDF format using DevExpress SpreadsheetControl with landscape orientation
        /// </summary>
        public static void ExportToPdf(IEnumerable<AuditLog> logs, string filePath, string documentName)
        {
            var spreadsheet = new SpreadsheetControl();
            spreadsheet.CreateNewDocument();

            var worksheet = spreadsheet.Document.Worksheets[0];
            worksheet.Name = "Audit Log";

            // Set page orientation to Landscape
            worksheet.ActiveView.Orientation = PageOrientation.Landscape;

            // Title
            worksheet.Cells["A1"].Value = $"Change History - {documentName}";
            worksheet.MergeCells(worksheet.Range["A1:F1"]);
            worksheet.Cells["A1"].Font.Size = 14;
            worksheet.Cells["A1"].Font.Bold = true;
            worksheet.Cells["A1"].Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;

            worksheet.Cells["A2"].Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            worksheet.MergeCells(worksheet.Range["A2:F2"]);
            worksheet.Cells["A2"].Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
            worksheet.Cells["A2"].Font.Size = 10;

            // Headers
            int headerRow = 3;
            worksheet.Cells[headerRow, 0].Value = "Timestamp";
            worksheet.Cells[headerRow, 1].Value = "User";
            worksheet.Cells[headerRow, 2].Value = "Field Changed";
            worksheet.Cells[headerRow, 3].Value = "Old Value";
            worksheet.Cells[headerRow, 4].Value = "New Value";
            worksheet.Cells[headerRow, 5].Value = "Revision";

            var headerRange = worksheet.Range.FromLTRB(0, headerRow, 5, headerRow);
            headerRange.Font.Bold = true;
            headerRange.Fill.BackgroundColor = System.Drawing.Color.LightGray;
            headerRange.Borders.SetAllBorders(System.Drawing.Color.Black, BorderLineStyle.Thin);
            headerRange.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
            headerRange.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;

            // Data
            int currentRow = headerRow + 1;
            foreach (var log in logs)
            {
                worksheet.Cells[currentRow, 0].Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[currentRow, 1].Value = log.User?.Name ?? "Unknown";
                worksheet.Cells[currentRow, 2].Value = log.FieldChanged;
                worksheet.Cells[currentRow, 3].Value = log.OldValue ?? "";
                worksheet.Cells[currentRow, 4].Value = log.NewValue ?? "";
                worksheet.Cells[currentRow, 5].Value = log.Revision;

                var rowRange = worksheet.Range.FromLTRB(0, currentRow, 5, currentRow);
                rowRange.Borders.SetAllBorders(System.Drawing.Color.Black, BorderLineStyle.Thin);
                rowRange.Alignment.Vertical = SpreadsheetVerticalAlignment.Top;

                currentRow++;
            }

            // Set column widths (in characters) - proportional for landscape
            worksheet.Columns[0].WidthInCharacters = 18;  // Timestamp
            worksheet.Columns[1].WidthInCharacters = 15;  // User
            worksheet.Columns[2].WidthInCharacters = 20;  // Field Changed
            worksheet.Columns[3].WidthInCharacters = 35;  // Old Value
            worksheet.Columns[4].WidthInCharacters = 35;  // New Value
            worksheet.Columns[5].WidthInCharacters = 10;  // Revision

            // Enable word wrap for all columns
            for (int i = 0; i <= 5; i++)
            {
                worksheet.Columns[i].Alignment.WrapText = true;
            }

            // Set print options for better PDF output
            worksheet.PrintOptions.FitToPage = true;
            worksheet.PrintOptions.FitToWidth = 1;
            worksheet.PrintOptions.FitToHeight = 0; // Unlimited pages vertically

            // Export to PDF with landscape settings
            using var pdfStream = new MemoryStream();
            var pdfExportOptions = new PdfExportOptions
            {
          
                ConvertImagesToJpeg = false
            };

            spreadsheet.ExportToPdf(pdfStream, pdfExportOptions);
            File.WriteAllBytes(filePath, pdfStream.ToArray());
        }

        /// <summary>
        /// Alternative PDF export using simple memory stream approach
        /// </summary>
        public static byte[] ExportToPdfBytes(IEnumerable<AuditLog> logs, string documentName)
        {
            var spreadsheet = new SpreadsheetControl();
            spreadsheet.CreateNewDocument();

            var worksheet = spreadsheet.Document.Worksheets[0];
            worksheet.Name = "Audit Log";

            // Set page orientation to Landscape
            worksheet.ActiveView.Orientation = PageOrientation.Landscape;

            // Title
            worksheet.Cells["A1"].Value = $"Change History - {documentName}";
            worksheet.MergeCells(worksheet.Range["A1:F1"]);
            worksheet.Cells["A1"].Font.Size = 14;
            worksheet.Cells["A1"].Font.Bold = true;
            worksheet.Cells["A1"].Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;

            worksheet.Cells["A2"].Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            worksheet.MergeCells(worksheet.Range["A2:F2"]);
            worksheet.Cells["A2"].Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;

            // Headers
            int headerRow = 3;
            worksheet.Cells[headerRow, 0].Value = "Timestamp";
            worksheet.Cells[headerRow, 1].Value = "User";
            worksheet.Cells[headerRow, 2].Value = "Field Changed";
            worksheet.Cells[headerRow, 3].Value = "Old Value";
            worksheet.Cells[headerRow, 4].Value = "New Value";
            worksheet.Cells[headerRow, 5].Value = "Revision";

            var headerRange = worksheet.Range.FromLTRB(0, headerRow, 5, headerRow);
            headerRange.Font.Bold = true;
            headerRange.Fill.BackgroundColor = System.Drawing.Color.LightGray;
            headerRange.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;

            // Data
            int currentRow = headerRow + 1;
            foreach (var log in logs)
            {
                worksheet.Cells[currentRow, 0].Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[currentRow, 1].Value = log.User?.Name ?? "Unknown";
                worksheet.Cells[currentRow, 2].Value = log.FieldChanged;
                worksheet.Cells[currentRow, 3].Value = log.OldValue ?? "";
                worksheet.Cells[currentRow, 4].Value = log.NewValue ?? "";
                worksheet.Cells[currentRow, 5].Value = log.Revision;

                currentRow++;
            }

            // Column widths and wrapping
            worksheet.Columns[0].WidthInCharacters = 18;
            worksheet.Columns[1].WidthInCharacters = 15;
            worksheet.Columns[2].WidthInCharacters = 20;
            worksheet.Columns[3].WidthInCharacters = 35;
            worksheet.Columns[4].WidthInCharacters = 35;
            worksheet.Columns[5].WidthInCharacters = 10;

            for (int i = 0; i <= 5; i++)
            {
                worksheet.Columns[i].Alignment.WrapText = true;
            }

            // Print options
            worksheet.PrintOptions.FitToPage = true;
            worksheet.PrintOptions.FitToWidth = 1;
            worksheet.PrintOptions.FitToHeight = 0;

            using var pdfStream = new MemoryStream();
            var pdfExportOptions = new PdfExportOptions
            {
               
                ConvertImagesToJpeg = false
            };
            
            spreadsheet.ExportToPdf(pdfStream, pdfExportOptions);
            return pdfStream.ToArray();
        }
    }
}