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
/// F1.1-C: validates an isolated quadrature+energy+Cholesky reference against
/// the frozen historical independent benchmark, then uses it to compare all
/// G8B member end forces and section N/V/M with production output.
/// The reference recalculates numerically from HASH-LOCKED inputs at test time;
/// per-member numerical JSON golden values are NOT yet frozen on disk.
/// </summary>
public sealed class G8BIndependentMemberF1CTests
{
    private const string GoldenSha256 =
        "db41c1afbe08614ac62d1dcb7ef7ba2b0aa8fdc5af5fba7c3e94d9099021de47";

    // Locks all four independently validated G8B inputs; the reference cannot
    // silently adapt to changed JSON geometry, loads or combinations.
    private static readonly IReadOnlyDictionary<string, string> ModelSha256 =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["g8b-a_leggero-controventato.json"] =
                "d62659e2617cbd1542fbb87f66294f3c39e6fcbcb55fc0efe65662c2ec8d1a0c",
            ["g8b-b_riferimento-controventato.json"] =
                "e9c6649420e8ab858f2458fd3359dd49053d8d9c883b2bb7a72f13bb65a6a8f1",
            ["g8b-b_riferimento-senza-controventi.json"] =
                "da10a8e7a3c65a048505506fbd295d890f49075631a4d490779f8379335f1c36",
            ["g8b-c_robusto-controventato.json"] =
                "3f096fde5ad1dd82b618bf4293af20d55aea8cea4352ddefebe7ebe9c4c5a475"
        };

    private static readonly string[] CombinationIds =
    {
        "ULS_GRAVITY", "ULS_WIND_POS", "ULS_WIND_NEG", "SLS_RARE"
    };

    private const double DisplacementAbsMm = 0.0005;
    private const double RotationAbsRad = 0.0000001;
    private const double ForceAbsKn = 0.005;
    private const double MomentAbsKnm = 0.005;
    private const double RelativeTolerance = 0.0001;

    public static IEnumerable<object[]> G8BCases() =>
        from model in ModelSha256.Keys.OrderBy(k => k, StringComparer.Ordinal)
        from combination in CombinationIds
        select new object[] { model, combination };

    [Theory]
    [MemberData(nameof(G8BCases))]
    public void AllSixteenScenarios_StandaloneReferenceMatchesPreviouslyFrozenIndependentDisplacementsAndReactions(
        string filename, string combinationId)
    {
        StructuralModel model = LoadHashLockedModel(filename);
        G8BVariationalReferenceF1C.Solution independent =
            G8BVariationalReferenceF1C.Analyze(model, combinationId);
        using JsonDocument originalGolden = LoadHashLockedGolden();
        JsonElement expected = Scenario(originalGolden, filename, combinationId);
        List<string> mismatches = new();

        foreach (JsonProperty item in expected.GetProperty("topUx_mm").EnumerateObject())
            Check(mismatches, $"{item.Name}.Ux independent [mm]",
                item.Value.GetDouble(), 1000 * independent.Displacements[item.Name].Ux,
                DisplacementAbsMm);

        foreach (JsonProperty item in expected.GetProperty("floorUy_mm").EnumerateObject())
            Check(mismatches, $"{item.Name}.Uy independent [mm]",
                item.Value.GetDouble(), 1000 * independent.Displacements[item.Name].Uy,
                DisplacementAbsMm);

        Check(mismatches, "maxAbsUx independent [mm]",
            expected.GetProperty("maxAbsUx_mm").GetDouble(),
            1000 * independent.Displacements.Values.Max(d => Math.Abs(d.Ux)), DisplacementAbsMm);
        Check(mismatches, "maxAbsUy independent [mm]",
            expected.GetProperty("maxAbsUy_mm").GetDouble(),
            1000 * independent.Displacements.Values.Max(d => Math.Abs(d.Uy)), DisplacementAbsMm);

        foreach (JsonProperty support in expected.GetProperty("supportReactions").EnumerateObject())
        {
            var actual = independent.Reactions[support.Name];
            Check(mismatches, $"{support.Name}.Rx independent [kN]",
                support.Value.GetProperty("Rx_kN").GetDouble(), actual.Rx, ForceAbsKn);
            Check(mismatches, $"{support.Name}.Ry independent [kN]",
                support.Value.GetProperty("Ry_kN").GetDouble(), actual.Ry, ForceAbsKn);
            Check(mismatches, $"{support.Name}.Mz independent [kNm]",
                support.Value.GetProperty("Mz_kNm").GetDouble(), actual.Mz, MomentAbsKnm);
        }

        AssertNoMismatches(filename, combinationId, mismatches);
    }

    [Theory]
    [MemberData(nameof(G8BCases))]
    public void AllSixteenScenarios_EveryMemberEndAndSectionForceMatchesIsolatedReference(
        string filename, string combinationId)
    {
        StructuralModel model = LoadHashLockedModel(filename);
        G8BVariationalReferenceF1C.Solution independent =
            G8BVariationalReferenceF1C.Analyze(model, combinationId);
        StructuralAnalysisResult actual = new PlaneStructureAnalyzer()
            .AnalyzeCombination(model, combinationId);
        var sampler = new Frame2DInternalForceSampler();
        List<string> mismatches = new();

        Assert.Equal(model.Nodes.Count, independent.Displacements.Count);
        Assert.Equal(model.Members.Count, independent.Members.Count);
        Assert.Equal(model.Members.Count, actual.MemberEndForces.Count);

        // Reference and production have independently assembled and solved all
        // three nodal DOFs, not only the selected historical golden stations.
        foreach (StructuralNode node in model.Nodes)
        {
            var expected = independent.Displacements[node.Id];
            NodalDisplacementResult observed = actual.GetDisplacement(node.Id);
            Check(mismatches, $"{node.Id}.Ux [mm]", expected.Ux * 1000,
                observed.Ux * 1000, DisplacementAbsMm);
            Check(mismatches, $"{node.Id}.Uy [mm]", expected.Uy * 1000,
                observed.Uy * 1000, DisplacementAbsMm);
            Check(mismatches, $"{node.Id}.Rz [rad]", expected.Rz,
                observed.Rz, RotationAbsRad);
        }

        foreach (StructuralMember member in model.Members)
        {
            var expected = independent.Members[member.Id];
            MemberEndForceResult observed = actual.MemberEndForces.Single(e => e.MemberId == member.Id);
            Check(mismatches, $"{member.Id}.StartAxial [kN]", expected.StartAxial,
                observed.StartAxial, ForceAbsKn);
            Check(mismatches, $"{member.Id}.StartShear [kN]", expected.StartShear,
                observed.StartShear, ForceAbsKn);
            Check(mismatches, $"{member.Id}.StartMoment [kNm]", expected.StartMoment,
                observed.StartMoment, MomentAbsKnm);
            Check(mismatches, $"{member.Id}.EndAxial [kN]", expected.EndAxial,
                observed.EndAxial, ForceAbsKn);
            Check(mismatches, $"{member.Id}.EndShear [kN]", expected.EndShear,
                observed.EndShear, ForceAbsKn);
            Check(mismatches, $"{member.Id}.EndMoment [kNm]", expected.EndMoment,
                observed.EndMoment, MomentAbsKnm);

            foreach (double ratio in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
            {
                double x = ratio * expected.Length;
                (double n, double v, double m) = expected.At(x);
                MemberInternalForceSample sample = sampler.EvaluateMemberSection(
                    model, actual, member.Id, x);
                Check(mismatches, $"{member.Id}.N({ratio:R}L) [kN]", n,
                    sample.NormalForce, ForceAbsKn);
                Check(mismatches, $"{member.Id}.V({ratio:R}L) [kN]", v,
                    sample.ShearForce, ForceAbsKn);
                Check(mismatches, $"{member.Id}.M({ratio:R}L) [kNm]", m,
                    sample.BendingMoment, MomentAbsKnm);
            }
        }

        AssertNoMismatches(filename, combinationId, mismatches);
    }

    [Fact]
    public void MutatedWindLoad_IsDetectedByTheUnchangedIndependentReference()
    {
        const string filename = "g8b-b_riferimento-controventato.json";
        const string combinationId = "ULS_WIND_POS";
        StructuralModel frozen = LoadHashLockedModel(filename);
        var reference = G8BVariationalReferenceF1C.Analyze(frozen, combinationId);
        StructuralModel modified = LoadHashLockedModel(filename);
        StructuralLoad wind = modified.Loads.Single(l => l.Id == "W3_RIDGE");
        modified.Loads[modified.Loads.IndexOf(wind)] = wind with { Value = wind.Value + 10.0 };

        StructuralAnalysisResult altered = new PlaneStructureAnalyzer()
            .AnalyzeCombination(modified, combinationId);
        double expectedRidgeUxMm = 1000 * reference.Displacements["RIDGE"].Ux;
        double actualRidgeUxMm = 1000 * altered.GetDisplacement("RIDGE").Ux;
        Assert.True(Math.Abs(actualRidgeUxMm - expectedRidgeUxMm) > DisplacementAbsMm,
            "Mutating WIND_POS must be rejected against the original independent G8B oracle.");
    }

    internal static StructuralModel LoadHashLockedModel(string filename)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "G8B", "Examples", filename);
        Assert.Equal(ModelSha256[filename], HashFile(path));
        return StructuralModelJsonReader.ReadValidated(path).Model;
    }

    private static JsonDocument LoadHashLockedGolden()
    {
        string path = Path.Combine(AppContext.BaseDirectory,
            "G8B", "g8b-risultati-elastici-indipendenti.json");
        Assert.Equal(GoldenSha256, HashFile(path));
        return JsonDocument.Parse(File.ReadAllBytes(path));
    }

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static JsonElement Scenario(JsonDocument golden, string file, string combination) =>
        golden.RootElement.GetProperty("models").EnumerateArray()
            .Single(m => m.GetProperty("file").GetString() == file)
            .GetProperty("combinations").EnumerateArray()
            .Single(c => c.GetProperty("combination").GetString() == combination);

    private static void Check(List<string> mismatches, string name,
        double expected, double actual, double absoluteTolerance)
    {
        double tolerance = absoluteTolerance + RelativeTolerance * Math.Abs(expected);
        if (double.IsFinite(expected) && double.IsFinite(actual) &&
            Math.Abs(actual - expected) <= tolerance)
            return;

        mismatches.Add(string.Format(CultureInfo.InvariantCulture,
            "{0}: expected={1:R}, actual={2:R}, tolerance={3:R}",
            name, expected, actual, tolerance));
    }

    private static void AssertNoMismatches(string filename, string combination,
        IReadOnlyList<string> mismatches) =>
        Assert.True(mismatches.Count == 0,
            $"G8B F1C {filename}/{combination}: {mismatches.Count} mismatches:\n"
            + string.Join("\n", mismatches));
}
