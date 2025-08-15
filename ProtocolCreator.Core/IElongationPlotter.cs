namespace ProtocolCreator.Core;

public interface IElongationPlotter
{
    void DriftElongationPlotter(FileInfo file, IReadOnlyList<double> drift, IReadOnlyList<double> elongation, int with = 1920, int height = 1080);
    void CycleElongationPlotter(FileInfo file, IReadOnlyList<double> cycle, IReadOnlyList<double> elongation, int with = 1920, int height = 1080);
}