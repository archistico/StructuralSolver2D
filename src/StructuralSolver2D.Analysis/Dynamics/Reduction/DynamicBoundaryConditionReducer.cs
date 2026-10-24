using StructuralSolver2D.Analysis.Constraints;
using StructuralSolver2D.Analysis.Dynamics.Assembly;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Dynamics.Reduction;

internal enum DynamicReductionStatus
{
    Ready,
    FullyConstrained,
    TooLarge,
    InvalidMatrices,
    IndefiniteMass,
    IndefiniteStiffness,
    SingularMass,
    MasslessCoupledDofs,
    FreeFreeDeferred,
    UnrestrainedRigidBodyModes,
    Mechanism,
    StiffnessNearSingular,
    MassNearSingular
}

/// <summary>
/// M54-only pair of admissible, symmetric matrices. Columns of Basis map reduced
/// displacements back to original global [Ux,Uy,Rz] coordinates: u=Z*q.
/// No eigenvalues, frequencies, artificial inertia or production static changes.
/// </summary>
internal sealed record DynamicReductionResult(
    DynamicReductionStatus Status,
    string DiagnosticCode,
    string Message,
    double[,] Stiffness,
    double[,] Mass,
    double[,] Basis,
    int IndependentSupportConstraints,
    int RedundantSupportConstraints,
    IReadOnlyList<int> RemovedUnusedRotationDofs,
    int EstimatedRigidBodyModes,
    int StiffnessNullity,
    int MassNullity)
{
    internal bool ReadyForModalSolver => Status == DynamicReductionStatus.Ready;
}

/// <summary>
/// Projects independently validated M53 matrices onto the nullspace of homogeneous
/// support equations. Free-free cases, mechanisms and stiffness-coupled massless
/// coordinates remain EXPLICITLY unsupported pending the respective algorithms.
/// </summary>
internal static class DynamicBoundaryConditionReducer
{
    internal const int MaximumReducedDofs = 120;
    private const double BasisTolerance = 1e-11;
    private const double NearSingularPivotRatio = 1e-9;

    internal static DynamicReductionResult AssembleAndReduce(
        StructuralModel model, StructuralDynamicsDefinition dynamics,
        ElementMassFormulation formulation = ElementMassFormulation.Consistent) =>
        Reduce(model, DynamicMatrixAssembler.Assemble(model, dynamics, formulation));

    internal static DynamicReductionResult Reduce(StructuralModel model, DynamicMatrixAssembly assembly)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(assembly);
        int n = checked(model.Nodes.Count * 3);
        if (assembly.Stiffness.GetLength(0) != n || assembly.Stiffness.GetLength(1) != n ||
            assembly.Mass.GetLength(0) != n || assembly.Mass.GetLength(1) != n)
            throw new ArgumentException("M54_MATRIX_DIMENSIONS: Expected 3 DOFs per node.", nameof(assembly));

        var empty = new double[0, 0];
        var emptyBasis = new double[n, 0];
        DynamicReductionResult Fail(DynamicReductionStatus status, string code, string message,
            double[,]? k = null, double[,]? m = null, double[,]? z = null,
            int independent = 0, int redundant = 0, IReadOnlyList<int>? removed = null,
            int rigid = 0, int stiffnessNull = 0, int massNull = 0) =>
            new(status, code, message, k ?? empty, m ?? empty, z ?? emptyBasis,
                independent, redundant, removed ?? Array.Empty<int>(), rigid, stiffnessNull, massNull);

        var ki = SymmetricMatrixRankInspector.Inspect(assembly.Stiffness);
        var mi = SymmetricMatrixRankInspector.Inspect(assembly.Mass);
        if (ki.Invalid || mi.Invalid)
            return Fail(DynamicReductionStatus.InvalidMatrices, "M54_INVALID_MATRIX",
                "Global matrices must be finite and symmetric before constraints are applied.");
        if (ki.Indefinite || mi.Indefinite)
            return Fail(ki.Indefinite ? DynamicReductionStatus.IndefiniteStiffness : DynamicReductionStatus.IndefiniteMass,
                ki.Indefinite ? "M54_INDEFINITE_STIFFNESS" : "M54_INDEFINITE_MASS",
                "Global mass and stiffness matrices must be positive semidefinite.");

        // Same support geometry as the production static solver; never reuse its
        // augmented Lagrange-multiplier matrix as an eigenproblem.
        var supportRows = SupportConstraintSystem.GetHomogeneousConstraintRows(
            model, assembly.NodeIndices, 3, n, assembly.Stiffness, new double[n],
            includeRotationalDof: true, addInactiveDofConstraints: false);
        var supportNormal = Orthonormalize(supportRows, n);
        int rawSupportDirections = model.Supports.Sum(s => (s.RestrainedUx ? 1 : 0) +
            (s.RestrainedUy ? 1 : 0) + (s.RestrainedRz ? 1 : 0));
        int redundant = rawSupportDirections - supportNormal.Count;

