using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Integration;

/// <summary>
/// A finite, externally declared screening station. Each rule list is a set of
/// requested RULE IDENTITIES, not evidence those rules were evaluated or passed.
/// </summary>
public sealed record FemDeclaredStationC1(
    string StationId,
    DesignEffectTargetKind TargetKind,
    string TargetId,
    double DistanceMetres,
    DesignEffectSectionSide SectionSide,
    DesignLimitState LimitState,
    string ProfileId,
    IReadOnlyList<string> RuleIds);

/// <summary>
/// Required stations for ONE Frame2D member. Caller assertions about station
/// and combination coverage are NOT authenticated and cannot prove exhaustiveness.
/// </summary>
public sealed record FemMemberStationPlanC1(
    string PlanId,
    DesignMemberContext Member,
    DesignCodeEdition Code,
    IReadOnlyList<FemDeclaredStationC1> Stations,
    string StationSelectionEvidenceId,
    string CombinationSetEvidenceId);

public enum FemStationAuditIssueLevelC1 { Invalid, Incomplete, Unsupported }
public enum FemStationAuditStatusC1 { MatchedDeclaredStations, Incomplete, Unsupported, Invalid }

public sealed record FemStationAuditIssueC1(
    string Code, FemStationAuditIssueLevelC1 Level, string? StationId, string Message);

/// <summary>
/// Even a perfect FEM match says NOTHING about the interval between stations,
/// the global maximum of a nonlinear utilization function, member adequacy,
/// or physical/normative completeness.
/// </summary>
public sealed class FemConcomitantStationAuditResultC1
{
    public FemConcomitantStationAuditResultC1(IEnumerable<FemStationAuditIssueC1> issues,
        int matchedStations)
    {
        Issues = Array.AsReadOnly(issues.ToArray());
        MatchedStations = matchedStations;
        Status = Issues.Any(x => x.Level == FemStationAuditIssueLevelC1.Invalid)
            ? FemStationAuditStatusC1.Invalid
            : Issues.Any(x => x.Level == FemStationAuditIssueLevelC1.Unsupported)
                ? FemStationAuditStatusC1.Unsupported
                : Issues.Any(x => x.Level == FemStationAuditIssueLevelC1.Incomplete)
                    ? FemStationAuditStatusC1.Incomplete
                    : FemStationAuditStatusC1.MatchedDeclaredStations;
    }

    public IReadOnlyList<FemStationAuditIssueC1> Issues { get; }
    public FemStationAuditStatusC1 Status { get; }
    public int MatchedStations { get; }
    public bool MemberAdequacyEstablished => false;
    public bool ContinuousUtilizationMaximumEstablished => false;
    public bool StationSetExhaustivenessEstablished => false;
}

/// <summary>
/// Re-solves each distinct declared load target ONCE and checks that all three
/// signed N/V/M values actually belong to the same FEM member, load target,
/// station and one-sided limit. Does not alter Analysis or code-check rules.
/// This is an input/provenance consistency check, NOT a member verification.
/// </summary>
public static class FemConcomitantStationAuditorC1
{
    private readonly record struct Key(
        DesignEffectTargetKind Kind, string Target, double Distance,
        DesignEffectSectionSide Side, DesignLimitState LimitState, string Profile);

