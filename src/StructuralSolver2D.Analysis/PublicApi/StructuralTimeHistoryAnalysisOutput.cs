namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>Physical nodal motion, not mass-normalized mode amplitudes. SI-compatible kN-m-s units.</summary>
public sealed record StructuralTimeNodeResult(
    string NodeId,
    double Ux, double Uy, double Rz,
    double Vx, double Vy, double VrZ,
    double Ax, double Ay, double ArZ,
    double Fx, double Fy, double Mz);

/// <summary>Elastic + kinetic energy, accumulated input work and viscous dissipation [kN*m].
/// BalanceResidual = totalEnergy + dissipated - initialEnergy - externalWork.</summary>
public sealed record StructuralTimeEnergyResult(
    double Kinetic, double Elastic, double ExternalWork,
    double DissipatedEnergy, double BalanceResidual)
{
    public double Total => Kinetic + Elastic;
}

public sealed record StructuralTimeStepResult(
    int StepIndex, double TimeSeconds,
    IReadOnlyList<StructuralTimeNodeResult> Nodes,
    StructuralTimeEnergyResult Energy);

public sealed record StructuralTimeNodeEnvelope(string NodeId,
    double MaxAbsUx, double MaxAbsUy, double MaxAbsRz,
    double MaxAbsVx, double MaxAbsVy, double MaxAbsVrZ,
    double MaxAbsAx, double MaxAbsAy, double MaxAbsArZ);

public sealed record StructuralTimeHistoryAnalysisOutput(
    IReadOnlyList<StructuralTimeStepResult> Steps,
    IReadOnlyList<StructuralTimeNodeEnvelope> Envelopes,
    IReadOnlyList<double> NaturalFrequenciesHertz,
    IReadOnlyList<double> ModalDampingRatios,
    IReadOnlyList<string> Warnings)
{
    public string Integration => "Average-acceleration Newmark (beta=1/4, gamma=1/2) in a complete mass-normalized modal basis";
    public string LoadConvention => "Piecewise-linear load multipliers; no extrapolation; initial time t=0 s";
}
