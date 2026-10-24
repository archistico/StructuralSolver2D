using StructuralSolver2D.Analysis.Dynamics.Modal;
using StructuralSolver2D.Analysis.Dynamics.Reduction;
using StructuralSolver2D.Analysis.Dynamics.Transient;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Linear dynamic response in a COMPLETE M55 modal basis using average-acceleration Newmark.
/// No truncated modes, invented mass, imposed-ground motion, geometric nonlinearities,
/// start-up approximations, or use of the static load-case pipeline.
/// </summary>
public sealed class StructuralTimeHistoryAnalysisService
{
    private const int MaxSteps = 2000;

    public StructuralTimeHistoryAnalysisOutput Analyze(StructuralTimeHistoryAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Model);
        ArgumentNullException.ThrowIfNull(request.Dynamics);
        ArgumentNullException.ThrowIfNull(request.Damping);
        ArgumentNullException.ThrowIfNull(request.Loads);
        if (request.StepCount < 1 || request.StepCount > MaxSteps ||
            !double.IsFinite(request.TimeStepSeconds) || request.TimeStepSeconds <= 0 ||
            !double.IsFinite(request.TimeStepSeconds * request.StepCount))
            throw new ArgumentException("M59_TIME_GRID: Require 1..2000 steps and finite positive dt [s].");
        double end = request.TimeStepSeconds * request.StepCount;
        if (request.Loads.Count > 64)
            throw new ArgumentException("M59_LOAD_COUNT: At most 64 load patterns are supported.");
        StructuralModelValidationResult validity = new StructuralHarmonicInputValidator()
            .Validate(request.Model, request.Dynamics);
        if (!validity.IsValid)
            throw new StructuralAnalysisException("Transient dynamic input validation failed.", validity.Issues);
        DynamicReductionResult reduced;
        try { reduced = DynamicBoundaryConditionReducer.AssembleAndReduce(request.Model, request.Dynamics); }
        catch (NotSupportedException ex) { throw new StructuralAnalysisException($"M59_UNSUPPORTED: {ex.Message}"); }
        if (!reduced.ReadyForModalSolver)
            throw new StructuralAnalysisException($"M59_REDUCTION: {reduced.DiagnosticCode}: {reduced.Message}");

        int n = reduced.Stiffness.GetLength(0);
        int ndof = checked(request.Model.Nodes.Count * 3);
        ExperimentalModalSpectrum spectrum = SymmetricGeneralizedModalSolver.Solve(reduced, n);
        // This build supports complete-spectrum physical response only.
        if (spectrum.Modes.Count != n)
            throw new InvalidOperationException("M59_TRUNCATED_BASIS: Complete admissible modes are required.");
        var nodeIds = request.Model.Nodes.Select((node, i) => (node.Id, Index: i))
            .ToDictionary(x => x.Id, x => x.Index, StringComparer.Ordinal);

        var patterns = new List<PreparedPattern>();
        foreach (StructuralTimeLoadPattern input in request.Loads)
        {
            if (input is null || input.Forces is null || input.History is null ||
                input.History.Count < 2 || input.History.Count > 10000)
                throw new ArgumentException("M59_HISTORY: Each pattern needs 2..10000 time knots and a force list.");
            if (input.History.Any(k => k is null || !double.IsFinite(k.TimeSeconds) ||
                !double.IsFinite(k.Factor)) || input.History[0].TimeSeconds != 0 ||
                input.History[^1].TimeSeconds < end)
                throw new ArgumentException("M59_HISTORY_COVERAGE: History must begin at 0 and cover the analysis end time; no extrapolation.");
            for (int i = 1; i < input.History.Count; i++)
                if (input.History[i].TimeSeconds <= input.History[i-1].TimeSeconds)
                    throw new ArgumentException("M59_HISTORY_ORDER: Knot times must be strictly increasing.");
            var forces = new double[ndof];
            foreach (StructuralHarmonicNodalForce f in input.Forces)
            {
                if (f is null || string.IsNullOrWhiteSpace(f.NodeId) || !nodeIds.TryGetValue(f.NodeId, out int index))
                    throw new ArgumentException("M59_UNKNOWN_NODE: Load must target an existing node.");
                if (!double.IsFinite(f.Fx) || !double.IsFinite(f.Fy) || !double.IsFinite(f.Mz))
                    throw new ArgumentException("M59_LOAD_RANGE: Load components must be finite.");
                forces[index * 3] += f.Fx;
                forces[index * 3 + 1] += f.Fy;
                forces[index * 3 + 2] += f.Mz;
            }
            if (forces.Any(x => !double.IsFinite(x)))
                throw new ArgumentException("M59_LOAD_RANGE: Sum of nodal forces overflows.");
            var modalForces = spectrum.Modes.Select(mode => Dot(mode.GlobalMassNormalizedShape, forces)).ToArray();
            patterns.Add(new PreparedPattern(forces, modalForces, input.History));
        }

