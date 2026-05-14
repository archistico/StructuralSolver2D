using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Converts structural analysis results into UI-independent geometry for graphical viewers.
/// The generated model can be rendered by Avalonia, WPF, SVG, HTML exporters or any custom canvas.
/// </summary>
public sealed class StructuralVisualizationModelBuilder
{
    /// <summary>
    /// Builds a visualization model from the public analysis output bundle.
    /// </summary>
    public StructuralVisualizationModel Build(
        StructuralModel model,
        StructuralAnalysisOutput output,
        VisualizationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(output);

        return Build(
            model,
            output.Result,
            output.InternalForceDiagrams,
            output.DisplacementDiagrams,
            options);
    }

    /// <summary>
    /// Builds a visualization model from structural model geometry and solver results.
    /// </summary>
    public StructuralVisualizationModel Build(
        StructuralModel model,
        StructuralAnalysisResult result,
        IReadOnlyList<MemberInternalForceDiagram> internalForceDiagrams,
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams,
        VisualizationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(internalForceDiagrams);
        ArgumentNullException.ThrowIfNull(displacementDiagrams);

        options ??= new VisualizationOptions();
        ValidateOptions(options);

        Dictionary<string, StructuralNode> nodesById = model.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, NodalDisplacementResult> displacementsByNodeId = result.Displacements.ToDictionary(item => item.NodeId, StringComparer.OrdinalIgnoreCase);

        List<VisualizationPoint> allPoints = new();

        List<VisualizationNode> nodes = model.Nodes
            .Select(node => CreateVisualizationNode(node, displacementsByNodeId, options.DeformationScale, allPoints))
            .ToList();

        List<VisualizationMember> members = model.Members
            .Select(member => CreateVisualizationMember(member, nodesById, allPoints))
            .ToList();

        List<VisualizationSupport> supports = CreateSupportGlyphs(model, nodesById);
        List<MemberDimensionAnnotation> dimensions = CreateMemberDimensions(members);

        List<DeformedMemberShape> deformedShapes = CreateDeformedShapes(
            model,
            nodesById,
            displacementsByNodeId,
            displacementDiagrams,
            options.DeformationScale,
            allPoints);

        (List<MemberDiagramPolyline> diagrams, List<DiagramValueAnnotation> diagramAnnotations) = CreateDiagramPolylines(
            model,
            nodesById,
            internalForceDiagrams,
            options,
            allPoints);

        (List<VisualizationReactionArrow> reactionArrows, List<VisualizationReactionMoment> reactionMoments) = CreateReactionVisualizations(
            model,
            nodesById,
            result.Reactions,
            options,
            allPoints);

        VisualizationDisplacementAnnotation? maximumDisplacement = CreateMaximumDisplacementAnnotation(nodes);

        IReadOnlyList<VisualizationAnimationFrame> animationFrames = CreateAnimationFrames(
            model,
            nodesById,
            displacementsByNodeId,
            displacementDiagrams,
            options,
            allPoints);

        VisualizationBounds bounds = CalculateBounds(allPoints, options.BoundsPadding);

        return new StructuralVisualizationModel(
            nodes,
            members,
            deformedShapes,
            diagrams,
            bounds,
            options.DeformationScale,
            supports,
            reactionArrows,
            reactionMoments,
            dimensions,
            maximumDisplacement,
            diagramAnnotations,
            animationFrames);
    }

    private static VisualizationNode CreateVisualizationNode(
        StructuralNode node,
        IReadOnlyDictionary<string, NodalDisplacementResult> displacementsByNodeId,
        double deformationScale,
        ICollection<VisualizationPoint> allPoints)
    {
        displacementsByNodeId.TryGetValue(node.Id, out NodalDisplacementResult? displacement);

        double ux = displacement?.Ux ?? 0.0;
        double uy = displacement?.Uy ?? 0.0;
        double rz = displacement?.Rz ?? 0.0;

        VisualizationPoint position = new(node.X, node.Y);
        VisualizationPoint deformedPosition = new(
            node.X + (ux * deformationScale),
            node.Y + (uy * deformationScale));

        allPoints.Add(position);
        allPoints.Add(deformedPosition);

        return new VisualizationNode(node.Id, node.Label, position, deformedPosition, ux, uy, rz);
    }

