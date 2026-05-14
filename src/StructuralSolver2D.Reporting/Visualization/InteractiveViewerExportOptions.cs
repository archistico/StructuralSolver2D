namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Controls the standalone interactive HTML viewer export.
/// </summary>
public sealed class InteractiveViewerExportOptions
{
    /// <summary>
    /// Gets or sets the HTML page title.
    /// </summary>
    public string Title { get; set; } = "StructuralSolver2D interactive viewer";

    /// <summary>
    /// Gets or sets an optional description displayed above the viewer.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether nodal displacement labels are visible when the viewer opens.
    /// The labels are always embedded so the user can toggle them from the toolbar.
    /// </summary>
    public bool ShowNodeDisplacementLabelsByDefault { get; set; } = false;

    /// <summary>
    /// Gets or sets whether member displacement labels at L/4, L/2 and 3L/4 are visible when the viewer opens.
    /// The labels are always embedded so the user can toggle them from the toolbar.
    /// </summary>
    public bool ShowMemberDisplacementLabelsByDefault { get; set; } = false;



    /// <summary>
    /// Gets or sets whether applied loads are visible when the viewer opens.
    /// </summary>
    public bool ShowLoadsByDefault { get; set; } = true;
    /// <summary>
    /// Gets or sets the initial deformation amplification applied by the viewer.
    /// 100 means the scale already used when the SVG was generated.
    /// </summary>
    public double InitialDeformationVisualScalePercent { get; set; } = 100.0;

    /// <summary>
    /// Gets or sets the default animation amplitude in percent of the exported deformed-shape amplitude.
    /// </summary>
    public double InitialAnimationAmplitudePercent { get; set; } = 100.0;

    /// <summary>
    /// Gets or sets the default animation speed multiplier.
    /// </summary>
    public double InitialAnimationSpeed { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the SVG options used for the embedded scene.
    /// </summary>
    public SvgExportOptions SvgOptions { get; set; } = new()
    {
        Width = 1400.0,
        Height = 900.0,
    };
}
