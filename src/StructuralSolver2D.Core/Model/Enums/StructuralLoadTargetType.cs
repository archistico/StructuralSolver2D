namespace StructuralSolver2D.Core.Model.Enums;

/// <summary>
/// Defines the structural entity type targeted by a load.
/// </summary>
public enum StructuralLoadTargetType
{
    /// <summary>
    /// Load applied to a structural node.
    /// </summary>
    Node = 0,

    /// <summary>
    /// Load applied to a structural member.
    /// </summary>
    Member = 1,

    /// <summary>
    /// Load applied to the whole model, for example self-weight.
    /// </summary>
    Model = 2
}