    private static VisualizationMember CreateVisualizationMember(
        StructuralMember member,
        IReadOnlyDictionary<string, StructuralNode> nodesById,
        ICollection<VisualizationPoint> allPoints)
    {
        StructuralNode startNode = nodesById[member.StartNodeId];
        StructuralNode endNode = nodesById[member.EndNodeId];

        VisualizationPoint start = new(startNode.X, startNode.Y);
        VisualizationPoint end = new(endNode.X, endNode.Y);

        allPoints.Add(start);
        allPoints.Add(end);

        return new VisualizationMember(
            member.Id,
            member.StartNodeId,
            member.EndNodeId,
            member.Type,
            start,
            end);
    }

    private static List<VisualizationSupport> CreateSupportGlyphs(
        StructuralModel model,
        IReadOnlyDictionary<string, StructuralNode> nodesById)
    {
        List<VisualizationSupport> supports = new(model.Supports.Count);

        foreach (StructuralSupport support in model.Supports)
        {
            if (!nodesById.TryGetValue(support.NodeId, out StructuralNode? node))
            {
                continue;
            }

            supports.Add(new VisualizationSupport(
                support.Id,
                support.NodeId,
                MapSupportGlyphKind(support),
                new VisualizationPoint(node.X, node.Y),
                support.Label));
        }

        return supports;
    }

    private static SupportGlyphKind MapSupportGlyphKind(StructuralSupport support) =>
        support.Type switch
        {
            SupportType.SimpleSupport => SupportGlyphKind.SimpleSupport,
            SupportType.Roller => SupportGlyphKind.SimpleSupport,
            SupportType.Hinge => SupportGlyphKind.Hinge,
            SupportType.Fixed => SupportGlyphKind.Fixed,
            _ when support.RestrainedUx && support.RestrainedUy && !support.RestrainedRz => SupportGlyphKind.Hinge,
            _ when !support.RestrainedUx && support.RestrainedUy && !support.RestrainedRz => SupportGlyphKind.SimpleSupport,
            _ when support.RestrainedUx && support.RestrainedUy && support.RestrainedRz => SupportGlyphKind.Fixed,
            _ => SupportGlyphKind.Custom,
        };

    private static List<MemberDimensionAnnotation> CreateMemberDimensions(IReadOnlyList<VisualizationMember> members)
    {
        List<MemberDimensionAnnotation> dimensions = new(members.Count);

        foreach (VisualizationMember member in members)
        {
            double dx = member.End.X - member.Start.X;
            double dy = member.End.Y - member.Start.Y;
            double distance = Math.Sqrt((dx * dx) + (dy * dy));
            dimensions.Add(new MemberDimensionAnnotation(member.MemberId, member.Start, member.End, distance));
        }

        return dimensions;
    }

    private static List<DeformedMemberShape> CreateDeformedShapes(
        StructuralModel model,
        IReadOnlyDictionary<string, StructuralNode> nodesById,
        IReadOnlyDictionary<string, NodalDisplacementResult> displacementsByNodeId,
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams,
        double deformationScale,
        ICollection<VisualizationPoint> allPoints)
    {
        Dictionary<string, MemberDisplacementDiagram> diagramsByMemberId = displacementDiagrams.ToDictionary(item => item.MemberId, StringComparer.OrdinalIgnoreCase);
        List<DeformedMemberShape> shapes = new();

        foreach (StructuralMember member in model.Members)
        {
            StructuralNode startNode = nodesById[member.StartNodeId];
            StructuralNode endNode = nodesById[member.EndNodeId];

            IReadOnlyList<VisualizationPoint> points = diagramsByMemberId.TryGetValue(member.Id, out MemberDisplacementDiagram? diagram)
                ? CreateCurvedDeformedShape(startNode, endNode, diagram, deformationScale)
                : CreateStraightDeformedShape(startNode, endNode, member, displacementsByNodeId, deformationScale);

            foreach (VisualizationPoint point in points)
            {
                allPoints.Add(point);
            }

            shapes.Add(new DeformedMemberShape(member.Id, points));
        }

        return shapes;
    }

