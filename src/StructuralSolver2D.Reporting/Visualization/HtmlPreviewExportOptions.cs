namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Controls how a static HTML preview page is produced for a structural result visualization.
/// </summary>
public sealed class HtmlPreviewExportOptions
{
    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title { get; set; } = "Structural result preview";

    /// <summary>
    /// Gets or sets an optional short description shown above the preview.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the SVG export options used for the embedded inline SVG.
    /// </summary>
    public SvgExportOptions SvgOptions { get; set; } = new();
}
