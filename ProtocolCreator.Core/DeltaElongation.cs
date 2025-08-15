using System.Diagnostics;

namespace ProtocolCreator.Core;

[DebuggerDisplay("E {Start}-->{End}")]
public class DeltaElongation(double start, double end)
{
   

    public double Start { get; } = start; // Start elongation value
    public double End { get; } = end; // B elongation value
    public double Center { get; } = (start + end) / 2.0; // Center elongation value

}