    private static IReadOnlyList<VisualizationPoint> CreateCurvedDeformedShape(
        StructuralNode startNode,
        StructuralNode endNode,
        MemberDisplacementDiagram diagram,
        double deformationScale)
    {
        return diagram.Samples
            .Select(sample =>
            {
                double x = Interpolate(startNode.X, endNode.X, sample.NormalizedPosition);
                double y = Interpolate(startNode.Y, endNode.Y, sample.NormalizedPosition);

                return new VisualizationPoint(
                    x + (sample.GlobalUx * deformationScale),
                    y + (sample.GlobalUy * deformationScale));
            })
            .ToList();
    }

    private static IReadOnlyList<VisualizationPoint> CreateStraightDeformedShape(
        StructuralNode startNode,
        StructuralNode endNode,
        StructuralMember member,
        IReadOnlyDictionary<string, NodalDisplacementResult> displacementsByNodeId,
        double deformationScale)
    {
        displacementsByNodeId.TryGetValue(member.StartNodeId, out NodalDisplacementResult? startDisplacement);
        displacementsByNodeId.TryGetValue(member.EndNodeId, out NodalDisplacementResult? endDisplacement);

        return new[]
        {
            new VisualizationPoint(
                startNode.X + ((startDisplacement?.Ux ?? 0.0) * deformationScale),
                startNode.Y + ((startDisplacement?.Uy ?? 0.0) * deformationScale)),
            new VisualizationPoint(
                endNode.X + ((endDisplacement?.Ux ?? 0.0) * deformationScale),
                endNode.Y + ((endDisplacement?.Uy ?? 0.0) * deformationScale)),
        };
    }

    private static (List<MemberDiagramPolyline> Polylines, List<DiagramValueAnnotation> Annotations) CreateDiagramPolylines(
        StructuralModel model,
        IReadOnlyDictionary<string, StructuralNode> nodesById,
        IReadOnlyList<MemberInternalForceDiagram> internalForceDiagrams,
        VisualizationOptions options,
        ICollection<VisualizationPoint> allPoints)
    {
        Dictionary<string, StructuralMember> membersById = model.Members.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        List<MemberDiagramPolyline> polylines = new();
        List<DiagramValueAnnotation> annotations = new();

        foreach (MemberInternalForceDiagram diagram in internalForceDiagrams)
        {
            if (!membersById.TryGetValue(diagram.MemberId, out StructuralMember? member))
            {
                continue;
            }

            StructuralNode startNode = nodesById[member.StartNodeId];
            StructuralNode endNode = nodesById[member.EndNodeId];

            if (options.IncludeNormalForceDiagram)
            {
                polylines.Add(CreateDiagramPolyline(startNode, endNode, diagram, VisualizationDiagramKind.NormalForce, options.NormalForceDiagramScale, sample => sample.NormalForce, allPoints, annotations));
            }

            if (options.IncludeShearForceDiagram)
            {
                polylines.Add(CreateDiagramPolyline(startNode, endNode, diagram, VisualizationDiagramKind.ShearForce, options.ShearForceDiagramScale, sample => sample.ShearForce, allPoints, annotations));
            }

            if (options.IncludeBendingMomentDiagram)
            {
                polylines.Add(CreateDiagramPolyline(startNode, endNode, diagram, VisualizationDiagramKind.BendingMoment, options.BendingMomentDiagramScale, sample => sample.BendingMoment, allPoints, annotations));
            }
        }

        return (polylines, annotations);
    }

