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
public sealed record StructuralSupport(
    string Id,
    string NodeId,
    bool RestrainedUx,
    bool RestrainedUy,
    bool RestrainedRz,
    SupportType Type = SupportType.Custom,
    string? Label = null)
{
    /// <summary>
    /// Creates a pinned support: Ux and Uy restrained, Rz free.
    /// </summary>
    public static StructuralSupport Hinge(string id, string nodeId, string? label = null) =>
        new(id, nodeId, true, true, false, SupportType.Hinge, label);

    /// <summary>
    /// Creates a vertical roller/simple support: Uy restrained, Ux and Rz free.
    /// </summary>
    public static StructuralSupport SimpleSupport(string id, string nodeId, string? label = null) =>
        new(id, nodeId, false, true, false, SupportType.SimpleSupport, label);

    /// <summary>
    /// Creates a fixed support: Ux, Uy and Rz restrained.
    /// </summary>
    public static StructuralSupport Fixed(string id, string nodeId, string? label = null) =>
        new(id, nodeId, true, true, true, SupportType.Fixed, label);
}
