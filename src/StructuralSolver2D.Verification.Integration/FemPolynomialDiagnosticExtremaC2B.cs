using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Verification.Integration;

public enum PolynomialDiagnosticExtremaStatusC2B
{
    LocatedWithinPolynomialAssumptions,
    ScreeningIncomplete,
    InvalidInput,
    AnalysisFailed
}

/// <summary>
/// Mathematical maximum of ONE named synthetic diagnostic on ONE Frame2D member
/// and ONE first-order load target. The status is never a design-code pass.
/// </summary>
public sealed class FemPolynomialDiagnosticResultC2B
{
    public FemPolynomialDiagnosticResultC2B(PolynomialDiagnosticExtremaStatusC2B status,
        string reasonCode, string explanation, IEnumerable<FemAffineUtilizationCandidateC2A> candidates)
    {
        Status = status;
        ReasonCode = reasonCode;
        Explanation = explanation;
        Candidates = Array.AsReadOnly(candidates.ToArray());
    }

    public PolynomialDiagnosticExtremaStatusC2B Status { get; }
    public string ReasonCode { get; }
    public string Explanation { get; }
    public IReadOnlyList<FemAffineUtilizationCandidateC2A> Candidates { get; }
    public FemAffineUtilizationCandidateC2A? Maximum =>
        Status == PolynomialDiagnosticExtremaStatusC2B.LocatedWithinPolynomialAssumptions
            ? Candidates.OrderByDescending(c => c.MetricValue).FirstOrDefault() : null;
    public bool NormativeRuleEvaluated => false;
    public bool MemberAdequacyEstablished => false;
    public bool WholeStructureVerified => false;
    public bool CompleteActionSetEstablished => false;
    public bool NumericalIntervalCertificateEstablished => false;
}

/// <summary>
/// Isolates the real stationary points of polynomials of degree at most five
/// on [0,1], using recursively isolated derivative roots and monotone brackets.
/// No plotting grid is used. IEEE-754 double is NOT interval arithmetic.
/// </summary>
public static class PolynomialDiagnosticMathC2B
{
    private const double UnitRoundoff = 2.2204460492503131e-16;

