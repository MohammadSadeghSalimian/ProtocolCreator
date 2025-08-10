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
public class Engine(IReadOnlyList<DriftSegment> driftSegments, AnalysisInformation info)
{
    private readonly List<Delta> _deltas = [];
    public IReadOnlyList<Delta> Deltas => _deltas;
    public IReadOnlyList<DriftSegment> DriftSegments { get; } = driftSegments;
    private Dictionary<DoublePair, int> _repeatCounter = new();



    public AnalysisInformation Info { get; } = info;

    private int GetRepat(double a, double b)
    {
        var dd = new DoublePair(a, b);
        if (_repeatCounter.TryGetValue(dd, out var count))
        {
            _repeatCounter[dd] = count + 1;
            return count + 1;
        }

        _repeatCounter[dd] = 1;
        return 1;
    }
    public void Calculate()
    {
        double positive = 0;
        double negative = 0;
        var dy = Info.RebarYieldDrift;
        var dEff = Info.EffectiveDepth;
        var residualElongations = Info.Coefficients.GetResidualElongation(dEff, dy);
        var currentElongation = 0.0;
        var co = Info.Coefficients;
        foreach (var item in DriftSegments)
        {
            var step = item.GetSignedStep();
            var deltaValues = MathExtension.Arrange(item.Start, item.End, item.UnsignedStep);
            var n = deltaValues.Length;
            var deltas = new Delta[n];
            (positive, negative) = item.GetPeaks(positive, negative);
            var elongationA = currentElongation;

            for (int i = 0; i < n; i++)
            {
                var a = deltaValues[i];
                var b = a + step;
                var deltaDrift = new DeltaDrift(a, b);
                var dir = deltaDrift.GetDirection();
                var rebarCondition = deltaDrift.GetRebarCondition(dy);
                var depC = Info.Coefficients.GetCurrentDepthCoefficient(dir, rebarCondition);
                var repeat = GetRepat(a, b);
                var eccentricity = Extensions.GetEccentricity(dEff, depC);
                var slope = Extensions.GetSlopeOfElongationLine(repeat);
                var sec = new SectionCondition(rebarCondition, eccentricity, depC, repeat, slope);

                var residual = (dir == Direction.Positive) ? residualElongations.Positive : residualElongations.Negative; //Coeff*D*dy
                double deltaE = 0;
                double startE = 0;
                double endE = 0;
                if (item.LoadingPhase == LoadingPhase.Loading)
                {
                    deltaE = slope * eccentricity * Math.Abs(step);
                    startE = currentElongation;
                    endE = startE + deltaE;
                }
                else
                {
                    startE = currentElongation;
                    var destination = currentElongation - residual;
                    var cc = (dir == Direction.Positive) ? co.PositiveElastic : co.NegativeElastic;
                    var ecrElastic = cc * dEff;
                    deltaE = -slope * ecrElastic * Math.Abs(step);
                    endE = startE + deltaE;
                    if (endE >= destination || Math.Abs(endE - destination) < 1e-6)
                    {

                    }
                    else
                    {
                        deltaE = 0;
                        endE = destination;
                    }




                }
                currentElongation += deltaE;
                var elongation = new DeltaElongation();

                deltas[i] = new Delta(i, deltaDrift, elongation, sec);



            }
            var ls = new LineSegment(item, deltas);

            var lineSegment = new LineSegment(item);

        }
    }

}