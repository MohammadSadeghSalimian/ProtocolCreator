namespace ProtocolCreator.Core;


public class SectionCondition(
    bool experiencedYield,
    double depthCoefficient,
    int repeat,
    double slope,
    double ecr,
    DeltaCondition condition)
{
    public DeltaCondition Condition { get; } = condition;
    public bool ExperiencedYield { get; } = experiencedYield;
   
    public double DepthCoefficient { get; } = depthCoefficient;
    public int Repeat { get; } = repeat;
    public double Slope { get; } = slope; // Slope of the line in the elongation curve
    public double Eccentricity { get; } = ecr;
}