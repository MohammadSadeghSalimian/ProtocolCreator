namespace ProtocolCreator.Core;

public class DriftSegment(double start, double end, double unsignedStep)
{
    public double Start { get; } = start;
    public double End { get; } = end;
    public double UnsignedStep { get; } = unsignedStep; // Step value for the drift segment which creates the delta drifts at each segment (it is unsigned because it can be positive or negative depending on the direction of the drift)
    public CycleState CycleState { get; } = Calculate(start, end);


    private static CycleState Calculate(double start, double end)
    {
        if (end >= start && Math.Abs(start) <= Math.Abs(end))
        {
            return CycleState.PositiveLoading;
        }

        if (end < start && Math.Abs(start) <= Math.Abs(end))
        {
            return CycleState.NegativeLoading;
        }

        if (end < start && Math.Abs(start) > Math.Abs(end))
        {
            return CycleState.PositiveUnloading;
        }

        if (end >= start && Math.Abs(start) > Math.Abs(end))
        {
            return CycleState.NegativeUnloading;
        }
        throw new ArgumentException("Invalid drift segment state. Cannot determine cycle state.");
    }
}