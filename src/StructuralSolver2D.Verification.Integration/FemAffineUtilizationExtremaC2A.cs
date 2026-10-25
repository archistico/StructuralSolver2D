using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Verification.Integration;

/// <summary>
/// Purely mathematical dimensionless indicators; NEITHER is an EC3, EC5 or
/// NTC design equation. They cannot be used to issue an engineering pass.
/// </summary>
public enum AffineDiagnosticMetricC2A
{
    AbsoluteNormalizedForceSum,
    AbsoluteNormalizedAxialMomentProduct
}

public enum AffineUtilizationExtremaStatusC2A
{
    LocatedWithinAffineAssumptions,
    ScreeningIncomplete,
    InvalidInput,
    AnalysisFailed
}

/// <summary>Each scale must be positive; these are diagnostic denominators, NOT normative resistances.</summary>
public sealed record FemAffineUtilizationRequestC2A(
    string MemberId,
    StructuralAnalysisTargetKind TargetKind,
    string TargetId,
    AffineDiagnosticMetricC2A Metric,
    double AxialScaleKn,
    double ShearScaleKn,
    double BendingScaleKnm);

public sealed record FemAffineUtilizationCandidateC2A(
    double DistanceMetres,
    SectionLimitM78 SectionSide,
    double MetricValue,
    string Origin);

/// <summary>
/// LocatedWithinAffineAssumptions is ONLY the maximum of one diagnostic metric
/// over piecewise-affine first-order FEM forces for this ONE target/member.
/// It is not a code-check, member design pass or proof of all action patterns.
/// </summary>
public sealed class FemAffineUtilizationResultC2A
{
    public FemAffineUtilizationResultC2A(AffineUtilizationExtremaStatusC2A status,
        string reasonCode, string explanation, IReadOnlyList<FemAffineUtilizationCandidateC2A> candidates)
    {
        Status = status;
        ReasonCode = reasonCode;
        Explanation = explanation;
        Candidates = Array.AsReadOnly(candidates.ToArray());
    }

    public AffineUtilizationExtremaStatusC2A Status { get; }
    public string ReasonCode { get; }
    public string Explanation { get; }
    public IReadOnlyList<FemAffineUtilizationCandidateC2A> Candidates { get; }
    public FemAffineUtilizationCandidateC2A? Maximum =>
        Status == AffineUtilizationExtremaStatusC2A.LocatedWithinAffineAssumptions
            ? Candidates.OrderByDescending(x => x.MetricValue).FirstOrDefault() : null;
    public bool NormativeRuleEvaluated => false;
    public bool MemberAdequacyEstablished => false;
    public bool WholeStructureVerified => false;
    public bool CompleteActionSetEstablished => false;
}

/// <summary>
/// Finds the maximum of |(a0 + da*t)*(b0 + db*t)| for 0 &lt;= t &lt;= 1.
/// The derivative has at most one interior root. No grid is involved.
/// This algebraic helper is also tested against independent golden values.
/// </summary>
public static class AffineUtilizationMathC2A
{
    public static (double Fraction, double AbsoluteMaximum) MaximumAbsoluteProduct(
        double a0, double a1, double b0, double b1)
    {
        if (!double.IsFinite(a0) || !double.IsFinite(a1) ||
            !double.IsFinite(b0) || !double.IsFinite(b1))
            throw new ArgumentOutOfRangeException(nameof(a0), "Affine endpoints must be finite.");

        double deltaA = a1 - a0;
        double deltaB = b1 - b0;
        double qa = deltaA * deltaB;
        double qb = a0 * deltaB + b0 * deltaA;
        if (!double.IsFinite(deltaA) || !double.IsFinite(deltaB) ||
            !double.IsFinite(qa) || !double.IsFinite(qb))
            throw new OverflowException("Affine polynomial coefficients are not representable as finite doubles.");
        double bestT = 0;
        double best = Math.Abs(a0 * b0);
        void Consider(double t)
        {
            double a = a0 + deltaA * t;
            double b = b0 + deltaB * t;
            double value = Math.Abs(a * b);
            if (value > best)
            {
                best = value;
                bestT = t;
            }
        }

        Consider(1);
        if (qa != 0)
        {
            double stationary = -qb / (2 * qa);
            if (stationary > 0 && stationary < 1 && double.IsFinite(stationary))
                Consider(stationary);
        }

        if (!double.IsFinite(best))
            throw new OverflowException("Affine product overflowed; no finite extremum can be reported.");
        return (bestT, best);
    }
}

