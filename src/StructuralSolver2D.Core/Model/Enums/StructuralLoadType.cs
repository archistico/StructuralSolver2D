namespace StructuralSolver2D.Core.Model.Enums;

/// <summary>
/// Defines the supported structural load kinds.
/// </summary>
public enum StructuralLoadType
{
    /// <summary>
    /// Concentrated force applied to a structural node.
    /// Value unit: kilonewton [kN].
    /// </summary>
    NodalForce = 0,

    /// <summary>
    /// Concentrated moment applied to a structural node.
    /// Value unit: kilonewton meter [kNm].
    /// </summary>
    NodalMoment = 1,

    /// <summary>
    /// Uniformly distributed load applied along a structural member.
    /// Value unit: kilonewton per meter [kN/m].
    /// </summary>
    UniformDistributedLoad = 2,

    /// <summary>
    /// Concentrated force applied at a normalized position along a structural member.
    /// Value unit: kilonewton [kN].
    /// </summary>
    PointLoadOnMember = 3,

    /// <summary>
    /// Linearly varying distributed load applied along a structural member.
    /// <see cref="StructuralLoad.Value"/> is the start value, and <see cref="StructuralLoad.EndValue"/> is the end value.
    /// Value unit: kilonewton per meter [kN/m].
    /// </summary>
    LinearDistributedLoad = 4,

    /// <summary>
    /// Self-weight load derived from member section area and material unit weight.
    /// This is planned for a later analysis milestone.
    /// </summary>
    SelfWeight = 5
}
