namespace ProtocolCreator.Core;

public class Delta(int id, DeltaDrift drift, DeltaElongation elongation, SectionCondition section)
{
    public int Id { get; } = id;
    public DeltaDrift Drift { get; } = drift;
    public DeltaElongation Elongation { get; } = elongation;
    public SectionCondition Section { get; } = section;
}
   