    public static double Evaluate(IReadOnlyList<double> coefficients, double t)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        if (coefficients.Count == 0 || coefficients.Any(c => !double.IsFinite(c)) || !double.IsFinite(t))
            throw new ArgumentException("Finite, nonempty polynomial coefficients and an evaluation point are required.");
        double value = 0;
        for (int i = coefficients.Count - 1; i >= 0; i--)
            value = value * t + coefficients[i];
        if (!double.IsFinite(value))
            throw new ArithmeticException("Nonfinite polynomial evaluation.");
        return value;
    }

    public static double[] Multiply(IReadOnlyList<double> a, IReadOnlyList<double> b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Count == 0 || b.Count == 0 || a.Any(x => !double.IsFinite(x)) ||
            b.Any(x => !double.IsFinite(x)))
            throw new ArgumentException("Finite, nonempty polynomials are required.");
        double[] result = new double[a.Count + b.Count - 1];
        for (int i = 0; i < a.Count; i++)
            for (int j = 0; j < b.Count; j++)
                result[i + j] += a[i] * b[j];
        if (result.Any(x => !double.IsFinite(x)))
            throw new ArithmeticException("Polynomial product overflowed.");
        return result;
    }

    public static IReadOnlyList<double> StationaryPointsInUnitInterval(IReadOnlyList<double> polynomial)
    {
        ArgumentNullException.ThrowIfNull(polynomial);
        if (polynomial.Count == 0 || polynomial.Count > 6 || polynomial.Any(c => !double.IsFinite(c)))
            throw new ArgumentException("Only finite polynomials of degree <= 5 are supported.");
        return RealRootsInUnitInterval(Derivative(polynomial));
    }

    private static double[] Derivative(IReadOnlyList<double> polynomial)
    {
        if (polynomial.Count < 2)
            return new[] { 0d };
        return Enumerable.Range(1, polynomial.Count - 1)
            .Select(i => i * polynomial[i]).ToArray();
    }

    public static IReadOnlyList<double> RealRootsInUnitInterval(IReadOnlyList<double> polynomial)
    {
        ArgumentNullException.ThrowIfNull(polynomial);
        if (polynomial.Count == 0 || polynomial.Count > 6 || polynomial.Any(c => !double.IsFinite(c)))
            throw new ArgumentException("Finite degree <= 5 polynomial required.");

        double magnitude = polynomial.Max(c => Math.Abs(c));
        if (magnitude == 0)
            return Array.Empty<double>(); // Identically zero: no isolated roots to report.
        double[] normalized = polynomial.Select(c => c / magnitude).ToArray();
        int degree = normalized.Length - 1;
        while (degree > 0 && Math.Abs(normalized[degree]) <= 64 * UnitRoundoff)
            degree--;
        if (degree == 0)
            return Array.Empty<double>();
        normalized = normalized.Take(degree + 1).ToArray();

        if (degree == 1)
        {
            double root = -normalized[0] / normalized[1];
            return double.IsFinite(root) && root >= 0 && root <= 1
                ? new[] { root } : Array.Empty<double>();
        }

        IReadOnlyList<double> critical = RealRootsInUnitInterval(Derivative(normalized));
        double[] partitions = new[] { 0d }.Concat(critical).Concat(new[] { 1d })
            .Distinct().OrderBy(x => x).ToArray();
        List<double> roots = new();
        // A multiple root may touch zero without changing sign. It is then
        // also a derivative root and must be examined explicitly.
        foreach (double x in partitions)
            if (Math.Abs(Evaluate(normalized, x)) <= 1e-12)
                roots.Add(x);

        for (int i = 0; i + 1 < partitions.Length; i++)
        {
            double lo = partitions[i], hi = partitions[i + 1];
            double flo = Evaluate(normalized, lo), fhi = Evaluate(normalized, hi);
            if ((flo < 0 && fhi < 0) || (flo > 0 && fhi > 0) ||
                flo == 0 || fhi == 0)
                continue;
            // Derivative-root partitioning guarantees monotonicity here in
            // exact arithmetic; bisection resolves a sign-changing root.
            for (int step = 0; step < 90; step++)
            {
                double mid = lo + (hi - lo) * .5;
                if (mid == lo || mid == hi)
                    break;
                double fm = Evaluate(normalized, mid);
                if (fm == 0)
                {
                    lo = hi = mid;
                    break;
                }
                if ((fm < 0) == (flo < 0))
                {
                    lo = mid;
                    flo = fm;
                }
                else
                    hi = mid;
            }
            roots.Add(lo + (hi - lo) * .5);
        }
        return roots.Where(x => double.IsFinite(x) && x >= 0 && x <= 1)
            .OrderBy(x => x)
            .Aggregate(new List<double>(), (list, root) =>
            {
                if (list.Count == 0 || Math.Abs(root - list[^1]) > 1e-10)
                    list.Add(root);
                return list;
            });
    }
}

