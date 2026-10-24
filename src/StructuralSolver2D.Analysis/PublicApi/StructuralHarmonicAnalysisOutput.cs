namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>Complex phasor components [m] for Ux/Uy and [rad] for Rz.
/// A real sinusoidal time history is u(t)=Re(U)*sin(Omega*t)+Im(U)*cos(Omega*t).
/// Phase is Arg(U), negative values represent lag behind the applied sine force.</summary>
public sealed record StructuralHarmonicComponent(double Real, double Imaginary)
{
    public double Amplitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);
    public double PhaseRadians => Math.Atan2(Imaginary, Real);
    public double PhaseDegrees => PhaseRadians * 180 / Math.PI;
}

public sealed record StructuralHarmonicNodeResult(
    string NodeId, StructuralHarmonicComponent Ux, StructuralHarmonicComponent Uy,
    StructuralHarmonicComponent Rz);

public sealed record StructuralHarmonicFrequencyResult(double FrequencyHertz,
    IReadOnlyList<StructuralHarmonicNodeResult> Nodes)
{
    public double AngularFrequencyRadiansPerSecond => 2 * Math.PI * FrequencyHertz;
}

public sealed record StructuralHarmonicAnalysisOutput(IReadOnlyList<StructuralHarmonicFrequencyResult> Samples,
    IReadOnlyList<double> NaturalFrequenciesHertz,
    IReadOnlyList<double> ModalDampingRatios,
    IReadOnlyList<string> Warnings)
{
    public string ForceConvention => "F(t)=F0 sin(Omega*t), all nodal inputs in phase";
    public string ResponseConvention => "u(t)=Re(U) sin(Omega*t)+Im(U) cos(Omega*t); phase=Arg(U)";
}
