using ClosedXML.Excel;
using ProtocolCreator.Core;

namespace ProtocolCreator.Infrastructures
{
    public class ExcelInputLoader : IDriftSegmentFileLoader, IAnalysisInformationFileLoader, IDisposable
    {
        private XLWorkbook? _workbook;

        public void Open(FileInfo path)
        {
            if (path is not { Exists: true })
            {
                throw new ArgumentException("File path is null or does not exist.", nameof(path));
            }

            FilePath = path;

            _workbook = new XLWorkbook(FilePath.FullName);
        }
        public IReadOnlyList<DriftSegment> LoadDriftSegments()
        {

            ArgumentNullException.ThrowIfNull(_workbook);
            var worksheet = _workbook.Worksheet("DriftSegments");
            var rows = worksheet.RowsUsed().Skip(1); // Skip header row
            var xlRows = rows as IXLRow[] ?? rows.ToArray();
            var driftSegments = new List<DriftSegment>(xlRows.Length);
            foreach (var row in xlRows)
            {
                // Read values from columns: ID (int), Start (double), End (double), Step (double)
                // ID is not used in DriftSegment constructor, so we just read and ignore it
                var id = row.Cell(1).GetValue<int>();
                var start = row.Cell(2).GetValue<double>();
                var end = row.Cell(3).GetValue<double>();
                var step = row.Cell(4).GetValue<double>();

                driftSegments.Add(new DriftSegment(start, end, step));
            }

            return driftSegments;
        }

        public FileInfo? FilePath { get; private set; }
        public AnalysisInformation LoadAnalysis()
        {
            ArgumentNullException.ThrowIfNull(_workbook);
            var worksheet = _workbook.Worksheet("Information");
            var rows = worksheet.RowsUsed().ToArray();
            var rebarYieldDrift = rows[1].Cell(2).GetValue<double>();
            var effectiveDepth = rows[2].Cell(2).GetValue<double>();
            var elasticPositive = rows[3].Cell(2).GetValue<double>();
            var elasticNegative = rows[4].Cell(2).GetValue<double>();
            var plasticPositive = rows[5].Cell(2).GetValue<double>();
            var plasticNegative = rows[6].Cell(2).GetValue<double>();

            var coefficients = new CoefficientContainer(elasticPositive, elasticNegative, plasticPositive, plasticNegative);
            var aa = new AnalysisInformation(rebarYieldDrift, effectiveDepth, coefficients);
            return aa;
        }

        public void Close()
        {
            _workbook?.Dispose();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }
    }
}
