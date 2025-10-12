namespace ProtocolCreator.Core.NewDesign;

internal abstract class StageHandlerBase(AnalysisInformation info, ICoefficientProvider coefs) : IStageHandler
{
    protected readonly AnalysisInformation Info = info;
    protected readonly ICoefficientProvider Coefs = coefs;
    public abstract void Handle(DriftSegment segment, EngineContext ctx);

    protected static void GuardNoCrossing(double a, double b, double marker, string markerName)
    {
        // logic unchanged: throw if marker is crossed within a single step
        if (a < marker && b > marker || a > marker && b < marker)
            throw new ArgumentException(
                $"The {markerName} ({marker}) is placed between the steps. This is not allowed.");
    }

    // StageHandlerBase
    protected static int GetRepeat(
        Dictionary<DoublePair, int> map,
        double a, double b,
        bool isYielded, bool inElastic)
    {
        var key = new DoublePair(a, b);
        if (map.TryGetValue(key, out var count))
        {
            if (!isYielded && inElastic)
            {
                map[key] = 1;
                return 1;
            }

            map[key] = count + 1;
            return count + 1;
        }

        map[key] = 1;
        return 1;
    }

    protected static double SlopeFromRepeat(int repeat) => Extensions.GetSlopeOfElongationLine(repeat);

    // StageHandlerBase
    protected static Delta BuildDelta(
        int id, double cycle,
        double a, double b,
        double currentElongation, double destinationElongation,
        bool experiencedYield, double currentCoefficient,
        int repeat, double slope, double ecr,
        DriftSegment item,
        DeltaCondition condition) // <-- matches your SectionCondition
    {
        var dd = new DeltaDrift(a, b);
        var ee = new DeltaElongation(currentElongation, destinationElongation);
        var sec = new SectionCondition(
            experiencedYield,
            currentCoefficient,
            repeat,
            slope,
            ecr,
            condition
        );
        return new Delta(id, cycle, dd, ee, sec, item);
    }
}