namespace ProtocolCreator.Core;

public interface IDriftSegmentLoader
{
    public IReadOnlyList<DriftSegment> LoadDriftSegments();
}