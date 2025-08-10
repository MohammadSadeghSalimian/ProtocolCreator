using System.Diagnostics;

namespace ProtocolCreator.Core;

[DebuggerDisplay("E {Start}-->{End}")]
public class DeltaElongation(double start, double end, Direction direction, LoadingPhase loading)
{
    public Direction Direction { get; } = direction; // Direction of the elongation, positive or negative
    public LoadingPhase Loading { get; } = loading; // Loading or unloading phase

    public double Start { get; } = start; // Start elongation value
    public double End { get; } = end; // B elongation value
    public double Center { get; } = (start + end) / 2.0; // Center elongation value

}