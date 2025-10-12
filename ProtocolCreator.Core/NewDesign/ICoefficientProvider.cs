namespace ProtocolCreator.Core.NewDesign;

public interface ICoefficientProvider
{
    double GetElastic(Direction dir);
    double GetPlastic(Direction dir);
    double GetUnloadingPlastic(Direction dir);
}