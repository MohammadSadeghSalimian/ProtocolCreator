namespace ProtocolCreator.Core;

public class Engine(IReadOnlyList<DriftSegment> driftSegments, AnalysisInformation info)
{
    private readonly List<Delta> _allDeltas = [];
    public IReadOnlyList<Delta> Deltas => _allDeltas;
    public IReadOnlyList<DriftSegment> DriftSegments { get; } = driftSegments;
    private readonly Dictionary<DoublePair, int> _repeatCounter = new();
    private readonly List<LineSegment> _lines = new List<LineSegment>(driftSegments.Count);
    public IReadOnlyList<LineSegment> Lines => _lines;

    public AnalysisInformation Info { get; } = info;

    private int GetRepeat(double a, double b)
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
            var deltaValues = MathExtension.Arrange(item.Start, item.End, step);
            var n = deltaValues.Length;
            var deltas = new Delta[n];
            (positive, negative) = item.GetPeaks(positive, negative);
            
            for (int i = 0; i < n; i++)
            {
                var a = deltaValues[i];
                var b = a + step;
                var deltaDrift = new DeltaDrift(a, b);
                var dir = deltaDrift.GetDirection();
                var rebarCondition = deltaDrift.GetRebarCondition(dy);
                var depC = Info.Coefficients.GetCurrentDepthCoefficient(dir, rebarCondition);
                var repeat = GetRepeat(a, b);
                var slope = Extensions.GetSlopeOfElongationLine(repeat);
                var residual = (dir == Direction.Positive) ? residualElongations.Positive : residualElongations.Negative; //Coeff*D*dy
                double deltaE = 0;
                double startE = 0;
                double endE = 0;
                double eccentricity = 0;
                if (item.LoadingPhase == LoadingPhase.Loading)
                {
                     eccentricity = Extensions.GetEccentricity(dEff, depC);
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
                        eccentricity = Extensions.GetEccentricity(dEff, depC);
                    }
                    else
                    {
                        deltaE = 0;
                        endE = destination;
                        eccentricity = ecrElastic;
                    }
                }
                var sec = new SectionCondition(rebarCondition, eccentricity, depC, repeat, slope);
                currentElongation += deltaE;
                var elongation = new DeltaElongation(startE,endE,dir,item.LoadingPhase);
                var delta = new Delta(i, deltaDrift, elongation, sec);
                deltas[i]=delta;
                this._allDeltas.Add(delta);
            }
            var ls = new LineSegment(item, deltas);
            _lines.Add(ls);

        }
    }

}