using System.Diagnostics;

namespace ProtocolCreator.Core;

[DebuggerDisplay("{Drift.Start}->{Drift.End}")]
public class Delta(int id, double cycle, DeltaDrift drift, DeltaElongation elongation, SectionCondition section, DriftSegment segment)
{
    public int Id { get; } = id;
    public double Cycle { get; } = cycle; // Cycle number, used to identify the drift in the protocol
    public DeltaDrift Drift { get; } = drift;
    public DeltaElongation Elongation { get; } = elongation;
    public SectionCondition Section { get; } = section;
    public DriftSegment Segment { get; } = segment;
}
