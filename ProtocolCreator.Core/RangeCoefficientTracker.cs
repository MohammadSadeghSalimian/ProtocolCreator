namespace ProtocolCreator.Core;

public class RangeCoefficientTracker(TrackerOptions options)
{
    private const double EPS = 1e-6;

    private static bool NearlyEqual(double a, double b) => Math.Abs(a - b) < EPS;
    private static bool InOpen(double x, double lo, double hi) => x > lo + EPS && x < hi - EPS;

    private class Segment
    {
        public double Start { get; init; }
        public double End { get; init; }
        public int Count { get; set; }
    }

    private readonly List<Segment> _segments = [];
    private TrackerOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    // Sticky yield memory
    private bool _hasYieldedEitherSide = false;
    private bool _hasYieldedPositive = false;
    private bool _hasYieldedNegative = false;

    public RangeCoefficientTracker(double positiveStation, double negativeStation, YieldMode mode = YieldMode.EitherSide)
        : this(new TrackerOptions { PositiveStation = positiveStation, NegativeStation = negativeStation, Mode = mode })
    { }

    public double FarthestPositiveStation { get; private set; } = double.NegativeInfinity;
    public double FarthestNegativeStation { get; private set; } = double.PositiveInfinity;
    public double PeakStationAbs =>
        Math.Abs(FarthestPositiveStation) >= Math.Abs(FarthestNegativeStation)
            ? FarthestPositiveStation
            : FarthestNegativeStation;

    public void SetOptions(TrackerOptions options) => _options = options ?? throw new ArgumentNullException(nameof(options));

    public List<TravelPiece> AddTravel(double start, double end)
    {
        var descending = end < start;
        var lo = Math.Min(start, end);
        var hi = Math.Max(start, end);
        var positiveDirection = end > start;

        UpdatePeaks(start, end);

        // 1) Yield (with stickiness)
        var isYield = IsYieldNowWithStickiness(start, end, positiveDirection,
                            out var newlyYieldedPos, out var newlyYieldedNeg);

        if (newlyYieldedPos || newlyYieldedNeg)
        {
            if (_options.Mode == YieldMode.EitherSide)
            {
                _hasYieldedEitherSide = true;
            }
            else
            {
                if (newlyYieldedPos) _hasYieldedPositive = true;
                if (newlyYieldedNeg) _hasYieldedNegative = true;
            }
            isYield = IsYieldNowWithStickiness(start, end, positiveDirection, out _, out _);
        }

        // 2) Forced cuts at yield stations (to avoid merging across the gate)
        var forcedCuts = new List<double>();
        if (isYield)
        {
            if (_options.Mode == YieldMode.EitherSide)
            {
                if (InOpen(_options.PositiveStation, lo, hi)) forcedCuts.Add(_options.PositiveStation);
                if (InOpen(_options.NegativeStation, lo, hi)) forcedCuts.Add(_options.NegativeStation);
            }
            else // Directional: only the relevant station for the call direction
            {
                if (positiveDirection)
                {
                    if (InOpen(_options.PositiveStation, lo, hi)) forcedCuts.Add(_options.PositiveStation);
                }
                else
                {
                    if (InOpen(_options.NegativeStation, lo, hi)) forcedCuts.Add(_options.NegativeStation);
                }
            }
        }

        // 3) Ensure subdivisions including the forced cuts
        EnsureSubdivisions(lo, hi, forcedCuts);

        // 4) Increment counts ONLY when yielded
        if (isYield)
        {
            foreach (var seg in _segments.Where(s => s.Start < hi && s.End > lo))
                seg.Count++;
        }

        // 5) Build raw pieces (after any increment)
        var rawAsc = _segments
            .Where(s => s.Start < hi && s.End > lo)
            .Select(s => (Start: Math.Max(s.Start, lo), End: Math.Min(s.End, hi), Cnt: s.Count))
            .OrderBy(t => t.Start)
            .ToList();

        var mergedAsc = new List<(double Start, double End, int Rep, double Coef)>();
        foreach (var r in rawAsc)
        {
            var rep = ReportedRepeat(r.Cnt);
            var coef = ReportedCoef(r.Cnt);

            if (mergedAsc.Count > 0 &&
                mergedAsc[^1].Rep == rep &&
                Math.Abs(mergedAsc[^1].Coef - coef) < 1e-15 &&
                NearlyEqual(mergedAsc[^1].End, r.Start) &&
                !IsForcedBoundary(r.Start)) // <- do not merge across station
            {
                var last = mergedAsc[^1];
                mergedAsc[^1] = (last.Start, r.End, rep, coef);
            }
            else
            {
                mergedAsc.Add((r.Start, r.End, rep, coef));
            }
        }

        // 7) Return in call direction
        if (descending)
        {
            mergedAsc.Reverse();
            for (var i = 0; i < mergedAsc.Count; i++)
            {
                var m = mergedAsc[i];
                mergedAsc[i] = (m.End, m.Start, m.Rep, m.Coef);
            }
        }

        var result = new List<TravelPiece>(mergedAsc.Count);
        foreach (var m in mergedAsc)
            result.Add(new TravelPiece(m.Start, m.End, m.Coef, isYield, m.Rep));
        return result;

        int ReportedRepeat(int actual) => isYield ? actual : 0;

        double ReportedCoef(int actual) => CoefFromRepeat(ReportedRepeat(actual));

        // 6) Merge adjacent pieces with same (repeat, coef) BUT NEVER across a forced cut
        bool IsForcedBoundary(double x)
        {
            if (forcedCuts.Count == 0) return false;
            foreach (var c in forcedCuts) if (NearlyEqual(c, x)) return true;
            return false;
        }
    }

