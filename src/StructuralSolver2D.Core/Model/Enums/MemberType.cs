namespace StructuralSolver2D.Core.Model.Enums;

/// <summary>
/// Defines the structural behavior assigned to a one-dimensional member.
/// </summary>
public enum MemberType
{
    /// <summary>
    /// Two-dimensional frame element with axial, shear and bending behavior.
    /// Each node has Ux, Uy and Rz degrees of freedom.
    /// </summary>
    Frame2D = 0,

    /// <summary>
    /// Two-dimensional truss element with axial behavior only.
    /// This type is planned for a later milestone.
    /// </summary>
    Truss2D = 1
}
