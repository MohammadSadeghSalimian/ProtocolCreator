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
            "Id", "Start", "End", "Center", "Direction", "Loading Phase",
            "Start Elongation", "End Elongation", "Center Elongation",
            "RebarCondition", "Eccentricity", "DepthCoefficient", "K", "Repeat"
        };
        for (var h = 0; h < headers.Length; h++)
        {
            sheet.Cell(1, h + 1).Value = headers[h];
        }

        // Use a single loop, minimize property access, and avoid repeated .Row() calls
        var rowCount = deltas.Count;
        for (var i = 0; i < rowCount; i++)
        {
            var delta = deltas[i];
            var drift = delta.Drift;
            var elongation = delta.Elongation;
            var section = delta.Section;
            var rowIdx = i + 2;

            sheet.Cell(rowIdx, 1).Value = i + 1; // Id
            sheet.Cell(rowIdx, 2).Value = drift.Start;
            sheet.Cell(rowIdx, 3).Value = drift.End;
            sheet.Cell(rowIdx, 4).Value = drift.Center;
            sheet.Cell(rowIdx, 5).Value = elongation.Direction.ToString();
            sheet.Cell(rowIdx, 6).Value = elongation.Loading.ToString();
            sheet.Cell(rowIdx, 7).Value = elongation.Start;
            sheet.Cell(rowIdx, 8).Value = elongation.End;
            sheet.Cell(rowIdx, 9).Value = elongation.Center;
            sheet.Cell(rowIdx, 10).Value = section.RebarCondition.ToString();
            sheet.Cell(rowIdx, 11).Value = section.Eccentricity;
            sheet.Cell(rowIdx, 12).Value = section.DepthCoefficient;
            sheet.Cell(rowIdx, 13).Value = section.K;
            sheet.Cell(rowIdx, 14).Value = section.Repeat;
        }
        workbook.SaveAs(file.FullName);
    }
}


public class DriftSegmentCreator : IDriftSegmentCreator
{
    public void Create(FileInfo file, int repeat, IReadOnlyList<double> driftLevels)
    {
        if (repeat <= 0)
            throw new ArgumentOutOfRangeException(nameof(repeat), "Repeat must be greater than zero.");

        ArgumentNullException.ThrowIfNull(file.Directory);
        Directory.CreateDirectory(file.Directory.FullName);

        using var workbook = new XLWorkbook(file.FullName);
        var sheet = workbook.Worksheets.Add("DriftSegments");

        // Write header row
        var headerRow = sheet.Row(1);
        headerRow.Cell(1).Value = "ID";
        headerRow.Cell(2).Value = "Start";
        headerRow.Cell(3).Value = "End";
        headerRow.Cell(4).Value = "Step";

        var n = driftLevels.Count;
        var k = 1;
        var step = 0.05;
        var totalRows = n * repeat * 4;
        // Use a single loop to avoid repeated index calculations and minimize Cell() calls
        for (var i = 0; i < n; i++)
        {
            var level = driftLevels[i];
            for (var j = 0; j < repeat; j++)
            {
                var baseRow = 2 + (i * repeat + j) * 4;

                // 1st row: 0 -> +level
                sheet.Cell(baseRow, 1).Value = k++;
                sheet.Cell(baseRow, 2).Value = 0.0;
                sheet.Cell(baseRow, 3).Value = level;
                sheet.Cell(baseRow, 4).Value = step;

                // 2nd row: +level -> 0
                sheet.Cell(baseRow + 1, 1).Value = k++;
                sheet.Cell(baseRow + 1, 2).Value = level;
                sheet.Cell(baseRow + 1, 3).Value = 0.0;
                sheet.Cell(baseRow + 1, 4).Value = step;

                // 3rd row: 0 -> -level
                sheet.Cell(baseRow + 2, 1).Value = k++;
                sheet.Cell(baseRow + 2, 2).Value = 0.0;
                sheet.Cell(baseRow + 2, 3).Value = -level;
                sheet.Cell(baseRow + 2, 4).Value = step;

                // 4th row: -level -> 0
                sheet.Cell(baseRow + 3, 1).Value = k++;
                sheet.Cell(baseRow + 3, 2).Value = -level;
                sheet.Cell(baseRow + 3, 3).Value = 0.0;
                sheet.Cell(baseRow + 3, 4).Value = step;
            }
        }

        workbook.SaveAs(file.FullName);
    }
}