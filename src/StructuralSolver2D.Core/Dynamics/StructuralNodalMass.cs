namespace StructuralSolver2D.Core.Dynamics;

/// <summary>
/// User-supplied ADDITIONAL nodal inertial mass; it is added to member self-mass,
/// never substituted for it. Both Ux and Uy use TranslationalMass.
/// Units: TranslationalMass [kN*s²/m], RotationalInertia [kN*s²*m].
/// A rotational inertia is meaningful only at a node attached to a Frame2D member.
/// </summary>
public sealed record StructuralNodalMass(
    string Id,
    string NodeId,
    double TranslationalMass,
    double? RotationalInertia = null);
