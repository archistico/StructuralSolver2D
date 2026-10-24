using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Solvers;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Constraints;

/// <summary>
/// Builds and solves support constraints, including oriented translational restraints.
/// </summary>
internal static class SupportConstraintSystem
{
    private const double InactiveDofTolerance = 1e-12;

    /// <summary>
    /// Solves a linear system with oriented support constraints C*u=g and optional elastic support stiffness.
    /// </summary>
    public static double[] SolveConstrainedSystem(
        StructuralModel model,
        IReadOnlyDictionary<string, int> nodeIndexById,
        int dofsPerNode,
        int totalDofCount,
        double[,] globalStiffness,
        double[] globalLoadVector,
        bool includeRotationalDof,
        bool addInactiveDofConstraints,
        IReadOnlyDictionary<string, double>? loadCaseFactors = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(nodeIndexById);
        ArgumentNullException.ThrowIfNull(globalStiffness);
        ArgumentNullException.ThrowIfNull(globalLoadVector);

        // The stiffness argument is owned by the caller and modified exactly once per analysis.
        AddSupportSpringStiffness(model, nodeIndexById, dofsPerNode, globalStiffness, includeRotationalDof);

        List<ConstraintEquation> constraints = BuildSupportConstraints(
            model, nodeIndexById, dofsPerNode, totalDofCount, includeRotationalDof, loadCaseFactors);

        if (addInactiveDofConstraints)
        {
            AddInactiveDofConstraints(
                constraints,
                totalDofCount,
                globalStiffness,
                globalLoadVector,
                dofsPerNode,
                includeRotationalDof);
        }

        if (constraints.Count == 0)
        {
            return DenseLinearSystemSolver.Solve(globalStiffness, globalLoadVector);
        }

        int augmentedSize = totalDofCount + constraints.Count;
        double[,] augmentedMatrix = new double[augmentedSize, augmentedSize];
        double[] augmentedRightHandSide = new double[augmentedSize];

        CopyStiffness(globalStiffness, augmentedMatrix, totalDofCount);
        Array.Copy(globalLoadVector, augmentedRightHandSide, totalDofCount);

        for (int constraintIndex = 0; constraintIndex < constraints.Count; constraintIndex++)
        {
            int constraintRow = totalDofCount + constraintIndex;
            ConstraintEquation constraint = constraints[constraintIndex];

            augmentedRightHandSide[constraintRow] = constraint.RightHandSide;
            foreach (KeyValuePair<int, double> coefficient in constraint.Coefficients)
            {
                int dof = coefficient.Key;
                double value = coefficient.Value;
                augmentedMatrix[constraintRow, dof] = value;
                augmentedMatrix[dof, constraintRow] = value;
            }
        }

        double[] solution = DenseLinearSystemSolver.Solve(augmentedMatrix, augmentedRightHandSide);
        double[] displacements = new double[totalDofCount];
        Array.Copy(solution, displacements, totalDofCount);

        return displacements;
    }

    /// <summary>
    /// Returns the same homogeneous support/inactive-rotation equations used by the solver,
    /// solely for independent verification. Rows are expressed in the full global DOF space.
    /// </summary>
    internal static IReadOnlyList<double[]> GetHomogeneousConstraintRows(
        StructuralModel model,
        IReadOnlyDictionary<string, int> nodeIndexById,
        int dofsPerNode,
        int totalDofCount,
        double[,] stiffness,
        double[] loads,
        bool includeRotationalDof,
        bool addInactiveDofConstraints)
    {
        List<ConstraintEquation> equations = BuildSupportConstraints(
            model, nodeIndexById, dofsPerNode, totalDofCount, includeRotationalDof);
        if (addInactiveDofConstraints)
            AddInactiveDofConstraints(equations, totalDofCount, stiffness, loads,
                dofsPerNode, includeRotationalDof);
        return equations.Select(equation =>
        {
            double[] row = new double[totalDofCount];
            foreach (KeyValuePair<int, double> coefficient in equation.Coefficients)
                row[coefficient.Key] = coefficient.Value;
            return row;
        }).ToList();
    }

