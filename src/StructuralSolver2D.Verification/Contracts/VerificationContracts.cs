namespace StructuralSolver2D.Verification.Contracts;

/// <summary>Distinct from FEM validation: these are normative design-check statuses.</summary>
public enum DesignMaterial { Steel, Timber }
public enum DesignLimitState { Ultimate, Serviceability }
public enum DesignEffectOrigin { FemAnalysis, ExternalManual }
/// <summary>Target kind of the FEM action set; manual provenance remains optional for older integrations.</summary>
public enum DesignEffectTargetKind { LoadCase, LoadCombination }
/// <summary>The selected one-sided section limit for concentrated member loads.</summary>
public enum DesignEffectSectionSide { Continuous, BeforePointLoad, AfterPointLoad }
public enum DesignCheckStatus
{
    Passed, Failed, MissingData, NotApplicable, Unsupported, InvalidInput, CalculationError
}
public enum DesignOverallStatus { Passed, Failed, Incomplete }

/// <summary>All three identifiers must be specified; there is no implicit latest edition or national annex.</summary>
public sealed record DesignCodeEdition(string DocumentId, string EditionId, string NationalAnnexId);

/// <summary>Explicit provenance: no default gamma, kmod or any other coefficient is injected.</summary>
public sealed record NormativeCoefficient(string Id, double Value, string SourceClause, string Description);

public sealed record NormativeProfile(
    string ProfileId,
    DesignCodeEdition Code,
    IReadOnlyList<NormativeCoefficient> Coefficients);

/// <summary>Out-of-plane parameters are deliberately nullable: unknown must never mean restrained.</summary>
public sealed record DesignMemberContext(
    string MemberId,
    DesignMaterial Material,
    string SectionId,
    double MemberLengthMetres,
    double? UnbracedLengthInPlaneMetres = null,
    double? UnbracedLengthOutOfPlaneMetres = null,
    bool? IsLaterallyRestrained = null);

/// <summary>
/// Forces are signed in the solver's local axes; no implicit absolute values or sign inversion.
/// A single station is intentionally used; uncorrelated force envelopes are NOT accepted here.
/// Units: kN, kNm, m. No automatic unit conversion.
/// </summary>
public sealed record DesignEffectSnapshot(
    string AnalysisId,
    string CombinationId,
    DesignEffectOrigin Origin,
    double PositionMetres,
    double? AxialForceKn = null,
    double? ShearForceYKn = null,
    double? BendingMomentZKnm = null,
    double? DeflectionMetres = null)
{
    /// <summary>Optional strongly typed provenance filled by the M87-B FEM bridge.</summary>
    public string? MemberId { get; init; }
    public DesignEffectTargetKind? TargetKind { get; init; }
    public DesignEffectSectionSide? SectionSide { get; init; }
}

/// <summary>No member/section material properties are silently inferred from the 2D FEM.</summary>
public sealed record DesignCheckRequest(
    DesignMemberContext Member,
    DesignLimitState LimitState,
    NormativeProfile Profile,
    DesignEffectSnapshot Effects,
    IReadOnlyList<string> RuleIds);

public sealed record DesignCheckResult(
    string RuleId,
    DesignCheckStatus Status,
    string Message,
    string? SourceClause = null,
    double? Demand = null,
    double? Resistance = null,
    double? Utilization = null,
    string? Unit = null,
    string? CalculationTrace = null);

public sealed class DesignCheckReport
{
    public DesignCheckReport(DesignCheckRequest request, IReadOnlyList<DesignCheckResult> results)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Results = Array.AsReadOnly((results ?? throw new ArgumentNullException(nameof(results))).ToArray());
    }

    public DesignCheckRequest Request { get; }
    public IReadOnlyList<DesignCheckResult> Results { get; }
    public bool IsComplete => Results.Count > 0 && Results.All(r => r.Status is DesignCheckStatus.Passed or DesignCheckStatus.Failed);
    public DesignOverallStatus OverallStatus =>
        Results.Any(r => r.Status == DesignCheckStatus.Failed) ? DesignOverallStatus.Failed :
        Results.Count > 0 && Results.All(r => r.Status == DesignCheckStatus.Passed) ? DesignOverallStatus.Passed :
        DesignOverallStatus.Incomplete;
}
