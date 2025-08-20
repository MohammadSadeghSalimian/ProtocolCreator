using System.Diagnostics;

namespace ProtocolCreator.Core;

[DebuggerDisplay("D {Start}-->{End}")]
public class DeltaDrift(double start, double end)
{
    public double Start { get; } = start; // Start drift value

    public double End { get; } = end; // B drift value
    public double Delta { get; } = end - start;

}