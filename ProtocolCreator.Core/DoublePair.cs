namespace ProtocolCreator.Core;

public readonly struct DoublePair(double a, double b) : IEquatable<DoublePair>
{
    public double A { get; } = Math.Round(a, 6);
    public double B { get; } = Math.Round(b, 6);

    public bool Equals(DoublePair other) =>
        Math.Abs(A - other.A) < 1e-7 && Math.Abs(B - other.B) < 1e-7;

    public override bool Equals(object? obj) =>
        obj is DoublePair other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(A, B);
    public static bool operator ==(DoublePair left, DoublePair right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DoublePair left, DoublePair right)
    {
        return !(left == right);
    }
}