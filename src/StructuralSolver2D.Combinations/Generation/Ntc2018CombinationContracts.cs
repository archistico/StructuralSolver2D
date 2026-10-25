using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Combinations.Generation;

/// <summary>
/// Restricted, non-geotechnical, static building combinations from NTC 2018
/// section 2.5.3. EQU, GEO, seismic, accidental and bridge cases are excluded.
/// </summary>
public enum Ntc2018CombinationFamily
{
    UlsStrA1,
    SlsRare,
    SlsFrequent,
    SlsQuasiPermanent
}

public enum Ntc2018ActionRole { Permanent, LeadingVariable, AccompanyingVariable, QuasiPermanentVariable }

public enum Ntc2018OmissionReason
{
    FavorableEffect,
    ZeroPsi,
    PhysicallyAbsentByEvidence,
    NotSelectedInScenario
}

/// <summary>
/// Explicit *engineering* choice for one scalar structural effect, not an
/// automatic search for every governing condition of an entire frame/member.
/// Every variable to be considered is listed in PresentActionIds, and every
/// favorable action is explicitly flagged with a traceable evidence reference.
/// </summary>
public sealed record Ntc2018CombinationScenario(
    string CombinationId,
    string EffectId,
    Ntc2018CombinationFamily Family,
    IReadOnlyList<string> PresentActionIds,
    string? LeadingActionId,
    IReadOnlyList<string> FavorableActionIds,
    string FavorabilityEvidenceId,
    IReadOnlyDictionary<string, string>? PhysicallyAbsentActionEvidence = null);

/// <summary>Provenance of one strictly non-zero factored Core load-case term.</summary>
public sealed record Ntc2018GeneratedTerm(
    string ActionId,
    string LoadCaseId,
    Ntc2018ActionRole Role,
    decimal Gamma,
    decimal Psi,
    decimal EffectiveFactor,
    string CoefficientSource,
    string ActionSourceEvidenceId,
    string? ExceptionEvidenceId);

/// <summary>Every action omitted has an explicit reason; not all omissions prove completeness.</summary>
public sealed record Ntc2018OmittedAction(
    string ActionId,
    string LoadCaseId,
    Ntc2018OmissionReason Reason,
    string EvidenceId);

/// <summary>
/// Candidate manifestation only: status never means a complete code-prescribed
/// family/envelope or member adequacy. P0-B4 must audit coverage against the
/// actual FEM model and every pertinent load pattern/effect.
/// </summary>
public sealed record Ntc2018CombinationManifest(
    string CombinationId,
    string EffectId,
    Ntc2018CombinationFamily Family,
    string CodeEdition,
    string? LeadingActionId,
    IReadOnlyList<Ntc2018GeneratedTerm> Terms,
    IReadOnlyList<Ntc2018OmittedAction> OmittedActions,
    bool FullCombinationSetAudited = false);

public sealed record Ntc2018GeneratedCombination(
    StructuralLoadCombination FemCombination,
    Ntc2018CombinationManifest Manifest);
