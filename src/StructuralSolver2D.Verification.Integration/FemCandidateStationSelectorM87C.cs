using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Integration;

/// <summary>One explicitly identified case or manual combination. No envelope IDs.</summary>
public sealed record FemCandidateTargetM87C(StructuralAnalysisTargetKind Kind, string Id);

/// <summary>Additional physical member section. At a point load, caller must identify the side.</summary>
public sealed record FemExtraSectionM87C(double DistanceMetres, SectionLimitM78 Side = SectionLimitM78.Continuous);

/// <summary>
/// A screening candidate, NEVER a complete member code check. The N/V/M in Request
/// always correspond to one analyzed target and one section (including a side).
/// </summary>
public sealed record FemSectionCandidateM87C(DesignCheckRequest Request, IReadOnlyList<string> Reasons);

/// <summary>
/// Generates first-order elastic Frame2D screening stations from exact individual
/// N/V/M extrema (M78), member ends and both limits at concentrated member loads.
/// Optional explicit and uniform screening stations supplement that finite set.
/// The extrema of a nonlinear utilization/interacting code rule are NOT guaranteed
/// to lie at these stations. No member-wide pass/fail verdict is produced.
/// </summary>
public sealed class FemCandidateStationSelectorM87C
{
    private readonly StructuralSolver2DService solver = new();
    private readonly Frame2DExactExtremaFinderM78 extrema = new();
    private readonly FemStationVerificationBridgeM87B bridge = new();

    /// <param name="gridIntervals">0 disables supplemental grid; otherwise 1..2000
    /// intervals (including ends). Grid screening does not establish completeness.</param>
    public IReadOnlyList<FemSectionCandidateM87C> CaptureCandidates(
        StructuralModel model, DesignMemberContext member, DesignLimitState limitState,
        NormativeProfile profile, IReadOnlyList<string> ruleIds,
        FemCandidateTargetM87C target, IReadOnlyList<FemExtraSectionM87C>? extraSections = null,
        int gridIntervals = 0)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(ruleIds);
        ArgumentNullException.ThrowIfNull(target);
        if (gridIntervals < 0 || gridIntervals > 2000)
            throw new ArgumentOutOfRangeException(nameof(gridIntervals), "0 or 1..2000 required.");
        if (string.IsNullOrWhiteSpace(target.Id) || !Enum.IsDefined(target.Kind))
            throw new ArgumentException("An existing, explicitly typed load target is required.", nameof(target));

        // Reject case/combination ID collisions before solving, even if the FEM
        // public API would otherwise choose one arbitrarily.
        bool isCase = model.LoadCases.Any(c => string.Equals(c.Id, target.Id, StringComparison.OrdinalIgnoreCase));
        bool isCombination = model.LoadCombinations.Any(c => string.Equals(c.Id, target.Id, StringComparison.OrdinalIgnoreCase));
        if ((isCase && isCombination) ||
            (target.Kind == StructuralAnalysisTargetKind.LoadCase ? !isCase : !isCombination))
            throw new ArgumentException("Missing or ambiguous load target.", nameof(target));

        // M87-B validation and provenance construction is reused for every section;
        // the expensive global FEM calculation is executed ONCE for this target.
        StructuralAnalysisResult result = solver.Analyze(new StructuralAnalysisRequest(
            model, target.Id, target.Kind)).Result;
        ExactMemberInternalForceExtremaM78 exact = extrema.Find(model, result, member.MemberId);
        double length = model.Members
            .Where(m => string.Equals(m.Id, member.MemberId, StringComparison.OrdinalIgnoreCase))
            .Select(m => StructuralMember.GetLength(
                model.Nodes.Single(n => string.Equals(n.Id, m.StartNodeId, StringComparison.OrdinalIgnoreCase)),
                model.Nodes.Single(n => string.Equals(n.Id, m.EndNodeId, StringComparison.OrdinalIgnoreCase))))
            .Single();
        // Guard against mismatch BEFORE generating any candidate; full check is
        // also repeated by the M87-B bridge for every captured candidate.
        if (!double.IsFinite(length) || length <= 0 || !double.IsFinite(member.MemberLengthMetres) ||
            Math.Abs(length - member.MemberLengthMetres) > 1e-9 * Math.Max(1, length))
            throw new ArgumentException("Design/FEM member lengths differ.", nameof(member));

