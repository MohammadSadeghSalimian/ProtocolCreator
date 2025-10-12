using ProtocolCreator.Core.NewDesign;

namespace ProtocolCreator.Core
{
    public static class Extensions
    {
        public static Direction GetDirection(double a, double b)
        {
            var c=0.5*(a + b);
            return c >= 0 ? Direction.Positive : Direction.Negative;
        }

        public static Direction GetDirection(this DeltaDrift deltaDrift)
        {
            return deltaDrift.Center>=0 ? Direction.Positive : Direction.Negative;
        }
        public static Direction GetDirection(this DriftSegment driftSegment)
        {
            return 0.5*(driftSegment.Start+driftSegment.End) >= 0 ? Direction.Positive : Direction.Negative;
        }
        public static DeltaCondition GetConditionInLoading(bool isExperiencedYield,bool newExperiencedDrift,bool isInPlastic)
        {
            return isExperiencedYield switch
            {
                false when !isInPlastic => DeltaCondition.NewElastic,
                true when !isInPlastic => DeltaCondition.OldElastic,
                true when isInPlastic && !newExperiencedDrift => DeltaCondition.OldPlastic,
                true when isInPlastic && newExperiencedDrift => DeltaCondition.NewPlastic,
                _ => throw new ArgumentException("Invalid state for loading condition")
            };
        }
        public static DeltaCondition GetDeltaConditionInUnLoading(bool isInResidual)
        {
            return isInResidual ? DeltaCondition.Residual : DeltaCondition.Slip;
        }
        //public static DeltaCondition GetRebarCondition(this DeltaDrift deltaDrift,double reinforcementYieldDrift)
        //{
        //    return Math.Abs(deltaDrift.Center)>Math.Abs(reinforcementYieldDrift) ? DeltaCondition.Yield : DeltaCondition.Elastic;
        //}
        //public static double GetCurrentDepthCoefficient(this CoefficientContainer coefficientContainer,Direction direction, DeltaCondition deltaCondition)
        //{
        //    if (deltaCondition == DeltaCondition.Elastic)
        //    {
        //        return direction == Direction.Positive ? coefficientContainer. PositiveElastic : coefficientContainer.NegativeElastic;
        //    }

        //    return direction == Direction. Positive ? coefficientContainer.PositivePlastic : coefficientContainer.NegativePlastic;
        //}

        public static double GetEccentricity(double effectiveDepth, double depthCoefficient)
        {
            return effectiveDepth* depthCoefficient;
        }

        public static double GetSlopeOfElongationLine(int repeat)
        {
            return repeat switch
            {
                0 => 1,
                1 => 1,
                2 => 0.5,
                3 => 0.25,
                _ => 0
            };
        }

        public static ResidualElongations GetResidualElongation(this CoefficientContainer coefficientContainer,double effectiveDepth,double dy)
        {
            var res = new ResidualElongations(coefficientContainer.Positive.ElasticLoading * dy * effectiveDepth,
                coefficientContainer.Negative.ElasticLoading * dy * effectiveDepth);
            return res;
        }


        //public static double GetSignedStep(this DriftSegment driftSegment)
        //{
        //    if (driftSegment is { Direction: Direction.Positive, LoadingPhase: LoadingPhase.Unloading } or { Direction: Direction.Negative, LoadingPhase: LoadingPhase.Loading })
        //    {
        //        return -driftSegment.UnsignedStep;
        //    }

        //    return driftSegment.UnsignedStep;
        //}

        public static (double, double) GetPeaks(this DriftSegment driftSegment, double positive, double negative)
        {
            var start = driftSegment.Start;
            var end = driftSegment.End;

            if (start > positive || end > positive)
                positive = start > end ? start : end;

            if (start < negative || end < negative)
                negative = start < end ? start : end;

            return (positive, negative);
        }
    }
}
