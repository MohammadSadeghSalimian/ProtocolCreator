namespace ProtocolCreator.Core.NewDesign;

public sealed class DefaultCoefficientProvider(CoefficientContainer co) : ICoefficientProvider
{
    public double GetElastic(Direction dir) => dir == Direction.Positive ? co.Positive.ElasticLoading : co.Negative.ElasticLoading;
    public double GetPlastic(Direction dir) => dir == Direction.Positive ? co.Positive.PlasticLoading : co.Negative.PlasticLoading;

    public double GetUnloadingPlastic(Direction dir) =>
        dir == Direction.Positive ? co.Positive.PlasticUnloading : co.Negative.PlasticUnloading;

}