        // Delete ONLY rotations that carry neither stiffness nor mass and that
        // have no coupling to another DOF. Translational zero rows are mechanisms.
        var removed = new List<int>();
        var allConstraints = new List<double[]>(supportNormal);
        for (int dof = 2; dof < n; dof += 3)
        {
            if (!HasZeroRow(assembly.Stiffness, dof) || !HasZeroRow(assembly.Mass, dof)) continue;
            var direction = Unit(n, dof);
            if (AddIndependent(direction, allConstraints)) removed.Add(dof);
        }
        var basisColumns = OrthonormalComplement(n, allConstraints);
        int reducedDofs = basisColumns.Count;
        var z = new double[n, reducedDofs];
        for (int col = 0; col < reducedDofs; col++)
            for (int row = 0; row < n; row++) z[row, col] = basisColumns[col][row];
        // Check the actual coordinate map before it can be consumed by M55.
        for (int i = 0; i < reducedDofs; i++)
        {
            for (int j = i; j < reducedDofs; j++)
                if (Math.Abs(Dot(basisColumns[i], basisColumns[j]) - (i == j ? 1.0 : 0.0)) > 1e-9)
                    return Fail(DynamicReductionStatus.InvalidMatrices, "M54_BASIS_ORTHOGONALITY",
                        "Could not construct an orthonormal dynamic coordinate basis.");
            foreach (double[] constraint in allConstraints)
                if (Math.Abs(Dot(constraint, basisColumns[i])) > 1e-9)
                    return Fail(DynamicReductionStatus.InvalidMatrices, "M54_CONSTRAINT_RESIDUAL",
                        "An admissible coordinate does not satisfy all homogeneous restraints.");
        }
        if (reducedDofs == 0)
            return Fail(DynamicReductionStatus.FullyConstrained, "M54_FULLY_CONSTRAINED",
                "Every physical displacement direction is restrained; no dynamic eigenproblem remains.",
                z: z, independent: supportNormal.Count, redundant: redundant, removed: removed);
        if (reducedDofs > MaximumReducedDofs)
            return Fail(DynamicReductionStatus.TooLarge, "M54_DENSE_LIMIT",
                $"M54 experimental dense reduction limited to {MaximumReducedDofs} free DOFs.",
                z: z, independent: supportNormal.Count, redundant: redundant, removed: removed);

        var kr = Project(assembly.Stiffness, z);
        var mr = Project(assembly.Mass, z);
        var kd = SymmetricMatrixRankInspector.Inspect(kr);
        var md = SymmetricMatrixRankInspector.Inspect(mr);
        DynamicReductionResult Report(DynamicReductionStatus status, string code, string msg,
            int rigidModes = 0) => Fail(status, code, msg, kr, mr, z, supportNormal.Count,
            redundant, removed, rigidModes, kd.Nullity, md.Nullity);
        if (kd.Invalid || md.Invalid)
            return Report(DynamicReductionStatus.InvalidMatrices, "M54_INVALID_REDUCED_MATRIX",
                "Projected matrices contain invalid or asymmetric entries.");
        if (md.Indefinite)
            return Report(DynamicReductionStatus.IndefiniteMass, "M54_INDEFINITE_MASS",
                "The admissible mass matrix is indefinite.");
        if (kd.Indefinite)
            return Report(DynamicReductionStatus.IndefiniteStiffness, "M54_INDEFINITE_STIFFNESS",
                "The admissible stiffness matrix is indefinite.");

        if (md.Nullity > 0)
        {
            // A zero-mass basis direction can still have stiffness (e.g. lumped
            // beam rotations); dropping it changes the physical eigenproblem.
            bool hasCoupled = false;
            for (int i = 0; i < reducedDofs; i++)
                if (Math.Abs(mr[i, i]) < 1e-25 && !HasZeroRow(kr, i)) hasCoupled = true;
            return hasCoupled
                ? Report(DynamicReductionStatus.MasslessCoupledDofs, "M54_MASSLESS_STIFFNESS_COUPLING",
                    "Massless but stiffness-coupled DOFs require proven algebraic condensation; educational lumping is not modal-ready.")
                : Report(DynamicReductionStatus.SingularMass, "M54_SINGULAR_MASS",
                    "The admissible mass matrix is singular; no fictitious mass or unverified DOF elimination is applied.");
        }
        if (md.PivotRatio < NearSingularPivotRatio)
            return Report(DynamicReductionStatus.MassNearSingular, "M54_MASS_NEAR_SINGULAR",
                "Very small normalized mass pivot; recheck units and modeling before eigenanalysis.");

