using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>Load multiplier at time [s]. Linear interpolation between knots; no extrapolation.</summary>
public sealed record StructuralTimeLoadKnot(double TimeSeconds, double Factor);

/// <summary>One fixed global nodal load pattern [kN, kN, kN*m], scaled by a piecewise-linear history.</summary>
public sealed record StructuralTimeLoadPattern(
    IReadOnlyList<StructuralHarmonicNodalForce> Forces,
    IReadOnlyList<StructuralTimeLoadKnot> History);

/// <summary>Physical prescribed initial states, in m, rad, m/s, and rad/s.</summary>
public sealed record StructuralTimeInitialNodeState(
    string NodeId, double Ux = 0, double Uy = 0, double Rz = 0,
    double Vx = 0, double Vy = 0, double VrZ = 0);

/// <summary>Opt-in linear time history; only admitted M54 consistent-mass, restrained models.
/// Grid times = stepIndex * TimeStepSeconds, from 0 through StepCount * dt inclusive.
/// Load history must cover both ends of this interval. Average acceleration Newmark beta=1/4 gamma=1/2.</summary>
public sealed record StructuralTimeHistoryAnalysisRequest(
    StructuralModel Model,
    StructuralDynamicsDefinition Dynamics,
    double TimeStepSeconds,
    int StepCount,
    StructuralHarmonicDamping Damping,
    IReadOnlyList<StructuralTimeLoadPattern> Loads,
    IReadOnlyList<StructuralTimeInitialNodeState>? InitialStates = null);
