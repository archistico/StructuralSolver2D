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
    /// Gets or sets whether educational explanations should be included in the report.
    /// </summary>
    public bool IncludeEducationalExplanations { get; set; } = true;

    /// <summary>
    /// Gets or sets whether a compact model-statistics section should be included in the report.
    /// </summary>
    public bool IncludeModelStatistics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether sampled N/V/M values should be included in the report.
    /// </summary>
    public bool IncludeInternalForceSamples { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of internal-force samples printed for each member.
    /// </summary>
    public int MaxSamplesPerMember { get; set; } = 21;

    /// <summary>
    /// Gets or sets whether characteristic internal-force diagram points should be included in the report.
    /// </summary>
    public bool IncludeCharacteristicPoints { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of characteristic points printed for each member.
    /// </summary>
    public int MaxCharacteristicPointsPerMember { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether sampled displacement/deformed-shape values should be included in the report.
    /// </summary>
    public bool IncludeDisplacementSamples { get; set; }

    /// <summary>
    /// Gets or sets whether preliminary deflection check results should be included when supplied.
    /// </summary>
    public bool IncludeDeflectionChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of displacement samples printed for each member.
    /// </summary>
    public int MaxDisplacementSamplesPerMember { get; set; } = 21;

    /// <summary>
    /// Gets or sets the report creation timestamp in UTC.
    /// </summary>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
