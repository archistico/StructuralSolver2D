using StructuralSolver2D.Core.Model.Loads;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Solvers;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Frame2D;

/// <summary>
/// Samples the finite-element displacement interpolation along 2D frame members.
/// The sampler does not solve the structure again; it post-processes nodal displacements already produced by <see cref="Frame2DAnalyzer"/>.
/// </summary>
/// <remarks>
/// The sampled transverse displacement uses the standard cubic Hermite interpolation of the 2D frame element.
/// This is the finite-element displacement field. For distributed loads, an internal sampled value can differ from a closed-form
/// beam deflection unless that position is explicitly modeled as a structural node.
/// </remarks>
public sealed class Frame2DDisplacementSampler
{
    /// <summary>
    /// Samples interpolated displacements along one frame member.
    /// </summary>
    /// <param name="model">Structural model used for the analysis.</param>
    /// <param name="analysisResult">Analysis result to post-process.</param>
    /// <param name="memberId">Member identifier to sample.</param>
    /// <param name="sampleCount">Number of samples. Must be at least 2.</param>
    /// <returns>Sampled member displacement diagram.</returns>
    public MemberDisplacementDiagram SampleMember(
        StructuralModel model,
        StructuralAnalysisResult analysisResult,
        string memberId,
        int sampleCount = 21)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(analysisResult);

        if (string.IsNullOrWhiteSpace(memberId))
        {
            throw new ArgumentException("Member id cannot be empty.", nameof(memberId));
        }

