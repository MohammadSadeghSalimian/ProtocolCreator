using System.Diagnostics;

namespace ProtocolCreator.Core
{
    public static class Extensions
    {

        public static Direction GetDirection(this DeltaDrift deltaDrift)
        {
            return deltaDrift.Center>=0 ? Direction.Positive : Direction.Negative;
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

            return direction == Direction.Positive ? coefficientContainer.PositivePlastic : coefficientContainer.NegativePlastic;
        }

        public static double GetEccentricity(double effectiveDepth, double depthCoefficient)
        {
            return effectiveDepth* depthCoefficient;
        }

        public static double GetSlopeOfElongationLine(int repeat)
        {
            return repeat switch
            {
                0 => 1,
                1 => 0.5,
                2 => 0.25,
                _ => 0
            };
        }

        public static ResidualElongations GetResidualElongation(this CoefficientContainer coefficientContainer,double effectiveDepth,double dy)
        {
            var res = new ResidualElongations(coefficientContainer.PositiveElastic * dy * effectiveDepth,
                coefficientContainer.NegativeElastic * dy * effectiveDepth);
            return res;
        }

    }

    public class CoefficientContainer(
        double positiveElastic,
        double negativeElastic,
        double positivePlastic,
        double negativePlastic)
    {
        public double PositivePlastic { get;  } = positivePlastic;
        public double NegativePlastic { get; } = negativePlastic;
        public double PositiveElastic { get; } = positiveElastic;
        public double NegativeElastic { get; } = negativeElastic;

        
        
    }
    public enum Direction:byte
    {
        Positive = 0,
        Negative = 1,
    }

    public enum LoadingPhase:byte
    {
       Loading,
       Unloading,
    }

    public enum RebarCondition : byte
    {
        Yield,Elastic
    }
    [DebuggerDisplay("D {Start}-->{End}")]
    public class DeltaDrift(double start, double end, int repeat)
    {
        public double Start { get; } = start; // Start drift value

        public double End { get; } = end; // End drift value
        public double Center { get;  } = (start+end)/2.0; // Center drift value, average of start and end
        public int Repeat { get; } = repeat; // Number of repetitions of the drift. It means how many times the beam experienced this drift
    }

    [DebuggerDisplay("E {Start}-->{End}")]
    public class DeltaElongation(double start, double end, Direction direction, LoadingPhase loading)
    {
        public Direction Direction { get; } = direction; // Direction of the elongation, positive or negative
        public LoadingPhase Loading { get; } = loading; // Loading or unloading phase

        public double Start { get; } = start; // Start elongation value
        public double End { get; } = end; // End elongation value
        public double Center { get; } = (start + end) / 2.0; // Center elongation value

    }

    public class SectionCondition
    {
        public RebarCondition RebarCondition { get; }
        public double Ecentericity { get; }

        public double DepthCoefficient { get; }

        public double K { get; } // Slope of the line in the elongation curve
    }

    public class Delta
    {
        public int Id { get; }
        public DeltaDrift Drift { get; }
        public DeltaElongation Elongation { get; }
        public SectionCondition Section { get; }
    }

    public class LineSegment(DriftSegment driftSegment)
    {
        public DriftSegment DriftSegment { get; } = driftSegment;
        private readonly List<Delta> _deltas = [];

        public IReadOnlyList<Delta> Deltas =>_deltas; // List of deltas in the segment

       

        

    }

    public class DriftSegment(double startDrift, double endDrift)
    {
        public double StartDrift { get; } = startDrift;
        public double EndDrift { get; } = endDrift;
    }

    public class ResidualElongations(double positive,double negative)
    {
        public double Positive { get; } = positive; // Positive residual elongation
        public double Negative { get; } = negative; // Negative residual elongation
    }
    public class AnalysisInformation(
        double rebarYieldDrift,
        double effectiveDepth,
        CoefficientContainer coefficients,
        ResidualElongations residualElongations,double step)
    {
        private double RebarYieldDrift { get; } = rebarYieldDrift;

        public double EffectiveDepth { get; } = effectiveDepth;

        public CoefficientContainer Coefficients { get; } = coefficients;
        public ResidualElongations ResidualElongations { get; } = residualElongations;

        public double Step { get; } = step;
    }
    public class Engine(IReadOnlyList<DriftSegment> driftSegments, AnalysisInformation info)
    {
        private readonly List<Delta> _deltas = [];
        public IReadOnlyList<Delta> Deltas => _deltas;
        public IReadOnlyList<DriftSegment> DriftSegments { get; } = driftSegments;




        public AnalysisInformation  Info { get; } = info;

        public void Setup()
        {
            foreach (var item in DriftSegments)
            {
                var deltaValues = MathExtension.Arrange(item.StartDrift, item.EndDrift, Info.Step);
                var deltas=new List<Delta>()
                var lineSegment = new LineSegment(item);
                
            }
        }

    }


    public static class MathExtension
    {

        public static double[] Arrange(double start, double stop, double step)
        {
            if (step == 0)
                throw new ArgumentException("Step cannot be zero.", nameof(step));

            int count = (int)Math.Ceiling((stop - start) / step);
            if (count <= 0)
                return [];

            var result = new double[count];
            double value = start;
            for (int i = 0; i < count; i++)
            {
                result[i] = value;
                value += step;
            }
            return result;
        }

        public static double[] LinearSpace(double start, double stop, int num)
        {
            if (num < 2)
                throw new ArgumentException("num must be at least 2.", nameof(num));

            var result = new double[num];
            double step = (stop - start) / (num - 1);
            for (int i = 0; i < num; i++)
            {
                result[i] = start + step * i;
            }
            // Ensure last value is exactly stop to avoid floating point error
            result[num - 1] = stop;
            return result;
        }

    }
}
