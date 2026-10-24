using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Frame2D.SecondOrder;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Serviceability;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Stable high-level facade for the standard StructuralSolver2D analysis workflow.
/// </summary>
/// <remarks>
/// This class is intended as the preferred public entry point for applications that want a complete first-order
/// linear elastic 2D analysis bundle without depending on every low-level solver and post-processing component.
/// Low-level classes remain available for advanced usage and tests.
/// </remarks>
public sealed class StructuralSolver2DService
{
    /// <summary>
    /// M82 experimental, global linearized second-order Frame2D calculation.
    /// First-order axial forces are frozen; only nodal actions and unreleased
    /// members with rigid axis-aligned restraints are supported. No design check.
    /// </summary>
    public Frame2DSecondOrderResultM82 AnalyzeFrameSecondOrder(
        StructuralModel model, string loadCaseId) =>
        new Frame2DGlobalSecondOrderM82().Analyze(model, loadCaseId);

    /// <summary>M68 opt-in static incremental elastoplastic Truss2D (research-only).</summary>
    public StructuralNonlinearStaticOutput AnalyzeNonlinearTrussStatic(StructuralNonlinearStaticRequest request) =>
        new StructuralNonlinearTruss2DService().AnalyzeStatic(request);

    /// <summary>M68 opt-in nonlinear Truss2D time integration (research-only).</summary>
    public StructuralNonlinearTimeOutput AnalyzeNonlinearTrussTime(StructuralNonlinearTimeRequest request) =>
        new StructuralNonlinearTruss2DService().AnalyzeTime(request);

    /// <summary>M67 opt-in time-dependent net support-node reactions and local elastic member end actions.</summary>
    public StructuralDynamicForceRecoveryOutput AnalyzeTimeHistoryForces(StructuralTimeHistoryAnalysisRequest request) =>
        new StructuralDynamicForceRecoveryService().Analyze(request);

    /// <summary>M60 uniform prescribed ground acceleration, physical relative response.</summary>
    public StructuralSeismicTimeOutput AnalyzeSeismicTime(StructuralSeismicTimeRequest request) =>
        new StructuralSeismicAnalysisService().AnalyzeTime(request);

    /// <summary>M60 non-normative elastic pseudo-response spectrum with modal SRSS envelopes.</summary>
    public StructuralSeismicSpectrumOutput AnalyzeSeismicSpectrum(StructuralSeismicSpectrumRequest request) =>
        new StructuralSeismicAnalysisService().AnalyzeSpectrum(request);

    /// <summary>M59.1 opt-in time-grid sensitivity study (dt, dt/2, dt/4).</summary>
    public StructuralTimeConvergenceOutput AnalyzeTimeHistoryConvergence(StructuralTimeConvergenceRequest request) =>
        new StructuralTimeConvergenceAnalysisService().Analyze(request);

    /// <summary>M59 opt-in linear transient response with Newmark integration.</summary>
    public StructuralTimeHistoryAnalysisOutput AnalyzeTimeHistory(StructuralTimeHistoryAnalysisRequest request) =>
        new StructuralTimeHistoryAnalysisService().Analyze(request);

    /// <summary>M58 opt-in linear steady-state harmonic response; unaffected static/modal entry points.</summary>
    public StructuralHarmonicAnalysisOutput AnalyzeHarmonic(StructuralHarmonicAnalysisRequest request) =>
        new StructuralHarmonicAnalysisService().Analyze(request);

    private readonly PlaneStructureAnalyzer analyzer = new();
    private readonly Frame2DInternalForceSampler internalForceSampler = new();
    private readonly Frame2DDisplacementSampler displacementSampler = new();
    private readonly Frame2DResultSummarizer summarizer = new();
    private readonly PreliminaryDeflectionChecker deflectionChecker = new();

    /// <summary>
    /// Analyzes one load case and returns the complete public result bundle.
    /// </summary>
    public StructuralAnalysisOutput AnalyzeLoadCase(
        StructuralModel model,
        string loadCaseId,
        StructuralAnalysisOptions? options = null) =>
        Analyze(StructuralAnalysisRequest.ForLoadCase(model, loadCaseId, options));

    /// <summary>
    /// Analyzes one manual load combination and returns the complete public result bundle.
    /// </summary>
    public StructuralAnalysisOutput AnalyzeLoadCombination(
        StructuralModel model,
        string loadCombinationId,
        StructuralAnalysisOptions? options = null) =>
        Analyze(StructuralAnalysisRequest.ForLoadCombination(model, loadCombinationId, options));

    /// <summary>
    /// Executes the standard public analysis workflow for a load case or a load combination.
    /// </summary>
    public StructuralAnalysisOutput Analyze(StructuralAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Model);

        if (string.IsNullOrWhiteSpace(request.TargetId))
        {
            throw new ArgumentException("The analysis target id cannot be empty.", nameof(request));
        }

        StructuralAnalysisOptions options = request.Options ?? new StructuralAnalysisOptions();
        options.Validate();

