using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;

namespace StructuralSolver2D.Verification.Integration;

/// <summary>
/// Explicit, single-station first-order static FEM-to-verification transfer.
/// This module never calculates a code resistance and never mixes N/V/M extrema
/// from different stations, cases, combinations, or sides of a point-load jump.
/// </summary>
public sealed record FemStationSelectionM87B(
    StructuralAnalysisTargetKind TargetKind, string TargetId,
    double DistanceMetres, SectionLimitM78 Side = SectionLimitM78.Continuous);

public sealed class FemStationVerificationBridgeM87B
{
    private readonly StructuralSolver2DService solver = new();
    private readonly Frame2DInternalForceSampler sampler = new();

    /// <summary>
    /// Solves one explicitly selected linear static case/combination, then builds a
    /// design request using signed, concomitant forces at precisely the same station.
    /// Material category, normative profile, restraint data and rules come ONLY from the caller.
    /// The result's CombinationId carries the explicit target ID even for a single load case;
    /// Effects.TargetKind distinguishes the two. AnalysisId identifies the analysis method.
    /// </summary>
    public DesignCheckRequest Capture(
        StructuralModel model, DesignMemberContext member, DesignLimitState limitState,
        NormativeProfile profile, IReadOnlyList<string> ruleIds, FemStationSelectionM87B selection) =>
        CaptureUsingResult(model, member, limitState, profile, ruleIds, selection, null);

    /// <summary>
    /// Reuses one already analyzed target for the M87-C station screening.
    /// Internal API: the caller must have solved the selected model/target.
    /// Public Capture retains the previous single-station behavior.
    /// </summary>
    internal DesignCheckRequest CaptureUsingResult(
        StructuralModel model, DesignMemberContext member, DesignLimitState limitState,
        NormativeProfile profile, IReadOnlyList<string> ruleIds, FemStationSelectionM87B selection,
        StructuralAnalysisResult? analyzedResult)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(ruleIds);
        ArgumentNullException.ThrowIfNull(selection);
        if (string.IsNullOrWhiteSpace(member.MemberId) || string.IsNullOrWhiteSpace(selection.TargetId) ||
            !Enum.IsDefined(selection.TargetKind))
            throw new ArgumentException("Member, load target and target kind must be valid.");

        StructuralMember physicalMember = model.Members.Single(m =>
            string.Equals(m.Id, member.MemberId, StringComparison.OrdinalIgnoreCase));
        if (physicalMember.Type != MemberType.Frame2D)
            throw new NotSupportedException("M87-B bridge currently supports first-order static Frame2D members only.");
        if (!string.Equals(physicalMember.SectionId, member.SectionId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Design section and FEM section identifiers differ.", nameof(member));
        StructuralNode from = model.Nodes.Single(n => string.Equals(n.Id, physicalMember.StartNodeId, StringComparison.OrdinalIgnoreCase));
        StructuralNode to = model.Nodes.Single(n => string.Equals(n.Id, physicalMember.EndNodeId, StringComparison.OrdinalIgnoreCase));
        double length = StructuralMember.GetLength(from, to);
        if (!double.IsFinite(length) || length <= 0 || !double.IsFinite(member.MemberLengthMetres) ||
            Math.Abs(length - member.MemberLengthMetres) > 1e-9 * Math.Max(1, length))
            throw new ArgumentException("Design length and FEM geometric length differ.", nameof(member));
        if (!double.IsFinite(selection.DistanceMetres) || selection.DistanceMetres < 0 || selection.DistanceMetres > length)
            throw new ArgumentOutOfRangeException(nameof(selection), "Section must lie within the FEM member.");

        // The post-processor's existing LoadCaseId lookup prioritizes combinations.
        // Refuse name collisions across target kinds to prevent a provenance swap.
        bool hasCase = model.LoadCases.Any(c => string.Equals(c.Id, selection.TargetId, StringComparison.OrdinalIgnoreCase));
        bool hasCombination = model.LoadCombinations.Any(c => string.Equals(c.Id, selection.TargetId, StringComparison.OrdinalIgnoreCase));
        if (hasCase && hasCombination)
            throw new ArgumentException("Ambiguous identifier exists as both load case and combination.", nameof(selection));
        if (selection.TargetKind == StructuralAnalysisTargetKind.LoadCase ? !hasCase : !hasCombination)
            throw new ArgumentException("The selected case or combination does not exist.", nameof(selection));

        StructuralAnalysisResult result = analyzedResult ?? solver.Analyze(new StructuralAnalysisRequest(model,
            selection.TargetId, selection.TargetKind)).Result;
        if (!string.Equals(result.LoadCaseId, selection.TargetId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Analysis result identifier differs from the requested target.");
        MemberInternalForceSample forces = sampler.EvaluateMemberSection(model, result,
            physicalMember.Id, selection.DistanceMetres, selection.Side);
        DesignEffectSnapshot effects = new(
            "FEM:linear-static:first-order", result.LoadCaseId,
            DesignEffectOrigin.FemAnalysis, forces.Distance,
            AxialForceKn: forces.NormalForce, ShearForceYKn: forces.ShearForce,
            BendingMomentZKnm: forces.BendingMoment)
        {
            MemberId = physicalMember.Id,
            TargetKind = selection.TargetKind == StructuralAnalysisTargetKind.LoadCase
                ? DesignEffectTargetKind.LoadCase : DesignEffectTargetKind.LoadCombination,
            SectionSide = selection.Side switch
            {
                SectionLimitM78.Continuous => DesignEffectSectionSide.Continuous,
                SectionLimitM78.BeforePointLoad => DesignEffectSectionSide.BeforePointLoad,
                SectionLimitM78.AfterPointLoad => DesignEffectSectionSide.AfterPointLoad,
                _ => throw new ArgumentOutOfRangeException(nameof(selection))
            }
        };
        return new DesignCheckRequest(member, limitState, profile, effects, Array.AsReadOnly(ruleIds.ToArray()));
    }

    /// <summary>Capture then evaluate; an empty registry returns Unsupported, never Passed.</summary>
    public DesignCheckReport Evaluate(StructuralModel model, DesignMemberContext member,
        DesignLimitState limitState, NormativeProfile profile, IReadOnlyList<string> ruleIds,
        FemStationSelectionM87B selection, DesignCheckEngine checks) =>
        (checks ?? throw new ArgumentNullException(nameof(checks))).Evaluate(
            Capture(model, member, limitState, profile, ruleIds, selection));
}
