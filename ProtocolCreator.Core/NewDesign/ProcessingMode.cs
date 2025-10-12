namespace ProtocolCreator.Core.NewDesign;

public readonly record struct ProcessingMode(Direction Direction, Phase Phase)
{
    public bool IsPositive => Direction == Direction.Positive;
    public bool IsLoading => Phase == Phase.Loading;
}