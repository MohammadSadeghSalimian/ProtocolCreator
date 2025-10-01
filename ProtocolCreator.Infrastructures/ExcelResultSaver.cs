using ClosedXML.Excel;
using ProtocolCreator.Core;

namespace ProtocolCreator.Infrastructures;

public class ExcelResultSaver:IResultSaver
{
    public void Save(FileInfo file, IReadOnlyList<Delta> deltas)
    {
        ArgumentNullException.ThrowIfNull(file.Directory);
        Directory.CreateDirectory(file.Directory.FullName);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Results");

        // Write header row in a single call for better performance
        var headers = new[]
        {
            "Id", "Cycle", "Start", "End",
            "State", "IsYield","Condition",
            "Current Drift", "Destination Drift", "Delta Drift", "Depth Coefficient", "K", "Ecr","Repeat",
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
        sheet.Cell(2, 1).Value = 0; // Id
        sheet.Cell(2, 2).Value = 0; // Cycle
        sheet.Cell(2, 3).Value = 0; // Drift Start
        sheet.Cell(2, 4).Value = 0; // Drift End
        sheet.Cell(2, 5).Value = ""; // State
        sheet.Cell(2, 6).Value = ""; // IsYield
        sheet.Cell(2, 7).Value = ""; // Condition
        sheet.Cell(2, 8).Value = 0; // Current Drift
        sheet.Cell(2, 9).Value = 0; // Destination Drift
        sheet.Cell(2, 10).Value = 0; // Delta Drift
        sheet.Cell(2, 11).Value = ""; // Depth Coefficient
        sheet.Cell(2, 12).Value = 0; // Slope
        sheet.Cell(2, 13).Value = 0; // Eccentricity
        sheet.Cell(2, 14).Value = 0; // Repeat
        sheet.Cell(2, 15).Value = 0; // Delta Elongation
        sheet.Cell(2, 16).Value = 0;  // Current Elongation
        sheet.Cell(2, 17).Value = 0;  // Destination Elongation

        var rowCount = deltas.Count;
        for (var i = 0; i < rowCount; i++)
        {
            var delta = deltas[i];
            var drift = delta.Drift;
            var elongation = delta.Elongation;
            var segment = delta.Segment;
            var section = delta.Section;
            var rowIdx = i + 3;

            sheet.Cell(rowIdx, 1).Value = delta.Id; // Id
            sheet.Cell(rowIdx, 2).Value = delta.Cycle; // Cycle
            sheet.Cell(rowIdx, 3).Value = segment.Start; // Drift Start
            sheet.Cell(rowIdx, 4).Value = segment.End; // Drift End
            sheet.Cell(rowIdx, 5).Value = segment.CycleState.ToString(); // State
            sheet.Cell(rowIdx, 6).Value = (section.ExperiencedYield)?"Yes":"No"; // IsYield
            sheet.Cell(rowIdx, 7).Value = section.Condition.ToString(); // IsYield
            sheet.Cell(rowIdx, 8).Value = drift.Start; // Current Drift (using Start)
            sheet.Cell(rowIdx, 9).Value = drift.End; // Destination Drift (using End)
            sheet.Cell(rowIdx, 10).Value = drift.End-drift.Start; // Delta Drift
            sheet.Cell(rowIdx, 11).Value = section.DepthCoefficient; // Depth Coefficient
            sheet.Cell(rowIdx, 12).Value = section.Slope; // K
            sheet.Cell(rowIdx, 13).Value = section.Eccentricity; // Ecr
            sheet.Cell(rowIdx, 14).Value = section.Repeat; // Repeat
            sheet.Cell(rowIdx, 15).Value = elongation.End - elongation.Start; // Delta Elongation
            sheet.Cell(rowIdx, 16).Value = elongation.Start; // Current Elongation,Elongation
            sheet.Cell(rowIdx, 17).Value = elongation.End; // Destination Elongation

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
    

       


        workbook.SaveAs(file.FullName);
    }
}