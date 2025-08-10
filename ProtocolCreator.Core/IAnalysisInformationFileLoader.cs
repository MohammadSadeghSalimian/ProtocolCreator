namespace ProtocolCreator.Core;

public interface IAnalysisInformationFileLoader : IAnalysisInformationLoader
{
    public void Open(FileInfo path);
    public FileInfo? FilePath { get; }
    void Close();
}