        var initialU = new double[ndof];
        var initialV = new double[ndof];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (StructuralTimeInitialNodeState s in request.InitialStates ?? Array.Empty<StructuralTimeInitialNodeState>())
        {
            if (s is null || string.IsNullOrWhiteSpace(s.NodeId) || !nodeIds.TryGetValue(s.NodeId, out int i) || !seen.Add(s.NodeId))
                throw new ArgumentException("M59_INITIAL_NODE: Invalid, duplicated, or unknown initial node.");
            var values = new[] { s.Ux, s.Uy, s.Rz, s.Vx, s.Vy, s.VrZ };
            if (values.Any(x => !double.IsFinite(x)))
                throw new ArgumentException("M59_INITIAL_RANGE: Initial coordinates must be finite.");
            Array.Copy(values, 0, initialU, 3*i, 3);
            Array.Copy(values, 3, initialV, 3*i, 3);
        }
        double[] ru = ProjectInitial(reduced.Basis, initialU, "displacement");
        double[] rv = ProjectInitial(reduced.Basis, initialV, "velocity");
        double[] massRu = Multiply(reduced.Mass, ru);
        double[] massRv = Multiply(reduced.Mass, rv);
        double[] initialModalForces = ForceAt(patterns, 0, n, ndof, out double[] initialGlobalForce);
        var modes = new NewmarkAverageAcceleration[n];
        var ratios = new double[n];
        for (int i = 0; i < n; i++)
        {
            ModalEigenpair mode = spectrum.Modes[i];
            ratios[i] = request.Damping.Ratio(mode.AngularFrequencyRadiansPerSecond);
            modes[i] = new NewmarkAverageAcceleration(mode.AngularFrequencyRadiansPerSecond,
                ratios[i], request.TimeStepSeconds,
                Dot(mode.ReducedMassNormalizedShape, massRu),
                Dot(mode.ReducedMassNormalizedShape, massRv), initialModalForces[i]);
        }
        double initialEnergy = Energy(modes);
        double work = 0, dissipated = 0;
        double[] previousModalForces = initialModalForces;
        var output = new List<StructuralTimeStepResult>(request.StepCount + 1);
        var envelopes = new double[request.Model.Nodes.Count, 9];
        output.Add(Capture(0, 0, modes, spectrum, initialGlobalForce, request, nodeIds, envelopes,
            initialEnergy, work, dissipated));
        for (int step = 1; step <= request.StepCount; step++)
        {
            double t = step * request.TimeStepSeconds;
            double[] modalForces = ForceAt(patterns, t, n, ndof, out double[] globalForce);
            for (int mode = 0; mode < n; mode++)
            {
                double previousQ = modes[mode].Displacement;
                double previousV = modes[mode].Velocity;
                modes[mode].Advance(modalForces[mode]);
                // Energy-conjugate discrete work. Average-acceleration Newmark satisfies
                // delta(E) = mean(F) * delta(q) - dt * c * mean(v)^2 per mode.
                // Sampling F*v at endpoints would add an avoidable energy-balance error.
                work += 0.5 * (previousModalForces[mode] + modalForces[mode]) *
                    (modes[mode].Displacement - previousQ);
                double midVelocity = 0.5 * (previousV + modes[mode].Velocity);
                dissipated += request.TimeStepSeconds * modes[mode].Damping * midVelocity * midVelocity;
            }
            if (!double.IsFinite(work) || !double.IsFinite(dissipated))
                throw new InvalidOperationException("M59_ENERGY_RANGE: Accumulated work or dissipation is nonfinite.");
            output.Add(Capture(step, t, modes, spectrum, globalForce, request, nodeIds, envelopes,
                initialEnergy, work, dissipated));
            previousModalForces = modalForces;
        }
        var env = request.Model.Nodes.Select((node, i) => new StructuralTimeNodeEnvelope(node.Id,
            envelopes[i,0], envelopes[i,1], envelopes[i,2], envelopes[i,3], envelopes[i,4],
            envelopes[i,5], envelopes[i,6], envelopes[i,7], envelopes[i,8])).ToArray();
        var warnings = new List<string> {
            "M59 linear first-order elastic motion only; histories are explicit piecewise-linear nodal loads, not earthquakes or safety verification.",
            "All reported node displacements, velocities and accelerations are physical; no artificial modal scale is applied.",
            "Energy balance uses Newmark-consistent average-force displacement work and midpoint viscous dissipation; residual checks numerical arithmetic, not timestep convergence or code safety.",
            "Reaction histories and member stress/force envelopes are not computed in this milestone; node envelopes only."
        };
        if (request.TimeStepSeconds * spectrum.Modes[^1].FrequencyHertz > 0.1)
            warnings.Add("M59_RESOLUTION: dt * highest natural frequency > 0.1; higher-mode phase/acceleration may be inaccurate. Refine dt and compare.");
        return new StructuralTimeHistoryAnalysisOutput(output, env,
            spectrum.Modes.Select(x => x.FrequencyHertz).ToArray(), ratios, warnings);
    }

    private sealed record PreparedPattern(double[] Forces, double[] ModalForces,
        IReadOnlyList<StructuralTimeLoadKnot> Knots);

    private static double[] ForceAt(IReadOnlyList<PreparedPattern> patterns, double time, int n, int ndof,
        out double[] global)
    {
        var result = new double[n];
        global = new double[ndof];
        foreach (PreparedPattern pattern in patterns)
        {
            double factor = Factor(pattern.Knots, time);
            for (int i = 0; i < n; i++) result[i] += factor * pattern.ModalForces[i];
            for (int i = 0; i < ndof; i++) global[i] += factor * pattern.Forces[i];
        }
        if (result.Any(x => !double.IsFinite(x)) || global.Any(x => !double.IsFinite(x)))
            throw new ArgumentException("M59_FORCE_RANGE: Combined force values exceed double range.");
        return result;
    }

    private static double Factor(IReadOnlyList<StructuralTimeLoadKnot> knots, double time)
    {
        if (time == 0) return knots[0].Factor;
        int lo = 0, hi = knots.Count - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (knots[mid].TimeSeconds < time) lo = mid;
            else hi = mid;
        }
        if (time >= knots[^1].TimeSeconds) return knots[^1].Factor;
        double x0 = knots[lo].TimeSeconds, x1 = knots[hi].TimeSeconds;
        double t = (time - x0) / (x1 - x0);
        return knots[lo].Factor + t * (knots[hi].Factor - knots[lo].Factor);
    }

    private static double[] ProjectInitial(double[,] basis, double[] state, string label)
    {
        int n = basis.GetLength(0), r = basis.GetLength(1);
        var reduced = new double[r];
        for (int j = 0; j < r; j++)
            for (int i = 0; i < n; i++) reduced[j] += basis[i,j] * state[i];
        double max = state.Select(x => Math.Abs(x)).DefaultIfEmpty(0).Max();
        for (int i = 0; i < n; i++)
        {
            double recovered = 0;
            for (int j = 0; j < r; j++) recovered += basis[i,j] * reduced[j];
            if (Math.Abs(state[i] - recovered) > 1e-10 * Math.Max(1, max))
                throw new ArgumentException($"M59_INITIAL_CONSTRAINT: Initial {label} violates a support or removed nonphysical DOF.");
        }
        return reduced;
    }
    private static double[] Multiply(double[,] matrix, double[] vector)
    {
        var result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
            for (int j = 0; j < vector.Length; j++) result[i] += matrix[i,j] * vector[j];
        return result;
    }
    private static double Dot(IReadOnlyList<double> a, IReadOnlyList<double> b)
    {
        double v = 0;
        for (int i = 0; i < a.Count; i++) v += a[i] * b[i];
        return v;
    }
    private static double Energy(IReadOnlyList<NewmarkAverageAcceleration> modes) =>
        modes.Sum(m => 0.5 * (m.Velocity * m.Velocity + m.Stiffness * m.Displacement * m.Displacement));
    private static StructuralTimeStepResult Capture(int step, double time,
        IReadOnlyList<NewmarkAverageAcceleration> modes, ExperimentalModalSpectrum spectrum,
        double[] forces, StructuralTimeHistoryAnalysisRequest request,
        IReadOnlyDictionary<string, int> nodeIds, double[,] envelopes,
        double initialEnergy, double work, double dissipated)
    {
        int n = forces.Length;
        var u = new double[n]; var v = new double[n]; var a = new double[n];
        for (int j = 0; j < modes.Count; j++)
        {
            double[] phi = spectrum.Modes[j].GlobalMassNormalizedShape;
            for (int i = 0; i < n; i++)
            {
                u[i] += phi[i] * modes[j].Displacement;
                v[i] += phi[i] * modes[j].Velocity;
                a[i] += phi[i] * modes[j].Acceleration;
            }
        }
        var nodes = new StructuralTimeNodeResult[request.Model.Nodes.Count];
        for (int i = 0; i < nodes.Length; i++)
        {
            int k = i * 3;
            double[] nine = { u[k], u[k+1], u[k+2], v[k], v[k+1], v[k+2], a[k], a[k+1], a[k+2] };
            if (nine.Any(x => !double.IsFinite(x)))
                throw new InvalidOperationException("M59_NONFINITE_RESPONSE: Global response exceeded representable range.");
            for (int j = 0; j < 9; j++) envelopes[i,j] = Math.Max(envelopes[i,j], Math.Abs(nine[j]));
            nodes[i] = new StructuralTimeNodeResult(request.Model.Nodes[i].Id,
                nine[0], nine[1], nine[2], nine[3], nine[4], nine[5],
                nine[6], nine[7], nine[8], forces[k], forces[k+1], forces[k+2]);
        }
        double total = Energy(modes);
        double kinetic = modes.Sum(m => 0.5 * m.Velocity * m.Velocity);
        double elastic = total - kinetic;
        if (!double.IsFinite(total)) throw new InvalidOperationException("M59_ENERGY_RANGE: Nonfinite energy.");
        return new StructuralTimeStepResult(step, time, nodes,
            new StructuralTimeEnergyResult(kinetic, elastic, work, dissipated,
                total + dissipated - initialEnergy - work));
    }
}