        StructuralAnalysisResult result = request.TargetKind switch
        {
            StructuralAnalysisTargetKind.LoadCase => analyzer.Analyze(request.Model, request.TargetId),
            StructuralAnalysisTargetKind.LoadCombination => analyzer.AnalyzeCombination(request.Model, request.TargetId),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.TargetKind, "Unsupported analysis target kind."),
        };

        IReadOnlyList<MemberInternalForceDiagram> internalForceDiagrams = internalForceSampler.SampleAllMembers(
            request.Model,
            result,
            options.InternalForceSampleCount);

        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams = ShouldSampleDisplacements(options)
            ? displacementSampler.SampleAllMembers(request.Model, result, options.DisplacementSampleCount)
            : Array.Empty<MemberDisplacementDiagram>();

        IReadOnlyList<DeflectionCheckResult> deflectionChecks = options.DeflectionLimit is null
            ? Array.Empty<DeflectionCheckResult>()
            : deflectionChecker.Check(displacementDiagrams, options.DeflectionLimit);

        StructuralAnalysisSummary summary = summarizer.Summarize(result, internalForceDiagrams);

        return new StructuralAnalysisOutput(
            result,
            internalForceDiagrams,
            displacementDiagrams,
            deflectionChecks,
            summary);
    }

    /// <summary>
    /// M78 opt-in, analytical piecewise-polynomial extrema N/V/M for a previously solved
    /// first-order linear static load case or combination. Independent of graphic sampling.
    /// This does not combine several alternative load combinations (planned M79).
    /// </summary>
    public IReadOnlyList<ExactMemberInternalForceExtremaM78> FindExactStaticMemberForceExtrema(
        StructuralModel model, StructuralAnalysisResult result) =>
        new Frame2DExactExtremaFinderM78().FindAll(model, result);

    /// <summary>
    /// M79 opt-in: exact global N/V/M extrema over explicitly selected elastic static
    /// load cases and/or manual combinations. Each scenario is solved once using
    /// the established 2D solver; M78 finds the exact extrema per Frame2D member.
    /// No automatic SLU/SLE combinations and no section-by-section envelope curves.
    /// </summary>
    public StructuralStaticForceEnvelopeM79 AnalyzeStaticForceEnvelope(
        StructuralModel model, IReadOnlyList<StaticForceEnvelopeScenarioM79> scenarios)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(scenarios);
        if (scenarios.Count == 0)
            throw new StructuralAnalysisException("M79_EMPTY_TARGETS: At least one case or combination must be selected.");
        if (!model.Members.Any(member => member.Type == StructuralSolver2D.Core.Model.Enums.MemberType.Frame2D))
            throw new StructuralAnalysisException("M79_NO_FRAME_MEMBERS: At least one Frame2D member is required.");

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        List<(StaticForceEnvelopeScenarioM79 Scenario, StructuralAnalysisResult Result)> results = new();
        foreach (StaticForceEnvelopeScenarioM79 scenario in scenarios)
        {
            if (scenario is null || string.IsNullOrWhiteSpace(scenario.Id))
                throw new StructuralAnalysisException("M79_INVALID_TARGET: Scenario and its id are required.");
            if (!Enum.IsDefined(scenario.Kind))
                throw new StructuralAnalysisException("M79_INVALID_TARGET: Unsupported static target kind.");
            if (!seen.Add(scenario.Id))
                throw new StructuralAnalysisException($"M79_DUPLICATE_TARGET: Result identifier '{scenario.Id}' appears more than once.");

            // M78 identifies the load factors by result id (combination first).
            // Ambiguous case/combination identifiers would yield a wrong post-processed profile.
            bool caseExists = model.LoadCases.Any(item => string.Equals(item.Id, scenario.Id, StringComparison.OrdinalIgnoreCase));
            bool combinationExists = model.LoadCombinations.Any(item => string.Equals(item.Id, scenario.Id, StringComparison.OrdinalIgnoreCase));
            if (caseExists && combinationExists)
                throw new StructuralAnalysisException($"M79_AMBIGUOUS_ID: '{scenario.Id}' is both a load case and a combination.");
            if (scenario.Kind == StructuralAnalysisTargetKind.LoadCase && !caseExists ||
                scenario.Kind == StructuralAnalysisTargetKind.LoadCombination && !combinationExists)
                throw new StructuralAnalysisException($"M79_UNKNOWN_TARGET: '{scenario.Id}' was not found for {scenario.Kind}.");

            StructuralAnalysisResult result = scenario.Kind switch
            {
                StructuralAnalysisTargetKind.LoadCase => analyzer.Analyze(model, scenario.Id),
                StructuralAnalysisTargetKind.LoadCombination => analyzer.AnalyzeCombination(model, scenario.Id),
                _ => throw new StructuralAnalysisException("M79_INVALID_TARGET: Unsupported target kind."),
            };
            results.Add((scenario, result));
        }
        return new Frame2DStaticForceEnvelopeBuilderM79().Build(model, results);
    }

    /// <summary>Opt-in, free-vibration modal analysis. Does not apply static loads.</summary>
    public StructuralModalAnalysisOutput AnalyzeModes(StructuralModalAnalysisRequest request) =>
        new StructuralModalAnalysisService().Analyze(request);

    private static bool ShouldSampleDisplacements(StructuralAnalysisOptions options) =>
        options.IncludeDisplacementDiagrams || options.DeflectionLimit is not null;
}
