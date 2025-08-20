using System;
using System.Collections.Generic;
using System.Linq;
namespace ProtocolCreator.Core;

public class Engine(IReadOnlyList<DriftSegment> driftSegments, AnalysisInformation info)
{
    private readonly List<DeData> _allDeltas = [];
    public IReadOnlyList<DeData> Deltas => _allDeltas;
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



    //public void Calculate()
    //{
    //    double positive = 0;
    //    double negative = 0;
    //    var dy = Info.RebarYieldDrift;
    //    var dEff = Info.EffectiveDepth;
    //    var residualElongations = Info.Coefficients.GetResidualElongation(dEff, dy);
    //    var currentElongation = 0.0;
    //    var co = Info.Coefficients;
    //    foreach (var item in DriftSegments)
    //    {
    //        var step = item.GetSignedStep();
    //        var deltaValues = MathExtension.Arrange(item.Start, item.End, step);
    //        var n = deltaValues.Delta;
    //        var deltas = new Delta[n];
    //        (positive, negative) = item.GetPeaks(positive, negative);
    //        for (var i = 0; i < n; i++)
    //        {
    //            var a = deltaValues[i];
    //            var b = a + step;
    //            var deltaDrift = new DeltaDrift(a, b);
    //            var dir = deltaDrift.GetDirection(); // positive or negative
    //            var rebarCondition = deltaDrift.GetRebarCondition(dy); // elastic or plastic
    //            var depC = Info.Coefficients.GetCurrentDepthCoefficient(dir, rebarCondition); // elastic or plastic
    //            var repeat = GetRepeat(a, b); // the number of repeats in cycles
    //            var slope = Extensions.GetSlopeOfElongationLine(repeat); //based on the repeat number
    //            var residual = (dir == Direction.Positive) ? residualElongations.Positive : residualElongations.Negative; //Coeff*D*dy
    //            double deltaE = 0;
    //            double startE = 0;
    //            double endE = 0;
    //            double eccentricity = 0;
    //            if (item.LoadingPhase == LoadingPhase.Loading)
    //            {
    //                eccentricity = dEff * depC;
    //                deltaE = slope * eccentricity * Math.Abs(step);
    //                startE = currentElongation;
    //                endE = startE + deltaE;
    //            }
    //            else
    //            {
    //                startE = currentElongation;
    //                var destination = currentElongation - residual;
    //                var cc = (dir == Direction.Positive) ? co.PositiveElastic : co.NegativeElastic;
    //                var ecrElastic = cc * dEff;
    //                deltaE = -slope * ecrElastic * Math.Abs(step);
    //                endE = startE + deltaE;
    //                if (endE >= destination || Math.Abs(endE - destination) < 1e-6)
    //                {
    //                    eccentricity = Extensions.GetEccentricity(dEff, depC);
    //                }
    //                else
    //                {
    //                    deltaE = 0;
    //                    endE = destination;
    //                    eccentricity = ecrElastic;
    //                }
    //            }
    //            var sec = new SectionCondition(rebarCondition, eccentricity, depC, repeat, slope);
    //            currentElongation += deltaE;
    //            var elongation = new DeltaElongation(startE, endE, dir, item.LoadingPhase);
    //            var delta = new Delta(i, deltaDrift, elongation, sec);
    //            deltas[i] = delta;
    //            this._allDeltas.Add(delta);
    //        }
    //        var ls = new LineSegment(item, deltas);
    //        _lines.Add(ls);
    //    }
    //}
    public void Calculate()
    {
        _allDeltas.Clear();
       

        var dy = Info.RebarYieldDrift;
        var dEff = Info.EffectiveDepth;
        bool experiencedYield = false;
        double currentE = 0;
        var trackerOption = new TrackerOptions
        {
         
            Mode = YieldMode.EitherSide,
            NegativeStation = 0.5,
            PositiveStation = 0.5
        };
        var travelCounter = new RangeCoefficientTracker(trackerOption);

        var co = Info.Coefficients;
        var list = new List<DeData>();
        int index = 0;
        foreach (var item in DriftSegments)
        {
            switch (item.CycleState)
            {
                case CycleState.PL:
                    
                    var travels = travelCounter.AddTravel(item.Start, item.End);
                    foreach (var travelPiece in travels)
                    {
                        var depthCoeff = (Math.Abs(travelPiece.End) <= dy) ? co.PositiveElastic : co.PositivePlastic;
                        var ecr = travelPiece.Coefficient * depthCoeff * dEff;
                        var df = (travelPiece.End - travelPiece.Start);
                        var drift = new DeltaDrift(travelPiece.Start, travelPiece.End);
                        var de = df * ecr/100;
                        var elongation = new DeltaElongation(currentE, currentE + de);
                        currentE += de;
                        var section = new SectionCondition(RebarCondition.Yield, travelPiece.IsYield, ecr, depthCoeff, travelPiece.RepeatCount, travelPiece.Coefficient);
                        var delta = new DeData(++index, drift, elongation, section, item);
                        list.Add(delta);
                        if (travelPiece.IsYield)
                        {
                            experiencedYield=true;
                        }
                        break;
                    }
                    break;
                case CycleState.PU:
                    {
                        var df = item.End - item.Start;
                        if (Math.Abs(df) <= dy)
                        {
                            var ecr = 1 * co.PositiveElastic * dEff;
                            var drift = new DeltaDrift(item.Start, item.End);
                            var de = drift.Delta * ecr/100;
                            var elongation = new DeltaElongation(currentE, currentE + de);
                            var section = new SectionCondition(RebarCondition.Elastic, experiencedYield, ecr, co.PositiveElastic, 0, 1);
                            currentE += de;
                            var delta = new DeData(++index, drift, elongation, section, item);
                            list.Add(delta);
                            break;
                        }

                        var deltaDrift1 = new DeltaDrift(item.Start, item.Start - dy);
                        var ecr1 = 1 * co.PositiveElastic * dEff;
                        var de1 = -dy * ecr1 / 100;
                        var elongation1 = new DeltaElongation(currentE, currentE + de1);
                        var section1 = new SectionCondition(RebarCondition.Elastic, experiencedYield, ecr1, co.PositiveElastic, 0, 1);
                        currentE += de1;
                        var delta1 = new DeData(++index, deltaDrift1, elongation1, section1, item);
                        var deltaDrift2 = new DeltaDrift(item.Start - dy, item.End);
                        var ecr2 = co.PositiveElastic * dEff;
                        var de2 = 0.0;
                        var elongation2 = new DeltaElongation(currentE, currentE + de2);
                        var section2 = new SectionCondition(RebarCondition.Elastic, experiencedYield, ecr2, co.PositiveElastic, 0, 0);
                        currentE += de2;
                        var delta2 = new DeData(++index, deltaDrift2, elongation2, section2, item);
                        list.Add(delta1);
                        list.Add(delta2);
                    }
                    break;
                case CycleState.NL:

                   
                    travels = travelCounter.AddTravel(item.Start, item.End);
                    foreach (var travelPiece in travels)
                    {
                        var depthCoeff = (Math.Abs(travelPiece.End) <= dy) ? co.PositiveElastic : co.PositivePlastic;
                        var ecr = travelPiece.Coefficient * depthCoeff * dEff;
                        var df = (travelPiece.End - travelPiece.Start);
                        var drift = new DeltaDrift(travelPiece.Start, travelPiece.End);
                        var de = -df * ecr / 100;
                        var elongation = new DeltaElongation(currentE, currentE + de);
                        currentE += de;
                        var section = new SectionCondition(RebarCondition.Yield, travelPiece.IsYield, ecr, depthCoeff, travelPiece.RepeatCount, travelPiece.Coefficient);
                        var delta = new DeData(++index, drift, elongation, section, item);
                        list.Add(delta);
                        if (travelPiece.IsYield)
                        {
                            experiencedYield = true;
                        }
                        break;
                    }

                    break;
                case CycleState.NU:
                    {
                        var df = item.End - item.Start;
                        if (Math.Abs(df) <= dy)
                        {
                            var ecr = 1 * co.PositiveElastic * dEff;
                            var drift = new DeltaDrift(item.Start, item.End);
                            var de = -drift.Delta * ecr / 100;
                            var elongation = new DeltaElongation(currentE, currentE + de);
                            var section = new SectionCondition(RebarCondition.Elastic, experiencedYield, ecr, co.PositiveElastic, 0, 1);
                            currentE += de;
                            var delta = new DeData(++index, drift, elongation, section, item);
                            list.Add(delta);
                            break;
                        }

                        var deltaDrift1 = new DeltaDrift(item.Start, item.Start - dy);
                        var ecr1 = 1 * co.PositiveElastic * dEff;
                        var de1 = +dy * ecr1 / 100;
                        var elongation1 = new DeltaElongation(currentE, currentE + de1);
                        var section1 = new SectionCondition(RebarCondition.Elastic, experiencedYield, ecr1, co.PositiveElastic, 0, 1);
                        currentE += de1;
                        var delta1 = new DeData(++index, deltaDrift1, elongation1, section1, item);
                        var deltaDrift2 = new DeltaDrift(item.Start - dy, item.End);
                        var ecr2 = co.PositiveElastic * dEff;
                        const double de2 = 0.0;
                        var elongation2 = new DeltaElongation(currentE, currentE + de2);
                        var section2 = new SectionCondition(RebarCondition.Elastic, experiencedYield, ecr2, co.PositiveElastic, 0, 0);
                        currentE += de2;
                        var delta2 = new DeData(++index, deltaDrift2, elongation2, section2, item);
                        list.Add(delta1);
                        list.Add(delta2);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        _allDeltas.AddRange(list);
    }
}