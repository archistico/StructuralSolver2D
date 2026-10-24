using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Frame2D;

/// <summary>
/// Calculates exact (up to floating-point roundoff) extrema of the piecewise-polynomial
/// first-order elastic Frame2D section-force equations. Does not depend on diagram sampling.
/// </summary>
/// <remarks>
/// Candidates consist of element ends, both one-sided limits at interior point-load jumps,
/// stationary N and V points, and zeros of V (= stationary M). No geometric nonlinearity,
/// dynamic section forces or distributed couples are represented by these equations.
/// </remarks>
public sealed class Frame2DExactExtremaFinderM78
{
    public ExactMemberInternalForceExtremaM78 Find(
        StructuralModel model, StructuralAnalysisResult result, string memberId)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(result);
        Frame2DInternalForceSampler.MemberForceProfile profile =
            Frame2DInternalForceSampler.PrepareMember(model, result, memberId);
        double length = profile.Length;
        if (!double.IsFinite(length) || length <= 0)
            throw new StructuralAnalysisException("M78_GEOMETRY: Member length must be positive and finite.");

        // Point loads split the beam into smooth, polynomial intervals. At an internal
        // load station the N/V diagram has two distinct one-sided limits.
        double[] boundaries = new[] { 0.0 }
            .Concat(profile.Discontinuities.Where(x => x > 0 && x < length))
            .Concat(new[] { length })
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        List<(MemberInternalForceSample Sample, SectionLimitM78 Side)> candidates = new();
        candidates.Add((profile.Evaluate(0, after: true), SectionLimitM78.Continuous));
        candidates.Add((profile.Evaluate(length, after: false), SectionLimitM78.Continuous));
        for (int i = 1; i < boundaries.Length - 1; i++)
        {
            double x = boundaries[i];
            candidates.Add((profile.Evaluate(x, after: false), SectionLimitM78.BeforePointLoad));
            candidates.Add((profile.Evaluate(x, after: true), SectionLimitM78.AfterPointLoad));
        }

        for (int i = 0; i < boundaries.Length - 1; i++)
        {
            double a = boundaries[i], b = boundaries[i + 1];
            if (b <= a) continue;
            double m = a + (b - a) / 2.0;
            // Values at the same smooth interval's endpoints: AFTER a, BEFORE b.
            MemberInternalForceSample first = profile.Evaluate(a, after: true);
            MemberInternalForceSample middle = profile.Evaluate(m);
            MemberInternalForceSample last = profile.Evaluate(b, after: false);
            AddStationaryRoots(first.NormalForce, middle.NormalForce, last.NormalForce, a, b, profile, candidates);
            AddStationaryRoots(first.ShearForce, middle.ShearForce, last.ShearForce, a, b, profile, candidates);
            // dM/dx = V under the established Frame2D sign convention.
            AddPolynomialRoots(first.ShearForce, middle.ShearForce, last.ShearForce, a, b, profile, candidates);
        }

        InternalForceQuantity[] quantities =
        {
            InternalForceQuantity.NormalForce,
            InternalForceQuantity.ShearForce,
            InternalForceQuantity.BendingMoment,
        };
        List<ExactInternalForceExtremeM78> minima = new();
        List<ExactInternalForceExtremeM78> maxima = new();
        List<ExactInternalForceExtremeM78> magnitudes = new();
        foreach (InternalForceQuantity quantity in quantities)
        {
            // Order of candidate insertion ensures stable ties: start, end, jumps, roots.
            var min = candidates.MinBy(c => Value(c.Sample, quantity));
            var max = candidates.MaxBy(c => Value(c.Sample, quantity));
            var abs = candidates.MaxBy(c => Math.Abs(Value(c.Sample, quantity)));
            minima.Add(ToExtreme(min, memberId, result.LoadCaseId, quantity));
            maxima.Add(ToExtreme(max, memberId, result.LoadCaseId, quantity));
            magnitudes.Add(ToExtreme(abs, memberId, result.LoadCaseId, quantity));
        }
        return new ExactMemberInternalForceExtremaM78(memberId, result.LoadCaseId, minima, maxima, magnitudes);
    }

    public IReadOnlyList<ExactMemberInternalForceExtremaM78> FindAll(
        StructuralModel model, StructuralAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.MemberEndForces.Select(e => Find(model, result, e.MemberId)).ToArray();
    }

    private static ExactInternalForceExtremeM78 ToExtreme(
        (MemberInternalForceSample Sample, SectionLimitM78 Side) candidate,
        string memberId, string resultId, InternalForceQuantity quantity) =>
        new(memberId, resultId, quantity, Value(candidate.Sample, quantity),
            candidate.Sample.Distance, candidate.Sample.Position, candidate.Side);

    private static double Value(MemberInternalForceSample sample, InternalForceQuantity quantity) =>
        quantity switch
        {
            InternalForceQuantity.NormalForce => sample.NormalForce,
            InternalForceQuantity.ShearForce => sample.ShearForce,
            InternalForceQuantity.BendingMoment => sample.BendingMoment,
            _ => throw new ArgumentOutOfRangeException(nameof(quantity)),
        };

    private static (double A, double B, double C) Quadratic(double first, double middle, double last)
    {
        // q(u) = A*u*u+B*u+C, u=(x-a)/(b-a), u in [0,1].
        double a = 2.0 * (first - 2.0 * middle + last);
        double b = 4.0 * middle - 3.0 * first - last;
        return (a, b, first);
    }

    private static void AddStationaryRoots(
        double first, double middle, double last, double start, double end,
        Frame2DInternalForceSampler.MemberForceProfile profile,
        List<(MemberInternalForceSample Sample, SectionLimitM78 Side)> candidates)
    {
        (double a, double b, _) = Quadratic(first, middle, last);
        double scale = Math.Max(1.0, Math.Max(Math.Abs(first), Math.Max(Math.Abs(middle), Math.Abs(last))));
        if (Math.Abs(a) <= 1e-13 * scale) return;
        AddCandidate(-b / (2.0 * a), start, end, profile, candidates);
    }

    private static void AddPolynomialRoots(
        double first, double middle, double last, double start, double end,
        Frame2DInternalForceSampler.MemberForceProfile profile,
        List<(MemberInternalForceSample Sample, SectionLimitM78 Side)> candidates)
    {
        (double a, double b, double c) = Quadratic(first, middle, last);
        double scale = Math.Max(1.0, Math.Max(Math.Abs(first), Math.Max(Math.Abs(middle), Math.Abs(last))));
        if (Math.Abs(a) <= 1e-13 * scale)
        {
            if (Math.Abs(b) > 1e-13 * scale)
                AddCandidate(-c / b, start, end, profile, candidates);
            return;
        }
        double discriminant = b * b - 4.0 * a * c;
        if (discriminant < 0) return;
        double root = Math.Sqrt(discriminant);
        // Stable quadratic formula, including the c=0 case.
        double q = -0.5 * (b + Math.CopySign(root, b));
        if (q == 0)
        {
            AddCandidate(-b / (2.0 * a), start, end, profile, candidates);
            return;
        }
        AddCandidate(q / a, start, end, profile, candidates);
        AddCandidate(c / q, start, end, profile, candidates);
    }

    private static void AddCandidate(
        double u, double start, double end,
        Frame2DInternalForceSampler.MemberForceProfile profile,
        List<(MemberInternalForceSample Sample, SectionLimitM78 Side)> candidates)
    {
        if (double.IsFinite(u) && u > 1e-12 && u < 1.0 - 1e-12)
            candidates.Add((profile.Evaluate(start + (end - start) * u), SectionLimitM78.Continuous));
    }
}
