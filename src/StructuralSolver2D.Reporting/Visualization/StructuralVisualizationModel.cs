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
        IReadOnlyList<VisualizationSupport>? supports = null,
        IReadOnlyList<VisualizationReactionArrow>? reactionArrows = null,
        IReadOnlyList<VisualizationReactionMoment>? reactionMoments = null,
        IReadOnlyList<MemberDimensionAnnotation>? memberDimensions = null,
        VisualizationDisplacementAnnotation? maximumDisplacement = null,
        IReadOnlyList<DiagramValueAnnotation>? diagramValueAnnotations = null,
        IReadOnlyList<VisualizationAnimationFrame>? animationFrames = null)
    {
        Nodes = nodes;
        Members = members;
        DeformedShapes = deformedShapes;
        Diagrams = diagrams;
        Bounds = bounds;
        DeformationScale = deformationScale;
        Supports = supports ?? Array.Empty<VisualizationSupport>();
        ReactionArrows = reactionArrows ?? Array.Empty<VisualizationReactionArrow>();
        ReactionMoments = reactionMoments ?? Array.Empty<VisualizationReactionMoment>();
        MemberDimensions = memberDimensions ?? Array.Empty<MemberDimensionAnnotation>();
        MaximumDisplacement = maximumDisplacement;
        DiagramValueAnnotations = diagramValueAnnotations ?? Array.Empty<DiagramValueAnnotation>();
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
    /// Gets support glyphs prepared for rendering.
    /// </summary>
    public IReadOnlyList<VisualizationSupport> Supports { get; }

    /// <summary>
    /// Gets support reaction force arrows.
    /// </summary>
    public IReadOnlyList<VisualizationReactionArrow> ReactionArrows { get; }

    /// <summary>
    /// Gets support reaction moment glyphs.
    /// </summary>
    public IReadOnlyList<VisualizationReactionMoment> ReactionMoments { get; }

    /// <summary>
    /// Gets member-length dimension annotations.
    /// </summary>
    public IReadOnlyList<MemberDimensionAnnotation> MemberDimensions { get; }

    /// <summary>
    /// Gets the maximum translational displacement annotation, if available.
    /// </summary>
    public VisualizationDisplacementAnnotation? MaximumDisplacement { get; }

    /// <summary>
    /// Gets maximum-value labels for internal-force diagrams.
    /// </summary>
    public IReadOnlyList<DiagramValueAnnotation> DiagramValueAnnotations { get; }

    /// <summary>
    /// Gets optional prepared animation frames for cyclic deformed-shape visualization.
    /// </summary>
    public IReadOnlyList<VisualizationAnimationFrame> AnimationFrames { get; }
}
