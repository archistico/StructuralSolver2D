using StructuralSolver2D.Analysis.Results;
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
