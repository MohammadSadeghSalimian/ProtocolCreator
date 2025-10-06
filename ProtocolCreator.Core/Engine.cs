using System.ComponentModel;

namespace ProtocolCreator.Core;
public class Engine(IReadOnlyList<DriftSegment> driftSegments, AnalysisInformation info)
{
    private readonly List<Delta> _allDeltas = [];
    public IReadOnlyList<Delta> Deltas => _allDeltas;
    public IReadOnlyList<DriftSegment> DriftSegments { get; } = driftSegments;
    private readonly Dictionary<DoublePair, int> _repeatCounter = new();
    private readonly List<LineSegment> _lines = new(driftSegments.Count);
    public IReadOnlyList<LineSegment> Lines => _lines;
    public AnalysisInformation Info { get; } = info;
    private int GetRepeat(double a, double b, bool isYielded,bool isInElastic)
    {
        var dd = new DoublePair(a, b);
        if (_repeatCounter.TryGetValue(dd, out var count))
        {
            if (!isYielded && isInElastic)
            {
                _repeatCounter[dd] = 1;
                return 1;
            }
            _repeatCounter[dd] = count + 1;
            return count + 1;
        }
        _repeatCounter[dd] = 1;
        return 1;
    }
    public void Calculate()
    {
        double peakPositive = 0;
        double peakNegative = 0;
        var dy = Info.RebarYieldDrift;
        var dEff = Info.EffectiveDepth;
      
        var co = Info.Coefficients;
        var isExperiencedYield = false;


        double currentElongation = 0;

        double cycle = 0;
        var id = 0;
        foreach (var item in DriftSegments)
        {
            var step = item.UnsignedStep;
            double[]? deltaValues;
            var newExperiencedDrift = false;
            var isInPlasticArea = false;
            bool isInResidualArea;
            double ecr = 0;
            double currentDrift = 0;
            double destinationDrift = 0;
            double deltaD = 0;
            double deltaE = 0;
            double slope = 0;
            var repeat = 0;
            double futureDrift = 0;
            double currentCoefficient = 0;
            double destinationElongation = 0;
            switch (item.CycleState)
            {
                case CycleState.PL:
                    deltaValues = MathExtension.Arrange(item.Start, item.End, step);
                    var n = deltaValues.Length;

                    var deltas = new Delta[n];
                    for (var i = 0; i < n; i++)
                    {
                        id += 1;
                        cycle += 0.25 / n;
                        var a = deltaValues[i];
                        var b = a + step;
                        deltaD = step;
                        if (a < dy && b > dy)
                        {
                            throw new ArgumentException(
                                $"The yield drift ({dy}) is placed between the steps. This is not allowed.");
                        }
                        if (a < peakPositive && b > peakPositive)
                        {
                            throw new ArgumentException(
                                $"The peak positive drift ({peakPositive}) is placed between the steps. This is not allowed.");
                        }
                        if (a >= dy && b > dy)
                        {
                            isExperiencedYield = true;
                        }
                        if (a < dy && b <= dy)
                        {
                            currentCoefficient = co.PositiveElastic;
                            isInPlasticArea = false;
                        }
                        else
                        {
                            currentCoefficient = co.PositivePlastic;
                            isInPlasticArea = true;
                        }
                        if (!isExperiencedYield)
                        {
                            repeat = GetRepeat(a, b, isExperiencedYield, !isInPlasticArea);
                            slope = Extensions.GetSlopeOfElongationLine(repeat);
                        }
                        else
                        {
                            repeat = GetRepeat(a, b, isExperiencedYield, !isInPlasticArea); // the number of repeats in cycles
                            slope = Extensions.GetSlopeOfElongationLine(repeat);
                        }
                        if (a >= peakPositive && b > peakPositive)
                        {
                            newExperiencedDrift = true;
                            peakPositive = b;
                        }
                        else
                        {
                            newExperiencedDrift = false;
                        }
                        ecr = slope * currentCoefficient * dEff; // eccentricity coefficient
                        deltaE = ecr * deltaD/100;
                        currentDrift = a;
                        destinationDrift = b;
                        destinationElongation = currentElongation + deltaE;
                        var condition = Extensions.GetConditionInLoading(isExperiencedYield, newExperiencedDrift, isInPlasticArea);
                        var dd = new DeltaDrift(currentDrift, destinationDrift);
                        var ee = new DeltaElongation(currentElongation, destinationElongation);
                        var sec = new SectionCondition(isExperiencedYield,
                            currentCoefficient, repeat, slope, ecr, condition);
                        deltas[i] = new Delta(id, cycle, dd, ee, sec, item);
                        currentElongation = destinationElongation;
                    }
                    _allDeltas.AddRange(deltas);
                    break;
                case CycleState.PU:
                   
                    deltaValues = MathExtension.Arrange(item.Start, item.End, -step);
                    futureDrift = item.Start - dy;
                    n = deltaValues.Length; deltas = new Delta[n];
                    for (var i = 0; i < n; i++)
                    {
                        id += 1;
                        cycle += 0.25 / n;
                        var a = deltaValues[i];
                        deltaD = -step;
                        var b = a - step;
                        if (b < futureDrift && a > futureDrift)
                        {
                            throw new ArgumentException(
                                $"The future drift ({futureDrift}) is placed between the steps. This is not allowed.");
                        }

                        currentCoefficient = co.PositiveElastic;
                        if (b >= futureDrift)
                        {
                            slope = 1;
                            repeat = 0;
                            isInResidualArea = true;
                        }
                        else
                        {
                            slope = 0;
                            repeat = 0;
                            isInResidualArea = false;
                        }
                        ecr = slope * currentCoefficient * dEff; // eccentricity coefficient
                        deltaE = ecr * deltaD/100;
                        currentDrift = a;
                        destinationDrift = b;
                        destinationElongation = currentElongation + deltaE;
                        var condition = Extensions.GetDeltaConditionInUnLoading(isInResidualArea);
                        var dd = new DeltaDrift(currentDrift, destinationDrift);
                        var ee = new DeltaElongation(currentElongation, destinationElongation);
                        var sec = new SectionCondition(isExperiencedYield,
                            currentCoefficient, repeat, slope, ecr, condition);
                        deltas[i] = new Delta(id, cycle, dd, ee, sec, item);
                        currentElongation = destinationElongation;
                    }
                    _allDeltas.AddRange(deltas);
                    break;
                case CycleState.NL:
                    deltaValues = MathExtension.Arrange(item.Start, item.End, -step);
                     n = deltaValues.Length;
                    deltas = new Delta[n];
                    for (var i = 0; i < n; i++)
                    {
                        id += 1;
                        cycle += 0.25 / n;
                        var a = deltaValues[i];
                        var b = a - step;
                        deltaD = -step;
                        if (b < -dy && a > -dy)
                        {
                            throw new ArgumentException(
                                $"The yield drift ({-dy}) is placed between the steps. This is not allowed.");
                        }
                        if (a > peakNegative && b < peakNegative)
                        {
                            throw new ArgumentException(
                                $"The peak negative drift ({peakNegative}) is placed between the steps. This is not allowed.");
                        }
                        if (a <= -dy && b < -dy)
                        {
                            isExperiencedYield = true;
                        }
                        if (a > -dy && b>= -dy)
                        {
                            currentCoefficient = co.NegativeElastic;
                            isInPlasticArea = false;
                        }
                        else
                        {
                            currentCoefficient = co.NegativePlastic;
                            isInPlasticArea = true;
                        }
                        if (!isExperiencedYield) // it can change the behavior
                        {
                            repeat = GetRepeat(a, b, isExperiencedYield, !isInPlasticArea);
                            slope = Extensions.GetSlopeOfElongationLine(repeat);
                        }
                        else
                        {
                            repeat = GetRepeat(a, b, isExperiencedYield, !isInPlasticArea); // the number of repeats in cycles
                            slope = Extensions.GetSlopeOfElongationLine(repeat);
                        }
                        if (a <= peakNegative && b < peakNegative)
                        {
                            newExperiencedDrift = true;
                            peakNegative = b;
                        }
                        else
                        {
                            newExperiencedDrift = false;
                        }
                        ecr = slope * currentCoefficient * dEff; // eccentricity coefficient
                        deltaE = -ecr * deltaD/100.0;
                        currentDrift = a;
                        destinationDrift = b;
                        destinationElongation = currentElongation + deltaE;
                        var condition = Extensions.GetConditionInLoading(isExperiencedYield, newExperiencedDrift, isInPlasticArea);
                        var dd = new DeltaDrift(currentDrift, destinationDrift);
                        var ee = new DeltaElongation(currentElongation, destinationElongation);
                        var sec = new SectionCondition(isExperiencedYield,
                            currentCoefficient, repeat, slope, ecr, condition);
                        deltas[i] = new Delta(id, cycle, dd, ee, sec, item);
                        currentElongation = destinationElongation;
                    }
                    _allDeltas.AddRange(deltas);
                    break;
                case CycleState.NU:
                    deltaValues = MathExtension.Arrange(item.Start, item.End, step);
                    futureDrift =item.Start + dy;
                    n = deltaValues.Length; deltas = new Delta[n];
                    for (var i = 0; i < n; i++)
                    {
                        id += 1;
                        cycle += 0.25 / n;
                        var a = deltaValues[i];
                        deltaD = step;
                        var b = a + step;
                        if (b > futureDrift && a < futureDrift)
                        {
                            throw new ArgumentException(
                                $"The future drift ({futureDrift}) is placed between the steps. This is not allowed.");
                        }

                        currentCoefficient = co.NegativeElastic;
                        if (b <= futureDrift)
                        {
                            slope = 1;
                            repeat = 0;
                            isInResidualArea = true;
                        }
                        else
                        {
                            slope = 0;
                            repeat = 0;
                            isInResidualArea = false;
                        }
                        ecr = slope * currentCoefficient * dEff; // eccentricity coefficient
                        deltaE = -ecr * deltaD / 100;
                        currentDrift = a;
                        destinationDrift = b;
                        destinationElongation = currentElongation + deltaE;
                        var condition = Extensions.GetDeltaConditionInUnLoading(isInResidualArea);
                        var dd = new DeltaDrift(currentDrift, destinationDrift);
                        var ee = new DeltaElongation(currentElongation, destinationElongation);
                        var sec = new SectionCondition(isExperiencedYield,
                            currentCoefficient, repeat, slope, ecr, condition);
                        deltas[i] = new Delta(id, cycle, dd, ee, sec, item);
                        currentElongation = destinationElongation;
                    }
                    _allDeltas.AddRange(deltas);
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
          
        }
    }
}