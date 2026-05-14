namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Controls how solver results are converted into viewer-ready geometry.
/// </summary>
public sealed class VisualizationOptions
{
    /// <summary>
    /// Gets or sets the scale factor applied to translational displacements when drawing the deformed shape.
    /// Example: 100.0 means 1 mm of displacement is drawn as 100 mm.
    /// </summary>
    public double DeformationScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the scale factor applied to normal-force diagram offsets.
    /// </summary>
    public double NormalForceDiagramScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the scale factor applied to shear-force diagram offsets.
    /// </summary>
    public double ShearForceDiagramScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the scale factor applied to bending-moment diagram offsets.
    /// </summary>
    public double BendingMomentDiagramScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether normal-force diagram geometry should be generated.
    /// </summary>
    public bool IncludeNormalForceDiagram { get; set; } = true;

    /// <summary>
    /// Gets or sets whether shear-force diagram geometry should be generated.
    /// </summary>
    public bool IncludeShearForceDiagram { get; set; } = true;

    /// <summary>
    /// Gets or sets whether bending-moment diagram geometry should be generated.
    /// </summary>
    public bool IncludeBendingMomentDiagram { get; set; } = true;

    /// <summary>
    /// Gets or sets a value used to expand computed drawing bounds.
    /// </summary>
    public double BoundsPadding { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the number of cyclic deformed-shape animation frames to prepare.
    /// Use 0 to disable animation-frame generation.
    /// </summary>
    public int AnimationFrameCount { get; set; } = 0;


    /// <summary>
    /// Gets or sets the scale factor applied to support reaction force arrows.
    /// </summary>
    public double ReactionForceScale { get; set; } = 0.02;

    /// <summary>
    /// Gets or sets the scale factor applied to support reaction moment glyph radii.
    /// </summary>
    public double ReactionMomentScale { get; set; } = 0.02;

    /// <summary>
    /// Gets or sets the minimum reaction moment glyph radius in model units.
    /// </summary>
    public double MinimumReactionMomentRadius { get; set; } = 0.15;
}