/// <summary>
/// M78/M87 postprocessing + a RESTRICTED mathematical maximum locator.
/// Supported: first-order linear Frame2D with no distributed member loads
/// (including expanded model self-weight) in selected load cases. Between
/// concentrated member loads N/V are constant and M is affine. The caller
/// receives ScreeningIncomplete for anything outside this explicitly checked
/// scope; no generic EC3/EC5/NTC utilization can be certified here.
/// </summary>
public static class FemAffineUtilizationExtremaC2A
{
    public static FemAffineUtilizationResultC2A Locate(
        StructuralModel model, FemAffineUtilizationRequestC2A request)
    {
        static FemAffineUtilizationResultC2A End(AffineUtilizationExtremaStatusC2A status,
            string code, string message) => new(status, code, message,
                Array.Empty<FemAffineUtilizationCandidateC2A>());

        if (model is null || request is null || string.IsNullOrWhiteSpace(request.MemberId) ||
            string.IsNullOrWhiteSpace(request.TargetId) || !Enum.IsDefined(request.Metric) ||
            !Enum.IsDefined(request.TargetKind) ||
            !ValidScale(request.AxialScaleKn) || !ValidScale(request.ShearScaleKn) ||
            !ValidScale(request.BendingScaleKnm))
            return End(AffineUtilizationExtremaStatusC2A.InvalidInput, "C2A_INPUT_INVALID",
                "An explicit member/target, supported metric and positive finite diagnostic scales are mandatory.");

        StructuralModelValidationResult validation = new StructuralModelValidator().Validate(model);
        if (!validation.IsValid)
            return End(AffineUtilizationExtremaStatusC2A.InvalidInput, "C2A_CORE_MODEL_INVALID",
                "The FEM model did not pass the existing Core validator.");

        StructuralMember[] matches = model.Members.Where(m => Same(m.Id, request.MemberId)).ToArray();
        if (matches.Length != 1)
            return End(AffineUtilizationExtremaStatusC2A.InvalidInput, "C2A_MEMBER_MISSING",
                "Exactly one member must match the requested ID.");
        StructuralMember member = matches[0];
        if (member.Type != MemberType.Frame2D)
            return End(AffineUtilizationExtremaStatusC2A.ScreeningIncomplete, "C2A_NONFRAME_UNSUPPORTED",
                "Truss and non-Frame2D types require distinct force laws and utilization rules.");

        bool loadCase = model.LoadCases.Any(l => Same(l.Id, request.TargetId));
        bool combination = model.LoadCombinations.Any(c => Same(c.Id, request.TargetId));
        if (loadCase == combination ||
            (request.TargetKind == StructuralAnalysisTargetKind.LoadCase ? !loadCase : !combination))
            return End(AffineUtilizationExtremaStatusC2A.InvalidInput, "C2A_TARGET_INVALID",
                "Missing, mistyped or ambiguous FEM case/combination identifier.");

        Dictionary<string, double> factors = new(StringComparer.OrdinalIgnoreCase);
        if (request.TargetKind == StructuralAnalysisTargetKind.LoadCase)
            factors.Add(request.TargetId, 1);
        else
            foreach (StructuralLoadCombinationTerm term in model.LoadCombinations.Single(c => Same(c.Id, request.TargetId)).Terms)
                factors.Add(term.LoadCaseId, term.Factor);

        // Absolutely no distributed load on THIS member may be represented as
        // piecewise-affine; model-wide self-weight touches every member.
        foreach (StructuralLoad load in model.Loads.Where(l => factors.ContainsKey(l.LoadCaseId)))
        {
            if (load.Type == StructuralLoadType.SelfWeight ||
                (Same(load.TargetId, member.Id) && load.Type is
                    StructuralLoadType.UniformDistributedLoad or StructuralLoadType.LinearDistributedLoad))
                return End(AffineUtilizationExtremaStatusC2A.ScreeningIncomplete,
                    "C2A_DISTRIBUTED_LOAD_UNSUPPORTED",
                    "Distributed/member self-weight loading gives higher-degree force diagrams; no affine maximum claim.");
        }

        StructuralNode startNode = model.Nodes.Single(n => Same(n.Id, member.StartNodeId));
        StructuralNode endNode = model.Nodes.Single(n => Same(n.Id, member.EndNodeId));
        double dx = endNode.X - startNode.X, dy = endNode.Y - startNode.Y;
        // Use the exact geometric arithmetic of the existing M78 sampler for
        // its endpoint coordinates; Core's overflow-safe hypot may round one
        // ulp differently on diagonal members and wrongly exceed that range.
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (!double.IsFinite(length) || length <= 0)
            return End(AffineUtilizationExtremaStatusC2A.InvalidInput, "C2A_LENGTH_INVALID",
                "The physical member length must be positive and finite.");

        double[] rawJumps = model.Loads.Where(l =>
                factors.ContainsKey(l.LoadCaseId) && Same(l.TargetId, member.Id) &&
                l.Type == StructuralLoadType.PointLoadOnMember)
            .Select(l => l.Position!.Value * length).OrderBy(x => x).ToArray();
        const double machineEpsilon = 2.2204460492503131e-16;
        double tolerance = 16 * machineEpsilon * Math.Max(1, length);
        for (int i = 1; i < rawJumps.Length; i++)
            if (Math.Abs(rawJumps[i] - rawJumps[i - 1]) <= tolerance && rawJumps[i] != rawJumps[i - 1])
                return End(AffineUtilizationExtremaStatusC2A.ScreeningIncomplete,
                    "C2A_CLOSE_JUMPS_UNRESOLVED", "Nearly coincident jumps cannot be separated reliably.");

        double[] jumps = rawJumps.Distinct().ToArray();
        double[] breakpoints = new[] { 0d, length }.Concat(jumps)
            .Distinct().OrderBy(x => x).ToArray();
        if (breakpoints.Length < 2 || breakpoints.Any(x => !double.IsFinite(x) || x < 0 || x > length))
            return End(AffineUtilizationExtremaStatusC2A.InvalidInput, "C2A_BREAKPOINTS_INVALID",
                "Point-load positions must define valid distances along the member.");

        try
        {
            StructuralAnalysisResult solved = new StructuralSolver2DService().Analyze(
                new StructuralAnalysisRequest(model, request.TargetId, request.TargetKind)).Result;
            if (!Same(solved.LoadCaseId, request.TargetId))
                return End(AffineUtilizationExtremaStatusC2A.AnalysisFailed, "C2A_RESULT_ID_MISMATCH",
                    "FEM solved a different case or combination.");
            Frame2DInternalForceSampler sampler = new();
            List<FemAffineUtilizationCandidateC2A> candidates = new();
            bool IsJump(double x) => jumps.Contains(x);
            SectionLimitM78 StartSide(double x) => IsJump(x)
                ? SectionLimitM78.AfterPointLoad : SectionLimitM78.Continuous;
            SectionLimitM78 EndSide(double x) => IsJump(x)
                ? SectionLimitM78.BeforePointLoad : SectionLimitM78.Continuous;

            double Metric(MemberInternalForceSample s) => request.Metric switch
            {
                AffineDiagnosticMetricC2A.AbsoluteNormalizedForceSum =>
                    Math.Abs(s.NormalForce / request.AxialScaleKn) +
                    Math.Abs(s.ShearForce / request.ShearScaleKn) +
                    Math.Abs(s.BendingMoment / request.BendingScaleKnm),
                AffineDiagnosticMetricC2A.AbsoluteNormalizedAxialMomentProduct =>
                    Math.Abs((s.NormalForce / request.AxialScaleKn) *
                             (s.BendingMoment / request.BendingScaleKnm)),
                _ => throw new NotSupportedException("Unknown mathematical diagnostic metric.")
            };

            void Add(double x, SectionLimitM78 side, double value, string origin)
            {
                if (!double.IsFinite(value) || value < 0)
                    throw new ArithmeticException("Metric is nonfinite or negative.");
                candidates.Add(new FemAffineUtilizationCandidateC2A(x, side, value, origin));
            }

            for (int i = 0; i < breakpoints.Length - 1; i++)
            {
                double left = breakpoints[i], right = breakpoints[i + 1];
                if (!(right > left))
                    throw new ArithmeticException("No positive-length affine interval.");

                SectionLimitM78 leftSide = StartSide(left), rightSide = EndSide(right);
                MemberInternalForceSample a = sampler.EvaluateMemberSection(model, solved,
                    member.Id, left, leftSide);
                MemberInternalForceSample b = sampler.EvaluateMemberSection(model, solved,
                    member.Id, right, rightSide);

                // An independent internal sample guards against accidental
                // misuse of a non-affine load profile even when metadata appears valid.
                MemberInternalForceSample midpoint = sampler.EvaluateMemberSection(model, solved,
                    member.Id, (left + right) / 2, SectionLimitM78.Continuous);
                if (!Approximately(midpoint.NormalForce, (a.NormalForce + b.NormalForce) / 2) ||
                    !Approximately(midpoint.ShearForce, (a.ShearForce + b.ShearForce) / 2) ||
                    !Approximately(midpoint.BendingMoment, (a.BendingMoment + b.BendingMoment) / 2))
                    return End(AffineUtilizationExtremaStatusC2A.ScreeningIncomplete,
                        "C2A_FORCE_NOT_AFFINE", "FEM forces failed the piecewise-affine check; refuse maximum certificate.");

                Add(left, leftSide, Metric(a), "interval-left");
                Add(right, rightSide, Metric(b), "interval-right");

                if (request.Metric == AffineDiagnosticMetricC2A.AbsoluteNormalizedAxialMomentProduct)
                {
                    (double t, double max) = AffineUtilizationMathC2A.MaximumAbsoluteProduct(
                        a.NormalForce / request.AxialScaleKn, b.NormalForce / request.AxialScaleKn,
                        a.BendingMoment / request.BendingScaleKnm, b.BendingMoment / request.BendingScaleKnm);
                    if (t > 0 && t < 1)
                    {
                        double x = left + (right - left) * t;
                        MemberInternalForceSample exact = sampler.EvaluateMemberSection(model, solved,
                            member.Id, x, SectionLimitM78.Continuous);
                        if (!Approximately(Metric(exact), max))
                            return End(AffineUtilizationExtremaStatusC2A.ScreeningIncomplete,
                                "C2A_STATIONARY_POINT_NOT_REPRODUCED",
                                "Analytic stationary metric was not reproduced by the exact FEM sampler.");
                        Add(x, SectionLimitM78.Continuous, max, "analytic-product-stationary");
                    }
                }
            }

            return new FemAffineUtilizationResultC2A(
                AffineUtilizationExtremaStatusC2A.LocatedWithinAffineAssumptions,
                "C2A_AFFINE_DIAGNOSTIC_LOCATED",
                "Maximum of the named synthetic metric only, under the validated affine FEM loading scope.",
                candidates);
        }
        catch (Exception ex)
        {
            return End(AffineUtilizationExtremaStatusC2A.AnalysisFailed, "C2A_ANALYSIS_UNAVAILABLE",
                "FEM or numerical metric evaluation failed (" + ex.GetType().Name + ").");
        }
    }

    private static bool ValidScale(double x) => double.IsFinite(x) && x > 0;
    private static bool Same(string? a, string? b) => !string.IsNullOrWhiteSpace(a) &&
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static bool Approximately(double a, double b) => double.IsFinite(a) && double.IsFinite(b) &&
        Math.Abs(a - b) <= 1e-8 + 1e-10 * Math.Max(Math.Abs(a), Math.Abs(b));
}
