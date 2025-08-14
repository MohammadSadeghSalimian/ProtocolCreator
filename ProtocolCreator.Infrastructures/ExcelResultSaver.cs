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