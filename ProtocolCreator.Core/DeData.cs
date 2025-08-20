namespace ProtocolCreator.Core;

public class DeData(int id, DeltaDrift drift, DeltaElongation elongation, SectionCondition section,DriftSegment driftSegment)
{
    public int Id { get; } = id;
    public DeltaDrift Drift { get; } = drift;
    public DeltaElongation Elongation { get; } = elongation;
    public SectionCondition Section { get; } = section;
    public DriftSegment DriftSegment => driftSegment;
}
   