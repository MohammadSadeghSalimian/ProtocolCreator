namespace ProtocolCreator.Core;

public class AnalysisInformation(
    double rebarYieldDrift,
    double effectiveDepth,
    CoefficientContainer coefficients)
{
    public double RebarYieldDrift { get; } = rebarYieldDrift;

    public double EffectiveDepth { get; } = effectiveDepth;

    public CoefficientContainer Coefficients { get; } = coefficients;
   

  
}