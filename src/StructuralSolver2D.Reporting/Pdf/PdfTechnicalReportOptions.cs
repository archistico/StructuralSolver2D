namespace StructuralSolver2D.Reporting.Pdf;

/// <summary>
/// Controls the content and layout of the lightweight PDF technical report.
/// </summary>
public sealed class PdfTechnicalReportOptions
{
    /// <summary>
    /// Gets or sets the report title.
    /// </summary>
    public string Title { get; set; } = "StructuralSolver2D technical report";

    /// <summary>
    /// Gets or sets an optional model or project description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets an optional source label, usually the input JSON path.
    /// </summary>
    public string? SourceLabel { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of detail rows written for long result sections.
    /// </summary>
    public int MaxRowsPerSection { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether sampled internal-force rows should be included.
    /// </summary>
    public bool IncludeInternalForceSamples { get; set; } = true;

    /// <summary>
    /// Gets or sets whether sampled displacement rows should be included.
    /// </summary>
    public bool IncludeDisplacementSamples { get; set; } = true;

    /// <summary>
    /// Validates option values and throws a clear exception if they are not usable.
    /// </summary>
    public void Validate()
    {
        if (MaxRowsPerSection < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxRowsPerSection),
                MaxRowsPerSection,
                "At least one detail row per section is required.");
        }
    }
}
