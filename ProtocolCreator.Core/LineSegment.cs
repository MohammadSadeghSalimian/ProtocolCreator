namespace ProtocolCreator.Core;

public class LineSegment(DriftSegment driftSegment,IReadOnlyList<Delta> delta)
{
    public DriftSegment DriftSegment { get; } = driftSegment;
    private readonly Delta[] _deltas = [..delta];

    public IReadOnlyList<Delta> Deltas =>_deltas; // List of deltas in the segment

    



}