namespace ProtocolCreator.Core;

public class DriftSegment(double start, double end,double unsignedStep)
{
    public double Start { get; } = start;
    public double End { get; } = end;
    public double UnsignedStep { get; } = unsignedStep; // Step value for the drift segment which creates the delta drifts at each segment (it is unsigned because it can be positive or negative depending on the direction of the drift)
    public Direction Direction { get; } = (start + end) / 2.0 >= 0 ? Direction.Positive : Direction.Negative; // Direction of the segment based on average drift

    public LoadingPhase LoadingPhase { get; } = start < end ? LoadingPhase.Loading : LoadingPhase.Unloading; // Determine loading phase based on drift values
}