    private static MemberDiagramPolyline CreateDiagramPolyline(
        StructuralNode startNode,
        StructuralNode endNode,
        MemberInternalForceDiagram diagram,
        VisualizationDiagramKind kind,
        double scale,
        Func<MemberInternalForceSample, double> valueSelector,
        ICollection<VisualizationPoint> allPoints,
        ICollection<DiagramValueAnnotation> annotations)
    {
        (double normalX, double normalY) = GetLocalNormal(startNode, endNode);
        List<VisualizationPoint> points = new();
        double maxAbsValue = 0.0;
        double extremumValue = 0.0;
        VisualizationPoint? extremumPoint = null;

        foreach (MemberInternalForceSample sample in diagram.Samples)
        {
            double baseX = Interpolate(startNode.X, endNode.X, sample.Position);
            double baseY = Interpolate(startNode.Y, endNode.Y, sample.Position);
            double value = valueSelector(sample);

            VisualizationPoint point = new(
                baseX + (normalX * value * scale),
                baseY + (normalY * value * scale));

            points.Add(point);
            allPoints.Add(point);

            if (Math.Abs(value) >= maxAbsValue)
            {
                maxAbsValue = Math.Abs(value);
                extremumValue = value;
                extremumPoint = point;
            }
        }

        if (extremumPoint is not null)
        {
            annotations.Add(new DiagramValueAnnotation(diagram.MemberId, kind, extremumPoint, extremumValue, maxAbsValue));
        }

        return new MemberDiagramPolyline(diagram.MemberId, kind, points, maxAbsValue);
    }

    private static (double NormalX, double NormalY) GetLocalNormal(StructuralNode startNode, StructuralNode endNode)
    {
        double dx = endNode.X - startNode.X;
        double dy = endNode.Y - startNode.Y;
        double length = Math.Sqrt((dx * dx) + (dy * dy));

        if (length <= 0.0)
        {
            throw new InvalidOperationException("Cannot create visualization geometry for a zero-length member.");
        }

        return (-dy / length, dx / length);
    }

    private static (List<VisualizationReactionArrow> Arrows, List<VisualizationReactionMoment> Moments) CreateReactionVisualizations(
        StructuralModel model,
        IReadOnlyDictionary<string, StructuralNode> nodesById,
        IReadOnlyList<SupportReactionResult> reactions,
        VisualizationOptions options,
        ICollection<VisualizationPoint> allPoints)
    {
        Dictionary<string, SupportReactionResult> reactionsBySupportId = reactions.ToDictionary(item => item.SupportId, StringComparer.OrdinalIgnoreCase);
        List<VisualizationReactionArrow> arrows = new();
        List<VisualizationReactionMoment> moments = new();
        const double tolerance = 1e-12;

        foreach (StructuralSupport support in model.Supports)
        {
            if (!nodesById.TryGetValue(support.NodeId, out StructuralNode? node) ||
                !reactionsBySupportId.TryGetValue(support.Id, out SupportReactionResult? reaction))
            {
                continue;
            }

            VisualizationPoint center = new(node.X, node.Y);

            if (Math.Abs(reaction.Fx) > tolerance)
            {
                VisualizationPoint end = new(node.X + (reaction.Fx * options.ReactionForceScale), node.Y);
                arrows.Add(new VisualizationReactionArrow(support.Id, support.NodeId, ReactionComponentKind.ForceX, center, end, reaction.Fx));
                allPoints.Add(end);
            }

            if (Math.Abs(reaction.Fy) > tolerance)
            {
                VisualizationPoint end = new(node.X, node.Y + (reaction.Fy * options.ReactionForceScale));
                arrows.Add(new VisualizationReactionArrow(support.Id, support.NodeId, ReactionComponentKind.ForceY, center, end, reaction.Fy));
                allPoints.Add(end);
            }

            if (Math.Abs(reaction.Mz) > tolerance)
            {
                double radius = Math.Max(options.MinimumReactionMomentRadius, Math.Abs(reaction.Mz) * options.ReactionMomentScale);
                moments.Add(new VisualizationReactionMoment(support.Id, support.NodeId, center, radius, reaction.Mz < 0.0, reaction.Mz));
                allPoints.Add(new VisualizationPoint(node.X - radius, node.Y - radius));
                allPoints.Add(new VisualizationPoint(node.X + radius, node.Y + radius));
            }
        }

        return (arrows, moments);
    }

