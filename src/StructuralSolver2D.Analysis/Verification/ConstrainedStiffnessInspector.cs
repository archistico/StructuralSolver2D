namespace StructuralSolver2D.Analysis.Verification;

/// <summary>
/// Optional pre-solve investigation of the constrained stiffness on its kinematically
/// admissible subspace. No decisions in the production solver depend on this heuristic.
/// </summary>
internal static class ConstrainedStiffnessInspector
{
    // An eigenspectrum is intentionally restricted to small dense teaching models.
    internal const int MaximumFreeDofs = 60;
    private const double ConstraintOrthogonalityTolerance = 1e-12; // same order as support-basis deduplication
    private const double SymmetryTolerance = 1e-9;
    private const double PossibleMechanismRatio = 1e-10;
    private const double PoorConditioningRatio = 1e-5;

    internal static StiffnessInspectionResult Inspect(
        double[,] stiffness,
        IReadOnlyList<double[]> homogeneousConstraintRows,
        IReadOnlyList<string>? dofLabels = null)
    {
        ArgumentNullException.ThrowIfNull(stiffness);
        ArgumentNullException.ThrowIfNull(homogeneousConstraintRows);
        int n = stiffness.GetLength(0);
        if (stiffness.GetLength(1) != n || homogeneousConstraintRows.Any(row => row is null || row.Length != n))
            throw new ArgumentException("Stiffness matrix and support constraint dimensions must agree.");
        if (dofLabels is not null && dofLabels.Count != n)
            throw new ArgumentException("The DOF label count must match the stiffness size.", nameof(dofLabels));

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                if (!double.IsFinite(stiffness[i, j]))
                    return New(StiffnessInspectionStatus.InvalidInput, 0, 0, "Non-finite stiffness entry.");
        }
        if (homogeneousConstraintRows.SelectMany(row => row).Any(x => !double.IsFinite(x)))
            return New(StiffnessInspectionStatus.InvalidInput, 0, 0, "Non-finite constraint coefficient.");

