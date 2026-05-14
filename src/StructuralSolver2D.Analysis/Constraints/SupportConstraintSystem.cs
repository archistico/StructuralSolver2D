using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Solvers;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Constraints;

/// <summary>
/// Builds and solves support constraints, including oriented translational restraints.
/// </summary>
internal static class SupportConstraintSystem
{
    private const double InactiveDofTolerance = 1e-12;

    /// <summary>
    /// Solves a globally assembled linear system with support constraints expressed as homogeneous equations.
    /// </summary>
    public static double[] SolveConstrainedSystem(
        StructuralModel model,
        IReadOnlyDictionary<string, int> nodeIndexById,
        int dofsPerNode,
        int totalDofCount,
        double[,] globalStiffness,
        double[] globalLoadVector,
        bool includeRotationalDof,
        bool addInactiveDofConstraints)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(nodeIndexById);
        ArgumentNullException.ThrowIfNull(globalStiffness);
        ArgumentNullException.ThrowIfNull(globalLoadVector);

        List<ConstraintEquation> constraints = BuildSupportConstraints(
            model,
            nodeIndexById,
            dofsPerNode,
            totalDofCount,
            includeRotationalDof);

        if (addInactiveDofConstraints)
        {
            AddInactiveDofConstraints(
                constraints,
                totalDofCount,
                globalStiffness,
                globalLoadVector);
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
    /// Builds support reaction result objects from the global residual vector.
    /// </summary>
    public static IReadOnlyList<SupportReactionResult> BuildSupportReactionResults(
        StructuralModel model,
        IReadOnlyDictionary<string, int> nodeIndexById,
        int dofsPerNode,
        double[] globalResidual,
        bool includeRotationalDof)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(nodeIndexById);
        ArgumentNullException.ThrowIfNull(globalResidual);

        return model.Supports
            .Select(support =>
            {
                int nodeBaseDof = nodeIndexById[support.NodeId] * dofsPerNode;
                bool hasTranslationalRestraint = support.RestrainedUx || support.RestrainedUy;

                return new SupportReactionResult(
                    support.Id,
                    support.NodeId,
                    hasTranslationalRestraint ? Clean(globalResidual[nodeBaseDof]) : 0.0,
                    hasTranslationalRestraint ? Clean(globalResidual[nodeBaseDof + 1]) : 0.0,
                    includeRotationalDof && support.RestrainedRz ? Clean(globalResidual[nodeBaseDof + 2]) : 0.0);
            })
            .ToList();
    }

    private static List<ConstraintEquation> BuildSupportConstraints(
        StructuralModel model,
        IReadOnlyDictionary<string, int> nodeIndexById,
        int dofsPerNode,
        int totalDofCount,
        bool includeRotationalDof)
    {
        List<ConstraintEquation> constraints = new();

        foreach (StructuralSupport support in model.Supports)
        {
            int nodeBaseDof = nodeIndexById[support.NodeId] * dofsPerNode;
            double angle = support.OrientationDegrees * Math.PI / 180.0;
            double cosine = Math.Cos(angle);
            double sine = Math.Sin(angle);

            if (support.RestrainedUx)
            {
                constraints.Add(CreateConstraint(totalDofCount, new[]
                {
                    new KeyValuePair<int, double>(nodeBaseDof, cosine),
                    new KeyValuePair<int, double>(nodeBaseDof + 1, sine),
                }));
            }

            if (support.RestrainedUy)
            {
                constraints.Add(CreateConstraint(totalDofCount, new[]
                {
                    new KeyValuePair<int, double>(nodeBaseDof, -sine),
                    new KeyValuePair<int, double>(nodeBaseDof + 1, cosine),
                }));
            }

            if (includeRotationalDof && support.RestrainedRz)
            {
                constraints.Add(CreateConstraint(totalDofCount, new[]
                {
                    new KeyValuePair<int, double>(nodeBaseDof + 2, 1.0),
                }));
            }
        }

        return constraints;
    }

    private static void AddInactiveDofConstraints(
        ICollection<ConstraintEquation> constraints,
        int totalDofCount,
        double[,] globalStiffness,
        double[] globalLoadVector)
    {
        for (int dof = 0; dof < totalDofCount; dof++)
        {
            if (IsDofReferencedByConstraint(constraints, dof))
            {
                continue;
            }

            if (!HasStiffness(globalStiffness, dof, totalDofCount) &&
                Math.Abs(globalLoadVector[dof]) <= InactiveDofTolerance)
            {
                constraints.Add(CreateConstraint(totalDofCount, new[]
                {
                    new KeyValuePair<int, double>(dof, 1.0),
                }));
            }
        }
    }

    private static bool HasStiffness(double[,] globalStiffness, int dof, int totalDofCount)
    {
        for (int column = 0; column < totalDofCount; column++)
        {
            if (Math.Abs(globalStiffness[dof, column]) > InactiveDofTolerance)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDofReferencedByConstraint(IEnumerable<ConstraintEquation> constraints, int dof) =>
        constraints.Any(constraint => constraint.Coefficients.TryGetValue(dof, out double coefficient) && Math.Abs(coefficient) > InactiveDofTolerance);

    private static ConstraintEquation CreateConstraint(
        int totalDofCount,
        IEnumerable<KeyValuePair<int, double>> coefficients)
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

        return new ConstraintEquation(row);
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

    private sealed record ConstraintEquation(IReadOnlyDictionary<int, double> Coefficients);
}
