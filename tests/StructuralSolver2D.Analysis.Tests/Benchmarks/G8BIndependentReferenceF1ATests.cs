using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// F1.1-A: compares all sixteen G8B first-order analyses to the pre-existing,
/// frozen independent numerical reference. This code NEVER invokes or re-runs
/// its historical Python producer. The N/V/M checks below use simple separate
/// hand-equilibrium/Hooke identities at demonstrable lower-column stations;
/// they are NOT full-member independent golden N/V/M data for all members.
/// </summary>
public sealed class G8BIndependentReferenceF1ATests
{
    // Frozen independently generated data from the pre-F1 checkpoint.
    // Changing this hash requires a reviewed, documented reference replacement.
    private const string ReferenceSha256 = "db41c1afbe08614ac62d1dcb7ef7ba2b0aa8fdc5af5fba7c3e94d9099021de47";

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

    private const double DisplacementAbsMm = 0.0005;
    private const double DisplacementRel = 0.0001;
    private const double ForceAbsKn = 0.005;
    private const double MomentAbsKnm = 0.005;
    private const double ForceRel = 0.0001;

    public static IEnumerable<object[]> G8BCases() =>
        from model in ModelFiles
        from combination in Combinations
        select new object[] { model, combination };

    [Fact]
    public void FrozenReference_CoversExactlyFourModelsAndFourCombinationsEach()
    {
        using JsonDocument document = LoadReference();
        JsonElement[] models = document.RootElement.GetProperty("models").EnumerateArray().ToArray();

        Assert.Equal(4, models.Length);
        Assert.Equal(ModelFiles.OrderBy(x => x).ToArray(), models
            .Select(m => m.GetProperty("file").GetString()!).OrderBy(x => x).ToArray());

        foreach (JsonElement model in models)
        {
            JsonElement[] combinations = model.GetProperty("combinations").EnumerateArray().ToArray();
            Assert.Equal(4, combinations.Length);
            Assert.Equal(Combinations.OrderBy(x => x).ToArray(), combinations
                .Select(c => c.GetProperty("combination").GetString()!).OrderBy(x => x).ToArray());

            foreach (JsonElement combination in combinations)
            {
                Assert.Equal(5, combination.GetProperty("topUx_mm").EnumerateObject().Count());
                Assert.Equal(4, combination.GetProperty("floorUy_mm").EnumerateObject().Count());
                Assert.Equal(4, combination.GetProperty("supportReactions").EnumerateObject().Count());
                Assert.Equal(3, combination.GetProperty("globalLoads").EnumerateObject().Count());
            }
        }
    }

    [Theory]
    [MemberData(nameof(G8BCases))]
    public void AllSixteenScenarios_MatchIndependentDisplacementsReactionsAndLowerColumnNvm(
        string file, string combinationId)
    {
        using JsonDocument reference = LoadReference();
        JsonElement expected = GetScenario(reference, file, combinationId);
        StructuralModel model = LoadModel(file);
        StructuralAnalysisResult actual = new PlaneStructureAnalyzer().AnalyzeCombination(model, combinationId);

        List<string> discrepancies = CompareAgainstFrozenReference(model, actual, expected);

        Assert.True(discrepancies.Count == 0,
            $"G8B {file}/{combinationId}: {discrepancies.Count} independent reference mismatches:\n" +
            string.Join("\n", discrepancies));
    }

