namespace ProtocolCreator.Core;

public interface IResultSaver
{
    void Save(FileInfo file, IReadOnlyList<Delta> deltas);
}