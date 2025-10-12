namespace ProtocolCreator.Core;

public class CoefficientContainer(CoefficientDirectionContainer positive, CoefficientDirectionContainer negative)
{
   
    public CoefficientDirectionContainer Positive { get; } = positive;

    public CoefficientDirectionContainer Negative { get; } = negative;
}


public class CoefficientDirectionContainer(
    double plasticLoading,
    double elasticLoading,
    double plasticUnloading,
    double elasticUnloading)
{
    public double PlasticLoading { get;  } = plasticLoading;
    public double ElasticLoading { get;  } = elasticLoading;
    public double PlasticUnloading { get; } = plasticUnloading;
    public double ElasticUnloading { get; } = elasticUnloading;
}