    public static FemConcomitantStationAuditResultC1 Audit(
        StructuralModel model, FemMemberStationPlanC1 plan,
        IReadOnlyList<DesignCheckRequest> actualRequests)
    {
        List<FemStationAuditIssueC1> issues = new();
        void Add(string code, FemStationAuditIssueLevelC1 level, string? stationId, string message) =>
            issues.Add(new FemStationAuditIssueC1(code, level, stationId, message));

        if (model is null || plan is null || actualRequests is null ||
            plan.Member is null || plan.Code is null || plan.Stations is null)
        {
            Add("C1_INPUT_MISSING", FemStationAuditIssueLevelC1.Invalid, null,
                "Model, plan, member, edition and actual captured requests are mandatory.");
            return new FemConcomitantStationAuditResultC1(issues, 0);
        }

        StructuralModelValidationResult core = new StructuralModelValidator().Validate(model);
        if (!core.IsValid)
        {
            Add("C1_CORE_MODEL_INVALID", FemStationAuditIssueLevelC1.Invalid, null,
                "The supplied FEM model is invalid: " + string.Join(", ", core.Errors.Select(x => x.Code)));
            return new FemConcomitantStationAuditResultC1(issues, 0);
        }

        if (string.IsNullOrWhiteSpace(plan.PlanId) ||
            string.IsNullOrWhiteSpace(plan.Member.MemberId) ||
            string.IsNullOrWhiteSpace(plan.Member.SectionId) ||
            !double.IsFinite(plan.Member.MemberLengthMetres) || plan.Member.MemberLengthMetres <= 0 ||
            !Enum.IsDefined(plan.Member.Material) ||
            string.IsNullOrWhiteSpace(plan.Code.DocumentId) ||
            string.IsNullOrWhiteSpace(plan.Code.EditionId) ||
            string.IsNullOrWhiteSpace(plan.Code.NationalAnnexId))
            Add("C1_PLAN_IDENTITY_INVALID", FemStationAuditIssueLevelC1.Invalid, null,
                "Member, physical length and explicit code edition must be valid.");
        if (string.IsNullOrWhiteSpace(plan.StationSelectionEvidenceId) ||
            string.IsNullOrWhiteSpace(plan.CombinationSetEvidenceId))
            Add("C1_EXTERNAL_REVIEW_MISSING", FemStationAuditIssueLevelC1.Incomplete, null,
                "Station selection and combination scope require external declared evidence.");
        if (plan.Stations.Count == 0)
            Add("C1_STATION_PLAN_EMPTY", FemStationAuditIssueLevelC1.Incomplete, null,
                "A nonempty finite set of required stations must be declared.");

        StructuralMember[] members = model.Members.Where(m => Same(m.Id, plan.Member.MemberId)).ToArray();
        if (members.Length != 1)
            Add("C1_MEMBER_UNKNOWN", FemStationAuditIssueLevelC1.Invalid, null,
                "Exactly one FEM member must match the declared member ID.");
        else if (members[0].Type != MemberType.Frame2D)
            Add("C1_NON_FRAME_UNSUPPORTED", FemStationAuditIssueLevelC1.Unsupported, null,
                "This audit is limited to first-order linear Frame2D; Truss2D requires a separate axial bridge.");
        else if (!Same(members[0].SectionId, plan.Member.SectionId))
            Add("C1_SECTION_MISMATCH", FemStationAuditIssueLevelC1.Invalid, null,
                "Declared section differs from the actual FEM member section.");
        else
        {
            StructuralNode from = model.Nodes.Single(n => Same(n.Id, members[0].StartNodeId));
            StructuralNode to = model.Nodes.Single(n => Same(n.Id, members[0].EndNodeId));
            double length = StructuralMember.GetLength(from, to);
            if (Math.Abs(length - plan.Member.MemberLengthMetres) > 1e-9 * Math.Max(1, length))
                Add("C1_MEMBER_LENGTH_MISMATCH", FemStationAuditIssueLevelC1.Invalid, null,
                    "Member geometric length differs from declared design length.");
        }

        Dictionary<Key, FemDeclaredStationC1> expected = new();
        HashSet<string> stationIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (FemDeclaredStationC1? station in plan.Stations)
        {
            if (station is null || string.IsNullOrWhiteSpace(station.StationId) ||
                !stationIds.Add(station.StationId))
            {
                Add("C1_STATION_ID_INVALID", FemStationAuditIssueLevelC1.Invalid, null,
                    "Station identifiers must be unique and nonempty.");
                continue;
            }
            if (!ValidStation(station, plan.Member.MemberLengthMetres))
            {
                Add("C1_STATION_INVALID", FemStationAuditIssueLevelC1.Invalid, station.StationId,
                    "Station requires a finite in-member position, typed target, profile and nonempty distinct rule IDs.");
                continue;
            }
            bool isCase = model.LoadCases.Any(c => Same(c.Id, station.TargetId));
            bool isCombination = model.LoadCombinations.Any(c => Same(c.Id, station.TargetId));
            if (isCase == isCombination ||
                (station.TargetKind == DesignEffectTargetKind.LoadCase ? !isCase : !isCombination))
            {
                Add("C1_TARGET_NOT_UNAMBIGUOUS", FemStationAuditIssueLevelC1.Invalid, station.StationId,
                    "The target must exist as exactly its declared FEM kind, with no ID collision.");
                continue;
            }
            if (!expected.TryAdd(ToKey(station), station))
                Add("C1_STATION_DUPLICATED", FemStationAuditIssueLevelC1.Invalid, station.StationId,
                    "Same target, position, side, limit state and profile were scheduled twice.");
        }

        Dictionary<Key, DesignCheckRequest> observed = new();
        foreach (DesignCheckRequest? request in actualRequests)
        {
            if (request?.Member is null || request.Effects is null || request.Profile is null ||
                request.Profile.Code is null || !ValidObservedKey(request))
            {
                Add("C1_SNAPSHOT_INVALID", FemStationAuditIssueLevelC1.Invalid, null,
                    "Captured request is null or lacks explicit station, case/combination and FEM provenance.");
                continue;
            }
            Key key = ToKey(request);
            if (!observed.TryAdd(key, request))
                Add("C1_SNAPSHOT_DUPLICATED", FemStationAuditIssueLevelC1.Invalid, null,
                    "Duplicate actual request for the same declared station and target.");
            if (!expected.ContainsKey(key))
                Add("C1_UNSCHEDULED_SNAPSHOT", FemStationAuditIssueLevelC1.Invalid, null,
                    "FEM request is not in the frozen declared station plan.");
        }

        if (issues.Any(x => x.Level is FemStationAuditIssueLevelC1.Invalid or FemStationAuditIssueLevelC1.Unsupported))
            return new FemConcomitantStationAuditResultC1(issues, 0);

        StructuralSolver2DService solver = new();
        Frame2DInternalForceSampler sampler = new();
        Dictionary<(DesignEffectTargetKind Kind, string Id), StructuralAnalysisResult> solved = new();
        int matched = 0;
        foreach ((Key key, FemDeclaredStationC1 station) in expected)
        {
            if (!observed.TryGetValue(key, out DesignCheckRequest? request))
            {
                Add("C1_REQUIRED_SNAPSHOT_MISSING", FemStationAuditIssueLevelC1.Incomplete,
                    station.StationId, "Required declared station has no FEM request.");
                continue;
            }
            if (request.Member != plan.Member ||
                request.LimitState != station.LimitState || request.Profile.Code != plan.Code ||
                !Same(request.Profile.ProfileId, station.ProfileId) ||
                !Same(request.Effects.MemberId, plan.Member.MemberId) ||
                request.Effects.Origin != DesignEffectOrigin.FemAnalysis ||
                !string.Equals(request.Effects.AnalysisId, "FEM:linear-static:first-order", StringComparison.Ordinal) ||
                !SameSet(request.RuleIds, station.RuleIds))
            {
                Add("C1_PROVENANCE_OR_RULE_SET_MISMATCH", FemStationAuditIssueLevelC1.Invalid,
                    station.StationId, "Member, code edition, rule set or signed-effect provenance is inconsistent.");
                continue;
            }

            if (request.Effects.AxialForceKn is not double n || !double.IsFinite(n) ||
                request.Effects.ShearForceYKn is not double v || !double.IsFinite(v) ||
                request.Effects.BendingMomentZKnm is not double m || !double.IsFinite(m))
            {
                Add("C1_FORCE_TRIPLE_INCOMPLETE", FemStationAuditIssueLevelC1.Incomplete,
                    station.StationId, "Frame2D station must contain all three signed concomitant N/V/M values.");
                continue;
            }

            var targetKey = (station.TargetKind, station.TargetId.ToUpperInvariant());
            try
            {
                if (!solved.TryGetValue(targetKey, out StructuralAnalysisResult? result))
                {
                    StructuralAnalysisTargetKind kind = station.TargetKind == DesignEffectTargetKind.LoadCase
                        ? StructuralAnalysisTargetKind.LoadCase : StructuralAnalysisTargetKind.LoadCombination;
                    result = solver.Analyze(new StructuralAnalysisRequest(model, station.TargetId, kind)).Result;
                    solved.Add(targetKey, result);
                }
                if (!Same(result.LoadCaseId, station.TargetId))
                {
                    Add("C1_ANALYSIS_TARGET_MISMATCH", FemStationAuditIssueLevelC1.Invalid,
                        station.StationId, "Solver result identity differs from the audited target.");
                    continue;
                }
                MemberInternalForceSample forces = sampler.EvaluateMemberSection(model, result,
                    plan.Member.MemberId, station.DistanceMetres, ToSide(station.SectionSide));
                if (!Approximately(n, forces.NormalForce) || !Approximately(v, forces.ShearForce) ||
                    !Approximately(m, forces.BendingMoment))
                    Add("C1_NONCONCOMITANT_FORCE", FemStationAuditIssueLevelC1.Invalid,
                        station.StationId, "Stored N/V/M are not the signed concomitant FEM values for this exact target/side/position.");
                else matched++;
            }
            catch (Exception ex)
            {
                Add("C1_FEM_STATION_UNAVAILABLE", FemStationAuditIssueLevelC1.Invalid,
                    station.StationId, "FEM could not substantiate this exact station: " + ex.GetType().Name);
            }
        }
        return new FemConcomitantStationAuditResultC1(issues, matched);
    }

