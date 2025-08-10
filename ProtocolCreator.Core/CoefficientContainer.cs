namespace ProtocolCreator.Core;

public class CoefficientContainer(
    double positiveElastic,
    double negativeElastic,
    double positivePlastic,
    double negativePlastic)
{
    public double PositivePlastic { get;  } = positivePlastic;
    public double NegativePlastic { get; } = negativePlastic;
    public double PositiveElastic { get; } = positiveElastic;
    public double NegativeElastic { get; } = negativeElastic;

        
        
}