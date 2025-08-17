namespace ProtocolCreator.Core;


using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a traffic counter for traversals over intervals.
/// Tracks how many times each interval is traversed and provides segment counts.
/// </summary>
public class IntervalTraffic
{
    /// <summary>
    /// Stores the difference array for interval traversal counts.
    /// Key: position, Value: delta count at that position.
    /// </summary>
    private readonly SortedDictionary<double, int> _diff = new();

    /// <summary>
    /// Adds a traversal from point <paramref name="a"/> to <paramref name="b"/>.
    /// Increments the count for the interval [min(a, b), max(a, b)).
    /// </summary>
    /// <param name="a">Start point of traversal.</param>
    /// <param name="b">End point of traversal.</param>
    public void AddTraversal(double a, double b)
    {
        if (Math.Abs(a - b) < 1e-6) return;
        var lo = Math.Min(a, b);
        var hi = Math.Max(a, b);
        AddToDiff(lo, +1);
        AddToDiff(hi, -1);
    }

    /// <summary>
    /// Updates the difference array at the specified <paramref name="key"/> by <paramref name="delta"/>.
    /// Removes the entry if the resulting value is zero.
    /// </summary>
    /// <param name="key">The position to update.</param>
    /// <param name="delta">The change in count.</param>
    private void AddToDiff(double key, int delta)
    {
        if (_diff.TryGetValue(key, out var cur))
            _diff[key] = cur + delta;
        else
            _diff[key] = delta;

        if (_diff[key] == 0) _diff.Remove(key);
    }

    /// <summary>
    /// Enumerates the segments with their start, end, and traversal count.
    /// </summary>
    /// <returns>
    /// A sequence of tuples containing the start, end, and count for each segment.
    /// </returns>
    public IEnumerable<(double Start, double End, int Count)> GetSegments()
    {
        if (_diff.Count == 0) yield break;

        var run = 0;
        double? prev = null;

        foreach (var kv in _diff)
        {
            var x = kv.Key;
            if (prev.HasValue && run != 0 && Math.Abs(x - prev.Value) > 1e-6)
                yield return (prev.Value, x, run);

            run += kv.Value;
            prev = x;
        }
    }

    /// <summary>
    /// Returns a string representation of the segments and their counts.
    /// </summary>
    /// <returns>
    /// A comma-separated list of segments in the format: [start..end):count
    /// </returns>
    public override string ToString()
        => string.Join(", ", GetSegments().Select(s => $"[{s.Start}..{s.End}):{s.Count}"));
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

            for (var i = 0; i < n; i++)
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

  
    public void Calculate2()
    {
        var travelCounter= new IntervalTraffic();
        double peakPositive = 0;
        double peakNegative = 0;
        var dy = Info.RebarYieldDrift;
        var dEff = Info.EffectiveDepth;
        bool experiencedYield=false;
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

                        if (currentDrift < dy && peakPositive < dy && driftDestination<=dy)
                        {
                            driftDestination = item.End;
                            var deltaD = driftDestination - currentDrift;
                            deltaE = deltaD * dEff * co.PositiveElastic;
                            break;
                        }
                        if (currentDrift < dy && peakPositive < dy && driftDestination > dy)
                        {
                            driftDestination = dy;
                            var deltaD = driftDestination - currentDrift;
                            deltaE = deltaD * dEff * co.PositiveElastic;
                            break;
                        }
                        else
                        {
                            throw new InvalidOperationException("The parameters are not in range");
                        }
                       
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

            for (var i = 0; i < n; i++)
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