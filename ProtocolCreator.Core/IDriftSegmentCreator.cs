namespace ProtocolCreator.Core;

public interface IDriftSegmentCreator
{
    void Create(FileInfo file, int repeat, IReadOnlyList<double> driftLevels);
}