    private static bool ValidStation(FemDeclaredStationC1 s, double length) =>
        !string.IsNullOrWhiteSpace(s.TargetId) && !string.IsNullOrWhiteSpace(s.ProfileId) &&
        Enum.IsDefined(s.TargetKind) && Enum.IsDefined(s.SectionSide) && Enum.IsDefined(s.LimitState) &&
        double.IsFinite(s.DistanceMetres) && s.DistanceMetres >= 0 &&
        double.IsFinite(length) && s.DistanceMetres <= length &&
        s.RuleIds is { Count: > 0 } &&
        s.RuleIds.All(id => !string.IsNullOrWhiteSpace(id)) &&
        s.RuleIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() == s.RuleIds.Count;

    private static bool ValidObservedKey(DesignCheckRequest request) =>
        request.Effects.TargetKind is { } kind && Enum.IsDefined(kind) &&
        request.Effects.SectionSide is { } side && Enum.IsDefined(side) &&
        Enum.IsDefined(request.LimitState) && double.IsFinite(request.Effects.PositionMetres) &&
        !string.IsNullOrWhiteSpace(request.Effects.CombinationId) &&
        !string.IsNullOrWhiteSpace(request.Profile.ProfileId);

    private static Key ToKey(FemDeclaredStationC1 s) => new(s.TargetKind,
        s.TargetId.ToUpperInvariant(), s.DistanceMetres, s.SectionSide,
        s.LimitState, s.ProfileId.ToUpperInvariant());

