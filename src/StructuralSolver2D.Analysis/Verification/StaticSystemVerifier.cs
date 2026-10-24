namespace StructuralSolver2D.Analysis.Verification;

/// <summary>
/// Independent, non-public post-solve checks for a homogeneous-constrained static FEM system.
/// The caller must supply the FULL assembled equivalent-nodal load vector (not only nodal forces).
/// Diagnostic quantities refer to the original, unaugmented global stiffness system.
/// </summary>
internal static class StaticSystemVerifier
{
    internal const double RelativeTolerance = 2e-8;
    internal const double AbsoluteEnergyTolerance = 1e-9; // kNm
    internal const double AbsoluteResidualTolerance = 1e-8; // generalized kN / kNm components
    internal const double AbsoluteConstraintTolerance = 1e-9; // generalized displacement components

    internal static StaticVerificationResult Verify(
        double[,] stiffness,
        double[] loads,
        double[] displacements,
        IReadOnlyList<double[]> homogeneousConstraintRows)
    {
        ArgumentNullException.ThrowIfNull(stiffness);
        ArgumentNullException.ThrowIfNull(loads);
        ArgumentNullException.ThrowIfNull(displacements);
        ArgumentNullException.ThrowIfNull(homogeneousConstraintRows);

        int n = loads.Length;
        if (stiffness.GetLength(0) != n || stiffness.GetLength(1) != n || displacements.Length != n)
            throw new ArgumentException("Stiffness, load and displacement dimensions must agree.");
        if (homogeneousConstraintRows.Any(row => row is null || row.Length != n))
            throw new ArgumentException("Every constraint row must have the global DOF count.", nameof(homogeneousConstraintRows));

        double stiffnessScale = 0;
        double maxSymmetryError = 0;
        for (int i = 0; i < n; i++)
        {
            if (!double.IsFinite(loads[i]) || !double.IsFinite(displacements[i]))
                return StaticVerificationResult.Failure("Nonfinite load or displacement.");
            for (int j = 0; j < n; j++)
            {
                double kij = stiffness[i, j];
                if (!double.IsFinite(kij))
                    return StaticVerificationResult.Failure("Nonfinite stiffness entry.");
                stiffnessScale = Math.Max(stiffnessScale, Math.Abs(kij));
                maxSymmetryError = Math.Max(maxSymmetryError, Math.Abs(kij - stiffness[j, i]));
            }
        }
        if (homogeneousConstraintRows.SelectMany(row => row).Any(value => !double.IsFinite(value)))
            return StaticVerificationResult.Failure("Nonfinite constraint direction.");

        // The load vector must include the same release-condensed equivalent member loads as the solve.
        double[] ku = Multiply(stiffness, displacements);
        double[] residual = new double[n];
        for (int i = 0; i < n; i++) residual[i] = ku[i] - loads[i];
        double twiceEnergy = Dot(displacements, ku);
        double externalWork = Dot(displacements, loads);
        double energyError = Math.Abs(twiceEnergy - externalWork);
        // Scale by absolute contributions, so cancellation cannot make a large erroneous work invisible.
        double energyScale = 0;
        for (int i = 0; i < n; i++)
            energyScale += Math.Abs(displacements[i] * ku[i]) + Math.Abs(displacements[i] * loads[i]);

        if (!double.IsFinite(twiceEnergy) || !double.IsFinite(externalWork) ||
            !double.IsFinite(energyError) || !double.IsFinite(energyScale) ||
            ku.Any(value => !double.IsFinite(value)) || residual.Any(value => !double.IsFinite(value)))
            return StaticVerificationResult.Failure("Nonfinite derived energy or equilibrium value (possible overflow).");

        // Homogeneous constraints C u = 0; remove reaction-space components from K u - F.
        // Reorthogonalize to handle repeated/redundant supports without rejecting their directions.
        List<double[]> reactionSpaceBasis = new();
        foreach (double[] constraint in homogeneousConstraintRows)
        {
            double[] q = (double[])constraint.Clone();
            for (int pass = 0; pass < 2; pass++)
                foreach (double[] existing in reactionSpaceBasis)
                {
                    double projection = Dot(q, existing);
                    for (int i = 0; i < n; i++) q[i] -= projection * existing[i];
                }
            double norm = Math.Sqrt(Dot(q, q));
            if (norm <= 1e-11) continue;
            for (int i = 0; i < n; i++) q[i] /= norm;
            reactionSpaceBasis.Add(q);
        }

        double[] freeResidual = (double[])residual.Clone();
        double maxConstraintError = 0;
        foreach (double[] q in reactionSpaceBasis)
        {
            maxConstraintError = Math.Max(maxConstraintError, Math.Abs(Dot(q, displacements)));
            double multiplier = Dot(q, residual);
            for (int i = 0; i < n; i++) freeResidual[i] -= multiplier * q[i];
        }
        double maxFreeResidual = MaxAbs(freeResidual);
        double forceScale = MaxAbs(ku) + MaxAbs(loads);
        double displacementScale = MaxAbs(displacements);

        if (!double.IsFinite(maxConstraintError) || !double.IsFinite(maxFreeResidual) ||
            !double.IsFinite(forceScale) || !double.IsFinite(displacementScale))
            return StaticVerificationResult.Failure("Nonfinite projected constraint/equilibrium residual.");

        bool symmetryPassed = maxSymmetryError <= RelativeTolerance * stiffnessScale + 1e-12;
        bool energyPassed = energyError <= AbsoluteEnergyTolerance + RelativeTolerance * energyScale;
        bool equilibriumPassed = maxFreeResidual <= AbsoluteResidualTolerance + RelativeTolerance * forceScale;
        bool constraintsPassed = maxConstraintError <= AbsoluteConstraintTolerance + RelativeTolerance * displacementScale;
        return new StaticVerificationResult(
            symmetryPassed && energyPassed && equilibriumPassed && constraintsPassed,
            0.5 * twiceEnergy, externalWork, energyError,
            maxSymmetryError, maxFreeResidual, maxConstraintError,
            symmetryPassed, energyPassed, equilibriumPassed, constraintsPassed,
            symmetryPassed && energyPassed && equilibriumPassed && constraintsPassed ? null :
                $"Static verification failed: symmetric={symmetryPassed}, energy={energyPassed}, free equilibrium={equilibriumPassed}, constraints={constraintsPassed}.");
    }

    private static double[] Multiply(double[,] a, double[] x)
    {
        double[] result = new double[x.Length];
        for (int i = 0; i < x.Length; i++)
            for (int j = 0; j < x.Length; j++) result[i] += a[i, j] * x[j];
        return result;
    }

    private static double Dot(double[] x, double[] y)
    {
        double result = 0;
        for (int i = 0; i < x.Length; i++) result += x[i] * y[i];
        return result;
    }

    private static double MaxAbs(double[] x) => x.Length == 0 ? 0 : x.Max(Math.Abs);
}

/// <summary>All residuals are absolute and in the problem's coherent generalized units.</summary>
internal sealed record StaticVerificationResult(
    bool Passed,
    double StrainEnergy,
    double ExternalWork,
    double EnergyBalanceError,
    double MaxSymmetryError,
    double MaxFreeEquationResidual,
    double MaxConstraintViolation,
    bool SymmetryPassed,
    bool EnergyPassed,
    bool EquilibriumPassed,
    bool ConstraintsPassed,
    string? Diagnostic)
{
    internal static StaticVerificationResult Failure(string message) =>
        new(false, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN,
            false, false, false, false, message);
}
