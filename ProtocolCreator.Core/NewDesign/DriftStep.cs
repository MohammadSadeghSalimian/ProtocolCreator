namespace ProtocolCreator.Core.NewDesign;

public readonly record struct DriftStep(double A, double B)
{
    public double Delta => B - A;
}