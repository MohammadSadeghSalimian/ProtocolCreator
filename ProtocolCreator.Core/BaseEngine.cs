namespace ProtocolCreator.Core;

public abstract class BaseEngine(IReadOnlyList<DriftSegment> driftSegments, AnalysisInformation info):IEngine
{
    protected readonly List<Delta> AllDeltas = [];
    public IReadOnlyList<Delta> Deltas => AllDeltas;
    public IReadOnlyList<DriftSegment> DriftSegments { get; } = driftSegments;
    protected readonly Dictionary<DoublePair, int> RepeatCounter = new();
    protected readonly List<LineSegment> LineList = new(driftSegments.Count);
    public IReadOnlyList<LineSegment> Lines => LineList;
    public AnalysisInformation Info { get; } = info;

    public abstract void Calculate();


}