namespace ProtocolCreator.Core;

public interface IEngine
{
    public IReadOnlyList<Delta> Deltas { get; }
    public IReadOnlyList<DriftSegment> DriftSegments { get; }
    public IReadOnlyList<LineSegment> Lines { get; }
    public AnalysisInformation Info { get; }
    public void Calculate();
}