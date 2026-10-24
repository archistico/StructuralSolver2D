using StructuralSolver2D.Analysis.Dynamics.Reduction;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Dynamics.Modal;

/// <summary>
/// Experimental M55 eigenpair in internal kN-m-s units. The full vector follows
/// original model node order [Ux,Uy,Rz]. Shape sign and amplitude have no physical
/// meaning: vectors are normalized so qᵀ M q = 1.
/// </summary>
internal sealed record ModalEigenpair(
    int ModeNumber,
    double EigenvalueRadiansSquaredPerSecondSquared,
    double AngularFrequencyRadiansPerSecond,
    double FrequencyHertz,
    double PeriodSeconds,
    double RayleighQuotient,
    double RelativeResidual,
    double[] ReducedMassNormalizedShape,
    double[] GlobalMassNormalizedShape);

internal sealed record ExperimentalModalSpectrum(
    int AvailableModeCount, IReadOnlyList<ModalEigenpair> Modes,
    double MaxMassOrthogonalityError,
    double MaxStiffnessOrthogonalityError);

/// <summary>
/// Dense self-contained symmetric generalized eigensolver for small M54-ready
/// positive definite pairs. Diagonally equilibrates M, applies Cholesky and
/// orthogonal cyclic Jacobi rotations. No inverse of M, fake masses, generalized
/// Gaussian elimination, static Schur condensation, or public modal service.
/// M56 independent convergence and analytical acceptance remains mandatory.
/// </summary>
internal static class SymmetricGeneralizedModalSolver
{
    private const double SymmetryTolerance = 1e-10;
    private const double CholeskyMinimumPivot = 1e-12;
    private const double EigenMinimumRelative = 1e-12;
    private const double JacobiStopRelative = 1e-13;
    private const double MaximumRelativeResidual = 5e-7;
    private const double MaximumOrthogonalityError = 5e-7;
    private const int MaximumJacobiSweeps = 80;

    internal static ExperimentalModalSpectrum AssembleAndSolve(StructuralModel model,
        StructuralDynamicsDefinition dynamics, int requestedModeCount = 3) =>
        Solve(DynamicBoundaryConditionReducer.AssembleAndReduce(model, dynamics), requestedModeCount);

    internal static ExperimentalModalSpectrum Solve(DynamicReductionResult reduced, int requestedModeCount = 3)
    {
        ArgumentNullException.ThrowIfNull(reduced);
        if (requestedModeCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedModeCount), "M55_MODE_COUNT: Must be positive.");
        if (!reduced.ReadyForModalSolver)
            throw new InvalidOperationException($"M55_NOT_READY ({reduced.DiagnosticCode}): {reduced.Message}");

        double[,] k = reduced.Stiffness, m = reduced.Mass, z = reduced.Basis;
        int n = k.GetLength(0);
        if (n < 1 || n > DynamicBoundaryConditionReducer.MaximumReducedDofs ||
            k.GetLength(1) != n || m.GetLength(0) != n || m.GetLength(1) != n ||
            z.GetLength(1) != n || z.GetLength(0) % 3 != 0)
            throw new InvalidOperationException("M55_DIMENSIONS: Invalid reduced pair or global basis dimensions.");