    private static VisualizationDisplacementAnnotation? CreateMaximumDisplacementAnnotation(IReadOnlyList<VisualizationNode> nodes)
    {
        VisualizationNode? maximumNode = null;
        double maximumMagnitude = 0.0;

        foreach (VisualizationNode node in nodes)
        {
            double magnitude = Math.Sqrt((node.Ux * node.Ux) + (node.Uy * node.Uy));
            if (magnitude >= maximumMagnitude)
            {
                maximumMagnitude = magnitude;
                maximumNode = node;
            }
        }

        if (maximumNode is null || maximumMagnitude <= 0.0)
        {
            return null;
        }

        return new VisualizationDisplacementAnnotation(
            maximumNode.NodeId,
            maximumNode.Position,
            maximumNode.DeformedPosition,
            maximumMagnitude);
    }

    private static IReadOnlyList<VisualizationAnimationFrame> CreateAnimationFrames(
        StructuralModel model,
        IReadOnlyDictionary<string, StructuralNode> nodesById,
        IReadOnlyDictionary<string, NodalDisplacementResult> displacementsByNodeId,
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams,
        VisualizationOptions options,
        ICollection<VisualizationPoint> allPoints)
    {
        if (options.AnimationFrameCount == 0)
        {
            return Array.Empty<VisualizationAnimationFrame>();
        }

        List<VisualizationAnimationFrame> frames = new(options.AnimationFrameCount);

        for (int index = 0; index < options.AnimationFrameCount; index++)
        {
            double factor = Math.Sin((Math.PI * 2.0 * index) / options.AnimationFrameCount);
            double frameScale = options.DeformationScale * factor;
            List<VisualizationNode> frameNodes = model.Nodes
                .Select(node => CreateVisualizationNode(node, displacementsByNodeId, frameScale, allPoints))
                .ToList();
            List<DeformedMemberShape> frameShapes = CreateDeformedShapes(
                model,
                nodesById,
                displacementsByNodeId,
                displacementDiagrams,
                frameScale,
                allPoints);

            frames.Add(new VisualizationAnimationFrame(index, factor, frameNodes, frameShapes));
        }

        return frames;
    }

    private static VisualizationBounds CalculateBounds(IReadOnlyCollection<VisualizationPoint> points, double padding)
    {
        if (points.Count == 0)
        {
            return VisualizationBounds.Empty;
        }

        double minX = points.Min(point => point.X) - padding;
        double minY = points.Min(point => point.Y) - padding;
        double maxX = points.Max(point => point.X) + padding;
        double maxY = points.Max(point => point.Y) + padding;

        return new VisualizationBounds(minX, minY, maxX, maxY);
    }

    private static double Interpolate(double start, double end, double position) =>
        start + ((end - start) * position);

    private static void ValidateOptions(VisualizationOptions options)
    {
        if (options.DeformationScale < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The deformation scale cannot be negative.");
        }

        if (options.NormalForceDiagramScale < 0.0 ||
            options.ShearForceDiagramScale < 0.0 ||
            options.BendingMomentDiagramScale < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Diagram scales cannot be negative.");
        }

        if (options.ReactionForceScale < 0.0 ||
            options.ReactionMomentScale < 0.0 ||
            options.MinimumReactionMomentRadius < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Reaction scales and minimum radius cannot be negative.");
        }

        if (options.BoundsPadding < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Bounds padding cannot be negative.");
        }

        if (options.AnimationFrameCount < 0 || options.AnimationFrameCount == 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Animation frame count must be 0 or at least 2.");
        }
    }
}