    /// <summary>
    /// Builds support reaction result objects from the global residual vector.
    /// </summary>
    /// <remarks>
    /// Reaction forces are reported as global components along each support's restrained directions.
    /// With redundant restraints, the local constraint multipliers use the minimum-norm distribution;
    /// individual reactions are conventional, while their sum is the nodal reaction.
    /// </remarks>
    public static IReadOnlyList<SupportReactionResult> BuildSupportReactionResults(
        StructuralModel model,
        IReadOnlyDictionary<string, int> nodeIndexById,
        int dofsPerNode,
        double[] globalResidual,
        bool includeRotationalDof,
        double[]? globalDisplacements = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(nodeIndexById);
        ArgumentNullException.ThrowIfNull(globalResidual);

        Dictionary<string, double[]> reactions = model.Supports.ToDictionary(
            support => support.Id, _ => new double[dofsPerNode], StringComparer.OrdinalIgnoreCase);

        foreach (var group in model.Supports.GroupBy(support => support.NodeId, StringComparer.OrdinalIgnoreCase))
        {
            var directions = group.SelectMany(support => GetSupportDirections(support, dofsPerNode, includeRotationalDof)
                .Select(direction => (support.Id, Direction: direction))).ToList();
            List<double[]> basis = BuildOrthonormalBasis(directions.Select(item => item.Direction));
            if (basis.Count == 0) continue;

            // Write r = C^T lambda in an independent orthonormal basis Q.
            // Solve (Q^T C^T C Q) z = Q^T r and use lambda = C Q z.
            // This also handles repeated constraint directions without duplicating r.
            double[,] gram = new double[basis.Count, basis.Count];
            double[] projectedResidual = new double[basis.Count];
            int nodeBaseDof = nodeIndexById[group.Key] * dofsPerNode;
            for (int row = 0; row < basis.Count; row++)
            {
                for (int dof = 0; dof < dofsPerNode; dof++)
                    projectedResidual[row] += basis[row][dof] * globalResidual[nodeBaseDof + dof];
                for (int column = 0; column < basis.Count; column++)
                    gram[row, column] = directions.Sum(item => Dot(item.Direction, basis[row]) * Dot(item.Direction, basis[column]));
            }
            double[] coordinates = DenseLinearSystemSolver.Solve(gram, projectedResidual);
            foreach (var item in directions)
            {
                double multiplier = 0;
                for (int row = 0; row < basis.Count; row++)
                    multiplier += Dot(item.Direction, basis[row]) * coordinates[row];
                for (int dof = 0; dof < dofsPerNode; dof++)
                    reactions[item.Id][dof] += multiplier * item.Direction[dof];
            }
        }

        if (globalDisplacements is not null)
        {
            foreach (StructuralSupport support in model.Supports)
            {
                int offset = nodeIndexById[support.NodeId] * dofsPerNode;
                foreach (var spring in GetSprings(support, dofsPerNode, includeRotationalDof))
                {
                    double deformation = 0;
                    for (int axis = 0; axis < dofsPerNode; axis++)
                        deformation += spring.Direction[axis] * globalDisplacements[offset + axis];
                    for (int axis = 0; axis < dofsPerNode; axis++)
                        reactions[support.Id][axis] -= spring.Stiffness * deformation * spring.Direction[axis];
                }
            }
        }

        return model.Supports.Select(support => new SupportReactionResult(
                support.Id, support.NodeId,
                Clean(reactions[support.Id][0]), Clean(reactions[support.Id][1]),
                includeRotationalDof ? Clean(reactions[support.Id][2]) : 0))
            .ToList();
    }

    private static IEnumerable<double[]> GetSupportDirections(StructuralSupport support, int dofsPerNode, bool includeRotationalDof)
    {
        double angle = support.OrientationDegrees * Math.PI / 180.0;
        double cosine = Math.Cos(angle), sine = Math.Sin(angle);
        if (Math.Abs(cosine) <= InactiveDofTolerance) cosine = 0;
        if (Math.Abs(sine) <= InactiveDofTolerance) sine = 0;
        if (support.RestrainedUx)
        {
            double[] row = new double[dofsPerNode];
            row[0] = cosine;
            row[1] = sine;
            yield return row;
        }
        if (support.RestrainedUy)
        {
            double[] row = new double[dofsPerNode];
            row[0] = -sine;
            row[1] = cosine;
            yield return row;
        }
        if (includeRotationalDof && support.RestrainedRz)
        {
            double[] row = new double[dofsPerNode];
            row[2] = 1;
            yield return row;
        }
    }

