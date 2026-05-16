namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Stores the reaction components associated with a support.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Fx"/> and <see cref="Fy"/> are always expressed as global force components,
/// independently from <c>StructuralSupport.OrientationDegrees</c>. Internal force units are kilonewton [kN].
/// </para>
/// <para>
/// For inclined supports, a single restrained local direction may generate both global components.
/// For example, an inclined roller can legitimately report both a non-zero <see cref="Fx"/> and
/// a non-zero <see cref="Fy"/> even though it has only one translational restraint in its local support system.
/// </para>
/// <para>
/// <see cref="Mz"/> is the global support moment component. Internal moment units are kilonewton meter [kNm].
/// </para>
/// </remarks>
public sealed record SupportReactionResult(
    string SupportId,
    string NodeId,
    double Fx,
    double Fy,
    double Mz);
