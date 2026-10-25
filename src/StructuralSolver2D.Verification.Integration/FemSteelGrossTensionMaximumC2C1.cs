using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Integration;

public enum FemSteelGrossTensionMaximumStatusC2C1
{
    InvalidInput,
    ScreeningIncomplete,
    RuleSpecificMaximumUnderConstantActionAssumptions,
    ObservedRuleFailure,
    AnalysisFailed
}

/// <summary>
/// An analytical CONSTANT-action special case for ONE already implemented
/// EC3 gross-section tension-yield rule, member and first-order load target.
/// A successful local gross-yield result is NEVER a whole-member design pass:
/// net-section rupture, connections, stability and other load targets are omitted.
/// </summary>
public sealed class FemSteelGrossTensionMaximumResultC2C1
{
    internal FemSteelGrossTensionMaximumResultC2C1(
        FemSteelGrossTensionMaximumStatusC2C1 status,
        string reasonCode, string explanation,
        NormativeRuleStationEnvelopeResultC2C0? stationAudit = null)
    {
        Status = status;
        ReasonCode = reasonCode;
        Explanation = explanation;
        StationAudit = stationAudit;
    }

    public FemSteelGrossTensionMaximumStatusC2C1 Status { get; }
    public string ReasonCode { get; }
    public string Explanation { get; }
    public NormativeRuleStationEnvelopeResultC2C0? StationAudit { get; }
    public double? ConstantGrossYieldUtilization =>
        SpecificRuleMaximumLocatedWithinIdealizedModel
            ? StationAudit?.HighestObservedUtilization?.Utilization : null;

    public bool SpecificRuleMaximumLocatedWithinIdealizedModel =>
        Status is FemSteelGrossTensionMaximumStatusC2C1.RuleSpecificMaximumUnderConstantActionAssumptions
            or FemSteelGrossTensionMaximumStatusC2C1.ObservedRuleFailure;
    public bool NumericalIntervalErrorBoundEstablished => false;
    public bool RuleImplementationIndependentlyAuthenticated => false;
    public bool CompleteActionSetEstablished => false;
    public bool AllApplicableRulesChecked => false;
    public bool MemberAdequacyEstablished => false;
    public bool WholeStructureVerified => false;
}

/// <summary>
/// C2C1 pilot: the first-order Frame2D sampler's load-free law has CONSTANT N
/// and V, with LINEAR M. Reject every selected member load (and self-weight)
/// and require V=M=0 exactly at both ends, N positive and unchanged. The only
/// enabled normative function is the caller-supplied EC3 M89-B gross-tension
/// yielding rule, evaluated through the real DesignCheckEngine at both ends.
/// No numerical zero tolerance is permitted: small nonzero secondary actions
/// must not silently become the pure-action design case.
/// </summary>
public static class FemSteelGrossTensionMaximumC2C1
{
    public const string SupportedRuleId = "EC3-2005-PURE-TENSION-GROSS-YIELD-M89B";
    public const string SupportedClause = "EN 1993-1-1:2005+A1:2014, 6.2.3(2)(a), Eq. (6.6)";

