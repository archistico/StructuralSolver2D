using StructuralSolver2D.Core.Model.Loads;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Frame2D;

/// <summary>
/// Samples local internal forces N, V and M along 2D frame members after an analysis.
/// This class is a post-processing component: it does not solve the structure again.
/// </summary>
public sealed class Frame2DInternalForceSampler
{
    /// <summary>
    /// Samples the internal-force diagram of one member.
    /// </summary>
    /// <param name="model">Structural model used for the analysis.</param>
    /// <param name="analysisResult">Analysis result for the requested load case.</param>
    /// <param name="memberId">Member identifier to sample.</param>
    /// <param name="sampleCount">Number of samples. Must be at least 2.</param>
    /// <returns>Sampled member internal-force diagram.</returns>
    public MemberInternalForceDiagram SampleMember(
        StructuralModel model,
        StructuralAnalysisResult analysisResult,
        string memberId,
        int sampleCount = 21)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(analysisResult);

        if (string.IsNullOrWhiteSpace(memberId))
        {
            throw new ArgumentException("Member id cannot be empty.", nameof(memberId));
        }

        if (sampleCount < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount), sampleCount, "At least two samples are required.");
        }

        MemberForceProfile profile = PrepareMember(model, analysisResult, memberId);
        List<MemberInternalForceSample> samples = new(sampleCount);
        for (int index = 0; index < sampleCount; index++)
        {
            double x = profile.Length * index / (sampleCount - 1);
            samples.Add(profile.Evaluate(x));
        }

        return new MemberInternalForceDiagram(memberId, profile.Length, samples);
    }

    /// <summary>
    /// M87-B: evaluates all concomitant signed N/V/M in one exact member station.
    /// Uses the existing first-order piecewise-polynomial equations (not a graphic sample).
    /// A member point-load jump requires an explicit one-sided limit; an ambiguous
    /// "Continuous" request at such a jump is rejected. Units: m, kN, kNm.
    /// </summary>
    public MemberInternalForceSample EvaluateMemberSection(
        StructuralModel model, StructuralAnalysisResult analysisResult, string memberId,
        double distanceMetres, SectionLimitM78 side = SectionLimitM78.Continuous)
    {
        if (!Enum.IsDefined(side))
            throw new ArgumentOutOfRangeException(nameof(side));
        MemberForceProfile profile = PrepareMember(model, analysisResult, memberId);
        if (!double.IsFinite(distanceMetres) || distanceMetres < 0 || distanceMetres > profile.Length)
            throw new ArgumentOutOfRangeException(nameof(distanceMetres));

        // Snap only within floating-point representation error, not a user-facing
        // engineering tolerance: never silently move a station by a finite distance.
        double coordinate = distanceMetres;
        foreach (double jump in profile.Discontinuities)
        {
            if (Math.Abs(jump - coordinate) <= 16 * 2.2204460492503131e-16 * Math.Max(1, profile.Length))
            {
                coordinate = jump;
                break;
            }
        }
        bool isJump = profile.Discontinuities.Contains(coordinate);
        if (isJump && side == SectionLimitM78.Continuous)
            throw new ArgumentException("Point load at section: choose BeforePointLoad or AfterPointLoad.", nameof(side));
        if (!isJump && side != SectionLimitM78.Continuous)
            throw new ArgumentException("One-sided section requested where no concentrated member load exists.", nameof(side));
        return profile.Evaluate(coordinate, after: side != SectionLimitM78.BeforePointLoad);
    }

    /// <summary>
    /// Prepares the same exact piecewise-polynomial section force equations used by the
    /// standard diagram sampler. Point-load jumps are evaluated on their chosen side.
    /// </summary>
    internal static MemberForceProfile PrepareMember(
        StructuralModel model, StructuralAnalysisResult result, string memberId)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(memberId))
            throw new ArgumentException("Member id cannot be empty.", nameof(memberId));

        model = SelfWeightStaticLoadExpander.Expand(model);
        StructuralMember member = model.Members.FirstOrDefault(m =>
            string.Equals(m.Id, memberId, StringComparison.OrdinalIgnoreCase))
            ?? throw new StructuralAnalysisException($"Member '{memberId}' was not found in the structural model.");
        MemberEndForceResult endForces = result.MemberEndForces.FirstOrDefault(e =>
            string.Equals(e.MemberId, memberId, StringComparison.OrdinalIgnoreCase))
            ?? throw new StructuralAnalysisException($"Member end forces for member '{memberId}' were not found in the analysis result.");
        StructuralNode startNode = model.Nodes.First(n => string.Equals(n.Id, member.StartNodeId, StringComparison.OrdinalIgnoreCase));
        StructuralNode endNode = model.Nodes.First(n => string.Equals(n.Id, member.EndNodeId, StringComparison.OrdinalIgnoreCase));
        MemberGeometry geometry = MemberGeometry.FromNodes(startNode, endNode);
        IReadOnlyDictionary<string, double> factors = BuildLoadCaseFactors(model, result.LoadCaseId);
        (double qx, double qy) = SumUniformLoadsInLocalCoordinates(model, factors, memberId, geometry);
        return new MemberForceProfile(memberId, geometry.Length, endForces, qx, qy,
            GetLinearDistributedLoadsInLocalCoordinates(model, factors, memberId, geometry),
            GetPointLoadsInLocalCoordinates(model, factors, memberId, geometry));
    }

    internal sealed class MemberForceProfile
    {
        private readonly string memberId;
        private readonly MemberEndForceResult endForces;
        private readonly double qx;
        private readonly double qy;
        private readonly IReadOnlyList<LinearlyVaryingLocalLoad> linearLoads;
        private readonly IReadOnlyList<ConcentratedLocalLoad> pointLoads;

        internal MemberForceProfile(string memberId, double length, MemberEndForceResult endForces,
            double qx, double qy, IReadOnlyList<LinearlyVaryingLocalLoad> linearLoads,
            IReadOnlyList<ConcentratedLocalLoad> pointLoads)
        {
            this.memberId = memberId;
            Length = length;
            this.endForces = endForces;
            this.qx = qx;
            this.qy = qy;
            this.linearLoads = linearLoads;
            this.pointLoads = pointLoads;
        }

        public double Length { get; }
        public IReadOnlyList<double> Discontinuities => pointLoads.Select(p => p.DistanceFromStart).Distinct().OrderBy(x => x).ToList();

        /// <summary>after=true takes the right-hand limit at a concentrated force.</summary>
        public MemberInternalForceSample Evaluate(double x, bool after = true)
        {
            if (!double.IsFinite(x) || x < 0 || x > Length)
                throw new ArgumentOutOfRangeException(nameof(x), x, "Section position must lie within the member.");

            double normal = -endForces.StartAxial - qx * x;
            double shear = endForces.StartShear + qy * x;
            double moment = -endForces.StartMoment + endForces.StartShear * x + qy * x * x / 2.0;
            foreach (LinearlyVaryingLocalLoad load in linearLoads)
            {
                double slopeX = (load.EndLocalXValue - load.StartLocalXValue) / Length;
                double slopeY = (load.EndLocalYValue - load.StartLocalYValue) / Length;
                normal -= load.StartLocalXValue * x + slopeX * x * x / 2.0;
                shear += load.StartLocalYValue * x + slopeY * x * x / 2.0;
                moment += load.StartLocalYValue * x * x / 2.0 + slopeY * x * x * x / 6.0;
            }
            foreach (ConcentratedLocalLoad load in pointLoads)
            {
                if (load.DistanceFromStart < x || (after && load.DistanceFromStart == x))
                {
                    normal -= load.LocalXValue;
                    shear += load.LocalYValue;
                    moment += load.LocalYValue * (x - load.DistanceFromStart);
                }
            }
            return new MemberInternalForceSample(memberId, x / Length, x, normal, shear, moment);
        }
    }

    /// <summary>
    /// Samples all members available in the analysis result.
    /// </summary>
    /// <param name="model">Structural model used for the analysis.</param>
    /// <param name="analysisResult">Analysis result for the requested load case.</param>
    /// <param name="sampleCount">Number of samples per member. Must be at least 2.</param>
    /// <returns>Sampled diagrams ordered as member end forces appear in the analysis result.</returns>
    public IReadOnlyList<MemberInternalForceDiagram> SampleAllMembers(
        StructuralModel model,
        StructuralAnalysisResult analysisResult,
        int sampleCount = 21) =>
        analysisResult.MemberEndForces
            .Select(memberEndForce => SampleMember(model, analysisResult, memberEndForce.MemberId, sampleCount))
            .ToList();

    private static (double LocalX, double LocalY) SumUniformLoadsInLocalCoordinates(
        StructuralModel model,
        IReadOnlyDictionary<string, double> loadCaseFactors,
        string memberId,
        MemberGeometry geometry)
    {
        double localX = 0;
        double localY = 0;

        foreach (StructuralLoad load in model.Loads.Where(load =>
            loadCaseFactors.ContainsKey(load.LoadCaseId) &&
            string.Equals(load.TargetId, memberId, StringComparison.OrdinalIgnoreCase) &&
            load.Type == StructuralLoadType.UniformDistributedLoad))
        {
            double factor = loadCaseFactors[load.LoadCaseId];
            (double loadLocalX, double loadLocalY) = ResolveUniformLoadInLocalCoordinates(load, geometry);
            localX += loadLocalX * factor;
            localY += loadLocalY * factor;
        }

        return (localX, localY);
    }


    private static IReadOnlyList<LinearlyVaryingLocalLoad> GetLinearDistributedLoadsInLocalCoordinates(
        StructuralModel model,
        IReadOnlyDictionary<string, double> loadCaseFactors,
        string memberId,
        MemberGeometry geometry) =>
        model.Loads
            .Where(load =>
                loadCaseFactors.ContainsKey(load.LoadCaseId) &&
                string.Equals(load.TargetId, memberId, StringComparison.OrdinalIgnoreCase) &&
                load.Type == StructuralLoadType.LinearDistributedLoad)
            .Select(load =>
            {
                if (!load.EndValue.HasValue)
                {
                    throw new StructuralAnalysisException($"Linear distributed load '{load.Id}' has no end value.");
                }

                double factor = loadCaseFactors[load.LoadCaseId];
                (double startLocalX, double startLocalY) = ResolveLoadValueInLocalCoordinates(load.Direction, load.Value, geometry, "linear distributed");
                (double endLocalX, double endLocalY) = ResolveLoadValueInLocalCoordinates(load.Direction, load.EndValue.Value, geometry, "linear distributed");

                return new LinearlyVaryingLocalLoad(startLocalX * factor, endLocalX * factor, startLocalY * factor, endLocalY * factor);
            })
            .ToList();


    private static IReadOnlyList<ConcentratedLocalLoad> GetPointLoadsInLocalCoordinates(
        StructuralModel model,
        IReadOnlyDictionary<string, double> loadCaseFactors,
        string memberId,
        MemberGeometry geometry) =>
        model.Loads
            .Where(load =>
                loadCaseFactors.ContainsKey(load.LoadCaseId) &&
                string.Equals(load.TargetId, memberId, StringComparison.OrdinalIgnoreCase) &&
                load.Type == StructuralLoadType.PointLoadOnMember)
            .Select(load =>
            {
                if (!load.Position.HasValue)
                {
                    throw new StructuralAnalysisException($"Point load '{load.Id}' has no normalized position.");
                }

                double factor = loadCaseFactors[load.LoadCaseId];
                (double localX, double localY) = ResolveConcentratedLoadInLocalCoordinates(load, geometry);
                return new ConcentratedLocalLoad(load.Position.Value * geometry.Length, localX * factor, localY * factor);
            })
            .OrderBy(load => load.DistanceFromStart)
            .ToList();

    private static IReadOnlyDictionary<string, double> BuildLoadCaseFactors(StructuralModel model, string resultId)
    {
        StructuralLoadCombination? combination = model.LoadCombinations.FirstOrDefault(
            combination => string.Equals(combination.Id, resultId, StringComparison.OrdinalIgnoreCase));

        if (combination is not null)
        {
            return combination.Terms.ToDictionary(
                term => term.LoadCaseId,
                term => term.Factor,
                StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            [resultId] = 1.0,
        };
    }

    private static (double LocalX, double LocalY) ResolveConcentratedLoadInLocalCoordinates(
        StructuralLoad load,
        MemberGeometry geometry) =>
        ResolveLoadValueInLocalCoordinates(load.Direction, load.Value, geometry, "point");

    private static (double LocalX, double LocalY) ResolveUniformLoadInLocalCoordinates(
        StructuralLoad load,
        MemberGeometry geometry) =>
        ResolveLoadValueInLocalCoordinates(load.Direction, load.Value, geometry, "uniform");

    private static (double LocalX, double LocalY) ResolveLoadValueInLocalCoordinates(
        StructuralLoadDirection direction,
        double value,
        MemberGeometry geometry,
        string loadKind) =>
        direction switch
        {
            StructuralLoadDirection.LocalX => (value, 0),
            StructuralLoadDirection.LocalY => (0, value),
            StructuralLoadDirection.GlobalX => (geometry.Cosine * value, -geometry.Sine * value),
            StructuralLoadDirection.GlobalY => (geometry.Sine * value, geometry.Cosine * value),
            _ => throw new StructuralAnalysisException($"Unsupported {loadKind} load direction '{direction}'.")
        };

    internal sealed record ConcentratedLocalLoad(double DistanceFromStart, double LocalXValue, double LocalYValue);

    internal sealed record LinearlyVaryingLocalLoad(
        double StartLocalXValue,
        double EndLocalXValue,
        double StartLocalYValue,
        double EndLocalYValue);

    private sealed record MemberGeometry(double Length, double Cosine, double Sine)
    {
        public static MemberGeometry FromNodes(StructuralNode startNode, StructuralNode endNode)
        {
            double dx = endNode.X - startNode.X;
            double dy = endNode.Y - startNode.Y;
            double length = Math.Sqrt((dx * dx) + (dy * dy));

            return new MemberGeometry(length, dx / length, dy / length);
        }
    }
}
