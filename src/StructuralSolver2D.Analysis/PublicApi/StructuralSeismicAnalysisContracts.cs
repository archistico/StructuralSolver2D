using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>Uniform horizontal or vertical prescribed translation of all model supports.
/// All displacements and velocities in the M60 time response are relative to this ground motion.</summary>
public enum SeismicGroundDirection { GlobalX, GlobalY }

/// <summary>Ground acceleration in m/s², with time in seconds.</summary>
public sealed record SeismicAccelerationKnot(double TimeSeconds, double AccelerationMetersPerSecondSquared);

/// <summary>Elastic pseudo-spectral acceleration Sa [m/s²] at period T [s]. NOT a building-code spectrum.</summary>
public sealed record SeismicSpectrumPoint(double PeriodSeconds, double PseudoAccelerationMetersPerSecondSquared);

public sealed record StructuralSeismicTimeRequest(
    StructuralModel Model, StructuralDynamicsDefinition Dynamics, SeismicGroundDirection Direction,
    double TimeStepSeconds, int StepCount, StructuralHarmonicDamping Damping,
    IReadOnlyList<SeismicAccelerationKnot> GroundAcceleration);

/// <summary>SRSS remains the legacy default; CQC uses equal, explicitly specified modal damping.</summary>
public enum SeismicModalCombination { Srss, Cqc }

public sealed record StructuralSeismicSpectrumRequest(
    StructuralModel Model, StructuralDynamicsDefinition Dynamics, SeismicGroundDirection Direction,
    IReadOnlyList<SeismicSpectrumPoint> Spectrum, int ModeCount = 0,
    SeismicModalCombination Combination = SeismicModalCombination.Srss,
    double? ModalDampingRatio = null);

/// <summary>Participation factor Gamma: sqrt(kN*s²/m); effective mass = Gamma² [kN*s²/m].
/// The percentage denominator is rᵀ M r BEFORE applying restraints. Even a complete
/// admissible modal basis may have cumulative participating mass below 100%.</summary>
public sealed record StructuralSeismicModeParticipation(
    int ModeNumber, double FrequencyHertz, double PeriodSeconds,
    double ParticipationFactor, double EffectiveMass, double PercentageOfGroundMass,
    double CumulativePercentageOfGroundMass, double? PseudoAccelerationMetersPerSecondSquared = null,
    double? SpectralDisplacementMeters = null);

/// <summary>Unsigned SRSS or CQC envelopes in physical global node axes. Maxima of distinct DOFs
/// are not simultaneous and cannot be used as a signed deformation or load combination.</summary>
public sealed record StructuralSeismicNodeSpectrumEnvelope(string NodeId,
    double AbsUxMeters, double AbsUyMeters, double AbsRzRadians);

public sealed record StructuralSeismicSpectrumOutput(
    SeismicGroundDirection Direction, double GroundMass, double RetainedEffectiveMass,
    double RetainedMassPercentage, IReadOnlyList<StructuralSeismicModeParticipation> Modes,
    IReadOnlyList<StructuralSeismicNodeSpectrumEnvelope> NodeEnvelopes,
    IReadOnlyList<string> Warnings,
    SeismicModalCombination Combination = SeismicModalCombination.Srss,
    double? ModalDampingRatio = null);

/// <summary>Accelerations are absolute only for these two translational quantities.
/// Rotational acceleration remains relative, and ground uniform translation has zero rotation.</summary>
public sealed record StructuralSeismicAbsoluteNodeAcceleration(string NodeId,
    double AxMetersPerSecondSquared, double AyMetersPerSecondSquared);

public sealed record StructuralSeismicTimeStep(
    double TimeSeconds, double GroundAccelerationMetersPerSecondSquared,
    IReadOnlyList<StructuralSeismicAbsoluteNodeAcceleration> AbsoluteAccelerations);

public sealed record StructuralSeismicTimeOutput(
    SeismicGroundDirection Direction, StructuralTimeHistoryAnalysisOutput RelativeResponse,
    IReadOnlyList<StructuralSeismicTimeStep> GroundAndAbsoluteAccelerations,
    double GroundMass, IReadOnlyList<StructuralSeismicModeParticipation> Modes,
    IReadOnlyList<string> Warnings);
