using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Frame2D;

/// <summary>
/// Samples local internal forces N, V and M along 2D frame members after an analysis.
/// This class is a post-processing component: it does not solve the structure again.
/// </summary>
public sealed class Frame2DInternalForceSampler
{
    /// <summary>
    /// Samples the internal-force diagram of one member.
    /// </summary>
    /// <param name="model">Structural model used for the analysis.</param>
    /// <param name="analysisResult">Analysis result for the requested load case.</param>
    /// <param name="memberId">Member identifier to sample.</param>
    /// <param name="sampleCount">Number of samples. Must be at least 2.</param>
    /// <returns>Sampled member internal-force diagram.</returns>
    public MemberInternalForceDiagram SampleMember(
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

        if (!members.TryGetValue(memberId, out StructuralMember? member))
        {
            throw new StructuralAnalysisException($"Member '{memberId}' was not found in the structural model.");
        }

        MemberEndForceResult endForces = analysisResult.MemberEndForces.FirstOrDefault(
            result => string.Equals(result.MemberId, memberId, StringComparison.OrdinalIgnoreCase))
            ?? throw new StructuralAnalysisException($"Member end forces for member '{memberId}' were not found in the analysis result.");

        StructuralNode startNode = nodes[member.StartNodeId];
        StructuralNode endNode = nodes[member.EndNodeId];
        MemberGeometry geometry = MemberGeometry.FromNodes(startNode, endNode);
        (double localXLoad, double localYLoad) = SumUniformLoadsInLocalCoordinates(model, analysisResult.LoadCaseId, memberId, geometry);
        IReadOnlyList<ConcentratedLocalLoad> pointLoads = GetPointLoadsInLocalCoordinates(model, analysisResult.LoadCaseId, memberId, geometry);

        List<MemberInternalForceSample> samples = new(sampleCount);

        for (int index = 0; index < sampleCount; index++)
        {
            double position = (double)index / (sampleCount - 1);
            double x = position * geometry.Length;

            double normalForce = -endForces.StartAxial - (localXLoad * x);
            double shearForce = endForces.StartShear + (localYLoad * x);
            double bendingMoment = -endForces.StartMoment + (endForces.StartShear * x) + (localYLoad * x * x / 2.0);

            foreach (ConcentratedLocalLoad pointLoad in pointLoads.Where(pointLoad => pointLoad.DistanceFromStart <= x))
            {
                normalForce -= pointLoad.LocalXValue;
                shearForce += pointLoad.LocalYValue;
                bendingMoment += pointLoad.LocalYValue * (x - pointLoad.DistanceFromStart);
            }

            samples.Add(new MemberInternalForceSample(
                memberId,
                position,
                x,
                normalForce,
                shearForce,
                bendingMoment));
        }

        return new MemberInternalForceDiagram(memberId, geometry.Length, samples);
    }

    /// <summary>
    /// Samples all members available in the analysis result.
    /// </summary>
    /// <param name="model">Structural model used for the analysis.</param>
    /// <param name="analysisResult">Analysis result for the requested load case.</param>
    /// <param name="sampleCount">Number of samples per member. Must be at least 2.</param>
    /// <returns>Sampled diagrams ordered as member end forces appear in the analysis result.</returns>
    public IReadOnlyList<MemberInternalForceDiagram> SampleAllMembers(
        StructuralModel model,
        StructuralAnalysisResult analysisResult,
        int sampleCount = 21) =>
        analysisResult.MemberEndForces
            .Select(memberEndForce => SampleMember(model, analysisResult, memberEndForce.MemberId, sampleCount))
            .ToList();

    private static (double LocalX, double LocalY) SumUniformLoadsInLocalCoordinates(
        StructuralModel model,
        string loadCaseId,
        string memberId,
        MemberGeometry geometry)
    {
        double localX = 0;
        double localY = 0;

        foreach (StructuralLoad load in model.Loads.Where(load =>
            string.Equals(load.LoadCaseId, loadCaseId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(load.TargetId, memberId, StringComparison.OrdinalIgnoreCase) &&
            load.Type == StructuralLoadType.UniformDistributedLoad))
        {
            (double loadLocalX, double loadLocalY) = ResolveUniformLoadInLocalCoordinates(load, geometry);
            localX += loadLocalX;
            localY += loadLocalY;
        }

        return (localX, localY);
    }


    private static IReadOnlyList<ConcentratedLocalLoad> GetPointLoadsInLocalCoordinates(
        StructuralModel model,
        string loadCaseId,
        string memberId,
        MemberGeometry geometry) =>
        model.Loads
            .Where(load =>
                string.Equals(load.LoadCaseId, loadCaseId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(load.TargetId, memberId, StringComparison.OrdinalIgnoreCase) &&
                load.Type == StructuralLoadType.PointLoadOnMember)
            .Select(load =>
            {
                if (!load.Position.HasValue)
                {
                    throw new StructuralAnalysisException($"Point load '{load.Id}' has no normalized position.");
                }

                (double localX, double localY) = ResolveConcentratedLoadInLocalCoordinates(load, geometry);
                return new ConcentratedLocalLoad(load.Position.Value * geometry.Length, localX, localY);
            })
            .OrderBy(load => load.DistanceFromStart)
            .ToList();

    private static (double LocalX, double LocalY) ResolveConcentratedLoadInLocalCoordinates(
        StructuralLoad load,
        MemberGeometry geometry) =>
        load.Direction switch
        {
            StructuralLoadDirection.LocalX => (load.Value, 0),
            StructuralLoadDirection.LocalY => (0, load.Value),
            StructuralLoadDirection.GlobalX => (geometry.Cosine * load.Value, -geometry.Sine * load.Value),
            StructuralLoadDirection.GlobalY => (geometry.Sine * load.Value, geometry.Cosine * load.Value),
            _ => throw new StructuralAnalysisException($"Unsupported point load direction '{load.Direction}'.")
        };

    private static (double LocalX, double LocalY) ResolveUniformLoadInLocalCoordinates(
        StructuralLoad load,
        MemberGeometry geometry) =>
        load.Direction switch
        {
            StructuralLoadDirection.LocalX => (load.Value, 0),
            StructuralLoadDirection.LocalY => (0, load.Value),
            StructuralLoadDirection.GlobalX => (geometry.Cosine * load.Value, -geometry.Sine * load.Value),
            StructuralLoadDirection.GlobalY => (geometry.Sine * load.Value, geometry.Cosine * load.Value),
            _ => throw new StructuralAnalysisException($"Unsupported uniform load direction '{load.Direction}'.")
        };

    private sealed record ConcentratedLocalLoad(double DistanceFromStart, double LocalXValue, double LocalYValue);

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
