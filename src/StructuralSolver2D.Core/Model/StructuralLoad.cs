using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Core.Model;

/// <summary>
/// Represents a structural load applied to a node, member or the whole model.
/// Internal units depend on <see cref="Type"/>:
/// nodal force and point member load use kilonewton [kN],
/// nodal moment uses kilonewton meter [kNm],
/// uniform and linearly varying distributed loads use kilonewton per meter [kN/m].
/// </summary>
/// <param name="Id">Unique load identifier.</param>
/// <param name="LoadCaseId">Identifier of the load case that owns this load.</param>
/// <param name="Type">Load type.</param>
/// <param name="TargetType">Type of entity targeted by the load.</param>
/// <param name="TargetId">Identifier of the targeted node/member. Empty for whole-model loads.</param>
/// <param name="Direction">Load direction.</param>
/// <param name="Value">Load value in internal units.</param>
/// <param name="Position">Optional normalized member position, from 0.0 to 1.0.</param>
/// <param name="Label">Optional user-facing label.</param>
/// <param name="EndValue">Optional end value for linearly varying distributed loads.</param>
public sealed record StructuralLoad(
    string Id,
    string LoadCaseId,
    StructuralLoadType Type,
    StructuralLoadTargetType TargetType,
    string TargetId,
    StructuralLoadDirection Direction,
    double Value,
    double? Position = null,
    string? Label = null,
    double? EndValue = null)
{
    /// <summary>
    /// Creates a nodal force in the global X or Y direction.
    /// </summary>
    public static StructuralLoad NodalForce(
        string id,
        string loadCaseId,
        string nodeId,
        StructuralLoadDirection direction,
        double value,
        string? label = null) =>
        new(id, loadCaseId, StructuralLoadType.NodalForce, StructuralLoadTargetType.Node, nodeId, direction, value, null, label);

    /// <summary>
    /// Creates a nodal moment around the global Z axis.
    /// </summary>
    public static StructuralLoad NodalMoment(
        string id,
        string loadCaseId,
        string nodeId,
        double value,
        string? label = null) =>
        new(id, loadCaseId, StructuralLoadType.NodalMoment, StructuralLoadTargetType.Node, nodeId, StructuralLoadDirection.MomentZ, value, null, label);

    /// <summary>
    /// Creates a uniform distributed load along a member.
    /// </summary>
    public static StructuralLoad UniformDistributedLoad(
        string id,
        string loadCaseId,
        string memberId,
        StructuralLoadDirection direction,
        double value,
        string? label = null) =>
        new(id, loadCaseId, StructuralLoadType.UniformDistributedLoad, StructuralLoadTargetType.Member, memberId, direction, value, null, label);

    /// <summary>
    /// Creates a concentrated member load at a normalized position from 0.0 to 1.0.
    /// </summary>
    public static StructuralLoad PointLoadOnMember(
        string id,
        string loadCaseId,
        string memberId,
        StructuralLoadDirection direction,
        double value,
        double position,
        string? label = null) =>
        new(id, loadCaseId, StructuralLoadType.PointLoadOnMember, StructuralLoadTargetType.Member, memberId, direction, value, position, label);

    /// <summary>
    /// Creates a linearly varying distributed load along a member.
    /// Start and end values are expressed in kN/m in the selected direction.
    /// </summary>
    public static StructuralLoad LinearDistributedLoad(
        string id,
        string loadCaseId,
        string memberId,
        StructuralLoadDirection direction,
        double startValue,
        double endValue,
        string? label = null) =>
        new(id, loadCaseId, StructuralLoadType.LinearDistributedLoad, StructuralLoadTargetType.Member, memberId, direction, startValue, null, label, endValue);
}
