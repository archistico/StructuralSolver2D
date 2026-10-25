using System.Globalization;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// F1.1-B: independent closed-form cantilever loads, nodal/member free-body
/// equilibrium, and independent Hooke checks on ideal bilateral X-braces.
/// Only the cantilever checks below provide absolute analytic N/V/M values;
/// the other checks are physics consistency checks, NOT independent member
/// golden values or normative design verifications.
/// </summary>
public sealed class G8BMemberForceF1BTests
{
    private static readonly string[] ModelFiles =
    {
        "g8b-a_leggero-controventato.json",
        "g8b-b_riferimento-controventato.json",
        "g8b-b_riferimento-senza-controventi.json",
        "g8b-c_robusto-controventato.json"
    };

    private static readonly string[] Combinations =
    {
        "ULS_GRAVITY", "ULS_WIND_POS", "ULS_WIND_NEG", "SLS_RARE"
    };

    private const double ForceAbsKn = 0.005;
    private const double MomentAbsKnm = 0.005;
    private const double RelativeTolerance = 0.0001;

    public static IEnumerable<object[]> G8BCases() =>
        from model in ModelFiles
        from combination in Combinations
        select new object[] { model, combination };

    [Theory]
    [MemberData(nameof(G8BCases))]
    public void AllSixteenScenarios_BothBalconiesMatchIndependentCantileverEquations(
        string filename, string combinationId)
    {
        StructuralModel model = LoadModel(filename);
        StructuralAnalysisResult result = new PlaneStructureAnalyzer().AnalyzeCombination(model, combinationId);
        var sampler = new Frame2DInternalForceSampler();
        List<string> mismatches = new();

        // Frozen G8B input values: G_BALC = -24 kN/m; Q_BALC = -16 kN/m.
        // The analytical target is deliberately NOT recomputed from the two
        // corresponding StructuralLoad.Value properties under test.
        (double permanentFactor, double variableFactor, double selfFactor) =
            FrozenBalconyCombinationFactors(combinationId);

        foreach (string memberId in new[] { "BAL_L", "BAL_R" })
        {
            StructuralMember member = model.Members.Single(m => m.Id == memberId);
            StructuralNode start = model.Nodes.Single(n => n.Id == member.StartNodeId);
            StructuralNode end = model.Nodes.Single(n => n.Id == member.EndNodeId);
            double length = Math.Abs(end.X - start.X);
            Assert.True(length > 0 && start.Y == end.Y,
                $"{memberId}: reference assumes a horizontal cantilever");
            Check(mismatches, $"{memberId}.L [m]", 1.2, length, 0.0000001);

            StructuralMaterial material = model.Materials.Single(m => m.Id == member.MaterialId);
            StructuralSection section = model.Sections.Single(s => s.Id == member.SectionId);
            double weightPerMetre = material.UnitWeight!.Value * section.Area;
            double downward = 24.0 * permanentFactor + 16.0 * variableFactor
                + weightPerMetre * selfFactor;
            double rootShear = downward * length;
            double rootMoment = downward * length * length / 2.0;

            MemberEndForceResult forces = result.MemberEndForces.Single(f => f.MemberId == memberId);
            bool freeAtStart = memberId == "BAL_L";

            // BAL_L is oriented free tip -> column; BAL_R column -> free tip.
            // Both are loaded downwards; end-force signs follow the local
            // Frame2D convention. At each free tip N=V=M=0.
            Check(mismatches, $"{memberId}.start axial [kN]", 0, forces.StartAxial);
            Check(mismatches, $"{memberId}.end axial [kN]", 0, forces.EndAxial);
            Check(mismatches, $"{memberId}.tip shear [kN]", 0,
                freeAtStart ? forces.StartShear : forces.EndShear);
            Check(mismatches, $"{memberId}.tip moment [kNm]", 0,
                freeAtStart ? forces.StartMoment : forces.EndMoment, MomentAbsKnm);
            Check(mismatches, $"{memberId}.root shear [kN]", rootShear,
                freeAtStart ? forces.EndShear : forces.StartShear);
            Check(mismatches, $"{memberId}.root moment [kNm]",
                freeAtStart ? -rootMoment : rootMoment,
                freeAtStart ? forces.EndMoment : forces.StartMoment, MomentAbsKnm);

            // Independent section cuts at 0, 1/4, 1/2, 3/4, L. These values
            // depend only on the frozen balcony loads and beam self-weight.
            foreach (double ratio in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
            {
                double x = ratio * length;
                double distanceFromFreeTip = freeAtStart ? x : length - x;
                MemberInternalForceSample sectionForce = sampler.EvaluateMemberSection(
                    model, result, memberId, x);

                double signedShear = (freeAtStart ? -1.0 : 1.0)
                    * downward * distanceFromFreeTip;
                double bendingMoment = -downward * distanceFromFreeTip
                    * distanceFromFreeTip / 2.0;
                Check(mismatches, $"{memberId}.N({ratio:R}L)", 0, sectionForce.NormalForce);
                Check(mismatches, $"{memberId}.V({ratio:R}L)", signedShear,
                    sectionForce.ShearForce);
                Check(mismatches, $"{memberId}.M({ratio:R}L)", bendingMoment,
                    sectionForce.BendingMoment, MomentAbsKnm);
            }
        }

        AssertNoMismatches(filename, combinationId, mismatches);
    }

    [Theory]
    [MemberData(nameof(G8BCases))]
    public void AllSixteenScenarios_AllFrameMembersBalanceUniformLoadsAndSectionCuts(
        string filename, string combinationId)
    {
        StructuralModel model = LoadModel(filename);
        StructuralAnalysisResult result = new PlaneStructureAnalyzer().AnalyzeCombination(model, combinationId);
        var sampler = new Frame2DInternalForceSampler();
        IReadOnlyDictionary<string, double> factors = LoadFactors(model, combinationId);
        List<string> mismatches = new();
        Assert.Equal(18, model.Members.Count(m => m.Type == MemberType.Frame2D));

        foreach (StructuralMember member in model.Members.Where(m => m.Type == MemberType.Frame2D))
        {
            (double length, double cosine, double sine) = Geometry(model, member);
            double globalYLineLoad = -SelfWeightLineLoadMagnitude(model, member)
                * SelfWeightFactor(model, factors);

            foreach (StructuralLoad load in model.Loads.Where(l => l.TargetId == member.Id))
            {
                Assert.Equal(StructuralLoadType.UniformDistributedLoad, load.Type);
                Assert.Equal(StructuralLoadDirection.GlobalY, load.Direction);
                if (factors.TryGetValue(load.LoadCaseId, out double factor))
                    globalYLineLoad += load.Value * factor;
            }

            double qx = sine * globalYLineLoad;
            double qy = cosine * globalYLineLoad;
            MemberEndForceResult f = result.MemberEndForces.Single(e => e.MemberId == member.Id);

            // The three equations are independent statics of the member, not
            // a comparison against separately computed FEM numerical golden.
            Check(mismatches, $"{member.Id}.sum Fx [kN]", 0,
                f.StartAxial + f.EndAxial + qx * length);
            Check(mismatches, $"{member.Id}.sum Fy [kN]", 0,
                f.StartShear + f.EndShear + qy * length);
            Check(mismatches, $"{member.Id}.sum Mz [kNm]", 0,
                f.StartMoment + f.EndMoment + f.EndShear * length
                + qy * length * length / 2.0, MomentAbsKnm);

            // Independently integrate constant qx/qy at exact stations;
            // catches failures in section sampling, including rotated rafters.
            foreach (double ratio in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
            {
                double x = ratio * length;
                MemberInternalForceSample actual = sampler.EvaluateMemberSection(
                    model, result, member.Id, x);
                Check(mismatches, $"{member.Id}.N({ratio:R}L) [kN]",
                    -f.StartAxial - qx * x, actual.NormalForce);
                Check(mismatches, $"{member.Id}.V({ratio:R}L) [kN]",
                    f.StartShear + qy * x, actual.ShearForce);
                Check(mismatches, $"{member.Id}.M({ratio:R}L) [kNm]",
                    -f.StartMoment + f.StartShear * x + qy * x * x / 2.0,
                    actual.BendingMoment, MomentAbsKnm);
            }
        }

        AssertNoMismatches(filename, combinationId, mismatches);
    }

    [Theory]
    [MemberData(nameof(G8BCases))]
    public void AllSixteenScenarios_UnrestrainedJointsBalanceNodalForcesAndTrussWeight(
        string filename, string combinationId)
    {
        StructuralModel model = LoadModel(filename);
        StructuralAnalysisResult result = new PlaneStructureAnalyzer().AnalyzeCombination(model, combinationId);
        IReadOnlyDictionary<string, double> factors = LoadFactors(model, combinationId);
        double selfFactor = SelfWeightFactor(model, factors);
        List<string> mismatches = new();
        Assert.Equal(11, model.Nodes.Count(n => !model.Supports.Any(s => s.NodeId == n.Id)));

        foreach (StructuralNode node in model.Nodes.Where(n =>
            !model.Supports.Any(s => s.NodeId == n.Id)))
        {
            double appliedFx = 0;
            double appliedFy = 0;
            foreach (StructuralLoad load in model.Loads.Where(l =>
                l.TargetId == node.Id && l.Type == StructuralLoadType.NodalForce))
            {
                if (!factors.TryGetValue(load.LoadCaseId, out double factor))
                    continue;
                if (load.Direction == StructuralLoadDirection.GlobalX)
                    appliedFx += load.Value * factor;
                else if (load.Direction == StructuralLoadDirection.GlobalY)
                    appliedFy += load.Value * factor;
                else
                    throw new InvalidOperationException("G8B unexpected nodal force direction");
            }

            // The truss's own weight was modelled as equal nodal loads,
            // not as transverse distributed bending on Truss2D members.
            foreach (StructuralMember truss in model.Members.Where(m =>
                m.Type == MemberType.Truss2D &&
                (m.StartNodeId == node.Id || m.EndNodeId == node.Id)))
            {
                double length = Geometry(model, truss).Length;
                appliedFy -= SelfWeightLineLoadMagnitude(model, truss)
                    * selfFactor * length / 2.0;
            }

            double endFx = 0;
            double endFy = 0;
            double endMz = 0;
            foreach (StructuralMember member in model.Members.Where(m =>
                m.StartNodeId == node.Id || m.EndNodeId == node.Id))
            {
                var geometry = Geometry(model, member);
                double cosine = geometry.Cosine;
                double sine = geometry.Sine;
                MemberEndForceResult f = result.MemberEndForces.Single(e => e.MemberId == member.Id);
                bool atStart = member.StartNodeId == node.Id;
                double localFx = atStart ? f.StartAxial : f.EndAxial;
                double localFy = atStart ? f.StartShear : f.EndShear;
                endFx += cosine * localFx - sine * localFy;
                endFy += sine * localFx + cosine * localFy;
                endMz += atStart ? f.StartMoment : f.EndMoment;
            }

            Check(mismatches, $"{node.Id}.joint Fx [kN]", appliedFx, endFx);
            Check(mismatches, $"{node.Id}.joint Fy [kN]", appliedFy, endFy);
            Check(mismatches, $"{node.Id}.joint Mz [kNm]", 0, endMz, MomentAbsKnm);
        }

        AssertNoMismatches(filename, combinationId, mismatches);
    }

    [Theory]
    [MemberData(nameof(G8BCases))]
    public void AllSixteenScenarios_DiagonalTrussesArePureAxialAndObeyHooke(
        string filename, string combinationId)
    {
        StructuralModel model = LoadModel(filename);
        StructuralAnalysisResult result = new PlaneStructureAnalyzer().AnalyzeCombination(model, combinationId);
        var sampler = new Frame2DInternalForceSampler();
        List<string> mismatches = new();
        Assert.Equal(filename.Contains("senza-controventi", StringComparison.Ordinal) ? 0 : 4,
            model.Members.Count(m => m.Type == MemberType.Truss2D));

        foreach (StructuralMember truss in model.Members.Where(m => m.Type == MemberType.Truss2D))
        {
            (double length, double cosine, double sine) = Geometry(model, truss);
            StructuralMaterial material = model.Materials.Single(m => m.Id == truss.MaterialId);
            StructuralSection section = model.Sections.Single(s => s.Id == truss.SectionId);
            NodalDisplacementResult start = result.GetDisplacement(truss.StartNodeId);
            NodalDisplacementResult end = result.GetDisplacement(truss.EndNodeId);
            double delta = cosine * (end.Ux - start.Ux) + sine * (end.Uy - start.Uy);
            double normal = material.ElasticModulus * section.Area * delta / length;
            MemberEndForceResult f = result.MemberEndForces.Single(e => e.MemberId == truss.Id);

            Check(mismatches, $"{truss.Id}.axial start [kN]", -normal, f.StartAxial);
            Check(mismatches, $"{truss.Id}.axial end [kN]", normal, f.EndAxial);
            Check(mismatches, $"{truss.Id}.transverse start [kN]", 0, f.StartShear);
            Check(mismatches, $"{truss.Id}.transverse end [kN]", 0, f.EndShear);
            Check(mismatches, $"{truss.Id}.moment start [kNm]", 0, f.StartMoment, MomentAbsKnm);
            Check(mismatches, $"{truss.Id}.moment end [kNm]", 0, f.EndMoment, MomentAbsKnm);

            foreach (double ratio in new[] { 0.0, 0.5, 1.0 })
            {
                MemberInternalForceSample sample = sampler.EvaluateMemberSection(
                    model, result, truss.Id, ratio * length);
                Check(mismatches, $"{truss.Id}.N({ratio:R}L) [kN]", normal,
                    sample.NormalForce);
                Check(mismatches, $"{truss.Id}.V({ratio:R}L) [kN]", 0,
                    sample.ShearForce);
                Check(mismatches, $"{truss.Id}.M({ratio:R}L) [kNm]", 0,
                    sample.BendingMoment, MomentAbsKnm);
            }
        }

        AssertNoMismatches(filename, combinationId, mismatches);
    }

    [Fact]
    public void PerturbedBalconyPermanentLoad_IsRejectedByFrozenAnalyticTarget()
    {
        const string file = "g8b-b_riferimento-controventato.json";
        const string combination = "ULS_GRAVITY";
        StructuralModel model = LoadModel(file);
        StructuralLoad load = model.Loads.Single(l => l.Id == "G_BAL_L");
        model.Loads[model.Loads.IndexOf(load)] = load with { Value = load.Value - 5.0 };

        StructuralAnalysisResult altered = new PlaneStructureAnalyzer().AnalyzeCombination(model, combination);
        MemberEndForceResult actual = altered.MemberEndForces.Single(f => f.MemberId == "BAL_L");
        StructuralMember member = model.Members.Single(m => m.Id == "BAL_L");
        StructuralMaterial material = model.Materials.Single(m => m.Id == member.MaterialId);
        StructuralSection section = model.Sections.Single(s => s.Id == member.SectionId);
        (double permanent, double variable, double self) = FrozenBalconyCombinationFactors(combination);
        double originalExpectedRootShear = 1.2 * (24.0 * permanent + 16.0 * variable
            + material.UnitWeight!.Value * section.Area * self);

        Assert.True(Math.Abs(actual.EndShear - originalExpectedRootShear) > 1.0,
            "Changing the left balcony load must not silently change its frozen analytic target.");
    }

    private static (double Permanent, double Variable, double Self) FrozenBalconyCombinationFactors(
        string combinationId) => combinationId switch
        {
            "ULS_GRAVITY" => (1.35, 1.50, 1.35),
            "ULS_WIND_POS" => (1.00, 0.70, 1.00),
            "ULS_WIND_NEG" => (1.00, 0.70, 1.00),
            "SLS_RARE" => (1.00, 1.00, 1.00),
            _ => throw new ArgumentOutOfRangeException(nameof(combinationId))
        };

    private static IReadOnlyDictionary<string, double> LoadFactors(
        StructuralModel model, string combinationId) =>
        model.LoadCombinations.Single(c => c.Id == combinationId).Terms
            .ToDictionary(t => t.LoadCaseId, t => t.Factor, StringComparer.Ordinal);

    private static double SelfWeightFactor(
        StructuralModel model, IReadOnlyDictionary<string, double> factors) =>
        model.Loads.Where(l => l.Type == StructuralLoadType.SelfWeight)
            .Sum(l => l.Value * (factors.TryGetValue(l.LoadCaseId, out double factor) ? factor : 0));

    private static double SelfWeightLineLoadMagnitude(StructuralModel model, StructuralMember member)
    {
        StructuralMaterial material = model.Materials.Single(m => m.Id == member.MaterialId);
        StructuralSection section = model.Sections.Single(s => s.Id == member.SectionId);
        return material.UnitWeight!.Value * section.Area;
    }

    private static (double Length, double Cosine, double Sine) Geometry(
        StructuralModel model, StructuralMember member)
    {
        StructuralNode start = model.Nodes.Single(n => n.Id == member.StartNodeId);
        StructuralNode end = model.Nodes.Single(n => n.Id == member.EndNodeId);
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        return (length, dx / length, dy / length);
    }

    private static void Check(List<string> mismatches, string name,
        double expected, double actual, double absoluteTolerance = ForceAbsKn)
    {
        double tolerance = absoluteTolerance + RelativeTolerance * Math.Abs(expected);
        if (double.IsFinite(expected) && double.IsFinite(actual)
            && Math.Abs(actual - expected) <= tolerance)
            return;

        mismatches.Add(string.Format(CultureInfo.InvariantCulture,
            "{0}: expected={1:R}, actual={2:R}, tolerance={3:R}",
            name, expected, actual, tolerance));
    }

    private static void AssertNoMismatches(string model, string combination,
        IReadOnlyList<string> mismatches) =>
        Assert.True(mismatches.Count == 0,
            $"G8B F1B {model}/{combination}: {mismatches.Count} mismatches:\n"
            + string.Join("\n", mismatches));

    private static StructuralModel LoadModel(string file) =>
        StructuralModelJsonReader.ReadValidated(Path.Combine(AppContext.BaseDirectory,
            "G8B", "Examples", file)).Model;
}
