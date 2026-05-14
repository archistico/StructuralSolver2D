namespace StructuralSolver2D.Core.Model.Enums;

/// <summary>
/// Defines the direction used to interpret a structural load value.
/// </summary>
public enum StructuralLoadDirection
{
    /// <summary>
    /// Global X direction.
    /// </summary>
    GlobalX = 0,

    /// <summary>
    /// Global Y direction.
    /// </summary>
    GlobalY = 1,

    /// <summary>
    /// Local member X direction.
    /// </summary>
    LocalX = 2,

    /// <summary>
    /// Local member Y direction.
    /// </summary>
    LocalY = 3,

    /// <summary>
    /// Out-of-plane moment around the global Z axis.
    /// </summary>
    MomentZ = 4
}
