namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Contains viewer-ready structural geometry and result geometry.
/// This class is intentionally independent from a specific UI toolkit.
/// </summary>
public sealed class StructuralVisualizationModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralVisualizationModel"/> class.
    /// </summary>
    public StructuralVisualizationModel(
        IReadOnlyList<VisualizationNode> nodes,
        IReadOnlyList<VisualizationMember> members,
        IReadOnlyList<DeformedMemberShape> deformedShapes,
        IReadOnlyList<MemberDiagramPolyline> diagrams,
        VisualizationBounds bounds,
        double deformationScale,
        IReadOnlyList<VisualizationAnimationFrame>? animationFrames = null)
    {
        Nodes = nodes;
        Members = members;
        DeformedShapes = deformedShapes;
        Diagrams = diagrams;
        Bounds = bounds;
        DeformationScale = deformationScale;
        AnimationFrames = animationFrames ?? Array.Empty<VisualizationAnimationFrame>();
    }

    /// <summary>
    /// Gets the nodes with undeformed and deformed coordinates.
    /// </summary>
    public IReadOnlyList<VisualizationNode> Nodes { get; }

    /// <summary>
    /// Gets undeformed member axis segments.
    /// </summary>
    public IReadOnlyList<VisualizationMember> Members { get; }

    /// <summary>
    /// Gets scaled deformed member shapes.
    /// </summary>
    public IReadOnlyList<DeformedMemberShape> DeformedShapes { get; }

    /// <summary>
    /// Gets internal-force diagram polylines.
    /// </summary>
    public IReadOnlyList<MemberDiagramPolyline> Diagrams { get; }

    /// <summary>
    /// Gets the drawing bounds including model geometry and result geometry.
    /// </summary>
    public VisualizationBounds Bounds { get; }

    /// <summary>
    /// Gets the deformation scale used to generate this model.
    /// </summary>
    public double DeformationScale { get; }

    /// <summary>
    /// Gets optional prepared animation frames for cyclic deformed-shape visualization.
    /// </summary>
    public IReadOnlyList<VisualizationAnimationFrame> AnimationFrames { get; }
}
