namespace ProtocolCreator.Core;

public static class PiecewiseLinearDownsampler
{
    /// <summary>
    /// Keeps the first and last points, plus any point where the slope (vs. t) changes
    /// for at least one of the provided value selectors. Assumes t is strictly increasing.
    /// </summary>
    /// <typeparam name="T">Your row/record type.</typeparam>
    /// <param name="data">Ordered by t (increasing).</param>
    /// <param name="t">Selector for the independent variable t.</param>
    /// <param name="minNewTrendSegments"></param>
    /// <param name="values">One or more selectors for dependent variables (x, y, z, ...).</param>
    /// <param name="tol">
    /// Slope equality tolerance. Use a small number ~1e-10..1e-6 depending on your data scale.
    /// </param>
    /// <returns>Downsampled sequence keeping only breakpoints.</returns>
    public static IReadOnlyList<T> Downsample<T>(
    this IReadOnlyList<T> data,
    Func<T, double> t,
    double tol = 1e-9,
    int minNewTrendSegments = 2,   // require the new slope to persist
    params Func<T, double>[] values)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(t);
        if (values == null || values.Length == 0)
            throw new ArgumentException("At least one dependent variable selector must be provided.", nameof(values));
        if (minNewTrendSegments < 1)
            throw new ArgumentOutOfRangeException(nameof(minNewTrendSegments), "Must be >= 1.");

        var n = data.Count;
        switch (n)
        {
            case 0: return [];
            case 1: return [data[0]];
        }

        var result = new List<T>(Math.Min(n, 64)) { data[0] };

        // Cache t and value results to avoid repeated delegate invocations
        var tCache = new double[n];
        var vCache = new double[values.Length, n];
        for (var i = 0; i < n; i++)
        {
            tCache[i] = t(data[i]);
            for (var v = 0; v < values.Length; v++)
                vCache[v, i] = values[v](data[i]);
        }

        // "Candidate change" bookkeeping
        int candidateIndex = -1;      // i where a change was first noticed
        int confirmCount = 0;         // how many consecutive segments the new slope has matched

        for (var i = 1; i < n - 1; i++)
        {
            var tPrev = tCache[i - 1];
            var tCurr = tCache[i];
            var tNext = tCache[i + 1];

            var dt1 = tCurr - tPrev;
            var dt2 = tNext - tCurr;

            // Any non-increasing/zero dt breaks assumptions: keep the point and reset state
            if (dt1 <= 0.0 || dt2 <= 0.0)
            {
                if (candidateIndex != -1)
                {
                    // Commit the previous run end before forcing this keep
                    result.Add(data[candidateIndex]);
                    candidateIndex = -1;
                    confirmCount = 0;
                }
                result.Add(data[i]);
                continue;
            }

            bool consecutiveSlopesEqual = true;
            for (var v = 0; v < values.Length; v++)
            {
                var s1Scaled = (vCache[v, i] - vCache[v, i - 1]) * dt2;     // (v_i - v_{i-1}) * dt2
                var s2Scaled = (vCache[v, i + 1] - vCache[v, i]) * dt1;     // (v_{i+1} - v_i) * dt1
                if (!NearlyEqual(s1Scaled, s2Scaled, tol))
                {
                    consecutiveSlopesEqual = false;
                    break;
                }
            }

            if (consecutiveSlopesEqual)
            {
                // No change at i. If we were *verifying* a new trend, increment confirmation.
                if (candidateIndex != -1)
                {
                    confirmCount++;
                    if (confirmCount >= (minNewTrendSegments - 1))
                    {
                        // New trend has persisted: commit the end of the previous run.
                        // That endpoint is the original candidate index (i when we first saw the change).
                        result.Add(data[candidateIndex]);
                        candidateIndex = -1;
                        confirmCount = 0;
                    }
                }
                // else: still within the same run; do nothing
            }
            else
            {
                // Change detected at i.
                // Start (or restart) a candidate; we will only commit it if the new trend persists.
                candidateIndex = i;    // i is the last point of the *previous* run
                confirmCount = 0;
            }
        }

        // If a change began near the end but didn't confirm, we simply don't add it.
        // Always keep the last point.
        result.Add(data[n - 1]);
        return result;

        static bool NearlyEqual(double a, double b, double tolLocal)
        {
            var scale = Math.Abs(a) + Math.Abs(b) + 1.0;
            return Math.Abs(a - b) <= tolLocal * scale;
        }
    }
}