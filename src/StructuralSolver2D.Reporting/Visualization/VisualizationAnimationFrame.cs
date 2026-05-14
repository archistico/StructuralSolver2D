namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Represents one prepared animation frame for a deformed-shape viewer.
/// </summary>
/// <param name="Index">Zero-based frame index.</param>
/// <param name="Factor">Animation multiplier applied to the configured deformation scale.</param>
/// <param name="Nodes">Nodes with frame-specific deformed coordinates.</param>
/// <param name="DeformedShapes">Members with frame-specific deformed polylines.</param>
public sealed record VisualizationAnimationFrame(
    int Index,
    double Factor,
    IReadOnlyList<VisualizationNode> Nodes,
    IReadOnlyList<DeformedMemberShape> DeformedShapes);
