// ***********************************************************************************
// File: CspProject/Services/FmeaTemplateGenerator.cs
// Description: Contains all logic for creating the visual FMEA template.
// Author: Enes Orak
// ***********************************************************************************

using System.IO;
using CspProject.Models;
using DevExpress.Spreadsheet;
 
using Color = System.Drawing.Color;
using DataValidationType = DevExpress.Spreadsheet.DataValidationType;
using Worksheet = DevExpress.Spreadsheet.Worksheet;

namespace CspProject.Services;

    public static class FmeaTemplateGenerator
    {
      public static void Apply(IWorkbook workbook)
        {
            var severityScale = GetSeverityScale();

            workbook.Styles.DefaultStyle.Font.Name = "Century Gothic";
            workbook.Unit = DevExpress.Office.DocumentUnit.Point;

            Worksheet worksheet = workbook.Worksheets[0];
            worksheet.Name = "DFMEA";

            Color headerBlue = ColorTranslator.FromHtml("#A9D0F5");
            Color subHeaderBlue = ColorTranslator.FromHtml("#DDEBF7");
            Color groupGray = ColorTranslator.FromHtml("#E0E0E0");
            Color rpnCyan = ColorTranslator.FromHtml("#AFEEEE");
            Color titleBlockLabelGray = ColorTranslator.FromHtml("#F2F2F2");

            worksheet.Rows.Insert(0, 10);
            var labelStyle = workbook.Styles.Add("LabelStyle");
            labelStyle.Fill.BackgroundColor = titleBlockLabelGray;
            labelStyle.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
            labelStyle.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
            labelStyle.Alignment.WrapText = true;
            labelStyle.Borders.SetAllBorders(Color.Gray, BorderLineStyle.Thin);
            var dataStyle = workbook.Styles.Add("DataStyle");
            dataStyle.Borders.SetAllBorders(Color.Gray, BorderLineStyle.Thin);
            worksheet.MergeCells(worksheet.Range["B3:F3"]);
            worksheet.Cells["B3"].Value = "Product / Part";
            worksheet.MergeCells(worksheet.Range["G3:H3"]);
            worksheet.Cells["G3"].Value = "DFMEA ID";
            worksheet.MergeCells(worksheet.Range["I3:J3"]);
            worksheet.Cells["I3"].Value = "Revision";
            worksheet.MergeCells(worksheet.Range["K3:L3"]);
            worksheet.Cells["K3"].Value = "Date";
            worksheet.Range["B3:L3"].Style = labelStyle;
            worksheet.MergeCells(worksheet.Range["B4:F4"]);
            worksheet.MergeCells(worksheet.Range["G4:H4"]);
            worksheet.MergeCells(worksheet.Range["I4:J4"]);
            worksheet.MergeCells(worksheet.Range["K4:L4"]);
            worksheet.Range["B4:L4"].Style = dataStyle;
            worksheet.MergeCells(worksheet.Range["B5:F5"]);
            worksheet.Cells["B5"].Value = "Project";
            worksheet.MergeCells(worksheet.Range["G5:H5"]);
            worksheet.Cells["G5"].Value = "Party Responsible";
            worksheet.MergeCells(worksheet.Range["I5:J5"]);
            worksheet.Cells["I5"].Value = "Approved By";
            worksheet.MergeCells(worksheet.Range["K5:L5"]);
            worksheet.Cells["K5"].Value = "Date Completed";
            worksheet.Range["B5:L5"].Style = labelStyle;
            worksheet.MergeCells(worksheet.Range["B6:F6"]);
            worksheet.MergeCells(worksheet.Range["G6:H6"]);
            worksheet.MergeCells(worksheet.Range["I6:J6"]);
            worksheet.MergeCells(worksheet.Range["K6:L6"]);
            worksheet.Range["B6:L6"].Style = dataStyle;
            worksheet.MergeCells(worksheet.Range["B7:L7"]);
            worksheet.Cells["B7"].Value = "Team";
            worksheet.Range["B7:L7"].Style = labelStyle;
            worksheet.MergeCells(worksheet.Range["B8:L8"]);
            worksheet.Range["B8:L8"].Style = dataStyle;
            worksheet.Rows[2].RowHeight = 25;
            worksheet.Rows[3].RowHeight = 25;
            worksheet.Rows[4].RowHeight = 25;
            worksheet.Rows[5].RowHeight = 25;
            worksheet.Rows[6].RowHeight = 25;
            worksheet.Rows[7].RowHeight = 25;

            int headerRow = 11;
            int subHeaderRow = 12;
            worksheet.Cells[9, 1].Value = "Design Failure Mode and Effects Analysis (DFMEA)";
            worksheet.Cells[9, 15].Value = "RPN Columns contain a formula to auto-calculate; do not alter or delete.";
            worksheet.Cells[9, 15].Font.Italic = true;
            worksheet.Cells[9, 15].Alignment.Horizontal = SpreadsheetHorizontalAlignment.Right;
            worksheet.Rows[9].Font.Size = 10;
            var mainHeaderRange = worksheet.Range[$"B{headerRow}:V{headerRow}"];
            var subHeaderRange = worksheet.Range[$"B{subHeaderRow}:V{subHeaderRow}"];
            var combinedHeaderRange = worksheet.Range[$"B{headerRow}:V{subHeaderRow}"];
            combinedHeaderRange.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Left;
            combinedHeaderRange.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
            combinedHeaderRange.Alignment.WrapText = true;
            mainHeaderRange.FillColor = headerBlue;
            subHeaderRange.FillColor = subHeaderBlue;
            worksheet.Rows[headerRow - 1].RowHeight = 44;
            worksheet.Rows[subHeaderRow - 1].RowHeight = 60;
            
            CreateHeader(worksheet, "B", "ID", "#", 0, true);
            CreateHeader(worksheet, "C", "Item", "Product component or part");
            CreateHeader(worksheet, "D", "Function", "Primary function");
            CreateHeader(worksheet, "E", "Potential Failure Mode", "How could the system / item / process potentially fail?");
            CreateHeader(worksheet, "F", "Effects of Failure", "Consequential impact on other systems, departments, etc.");
            CreateHeader(worksheet, "G", "Severity", "", 90, true);
            CreateHeader(worksheet, "H", "Causes", "All contributing factors");
            CreateHeader(worksheet, "I", "Occurrence", "", 90, true);
            worksheet.MergeCells(worksheet.Range[$"J{headerRow}:K{headerRow}"]);
            worksheet.Cells[$"J{headerRow}"].Value = "Current Design Controls";
            worksheet.Cells[$"J{subHeaderRow}"].Value = "Prevention";
            worksheet.Cells[$"K{subHeaderRow}"].Value = "Detection";
            CreateHeader(worksheet, "L", "Detection", "", 90, true);
            CreateHeader(worksheet, "M", "RPN", "Risk Priority Number");
            CreateHeader(worksheet, "N", "Recommended Actions", "Steps required to reduce severity, occurrence, and detection");
            CreateHeader(worksheet, "O", "Owner", "Organization, team, or individual responsible");
            CreateHeader(worksheet, "P", "Date Due", "Target date of completion");
            CreateHeader(worksheet, "Q", "Action Results", "Actions taken");
            CreateHeader(worksheet, "R", "Date Completed", "Actual date of completion");
            CreateHeader(worksheet, "S", "Severity", "", 90, true);
            CreateHeader(worksheet, "T", "Occurrence", "", 90, true);
            CreateHeader(worksheet, "U", "Detection", "", 90, true);
            CreateHeader(worksheet, "V", "RPN", "Risk Priority Number");
    
            var specialHeaders = worksheet.Range[$"G{headerRow}:G{subHeaderRow},I{headerRow}:I{subHeaderRow},L{headerRow}:L{subHeaderRow},M{headerRow}:M{subHeaderRow},S{headerRow}:S{subHeaderRow},T{headerRow}:T{subHeaderRow},U{headerRow}:U{subHeaderRow},V{headerRow}:V{subHeaderRow}"];
            specialHeaders.Font.Size = 13;
            specialHeaders.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
            specialHeaders.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;

            for (int i = 13; i <= 26; i++)
            {
                worksheet.Rows[i - 1].RowHeight = 50;
            }

            worksheet.Range[$"B{headerRow}:B26"].FillColor = groupGray;
            worksheet.Range[$"G{headerRow}:G26"].FillColor = groupGray;
            worksheet.Range[$"I{headerRow}:I26"].FillColor = groupGray;
            worksheet.Range[$"L{headerRow}:L26"].FillColor = groupGray;
            worksheet.Range[$"S{headerRow}:S26"].FillColor = groupGray;
            worksheet.Range[$"T{headerRow}:T26"].FillColor = groupGray;
            worksheet.Range[$"U{headerRow}:U26"].FillColor = groupGray;
            worksheet.Range[$"M{headerRow}:M26"].FillColor = rpnCyan;
            worksheet.Range[$"V{headerRow}:V26"].FillColor = rpnCyan;
            worksheet.Cells[$"M{headerRow}"].FillColor = headerBlue;
            worksheet.Cells[$"M{subHeaderRow}"].FillColor = subHeaderBlue;
            worksheet.Cells[$"V{headerRow}"].FillColor = headerBlue;
            worksheet.Cells[$"V{subHeaderRow}"].FillColor = subHeaderBlue;

            int dataStartRow = subHeaderRow + 1;
            Worksheet ratingSheet = workbook.Worksheets.Add("RatingScales");
            ratingSheet.Cells["A1"].Value = "Score";
            ratingSheet.Cells["B1"].Value = "Rating Text";
            for (int i = 0; i < severityScale.Count; i++)
            {
                ratingSheet.Cells[i + 1, 0].Value = severityScale[i].Score;
                ratingSheet.Cells[i + 1, 1].Value = severityScale[i].FullText;
            }

            var scoreListRange = ratingSheet.Range["A2:A11"];
            worksheet.DataValidations.Add(worksheet.Range[$"G{dataStartRow}:G100"], DataValidationType.List, ValueObject.FromRange(scoreListRange));
            worksheet.DataValidations.Add(worksheet.Range[$"I{dataStartRow}:I100"], DataValidationType.List, ValueObject.FromRange(scoreListRange));
            worksheet.DataValidations.Add(worksheet.Range[$"L{dataStartRow}:L100"], DataValidationType.List, ValueObject.FromRange(scoreListRange));
            worksheet.DataValidations.Add(worksheet.Range[$"S{dataStartRow}:S100"], DataValidationType.List, ValueObject.FromRange(scoreListRange));
            worksheet.DataValidations.Add(worksheet.Range[$"T{dataStartRow}:T100"], DataValidationType.List, ValueObject.FromRange(scoreListRange));
            worksheet.DataValidations.Add(worksheet.Range[$"U{dataStartRow}:U100"], DataValidationType.List, ValueObject.FromRange(scoreListRange));

            for (int i = dataStartRow; i <= 26; i++)
            {
                string rpnFormula = $"=IFERROR(G{i}*I{i}*L{i},0)";
                worksheet.Cells[$"M{i}"].Formula = rpnFormula;
                string rpnAfterFormula = $"=IFERROR(S{i}*T{i}*U{i},0)";
                worksheet.Cells[$"V{i}"].Formula = rpnAfterFormula;
            }

            var centeredDataStyle = workbook.Styles.Add("CenteredDataStyle");
            centeredDataStyle.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
            centeredDataStyle.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
            worksheet.Range[$"B{dataStartRow}:B100"].Style = centeredDataStyle;
            worksheet.Range[$"G{dataStartRow}:G100"].Style = centeredDataStyle;
            worksheet.Range[$"I{dataStartRow}:I100"].Style = centeredDataStyle;
            worksheet.Range[$"L{dataStartRow}:L100"].Style = centeredDataStyle;
            worksheet.Range[$"S{dataStartRow}:S100"].Style = centeredDataStyle;
            worksheet.Range[$"T{dataStartRow}:T100"].Style = centeredDataStyle;
            worksheet.Range[$"U{dataStartRow}:U100"].Style = centeredDataStyle;

            for (int i = 0; i < (26 - dataStartRow + 1); i++)
            {
                worksheet.Cells[dataStartRow + i - 1, 1].Value = i + 1;
            }

            worksheet.Cells[$"G{dataStartRow}"].Value = 10;
            worksheet.Cells[$"I{dataStartRow}"].Value = 8;
            worksheet.Cells[$"L{dataStartRow}"].Value = 7;

            // DÜZELTME: Koşullu Renklendirme ve Kenarlıklar
            var ratingColumns = new[] { "G", "I", "L", "S", "T", "U" };
            ConditionalFormattingCollection conditionalFormattings = worksheet.ConditionalFormattings;

            foreach (var col in ratingColumns)
            {
                var rangeToFormat = worksheet.Range[$"{col}{dataStartRow}:{col}100"];
                
                ConditionalFormattingValue minPoint = conditionalFormattings.CreateValue(ConditionalFormattingValueType.Number, "2");
                ConditionalFormattingValue midPoint = conditionalFormattings.CreateValue(ConditionalFormattingValueType.Number, "4");
                ConditionalFormattingValue maxPoint = conditionalFormattings.CreateValue(ConditionalFormattingValueType.Number, "7");

                conditionalFormattings.AddColorScale3ConditionalFormatting(rangeToFormat, 
                    minPoint, Color.Green, 
                    midPoint, Color.Yellow, 
                    maxPoint, Color.Red);
            }
            
            worksheet.Range[$"B{headerRow}:V26"].Borders.SetAllBorders(Color.Gray, BorderLineStyle.Thin);

            ratingSheet.Visible = false;

            worksheet.Columns["A"].WidthInCharacters = 2;
            worksheet.Columns["B"].WidthInCharacters = 4;
            worksheet.Columns["C"].WidthInCharacters = 13.71;
            worksheet.Columns["D"].WidthInCharacters = 13.71;
            worksheet.Columns["E"].WidthInCharacters = 15;
            worksheet.Columns["F"].WidthInCharacters = 15;
            worksheet.Columns["G"].WidthInCharacters = 5.71;
            worksheet.Columns["H"].WidthInCharacters = 13.14;
            worksheet.Columns["I"].WidthInCharacters = 5.71;
            worksheet.Columns["J"].WidthInCharacters = 13.14;
            worksheet.Columns["K"].WidthInCharacters = 13.14;
            worksheet.Columns["L"].WidthInCharacters = 5.71;
            worksheet.Columns["M"].WidthInCharacters = 9;
            worksheet.Columns["N"].WidthInCharacters = 15;
            worksheet.Columns["O"].WidthInCharacters = 20;
            worksheet.Columns["P"].WidthInCharacters = 12;
            worksheet.Columns["Q"].WidthInCharacters = 15;
            worksheet.Columns["R"].WidthInCharacters = 15;
            worksheet.Columns["S"].WidthInCharacters = 5.71;
            worksheet.Columns["T"].WidthInCharacters = 5.71;
            worksheet.Columns["U"].WidthInCharacters = 5.71;
            worksheet.Columns["V"].WidthInCharacters = 9;
            
            // 1. Hem yatay hem dikey ortalanacak hücreleri grupla ve tek seferde uygula.
            var centerAlignedCells = worksheet.Range["G4, I4, K4, I6, K6"];
            centerAlignedCells.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
            centerAlignedCells.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;

// 2. Sadece dikey ortalanacak hücreleri grupla ve tek seferde uygula.
// (dataStyle'dan dikey ortalamayı zaten aldılar ama burada tekrar belirtmek de sorun olmaz)
            var verticallyAlignedCells = worksheet.Range["B4, B6, B8"];
            verticallyAlignedCells.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
            
        }
      
      
        /// <summary>
        /// Creates a new FMEA template workbook and returns it as a byte array.
        /// </summary>
        /// <returns>A byte[] representing the XLSX file content.</returns>
        /// 
        [Obsolete]
        public static byte[] GenerateFmeaTemplateBytes()
        {
            // 1. Create a new, blank workbook in memory.
            var workbook = new Workbook();
            Apply(workbook);
            using (var memoryStream = new MemoryStream())
            {
                workbook.SaveDocument(memoryStream, DevExpress.Spreadsheet.DocumentFormat.Xlsx);
                return memoryStream.ToArray();
            }
        }
 
        private static void CreateHeader(Worksheet worksheet, string column, string title, string subtitle, int rotation = 0, bool isCentered = false)
        {
            int headerRow = 11;
            int subHeaderRow = 12;

            var cell = worksheet.Cells[$"{column}{headerRow}"];
            var subCell = worksheet.Cells[$"{column}{subHeaderRow}"];
            var combinedRange = worksheet.Range[$"{column}{headerRow}:{column}{subHeaderRow}"];

            if (!string.IsNullOrEmpty(subtitle))
            {
                worksheet.MergeCells(worksheet.Range[$"{column}{headerRow}:{column}{headerRow}"]);
                cell.Value = title;
                subCell.Value = subtitle;
            }
            else
            {
                worksheet.MergeCells(combinedRange);
                cell.Value = title;
            }

            if (rotation != 0)
            {
                cell.Alignment.RotationAngle = rotation;
            }

            if (isCentered)
            {
                combinedRange.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
            }
        }

        private static List<FmeaRating> GetSeverityScale()
        {
            return new List<FmeaRating>
            {
                new FmeaRating(1, "None", "No effect on performance or safety"),
                new FmeaRating(2, "Very Minor", "Results in no noticeable design issues, but could involve minor tweaks"),
                new FmeaRating(3, "Minor", "Causes negligible design flaws with no real impact on performance"),
                new FmeaRating(4, "Very Low", "Causes minor issues, such as slight inefficiencies, without customer impact"),
                new FmeaRating(5, "Low", "Causes slight reduction in performance but product remains functional"),
                new FmeaRating(6, "Moderate", "Causes moderate reduction in functionality or minor customer issues"),
                new FmeaRating(7, "High", "Causes noticeable performance degradation or safety concerns"),
                new FmeaRating(8, "Major", "Causes significant system failure, loss of function, or customer dissatisfaction"),
                new FmeaRating(9, "Critical - With Warning", "Results in catastrophic failure, major harm, or system loss, with warning"),
                new FmeaRating(10, "Critical - No Warning", "Results in catastrophic failure, major harm, or system loss, without warning")
            }.OrderBy(r => r.Score).ToList();
        }
        
        
        /// <summary>
/// Sadece title block (ilk 9 satır) içeren minimal template oluşturur.
/// Kullanıcı bu template'i temel alarak kendi template'ini yaratabilir.
/// </summary>
public static void ApplyTitleBlockOnly(IWorkbook workbook)
{
    workbook.Styles.DefaultStyle.Font.Name = "Century Gothic";
    workbook.Unit = DevExpress.Office.DocumentUnit.Point;

    Worksheet worksheet = workbook.Worksheets[0];
    worksheet.Name = "Template";

    Color titleBlockLabelGray = ColorTranslator.FromHtml("#F2F2F2");

    // İlk 9 satırı ekle (0-8 index)
    worksheet.Rows.Insert(0, 10);
    
    // Label ve Data stilleri
    var labelStyle = workbook.Styles.Add("LabelStyle");
    labelStyle.Fill.BackgroundColor = titleBlockLabelGray;
    labelStyle.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
    labelStyle.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
    labelStyle.Alignment.WrapText = true;
    labelStyle.Borders.SetAllBorders(Color.Gray, BorderLineStyle.Thin);
    
    var dataStyle = workbook.Styles.Add("DataStyle");
    dataStyle.Borders.SetAllBorders(Color.Gray, BorderLineStyle.Thin);

    // ==================== ROW 3 ====================
    worksheet.MergeCells(worksheet.Range["B3:F3"]);
    worksheet.Cells["B3"].Value = "Product / Part";
    worksheet.MergeCells(worksheet.Range["G3:H3"]);
    worksheet.Cells["G3"].Value = "Document ID";
    worksheet.MergeCells(worksheet.Range["I3:J3"]);
    worksheet.Cells["I3"].Value = "Revision";
    worksheet.MergeCells(worksheet.Range["K3:L3"]);
    worksheet.Cells["K3"].Value = "Date";
    worksheet.Range["B3:L3"].Style = labelStyle;
    
    // ROW 4 - Data cells
    worksheet.MergeCells(worksheet.Range["B4:F4"]);
    worksheet.MergeCells(worksheet.Range["G4:H4"]);
    worksheet.MergeCells(worksheet.Range["I4:J4"]);
    worksheet.MergeCells(worksheet.Range["K4:L4"]);
    worksheet.Range["B4:L4"].Style = dataStyle;

    // ==================== ROW 5 ====================
    worksheet.MergeCells(worksheet.Range["B5:F5"]);
    worksheet.Cells["B5"].Value = "Project";
    worksheet.MergeCells(worksheet.Range["G5:H5"]);
    worksheet.Cells["G5"].Value = "Party Responsible";
    worksheet.MergeCells(worksheet.Range["I5:J5"]);
    worksheet.Cells["I5"].Value = "Approved By";
    worksheet.MergeCells(worksheet.Range["K5:L5"]);
    worksheet.Cells["K5"].Value = "Date Completed";
    worksheet.Range["B5:L5"].Style = labelStyle;
    
    // ROW 6 - Data cells
    worksheet.MergeCells(worksheet.Range["B6:F6"]);
    worksheet.MergeCells(worksheet.Range["G6:H6"]);
    worksheet.MergeCells(worksheet.Range["I6:J6"]);
    worksheet.MergeCells(worksheet.Range["K6:L6"]);
    worksheet.Range["B6:L6"].Style = dataStyle;

    // ==================== ROW 7 ====================
    worksheet.MergeCells(worksheet.Range["B7:L7"]);
    worksheet.Cells["B7"].Value = "Team";
    worksheet.Range["B7:L7"].Style = labelStyle;
    
    // ROW 8 - Data cell
    worksheet.MergeCells(worksheet.Range["B8:L8"]);
    worksheet.Range["B8:L8"].Style = dataStyle;

    // Row heights
    worksheet.Rows[2].RowHeight = 25;
    worksheet.Rows[3].RowHeight = 25;
    worksheet.Rows[4].RowHeight = 25;
    worksheet.Rows[5].RowHeight = 25;
    worksheet.Rows[6].RowHeight = 25;
    worksheet.Rows[7].RowHeight = 25;

    // Column widths (sadece title block için gerekli olanlar)
    worksheet.Columns["A"].WidthInCharacters = 2;
    worksheet.Columns["B"].WidthInCharacters = 13.71;
    worksheet.Columns["C"].WidthInCharacters = 13.71;
    worksheet.Columns["D"].WidthInCharacters = 13.71;
    worksheet.Columns["E"].WidthInCharacters = 13.71;
    worksheet.Columns["F"].WidthInCharacters = 13.71;
    worksheet.Columns["G"].WidthInCharacters = 13.14;
    worksheet.Columns["H"].WidthInCharacters = 13.14;
    worksheet.Columns["I"].WidthInCharacters = 13.14;
    worksheet.Columns["J"].WidthInCharacters = 13.14;
    worksheet.Columns["K"].WidthInCharacters = 12;
    worksheet.Columns["L"].WidthInCharacters = 12;

    // Alignment düzeltmeleri
    var centerAlignedCells = worksheet.Range["G4, I4, K4, I6, K6"];
    centerAlignedCells.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
    centerAlignedCells.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;

    var verticallyAlignedCells = worksheet.Range["B4, B6, B8"];
    verticallyAlignedCells.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;

    // ROW 10'a açıklama ekle (opsiyonel)
    worksheet.Cells["B10"].Value = "Add your custom content below...";
    worksheet.Cells["B10"].Font.Italic = true;
    worksheet.Cells["B10"].Font.Color = Color.Gray;
}
      
    }