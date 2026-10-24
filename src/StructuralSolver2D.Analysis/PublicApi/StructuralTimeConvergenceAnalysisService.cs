namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// M59.1: three independent time integrations over the same physical interval.
/// All metrics are sampled errors against a finer computation, not errors against an exact solution.
/// </summary>
public sealed record StructuralTimeConvergenceRequest(
    StructuralTimeHistoryAnalysisRequest BaseRequest,
    string? FocusNodeId = null,
    string FocusComponent = "Uy");

public sealed record StructuralTimeComponentComparison(
    string Component, string Unit, bool HasResolvedSignal,
    double CoarseMediumRelativeDifference, double MediumFineRelativeDifference,
    double FineAbsoluteEnvelope, double? ObservedOrder);

public sealed record StructuralTimePeakComparison(
    string NodeId, string Component, string Unit,
    double CoarseAmplitude, double CoarseTimeSeconds,
    double MediumAmplitude, double MediumTimeSeconds,
    double FineAmplitude, double FineTimeSeconds,
    double CoarseFinePeakRelativeDifference,
    double MediumFinePeakRelativeDifference);

public sealed record StructuralTimeFrequencyResolution(
    int Mode, double FrequencyHertz, double CoarseCyclesPerStep,
    double MediumCyclesPerStep, double FineCyclesPerStep);

public sealed record StructuralTimeConvergenceOutput(
    double EndTimeSeconds, double CoarseTimeStepSeconds,
    int CoarseStepCount,
    IReadOnlyList<StructuralTimeComponentComparison> Components,
    StructuralTimePeakComparison FocusPeak,
    IReadOnlyList<StructuralTimeFrequencyResolution> ModalResolution,
    IReadOnlyList<string> Warnings,
    bool DifferencesDecrease,
    bool MeetsIllustrativeTolerances,
    bool FineGridResolvesAllModes,
    bool HasMeaningfulResponse)
{
    public string Method => "Three-grid Newmark comparison (dt, dt/2, dt/4); matched timestamps; unchanged physical loads";
    public string Interpretation =>
        "Grid-to-grid discrepancies and peak shifts, not a proven bound on exact error or structural safety.";
}

public sealed class StructuralTimeConvergenceAnalysisService
{
    // Keep the M59 2,000-step limit intact: no silent truncation or stretching of duration.
    private const int MaxCoarseSteps = 500;
    private const double MaxCyclesPerStep = 0.1; // illustrative, conservative resolution warning
    private const double NearZeroSignal = 1e-12;
    private const double NearZeroComparison = 1e-12;
    private static readonly string[] Components = { "Ux", "Uy", "Rz", "Vx", "Vy", "VrZ", "Ax", "Ay", "ArZ" };
    private static readonly string[] Units = { "m", "m", "rad", "m/s", "m/s", "rad/s", "m/s2", "m/s2", "rad/s2" };

    public StructuralTimeConvergenceOutput Analyze(StructuralTimeConvergenceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.BaseRequest);
        var input = request.BaseRequest;
        if (input.StepCount < 1 || input.StepCount > MaxCoarseSteps ||
            !double.IsFinite(input.TimeStepSeconds) || input.TimeStepSeconds <= 0 ||
            !double.IsFinite(input.TimeStepSeconds / 4) || input.TimeStepSeconds / 4 <= 0)
            throw new ArgumentException(
                "M591_GRID_LIMIT: Require 1..500 coarse steps and finite dt/4 > 0; finest grid uses 4 times the steps (M59 limit: 2000). Supply a coarser baseline or shorter duration.");
        if (input.Model is null || input.Model.Nodes.Count == 0)
            throw new ArgumentException("M591_MODEL: At least one node is required.");
        string focus = request.FocusNodeId ?? input.Model.Nodes[^1].Id;
        int focusIndex = -1;
        for (int i = 0; i < input.Model.Nodes.Count; i++)
            if (input.Model.Nodes[i].Id == focus) { focusIndex = i; break; }
        if (focusIndex < 0)
            throw new ArgumentException($"M591_FOCUS_NODE: Unknown focus node '{focus}'.");
        int focusComponentIndex = Array.IndexOf(Components, request.FocusComponent);
        if (focusComponentIndex < 0)
            throw new ArgumentException("M591_FOCUS_COMPONENT: Expected Ux, Uy, Rz, Vx, Vy, VrZ, Ax, Ay or ArZ (case-sensitive).");

        var solver = new StructuralTimeHistoryAnalysisService();
        StructuralTimeHistoryAnalysisOutput coarse = solver.Analyze(input);
        StructuralTimeHistoryAnalysisOutput medium = solver.Analyze(input with {
            TimeStepSeconds = input.TimeStepSeconds / 2, StepCount = checked(input.StepCount * 2) });
        StructuralTimeHistoryAnalysisOutput fine = solver.Analyze(input with {
            TimeStepSeconds = input.TimeStepSeconds / 4, StepCount = checked(input.StepCount * 4) });

