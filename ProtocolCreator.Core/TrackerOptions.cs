namespace ProtocolCreator.Core;

public sealed class TrackerOptions
{
    public double PositiveStation { get; init; } = double.PositiveInfinity;
    public double NegativeStation { get; init; } = double.NegativeInfinity;
    public YieldMode Mode { get; init; } = YieldMode.EitherSide;
}