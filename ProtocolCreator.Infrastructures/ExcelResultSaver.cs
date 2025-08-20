using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using ProtocolCreator.Core;

namespace ProtocolCreator.Infrastructures;

public class ExcelResultSaver : IResultSaver
{
    public void Save(FileInfo file, IReadOnlyList<DeData> deltas)
    {
        ArgumentNullException.ThrowIfNull(file.Directory);
        Directory.CreateDirectory(file.Directory.FullName);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Results");

        // Write header row in a single call for better performance
        var headers = new[]
        {
            "Id", "Cycle", "Start", "End",
            "State", "IsYield",
            "Current Drift", "Destination Drift", "Delta Drift", "Depth Coefficient", "K", "Ecr",
            "Delta Elongation", "Current Elongation","Destination Elongation",
        };
        var rowRange = sheet.Range(1, 1, 1, headers.Length);
        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#333333");
        rowRange.Style.Font.FontColor = XLColor.White;

        for (var h = 0; h < headers.Length; h++)
        {
            sheet.Cell(1, h + 1).Value = headers[h];
        }

        // Example row (can be removed if not needed)
        sheet.Cell(2, 1).Value = 0;
        sheet.Cell(2, 2).Value = 0;
        sheet.Cell(2, 3).Value = 0;
        sheet.Cell(2, 4).Value = 0;
        sheet.Cell(2, 5).Value = "";
        sheet.Cell(2, 6).Value = "";
        sheet.Cell(2, 7).Value = 0;
        sheet.Cell(2, 8).Value = 0;
        sheet.Cell(2, 9).Value = 0;
        sheet.Cell(2, 10).Value = "";
        sheet.Cell(2, 11).Value = 0;
        sheet.Cell(2, 12).Value = 0;
        sheet.Cell(2, 13).Value = 0;
        sheet.Cell(2, 14).Value = 0;
        sheet.Cell(2, 15).Value = 0;

        var rowCount = deltas.Count;
        for (var i = 0; i < rowCount; i++)
        {
            var delta = deltas[i];
            var drift = delta.Drift;
            var elongation = delta.Elongation;
            var segment = delta.DriftSegment;
            var section = delta.Section;
            var rowIdx = i + 3;

            sheet.Cell(rowIdx, 1).Value = delta.Id; // Id
            sheet.Cell(rowIdx, 2).Value = segment.Cycle; // Cycle
            sheet.Cell(rowIdx, 3).Value = segment.Start; // Drift Start
            sheet.Cell(rowIdx, 4).Value = segment.End; // Drift End
            sheet.Cell(rowIdx, 5).Value = segment.CycleState.ToString(); // State
            sheet.Cell(rowIdx, 6).Value = section.IsYield; // IsYield
            sheet.Cell(rowIdx, 7).Value = drift.Start; // Current Drift (using Start)
            sheet.Cell(rowIdx, 8).Value = drift.End; // Destination Drift (using End)
            sheet.Cell(rowIdx, 9).Value = drift.Delta; // Delta Drift
            sheet.Cell(rowIdx, 10).Value = section.DepthCoefficient; // Depth Coefficient
            sheet.Cell(rowIdx, 11).Value = section.K; // K
            sheet.Cell(rowIdx, 12).Value = section.Eccentricity; // Ecr
            sheet.Cell(rowIdx, 13).Value = elongation.End - elongation.Start; // Delta Elongation
            sheet.Cell(rowIdx, 14).Value = elongation.Start; // Current Elongation,Elongation
            sheet.Cell(rowIdx, 15).Value = elongation.End; // Destination Elongation

            // Set fill color based on state
            var state = segment.CycleState;
            XLColor fillColor = XLColor.NoColor;
            switch (state)
            {
                case CycleState.PL:
                    fillColor = XLColor.GrannySmithApple;
                    break;
                case CycleState.PU:
                    fillColor = XLColor.ColumbiaBlue;
                    break;
                case CycleState.NL:
                    fillColor = XLColor.BrilliantLavender;
                    break;
                case CycleState.NU:
                    fillColor = XLColor.BubbleGum;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            rowRange = sheet.Range(rowIdx, 1, rowIdx, headers.Length);
            rowRange.Style.Fill.BackgroundColor = fillColor;
        }

        // Set borders for all cells (header, example, and data rows)
        var totalRows = rowCount + 2;
        var tableRange = sheet.Range(1, 1, totalRows, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // Adjust column widths to fit content
        for (var col = 1; col <= headers.Length; col++)
        {
            sheet.Column(col).AdjustToContents();
        }

        // Add chart for Id vs Destination Elongation
        // Chart will be placed at row 2, two columns after last data column
        var chartCol = headers.Length + 2;
        var chartRow = 2;
        var idCol = 1;
        var elongationEndCol = 15;
        var firstDataRow = 3;
        var lastDataRow = rowCount + 2;

        // Prepare data for chart in a new range
        var chartDataStartCol = chartCol;
        var chartDataStartRow = chartRow;
        sheet.Cell(chartDataStartRow, chartDataStartCol).Value = "Id";
        sheet.Cell(chartDataStartRow, chartDataStartCol + 1).Value = "Destination Elongation";
        for (int i = 0; i < rowCount; i++)
        {
            sheet.Cell(chartDataStartRow + i + 1, chartDataStartCol).Value = sheet.Cell(firstDataRow + i, idCol).Value;
            sheet.Cell(chartDataStartRow + i + 1, chartDataStartCol + 1).Value = sheet.Cell(firstDataRow + i, elongationEndCol).Value;
        }



        workbook.SaveAs(file.FullName);
    }
}