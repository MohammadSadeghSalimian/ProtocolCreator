namespace ProtocolCreator.Core;

public interface IResultSaver
{
    void Save(FileInfo file, IReadOnlyList<Delta> deltas);
}

public interface IDriftSegmentCreator
{
    void Create(FileInfo file, int repeat, IReadOnlyList<double> driftLevels);
}