    private static double CoefFromRepeat(int repeat) => repeat switch
    {
        0 => 1.0,   // not yet yielded, “potential first time”
        1 => 1.0,
        2 => 0.5,
        3 => 0.25,
        _ => 0.0
    };

    // Snapshot unchanged (keeps merging across equal slices, no forced splits)
    public List<TravelPiece> GetSnapshot(double start, double end)
    {
        var descending = end < start;
        var lo = Math.Min(start, end);
        var hi = Math.Max(start, end);

        EnsureSubdivisions(lo, hi, forcedCuts: null);

        var rawAsc = _segments
            .Where(s => s.Start < hi && s.End > lo)
            .Select(s => (Start: Math.Max(s.Start, lo), End: Math.Min(s.End, hi),
                          Rep: s.Count, Coef: CoefFromRepeat(s.Count)))
            .OrderBy(t => t.Start)
            .ToList();

        var mergedAsc = new List<(double Start, double End, int Rep, double Coef)>();
        foreach (var r in rawAsc)
        {
            if (mergedAsc.Count > 0 &&
                mergedAsc[^1].Rep == r.Rep &&
                Math.Abs(mergedAsc[^1].Coef - r.Coef) < 1e-15 &&
                NearlyEqual(mergedAsc[^1].End, r.Start))
            {
                var last = mergedAsc[^1];
                mergedAsc[^1] = (last.Start, r.End, r.Rep, r.Coef);
            }
            else
            {
                mergedAsc.Add((r.Start, r.End, r.Rep, r.Coef));
            }
        }

        if (descending)
        {
            mergedAsc.Reverse();
            for (var i = 0; i < mergedAsc.Count; i++)
            {
                var m = mergedAsc[i];
                mergedAsc[i] = (m.End, m.Start, m.Rep, m.Coef);
            }
        }

        var result = new List<TravelPiece>(mergedAsc.Count);
        foreach (var m in mergedAsc)
            result.Add(new TravelPiece(m.Start, m.End, m.Coef, false, m.Rep));
        return result;
    }

    // ---- internals unchanged except EnsureSubdivisions signature ----

    private bool IsYieldNowWithStickiness(double start, double end, bool positiveDirection,
        out bool newlyYieldedPos, out bool newlyYieldedNeg)
    {
        var lo = Math.Min(start, end);
        var hi = Math.Max(start, end);

        var crossesPositive = _options.PositiveStation > lo && _options.PositiveStation < hi;
        var crossesNegative = _options.NegativeStation > lo && _options.NegativeStation < hi;

        newlyYieldedPos = false;
        newlyYieldedNeg = false;

        if (_options.Mode == YieldMode.EitherSide)
        {
            if (_hasYieldedEitherSide) return true;
            if (!crossesPositive && !crossesNegative) return false;
            newlyYieldedPos = crossesPositive;
            newlyYieldedNeg = crossesNegative;
            return true;
        }

        if (positiveDirection)
        {
            if (_hasYieldedPositive) return true;
            if (!crossesPositive) return false;
            newlyYieldedPos = true;
        }
        else
        {
            if (_hasYieldedNegative) return true;
            if (!crossesNegative) return false;
            newlyYieldedNeg = true;
        }

        return true;
    }

    private void UpdatePeaks(double start, double end)
    {
        var lo = Math.Min(start, end);
        var hi = Math.Max(start, end);
        if (hi > FarthestPositiveStation) FarthestPositiveStation = hi;
        if (lo < FarthestNegativeStation) FarthestNegativeStation = lo;
    }

    // NOTE: now accepts forcedCuts and uses them as mandatory split points
    private void EnsureSubdivisions(double start, double end, IEnumerable<double>? forcedCuts)
    {
        var cuts = new SortedSet<double> { start, end };

        if (forcedCuts != null)
        {
            foreach (var c in forcedCuts)
                if (c > start + EPS && c < end - EPS)
                    cuts.Add(c);
        }

        foreach (var seg in _segments.Where(s => s.Start < end && s.End > start))
        {
            cuts.Add(seg.Start);
            cuts.Add(seg.End);
        }

        var newSegments = new List<Segment>();
        var points = cuts.OrderBy(x => x).ToList();

        for (var i = 0; i < points.Count - 1; i++)
        {
            double a = points[i], b = points[i + 1];
            var existing = _segments.FirstOrDefault(s => s.Start <= a && s.End >= b);
            newSegments.Add(new Segment
            {
                Start = a,
                End = b,
                Count = existing?.Count ?? 0
            });
        }

        _segments.RemoveAll(s => s.Start < end && s.End > start);
        _segments.AddRange(newSegments);
        _segments.Sort((x, y) => x.Start.CompareTo(y.Start));
    }
}
