namespace StructuralSolver2D.Reporting.Markdown;

/// <summary>
/// Configures the Markdown structural report generator.
/// </summary>
public sealed class MarkdownReportOptions
{
    /// <summary>
    /// Gets or sets the report title.
    /// </summary>
    public string Title { get; set; } = "StructuralSolver2D Report";

    /// <summary>
    /// Gets or sets a short model description.
    /// </summary>
    public string Description { get; set; } = "Structural analysis report.";

    /// <summary>
    /// Gets or sets the source label shown in the report, for example an input JSON path.
    /// </summary>
    public string SourceLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether sampled N/V/M values should be included in the report.
    /// </summary>
    public bool IncludeInternalForceSamples { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of internal-force samples printed for each member.
    /// </summary>
    public int MaxSamplesPerMember { get; set; } = 21;

    /// <summary>
    /// Gets or sets the report creation timestamp in UTC.
    /// </summary>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
