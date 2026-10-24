using System.Globalization;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Idealized, isolated-member stability assumptions. They do NOT model real rotational
/// spring stiffness, global frame sway or the design of lateral restraints.
/// </summary>
public enum SteelEffectiveLengthMethodM91B
{
    /// <summary>Pinned ends prevented from translating laterally on the selected axis: k=1.</summary>
    PinnedPinned = 1,
    /// <summary>Perfectly fixed base and genuinely free head: k=2, isolated cantilever.</summary>
    FixedFreeCantilever = 2,
    /// <summary>Effective buckling length supplied by documented independent stability analysis.</summary>
    IndependentStabilityAnalysis = 3
}

/// <summary>
/// Location of a REAL lateral brace along a uniform member, measured from start [m].
/// Positive external assessment of both stiffness and resistance must be confirmed
/// for the requested buckling axis. This library does not perform that assessment.
/// </summary>
public sealed record SteelIntermediateBraceM91B(
    double PositionMetres,
    bool? RestrainsTranslationInAxisConfirmed,
    bool? StiffnessAdequateVerifiedExternally,
    bool? StrengthAdequateVerifiedExternally,
    string? ExternalVerificationEvidence);

/// <summary>
/// All lengths are in metres. Effective buckling lengths MUST NOT be inferred from
/// a planar FEM node, in-plane unbraced length or graphical bracing symbol.
/// PinnedPinned with braces is an explicit IDEAL PIN-SPAN idealization: each brace
/// must independently resist lateral translation in the selected axis.
/// </summary>
public sealed record SteelAxisRestraintPlanM91B(
    string Axis,
    double PhysicalLengthMetres,
    string? PhysicalLengthEvidence,
    SteelEffectiveLengthMethodM91B Method,
    bool? EndRestraintsVerifiedExternally,
    string? EndRestraintEvidence,
    IReadOnlyList<SteelIntermediateBraceM91B>? IntermediateBraces = null,
    double? IndependentEffectiveLengthMetres = null,
    string? IndependentStabilityEvidence = null);

public sealed record SteelMemberRestraintPlanM91B(
    string MemberId,
    SteelAxisRestraintPlanM91B StrongAxis,
    SteelAxisRestraintPlanM91B WeakAxis,
    bool? PrincipalAxisMappingConfirmed,
    string? PrincipalAxisMappingEvidence,
    bool? UniformPrismaticConcentricCompressionConfirmed,
    string? MemberIdealizationEvidence);

public enum SteelLengthResolutionStatusM91B
{
    Resolved,
    MissingData,
    InvalidInput,
    Unsupported
}

public sealed record SteelAxisLengthResolutionM91B(
    SteelLengthResolutionStatusM91B Status,
    double? EffectiveLengthMetres,
    double? EffectiveLengthFactor,
    IReadOnlyList<double> IdealizedSegmentsMetres,
    string Evidence,
    string Diagnostic);

public sealed record SteelMemberLengthResolutionM91B(
    SteelLengthResolutionStatusM91B Status,
    SteelAxisLengthResolutionM91B StrongAxis,
    SteelAxisLengthResolutionM91B WeakAxis,
    string Diagnostic);

public sealed record SteelPreparedBucklingAssignmentM91B(
    SteelLengthResolutionStatusM91B Status,
    SteelFlexuralBucklingAssignmentM91A? Assignment,
    string Diagnostic);

/// <summary>
/// M91-B: auditable input preparation for M91-A. Isolated Euler cases are assumptions,
/// NOT a stability solution for general frames. In particular no brace strength,
/// stiffness, global second-order behaviour or connection resistance is computed.
/// Supported idealizations deliberately exclude fixed/pinned and sway frames: these
/// require independently justified effective lengths, not guessed k factors.
/// </summary>
public static class SteelBucklingEffectiveLengthsM91B
{
    private const string Scope = "M91-B idealized buckling length ONLY; externally stated restraints/brace performance; " +
        "NOT a calculation of brace stiffness/strength, frame sway, N-M interaction or member compliance.";