    private static IEnumerable<(double[] Direction, double Stiffness)> GetSprings(
        StructuralSupport support, int dofsPerNode, bool includeRotationalDof)
    {
        if (!includeRotationalDof && support.StiffnessRz > 0)
            throw new StructuralAnalysisException($"M77_TRUSS_ROTATION: support '{support.Id}' has a rotational spring in a Truss2D-only solver.");
        double angle = support.OrientationDegrees * Math.PI / 180.0;
        double c = Math.Cos(angle), s = Math.Sin(angle);
        if (support.StiffnessUx > 0)
        {
            var direction = new double[dofsPerNode]; direction[0] = c; direction[1] = s;
            yield return (direction, support.StiffnessUx);
        }
        if (support.StiffnessUy > 0)
        {
            var direction = new double[dofsPerNode]; direction[0] = -s; direction[1] = c;
            yield return (direction, support.StiffnessUy);
        }
        if (includeRotationalDof && support.StiffnessRz > 0)
        {
            var direction = new double[dofsPerNode]; direction[2] = 1;
            yield return (direction, support.StiffnessRz);
        }
    }

    private static void AddSupportSpringStiffness(
        StructuralModel model, IReadOnlyDictionary<string, int> nodeIndexById,
        int dofsPerNode, double[,] globalStiffness, bool includeRotationalDof)
    {
        foreach (StructuralSupport support in model.Supports)
        {
            int start = nodeIndexById[support.NodeId] * dofsPerNode;
            foreach (var spring in GetSprings(support, dofsPerNode, includeRotationalDof))
                for (int i = 0; i < dofsPerNode; i++)
                    for (int j = 0; j < dofsPerNode; j++)
                        globalStiffness[start + i, start + j] +=
                            spring.Stiffness * spring.Direction[i] * spring.Direction[j];
        }
    }

    /// <summary>Applies only spring stiffness to a matrix for opt-in stability diagnostics.</summary>
    internal static void AddSupportSpringsForVerification(
        StructuralModel model, IReadOnlyDictionary<string, int> nodeIndexById,
        int dofsPerNode, double[,] stiffness, bool includeRotationalDof) =>
        AddSupportSpringStiffness(model, nodeIndexById, dofsPerNode, stiffness, includeRotationalDof);

    private static List<double[]> BuildOrthonormalBasis(IEnumerable<double[]> directions)
    {
        List<double[]> basis = new();
        foreach (double[] direction in directions)
        {
            double[] vector = (double[])direction.Clone();
            // Reorthogonalize to avoid accepting round-off from dependent rows as a new direction.
            for (int pass = 0; pass < 2; pass++)
            foreach (double[] existing in basis)
            {
                double projection = Dot(vector, existing);
                for (int dof = 0; dof < vector.Length; dof++) vector[dof] -= projection * existing[dof];
            }
            double norm = Math.Sqrt(Dot(vector, vector));
            if (norm <= InactiveDofTolerance) continue;
            for (int dof = 0; dof < vector.Length; dof++) vector[dof] /= norm;
            basis.Add(vector);
        }
        return basis;
    }

    private static double Dot(double[] left, double[] right)
    {
        double result = 0;
        for (int index = 0; index < left.Length; index++) result += left[index] * right[index];
        return result;
    }

    private static List<ConstraintEquation> BuildSupportConstraints(
        StructuralModel model,
        IReadOnlyDictionary<string, int> nodeIndexById,
        int dofsPerNode,
        int totalDofCount,
        bool includeRotationalDof,
        IReadOnlyDictionary<string, double>? loadCaseFactors = null)
    {
        List<ConstraintEquation> constraints = new();
        foreach (var group in model.Supports.GroupBy(s => s.NodeId, StringComparer.OrdinalIgnoreCase))
        {
            int start = nodeIndexById[group.Key] * dofsPerNode;
            List<(double[] Direction, double Value)> independent = new();
            foreach (StructuralSupport support in group)
            {
                double angle = support.OrientationDegrees * Math.PI / 180.0;
                double c = Math.Cos(angle), si = Math.Sin(angle);
                var localDirections = new[]
                {
                    new double[] { c, si, 0 }, new double[] { -si, c, 0 }, new double[] { 0, 0, 1 },
                };
                foreach (var (restrained, axis, dir) in new[]
                {
                    (support.RestrainedUx, 0, StructuralLoadDirection.LocalX),
                    (support.RestrainedUy, 1, StructuralLoadDirection.LocalY),
                    (includeRotationalDof && support.RestrainedRz, 2, StructuralLoadDirection.MomentZ),
                })
                {
                    if (!restrained) continue;
                    var vector = new double[dofsPerNode];
                    Array.Copy(localDirections[axis], vector, dofsPerNode);
                    double target = 0;
                    if (loadCaseFactors is not null)
                        foreach (StructuralLoad settlement in model.Loads.Where(l =>
                            l.Type == StructuralLoadType.SupportSettlement &&
                            string.Equals(l.TargetId, support.Id, StringComparison.OrdinalIgnoreCase)))
                            if (settlement.Direction == dir && loadCaseFactors.TryGetValue(settlement.LoadCaseId, out double factor))
                                target += settlement.Value * factor;
                    if (!double.IsFinite(target))
                        throw new StructuralAnalysisException("M77_SETTLEMENT_OVERFLOW: factored settlement is not finite.");

                    // Orthonormalize displacement constraints INCLUDING the right-hand sides.
                    // A duplicate row with contradictory settlement is an inconsistent model.
                    for (int pass = 0; pass < 2; pass++)
                        foreach (var prior in independent)
                        {
                            double projection = Dot(vector, prior.Direction);
                            for (int k = 0; k < vector.Length; k++)
                                vector[k] -= projection * prior.Direction[k];
                            target -= projection * prior.Value;
                        }
                    double length = Math.Sqrt(Dot(vector, vector));
                    if (length <= InactiveDofTolerance)
                    {
                        if (Math.Abs(target) > 1e-9)
                            throw new StructuralAnalysisException($"M77_INCOMPATIBLE_SETTLEMENT: conflicting restraints at node '{group.Key}'.");
                        continue;
                    }
                    for (int k = 0; k < vector.Length; k++) vector[k] /= length;
                    independent.Add((vector, target / length));
                }
            }
            foreach (var item in independent)
                constraints.Add(CreateConstraint(totalDofCount,
                    item.Direction.Select((v, dof) => new KeyValuePair<int, double>(start + dof, v)), item.Value));
        }
        return constraints;
    }

