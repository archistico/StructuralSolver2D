using System.Numerics;
using StructuralSolver2D.Analysis.Dynamics.Harmonic;
using StructuralSolver2D.Analysis.Dynamics.Modal;
using StructuralSolver2D.Analysis.Dynamics.Reduction;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>Opt-in M58 steady-state sinusoidal FEM response by COMPLETE modal superposition.
/// No mode truncation, transient integration, static solver modification or seismic inference.</summary>
public sealed class StructuralHarmonicAnalysisService
{
    public StructuralHarmonicAnalysisOutput Analyze(StructuralHarmonicAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Model);
        ArgumentNullException.ThrowIfNull(request.Dynamics);
        ArgumentNullException.ThrowIfNull(request.Forces);
        ArgumentNullException.ThrowIfNull(request.FrequenciesHertz);
        ArgumentNullException.ThrowIfNull(request.Damping);
        StructuralModelValidationResult validation = new StructuralHarmonicInputValidator().Validate(request.Model, request.Dynamics);
        if (!validation.IsValid)
            throw new StructuralAnalysisException("Harmonic input validation failed.", validation.Issues);
        if (request.Forces.Count == 0 || request.FrequenciesHertz.Count == 0 || request.FrequenciesHertz.Count > 1001)
            throw new ArgumentException("M58_SWEEP_COUNT: Require at least one nodal force and 1..1001 sample frequencies.");
        if (request.FrequenciesHertz.Any(f => !double.IsFinite(f) || f < 0))
            throw new ArgumentException("M58_FREQUENCY_RANGE: Frequencies must be finite and nonnegative [Hz].");
        for (int i = 1; i < request.FrequenciesHertz.Count; i++)
            if (request.FrequenciesHertz[i] <= request.FrequenciesHertz[i - 1])
                throw new ArgumentException("M58_FREQUENCY_ORDER: Frequencies must be strictly increasing for an unambiguous sweep.");