    public static SteelAxisLengthResolutionM91B ResolveAxis(SteelAxisRestraintPlanM91B? plan)
    {
        static SteelAxisLengthResolutionM91B Reject(SteelLengthResolutionStatusM91B status, string reason) =>
            new(status, null, null, Array.Empty<double>(), string.Empty, reason);
        if (plan is null) return Reject(SteelLengthResolutionStatusM91B.MissingData, "Missing axis restraint plan.");
        if (plan.Axis is not ("y-y" or "z-z"))
            return Reject(SteelLengthResolutionStatusM91B.InvalidInput, "Use a declared principal buckling axis: y-y or z-z.");
        var length = plan.PhysicalLengthMetres;
        if (!PositiveFinite(length))
            return Reject(SteelLengthResolutionStatusM91B.InvalidInput, "Member physical length must be positive and finite.");
        if (string.IsNullOrWhiteSpace(plan.PhysicalLengthEvidence))
            return Reject(SteelLengthResolutionStatusM91B.MissingData, "Physical length evidence required.");
        if (plan.EndRestraintsVerifiedExternally != true || string.IsNullOrWhiteSpace(plan.EndRestraintEvidence))
            return Reject(SteelLengthResolutionStatusM91B.MissingData,
                "The idealized end-restraint model must be confirmed and documented externally.");

        var braces = plan.IntermediateBraces ?? Array.Empty<SteelIntermediateBraceM91B>();
        // Never allow a bracing declaration to be ignored silently by another model.
        if (plan.Method != SteelEffectiveLengthMethodM91B.PinnedPinned && braces.Count != 0)
            return Reject(SteelLengthResolutionStatusM91B.Unsupported,
                "Intermediate braces are only implemented for externally verified pinned-to-pinned lateral spans.");

        double effective;
        IReadOnlyList<double> spans;
        string basis;
        switch (plan.Method)
        {
            case SteelEffectiveLengthMethodM91B.PinnedPinned:
            {
                var points = new List<double> { 0 };
                // Each intermediate restraint must be independently engineered for THIS AXIS.
                foreach (var brace in braces)
                {
                    if (brace is null)
                        return Reject(SteelLengthResolutionStatusM91B.InvalidInput, "Null intermediate brace.");
                    if (!double.IsFinite(brace.PositionMetres) || brace.PositionMetres <= 0 ||
                        brace.PositionMetres >= length)
                        return Reject(SteelLengthResolutionStatusM91B.InvalidInput,
                            "Every brace must be strictly inside the member, with a finite position.");
                    if (brace.RestrainsTranslationInAxisConfirmed != true ||
                        brace.StiffnessAdequateVerifiedExternally != true ||
                        brace.StrengthAdequateVerifiedExternally != true ||
                        string.IsNullOrWhiteSpace(brace.ExternalVerificationEvidence))
                        return Reject(SteelLengthResolutionStatusM91B.MissingData,
                            "Every brace must have documented external axis-specific stiffness AND strength verification.");
                    points.Add(brace.PositionMetres);
                }
                points.Sort();
                points.Add(length);
                var intervals = new List<double>(points.Count - 1);
                var separationTolerance = Math.Max(1d, length) * 1e-10;
                for (var i = 1; i < points.Count; ++i)
                {
                    var segment = points[i] - points[i - 1];
                    if (!double.IsFinite(segment) || segment <= separationTolerance)
                        return Reject(SteelLengthResolutionStatusM91B.InvalidInput,
                            "Duplicate or nearly coincident brace/end positions are not accepted.");
                    intervals.Add(segment);
                }
                effective = intervals.Max();
                spans = intervals.ToArray();
                basis = braces.Count == 0
                    ? "ideal pinned-pinned isolated member, k=1, no intermediate lateral braces"
                    : "ideal externally verified PIN-PIN braced segments, k=1 per segment; " +
                      "longest interval governs; brace design NOT checked by software";
                break;
            }
            case SteelEffectiveLengthMethodM91B.FixedFreeCantilever:
                effective = 2d * length;
                spans = new[] { length };
                basis = "isolated ideal fixed-free cantilever with genuinely free head, k=2";
                break;
            case SteelEffectiveLengthMethodM91B.IndependentStabilityAnalysis:
                if (plan.IndependentEffectiveLengthMetres is not double independentlyEstablished ||
                    !PositiveFinite(independentlyEstablished))
                    return Reject(SteelLengthResolutionStatusM91B.MissingData,
                        "Independent analysis must provide a positive finite Lcr.");
                if (string.IsNullOrWhiteSpace(plan.IndependentStabilityEvidence))
                    return Reject(SteelLengthResolutionStatusM91B.MissingData,
                        "Independent stability calculation reference is mandatory.");
                effective = independentlyEstablished;
                spans = Array.Empty<double>();
                basis = "engineer-supplied independent buckling analysis; not recalculated here: " +
                        plan.IndependentStabilityEvidence;
                break;
            default:
                return Reject(SteelLengthResolutionStatusM91B.Unsupported,
                    "Unsupported effective length model; no implicit factor selected.");
        }
        if (!PositiveFinite(effective))
            return Reject(SteelLengthResolutionStatusM91B.InvalidInput, "Effective length overflow/invalidity.");
        var factor = effective / length;
        if (!PositiveFinite(factor))
            return Reject(SteelLengthResolutionStatusM91B.InvalidInput, "Effective length factor invalid.");

        static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
        var braceDescriptions = braces.Select(b => F(b.PositionMetres) + "m: " +
            b.ExternalVerificationEvidence).ToArray();
        var evidence = $"{Scope} | axis={plan.Axis}; L={F(length)}m " +
            $"physicalSource={plan.PhysicalLengthEvidence}; Lcr={F(effective)}m k={F(factor)}; " +
            $"method={plan.Method}; basis={basis}; " +
            $"endVerification={plan.EndRestraintEvidence}; " +
            $"braces=[{string.Join("; ", braceDescriptions)}]";
        return new SteelAxisLengthResolutionM91B(
            SteelLengthResolutionStatusM91B.Resolved, effective, factor,
            spans, evidence, "Idealized input assumptions resolved; no real brace verification performed.");
    }

