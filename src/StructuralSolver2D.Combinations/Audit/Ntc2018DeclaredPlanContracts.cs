using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Combinations.Generation;

namespace StructuralSolver2D.Combinations.Audit;

/// <summary>
/// Finite, explicitly declared scope for ONE scalar effect. Evidence records
/// that an engineer has decided which load patterns may coexist; it does NOT
/// establish that every physical/normative case has been found.
/// </summary>
public sealed record Ntc2018DeclaredCombinationPlan(
    string PlanId,
    string EffectId,
    IReadOnlyList<Ntc2018CombinationFamily> RequiredFamilies,
    IReadOnlyList<string> InScopeActionIds,
    IReadOnlyDictionary<string, string> OutOfScopeActionEvidence,
    IReadOnlyDictionary<string, string> NeverLeadingVariableEvidence,
    string PhysicalCompatibilityEvidenceId,
    IReadOnlyList<Ntc2018CombinationScenario> Scenarios);

/// <summary>Immutable view of the Core terms expected before FEM execution.</summary>
public sealed record Ntc2018AuditedCandidate(
    string CombinationId,
    Ntc2018CombinationFamily Family,
    string? LeadingActionId,
    IReadOnlyList<StructuralLoadCombinationTerm> ExpectedCoreTerms,
    string CoreInputSha256);

/// <summary>
/// Baseline frozen before adding candidate combinations to the Core model.
/// Its hash is over the real Core model contents, a specific set of terms,
/// the typed inventory and the complete declared scenario plan.
/// </summary>
public sealed class Ntc2018CombinationAuditBaseline
{
    internal Ntc2018CombinationAuditBaseline(string planId, string effectId,
        IReadOnlyList<Ntc2018AuditedCandidate> candidates)
    {
        PlanId = planId;
        EffectId = effectId;
        Candidates = Array.AsReadOnly(candidates.ToArray());
    }

    public string PlanId { get; }
    public string EffectId { get; }
    public IReadOnlyList<Ntc2018AuditedCandidate> Candidates { get; }
}

public enum Ntc2018DeclaredAuditIssueLevel { Invalid, Incomplete, Unsupported }

public enum Ntc2018DeclaredAuditStatus
{
    MatchedDeclaredPlan,
    Incomplete,
    Unsupported,
    Invalid
}

public sealed record Ntc2018DeclaredAuditIssue(
    string Code,
    Ntc2018DeclaredAuditIssueLevel Level,
    string? CombinationId,
    string Message);

/// <summary>
/// MatchedDeclaredPlan is not a code-compliant structure, checked member or
/// objectively exhaustive building loading. Global completeness ALWAYS false.
/// </summary>
public sealed class Ntc2018DeclaredAuditResult
{
    public Ntc2018DeclaredAuditResult(IEnumerable<Ntc2018DeclaredAuditIssue> issues)
    {
        Issues = Array.AsReadOnly(issues.ToArray());
        Status = Issues.Any(x => x.Level == Ntc2018DeclaredAuditIssueLevel.Invalid)
            ? Ntc2018DeclaredAuditStatus.Invalid
            : Issues.Any(x => x.Level == Ntc2018DeclaredAuditIssueLevel.Unsupported)
                ? Ntc2018DeclaredAuditStatus.Unsupported
                : Issues.Any(x => x.Level == Ntc2018DeclaredAuditIssueLevel.Incomplete)
                    ? Ntc2018DeclaredAuditStatus.Incomplete
                    : Ntc2018DeclaredAuditStatus.MatchedDeclaredPlan;
    }

    public IReadOnlyList<Ntc2018DeclaredAuditIssue> Issues { get; }
    public Ntc2018DeclaredAuditStatus Status { get; }

    /// <summary>No automatic attestation is possible from a 2D FEM inventory.</summary>
    public bool GlobalNormativeCompletenessEstablished => false;

    /// <summary>No member's code-check obligations are evaluated by this module.</summary>
    public bool MemberAdequacyEstablished => false;
}
