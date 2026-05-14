namespace StructuralSolver2D.Core.Model.Enums;

/// <summary>
/// Describes a common support configuration.
/// The actual restrained degrees of freedom are stored explicitly in <c>StructuralSupport</c>.
/// </summary>
public enum SupportType
{
    /// <summary>
    /// User-defined restraint configuration.
    /// </summary>
    Custom = 0,

    /// <summary>
    /// No restrained degree of freedom.
    /// </summary>
    Free = 1,

    /// <summary>
    /// Simple vertical support: Uy restrained, Ux and Rz free.
    /// </summary>
    SimpleSupport = 2,

    /// <summary>
    /// Pinned support: Ux and Uy restrained, Rz free.
    /// </summary>
    Hinge = 3,

    /// <summary>
    /// Roller support with one translational restraint.
    /// </summary>
    Roller = 4,

    /// <summary>
    /// Fixed support: Ux, Uy and Rz restrained.
    /// </summary>
    Fixed = 5
}
