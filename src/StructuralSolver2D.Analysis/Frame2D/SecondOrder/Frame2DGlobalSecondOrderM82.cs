using StructuralSolver2D.Analysis.Constraints;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Frame2D.SecondOrder;

/// <summary>
/// M82 opt-in, global LINEARIZED initial-stress Frame2D analysis. Axial forces are
/// recovered from the FIRST-ORDER state and then HELD CONSTANT for the tangent
/// solve Kt*u=F. This is not a corotational or iterated large-displacement model.
/// Currently accepts unreleased frames, rigid axis-aligned supports and nodal
/// forces/moments only; other load/support types fail explicitly.
/// </summary>
public sealed class Frame2DGlobalSecondOrderM82
{
    private const int Dofs = 3;

    /// <summary>
    /// Solve a nodal load case with a frozen first-order axial force field.
    /// A critical multiplier is estimated for that field by the loss of positive
    /// definiteness of the constrained global tangent. Null means no instability
    /// was detected in the searched interval, not proof of unlimited stability.
    /// </summary>
    public Frame2DSecondOrderResultM82 Analyze(StructuralModel model, string loadCaseId)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(loadCaseId))
            throw new ArgumentException("A load case identifier is required.", nameof(loadCaseId));

        ValidateScope(model, loadCaseId);
        StructuralAnalysisResult firstOrder = new Frame2DAnalyzer().Analyze(model, loadCaseId);
        var indexes = model.Nodes.Select((node, position) => (node.Id, position))
            .ToDictionary(entry => entry.Id, entry => entry.position, StringComparer.OrdinalIgnoreCase);
        int count = model.Nodes.Count * Dofs;
        double[,] elastic = new double[count, count];
        double[,] geometric = new double[count, count];
        double[] loads = new double[count];
        var nodes = model.Nodes.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var materials = model.Materials.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var sections = model.Sections.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var endForces = firstOrder.MemberEndForces.ToDictionary(item => item.MemberId, StringComparer.OrdinalIgnoreCase);
        var axialReferences = new List<MemberAxialReferenceM82>();

        foreach (StructuralMember member in model.Members)
        {
            StructuralNode start = nodes[member.StartNodeId];
            StructuralNode end = nodes[member.EndNodeId];
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double length = StructuralMember.GetLength(start, end);
            double c = dx / length;
            double s = dy / length;
            StructuralMaterial material = materials[member.MaterialId];
            StructuralSection section = sections[member.SectionId];
            MemberEndForceResult forces = endForces[member.Id];
            // With NO axial member loads: Fstart=-N and Fend=+N (tension positive).
            double axial = 0.5d * (forces.EndAxial - forces.StartAxial);
            if (!double.IsFinite(axial))
                throw new StructuralAnalysisException($"M82_NONFINITE_AXIAL: member '{member.Id}'.");
            axialReferences.Add(new MemberAxialReferenceM82(member.Id, axial));

            double[,] transform = Frame2DElementMatrices.BuildTransformation(c, s);
            double[,] ke = Frame2DElementMatrices.TransformStiffnessToGlobal(
                Frame2DElementMatrices.BuildLocalStiffness(
                    material.ElasticModulus, section.Area, section.MomentOfInertia, length), transform);
            double[,] kg = Frame2DElementMatrices.TransformStiffnessToGlobal(
                Frame2DInitialStressM81.BuildLocalGeometricStiffness(axial, length), transform);
            int i = indexes[member.StartNodeId] * Dofs;
            int j = indexes[member.EndNodeId] * Dofs;
            int[] map = { i, i + 1, i + 2, j, j + 1, j + 2 };
            for (int row = 0; row < 6; row++)
                for (int col = 0; col < 6; col++)
                {
                    elastic[map[row], map[col]] += ke[row, col];
                    geometric[map[row], map[col]] += kg[row, col];
                }
        }

        foreach (StructuralLoad load in model.Loads.Where(item =>
            string.Equals(item.LoadCaseId, loadCaseId, StringComparison.OrdinalIgnoreCase)))
        {
            int offset = indexes[load.TargetId] * Dofs;
            if (load.Type == StructuralLoadType.NodalMoment)
                loads[offset + 2] += load.Value;
            else if (load.Direction == StructuralLoadDirection.GlobalX)
                loads[offset] += load.Value;
            else
                loads[offset + 1] += load.Value;
        }

        int[] free = FindFreeDofs(model, indexes, count);
        if (!PositiveDefinite(elastic, new double[count, count], free, 0d))
            throw new StructuralAnalysisException("M82_FIRST_ORDER_INSTABILITY: constrained elastic stiffness is not positive definite.");

        double? criticalMultiplier = FindCriticalMultiplier(elastic, geometric, free, axialReferences);
        if (!PositiveDefinite(elastic, geometric, free, 1d))
            throw new StructuralAnalysisException("M82_LINEARIZED_INSTABILITY: the global second-order tangent is not positive definite at the applied load.");

        double[,] tangent = Combine(elastic, geometric, 1d);
        double[] displacement;
        try
        {
            displacement = SupportConstraintSystem.SolveConstrainedSystem(
                model, indexes, Dofs, count, tangent, loads,
                includeRotationalDof: true, addInactiveDofConstraints: true);
        }
        catch (StructuralAnalysisException) { throw; }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new StructuralAnalysisException($"M82_TANGENT_SOLVE_FAILED: {ex.Message}");
        }

        double[] residual = new double[count];
        for (int row = 0; row < count; row++)
        {
            residual[row] = -loads[row];
            for (int col = 0; col < count; col++)
                residual[row] += tangent[row, col] * displacement[col];
        }

        IReadOnlyList<NodalDisplacementResult> displacements = model.Nodes.Select(node =>
        {
            int i = indexes[node.Id] * Dofs;
            return new NodalDisplacementResult(node.Id, displacement[i], displacement[i + 1], displacement[i + 2]);
        }).ToArray();
        IReadOnlyList<SupportReactionResult> reactions = SupportConstraintSystem.BuildSupportReactionResults(
            model, indexes, Dofs, residual, includeRotationalDof: true, globalDisplacements: displacement);
        return new Frame2DSecondOrderResultM82(loadCaseId, firstOrder, displacements,
            reactions, axialReferences, criticalMultiplier);
    }

    private static void ValidateScope(StructuralModel model, string loadCaseId)
    {
        if (model.Members.Count == 0 || model.Members.Any(member => member.Type != MemberType.Frame2D ||
                member.ReleaseStartMoment || member.ReleaseEndMoment))
            throw new StructuralAnalysisException("M82_UNSUPPORTED_MEMBER: requires unreleased Frame2D members only.");
        if (model.Supports.Any(support => support.OrientationDegrees != 0d ||
                support.StiffnessUx != 0d || support.StiffnessUy != 0d || support.StiffnessRz != 0d))
            throw new StructuralAnalysisException("M82_UNSUPPORTED_SUPPORT: requires axis-aligned rigid supports without springs.");
        foreach (StructuralLoad load in model.Loads.Where(item =>
                     string.Equals(item.LoadCaseId, loadCaseId, StringComparison.OrdinalIgnoreCase)))
        {
            if (load.TargetType != StructuralLoadTargetType.Node ||
                (load.Type != StructuralLoadType.NodalForce && load.Type != StructuralLoadType.NodalMoment) ||
                (load.Type == StructuralLoadType.NodalForce &&
                 load.Direction != StructuralLoadDirection.GlobalX && load.Direction != StructuralLoadDirection.GlobalY) ||
                (load.Type == StructuralLoadType.NodalMoment && load.Direction != StructuralLoadDirection.MomentZ))
                throw new StructuralAnalysisException($"M82_UNSUPPORTED_LOAD: '{load.Id}' requires nodal global forces/moments. Member loads, self-weight and settlement are not yet supported.");
        }
    }

    private static int[] FindFreeDofs(StructuralModel model, Dictionary<string, int> indexes, int count)
    {
        var restrained = new bool[count];
        foreach (StructuralSupport support in model.Supports)
        {
            int i = indexes[support.NodeId] * Dofs;
            if (support.RestrainedUx) restrained[i] = true;
            if (support.RestrainedUy) restrained[i + 1] = true;
            if (support.RestrainedRz) restrained[i + 2] = true;
        }
        return Enumerable.Range(0, count).Where(i => !restrained[i]).ToArray();
    }

    private static double[,] Combine(double[,] elastic, double[,] geometric, double factor)
    {
        int size = elastic.GetLength(0);
        var total = new double[size, size];
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                total[i, j] = elastic[i, j] + factor * geometric[i, j];
                if (!double.IsFinite(total[i, j]))
                    throw new StructuralAnalysisException("M82_NONFINITE_TANGENT: geometric stiffness overflow.");
            }
        return total;
    }

    private static bool PositiveDefinite(double[,] elastic, double[,] geometric, int[] free, double factor)
    {
        int count = free.Length;
        if (count == 0) return true;
        var l = new double[count, count];
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                double diagonal = elastic[free[i], free[j]] + factor * geometric[free[i], free[j]];
                double sum = diagonal;
                for (int k = 0; k < j; k++) sum -= l[i, k] * l[j, k];
                if (!double.IsFinite(sum)) return false;
                if (i == j)
                {
                    // Relative-to-this-DOF gate avoids mixing force/rotation units
                    // and flags the loss of numerical stability near buckling.
                    if (!(diagonal > 0d) || sum <= 1e-12d * diagonal) return false;
                    l[i, j] = Math.Sqrt(sum);
                }
                else l[i, j] = sum / l[j, j];
            }
        }
        return true;
    }

    private static double? FindCriticalMultiplier(double[,] elastic, double[,] geometric,
        int[] free, IReadOnlyList<MemberAxialReferenceM82> axial)
    {
        if (!axial.Any(item => item.AxialForce < 0d)) return null;
        double stable = 0d;
        double unstable = 1d;
        while (unstable < 1_048_576d && PositiveDefinite(elastic, geometric, free, unstable))
        {
            stable = unstable;
            unstable *= 2d;
        }
        if (PositiveDefinite(elastic, geometric, free, unstable)) return null;
        for (int i = 0; i < 64; i++)
        {
            double mid = stable + (unstable - stable) / 2d;
            if (PositiveDefinite(elastic, geometric, free, mid)) stable = mid;
            else unstable = mid;
        }
        return (stable + unstable) / 2d;
    }
}

/// <summary>A member's frozen first-order axial force, kN; compression is negative.</summary>
public sealed record MemberAxialReferenceM82(string MemberId, double AxialForce);

/// <summary>
/// Opt-in M82 LINEARIZED reference output: displacements and reactions satisfy
/// Kt*u=F. It does not provide updated N/V/M diagrams, plasticity, normative
/// buckling resistance or a converged nonlinear equilibrium configuration.
/// </summary>
public sealed record Frame2DSecondOrderResultM82(
    string LoadCaseId,
    StructuralAnalysisResult FirstOrder,
    IReadOnlyList<NodalDisplacementResult> Displacements,
    IReadOnlyList<SupportReactionResult> Reactions,
    IReadOnlyList<MemberAxialReferenceM82> MemberAxialReferences,
    double? CriticalLoadMultiplier)
{
    /// <summary>Returns the linearized second-order nodal displacement.</summary>
    public NodalDisplacementResult GetDisplacement(string nodeId) =>
        Displacements.First(item => string.Equals(item.NodeId, nodeId, StringComparison.OrdinalIgnoreCase));
}