    public static SteelMemberLengthResolutionM91B ResolveMember(SteelMemberRestraintPlanM91B? plan)
    {
        static SteelAxisLengthResolutionM91B Empty() => new(
            SteelLengthResolutionStatusM91B.MissingData, null, null,
            Array.Empty<double>(), string.Empty, "No resolved axis.");
        if (plan is null || string.IsNullOrWhiteSpace(plan.MemberId))
            return new(SteelLengthResolutionStatusM91B.MissingData, Empty(), Empty(), "Member and plan required.");
        var y = ResolveAxis(plan.StrongAxis);
        var z = ResolveAxis(plan.WeakAxis);
        if (y.Status != SteelLengthResolutionStatusM91B.Resolved)
            return new(y.Status, y, z, "Strong axis: " + y.Diagnostic);
        if (z.Status != SteelLengthResolutionStatusM91B.Resolved)
            return new(z.Status, y, z, "Weak axis: " + z.Diagnostic);
        if (plan.StrongAxis.Axis != "y-y" || plan.WeakAxis.Axis != "z-z")
            return new(SteelLengthResolutionStatusM91B.InvalidInput, y, z,
                "Strong axis must be y-y and weak axis z-z. No silent axis rotation.");
        var l1 = plan.StrongAxis.PhysicalLengthMetres;
        var l2 = plan.WeakAxis.PhysicalLengthMetres;
        if (Math.Abs(l1 - l2) > Math.Max(1d, Math.Max(l1, l2)) * 1e-9)
            return new(SteelLengthResolutionStatusM91B.InvalidInput, y, z,
                "Both axes must refer to the same physical member length.");
        if (plan.PrincipalAxisMappingConfirmed != true ||
            string.IsNullOrWhiteSpace(plan.PrincipalAxisMappingEvidence) ||
            plan.UniformPrismaticConcentricCompressionConfirmed != true ||
            string.IsNullOrWhiteSpace(plan.MemberIdealizationEvidence))
            return new(SteelLengthResolutionStatusM91B.MissingData, y, z,
                "Documented axis mapping, prismatic and uniform axial-compression assumptions required.");
        return new(SteelLengthResolutionStatusM91B.Resolved, y, z,
            "Both idealized buckling lengths resolved; real restraint capacity and global stability must be assessed separately.");
    }

    /// <summary>
    /// Copies RESOLVED lengths and their source into an M91-A assignment. All other
    /// M91-A prerequisites, class checks, material data, gamma_M1, N/V/M provenance
    /// and code edition are still checked independently by the original M91-A rule.
    /// </summary>
    public static SteelPreparedBucklingAssignmentM91B PrepareAssignment(
        SteelMemberRestraintPlanM91B? plan,
        SteelFlexuralBucklingAssignmentM91A? original)
    {
        if (original is null || plan is null ||
            !string.Equals(plan.MemberId, original.MemberId, StringComparison.OrdinalIgnoreCase))
            return new(SteelLengthResolutionStatusM91B.InvalidInput, null,
                "A matching original M91-A member assignment is required.");
        var solution = ResolveMember(plan);
        if (solution.Status != SteelLengthResolutionStatusM91B.Resolved)
            return new(solution.Status, null, solution.Diagnostic);
        return new(SteelLengthResolutionStatusM91B.Resolved,
            original with
            {
                EffectiveLengthStrongAxisMetres = solution.StrongAxis.EffectiveLengthMetres,
                StrongAxisLengthEvidence = solution.StrongAxis.Evidence,
                EffectiveLengthWeakAxisMetres = solution.WeakAxis.EffectiveLengthMetres,
                WeakAxisLengthEvidence = solution.WeakAxis.Evidence
            }, solution.Diagnostic);
    }

    private static bool PositiveFinite(double x) => double.IsFinite(x) && x > 0;
}
