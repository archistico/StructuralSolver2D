namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Free-vibration modes. These are unit-generalized, mass-normalized vectors, NOT
/// physical motion under applied loading. Normalization: phi^T M phi = 1.
/// </summary>
public sealed record StructuralModalAnalysisOutput(IReadOnlyList<StructuralModalModeResult> Modes)
{
    public int AvailableModeCount { get; init; }
    public double MaximumMassOrthogonalityError { get; init; }
    public double MaximumStiffnessOrthogonalityError { get; init; }
    public string Normalization { get; init; } = "Mass: phi^T M phi = 1; sign arbitrary";
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>One natural mode: angular frequency [rad/s], frequency [Hz], period [s].</summary>
public sealed record StructuralModalModeResult(
    int ModeNumber,
    double AngularFrequencyRadiansPerSecond,
    IReadOnlyList<StructuralModalNodeShape> Shape)
{
    public double FrequencyHertz => AngularFrequencyRadiansPerSecond / (2 * Math.PI);
    public double PeriodSeconds => 2 * Math.PI / AngularFrequencyRadiansPerSecond;
    public double EigenvalueRadiansSquaredPerSecondSquared { get; init; }
    public double RayleighQuotient { get; init; }
    public double RelativeResidual { get; init; }
}

/// <summary>Global nodal mass-normalized modal components (Ux, Uy, Rz).</summary>
public sealed record StructuralModalNodeShape(string NodeId, double Ux, double Uy, double Rz);