        double[] inverseMassSqrt = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (!double.IsFinite(m[i, i]) || m[i, i] <= 0)
                throw new InvalidOperationException("M55_MASS_NOT_POSITIVE: Positive diagonal inertia required.");
            inverseMassSqrt[i] = 1.0 / Math.Sqrt(m[i, i]);
            if (!double.IsFinite(inverseMassSqrt[i]))
                throw new InvalidOperationException("M55_MASS_SCALE: Mass cannot be equilibrated.");
        }

        // Mbar = D M D and Kbar = D K D, Dii = 1/sqrt(Mii).
        // Eigenvalues of (Kbar,Mbar) are unchanged; Mbar has unit diagonal.
        var mbar = new double[n, n];
        var kbar = new double[n, n];
        double stiffnessScale = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (!double.IsFinite(k[i, j]) || !double.IsFinite(m[i, j]))
                    throw new InvalidOperationException("M55_NONFINITE: Non-finite input matrix.");
                if (Math.Abs(k[i, j] - k[j, i]) > SymmetryTolerance * Math.Max(Math.Abs(k[i, j]), Math.Abs(k[j, i])) ||
                    Math.Abs(m[i, j] - m[j, i]) > SymmetryTolerance * Math.Max(Math.Abs(m[i, j]), Math.Abs(m[j, i])))
                    throw new InvalidOperationException("M55_NONSYMMETRIC: Matrices must be symmetric.");
                mbar[i, j] = (m[i, j] * inverseMassSqrt[i]) * inverseMassSqrt[j];
                kbar[i, j] = (k[i, j] * inverseMassSqrt[i]) * inverseMassSqrt[j];
                if (!double.IsFinite(mbar[i, j]) || !double.IsFinite(kbar[i, j]))
                    throw new InvalidOperationException("M55_EQUILIBRATION_OVERFLOW: Matrix scaling is not finite.");
                stiffnessScale = Math.Max(stiffnessScale, Math.Abs(kbar[i, j]));
            }
        if (!(stiffnessScale > 0) || !double.IsFinite(stiffnessScale))
            throw new InvalidOperationException("M55_NO_STIFFNESS: All stiffness is zero.");
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++) kbar[i, j] /= stiffnessScale;

        // Cholesky: Mbar = L Lᵀ. Reject ill-conditioned or non-positive Mbar.
        var lower = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j <= i; j++)
            {
                double entry = mbar[i, j];
                for (int p = 0; p < j; p++) entry -= lower[i, p] * lower[j, p];
                if (i == j)
                {
                    if (!double.IsFinite(entry) || entry <= CholeskyMinimumPivot)
                        throw new InvalidOperationException("M55_MASS_NOT_SPD: Nonpositive or numerically singular Cholesky pivot.");
                    lower[i, j] = Math.Sqrt(entry);
                }
                else lower[i, j] = entry / lower[j, j];
            }

        // T = L^-1 (internal triangular solves), B = T Kbar Tᵀ, symmetric.
        var inverseLower = new double[n, n];
        for (int col = 0; col < n; col++)
            for (int row = 0; row < n; row++)
            {
                double entry = row == col ? 1 : 0;
                for (int j = 0; j < row; j++) entry -= lower[row, j] * inverseLower[j, col];
                inverseLower[row, col] = entry / lower[row, row];
            }
        var first = Multiply(inverseLower, kbar);
        var b = Multiply(first, Transpose(inverseLower));
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                double v = (b[i, j] + b[j, i]) / 2;
                b[i, j] = b[j, i] = v;
            }
        var y = JacobiDiagonalize(b); // Columns are standard orthonormal eigenvectors.
        var order = Enumerable.Range(0, n).OrderBy(i => b[i, i]).ThenBy(i => i).ToArray();
        double maxEigenNormalized = order.Max(i => Math.Abs(b[i, i]));
        int count = Math.Min(requestedModeCount, n);
        var modes = new List<ModalEigenpair>(count);
        for (int index = 0; index < count; index++)
        {
            int col = order[index];
            double eigenScaled = b[col, col];
            if (!double.IsFinite(eigenScaled) || eigenScaled <= EigenMinimumRelative * maxEigenNormalized)
                throw new InvalidOperationException("M55_NONPOSITIVE_MODE: A rigid mode, mechanism or unresolved tiny eigenvalue was found; no frequency is invented.");
            double eigenvalue = eigenScaled * stiffnessScale;
            if (!double.IsFinite(eigenvalue) || eigenvalue <= 0)
                throw new InvalidOperationException("M55_EIGENVALUE_RANGE: Positive frequency cannot be represented.");
            var q = new double[n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++) q[i] += inverseLower[j, i] * y[j, col];
            for (int i = 0; i < n; i++) q[i] *= inverseMassSqrt[i];
            double modalMass = Quadratic(m, q);
            if (!double.IsFinite(modalMass) || modalMass <= 0)
                throw new InvalidOperationException("M55_NORMALIZATION: Nonpositive modal mass.");
            for (int i = 0; i < n; i++) q[i] /= Math.Sqrt(modalMass);
            var global = new double[z.GetLength(0)];
            for (int i = 0; i < global.Length; i++)
                for (int j = 0; j < n; j++) global[i] += z[i, j] * q[j];
            // Deterministic sign, but do not promise unique basis in degenerate eigenspaces.
            int anchor = 0;
            for (int i = 1; i < global.Length; i++)
                if (Math.Abs(global[i]) > Math.Abs(global[anchor])) anchor = i;
            if (global[anchor] < 0)
            {
                for (int i = 0; i < n; i++) q[i] = -q[i];
                for (int i = 0; i < global.Length; i++) global[i] = -global[i];
            }
            double rayleigh = RayleighQuotient(k, m, q);
            double residual = RelativeResidual(k, m, q, eigenvalue);
            if (!double.IsFinite(residual) || residual > MaximumRelativeResidual ||
                Math.Abs(rayleigh - eigenvalue) > MaximumRelativeResidual * eigenvalue)
                throw new InvalidOperationException($"M55_EIGENPAIR_RESIDUAL: Mode {index + 1} failed Rayleigh/residual checks ({residual:G5}).");
            double omega = Math.Sqrt(eigenvalue);
            double frequency = omega / (2 * Math.PI);
            if (!double.IsFinite(frequency) || frequency <= 0)
                throw new InvalidOperationException("M55_FREQUENCY_RANGE: Invalid calculated frequency.");
            double period = 1 / frequency;
            if (!double.IsFinite(period) || period <= 0)
                throw new InvalidOperationException("M55_PERIOD_RANGE: Invalid calculated natural period.");
            modes.Add(new ModalEigenpair(index + 1, eigenvalue, omega, frequency,
                period, rayleigh, residual, q, global));
        }

        double maxMassError = 0, maxStiffnessError = 0;
        for (int i = 0; i < modes.Count; i++)
            for (int j = i; j < modes.Count; j++)
            {
                var pi = modes[i].ReducedMassNormalizedShape;
                var pj = modes[j].ReducedMassNormalizedShape;
                double massOverlap = Bilinear(m, pi, pj);
                double stiffOverlap = Bilinear(k, pi, pj);
                double expectedMass = i == j ? 1 : 0;
                double expectedStiffness = i == j ? modes[i].EigenvalueRadiansSquaredPerSecondSquared : 0;
                maxMassError = Math.Max(maxMassError, Math.Abs(massOverlap - expectedMass));
                maxStiffnessError = Math.Max(maxStiffnessError,
                    Math.Abs(stiffOverlap - expectedStiffness) /
                    Math.Max(modes[i].EigenvalueRadiansSquaredPerSecondSquared,
                             modes[j].EigenvalueRadiansSquaredPerSecondSquared));
            }
        if (!double.IsFinite(maxMassError) || !double.IsFinite(maxStiffnessError) ||
            maxMassError > MaximumOrthogonalityError || maxStiffnessError > MaximumOrthogonalityError)
            throw new InvalidOperationException("M55_ORTHOGONALITY: Modal vectors failed generalized orthogonality checks.");
        return new ExperimentalModalSpectrum(n, modes, maxMassError, maxStiffnessError);
    }

    internal static double RayleighQuotient(double[,] k, double[,] m, double[] q)
    {
        ArgumentNullException.ThrowIfNull(k);
        ArgumentNullException.ThrowIfNull(m);
        ArgumentNullException.ThrowIfNull(q);
        int n = q.Length;
        if (n == 0 || k.GetLength(0) != n || k.GetLength(1) != n ||
            m.GetLength(0) != n || m.GetLength(1) != n)
            throw new ArgumentException("M55_RAYLEIGH_DIMENSIONS: Vectors and matrices must match.");
        double numerator = Quadratic(k, q), denominator = Quadratic(m, q);
        if (!double.IsFinite(numerator) || !double.IsFinite(denominator) || denominator <= 0)
            throw new InvalidOperationException("M55_RAYLEIGH_MASS: Invalid modal mass or energy.");
        return numerator / denominator;
    }

    private static double RelativeResidual(double[,] k, double[,] m, double[] q, double lambda)
    {
        double sumR = 0, sumK = 0, sumM = 0;
        for (int i = 0; i < q.Length; i++)
        {
            double ki = 0, mi = 0;
            for (int j = 0; j < q.Length; j++)
            {
                ki += k[i, j] * q[j];
                mi += m[i, j] * q[j];
            }
            double lm = lambda * mi;
            sumR += (ki - lm) * (ki - lm);
            sumK += ki * ki;
            sumM += lm * lm;
        }
        double denominator = Math.Sqrt(sumK) + Math.Sqrt(sumM);
        return denominator > 0 ? Math.Sqrt(sumR) / denominator : double.PositiveInfinity;
    }

    private static double Bilinear(double[,] a, double[] u, double[] v)
    {
        double result = 0;
        for (int i = 0; i < u.Length; i++)
            for (int j = 0; j < v.Length; j++) result += u[i] * a[i, j] * v[j];
        return result;
    }
    private static double Quadratic(double[,] a, double[] q) => Bilinear(a, q, q);
    private static double[,] Transpose(double[,] a)
    {
        int rows = a.GetLength(0), cols = a.GetLength(1);
        var t = new double[cols, rows];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++) t[j, i] = a[i, j];
        return t;
    }
    private static double[,] Multiply(double[,] a, double[,] b)
    {
        int rows = a.GetLength(0), width = a.GetLength(1), cols = b.GetLength(1);
        if (b.GetLength(0) != width) throw new ArgumentException("Incompatible matrix product.");
        var result = new double[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int p = 0; p < width; p++)
                for (int j = 0; j < cols; j++) result[i, j] += a[i, p] * b[p, j];
        return result;
    }
    private static double[,] JacobiDiagonalize(double[,] a)
    {
        int n = a.GetLength(0);
        var v = new double[n, n];
        for (int i = 0; i < n; i++) v[i, i] = 1;
        for (int sweep = 0; sweep < MaximumJacobiSweeps; sweep++)
        {
            double diagonalSquared = 0, offSquared = 0;
            for (int i = 0; i < n; i++)
            {
                diagonalSquared += a[i, i] * a[i, i];
                for (int j = i + 1; j < n; j++) offSquared += 2 * a[i, j] * a[i, j];
            }
            if (Math.Sqrt(offSquared) <= JacobiStopRelative * Math.Sqrt(diagonalSquared))
                return v;
            for (int p = 0; p < n; p++)
                for (int q = p + 1; q < n; q++)
                {
                    double apq = a[p, q];
                    if (apq == 0) continue;
                    double tau = (a[q, q] - a[p, p]) / (2 * apq);
                    double t = (tau >= 0 ? 1.0 : -1.0) /
                        (Math.Abs(tau) + Math.Sqrt(1 + tau * tau));
                    double c = 1.0 / Math.Sqrt(1 + t * t), s = t * c;
                    double oldP = a[p, p], oldQ = a[q, q];
                    a[p, p] = oldP - t * apq;
                    a[q, q] = oldQ + t * apq;
                    a[p, q] = a[q, p] = 0;
                    for (int r = 0; r < n; r++)
                    {
                        if (r != p && r != q)
                        {
                            double rp = a[r, p], rq = a[r, q];
                            a[r, p] = a[p, r] = c * rp - s * rq;
                            a[r, q] = a[q, r] = s * rp + c * rq;
                        }
                        double vp = v[r, p], vq = v[r, q];
                        v[r, p] = c * vp - s * vq;
                        v[r, q] = s * vp + c * vq;
                    }
                }
        }
        throw new InvalidOperationException("M55_JACOBI_NOT_CONVERGED: Symmetric eigenanalysis exceeded sweep limit.");
    }
}
