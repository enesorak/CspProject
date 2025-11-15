using System.IO;
using ClosedXML.Excel;
using CspProject.Data.Entities;
using DevExpress.Spreadsheet;
using DevExpress.Xpf.Spreadsheet;
using DevExpress.XtraPrinting;

namespace CspProject.Services.Export
{
    /// <summary>
    /// Export service for system-wide change logs (includes Document column)
    /// </summary>
    public static class ChangeLogExportService
    {
        /// <summary>
        /// Exports system-wide change logs to Excel format with landscape orientation
        /// </summary>
        public static void ExportToExcel(IEnumerable<AuditLog> logs, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Change Log");

            // Set page orientation to Landscape
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 0);
            worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;

            // Header styling
            worksheet.Cell(1, 1).Value = "System-Wide Change History";
            worksheet.Range(1, 1, 1, 7).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            worksheet.Range(2, 1, 2, 7).Merge();
            worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, 1).Style.Font.FontSize = 10;

            // Column headers
            int headerRow = 4;
            worksheet.Cell(headerRow, 1).Value = "Timestamp";
            worksheet.Cell(headerRow, 2).Value = "User";
            worksheet.Cell(headerRow, 3).Value = "Document";
            worksheet.Cell(headerRow, 4).Value = "Field Changed";
            worksheet.Cell(headerRow, 5).Value = "Old Value";
            worksheet.Cell(headerRow, 6).Value = "New Value";
            worksheet.Cell(headerRow, 7).Value = "Revision";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 7);
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
                worksheet.Cell(currentRow, 3).Value = log.Document?.DocumentName ?? "N/A";
                worksheet.Cell(currentRow, 4).Value = log.FieldChanged;
                worksheet.Cell(currentRow, 5).Value = log.OldValue ?? "";
                worksheet.Cell(currentRow, 6).Value = log.NewValue ?? "";
                worksheet.Cell(currentRow, 7).Value = log.Revision;

                var dataRange = worksheet.Range(currentRow, 1, currentRow, 7);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                currentRow++;
            }

            // Set column widths - proportional for landscape
            worksheet.Column(1).Width = 18;  // Timestamp
            worksheet.Column(2).Width = 15;  // User
            worksheet.Column(3).Width = 20;  // Document
            worksheet.Column(4).Width = 20;  // Field Changed
            worksheet.Column(5).Width = 30;  // Old Value
            worksheet.Column(6).Width = 30;  // New Value
            worksheet.Column(7).Width = 10;  // Revision

            // Enable text wrapping for all columns
            for (int col = 1; col <= 7; col++)
            {
                worksheet.Column(col).Style.Alignment.WrapText = true;
            }

            // Auto-adjust row heights based on content
            worksheet.Rows().AdjustToContents();

            workbook.SaveAs(filePath);
        }

        /// <summary>
        /// Exports system-wide change logs to PDF format with landscape orientation
        /// </summary>
        public static void ExportToPdf(IEnumerable<AuditLog> logs, string filePath)
        {
            var spreadsheet = new SpreadsheetControl();
            spreadsheet.CreateNewDocument();

            var worksheet = spreadsheet.Document.Worksheets[0];
            worksheet.Name = "Change Log";

            // Set page orientation to Landscape
            worksheet.ActiveView.Orientation = PageOrientation.Landscape;

            // Title
            worksheet.Cells["A1"].Value = "System-Wide Change History";
            worksheet.MergeCells(worksheet.Range["A1:G1"]);
            worksheet.Cells["A1"].Font.Size = 14;
            worksheet.Cells["A1"].Font.Bold = true;
            worksheet.Cells["A1"].Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;

            worksheet.Cells["A2"].Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            worksheet.MergeCells(worksheet.Range["A2:G2"]);
            worksheet.Cells["A2"].Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
            worksheet.Cells["A2"].Font.Size = 10;

            // Headers
            int headerRow = 3;
            worksheet.Cells[headerRow, 0].Value = "Timestamp";
            worksheet.Cells[headerRow, 1].Value = "User";
            worksheet.Cells[headerRow, 2].Value = "Document";
            worksheet.Cells[headerRow, 3].Value = "Field Changed";
            worksheet.Cells[headerRow, 4].Value = "Old Value";
            worksheet.Cells[headerRow, 5].Value = "New Value";
            worksheet.Cells[headerRow, 6].Value = "Revision";

            var headerRange = worksheet.Range.FromLTRB(0, headerRow, 6, headerRow);
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
                worksheet.Cells[currentRow, 2].Value = log.Document?.DocumentName ?? "N/A";
                worksheet.Cells[currentRow, 3].Value = log.FieldChanged;
                worksheet.Cells[currentRow, 4].Value = log.OldValue ?? "";
                worksheet.Cells[currentRow, 5].Value = log.NewValue ?? "";
                worksheet.Cells[currentRow, 6].Value = log.Revision;

                var rowRange = worksheet.Range.FromLTRB(0, currentRow, 6, currentRow);
                rowRange.Borders.SetAllBorders(System.Drawing.Color.Black, BorderLineStyle.Thin);
                rowRange.Alignment.Vertical = SpreadsheetVerticalAlignment.Top;

                currentRow++;
            }

            // Set column widths (in characters) - proportional for landscape
            worksheet.Columns[0].WidthInCharacters = 18;  // Timestamp
            worksheet.Columns[1].WidthInCharacters = 15;  // User
            worksheet.Columns[2].WidthInCharacters = 20;  // Document
            worksheet.Columns[3].WidthInCharacters = 20;  // Field Changed
            worksheet.Columns[4].WidthInCharacters = 30;  // Old Value
            worksheet.Columns[5].WidthInCharacters = 30;  // New Value
            worksheet.Columns[6].WidthInCharacters = 10;  // Revision

            // Enable word wrap for all columns
            for (int i = 0; i <= 6; i++)
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
    }
}