    private static void AddInactiveDofConstraints(
        ICollection<ConstraintEquation> constraints,
        int totalDofCount,
        double[,] globalStiffness,
        double[] globalLoadVector,
        int dofsPerNode,
        bool includeRotationalDof)
    {
        HashSet<int> constrainedDofs = BuildConstrainedDofSet(constraints);

        for (int dof = 0; dof < totalDofCount; dof++)
        {
            // Only unused rotations (truss nodes or released ends) are auxiliary DOFs.
            // A free translation with zero stiffness is a structural mechanism.
            if (!includeRotationalDof || dof % dofsPerNode != 2) continue;
            if (constrainedDofs.Contains(dof))
            {
                continue;
            }

            if (!HasStiffness(globalStiffness, dof, totalDofCount) &&
                globalLoadVector[dof] == 0)
            {
                constraints.Add(CreateConstraint(totalDofCount, new[]
                {
                    new KeyValuePair<int, double>(dof, 1.0),
                }));
                constrainedDofs.Add(dof);
            }
        }
    }

    private static bool HasStiffness(double[,] globalStiffness, int dof, int totalDofCount)
    {
        for (int column = 0; column < totalDofCount; column++)
        {
            if (globalStiffness[dof, column] != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<int> BuildConstrainedDofSet(IEnumerable<ConstraintEquation> constraints)
    {
        HashSet<int> constrainedDofs = new();

        foreach (ConstraintEquation constraint in constraints)
        {
            foreach (KeyValuePair<int, double> coefficient in constraint.Coefficients)
            {
                if (Math.Abs(coefficient.Value) > InactiveDofTolerance)
                {
                    constrainedDofs.Add(coefficient.Key);
                }
            }
        }

        return constrainedDofs;
    }

    private static ConstraintEquation CreateConstraint(
        int totalDofCount,
        IEnumerable<KeyValuePair<int, double>> coefficients,
        double rightHandSide = 0.0)
    {
        Dictionary<int, double> row = new();

        foreach (KeyValuePair<int, double> coefficient in coefficients)
        {
            if (coefficient.Key < 0 || coefficient.Key >= totalDofCount)
            {
                throw new ArgumentOutOfRangeException(nameof(coefficients), "A support constraint references a degree of freedom outside the global system.");
            }

            if (Math.Abs(coefficient.Value) <= InactiveDofTolerance)
            {
                continue;
            }

            row[coefficient.Key] = coefficient.Value;
        }

        return new ConstraintEquation(row, rightHandSide);
    }

    private static void CopyStiffness(double[,] source, double[,] target, int totalDofCount)
    {
        for (int row = 0; row < totalDofCount; row++)
        {
            for (int column = 0; column < totalDofCount; column++)
            {
                target[row, column] = source[row, column];
            }
        }
    }

    private static double Clean(double value) =>
        Math.Abs(value) <= 1e-9 ? 0.0 : value;

    private sealed record ConstraintEquation(IReadOnlyDictionary<int, double> Coefficients, double RightHandSide);
}
