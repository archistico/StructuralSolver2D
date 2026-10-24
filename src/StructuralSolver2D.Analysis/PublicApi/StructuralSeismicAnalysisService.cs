using StructuralSolver2D.Analysis.Dynamics.Seismic;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// M60 non-normative seismic analysis: uniform translational base excitation and
/// sampled user-supplied ELASTIC pseudo-acceleration response spectra.
/// Does not calculate stress, member forces, damage, seismic code compliance or seismic hazard.
/// </summary>
public sealed class StructuralSeismicAnalysisService
{
    public StructuralSeismicTimeOutput AnalyzeTime(StructuralSeismicTimeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.GroundAcceleration);
        if (request.GroundAcceleration.Count < 2 || request.GroundAcceleration.Count > 10000 ||
            request.GroundAcceleration.Any(k => k is null || !double.IsFinite(k.TimeSeconds) ||
                !double.IsFinite(k.AccelerationMetersPerSecondSquared)))
            throw new ArgumentException("M60_GROUND_HISTORY: Require 2..10000 finite acceleration knots [m/s²].");
        if (!double.IsFinite(request.TimeStepSeconds) || request.TimeStepSeconds <= 0 ||
            request.StepCount < 1 || request.StepCount > 2000 ||
            !double.IsFinite(request.TimeStepSeconds * request.StepCount))
            throw new ArgumentException("M60_TIME_GRID: Require finite positive dt and 1..2000 steps.");
        var knots = request.GroundAcceleration;
        if (knots[0].TimeSeconds != 0 ||
            knots[^1].TimeSeconds < request.StepCount * request.TimeStepSeconds)
            throw new ArgumentException("M60_GROUND_COVERAGE: Ground acceleration must start at zero and cover the last step.");
        for (int i = 1; i < knots.Count; i++)
            if (knots[i].TimeSeconds <= knots[i-1].TimeSeconds)
                throw new ArgumentException("M60_GROUND_ORDER: Times must be strictly increasing.");
        var participation = SeismicParticipation.Calculate(request.Model, request.Dynamics, request.Direction);
        // Every nodal DOF (including moments from consistent-mass coupling) receives -M*r*a_g(t).
        var pattern = new StructuralTimeLoadPattern(
            request.Model.Nodes.Select((node, i) => new StructuralHarmonicNodalForce(node.Id,
                Fx: -participation.EffectiveInertialForcesPerAcceleration[i * 3],
                Fy: -participation.EffectiveInertialForcesPerAcceleration[i * 3 + 1],
                Mz: -participation.EffectiveInertialForcesPerAcceleration[i * 3 + 2])).ToArray(),
            knots.Select(k => new StructuralTimeLoadKnot(k.TimeSeconds,
                k.AccelerationMetersPerSecondSquared)).ToArray());
        // Relative coordinates and relative energy are calculated by validated M59 Newmark.
        var relative = new StructuralTimeHistoryAnalysisService().Analyze(new(
            request.Model, request.Dynamics, request.TimeStepSeconds, request.StepCount,
            request.Damping, new[] { pattern }));
        // The shared M59 engine uses a generic warning that its direct nodal-load API
        // does not model earthquakes. In this M60 wrapper the input IS verified uniform
        // ground excitation; remove that obsolete wording without changing M59 itself.
        relative = relative with {
            Warnings = relative.Warnings.Where(w =>
                !w.StartsWith("M59 linear first-order elastic motion only;", StringComparison.Ordinal))
                .Append("M60_BASE_MOTION: This response was driven by uniform imposed base acceleration with equivalent inertial loads.")
                .ToArray()
        };
        var samples = new List<StructuralSeismicTimeStep>(relative.Steps.Count);
        foreach (StructuralTimeStepResult step in relative.Steps)
        {
            double ag = Interpolate(knots, step.TimeSeconds);
            var absolute = step.Nodes.Select(node => new StructuralSeismicAbsoluteNodeAcceleration(
                node.NodeId, node.Ax + (request.Direction == SeismicGroundDirection.GlobalX ? ag : 0),
                node.Ay + (request.Direction == SeismicGroundDirection.GlobalY ? ag : 0))).ToArray();
            samples.Add(new StructuralSeismicTimeStep(step.TimeSeconds, ag, absolute));
        }
        var warnings = participation.Warnings.Concat(new[] {
            "M60_RELATIVE_COORDINATES: Displacements, velocities and accelerations in RelativeResponse are relative to moving ground; absolute translational accelerations add a_g.",
            "Ground input is acceleration [m/s²]. Equivalent nodal inertia forces are -M*r*a_g in kN (consistent mass).",
            "M59 energy/work diagnostics are for relative response under equivalent inertial forces, NOT absolute earthquake input energy; do not reinterpret them as physical total energy.",
            "Explicit accelerogram interpolation and Newmark time-step convergence must be checked; high-frequency accuracy is not guaranteed."
        }).ToArray();
        return new StructuralSeismicTimeOutput(request.Direction, relative, samples,
            participation.GroundMass, participation.Modes, warnings);
    }

    public StructuralSeismicSpectrumOutput AnalyzeSpectrum(StructuralSeismicSpectrumRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Spectrum);
        if (!Enum.IsDefined(request.Combination))
            throw new ArgumentException("M70_COMBINATION: Select Srss or Cqc.");
        if (request.Combination == SeismicModalCombination.Cqc)
            EqualDampingCqc.Validate(request.ModalDampingRatio);
        else if (request.ModalDampingRatio is not null)
            throw new ArgumentException("M70_SRSS_DAMPING: Damping ratio applies only to CQC; omit it for SRSS.");
        if (request.Spectrum.Count < 2 || request.Spectrum.Count > 10000 ||
            request.Spectrum.Any(k => k is null || !double.IsFinite(k.PeriodSeconds) ||
                !double.IsFinite(k.PseudoAccelerationMetersPerSecondSquared) ||
                k.PeriodSeconds < 0 || k.PseudoAccelerationMetersPerSecondSquared < 0))
            throw new ArgumentException("M60_SPECTRUM_POINTS: Require 2..10000 finite nonnegative (T [s], Sa [m/s²]) points.");
        for (int i = 1; i < request.Spectrum.Count; i++)
            if (request.Spectrum[i].PeriodSeconds <= request.Spectrum[i-1].PeriodSeconds)
                throw new ArgumentException("M60_SPECTRUM_ORDER: Periods must increase strictly.");
        var participation = SeismicParticipation.Calculate(request.Model, request.Dynamics,
            request.Direction, request.ModeCount);
        int nNodes = request.Model.Nodes.Count;
        bool cqc = request.Combination == SeismicModalCombination.Cqc;
        var sumsSquared = new double[nNodes, 3];
        var signedContributions = cqc
            ? new double[nNodes * 3, participation.Spectrum.Modes.Count] : null;
        var modes = new List<StructuralSeismicModeParticipation>();
        for (int j = 0; j < participation.Spectrum.Modes.Count; j++)
        {
            var mode = participation.Spectrum.Modes[j];
            double sa = Interpolate(request.Spectrum, mode.PeriodSeconds);
            double sd = sa / (mode.AngularFrequencyRadiansPerSecond * mode.AngularFrequencyRadiansPerSecond);
            if (!double.IsFinite(sd))
                throw new ArgumentException("M60_SPECTRAL_DISPLACEMENT: Spectrum creates nonfinite Sd=Sa/omega².");
            modes.Add(participation.Modes[j] with {
                PseudoAccelerationMetersPerSecondSquared = sa,
                SpectralDisplacementMeters = sd });
            double modalAmplitude = participation.Modes[j].ParticipationFactor * sd;
            for (int i = 0; i < nNodes; i++)
                for (int component = 0; component < 3; component++)
                {
                    double value = mode.GlobalMassNormalizedShape[3 * i + component] * modalAmplitude;
                    if (cqc) signedContributions![3 * i + component, j] = value;
                    else sumsSquared[i, component] += value * value;
                    if (!double.IsFinite(value) || !double.IsFinite(sumsSquared[i, component]))
                        throw new ArgumentException(cqc
                            ? "M70_MODAL_AMPLITUDE_RANGE: Nonfinite signed modal contribution."
                            : "M60_SRSS_RANGE: Spectrum causes nonfinite SRSS square sum.");
                }
        }
        if (cqc)
        {
            double[] frequencies = participation.Spectrum.Modes.Select(m => m.FrequencyHertz).ToArray();
            for (int node = 0; node < nNodes; node++)
                for (int component = 0; component < 3; component++)
                {
                    int row = node * 3 + component;
                    double[] amplitudes = new double[frequencies.Length];
                    for (int j = 0; j < frequencies.Length; j++)
                        amplitudes[j] = signedContributions![row, j];
                    sumsSquared[node, component] = EqualDampingCqc.Combine(
                        amplitudes, frequencies, request.ModalDampingRatio!.Value);
                }
        }
        var nodes = request.Model.Nodes.Select((node, i) => new StructuralSeismicNodeSpectrumEnvelope(
            node.Id,
            cqc ? sumsSquared[i, 0] : Math.Sqrt(sumsSquared[i, 0]),
            cqc ? sumsSquared[i, 1] : Math.Sqrt(sumsSquared[i, 1]),
            cqc ? sumsSquared[i, 2] : Math.Sqrt(sumsSquared[i, 2]))).ToArray();
        var warnings = participation.Warnings.Concat(new[] {
            "M60_SPECTRUM_CONVENTION: Input Sa is ELASTIC PSEUDO-acceleration [m/s²], not true absolute acceleration; Sd=Sa/omega².",
            cqc
                ? $"M70_CQC: Unsigned CQC modal displacement/rotation envelopes using equal modal damping ratio {request.ModalDampingRatio!.Value:G6}. No directional combination, forces or simultaneous signed state."
                : "M60_SRSS: Unsigned modal SRSS displacement/rotation envelopes assume sufficiently separated modes; no CQC, directional combination, forces or simultaneous signed state.",
            cqc
                ? "User spectrum is piecewise-linearly interpolated in PERIOD with NO extrapolation. Its ordinates must correspond to the physical modal damping; CQC correlation does NOT modify or correct the supplied spectrum."
                : "User spectrum is piecewise-linearly interpolated in PERIOD with NO extrapolation. Damping must already be represented by the supplied spectrum; no implicit correction."
        }).ToList();
        for (int i = 1; i < participation.Spectrum.Modes.Count && !cqc; i++)
        {
            double f0 = participation.Spectrum.Modes[i-1].FrequencyHertz;
            double f1 = participation.Spectrum.Modes[i].FrequencyHertz;
            if ((f1 - f0) / f1 < .1 &&
                (participation.Modes[i-1].EffectiveMass > 1e-8 * participation.GroundMass ||
                 participation.Modes[i].EffectiveMass > 1e-8 * participation.GroundMass))
            {
                warnings.Add("M60_CLOSE_MODES: Participating frequencies within 10%; SRSS may be inaccurate. CQC is intentionally deferred.");
                break;
            }
        }
        double retained = modes.Sum(m => m.EffectiveMass);
        return new StructuralSeismicSpectrumOutput(request.Direction, participation.GroundMass,
            retained, 100 * retained / participation.GroundMass, modes, nodes, warnings,
            request.Combination, request.ModalDampingRatio);
    }

    private static double Interpolate(IReadOnlyList<SeismicAccelerationKnot> knots, double t)
    {
        if (t <= knots[0].TimeSeconds) return knots[0].AccelerationMetersPerSecondSquared;
        if (t >= knots[^1].TimeSeconds) return knots[^1].AccelerationMetersPerSecondSquared;
        int lower = 0, upper = knots.Count - 1;
        while (upper - lower > 1) {
            int mid = (lower + upper) / 2;
            if (knots[mid].TimeSeconds <= t) lower = mid; else upper = mid;
        }
        double w = (t - knots[lower].TimeSeconds) /
            (knots[upper].TimeSeconds - knots[lower].TimeSeconds);
        return (1-w) * knots[lower].AccelerationMetersPerSecondSquared +
            w * knots[upper].AccelerationMetersPerSecondSquared;
    }

    private static double Interpolate(IReadOnlyList<SeismicSpectrumPoint> knots, double t)
    {
        if (t < knots[0].PeriodSeconds - 1e-12 || t > knots[^1].PeriodSeconds + 1e-12)
            throw new ArgumentException($"M60_SPECTRUM_COVERAGE: Modal period {t:G8} s is outside supplied range [{knots[0].PeriodSeconds:G8}, {knots[^1].PeriodSeconds:G8}] s.");
        if (t <= knots[0].PeriodSeconds) return knots[0].PseudoAccelerationMetersPerSecondSquared;
        if (t >= knots[^1].PeriodSeconds) return knots[^1].PseudoAccelerationMetersPerSecondSquared;
        int lo = 0, hi = knots.Count - 1;
        while (hi - lo > 1) {
            int mid = (lo + hi) / 2;
            if (knots[mid].PeriodSeconds <= t) lo = mid; else hi = mid;
        }
        double w = (t - knots[lo].PeriodSeconds) /
            (knots[hi].PeriodSeconds - knots[lo].PeriodSeconds);
        return (1 - w) * knots[lo].PseudoAccelerationMetersPerSecondSquared +
            w * knots[hi].PseudoAccelerationMetersPerSecondSquared;
    }
}
