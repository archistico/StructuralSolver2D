using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Core.Model.Generators;

/// <summary>
/// Defines common defaults used by parametric structural model generators.
/// The generated values are intentionally generic and are meant for examples,
/// validation models and early design studies, not for normative design checks.
/// </summary>
public sealed class ParametricModelGenerationOptions
{
    /// <summary>
    /// Gets or sets the material assigned to generated members.
    /// </summary>
    public StructuralMaterial Material { get; set; } = new("MAT1", "Generic steel", 210_000_000.0, 78.5);

    /// <summary>
    /// Gets or sets the section assigned to generated members.
    /// </summary>
    public StructuralSection Section { get; set; } = new("SEC1", "Generic section", 0.01, 8.333333333333334E-6, 0.10, 0.10);

    /// <summary>
    /// Gets or sets the default load case identifier added to generated models.
    /// </summary>
    public string LoadCaseId { get; set; } = "LC1";

    /// <summary>
    /// Gets or sets the default load case name added to generated models.
    /// </summary>
    public string LoadCaseName { get; set; } = "Default load case";

    /// <summary>
    /// Gets or sets whether a default load case should be added to the generated model.
    /// </summary>
    public bool AddDefaultLoadCase { get; set; } = true;

    /// <summary>
    /// Gets or sets the member type used by generated beam and frame models.
    /// </summary>
    public MemberType FrameMemberType { get; set; } = MemberType.Frame2D;

    /// <summary>
    /// Gets or sets the member type used by generated truss models.
    /// </summary>
    public MemberType TrussMemberType { get; set; } = MemberType.Truss2D;
}
