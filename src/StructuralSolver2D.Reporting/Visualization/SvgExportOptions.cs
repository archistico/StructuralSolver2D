namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Controls how a <see cref="StructuralVisualizationModel"/> is exported to SVG.
/// </summary>
public sealed class SvgExportOptions
{
    /// <summary>
    /// Gets or sets the output SVG width in CSS pixels.
    /// </summary>
    public double Width { get; set; } = 1200.0;

    /// <summary>
    /// Gets or sets the output SVG height in CSS pixels.
    /// </summary>
    public double Height { get; set; } = 800.0;

    /// <summary>
    /// Gets or sets the inner margin between the SVG border and the plotted structural geometry.
    /// </summary>
    public double Padding { get; set; } = 32.0;

    /// <summary>
    /// Gets or sets the optional document title shown in the SVG metadata and top caption.
    /// </summary>
    public string Title { get; set; } = "Structural result preview";

    /// <summary>
    /// Gets or sets whether the undeformed model geometry should be drawn.
    /// </summary>
    public bool IncludeUndeformedModel { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the deformed shape should be drawn.
    /// </summary>
    public bool IncludeDeformedShape { get; set; } = true;

    /// <summary>
    /// Gets or sets whether internal-force diagrams should be drawn.
    /// </summary>
    public bool IncludeInternalForceDiagrams { get; set; } = true;

    /// <summary>
    /// Gets or sets whether node labels should be drawn.
    /// </summary>
    public bool IncludeNodeLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets whether a small legend should be drawn.
    /// </summary>
    public bool IncludeLegend { get; set; } = true;

    /// <summary>
    /// Validates option values and throws a clear exception if they are not usable.
    /// </summary>
    public void Validate()
    {
        if (Width <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), Width, "The SVG width must be greater than zero.");
        }

        if (Height <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(Height), Height, "The SVG height must be greater than zero.");
        }

        if (Padding < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(Padding), Padding, "The SVG padding cannot be negative.");
        }

        if ((Padding * 2.0) >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(Padding), Padding, "The SVG padding is too large for the configured width.");
        }

        if ((Padding * 2.0) >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(Padding), Padding, "The SVG padding is too large for the configured height.");
        }
    }


    /// <summary>
    /// Gets or sets whether support symbols should be drawn.
    /// </summary>
    public bool IncludeSupportSymbols { get; set; } = true;

    /// <summary>
    /// Gets or sets whether support reactions should be drawn.
    /// </summary>
    public bool IncludeReactions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether member dimensions should be drawn.
    /// </summary>
    public bool IncludeMemberDimensions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the maximum displacement annotation should be drawn.
    /// </summary>
    public bool IncludeMaximumDisplacementAnnotation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether nodal displacement labels should be drawn near the deformed shape.
    /// </summary>
    public bool IncludeNodeDisplacementLabels { get; set; } = false;

    /// <summary>
    /// Gets or sets whether member displacement labels should be drawn at L/4, L/2 and 3L/4.
    /// </summary>
    public bool IncludeMemberDisplacementLabels { get; set; } = false;

    /// <summary>
    /// Gets or sets whether maximum-value labels should be drawn on internal-force diagrams.
    /// </summary>
    public bool IncludeDiagramValueLabels { get; set; } = true;
}