        double largestEntry = 0, asymmetry = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                largestEntry = Math.Max(largestEntry, Math.Abs(stiffness[i, j]));
                asymmetry = Math.Max(asymmetry, Math.Abs(stiffness[i, j] - stiffness[j, i]));
            }
        if (asymmetry > SymmetryTolerance * largestEntry)
            return New(StiffnessInspectionStatus.InvalidInput, 0, 0, "Stiffness matrix is not symmetric.");

        var constraints = new List<double[]>();
        foreach (double[] row in homogeneousConstraintRows)
        {
            double[] candidate = (double[])row.Clone();
            double length = Norm(candidate);
            if (length == 0) continue;
            for (int j = 0; j < n; j++) candidate[j] /= length;
            Reorthogonalize(candidate, constraints);
            length = Norm(candidate);
            if (length <= ConstraintOrthogonalityTolerance) continue; // repeated/inclined redundant supports
            for (int j = 0; j < n; j++) candidate[j] /= length;
            constraints.Add(candidate);
        }
        int redundantRows = homogeneousConstraintRows.Count - constraints.Count;
        int freeCount = n - constraints.Count;
        if (freeCount == 0)
            return New(StiffnessInspectionStatus.FullyConstrained, 0, redundantRows,
                "All degrees of freedom are restrained; there is no free stiffness spectrum.");
        if (freeCount > MaximumFreeDofs)
            return New(StiffnessInspectionStatus.TooLarge, freeCount, redundantRows,
                $"Opt-in dense spectrum limited to {MaximumFreeDofs} free DOFs; static solving remains available.");

        // Orthonormal nullspace Z of C (not a principal minor: supports may be inclined).
        var freeBasis = new List<double[]>(freeCount);
        for (int axis = 0; axis < n; axis++)
        {
            var candidate = new double[n];
            candidate[axis] = 1;
            Reorthogonalize(candidate, constraints);
            Reorthogonalize(candidate, freeBasis);
            double length = Norm(candidate);
            if (length <= ConstraintOrthogonalityTolerance) continue;
            for (int j = 0; j < n; j++) candidate[j] /= length;
            freeBasis.Add(candidate);
        }
        if (freeBasis.Count != freeCount)
            return New(StiffnessInspectionStatus.NotConverged, freeCount, redundantRows,
                "Could not construct a numerically independent admissible displacement basis.");

        // Project K onto admissible displacements: Kr = Z^T K Z.
        var kz = new double[freeCount][];
        for (int col = 0; col < freeCount; col++)
        {
            kz[col] = new double[n];
            for (int row = 0; row < n; row++)
                for (int j = 0; j < n; j++) kz[col][row] += stiffness[row, j] * freeBasis[col][j];
        }
        var reduced = new double[freeCount, freeCount];
        var diagonalRoots = new double[freeCount];
        for (int row = 0; row < freeCount; row++)
        {
            for (int col = 0; col < freeCount; col++)
                reduced[row, col] = Dot(freeBasis[row], kz[col]);
            if (!double.IsFinite(reduced[row, row]))
                return New(StiffnessInspectionStatus.InvalidInput, freeCount, redundantRows, "Non-finite projected stiffness.");
            if (reduced[row, row] <= 0)
            {
                string location = DominantLabel(freeBasis[row], dofLabels);
                return new(StiffnessInspectionStatus.PossibleMechanism, freeCount, redundantRows, null,
                    null, location, "A free displacement direction has non-positive diagonal stiffness; probable mechanism or invalid stiffness.");
            }
            diagonalRoots[row] = Math.Sqrt(reduced[row, row]);
        }

        // Dimensionless diagonal equilibration: S = D^-1/2 Kr D^-1/2.
        // Its eigenvalue ratio is a CONDITION INDICATOR, not cond(K) in physical units.
        var normalized = new double[freeCount, freeCount];
        for (int i = 0; i < freeCount; i++)
            for (int j = 0; j < freeCount; j++)
            {
                normalized[i, j] = (reduced[i, j] / diagonalRoots[i]) / diagonalRoots[j];
                if (!double.IsFinite(normalized[i, j]))
                    return New(StiffnessInspectionStatus.InvalidInput, freeCount, redundantRows,
                        "The normalized reduced stiffness overflows; check incompatible physical scales.");
            }

        if (!TryEigenDecomposition(normalized, out double[] eigenvalues, out double[,] eigenvectors))
            return New(StiffnessInspectionStatus.NotConverged, freeCount, redundantRows,
                "Symmetric eigensolver did not converge; this is not a stability verdict.");
        int minIndex = 0;
        for (int i = 1; i < freeCount; i++)
            if (eigenvalues[i] < eigenvalues[minIndex]) minIndex = i;
        double min = eigenvalues[minIndex];
        double max = eigenvalues.Max();
        double ratio = max > 0 ? min / max : double.NaN;

        // Recover a candidate displacement direction for explaining the critical mode.
        var mode = new double[n];
        for (int j = 0; j < freeCount; j++)
        {
            double coefficient = eigenvectors[j, minIndex] / diagonalRoots[j];
            for (int i = 0; i < n; i++) mode[i] += coefficient * freeBasis[j][i];
        }
        string dominant = DominantLabel(mode, dofLabels);
        StiffnessInspectionStatus status = min < -PossibleMechanismRatio * max
            ? StiffnessInspectionStatus.Indefinite
            : ratio <= PossibleMechanismRatio
                ? StiffnessInspectionStatus.PossibleMechanism
                : ratio < PoorConditioningRatio
                    ? StiffnessInspectionStatus.PoorlyConditioned
                    : StiffnessInspectionStatus.Healthy;
        string message = status switch
        {
            StiffnessInspectionStatus.Indefinite => "Negative stiffness eigenvalue: check element assembly, releases and material properties.",
            StiffnessInspectionStatus.PossibleMechanism => "Zero or extremely small reduced stiffness eigenvalue: inspect restraints and internal mechanisms.",
            StiffnessInspectionStatus.PoorlyConditioned => "Small normalized stiffness eigenvalue: results may be sensitive to geometry or numerical errors.",
            _ => "No mechanism detected at the chosen numerical resolution."
        };
        return new(status, freeCount, redundantRows, min, ratio, dominant, message);
    }

    private static StiffnessInspectionResult New(StiffnessInspectionStatus status, int free, int redundant, string message) =>
        new(status, free, redundant, null, null, null, message);

    private static void Reorthogonalize(double[] vector, IEnumerable<double[]> basis)
    {
        for (int pass = 0; pass < 2; pass++)
            foreach (double[] unit in basis)
            {
                double projection = Dot(vector, unit);
                for (int i = 0; i < vector.Length; i++) vector[i] -= projection * unit[i];
            }
    }

    // Scaled Euclidean norm avoids overflow/underflow for rescaled constraint equations.
    private static double Norm(double[] x)
    {
        double scale = 0;
        foreach (double value in x) scale = Math.Max(scale, Math.Abs(value));
        if (scale == 0) return 0;
        double squareSum = 0;
        foreach (double value in x)
        {
            double v = value / scale;
            squareSum += v * v;
        }
        return scale * Math.Sqrt(squareSum);
    }
    private static double Dot(double[] x, double[] y)
    {
        double sum = 0;
        for (int i = 0; i < x.Length; i++) sum += x[i] * y[i];
        return sum;
    }
    private static string DominantLabel(double[] vector, IReadOnlyList<string>? labels)
    {
        if (vector.Length == 0) return "(no free DOF)";
        int maxIndex = 0;
        for (int i = 1; i < vector.Length; i++)
            if (Math.Abs(vector[i]) > Math.Abs(vector[maxIndex])) maxIndex = i;
        return labels is null ? $"DOF {maxIndex}" : labels[maxIndex];
    }

    // Cyclic Jacobi algorithm for symmetric matrices. Eigenvectors are columns of V.
    private static bool TryEigenDecomposition(double[,] matrix, out double[] values, out double[,] vectors)
    {
        int n = matrix.GetLength(0);
        var a = (double[,])matrix.Clone();
        vectors = new double[n, n];
        for (int i = 0; i < n; i++) vectors[i, i] = 1;
        for (int sweep = 0; sweep < 60; sweep++)
        {
            double maxOffDiagonal = 0;
            for (int p = 0; p < n; p++)
                for (int q = p + 1; q < n; q++)
                    maxOffDiagonal = Math.Max(maxOffDiagonal, Math.Abs(a[p, q]));
            if (maxOffDiagonal <= 1e-13)
            {
                values = new double[n];
                for (int i = 0; i < n; i++) values[i] = a[i, i];
                return values.All(double.IsFinite);
            }
            for (int p = 0; p < n; p++)
                for (int q = p + 1; q < n; q++)
                {
                    double apq = a[p, q];
                    if (Math.Abs(apq) <= 1e-15) continue;
                    double tau = (a[q, q] - a[p, p]) / (2 * apq);
                    double t = tau == 0 ? 1 : Math.CopySign(1.0, tau) /
                        (Math.Abs(tau) + Math.Sqrt(1 + tau * tau));
                    double c = 1 / Math.Sqrt(1 + t * t);
                    double s = t * c;
                    a[p, p] -= t * apq;
                    a[q, q] += t * apq;
                    a[p, q] = a[q, p] = 0;
                    for (int r = 0; r < n; r++)
                    {
                        if (r != p && r != q)
                        {
                            double arp = a[r, p], arq = a[r, q];
                            a[r, p] = a[p, r] = c * arp - s * arq;
                            a[r, q] = a[q, r] = s * arp + c * arq;
                        }
                        double vrp = vectors[r, p], vrq = vectors[r, q];
                        vectors[r, p] = c * vrp - s * vrq;
                        vectors[r, q] = s * vrp + c * vrq;
                    }
                }
        }
        values = Array.Empty<double>();
        return false;
    }
}

internal enum StiffnessInspectionStatus
{
    Healthy,
    PoorlyConditioned,
    PossibleMechanism,
    Indefinite,
    FullyConstrained,
    TooLarge,
    NotConverged,
    InvalidInput
}

internal sealed record StiffnessInspectionResult(
    StiffnessInspectionStatus Status,
    int FreeDofCount,
    int RedundantConstraintRows,
    double? SmallestScaledEigenvalue,
    double? ScaledEigenvalueRatio,
    string? DominantDof,
    string Diagnostic);