    [Theory]
    [InlineData("g8b-b_riferimento-controventato.json")]
    [InlineData("g8b-b_riferimento-senza-controventi.json")]
    public void PerturbedHorizontalLoad_IsDetectedByTheUnchangedFrozenReference(string file)
    {
        const string scenario = "ULS_WIND_POS";
        using JsonDocument reference = LoadReference();
        StructuralModel model = LoadModel(file);

        StructuralLoad load = model.Loads.First(item => item.LoadCaseId == "WIND_POS" &&
            item.Type == StructuralLoadType.NodalForce && item.Direction == StructuralLoadDirection.GlobalX);
        model.Loads[model.Loads.IndexOf(load)] = load with { Value = load.Value + 10.0 };

        StructuralAnalysisResult altered = new PlaneStructureAnalyzer().AnalyzeCombination(model, scenario);
        List<string> discrepancies = CompareAgainstFrozenReference(model, altered, GetScenario(reference, file, scenario));

        Assert.NotEmpty(discrepancies);
        Assert.Contains(discrepancies, item => item.Contains("Fx", StringComparison.Ordinal) ||
            item.Contains("Ux", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingWindCombinationTerm_IsDetectedByTheUnchangedFrozenReference()
    {
        const string file = "g8b-a_leggero-controventato.json";
        const string scenario = "ULS_WIND_POS";
        using JsonDocument reference = LoadReference();
        StructuralModel model = LoadModel(file);

        StructuralLoadCombination combination = model.LoadCombinations.Single(c => c.Id == scenario);
        StructuralLoadCombinationTerm wind = combination.Terms.Single(t => t.LoadCaseId == "WIND_POS");
        Assert.True(combination.Terms.Remove(wind));

        StructuralAnalysisResult altered = new PlaneStructureAnalyzer().AnalyzeCombination(model, scenario);
        Assert.NotEmpty(CompareAgainstFrozenReference(model, altered, GetScenario(reference, file, scenario)));
    }

    [Fact]
    public void IntroducedColumnBaseMomentRelease_IsDetectedByTheUnchangedFrozenReference()
    {
        const string file = "g8b-b_riferimento-senza-controventi.json";
        const string scenario = "ULS_WIND_POS";
        using JsonDocument reference = LoadReference();
        StructuralModel model = LoadModel(file);

        StructuralMember column = model.Members.Single(m => m.Id == "COL_C_1");
        model.Members[model.Members.IndexOf(column)] = column with { ReleaseStartMoment = true };

        StructuralAnalysisResult altered = new PlaneStructureAnalyzer().AnalyzeCombination(model, scenario);
        Assert.NotEmpty(CompareAgainstFrozenReference(model, altered, GetScenario(reference, file, scenario)));
    }

    private static List<string> CompareAgainstFrozenReference(
        StructuralModel model, StructuralAnalysisResult result, JsonElement expected)
    {
        List<string> discrepancies = new();

        foreach (JsonProperty item in expected.GetProperty("topUx_mm").EnumerateObject())
            Check(discrepancies, $"{item.Name}.Ux [mm]", item.Value.GetDouble(),
                1000.0 * result.GetDisplacement(item.Name).Ux, DisplacementAbsMm, DisplacementRel);

        foreach (JsonProperty item in expected.GetProperty("floorUy_mm").EnumerateObject())
            Check(discrepancies, $"{item.Name}.Uy [mm]", item.Value.GetDouble(),
                1000.0 * result.GetDisplacement(item.Name).Uy, DisplacementAbsMm, DisplacementRel);

        Check(discrepancies, "maxAbsUx [mm]", expected.GetProperty("maxAbsUx_mm").GetDouble(),
            1000.0 * result.Displacements.Max(d => Math.Abs(d.Ux)), DisplacementAbsMm, DisplacementRel);
        Check(discrepancies, "maxAbsUy [mm]", expected.GetProperty("maxAbsUy_mm").GetDouble(),
            1000.0 * result.Displacements.Max(d => Math.Abs(d.Uy)), DisplacementAbsMm, DisplacementRel);

        JsonElement supports = expected.GetProperty("supportReactions");
        foreach (JsonProperty item in supports.EnumerateObject())
        {
            SupportReactionResult reaction = result.GetReaction("SUP_" + item.Name);
            Check(discrepancies, $"{item.Name}.Rx [kN]", item.Value.GetProperty("Rx_kN").GetDouble(),
                reaction.Fx, ForceAbsKn, ForceRel);
            Check(discrepancies, $"{item.Name}.Ry [kN]", item.Value.GetProperty("Ry_kN").GetDouble(),
                reaction.Fy, ForceAbsKn, ForceRel);
            Check(discrepancies, $"{item.Name}.Mz [kNm]", item.Value.GetProperty("Mz_kNm").GetDouble(),
                reaction.Mz, MomentAbsKnm, ForceRel);
        }

        // Three scalar free-body balances: the total applied loads were computed
        // by the independent benchmark, not by the C# production load assembler.
        JsonElement global = expected.GetProperty("globalLoads");
        Check(discrepancies, "global Fx equilibrium [kN]", -global.GetProperty("Fx_kN").GetDouble(),
            result.Reactions.Sum(r => r.Fx), ForceAbsKn, ForceRel);
        Check(discrepancies, "global Fy equilibrium [kN]", -global.GetProperty("Fy_kN").GetDouble(),
            result.Reactions.Sum(r => r.Fy), ForceAbsKn, ForceRel);
        double reactionMoment = result.Reactions.Sum(r =>
        {
            StructuralNode node = model.Nodes.Single(n => n.Id == r.NodeId);
            return node.X * r.Fy - node.Y * r.Fx + r.Mz;
        });
        Check(discrepancies, "global Mz equilibrium [kNm]", -global.GetProperty("Mz_kNm").GetDouble(),
            reactionMoment, MomentAbsKnm, ForceRel);

        // At every lower column: independent axial shortening at floor 1 plus
        // exact one-dimensional N(x) for its own distributed vertical weight.
        // These comparisons do not use production displacements as expected data.
        var sampler = new Frame2DInternalForceSampler();
        foreach (string letter in new[] { "A", "B", "C", "D" })
        {
            string memberId = $"COL_{letter}_1";
            StructuralMember column = model.Members.Single(m => m.Id == memberId);
            StructuralNode start = model.Nodes.Single(n => n.Id == column.StartNodeId);
            StructuralNode end = model.Nodes.Single(n => n.Id == column.EndNodeId);
            double length = end.Y - start.Y;
            Assert.True(length > 0 && end.X == start.X, $"G8B lower column {memberId} must remain vertical");

            StructuralMaterial material = model.Materials.Single(m => m.Id == column.MaterialId);
            StructuralSection section = model.Sections.Single(s => s.Id == column.SectionId);
            double floorUy = expected.GetProperty("floorUy_mm").GetProperty(letter + "1").GetDouble() / 1000.0;
            double axialElastic = material.ElasticModulus * section.Area * floorUy / length;
            double lineWeight = material.UnitWeight!.Value * section.Area * SelfWeightFactor(model, result.LoadCaseId);

            // No directly specified member loads occur on these columns:
            // only the explicit uniform gravity generated from SelfWeight.
            Assert.DoesNotContain(model.Loads, load => load.TargetId == memberId);
            foreach (double x in new[] { 0.0, length / 2.0, length })
            {
                MemberInternalForceSample sample = sampler.EvaluateMemberSection(model, result, memberId, x);
                double expectedNormal = axialElastic + lineWeight * (x - length / 2.0);
                Check(discrepancies, $"{memberId}.N(x={x:R}) [kN]", expectedNormal,
                    sample.NormalForce, ForceAbsKn, ForceRel);

                // At the base, only the frame column resists bending moment;
                // diagonal trusses at A/B carry axial force but zero moment.
                if (x == 0.0)
                    Check(discrepancies, $"{memberId}.M(base) [kNm]",
                        -supports.GetProperty(letter + "0").GetProperty("Mz_kNm").GetDouble(),
                        sample.BendingMoment, MomentAbsKnm, ForceRel);

                // At C/D there are no braces at the base: horizontal support
                // reaction is exactly the negative local shear of a vertical column.
                // No transverse distributed loads act on those columns, so V is
                // constant and M(x) is linear; both are independent free-body checks.
                if (letter is "C" or "D")
                {
                    JsonElement reaction = supports.GetProperty(letter + "0");
                    double expectedShear = -reaction.GetProperty("Rx_kN").GetDouble();
                    double expectedMoment = -reaction.GetProperty("Mz_kNm").GetDouble() + expectedShear * x;
                    Check(discrepancies, $"{memberId}.V(x={x:R}) [kN]", expectedShear,
                        sample.ShearForce, ForceAbsKn, ForceRel);
                    Check(discrepancies, $"{memberId}.M(x={x:R}) [kNm]", expectedMoment,
                        sample.BendingMoment, MomentAbsKnm, ForceRel);
                }
            }
        }

        return discrepancies;
    }

    private static double SelfWeightFactor(StructuralModel model, string combinationId)
    {
        StructuralLoadCombination combination = model.LoadCombinations.Single(c => c.Id == combinationId);
        StructuralLoad selfWeight = model.Loads.Single(l => l.Type == StructuralLoadType.SelfWeight);
        StructuralLoadCombinationTerm factor = combination.Terms.Single(t => t.LoadCaseId == selfWeight.LoadCaseId);
        return selfWeight.Value * factor.Factor;
    }

    private static void Check(List<string> discrepancies, string name, double expected,
        double actual, double absoluteTolerance, double relativeTolerance)
    {
        double tolerance = absoluteTolerance + relativeTolerance * Math.Abs(expected);
        if (double.IsFinite(expected) && double.IsFinite(actual) && Math.Abs(actual - expected) <= tolerance)
            return;

        discrepancies.Add(string.Format(CultureInfo.InvariantCulture,
            "{0}: expected {1:R}, actual {2:R}, |delta| {3:R}, tolerance {4:R}",
            name, expected, actual, Math.Abs(actual - expected), tolerance));
    }

    private static JsonDocument LoadReference()
    {
        byte[] original = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "G8B", "g8b-risultati-elastici-indipendenti.json"));
        string actualHash = Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant();
        Assert.Equal(ReferenceSha256, actualHash);
        return JsonDocument.Parse(original);
    }

    private static StructuralModel LoadModel(string file) =>
        StructuralModelJsonReader.ReadValidated(Path.Combine(AppContext.BaseDirectory,
            "G8B", "Examples", file)).Model;

    private static JsonElement GetScenario(JsonDocument document, string file, string combinationId) =>
        document.RootElement.GetProperty("models").EnumerateArray()
            .Single(model => model.GetProperty("file").GetString() == file)
            .GetProperty("combinations").EnumerateArray()
            .Single(combination => combination.GetProperty("combination").GetString() == combinationId);
}