/// <summary>
/// C2B: piecewise polynomial extension of C2A for member uniform/linearly
/// varying loads and self-weight, using the existing exact Frame2D sampler.
/// Within a load-free-jump interval N,V have degree <=2, M degree <=3.
/// Four exact section queries reconstruct each polynomial, with further
/// independent section controls. Numeric stationarity is NOT a certified
/// interval upper bound; code-check and structural adequacy remain unknown.
/// </summary>
public static class FemPolynomialDiagnosticExtremaC2B
{
    public static FemPolynomialDiagnosticResultC2B Locate(StructuralModel model,
        FemAffineUtilizationRequestC2A request)
    {
        static FemPolynomialDiagnosticResultC2B End(PolynomialDiagnosticExtremaStatusC2B status,
            string code, string why) => new(status, code, why,
                Array.Empty<FemAffineUtilizationCandidateC2A>());

        if (model is null || request is null || string.IsNullOrWhiteSpace(request.MemberId) ||
            string.IsNullOrWhiteSpace(request.TargetId) || !Enum.IsDefined(request.Metric) ||
            !Enum.IsDefined(request.TargetKind) ||
            !ValidScale(request.AxialScaleKn) || !ValidScale(request.ShearScaleKn) ||
            !ValidScale(request.BendingScaleKnm))
            return End(PolynomialDiagnosticExtremaStatusC2B.InvalidInput, "C2B_INPUT_INVALID",
                "Explicit Frame2D member, load target, metric and positive finite diagnostic scales required.");

        if (!new StructuralModelValidator().Validate(model).IsValid)
            return End(PolynomialDiagnosticExtremaStatusC2B.InvalidInput, "C2B_CORE_MODEL_INVALID",
                "Model failed the Core validator.");
        StructuralMember[] matching = model.Members.Where(m => Same(m.Id, request.MemberId)).ToArray();
        if (matching.Length != 1)
            return End(PolynomialDiagnosticExtremaStatusC2B.InvalidInput, "C2B_MEMBER_INVALID",
                "Exactly one requested member must exist.");
        StructuralMember member = matching[0];
        if (member.Type != MemberType.Frame2D)
            return End(PolynomialDiagnosticExtremaStatusC2B.ScreeningIncomplete, "C2B_NONFRAME_UNSUPPORTED",
                "Only linear Frame2D internal-force polynomials are in scope.");

        bool isCase = model.LoadCases.Any(c => Same(c.Id, request.TargetId));
        bool isCombination = model.LoadCombinations.Any(c => Same(c.Id, request.TargetId));
        if (isCase == isCombination ||
            (request.TargetKind == StructuralAnalysisTargetKind.LoadCase ? !isCase : !isCombination))
            return End(PolynomialDiagnosticExtremaStatusC2B.InvalidInput, "C2B_TARGET_INVALID",
                "Target case/combination must be uniquely identifiable and match its declared kind.");

        HashSet<string> activeCases = new(StringComparer.OrdinalIgnoreCase);
        if (isCase)
            activeCases.Add(request.TargetId);
        else
            foreach (StructuralLoadCombinationTerm term in model.LoadCombinations
                .Single(c => Same(c.Id, request.TargetId)).Terms)
                activeCases.Add(term.LoadCaseId);

        StructuralNode aNode = model.Nodes.Single(n => Same(n.Id, member.StartNodeId));
        StructuralNode bNode = model.Nodes.Single(n => Same(n.Id, member.EndNodeId));
        double dx = bNode.X - aNode.X, dy = bNode.Y - aNode.Y;
        // Exactly mirrors the current Frame2DInternalForceSampler geometry.
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (!double.IsFinite(length) || !(length > 0))
            return End(PolynomialDiagnosticExtremaStatusC2B.InvalidInput, "C2B_LENGTH_INVALID",
                "Member length must be finite and positive.");

        double[] jumpDistances = model.Loads.Where(load => activeCases.Contains(load.LoadCaseId) &&
                Same(load.TargetId, member.Id) && load.Type == StructuralLoadType.PointLoadOnMember)
            .Select(load => load.Position!.Value * length).OrderBy(x => x).ToArray();
        const double eps = 2.2204460492503131e-16;
        double coordTolerance = 16 * eps * Math.Max(1, length);
        for (int i = 1; i < jumpDistances.Length; i++)
            if (jumpDistances[i] != jumpDistances[i - 1] &&
                Math.Abs(jumpDistances[i] - jumpDistances[i - 1]) <= coordTolerance)
                return End(PolynomialDiagnosticExtremaStatusC2B.ScreeningIncomplete,
                    "C2B_NEAR_JUMPS_UNRESOLVED", "Nearly coincident concentrated forces cannot be separated robustly.");

        double[] jumps = jumpDistances.Distinct().ToArray();
        double[] partitions = new[] { 0d, length }.Concat(jumps)
            .Distinct().OrderBy(x => x).ToArray();
        if (partitions.Length < 2 || partitions.Any(x => !double.IsFinite(x) || x < 0 || x > length))
            return End(PolynomialDiagnosticExtremaStatusC2B.InvalidInput, "C2B_PARTITIONS_INVALID",
                "Concentrated forces must lie inside the member.");

        try
        {
            StructuralAnalysisResult solved = new StructuralSolver2DService().Analyze(
                new StructuralAnalysisRequest(model, request.TargetId, request.TargetKind)).Result;
            if (!Same(solved.LoadCaseId, request.TargetId))
                return End(PolynomialDiagnosticExtremaStatusC2B.AnalysisFailed,
                    "C2B_RESULT_MISMATCH", "FEM result target differs from requested target.");

            Frame2DInternalForceSampler sampler = new();
            List<FemAffineUtilizationCandidateC2A> candidates = new();
            bool Jump(double x) => jumps.Contains(x);
            SectionLimitM78 LeftSide(double x) => Jump(x)
                ? SectionLimitM78.AfterPointLoad : SectionLimitM78.Continuous;
            SectionLimitM78 RightSide(double x) => Jump(x)
                ? SectionLimitM78.BeforePointLoad : SectionLimitM78.Continuous;

            double[] Normalize(MemberInternalForceSample section) => new[]
            {
                section.NormalForce / request.AxialScaleKn,
                section.ShearForce / request.ShearScaleKn,
                section.BendingMoment / request.BendingScaleKnm
            };
            double Metric(double n, double v, double m) => request.Metric switch
            {
                AffineDiagnosticMetricC2A.AbsoluteNormalizedForceSum => Math.Abs(n) + Math.Abs(v) + Math.Abs(m),
                AffineDiagnosticMetricC2A.AbsoluteNormalizedAxialMomentProduct => Math.Abs(n * m),
                _ => throw new NotSupportedException("Unknown diagnostic metric.")
            };

            void Add(double x, SectionLimitM78 side, double value, string origin)
            {
                if (!double.IsFinite(value) || value < 0)
                    throw new ArithmeticException("Nonfinite diagnostic value.");
                candidates.Add(new FemAffineUtilizationCandidateC2A(x, side, value, origin));
            }

            for (int interval = 0; interval + 1 < partitions.Length; interval++)
            {
                double left = partitions[interval], right = partitions[interval + 1];
                double span = right - left;
                if (!(span > coordTolerance * 2))
                    return End(PolynomialDiagnosticExtremaStatusC2B.ScreeningIncomplete,
                        "C2B_INTERVAL_TOO_NARROW", "Cannot reconstruct a stable polynomial on a near-zero interval.");

                double[] tSamples = { 0, 1d / 3, 2d / 3, 1 };
                double[][] readings = new double[4][];
                for (int sample = 0; sample < 4; sample++)
                {
                    double t = tSamples[sample];
                    double position = sample == 0 ? left : sample == 3 ? right : left + span * t;
                    var side = sample == 0 ? LeftSide(left) :
                        sample == 3 ? RightSide(right) : SectionLimitM78.Continuous;
                    readings[sample] = Normalize(sampler.EvaluateMemberSection(model, solved,
                        member.Id, position, side));
                }

                double[] n = CubicFromThirds(readings.Select(r => r[0]).ToArray());
                double[] v = CubicFromThirds(readings.Select(r => r[1]).ToArray());
                double[] m = CubicFromThirds(readings.Select(r => r[2]).ToArray());

                // There is no cubic term in first-order N or V for uniform
                // and linearly varying distributed loads. Remove only the
                // interpolation roundoff term; the independent FEM checks
                // below must confirm this physical degree restriction.
                n[3] = 0;
                v[3] = 0;

                // The sampler's first-order load laws prove degree <= 3 for M
                // and <= 2 for N/V. Extra exact-section checks guard against
                // wrong target, jumps or ill-conditioned reconstruction.
                foreach (double t in new[] { .19d, .41d, .73d })
                {
                    var exact = Normalize(sampler.EvaluateMemberSection(model, solved,
                        member.Id, left + span * t));
                    if (!Near(PolynomialDiagnosticMathC2B.Evaluate(n, t), exact[0]) ||
                        !Near(PolynomialDiagnosticMathC2B.Evaluate(v, t), exact[1]) ||
                        !Near(PolynomialDiagnosticMathC2B.Evaluate(m, t), exact[2]))
                        return End(PolynomialDiagnosticExtremaStatusC2B.ScreeningIncomplete,
                            "C2B_POLYNOMIAL_NOT_REPRODUCED",
                            "Independent exact-section checks failed the reconstructed force polynomials.");
                }

                var leftSide = LeftSide(left);
                var rightSide = RightSide(right);
                Add(left, leftSide, Metric(readings[0][0], readings[0][1], readings[0][2]), "interval-left");
                Add(right, rightSide, Metric(readings[3][0], readings[3][1], readings[3][2]), "interval-right");

                IEnumerable<double> stationary;
                if (request.Metric == AffineDiagnosticMetricC2A.AbsoluteNormalizedAxialMomentProduct)
                {
                    stationary = PolynomialDiagnosticMathC2B.StationaryPointsInUnitInterval(
                        PolynomialDiagnosticMathC2B.Multiply(n.Take(3).ToArray(), m));
                }
                else
                {
                    List<double> all = new();
                    foreach (int sn in new[] { -1, 1 })
                    foreach (int sv in new[] { -1, 1 })
                    foreach (int sm in new[] { -1, 1 })
                    {
                        double[] signed = new double[4];
                        for (int k = 0; k < 4; k++)
                            signed[k] = sn * n[k] + sv * v[k] + sm * m[k];
                        all.AddRange(PolynomialDiagnosticMathC2B.StationaryPointsInUnitInterval(signed));
                    }
                    stationary = all.Distinct();
                }

                foreach (double t in stationary.Where(t => t > 0 && t < 1))
                {
                    double position = left + span * t;
                    var sampled = Normalize(sampler.EvaluateMemberSection(model, solved,
                        member.Id, position, SectionLimitM78.Continuous));
                    double predicted = Metric(PolynomialDiagnosticMathC2B.Evaluate(n, t),
                        PolynomialDiagnosticMathC2B.Evaluate(v, t), PolynomialDiagnosticMathC2B.Evaluate(m, t));
                    double actual = Metric(sampled[0], sampled[1], sampled[2]);
                    if (!Near(predicted, actual))
                        return End(PolynomialDiagnosticExtremaStatusC2B.ScreeningIncomplete,
                            "C2B_STATIONARY_NOT_REPRODUCED",
                            "FEM exact station does not reproduce the polynomial stationary value.");
                    Add(position, SectionLimitM78.Continuous, actual, "polynomial-stationary");
                }
            }

            return new FemPolynomialDiagnosticResultC2B(
                PolynomialDiagnosticExtremaStatusC2B.LocatedWithinPolynomialAssumptions,
                "C2B_POLYNOMIAL_DIAGNOSTIC_LOCATED",
                "Max of one synthetic metric and one FEM target/member, not a certified numeric enclosure or design-code check.",
                candidates);
        }
        catch (Exception ex)
        {
            return End(PolynomialDiagnosticExtremaStatusC2B.AnalysisFailed,
                "C2B_EVALUATION_FAILED", "FEM or polynomial evaluation unavailable (" + ex.GetType().Name + ").");
        }
    }

    private static double[] CubicFromThirds(double[] values)
    {
        double d1 = values[1] - values[0];
        double d2 = values[2] - 2 * values[1] + values[0];
        double d3 = values[3] - 3 * values[2] + 3 * values[1] - values[0];
        double[] coefficients = { values[0], 3 * d1 - 1.5 * d2 + d3,
            4.5 * (d2 - d3), 4.5 * d3 };
        if (coefficients.Any(x => !double.IsFinite(x)))
            throw new ArithmeticException("Polynomial coefficients overflowed.");
        return coefficients;
    }

    private static bool Near(double a, double b) => double.IsFinite(a) && double.IsFinite(b) &&
        Math.Abs(a - b) <= 1e-8 + 1e-9 * Math.Max(Math.Abs(a), Math.Abs(b));
    private static bool ValidScale(double value) => double.IsFinite(value) && value > 0;
    private static bool Same(string? a, string? b) => !string.IsNullOrWhiteSpace(a) &&
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