    private static Key ToKey(DesignCheckRequest r) => new(r.Effects.TargetKind!.Value,
        r.Effects.CombinationId.ToUpperInvariant(), r.Effects.PositionMetres,
        r.Effects.SectionSide!.Value, r.LimitState, r.Profile.ProfileId.ToUpperInvariant());

    private static SectionLimitM78 ToSide(DesignEffectSectionSide side) => side switch
    {
        DesignEffectSectionSide.Continuous => SectionLimitM78.Continuous,
        DesignEffectSectionSide.BeforePointLoad => SectionLimitM78.BeforePointLoad,
        DesignEffectSectionSide.AfterPointLoad => SectionLimitM78.AfterPointLoad,
        _ => throw new ArgumentOutOfRangeException(nameof(side))
    };

    private static bool Same(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool SameSet(IReadOnlyList<string>? a, IReadOnlyList<string> b) =>
        a is { Count: > 0 } && a.All(x => !string.IsNullOrWhiteSpace(x)) &&
        a.Count == b.Count && a.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(b);

    // Explicit unit-specific tolerance for first-order static kN/kNm reference data.
    private static bool Approximately(double a, double b) => double.IsFinite(a) && double.IsFinite(b) &&
        Math.Abs(a - b) <= 1e-8 + 1e-10 * Math.Max(Math.Abs(a), Math.Abs(b));
}
