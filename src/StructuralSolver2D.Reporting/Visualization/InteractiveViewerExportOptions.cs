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
    /// Gets or sets the SVG options used for the embedded scene.
    /// </summary>
    public SvgExportOptions SvgOptions { get; set; } = new()
    {
        Width = 1400.0,
        Height = 900.0,
    };
}