    public static FemSteelGrossTensionMaximumResultC2C1 Evaluate(
        StructuralModel model, DesignMemberContext member, NormativeProfile profile,
        StructuralAnalysisTargetKind targetKind, string targetId, IDesignCheckRule rule)
    {
        static FemSteelGrossTensionMaximumResultC2C1 End(
            FemSteelGrossTensionMaximumStatusC2C1 status, string code, string why,
            NormativeRuleStationEnvelopeResultC2C0? audit = null) =>
            new(status, code, why, audit);

        if (model is null || member is null || profile is null || profile.Code is null ||
            profile.Coefficients is null || rule is null ||
            string.IsNullOrWhiteSpace(member.MemberId) ||
            string.IsNullOrWhiteSpace(member.SectionId) ||
            !double.IsFinite(member.MemberLengthMetres) || member.MemberLengthMetres <= 0 ||
            string.IsNullOrWhiteSpace(targetId) || !Enum.IsDefined(targetKind) ||
            member.Material != DesignMaterial.Steel ||
            !string.Equals(rule.RuleId, SupportedRuleId, StringComparison.Ordinal) ||
            !string.Equals(rule.SourceClause, SupportedClause, StringComparison.Ordinal) ||
            rule.Material != DesignMaterial.Steel || rule.LimitState != DesignLimitState.Ultimate ||
            rule.Code != profile.Code ||
            profile.Code.DocumentId != "EN 1993-1-1" ||
            profile.Code.EditionId != "2005+A1:2014" ||
            string.IsNullOrWhiteSpace(profile.Code.NationalAnnexId))
            return End(FemSteelGrossTensionMaximumStatusC2C1.InvalidInput,
                "C2C1_RULE_OR_INPUT_INVALID", "Only the declared EN 1993-1-1:2005+A1:2014 M89-B gross-tension-yield rule is in scope.");

        if (!new StructuralModelValidator().Validate(model).IsValid)
            return End(FemSteelGrossTensionMaximumStatusC2C1.InvalidInput,
                "C2C1_MODEL_INVALID", "The Core model validator rejected the structure.");

        StructuralMember[] matching = model.Members.Where(m =>
            string.Equals(m.Id, member.MemberId, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matching.Length != 1 || matching[0].Type != MemberType.Frame2D ||
            !string.Equals(matching[0].SectionId, member.SectionId, StringComparison.OrdinalIgnoreCase))
            return End(FemSteelGrossTensionMaximumStatusC2C1.InvalidInput,
                "C2C1_MEMBER_INVALID", "Exactly one Frame2D member must match the declared section.");

        StructuralNode a = model.Nodes.Single(n =>
            string.Equals(n.Id, matching[0].StartNodeId, StringComparison.OrdinalIgnoreCase));
        StructuralNode b = model.Nodes.Single(n =>
            string.Equals(n.Id, matching[0].EndNodeId, StringComparison.OrdinalIgnoreCase));
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (!double.IsFinite(length) || length <= 0 ||
            Math.Abs(length - member.MemberLengthMetres) > 1e-9 * Math.Max(1, length))
            return End(FemSteelGrossTensionMaximumStatusC2C1.InvalidInput,
                "C2C1_LENGTH_MISMATCH", "FEM and normative member lengths differ.");

        bool caseExists = model.LoadCases.Any(c =>
            string.Equals(c.Id, targetId, StringComparison.OrdinalIgnoreCase));
        bool comboExists = model.LoadCombinations.Any(c =>
            string.Equals(c.Id, targetId, StringComparison.OrdinalIgnoreCase));
        if (caseExists == comboExists ||
            (targetKind == StructuralAnalysisTargetKind.LoadCase ? !caseExists : !comboExists))
            return End(FemSteelGrossTensionMaximumStatusC2C1.InvalidInput,
                "C2C1_TARGET_INVALID", "The FEM target must be unique and match the declared kind.");

        HashSet<string> activeCases = new(StringComparer.OrdinalIgnoreCase);
        if (caseExists)
            activeCases.Add(targetId);
        else
            foreach (StructuralLoadCombinationTerm term in model.LoadCombinations.Single(c =>
                string.Equals(c.Id, targetId, StringComparison.OrdinalIgnoreCase)).Terms)
                activeCases.Add(term.LoadCaseId);

        // Even a zero-scaled or mutually cancelling member load is excluded:
        // the promise is based on the declared load topology, not cancellation.
        if (model.Loads.Any(l => activeCases.Contains(l.LoadCaseId) &&
            (l.Type == StructuralLoadType.SelfWeight ||
             l.TargetType == StructuralLoadTargetType.Member &&
             string.Equals(l.TargetId, member.MemberId, StringComparison.OrdinalIgnoreCase))))
            return End(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete,
                "C2C1_MEMBER_LOAD_UNSUPPORTED", "A selected member action or model self-weight invalidates the load-free constant-action proof.");

        try
        {
            StructuralAnalysisResult solved = new StructuralSolver2DService().Analyze(
                new StructuralAnalysisRequest(model, targetId, targetKind)).Result;
            if (!string.Equals(solved.LoadCaseId, targetId, StringComparison.OrdinalIgnoreCase))
                return End(FemSteelGrossTensionMaximumStatusC2C1.AnalysisFailed,
                    "C2C1_ANALYSIS_TARGET_MISMATCH", "The FEM solved a different target.");

            var bridge = new FemStationVerificationBridgeM87B();
            var ids = new[] { SupportedRuleId };
            DesignCheckRequest start = bridge.CaptureUsingResult(model, member,
                DesignLimitState.Ultimate, profile, ids,
                new FemStationSelectionM87B(targetKind, targetId, 0), solved);
            DesignCheckRequest finish = bridge.CaptureUsingResult(model, member,
                DesignLimitState.Ultimate, profile, ids,
                new FemStationSelectionM87B(targetKind, targetId, length), solved);
            DesignEffectSnapshot e0 = start.Effects, e1 = finish.Effects;
            if (e0.AxialForceKn is not double n0 || e1.AxialForceKn is not double n1 ||
                e0.ShearForceYKn != 0d || e1.ShearForceYKn != 0d ||
                e0.BendingMomentZKnm != 0d || e1.BendingMomentZKnm != 0d ||
                n0 != n1 || !double.IsFinite(n0) || n0 <= 0)
                return End(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete,
                    "C2C1_NOT_STRICT_PURE_CONSTANT_TENSION",
                    "The solved member is not exactly pure constant positive axial tension at both endpoints.");

            var engine = new DesignCheckEngine(new[] { rule });
            DesignCheckReport first = engine.Evaluate(start);
            DesignCheckReport last = engine.Evaluate(finish);
            var audit = NormativeRuleStationEnvelopeAuditorC2C0.Audit(
                new NormativeRuleStationPlanC2C0("C2C1-EVALUATED-PILOT", member, profile.Code,
                    DesignLimitState.Ultimate, SupportedRuleId,
                    targetKind == StructuralAnalysisTargetKind.LoadCase
                        ? DesignEffectTargetKind.LoadCase : DesignEffectTargetKind.LoadCombination,
                    targetId, new[] { first, last }));

            if (audit.Status == NormativeRuleStationEnvelopeStatusC2C0.InvalidInput)
                return End(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete,
                    "C2C1_RULE_AUDIT_INVALID", "The evaluated reports did not pass the C2C0 consistency contract.", audit);

            DesignCheckResult r0 = first.Results.Single(), r1 = last.Results.Single();
            if (r0.Status is not (DesignCheckStatus.Passed or DesignCheckStatus.Failed) ||
                r1.Status != r0.Status ||
                r0.Demand != n0 || r1.Demand != n0 ||
                r0.Resistance != r1.Resistance ||
                r0.Utilization != r1.Utilization ||
                r0.Unit != "kN")
                return End(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete,
                    "C2C1_RULE_NONUNIFORM_OR_INCOMPLETE",
                    "The registered rule did not yield one identical, calculated gross-yield ratio at both ends.", audit);

            // From the load-free Frame2D section-force law, N(x)=N0, V(x)=0,
            // M(x)=0 for all x in [0,L], under the explicitly checked exact-zero
            // conditions. This does not validate other EC3 limit states.
            return End(r0.Status == DesignCheckStatus.Failed
                    ? FemSteelGrossTensionMaximumStatusC2C1.ObservedRuleFailure
                    : FemSteelGrossTensionMaximumStatusC2C1.RuleSpecificMaximumUnderConstantActionAssumptions,
                r0.Status == DesignCheckStatus.Failed ? "C2C1_GROSS_YIELD_FAILURE" : "C2C1_GROSS_YIELD_RATIO_LOCATED",
                "Uniform gross-tension-yield utilization located for this one load target under the exact pure-axial model assumptions; no member design pass.", audit);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or
            ArithmeticException or NotSupportedException or
            StructuralSolver2D.Analysis.StructuralAnalysisException)
        {
            return End(FemSteelGrossTensionMaximumStatusC2C1.AnalysisFailed,
                "C2C1_ANALYSIS_OR_BRIDGE_FAILED", "FEM analysis or station recovery failed: " + ex.GetType().Name + ".");
        }
    }
}
