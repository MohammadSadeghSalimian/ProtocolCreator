namespace ProtocolCreator.Core;

public class LineSegment(DriftSegment driftSegment,IReadOnlyList<DeData> delta)
{
    public DriftSegment DriftSegment { get; } = driftSegment;
    private readonly DeData[] _deltas = [..delta];

    public IReadOnlyList<DeData> Deltas =>_deltas; // List of deltas in the segment

    



}