        var comparisons = new List<StructuralTimeComponentComparison>(9);
        bool anyActive = false;
        bool decreasing = true;
        bool withinTolerance = true;
        for (int component = 0; component < Components.Length; component++)
        {
            double finePeak = Envelope(fine, component);
            // Detect an alias or loss of response: the finest run may be (nearly) zero
            // even when a coarser grid is not. Never classify that as "unexcited".
            bool active = Math.Max(finePeak,
                Math.Max(Envelope(coarse, component), Envelope(medium, component))) > NearZeroSignal;
            anyActive |= active;
            double scale = Math.Max(finePeak, NearZeroSignal);
            double cm = active ? Difference(coarse, medium, 2, component) / scale : 0;
            double mf = active ? Difference(medium, fine, 2, component) / scale : 0;
            // When both differences are near floating point noise, no observed order is meaningful.
            double? order = cm > NearZeroComparison && mf > NearZeroComparison
                ? Math.Log2(cm / mf) : null;
            if (active)
            {
                // A non-monotone comparison is evidence against an apparent convergence claim;
                // allow sub-noise variations when both differences are negligible.
                if (mf > cm * 1.05 && mf > NearZeroComparison) decreasing = false;
                double threshold = component < 3 ? 0.01 : component < 6 ? 0.02 : 0.05;
                if (mf > threshold) withinTolerance = false;
            }
            comparisons.Add(new StructuralTimeComponentComparison(Components[component], Units[component],
                active, cm, mf, finePeak, order));
        }
        double duration = input.TimeStepSeconds * input.StepCount;
        var freq = fine.NaturalFrequenciesHertz.Select((frequency, i) =>
            new StructuralTimeFrequencyResolution(i + 1, frequency,
                frequency * input.TimeStepSeconds,
                frequency * input.TimeStepSeconds / 2,
                frequency * input.TimeStepSeconds / 4)).ToArray();
        bool fineResolvesAll = freq.All(x => x.FineCyclesPerStep <= MaxCyclesPerStep);
        // Zero response cannot substantiate a convergence verdict even if all numerical
        // differences are zero. Keep the independent frequency-resolution flag separate.
        if (!anyActive) { decreasing = false; withinTolerance = false; }
        var warnings = new List<string> {
            "M591_SCOPE: Three-grid differences are not exact-solution error bounds, certification or engineering acceptance criteria. Thresholds (1% U, 2% V, 5% A) are illustrative diagnostics only.",
            "M591_HIGH_MODES: Frequency checks consider every retained mode regardless of actual modal participation. A stable Newmark computation may still suffer phase/acceleration error.",
            "M591_PEAK_SAMPLING: Peak amplitudes and occurrence times are sampled-grid estimates; changes may reflect a shifted peak even if common-time values converge."
        };
        if (!fineResolvesAll)
            warnings.Add("M591_FINE_RESOLUTION: Even dt/4 gives dt*f > 0.1 for some modes. Reduce the baseline dt or shorten the duration to stay within 2,000 steps.");
        // An all-mode resolution target may be mathematically impossible over this
        // observation interval under M59's hard cap, regardless of coarse-grid choice.
        // This is an advisory heuristic, not an engineering acceptance requirement.
        if (freq.Length > 0)
        {
            double highestFrequency = freq.Max(x => x.FrequencyHertz);
            double requiredFineSteps = Math.Ceiling(duration * highestFrequency / MaxCyclesPerStep);
            if (requiredFineSteps > 2000)
                warnings.Add($"M591_STEP_BUDGET: The highest mode would require approximately {requiredFineSteps:G9} steps over the unchanged {duration:G9} s interval to reach dt*f <= 0.1, exceeding the M59 limit of 2000. This heuristic cannot be satisfied for all modes here; assess modal participation and the quantities of interest rather than treating it as a pass/fail safety criterion.");
        }
        if (!anyActive)
            warnings.Add("M591_ZERO_RESPONSE: No response above the reporting noise floor; zero grid difference cannot establish convergence.");
        if (anyActive && !decreasing)
            warnings.Add("M591_NONMONOTONE: At least one active component has a larger medium-to-fine than coarse-to-medium discrepancy.");
        if (anyActive && !withinTolerance)
            warnings.Add("M591_TOLERANCE: At least one component exceeds its illustrative medium-to-fine comparison threshold.");
        if (input.Loads is not null && input.Loads.Any(load => load?.History is not null &&
            load.History.Any(knot => knot is not null && knot.TimeSeconds > 0 &&
                knot.TimeSeconds < duration &&
                Math.Abs(knot.TimeSeconds / input.TimeStepSeconds -
                    Math.Round(knot.TimeSeconds / input.TimeStepSeconds)) > 1e-8)))
            warnings.Add("M591_KNOT_ALIGNMENT: Some load-history knots lie between coarse-grid time samples. Refine and align critical load changes when interpreting peak errors.");

