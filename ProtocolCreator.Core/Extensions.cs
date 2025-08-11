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
        public static RebarCondition GetRebarCondition(double a,double b, double reinforcementYieldDrift)
        {
            var c=0.5*(a + b);
            return Math.Abs(c) > Math.Abs(reinforcementYieldDrift) ? RebarCondition.Yield : RebarCondition.Elastic;
        }
        public static RebarCondition GetRebarCondition(this DeltaDrift deltaDrift,double reinforcementYieldDrift)
        {
            return Math.Abs(deltaDrift.Center)>Math.Abs(reinforcementYieldDrift) ? RebarCondition.Yield : RebarCondition.Elastic;
        }
        public static double GetCurrentDepthCoefficient(this CoefficientContainer coefficientContainer,Direction direction, RebarCondition rebarCondition)
        {
            if (rebarCondition == RebarCondition.Elastic)
            {
                return direction == Direction.Positive ? coefficientContainer. PositiveElastic : coefficientContainer.NegativeElastic;
            }

            return direction == Direction. Positive ? coefficientContainer.PositivePlastic : coefficientContainer.NegativePlastic;
        }

        public static double GetEccentricity(double effectiveDepth, double depthCoefficient)
        {
            return effectiveDepth* depthCoefficient;
        }

        public static double GetSlopeOfElongationLine(int repeat)
        {
            return repeat switch
            {
                1 => 1,
                2 => 0.5,
                3 => 0.25,
                _ => 0
            };
        }

        public static ResidualElongations GetResidualElongation(this CoefficientContainer coefficientContainer,double effectiveDepth,double dy)
        {
            var res = new ResidualElongations(coefficientContainer.PositiveElastic * dy * effectiveDepth,
                coefficientContainer.NegativeElastic * dy * effectiveDepth);
            return res;
        }


        public static double GetSignedStep(this DriftSegment driftSegment)
        {
            if (driftSegment is { Direction: Direction.Positive, LoadingPhase: LoadingPhase.Unloading } or { Direction: Direction.Negative, LoadingPhase: LoadingPhase.Loading })
            {
                return -driftSegment.UnsignedStep;
            }

            return driftSegment.UnsignedStep;
        }

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