        HashSet<string> activeCases = target.Kind == StructuralAnalysisTargetKind.LoadCase
            ? new HashSet<string>(new[] { target.Id }, StringComparer.OrdinalIgnoreCase)
            : model.LoadCombinations.Single(c => string.Equals(c.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                .Terms.Select(t => t.LoadCaseId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        double[] jumps = model.Loads
            .Where(l => l.Type == StructuralLoadType.PointLoadOnMember && activeCases.Contains(l.LoadCaseId) &&
                string.Equals(l.TargetId, member.MemberId, StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Position.HasValue ? l.Position.Value * length
                : throw new ArgumentException("Point load has no normalized position."))
            .Distinct().OrderBy(x => x).ToArray();
        foreach (double x in jumps)
            if (!double.IsFinite(x) || x < 0 || x > length)
                throw new ArgumentException("Point load position is outside member.");

        // Key is the exact station AND the side. Do not discard one-sided values
        // at a concentrated force even when their moment values happen to match.
        var stations = new Dictionary<(double Distance, SectionLimitM78 Side), HashSet<string>>();
        const double epsilon = 2.2204460492503131e-16;
        double tol = 16 * epsilon * Math.Max(1, length);
        double Snap(double x)
        {
            foreach (double j in jumps)
                if (Math.Abs(x - j) <= tol) return j;
            if (Math.Abs(x) <= tol) return 0;
            if (Math.Abs(x - length) <= tol) return length;
            return x;
        }
        bool IsJump(double x) => jumps.Contains(x);
        void Add(double x, SectionLimitM78 side, string reason)
        {
            if (!double.IsFinite(x) || x < 0 || x > length || !Enum.IsDefined(side))
                throw new ArgumentOutOfRangeException(nameof(extraSections), "Invalid candidate section/side.");
            x = Snap(x);
            if (IsJump(x) && side == SectionLimitM78.Continuous)
                throw new ArgumentException("Point load at section requires an explicit side.", nameof(extraSections));
            if (!IsJump(x) && side != SectionLimitM78.Continuous)
                throw new ArgumentException("One-sided candidate without a point load.", nameof(extraSections));
            var key = (x, side);
            if (!stations.TryGetValue(key, out HashSet<string>? reasons))
                stations.Add(key, reasons = new HashSet<string>(StringComparer.Ordinal));
            reasons.Add(reason);
        }
        void AddAutomatically(double x, string reason)
        {
            x = Snap(x);
            if (IsJump(x))
            {
                // At a member endpoint, retain the physically internal side.
                if (x > 0) Add(x, SectionLimitM78.BeforePointLoad, reason);
                if (x < length) Add(x, SectionLimitM78.AfterPointLoad, reason);
            }
            else Add(x, SectionLimitM78.Continuous, reason);
        }

        AddAutomatically(0, "member-start");
        AddAutomatically(length, "member-end");
        foreach (double x in jumps) AddAutomatically(x, "member-point-load");
        foreach (ExactInternalForceExtremeM78 e in exact.Minima)
            AddAutomatically(e.Distance, $"{e.Quantity}:minimum");
        foreach (ExactInternalForceExtremeM78 e in exact.Maxima)
            AddAutomatically(e.Distance, $"{e.Quantity}:maximum");
        foreach (ExactInternalForceExtremeM78 e in exact.MaximumMagnitudes)
            AddAutomatically(e.Distance, $"{e.Quantity}:max-absolute");
        foreach (FemExtraSectionM87C section in extraSections ?? Array.Empty<FemExtraSectionM87C>())
        {
            if (section is null) throw new ArgumentException("Extra section cannot be null.", nameof(extraSections));
            Add(section.DistanceMetres, section.Side, "explicit-station");
        }
        if (gridIntervals > 0)
            for (int i = 0; i <= gridIntervals; i++)
                AddAutomatically(length * i / gridIntervals, "supplemental-grid");

        return stations.OrderBy(p => p.Key.Distance).ThenBy(p => p.Key.Side)
            .Select(p => new FemSectionCandidateM87C(
                bridge.CaptureUsingResult(model, member, limitState, profile, ruleIds,
                    new FemStationSelectionM87B(target.Kind, target.Id, p.Key.Distance, p.Key.Side), result),
                Array.AsReadOnly(p.Value.OrderBy(x => x, StringComparer.Ordinal).ToArray())))
            .ToArray();
    }

    /// <summary>Each target is solved independently; no combination mixing.</summary>
    public IReadOnlyList<FemSectionCandidateM87C> CaptureAcrossTargets(
        StructuralModel model, DesignMemberContext member, DesignLimitState limitState,
        NormativeProfile profile, IReadOnlyList<string> ruleIds,
        IReadOnlyList<FemCandidateTargetM87C> targets,
        IReadOnlyList<FemExtraSectionM87C>? extraSections = null, int gridIntervals = 0)
    {
        ArgumentNullException.ThrowIfNull(targets);
        if (targets.Count == 0) throw new ArgumentException("Select at least one case or combination.", nameof(targets));
        var seen = new HashSet<(StructuralAnalysisTargetKind, string)>();
        foreach (FemCandidateTargetM87C t in targets)
        {
            if (t is null || string.IsNullOrWhiteSpace(t.Id))
                throw new ArgumentException("Invalid target list.", nameof(targets));
            // OrdinalIgnoreCase semantics without changing the ID in snapshots.
            if (!seen.Add((t.Kind, t.Id.ToUpperInvariant())))
                throw new ArgumentException("Duplicate requested target.", nameof(targets));
        }
        return targets.SelectMany(t => CaptureCandidates(
            model, member, limitState, profile, ruleIds, t, extraSections, gridIntervals)).ToArray();
    }
}