        var cp = Peak(coarse, focusIndex, focusComponentIndex);
        var mp = Peak(medium, focusIndex, focusComponentIndex);
        var fp = Peak(fine, focusIndex, focusComponentIndex);
        var peak = new StructuralTimePeakComparison(focus, Components[focusComponentIndex], Units[focusComponentIndex],
            cp.Amplitude, cp.Time, mp.Amplitude, mp.Time, fp.Amplitude, fp.Time,
            RelativePeakDifference(cp.Amplitude, fp.Amplitude),
            RelativePeakDifference(mp.Amplitude, fp.Amplitude));
        double focusThreshold = focusComponentIndex < 3 ? 0.01 : focusComponentIndex < 6 ? 0.02 : 0.05;
        if (anyActive && Math.Max(cp.Amplitude, Math.Max(mp.Amplitude, fp.Amplitude)) <= NearZeroSignal)
            warnings.Add("M591_FOCUS_ZERO: The selected node/component has negligible response, although other components are active; choose a meaningful focus to assess peaks.");
        if (peak.MediumFinePeakRelativeDifference > focusThreshold &&
            Math.Max(mp.Amplitude, fp.Amplitude) > NearZeroSignal)
            warnings.Add("M591_PEAK_VARIATION: The selected peak amplitude changes by more than its illustrative component threshold between medium and fine grids; common-time convergence alone does not validate sampled peaks.");
        if (Math.Abs(mp.Time - fp.Time) > input.TimeStepSeconds / 2)
            warnings.Add("M591_PEAK_TIME_SHIFT: The sampled peak time moves by more than one medium-grid step; inspect the time histories and refine the grid.");
        return new StructuralTimeConvergenceOutput(duration, input.TimeStepSeconds,
            input.StepCount, comparisons, peak, freq, warnings,
            decreasing, withinTolerance, fineResolvesAll, anyActive);
    }

    private static double RelativePeakDifference(double a, double b) =>
        Math.Abs(a - b) / Math.Max(Math.Abs(b), NearZeroSignal);

    private static (double Amplitude, double Time) Peak(StructuralTimeHistoryAnalysisOutput output,
        int node, int component)
    {
        double maximum = -1, time = 0;
        foreach (StructuralTimeStepResult step in output.Steps)
        {
            double value = Math.Abs(Value(step.Nodes[node], component));
            if (value > maximum) { maximum = value; time = step.TimeSeconds; }
        }
        return (maximum, time);
    }

    private static double Envelope(StructuralTimeHistoryAnalysisOutput output, int component)
    {
        double maximum = 0;
        foreach (StructuralTimeStepResult step in output.Steps)
            foreach (StructuralTimeNodeResult node in step.Nodes)
                maximum = Math.Max(maximum, Math.Abs(Value(node, component)));
        return maximum;
    }

    private static double Difference(StructuralTimeHistoryAnalysisOutput coarse,
        StructuralTimeHistoryAnalysisOutput fine, int stride, int component)
    {
        if (coarse.Steps.Count * stride - (stride - 1) != fine.Steps.Count)
            throw new InvalidOperationException("M591_GRID_MISMATCH: Refined grids must share all coarse sample instants.");
        double maximum = 0;
        for (int i = 0; i < coarse.Steps.Count; i++)
        {
            StructuralTimeStepResult low = coarse.Steps[i], high = fine.Steps[i * stride];
            if (Math.Abs(low.TimeSeconds - high.TimeSeconds) >
                1e-9 * Math.Max(1, Math.Abs(low.TimeSeconds)))
                throw new InvalidOperationException("M591_TIME_MISMATCH: Grid sample instants differ.");
            for (int j = 0; j < low.Nodes.Count; j++)
                maximum = Math.Max(maximum, Math.Abs(Value(low.Nodes[j], component) -
                    Value(high.Nodes[j], component)));
        }
        return maximum;
    }

    private static double Value(StructuralTimeNodeResult n, int component) => component switch
    {
        0 => n.Ux, 1 => n.Uy, 2 => n.Rz,
        3 => n.Vx, 4 => n.Vy, 5 => n.VrZ,
        6 => n.Ax, 7 => n.Ay, 8 => n.ArZ,
        _ => throw new ArgumentOutOfRangeException(nameof(component))
    };
}
