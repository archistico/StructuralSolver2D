namespace StructuralSolver2D.Analysis.Dynamics.Reduction;

/// <summary>
/// Diagnostic rank/PSD test for small symmetric matrices. Diagonal equilibration
/// and diagonal-pivoted Cholesky are used ONLY to classify admissibility: this is
/// not the generalized eigensolver or a structural safety/condition-number proof.
/// </summary>
internal sealed record SymmetricMatrixRankDiagnostics(
    int Rank, int Nullity, bool Indefinite, bool Invalid,
    double PivotRatio);

internal static class SymmetricMatrixRankInspector
{
    private const double SymmetryTolerance = 1e-10;
    private const double NullPivotTolerance = 1e-10;

    internal static SymmetricMatrixRankDiagnostics Inspect(double[,] original)
    {
        int n = original.GetLength(0);
        if (original.GetLength(1) != n)
            return new(0, n, false, true, double.NaN);
        if (n == 0) return new(0, 0, false, false, 1);
        double maxEntry = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (!double.IsFinite(original[i, j]))
                    return new(0, n, false, true, double.NaN);
                maxEntry = Math.Max(maxEntry, Math.Abs(original[i, j]));
            }
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (Math.Abs(original[i, j] - original[j, i]) > SymmetryTolerance * maxEntry)
                    return new(0, n, false, true, double.NaN);
        if (maxEntry == 0) return new(0, n, false, false, 0);

        // A negative diagonal cannot occur in a positive semidefinite matrix.
        var scales = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (original[i, i] < -SymmetryTolerance * maxEntry)
                return new(0, n, true, false, 0);
            scales[i] = original[i, i] > 0 ? Math.Sqrt(original[i, i]) : 0;
        }
        // For a PSD matrix, a zero diagonal implies an identically zero row.
        // Do not add a fake diagonal mass or stiffness to conceal such coordinates.
        var active = new List<int>();
        for (int i = 0; i < n; i++)
        {
            if (scales[i] > 0) { active.Add(i); continue; }
            for (int j = 0; j < n; j++)
                if (Math.Abs(original[i, j]) > SymmetryTolerance * maxEntry)
                    return new(0, n, true, false, 0);
        }
        int count = active.Count;
        if (count == 0) return new(0, n, false, false, 0);
        var schur = new double[count, count];
        for (int i = 0; i < count; i++)
            for (int j = 0; j < count; j++)
                schur[i, j] = (original[active[i], active[j]] / scales[active[i]]) / scales[active[j]];
        if (Enumerable.Range(0, count).Any(i =>
                Enumerable.Range(0, count).Any(j => !double.IsFinite(schur[i, j]))))
            return new(0, n, false, true, double.NaN);

        // Complete diagonal pivoting of a PSD Schur complement. A small residual
        // pivot flags null/near-null direction and is intentionally conservative.
        var used = new bool[count];
        int rank = 0;
        double minPivot = double.PositiveInfinity, maxPivot = 0;
        for (int step = 0; step < count; step++)
        {
            int pivot = -1;
            double best = double.NegativeInfinity;
            for (int i = 0; i < count; i++)
                if (!used[i] && schur[i, i] > best) { best = schur[i, i]; pivot = i; }
            if (best < -NullPivotTolerance)
                return new(rank, n - rank, true, false, 0);
            if (best <= NullPivotTolerance) break;
            used[pivot] = true;
            rank++;
            minPivot = Math.Min(minPivot, best);
            maxPivot = Math.Max(maxPivot, best);
            for (int i = 0; i < count; i++)
            {
                if (used[i]) continue;
                for (int j = i; j < count; j++)
                {
                    if (used[j]) continue;
                    double updated = schur[i, j] - schur[i, pivot] * schur[pivot, j] / best;
                    schur[i, j] = schur[j, i] = updated;
                }
            }
        }
        return new(rank, n - rank, false, false,
            rank == 0 || rank < n ? 0 : minPivot / maxPivot);
    }
}