        int rigidCount = CountAdmissibleRigidDirections(model, assembly, allConstraints);
        if (kd.Nullity > rigidCount)
            return Report(DynamicReductionStatus.Mechanism, "M54_INTERNAL_MECHANISM",
                "Additional zero-stiffness directions exist beyond admissible rigid-body motions.", rigidCount);
        if (kd.Nullity > 0)
        {
            if (model.Supports.Count == 0 && kd.Nullity == rigidCount && rigidCount > 0)
                return Report(DynamicReductionStatus.FreeFreeDeferred, "M54_FREE_FREE_DEFERRED",
                    "Free-free rigid modes detected; zero-frequency mode treatment is deferred beyond M54.", rigidCount);
            return Report(DynamicReductionStatus.UnrestrainedRigidBodyModes, "M54_RIGID_BODY_MOTION",
                "One or more rigid-body displacement directions remain unrestrained.", rigidCount);
        }
        if (kd.PivotRatio < NearSingularPivotRatio)
            return Report(DynamicReductionStatus.StiffnessNearSingular, "M54_STIFFNESS_NEAR_SINGULAR",
                "Reduced stiffness has a very small normalized pivot; modal results may be unreliable.");
        return Report(DynamicReductionStatus.Ready, "M54_READY",
            "Symmetric constrained K/M pair assembled, with positive definite mass and stiffness; ready for M55 eigenanalysis.");
    }

    private static bool HasZeroRow(double[,] matrix, int row)
    {
        for (int j = 0; j < matrix.GetLength(1); j++)
            if (matrix[row, j] != 0 || matrix[j, row] != 0) return false;
        return true;
    }

    private static double[,] Project(double[,] matrix, double[,] z)
    {
        int n = z.GetLength(0), r = z.GetLength(1);
        var result = new double[r, r];
        var az = new double[n, r];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                double aij = matrix[i, j];
                if (aij == 0) continue;
                for (int col = 0; col < r; col++) az[i, col] += aij * z[j, col];
            }
        for (int i = 0; i < r; i++)
            for (int j = i; j < r; j++)
            {
                double sum = 0;
                for (int p = 0; p < n; p++) sum += z[p, i] * az[p, j];
                result[i, j] = result[j, i] = sum;
            }
        return result;
    }

    private static int CountAdmissibleRigidDirections(StructuralModel model,
        DynamicMatrixAssembly assembly, IReadOnlyList<double[]> constraints)
    {
        int n = model.Nodes.Count * 3;
        if (model.Nodes.Count == 0) return 0;
        double cx = model.Nodes.Average(x => x.X), cy = model.Nodes.Average(x => x.Y);
        var rigid = new double[n, 3];
        for (int i = 0; i < model.Nodes.Count; i++)
        {
            rigid[3*i, 0] = 1;
            rigid[3*i+1, 1] = 1;
            rigid[3*i, 2] = -(model.Nodes[i].Y - cy);
            rigid[3*i+1, 2] = model.Nodes[i].X - cx;
            // Truss-only rotational DOFs are not physical rigid-body coordinates.
            if (!HasZeroRow(assembly.Stiffness, 3*i+2) ||
                !HasZeroRow(assembly.Mass, 3*i+2)) rigid[3*i+2, 2] = 1;
        }
        var coefficientRows = new List<double[]>();
        foreach (double[] constraint in constraints)
        {
            var row = new double[3];
            for (int j = 0; j < 3; j++)
                for (int i = 0; i < n; i++) row[j] += constraint[i] * rigid[i, j];
            coefficientRows.Add(row);
        }
        var rigidConstraints = Orthonormalize(coefficientRows, 3);
        var surviving = OrthonormalComplement(3, rigidConstraints);
        return surviving.Count;
    }

    private static List<double[]> Orthonormalize(IEnumerable<double[]> rows, int n)
    {
        var basis = new List<double[]>();
        foreach (double[] row in rows)
        {
            if (row.Length != n || row.Any(x => !double.IsFinite(x)))
                throw new ArgumentException("M54_INVALID_CONSTRAINT: Non-finite or mismatched constraint.");
            AddIndependent((double[])row.Clone(), basis);
        }
        return basis;
    }
    private static List<double[]> OrthonormalComplement(int n, IReadOnlyList<double[]> constraintBasis)
    {
        var free = new List<double[]>();
        for (int dof = 0; dof < n; dof++)
        {
            var vector = Unit(n, dof);
            SubtractProjection(vector, constraintBasis);
            AddIndependent(vector, free);
        }
        if (free.Count != n - constraintBasis.Count)
            throw new InvalidOperationException("M54_BASIS_FAILURE: Could not resolve an independent admissible coordinate basis.");
        return free;
    }
    private static bool AddIndependent(double[] candidate, List<double[]> basis)
    {
        SubtractProjection(candidate, basis);
        double norm = Math.Sqrt(Dot(candidate, candidate));
        if (norm <= BasisTolerance) return false;
        for (int i = 0; i < candidate.Length; i++) candidate[i] /= norm;
        basis.Add(candidate);
        return true;
    }
    private static void SubtractProjection(double[] vector, IEnumerable<double[]> basis)
    {
        for (int repeat = 0; repeat < 2; repeat++)
            foreach (double[] b in basis)
            {
                double projection = Dot(vector, b);
                for (int i = 0; i < vector.Length; i++) vector[i] -= projection * b[i];
            }
    }
    private static double Dot(double[] a, double[] b)
    {
        double result = 0;
        for (int i = 0; i < a.Length; i++) result += a[i] * b[i];
        return result;
    }
    private static double[] Unit(int n, int index)
    {
        var result = new double[n]; result[index] = 1; return result;
    }
}
