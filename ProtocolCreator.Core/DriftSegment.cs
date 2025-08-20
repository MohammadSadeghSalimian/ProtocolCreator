using System.Diagnostics;

namespace ProtocolCreator.Core;

[DebuggerDisplay("{Start}-->{End}::{CycleState}")]
public class DriftSegment(double cycle,double start, double end)
{
    public double Cycle { get; } = cycle;
    public double Start { get; } = start;
    public double End { get; } = end;
    public CycleState CycleState { get; } = Calculate(start, end);

    private static CycleState Calculate(double start, double end)
    {
        if (end >= start && Math.Abs(start) <= Math.Abs(end))
        {
            return CycleState.PL;
        }

        if (end < start && Math.Abs(start) <= Math.Abs(end))
        {
            return CycleState.NL;
        }

        if (end < start && Math.Abs(start) > Math.Abs(end))
        {
            return CycleState.PU;
        }

        if (end >= start && Math.Abs(start) > Math.Abs(end))
        {
            return CycleState.NU;
        }
        throw new ArgumentException("Invalid drift segment state. Cannot determine cycle state.");
    }
}