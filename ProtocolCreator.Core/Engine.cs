namespace ProtocolCreator.Core;


public class DestinationFinder
{
    private double[] _drifts = new double[4];

    public double Dy { get;private set; }

    public double Peak { get; private set; }

    public bool IsYielded { get; private set; }

    public void MakeItYield()
    {
        IsYielded= true;
    }

    private double GetDestination(double current, double destination)
    {
        for (int i = 0; i < 4; i++)
        {
            if (current >= _drifts[i] && destination <= _drifts[i])
            {
                
            }
        }
    }

    public double GetDestination(double absoluteCurrentValue, double proposedDestination, CycleState cycleState,out double slope)
    {
        if (proposedDestination<=Dy && !IsYielded)
        {
            slope = 1;
            return proposedDestination;
        }
        if (proposedDestination > Dy)
        {
            if (absoluteCurrentValue<Dy)
            {
                return Dy;
            }
        }
    }
}


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
                var dir = deltaDrift.GetDirection(); // positive or negative
                var rebarCondition = deltaDrift.GetRebarCondition(dy); // elastic or plastic
                var depC = Info.Coefficients.GetCurrentDepthCoefficient(dir, rebarCondition); // elastic or plastic
                var repeat = GetRepeat(a, b); // the number of repeats in cycles
                var slope = Extensions.GetSlopeOfElongationLine(repeat); //based on the repeat number
                var residual = (dir == Direction.Positive) ? residualElongations.Positive : residualElongations.Negative; //Coeff*D*dy
                double deltaE = 0;
                double startE = 0;
                double endE = 0;
                double eccentricity = 0;
                if (item.LoadingPhase == LoadingPhase.Loading)
                {
                    eccentricity = dEff * depC;
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
                var elongation = new DeltaElongation(startE, endE, dir, item.LoadingPhase);
                var delta = new Delta(i, deltaDrift, elongation, sec);
                deltas[i] = delta;
                this._allDeltas.Add(delta);
            }
            var ls = new LineSegment(item, deltas);
            _lines.Add(ls);

        }
    }

    public (double destination, double k) GetDestination()
    {

    }
    public void Calculate2()
    {
        double peakPositive = 0;
        double peakNegative = 0;
        double dy = Info.RebarYieldDrift;
        double dEff = Info.EffectiveDepth;

        double currentElongation = 0;
        double currentDrift = 0;

        var co = Info.Coefficients;

        foreach (var item in DriftSegments)
        {


            while (Math.Abs(currentDrift - item.End) < 1e-6)
            {

                var driftDestination = item.End;
                var deltaE = 0.0;
                switch (item.CycleState)
                {
                    case CycleState.PositiveLoading:

                        if (currentDrift < dy && peakPositive < dy && driftDestination<dy)
                        {
                            driftDestination = item.End;
                            var deltaD = driftDestination - currentDrift;
                            deltaE = deltaD * dEff * co.PositiveElastic;
                        }
                        else
                        {
                            throw new InvalidOperationException("The parameters are not in range")
                        }
                        break;
                    case CycleState.PositiveUnloading:
                        break;
                    case CycleState.NegativeLoading:
                        break;
                    case CycleState.NegativeUnloading:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var deltaDrift = new DeltaDrift(currentDrift, driftDestination);
                var deltaElongation = new DeltaElongation(currentElongation, currentElongation + deltaE);
                currentDrift=driftDestination;
                currentElongation += deltaE;
            }


            var step = item.GetSignedStep();
            var deltaValues = MathExtension.Arrange(item.Start, item.End, step);
            var n = deltaValues.Length;
            var deltas = new Delta[n];
            (peakPositive, peakNegative) = item.GetPeaks(peakPositive, peakNegative);

            for (int i = 0; i < n; i++)
            {
                var a = deltaValues[i];
                var b = a + step;
                var deltaDrift = new DeltaDrift(a, b);
                var dir = deltaDrift.GetDirection(); // positive or negative
                var rebarCondition = deltaDrift.GetRebarCondition(dy); // elastic or plastic
                var depC = Info.Coefficients.GetCurrentDepthCoefficient(dir, rebarCondition); // elastic or plastic
                var repeat = GetRepeat(a, b); // the number of repeats in cycles
                var slope = Extensions.GetSlopeOfElongationLine(repeat); //based on the repeat number
                var residual = (dir == Direction.Positive) ? residualElongations.Positive : residualElongations.Negative; //Coeff*D*dy
                double deltaE = 0;
                double startE = 0;
                double endE = 0;
                double eccentricity = 0;
                if (item.LoadingPhase == LoadingPhase.Loading)
                {
                    eccentricity = dEff * depC;
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
                var elongation = new DeltaElongation(startE, endE, dir, item.LoadingPhase);
                var delta = new Delta(i, deltaDrift, elongation, sec);
                deltas[i] = delta;
                this._allDeltas.Add(delta);
            }
            var ls = new LineSegment(item, deltas);
            _lines.Add(ls);

        }
    }
}