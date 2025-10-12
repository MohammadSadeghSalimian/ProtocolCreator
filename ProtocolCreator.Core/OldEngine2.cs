using ProtocolCreator.Core.NewDesign;

namespace ProtocolCreator.Core;

public class OldEngine2(IReadOnlyList<DriftSegment> driftSegments, AnalysisInformation info) : BaseEngine(driftSegments, info)
{


    private int GetRepeat(double a, double b, bool isYielded, bool isInElastic)
    {
        var dd = new DoublePair(a, b);
        if (RepeatCounter.TryGetValue(dd, out var count))
        {
            if (!isYielded && isInElastic)
            {
                RepeatCounter[dd] = 1;
                return 1;
            }
            RepeatCounter[dd] = count + 1;
            return count + 1;
        }
        RepeatCounter[dd] = 1;
        return 1;
    }


    public override void Calculate()
    {
        double peakPositive = 0;
        double peakNegative = 0;
        var dy = Info.RebarYieldDrift;
        var dEff = Info.EffectiveDepth;

        var co = Info.Coefficients;
        var isExperiencedYieldPositive = false;
        var isExperiencedYieldNegative = false;

        double currentElongation = 0;

        double cycle = 0;
        var id = 0;
        foreach (var item in DriftSegments)
        {
            var step = item.UnsignedStep;
            switch (item.CycleState)
            {
                case CycleState.PL:

                    SetPositiveLoading(item, step, ref id, ref cycle, dy, ref peakPositive, ref isExperiencedYieldPositive, co, dEff, ref currentElongation);
                    break;
                case CycleState.PU:

                    SetPositiveUnloading(item, step, dy,ref id, ref cycle, co, dEff, ref currentElongation, ref isExperiencedYieldPositive);
                    break;
                case CycleState.NL:
                    SetNegativeLoading(item, step, ref id, ref cycle, dy, ref peakNegative, ref isExperiencedYieldNegative, co, dEff, ref currentElongation);
                    break;
                case CycleState.NU:
                    SetNegativeUnloading(item, step, dy, ref id, ref cycle, co, dEff, ref currentElongation, ref isExperiencedYieldNegative);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }

    private void SetNegativeUnloading(DriftSegment item, double step, double dy, ref int id, ref double cycle,
        CoefficientContainer co, double dEff, ref double currentElongation, ref bool isExperiencedYield)
    {
        var deltaValues = MathExtension.Arrange(item.Start, item.End, step);
        var futureDrift = item.Start + dy;
        var n = deltaValues.Length; var deltas = new Delta[n];
        for (var i = 0; i < n; i++)
        {
            id += 1;
            cycle += 0.25 / n;
            var a = deltaValues[i];
            var b = a + step;
            if (b > futureDrift && a < futureDrift)
            {
                throw new ArgumentException(
                    $"The future drift ({futureDrift}) is placed between the steps. This is not allowed.");
            }

            var currentCoefficient =(isExperiencedYield)? co.Negative.PlasticUnloading:co.Negative.ElasticUnloading;
            double slope;
            int repeat;
            bool isInResidualArea;
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
            var ecr = slope * currentCoefficient * dEff; // eccentricity coefficient
            var deltaE = -ecr * step / 100;
            var destinationElongation = currentElongation + deltaE;
            var condition = Extensions.GetDeltaConditionInUnLoading(isInResidualArea);
            var dd = new DeltaDrift(a, b);
            var ee = new DeltaElongation(currentElongation, destinationElongation);
            var sec = new SectionCondition(isExperiencedYield,
                currentCoefficient, repeat, slope, ecr, condition);
            deltas[i] = new Delta(id, cycle, dd, ee, sec, item);
            currentElongation = destinationElongation;
        }
        AllDeltas.AddRange(deltas);
    }

    private void SetNegativeLoading(DriftSegment item, double step, ref int id, ref double cycle, double dy, ref double peakNegative,
        ref bool isExperiencedYield, CoefficientContainer co, double dEff, ref double currentElongation)
    {
        var deltaValues = MathExtension.Arrange(item.Start, item.End, -step);
        var n = deltaValues.Length;
        var deltas = new Delta[n];
        for (var i = 0; i < n; i++)
        {
            id += 1;
            cycle += 0.25 / n;
            var a = deltaValues[i];
            var b = a - step;
            var deltaD = -step;
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

            double currentCoefficient;
            bool isInPlasticArea;
            if (a > -dy && b >= -dy)
            {
                currentCoefficient = co.Negative.ElasticLoading;
                isInPlasticArea = false;
            }
            else
            {
                currentCoefficient = co.Negative.PlasticLoading;
                isInPlasticArea = true;
            }
            if (!isExperiencedYield) // it can change the behavior
            {
            }

            // the number of repeats in cycles
            var repeat = GetRepeat(a, b, isExperiencedYield, !isInPlasticArea);
            var slope = Extensions.GetSlopeOfElongationLine(repeat);
            bool newExperiencedDrift;
            if (a <= peakNegative && b < peakNegative)
            {
                newExperiencedDrift = true;
                peakNegative = b;
            }
            else
            {
                newExperiencedDrift = false;
            }
            var ecr = slope * currentCoefficient * dEff; // eccentricity coefficient
            var deltaE = -ecr * deltaD / 100.0;
            var destinationElongation = currentElongation + deltaE;
            var condition = Extensions.GetConditionInLoading(isExperiencedYield, newExperiencedDrift, isInPlasticArea);
            var dd = new DeltaDrift(a, b);
            var ee = new DeltaElongation(currentElongation, destinationElongation);
            var sec = new SectionCondition(isExperiencedYield,
                currentCoefficient, repeat, slope, ecr, condition);
            deltas[i] = new Delta(id, cycle, dd, ee, sec, item);
            currentElongation = destinationElongation;
        }
        AllDeltas.AddRange(deltas);
    }

    private void SetPositiveUnloading(DriftSegment item, double step, double dy, ref int id, ref double cycle,
        CoefficientContainer co, double dEff, ref double currentElongation, ref bool isExperiencedYield)
    {
        var deltaValues = MathExtension.Arrange(item.Start, item.End, -step);
        var futureDrift = item.Start - dy;
        var n = deltaValues.Length;
        var deltas = new Delta[n];
        for (var i = 0; i < n; i++)
        {
            id += 1;
            cycle += 0.25 / n;
            var a = deltaValues[i];
            var deltaD = -step;
            var b = a - step;
            if (b < futureDrift && a > futureDrift)
            {
                throw new ArgumentException(
                    $"The future drift ({futureDrift}) is placed between the steps. This is not allowed.");
            }

            var currentCoefficient = (isExperiencedYield)? co.Positive.PlasticUnloading:co.Positive.ElasticUnloading;
            double slope;
            int repeat;
            bool isInResidualArea;
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
            var ecr = slope * currentCoefficient * dEff;
            var deltaE = ecr * deltaD / 100;
            var destinationElongation = currentElongation + deltaE;
            var condition = Extensions.GetDeltaConditionInUnLoading(isInResidualArea);
            var dd = new DeltaDrift(a, b);
            var ee = new DeltaElongation(currentElongation, destinationElongation);
            var sec = new SectionCondition(isExperiencedYield,
                currentCoefficient, repeat, slope, ecr, condition);
            deltas[i] = new Delta(id, cycle, dd, ee, sec, item);
            currentElongation = destinationElongation;
        }
        AllDeltas.AddRange(deltas);
    }

    private void SetPositiveLoading(DriftSegment item, double step,ref int id, ref double cycle, double dy, ref double peakPositive,
        ref bool isExperiencedYield, CoefficientContainer co, double dEff, ref double currentElongation)
    {
        var deltaValues = MathExtension.Arrange(item.Start, item.End, step);
        var n = deltaValues.Length;

        var deltas = new Delta[n];
        for (var i = 0; i < n; i++)
        {
            id += 1;
            cycle += 0.25 / n;
            var a = deltaValues[i];
            var b = a + step;
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

            double currentCoefficient;
            bool isInPlasticArea;

            if (a < dy && b <= dy)
            {
                currentCoefficient = co.Positive.ElasticUnloading;
                isInPlasticArea = false;
            }
            else
            {
                currentCoefficient = co.Positive.PlasticLoading;
                isInPlasticArea = true;
            }
            if (!isExperiencedYield)
            {
            }

            // the number of repeats in cycles
            var repeat = GetRepeat(a, b, isExperiencedYield, !isInPlasticArea);
            var slope = Extensions.GetSlopeOfElongationLine(repeat);
            bool newExperiencedDrift;
            if (a >= peakPositive && b > peakPositive)
            {
                newExperiencedDrift = true;
                peakPositive = b;
            }
            else
            {
                newExperiencedDrift = false;
            }
            var ecr = slope * currentCoefficient * dEff; // eccentricity coefficient
            var deltaE = ecr * step / 100;
            var destinationElongation = currentElongation + deltaE;
            var condition = Extensions.GetConditionInLoading(isExperiencedYield, newExperiencedDrift, isInPlasticArea);
            var dd = new DeltaDrift(a, b);
            var ee = new DeltaElongation(currentElongation, destinationElongation);
            var sec = new SectionCondition(isExperiencedYield,
                currentCoefficient, repeat, slope, ecr, condition);
            deltas[i] = new Delta(id, cycle, dd, ee, sec, item);
            currentElongation = destinationElongation;
        }
        AllDeltas.AddRange(deltas);
    }
}