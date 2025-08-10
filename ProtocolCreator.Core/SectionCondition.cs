namespace ProtocolCreator.Core;

public class SectionCondition(RebarCondition rebarCondition, double eccentricity, double depthCoefficient, int repeat,double k)
{
    public RebarCondition RebarCondition { get; } = rebarCondition;
    public double Eccentricity { get; } = eccentricity;

    public double DepthCoefficient { get; } = depthCoefficient;

    public double K { get; } = k; // Slope of the line in the elongation curve

    public int Repeat { get; } = repeat; // Number of repetitions of the drift. It means how many times the beam experienced this drift

}