using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Core.Model;

/// <summary>
/// Represents nodal restraints for a 2D frame model.
/// Degrees of freedom: Ux, Uy, Rz.
/// </summary>
/// <param name="Id">Unique support identifier.</param>
/// <param name="NodeId">Identifier of the restrained node.</param>
/// <param name="RestrainedUx">True if horizontal translation Ux is restrained.</param>
/// <param name="RestrainedUy">True if vertical translation Uy is restrained.</param>
/// <param name="RestrainedRz">True if in-plane rotation Rz is restrained.</param>
/// <param name="Type">Common support type, if applicable.</param>
/// <param name="Label">Optional user-facing label.</param>
/// <param name="OrientationDegrees">Graphical support orientation in degrees, measured counterclockwise in model coordinates.</param>
public sealed record StructuralSupport(
    string Id,
    string NodeId,
    bool RestrainedUx,
    bool RestrainedUy,
    bool RestrainedRz,
    SupportType Type = SupportType.Custom,
    string? Label = null,
    double OrientationDegrees = 0.0)
{
    /// <summary>
    /// Creates a pinned support: Ux and Uy restrained, Rz free.
    /// </summary>
    public static StructuralSupport Hinge(string id, string nodeId, string? label = null, double orientationDegrees = 0.0) =>
        new(id, nodeId, true, true, false, SupportType.Hinge, label, orientationDegrees);

    /// <summary>
    /// Creates a vertical roller/simple support: Uy restrained, Ux and Rz free.
    /// </summary>
    public static StructuralSupport SimpleSupport(string id, string nodeId, string? label = null, double orientationDegrees = 0.0) =>
        new(id, nodeId, false, true, false, SupportType.SimpleSupport, label, orientationDegrees);

    /// <summary>
    /// Creates a fixed support: Ux, Uy and Rz restrained.
    /// </summary>
    public static StructuralSupport Fixed(string id, string nodeId, string? label = null, double orientationDegrees = 0.0) =>
        new(id, nodeId, true, true, true, SupportType.Fixed, label, orientationDegrees);
}
