namespace ProtocolCreator.Core.NewDesign;

public sealed class DoublePairComparer(double eps = 1e-9) : IEqualityComparer<(double A, double B)>
{
    public bool Equals((double A, double B) x, (double A, double B) y)
        => Math.Abs(x.A - y.A) <= eps && Math.Abs(x.B - y.B) <= eps;

    public int GetHashCode((double A, double B) obj)
        => HashCode.Combine(Math.Round(obj.A / eps), Math.Round(obj.B / eps));
}