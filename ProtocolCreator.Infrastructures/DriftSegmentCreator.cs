using ClosedXML.Excel;
using ProtocolCreator.Core;

namespace ProtocolCreator.Infrastructures;

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