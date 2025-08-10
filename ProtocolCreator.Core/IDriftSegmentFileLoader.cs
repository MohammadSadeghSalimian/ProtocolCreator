namespace ProtocolCreator.Core;

public interface IDriftSegmentFileLoader : IDriftSegmentLoader
{
    public void Open(FileInfo path);
    public FileInfo? FilePath { get; }
    void Close();
}