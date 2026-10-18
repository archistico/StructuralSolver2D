namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Controls the standalone interactive HTML viewer export.
/// </summary>
public sealed class InteractiveViewerExportOptions
{
    /// <summary>Initial result mode. Model keeps result diagrams and deformation hidden until requested.</summary>
    public ViewerResultView InitialView { get; set; } = ViewerResultView.Model;

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
    /// Gets or sets the initial SVG label size in percent.
    /// 100 means the original exported text size.
    /// </summary>
    public double InitialTextScalePercent { get; set; } = 100.0;

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

    /// <summary>Validates initial values against the ranges supported by the viewer controls.</summary>
    public void Validate()
    {
        if (!Enum.IsDefined(InitialView)) throw new ArgumentOutOfRangeException(nameof(InitialView));
        ValidateRange(InitialDeformationVisualScalePercent, 0, 500, nameof(InitialDeformationVisualScalePercent));
        ValidateRange(InitialTextScalePercent, 50, 160, nameof(InitialTextScalePercent));
        ValidateRange(InitialAnimationSpeed, .25, 4, nameof(InitialAnimationSpeed));
        ArgumentNullException.ThrowIfNull(SvgOptions);
        SvgOptions.Validate();
    }

    private static void ValidateRange(double value, double minimum, double maximum, string name)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(name, value, $"The value must be finite and between {minimum} and {maximum}.");
    }
}

/// <summary>Available initial viewer result modes.</summary>
public enum ViewerResultView { Model, Displacements, N, V, M, All }