        if (sampleCount < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount), sampleCount, "At least two samples are required.");
        }

        model = SelfWeightStaticLoadExpander.Expand(model);
        Dictionary<string, StructuralMember> members = model.Members.ToDictionary(member => member.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralNode> nodes = model.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, NodalDisplacementResult> displacements = analysisResult.Displacements.ToDictionary(
            displacement => displacement.NodeId,
            StringComparer.OrdinalIgnoreCase);

        if (!members.TryGetValue(memberId, out StructuralMember? member))
        {
            throw new StructuralAnalysisException($"Member '{memberId}' was not found in the structural model.");
        }

        if (member.Type != MemberType.Frame2D)
        {
            throw new StructuralAnalysisException($"Member '{memberId}' is not a Frame2D member.");
        }

        StructuralNode startNode = nodes[member.StartNodeId];
        StructuralNode endNode = nodes[member.EndNodeId];
        MemberGeometry geometry = MemberGeometry.FromNodes(startNode, endNode);

        NodalDisplacementResult startDisplacement = displacements[member.StartNodeId];
        NodalDisplacementResult endDisplacement = displacements[member.EndNodeId];

        double[] globalElementDisplacements =
        {
            startDisplacement.Ux,
            startDisplacement.Uy,
            startDisplacement.Rz,
            endDisplacement.Ux,
            endDisplacement.Uy,
            endDisplacement.Rz,
        };

        double[,] transformation = Frame2DElementMatrices.BuildTransformation(geometry.Cosine, geometry.Sine);
        double[] localElementDisplacements = Frame2DElementMatrices.TransformDisplacementToLocal(globalElementDisplacements, transformation);
        RecoverReleasedRotations(model, member, analysisResult.LoadCaseId, geometry, localElementDisplacements);

        List<MemberDisplacementSample> samples = new(sampleCount);

        for (int index = 0; index < sampleCount; index++)
        {
            double normalizedPosition = (double)index / (sampleCount - 1);
            double distance = normalizedPosition * geometry.Length;

            (double localUx, double localUy, double localRz) = InterpolateLocalDisplacement(
                localElementDisplacements,
                geometry.Length,
                normalizedPosition);

            double globalUx = (geometry.Cosine * localUx) - (geometry.Sine * localUy);
            double globalUy = (geometry.Sine * localUx) + (geometry.Cosine * localUy);

            samples.Add(new MemberDisplacementSample(
                memberId,
                normalizedPosition,
                distance,
                localUx,
                localUy,
                localRz,
                globalUx,
                globalUy));
        }

        return new MemberDisplacementDiagram(memberId, geometry.Length, samples);
    }

    /// <summary>
    /// Samples interpolated displacements for all Frame2D members in the analysis result.
    /// </summary>
    /// <param name="model">Structural model used for the analysis.</param>
    /// <param name="analysisResult">Analysis result to post-process.</param>
    /// <param name="sampleCount">Number of samples per member. Must be at least 2.</param>
    /// <returns>Sampled displacement diagrams.</returns>
    public IReadOnlyList<MemberDisplacementDiagram> SampleAllMembers(
        StructuralModel model,
        StructuralAnalysisResult analysisResult,
        int sampleCount = 21)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(analysisResult);

        HashSet<string> frameMemberIds = model.Members
            .Where(member => member.Type == MemberType.Frame2D)
            .Select(member => member.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return analysisResult.MemberEndForces
            .Where(memberEndForce => frameMemberIds.Contains(memberEndForce.MemberId))
            .Select(memberEndForce => SampleMember(model, analysisResult, memberEndForce.MemberId, sampleCount))
            .ToList();
    }

    private static void RecoverReleasedRotations(
        StructuralModel model,
        StructuralMember member,
        string resultId,
        MemberGeometry geometry,
        double[] localDisplacements)
    {
        int[] released = new[] { member.ReleaseStartMoment ? 2 : -1, member.ReleaseEndMoment ? 5 : -1 }
            .Where(index => index >= 0).ToArray();
        if (released.Length == 0) return;

        StructuralMaterial material = model.Materials.Single(item => string.Equals(item.Id, member.MaterialId, StringComparison.OrdinalIgnoreCase));
        StructuralSection section = model.Sections.Single(item => string.Equals(item.Id, member.SectionId, StringComparison.OrdinalIgnoreCase));
        double[,] stiffness = Frame2DElementMatrices.BuildLocalStiffness(
            material.ElasticModulus, section.Area, section.MomentOfInertia, geometry.Length);
        double[] load = BuildOriginalLocalLoad(model, member.Id, resultId, geometry);
        double[,] rotationStiffness = new double[released.Length, released.Length];
        double[] rotationLoad = new double[released.Length];

        // Zero end moments imply Kcc * uc = fc - Kcr * ur before condensation.
        // The recovered rotations belong to this member, not to the shared global node.
        for (int row = 0; row < released.Length; row++)
        {
            rotationLoad[row] = load[released[row]];
            for (int dof = 0; dof < 6; dof++)
                if (!released.Contains(dof)) rotationLoad[row] -= stiffness[released[row], dof] * localDisplacements[dof];
            for (int column = 0; column < released.Length; column++)
                rotationStiffness[row, column] = stiffness[released[row], released[column]];
        }
        double[] rotations = DenseLinearSystemSolver.Solve(rotationStiffness, rotationLoad);
        for (int row = 0; row < released.Length; row++) localDisplacements[released[row]] = rotations[row];
    }

    private static double[] BuildOriginalLocalLoad(StructuralModel model, string memberId, string resultId, MemberGeometry geometry)
    {
        StructuralLoadCombination? combination = model.LoadCombinations.FirstOrDefault(item =>
            string.Equals(item.Id, resultId, StringComparison.OrdinalIgnoreCase));
        Dictionary<string, double> factors = combination is null
            ? new(StringComparer.OrdinalIgnoreCase) { [resultId] = 1 }
            : combination.Terms.ToDictionary(term => term.LoadCaseId, term => term.Factor, StringComparer.OrdinalIgnoreCase);
        double[] total = new double[6];
        foreach (StructuralLoad load in model.Loads.Where(load =>
            load.TargetType == StructuralLoadTargetType.Member &&
            string.Equals(load.TargetId, memberId, StringComparison.OrdinalIgnoreCase) && factors.ContainsKey(load.LoadCaseId)))
        {
            (double x, double y) = ResolveLocalLoad(load.Direction, load.Value * factors[load.LoadCaseId], geometry);
            double[] equivalent;
            switch (load.Type)
            {
                case StructuralLoadType.UniformDistributedLoad:
                    equivalent = Frame2DElementMatrices.BuildUniformLocalLoad(x, y, geometry.Length);
                    break;
                case StructuralLoadType.LinearDistributedLoad:
                    (double endX, double endY) = ResolveLocalLoad(load.Direction,
                        (load.EndValue ?? throw new StructuralAnalysisException($"Load '{load.Id}' has no end value.")) * factors[load.LoadCaseId], geometry);
                    equivalent = Frame2DElementMatrices.BuildLinearLocalLoad(x, endX, y, endY, geometry.Length);
                    break;
                case StructuralLoadType.PointLoadOnMember:
                    equivalent = Frame2DElementMatrices.BuildPointLocalLoad(x, y, geometry.Length,
                        load.Position ?? throw new StructuralAnalysisException($"Load '{load.Id}' has no position."));
                    break;
                default:
                    throw new StructuralAnalysisException($"Unsupported member load type '{load.Type}'.");
            }
            for (int dof = 0; dof < 6; dof++) total[dof] += equivalent[dof];
        }
        return total;
    }

    private static (double X, double Y) ResolveLocalLoad(StructuralLoadDirection direction, double value, MemberGeometry geometry) =>
        direction switch
        {
            StructuralLoadDirection.GlobalX => (geometry.Cosine * value, -geometry.Sine * value),
            StructuralLoadDirection.GlobalY => (geometry.Sine * value, geometry.Cosine * value),
            StructuralLoadDirection.LocalX => (value, 0),
            StructuralLoadDirection.LocalY => (0, value),
            _ => throw new StructuralAnalysisException($"Unsupported member load direction '{direction}'.")
        };

    private static (double LocalUx, double LocalUy, double LocalRz) InterpolateLocalDisplacement(
        double[] localElementDisplacements,
        double length,
        double normalizedPosition)
    {
        double r = normalizedPosition;
        double r2 = r * r;
        double r3 = r2 * r;

        double startAxial = localElementDisplacements[0];
        double startTransverse = localElementDisplacements[1];
        double startRotation = localElementDisplacements[2];
        double endAxial = localElementDisplacements[3];
        double endTransverse = localElementDisplacements[4];
        double endRotation = localElementDisplacements[5];

        double localUx = ((1.0 - r) * startAxial) + (r * endAxial);

        double n1 = 1.0 - (3.0 * r2) + (2.0 * r3);
        double n2 = length * (r - (2.0 * r2) + r3);
        double n3 = (3.0 * r2) - (2.0 * r3);
        double n4 = length * (-r2 + r3);

        double localUy = (n1 * startTransverse) + (n2 * startRotation) + (n3 * endTransverse) + (n4 * endRotation);

        double dn1dx = (-6.0 * r + 6.0 * r2) / length;
        double dn2dx = 1.0 - (4.0 * r) + (3.0 * r2);
        double dn3dx = (6.0 * r - 6.0 * r2) / length;
        double dn4dx = (-2.0 * r) + (3.0 * r2);

        double localRz = (dn1dx * startTransverse) + (dn2dx * startRotation) + (dn3dx * endTransverse) + (dn4dx * endRotation);

        return (localUx, localUy, localRz);
    }

    private sealed record MemberGeometry(double Length, double Cosine, double Sine)
    {
        public static MemberGeometry FromNodes(StructuralNode startNode, StructuralNode endNode)
        {
            double dx = endNode.X - startNode.X;
            double dy = endNode.Y - startNode.Y;
            double length = Math.Sqrt((dx * dx) + (dy * dy));

            return new MemberGeometry(length, dx / length, dy / length);
        }
    }
}