        int nNodes = request.Model.Nodes.Count;
        var force = new double[checked(nNodes * 3)];
        var nodeIds = request.Model.Nodes.Select((node, i) => (node.Id, Index: i))
            .ToDictionary(p => p.Id, p => p.Index, StringComparer.Ordinal);
        foreach (var nodal in request.Forces)
        {
            if (nodal is null || string.IsNullOrWhiteSpace(nodal.NodeId) ||
                !nodeIds.TryGetValue(nodal.NodeId, out int index))
                throw new ArgumentException("M58_UNKNOWN_NODE: Every harmonic force must target an existing node.");
            if (!double.IsFinite(nodal.Fx) || !double.IsFinite(nodal.Fy) || !double.IsFinite(nodal.Mz))
                throw new ArgumentException("M58_NONFINITE_LOAD: Forces and moments must be finite.");
            force[3 * index] += nodal.Fx;
            force[3 * index + 1] += nodal.Fy;
            force[3 * index + 2] += nodal.Mz;
        }
        if (force.Any(value => !double.IsFinite(value)) || !force.Any(value => value != 0))
            throw new ArgumentException("M58_EMPTY_LOAD: Resulting force vector must be finite and nonzero.");
        DynamicReductionResult reduced;
        try { reduced = DynamicBoundaryConditionReducer.AssembleAndReduce(request.Model, request.Dynamics); }
        catch (NotSupportedException ex) { throw new StructuralAnalysisException($"M58_UNSUPPORTED: {ex.Message}"); }
        if (!reduced.ReadyForModalSolver)
            throw new StructuralAnalysisException($"M58_REDUCTION: {reduced.DiagnosticCode}: {reduced.Message}");
        int n = reduced.Stiffness.GetLength(0);
        // Complete spectrum: any truncation would bias amplitudes and phases.
        ExperimentalModalSpectrum spectrum = SymmetricGeneralizedModalSolver.Solve(reduced, n);
        var qforce = new double[n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < force.Length; i++) qforce[j] += reduced.Basis[i, j] * force[i];
        double[] ratios = spectrum.Modes.Select(mode => request.Damping.Ratio(mode.AngularFrequencyRadiansPerSecond)).ToArray();
        var generalizedForces = spectrum.Modes.Select(mode =>
            mode.ReducedMassNormalizedShape.Zip(qforce, (a, b) => a * b).Sum()).ToArray();
        double forceProjectionScale = generalizedForces.Select(Math.Abs).DefaultIfEmpty(0).Max();
        var samples = new List<StructuralHarmonicFrequencyResult>(request.FrequenciesHertz.Count);
        foreach (double frequency in request.FrequenciesHertz)
        {
            double excitationOmega = 2 * Math.PI * frequency;
            if (!double.IsFinite(excitationOmega))
                throw new ArgumentException("M58_FREQUENCY_RANGE: Angular frequency exceeds representable range.");
            var global = new Complex[force.Length];
            for (int j = 0; j < n; j++)
            {
                if (Math.Abs(generalizedForces[j]) <= 1e-13 * forceProjectionScale) continue;
                ModalEigenpair mode = spectrum.Modes[j];
                double omega = mode.AngularFrequencyRadiansPerSecond;
                double damping = 2 * ratios[j] * omega; // modal generalized mass = 1
                Complex modalAmplitude = HarmonicFrequencyResponseSolver.SolveSdof(1, damping,
                    mode.EigenvalueRadiansSquaredPerSecondSquared, excitationOmega, generalizedForces[j]);
                for (int i = 0; i < global.Length; i++)
                    global[i] += modalAmplitude * mode.GlobalMassNormalizedShape[i];
            }
            var nodes = new StructuralHarmonicNodeResult[nNodes];
            for (int i = 0; i < nNodes; i++)
                nodes[i] = new StructuralHarmonicNodeResult(request.Model.Nodes[i].Id,
                    Part(global[3 * i]), Part(global[3 * i + 1]), Part(global[3 * i + 2]));
            samples.Add(new StructuralHarmonicFrequencyResult(frequency, nodes));
        }
        var warnings = new List<string> {
            "Linear steady-state response only; transient start-up and actual safety assessment are not included.",
            "All forces are in phase, use the supplied sinusoidal time origin; displacements are physical phasor amplitudes, unlike mass-normalized mode shapes.",
            "Only the fully supported M53/M54 consistent-mass models are accepted; released ends, free-free and singular-mass systems are deferred." };
        if (spectrum.Modes.Select((mode, index) => (mode, index)).Any(entry =>
            Math.Abs(generalizedForces[entry.index]) > 1e-13 * forceProjectionScale &&
            request.FrequenciesHertz.Any(freq =>
                Math.Abs(freq / entry.mode.FrequencyHertz - 1.0) <= 0.05)))
            warnings.Add("One or more sampled frequencies lie within 5% of an excited natural frequency. A local amplification peak may be sharp; refine the sweep and inspect phase/damping. This is not a safety rating.");
        if (forceProjectionScale == 0) warnings.Add("Harmonic loading has no projection onto admissible DOFs; displacements are zero, but restrained reactions may be nonzero.");
        if (ratios.All(value => value == 0)) warnings.Add("Zero damping: undamped resonant excitation has no finite steady-state response.");
        return new StructuralHarmonicAnalysisOutput(samples,
            spectrum.Modes.Select(mode => mode.FrequencyHertz).ToArray(), ratios, warnings);
    }

    private static StructuralHarmonicComponent Part(Complex complex)
    {
        if (!double.IsFinite(complex.Real) || !double.IsFinite(complex.Imaginary))
            throw new InvalidOperationException("M58_RESPONSE_RANGE: Non-finite global response.");
        return new StructuralHarmonicComponent(complex.Real, complex.Imaginary);
    }
}

internal sealed class StructuralHarmonicInputValidator
{
    internal StructuralModelValidationResult Validate(StructuralSolver2D.Core.Model.StructuralModel model,
        StructuralSolver2D.Core.Dynamics.StructuralDynamicsDefinition dynamics) =>
        new StructuralSolver2D.Core.Dynamics.StructuralDynamicsValidator().Validate(model, dynamics);
}
