using ProtocolCreator.Core;
using Xunit;

namespace ProtocolCreator.Tests;

public class RangeCoefficientTrackerStickyYieldTests
{
    // ---------- helpers ----------
    private static void AssertPieces(
        List<TravelPiece> actual,
        params TravelPiece[] expected)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Start, actual[i].Start, 6);
            Assert.Equal(expected[i].End, actual[i].End, 6);
            Assert.Equal(expected[i].Coefficient, actual[i].Coefficient, 6);
            Assert.Equal(expected[i].IsYield, actual[i].IsYield);
            Assert.Equal(expected[i].RepeatCount, actual[i].RepeatCount);
        }
    }

    public static List<List<TravelPiece>> RunSequence(
        RangeCoefficientTracker tracker,
        params (double start, double end)[] travels)
    {
        var results = new List<List<TravelPiece>>();
        foreach (var (start, end) in travels)
            results.Add(tracker.AddTravel(start, end));
        return results;
    }

    private static TravelPiece TP(double s, double e, double coef, bool y, int r)
        => new TravelPiece(s, e, coef, y, r);

    // ============================================================
    // 1) EITHER-SIDE MODE — strict pass + sticky memory + forced splits
    // ============================================================

    [Fact]
    public void EitherSide_BoundaryTouch_IsNotYield_Repeat0Coef1()
    {
        var t = new RangeCoefficientTracker(positiveStation: 1.0, negativeStation: -1.0, mode: YieldMode.EitherSide);

        // Touching +1.0 at the endpoint is NOT a strict pass
        var res = t.AddTravel(0.0, 1.0);

        // Not yielded ⇒ Repeat=0, Coef=1.0 and a single piece (no station inside)
        AssertPieces(res, TP(0.0, 1.0, 1.0, false, 0));
    }

    [Fact]
    public void EitherSide_StrictPass_SetsSticky_ForAllFutureTravels_AndSplitsAtStation()
    {
        var t = new RangeCoefficientTracker(0.5, -0.5, YieldMode.EitherSide);

        // First: strict pass (+0.5 inside 0..0.75) ⇒ yield AND split at station
        var a = t.AddTravel(0.0, 0.75);
        AssertPieces(a,
            TP(0.0, 0.5, 1.0, true, 1),
            TP(0.5, 0.75, 1.0, true, 1));

        // Sticky now: small segment (doesn't cross stations) still yields and increments that slice
        var b = t.AddTravel(0.10, 0.20);
        AssertPieces(b, TP(0.10, 0.20, 0.5, true, 2)); // that slice is now 2

        // New area still yields (first time for this slice)
        var c = t.AddTravel(-0.1, 0.0);
        AssertPieces(c, TP(-0.1, 0.0, 1.0, true, 1));
    }

    [Fact]
    public void EitherSide_SplitsWhenCountsDiffer_NoMergeAcrossStation()
    {
        var t = new RangeCoefficientTracker(0.5, -0.5, YieldMode.EitherSide);

        // First yielded pass: split into two 1/1
        t.AddTravel(0.0, 0.75);

        // Another yielded pass on 0..0.5 only: counts -> (2 / 1)
        t.AddTravel(0.0, 0.5);

        // Now a yielded pass across 0..0.75 again:
        var res = t.AddTravel(0.0, 0.75);
        // Expected: 0..0.5 → 3 (coef .25), 0.5..0.75 → 2 (coef .5)
        AssertPieces(res,
            TP(0.0, 0.5, 0.25, true, 3),
            TP(0.5, 0.75, 0.5, true, 2));

        // If we now traverse 0.5..0.75 only, its count becomes 3
        var res2 = t.AddTravel(0.5, 0.75);
        AssertPieces(res2, TP(0.5, 0.75, 0.25, true, 3));

        // After that, traversing 0..0.75 once more pushes both parts to 4
        var res3 = t.AddTravel(0.0, 0.75);
        // Now both have repeat=4 ⇒ coef=0.0 and MUST remain split at station
        AssertPieces(res3,
            TP(0.0, 0.5, 0.0, true, 4),
            TP(0.5, 0.75, 0.0, true, 4));
    }

    [Fact]
    public void EitherSide_DescendingOrder_IsPreserved_WithSplitAtNegativeStation()
    {
        var t = new RangeCoefficientTracker(1.0, -1.0, YieldMode.EitherSide);

        // Make it sticky by crossing +1.0 (this call would split at +1.0, but we don't assert it)
        t.AddTravel(0.0, 2.0);

        // Descending call crosses -1.0 → split at -1.0 and keep descending order
        var res = t.AddTravel(0.0, -3.0);
        AssertPieces(res,
            TP(0.0, -1.0, 1.0, true, 1),
            TP(-1.0, -3.0, 1.0, true, 1));
    }

    // ============================================================
    // 2) DIRECTIONAL MODE — sticky is per direction + forced split on its station
    // ============================================================

    [Fact]
    public void Directional_PositiveAndNegativeStickinessAreIndependent_WithSplits()
    {
        var t = new RangeCoefficientTracker(positiveStation: 2.0, negativeStation: -2.0, mode: YieldMode.Directional);

        // Positive-going not crossing +2.0 => not yielded
        var a = t.AddTravel(1.0, 1.9);
        AssertPieces(a, TP(1.0, 1.9, 1.0, false, 0));

        // Negative-going crossing -2.0 => yield and split at -2.0
        var b = t.AddTravel(0.0, -2.1);
        AssertPieces(b,
            TP(0.0, -2.0, 1.0, true, 1),
            TP(-2.0, -2.1, 1.0, true, 1));

        // Positive still not sticky
        var c = t.AddTravel(0.0, 1.5);
        AssertPieces(c, TP(0.0, 1.5, 1.0, false, 0));

        // Now cross +2.0 => positive becomes sticky; split at +2.0
        var d = t.AddTravel(0.0, 2.5);
        AssertPieces(d,
            TP(0.0, 2.0, 1.0, true, 1),
            TP(2.0, 2.5, 1.0, true, 1));

        // Subsequent positive travel is yielded; no station inside -> single piece incrementing to 2
        var e = t.AddTravel(0.0, 1.0);
        AssertPieces(e, TP(0.0, 1.0, 0.5, true, 2));
    }

    [Fact]
    public void Directional_StrictBoundaryDoesNotFlipStickiness_SplitWhenActuallyPassed()
    {
        var t = new RangeCoefficientTracker(positiveStation: 2.0, negativeStation: -2.0, mode: YieldMode.Directional);

        // Touch +2.0 exactly (no strict pass) => still not sticky on positive
        var a = t.AddTravel(0.0, 2.0);
        AssertPieces(a, TP(0.0, 2.0, 1.0, false, 0));

        // Now actually pass +2.0 => split there
        var b = t.AddTravel(0.0, 2.01);
        AssertPieces(b,
            TP(0.0, 2.0, 1.0, true, 1),
            TP(2.0, 2.01, 1.0, true, 1));

        // Positive is sticky now; a small positive segment yields and increments
        var c = t.AddTravel(1.0, 1.5);
        AssertPieces(c, TP(1.0, 1.5, 0.5, true, 2));
    }

    // ============================================================
    // 3) SNAPSHOT & PEAKS
    // ============================================================

    [Fact]
    public void Snapshot_IsNonMutating_IsYieldFalse_ReportsCountsAndCoefs_MergedSlices()
    {
        var t = new RangeCoefficientTracker(0.5, -0.5, YieldMode.EitherSide);
        // First yielded call splits at 0.5 but snapshot can merge equal slices
        t.AddTravel(0.0, 0.75); // [0..0.5]=1, [0.5..0.75]=1

        var snap = t.GetSnapshot(0.0, 1.0);
        // Snapshot merges adjacent equal (rep, coef), so [0..0.5] + [0.5..0.75] -> [0..0.75]
        // plus unseen tail [0.75..1.0] with rep=0→coef=1.0
        AssertPieces(snap,
            TP(0.0, 0.75, 1.0, false, 1),
            TP(0.75, 1.0, 1.0, false, 0));
    }

    [Fact]
    public void Peaks_AreTracked_PositiveNegativeAndOverall()
    {
        var t = new RangeCoefficientTracker(0.5, -0.5, YieldMode.EitherSide);

        t.AddTravel(-1.0, 0.0);
        Assert.Equal(-1.0, t.FarthestNegativeStation);
        Assert.Equal(0.0, t.FarthestPositiveStation);
        Assert.Equal(-1.0, t.PeakStationAbs);   // largest |x| so far is |-1| = 1 → returns -1

        t.AddTravel(0.0, 2.5);
        Assert.Equal(-1.0, t.FarthestNegativeStation);
        Assert.Equal(2.5, t.FarthestPositiveStation);
        Assert.Equal(2.5, t.PeakStationAbs);

        t.AddTravel(-3.0, -0.2);
        Assert.Equal(-3.0, t.FarthestNegativeStation);
        Assert.Equal(2.5, t.FarthestPositiveStation);
        Assert.Equal(-3.0, t.PeakStationAbs);
    }

    // ============================================================
    // 4) The requested sequence helper case (0→0.25, …, 0→1.0 twice)
    // ============================================================

    [Fact]
    public void RunSequence_EitherSide_YieldsExpected_WithSplitAtStation()
    {
        // Important stations ±0.5, EitherSide sticky mode
        var t = new RangeCoefficientTracker(0.5, -0.5, YieldMode.EitherSide);

        // Sequence: 0→0.25, 0→0.25, 0→0.5, 0→0.5, 0→0.75, 0→0.75, 0→1.0, 0→1.0
        var seq = RunSequence(t,
            (0, 0.25),
            (0, 0.25),
            (0, 0.5),
            (0, 0.5),
            (0, 0.75),
            (0, 0.75),
            (0, 1.0),
            (0, 1.0)
        );

        // 1) and 2) not yielded
        AssertPieces(seq[0], TP(0.0, 0.25, 1.0, false, 0));
        AssertPieces(seq[1], TP(0.0, 0.25, 1.0, false, 0));

        // 3) and 4) not yielded (touching 0.5)
        AssertPieces(seq[2], TP(0.0, 0.5, 1.0, false, 0));
        AssertPieces(seq[3], TP(0.0, 0.5, 1.0, false, 0));

        // 5) first yielded (split at 0.5)
        AssertPieces(seq[4],
            TP(0.0, 0.5, 1.0, true, 1),
            TP(0.5, 0.75, 1.0, true, 1));

        // 6) second yielded (split at 0.5), both parts now repeat=2 (coef .5)
        AssertPieces(seq[5],
            TP(0.0, 0.5, 0.5, true, 2),
            TP(0.5, 0.75, 0.5, true, 2));

        // 7) 0..0.5→3 (.25), 0.5..0.75→3 (.25), new tail 0.75..1.0→1 (1.0)
        AssertPieces(seq[6],
            TP(0.0, 0.5, 0.25, true, 3),
            TP(0.5, 0.75, 0.25, true, 3),
            TP(0.75, 1.0, 1.0, true, 1));

        // 8) 0..0.5→4 (0.0), 0.5..0.75→4 (0.0), 0.75..1.0→2 (0.5)
        AssertPieces(seq[7],
            TP(0.0, 0.5, 0.0, true, 4),
            TP(0.5, 0.75, 0.0, true, 4),
            TP(0.75, 1.0, 0.5, true, 2));
    }
}
