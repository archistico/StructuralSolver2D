using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using StructuralSolver2D.Analysis.Tests.Benchmarks;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.G8BReferenceF1D;

/// <summary>
/// Opt-in, standalone C# export/verification of G8B member N/V/M reference
/// values. Its only numerical solver is the F1C test-only independent energy
/// quadrature + Cholesky implementation, linked as shared C# source.
/// It cannot invoke PlaneStructureAnalyzer, Frame2DElementMatrices or a
/// production member sampler, and it refuses to overwrite existing files.
/// </summary>
internal static class Program
{
    private const string HistoricalGoldenSha256 =
        "db41c1afbe08614ac62d1dcb7ef7ba2b0aa8fdc5af5fba7c3e94d9099021de47";

    private static readonly IReadOnlyDictionary<string, string> FrozenInputHashes =
        new SortedDictionary<string, string>(StringComparer.Ordinal)
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

    private static readonly string[] ScenarioIds =
    {
        "ULS_GRAVITY", "ULS_WIND_POS", "ULS_WIND_NEG", "SLS_RARE"
    };

    private const double DisplacementAbsoluteMm = 0.0005;
    private const double ForceAbsoluteKn = 0.005;
    private const double MomentAbsoluteKnm = 0.005;
    private const double RelativeTolerance = 0.0001;

    private sealed record FrozenGoldenFile(
        string Format, string Status, string Producer,
        string HistoricalGoldenSha256,
        IReadOnlyDictionary<string, string> ModelSha256,
        string LengthUnit, string ForceUnit, string MomentUnit,
        IReadOnlyList<ModelSnapshot> Models);

    private sealed record ModelSnapshot(string Filename, IReadOnlyList<ScenarioSnapshot> Scenarios);
    private sealed record ScenarioSnapshot(string CombinationId,
        IReadOnlyList<DisplacementSnapshot> Displacements,
        IReadOnlyList<ReactionSnapshot> Reactions,
        IReadOnlyList<MemberSnapshot> Members);
    private sealed record DisplacementSnapshot(string NodeId,
        double UxMetres, double UyMetres, double RzRadians);
    private sealed record ReactionSnapshot(string NodeId,
        double FxKn, double FyKn, double MzKnm);
    private sealed record ForceSnapshot(double AxialKn, double ShearKn, double MomentKnm);
    private sealed record StationSnapshot(double XMetres, double NormalKn,
        double ShearKn, double MomentKnm);
    private sealed record MemberSnapshot(
        string MemberId, string ElementType, double LengthMetres,
        ForceSnapshot StartEndAction, ForceSnapshot EndEndAction,
        IReadOnlyList<StationSnapshot> Stations);

    private static int Main(string[] args)
    {
        if (args.Length != 2 || (args[0] != "--output" && args[0] != "--verify") ||
            string.IsNullOrWhiteSpace(args[1]))
        {
            Console.Error.WriteLine("Usage (run from StructuralSolver2D root):");
            Console.Error.WriteLine("  dotnet run --project tools/StructuralSolver2D.G8BReferenceF1D -c Release -- --output benchmarks/independent/g8b-house/g8b-member-nvm-f1d.proposed.json");
            Console.Error.WriteLine("  dotnet run --project tools/StructuralSolver2D.G8BReferenceF1D -c Release -- --verify benchmarks/independent/g8b-house/g8b-member-nvm-f1d.proposed.json");
            return 2;
        }

        try
        {
            string root = Directory.GetCurrentDirectory();
            if (!File.Exists(Path.Combine(root, "StructuralSolver2D.sln")))
                throw new InvalidOperationException(
                    "Run this tool from the StructuralSolver2D project root (StructuralSolver2D.sln).");

            string output = Path.GetFullPath(args[1], root);
            if (!string.Equals(Path.GetExtension(output), ".json", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The output/verification target must be a .json file.");

            byte[] independentlyComputed = Generate(root, out int members, out int stations);
            string hash = Hash(independentlyComputed);

            if (args[0] == "--output")
            {
                // CreateNew is an intentional safety property: never silently
                // overwrite frozen or already proposed engineering evidence.
                using FileStream stream = new(output, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None);
                stream.Write(independentlyComputed);
                stream.Flush(true);
                Console.WriteLine($"Created independent PROPOSED reference: {output}");
            }
            else
            {
                byte[] existing = File.ReadAllBytes(output);
                if (!existing.AsSpan().SequenceEqual(independentlyComputed))
                    throw new InvalidOperationException(
                        "Verification FAILED: file bytes differ from the independent F1D reference. " +
                        $"File SHA256: {Hash(existing)}; expected independent SHA256: {hash}.");
                Console.WriteLine($"Verified independent PROPOSED reference: {output}");
            }

            Console.WriteLine($"16 scenarios; {members} member-scenarios; {stations} N/V/M station records.");
            Console.WriteLine($"SHA256: {hash}");
            Console.WriteLine("Historical independent displacement/reaction anchor: PASS (all 16 scenarios).");
            Console.WriteLine("Status: PROPOSED. Requires review and a separately pinned golden before F1.1 closes.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"F1D FAILED: {error}");
            return 1;
        }
    }

    private static byte[] Generate(string root, out int memberCount, out int stationCount)
    {
        string historicalPath = Path.Combine(root, "benchmarks", "independent",
            "g8b-house", "g8b-risultati-elastici-indipendenti.json");
        byte[] historicalBytes = File.ReadAllBytes(historicalPath);
        RequireHash("historical golden", HistoricalGoldenSha256, historicalBytes);
        using JsonDocument historical = JsonDocument.Parse(historicalBytes);

        List<ModelSnapshot> snapshots = new();
        memberCount = 0;
        stationCount = 0;

        foreach (KeyValuePair<string, string> frozen in FrozenInputHashes)
        {
            string inputPath = Path.Combine(root, "examples", "mixed", "g8b-house", frozen.Key);
            RequireHash(frozen.Key, frozen.Value, File.ReadAllBytes(inputPath));
            StructuralModel model = StructuralModelJsonReader.ReadValidated(inputPath).Model;
            List<ScenarioSnapshot> scenarios = new();

            foreach (string combinationId in ScenarioIds)
            {
                // No production solver is invoked: this reference was already
                // checked separately by F1C against the historical golden.
                G8BVariationalReferenceF1C.Solution reference =
                    G8BVariationalReferenceF1C.Analyze(model, combinationId);
                ValidateAgainstHistoricalGolden(historical, frozen.Key, combinationId, reference);

                List<DisplacementSnapshot> displacementRows = model.Nodes
                    .OrderBy(n => n.Id, StringComparer.Ordinal)
                    .Select(node =>
                    {
                        var d = reference.Displacements[node.Id];
                        return new DisplacementSnapshot(node.Id, d.Ux, d.Uy, d.Rz);
                    }).ToList();

                List<ReactionSnapshot> reactionRows = model.Supports
                    .OrderBy(s => s.NodeId, StringComparer.Ordinal)
                    .Select(support =>
                    {
                        var r = reference.Reactions[support.NodeId];
                        return new ReactionSnapshot(support.NodeId, r.Rx, r.Ry, r.Mz);
                    }).ToList();

                List<MemberSnapshot> memberRows = new();
                foreach (StructuralMember member in model.Members.OrderBy(m => m.Id, StringComparer.Ordinal))
                {
                    var force = reference.Members[member.Id];
                    List<StationSnapshot> samples = new();
                    foreach (double ratio in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
                    {
                        double x = force.Length * ratio;
                        (double n, double v, double m) = force.At(x);
                        samples.Add(new StationSnapshot(x, n, v, m));
                        stationCount++;
                    }

                    memberRows.Add(new MemberSnapshot(member.Id, member.Type.ToString(),
                        force.Length,
                        new ForceSnapshot(force.StartAxial, force.StartShear, force.StartMoment),
                        new ForceSnapshot(force.EndAxial, force.EndShear, force.EndMoment), samples));
                    memberCount++;
                }

                scenarios.Add(new ScenarioSnapshot(combinationId,
                    displacementRows, reactionRows, memberRows));
            }
            snapshots.Add(new ModelSnapshot(frozen.Key, scenarios));
        }

        if (snapshots.Count != 4 || snapshots.Any(m => m.Scenarios.Count != 4) ||
            memberCount != 336 || stationCount != 1680)
            throw new InvalidOperationException(
                $"Unexpected G8B coverage: models={snapshots.Count}, member scenarios={memberCount}, stations={stationCount}.");

        var file = new FrozenGoldenFile(
            Format: "structuralsolver2d.g8b-member-nvm.f1d.v1",
            Status: "proposed-unreviewed",
            Producer: "independent-f1c-energy-quadrature-cholesky-csharp-net8",
            HistoricalGoldenSha256: HistoricalGoldenSha256,
            ModelSha256: FrozenInputHashes,
            LengthUnit: "m", ForceUnit: "kN", MomentUnit: "kNm",
            Models: snapshots);

        return JsonSerializer.SerializeToUtf8Bytes(file,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
    }

    private static void ValidateAgainstHistoricalGolden(
        JsonDocument historical, string filename, string combinationId,
        G8BVariationalReferenceF1C.Solution independent)
    {
        JsonElement expected = historical.RootElement.GetProperty("models")
            .EnumerateArray().Single(m => m.GetProperty("file").GetString() == filename)
            .GetProperty("combinations").EnumerateArray()
            .Single(c => c.GetProperty("combination").GetString() == combinationId);

        foreach (JsonProperty item in expected.GetProperty("topUx_mm").EnumerateObject())
            RequireClose($"{filename}/{combinationId}/{item.Name}.Ux [mm]",
                item.Value.GetDouble(), 1000 * independent.Displacements[item.Name].Ux,
                DisplacementAbsoluteMm);

        foreach (JsonProperty item in expected.GetProperty("floorUy_mm").EnumerateObject())
            RequireClose($"{filename}/{combinationId}/{item.Name}.Uy [mm]",
                item.Value.GetDouble(), 1000 * independent.Displacements[item.Name].Uy,
                DisplacementAbsoluteMm);

        RequireClose($"{filename}/{combinationId}/maxAbsUx [mm]",
            expected.GetProperty("maxAbsUx_mm").GetDouble(),
            1000 * independent.Displacements.Values.Max(d => Math.Abs(d.Ux)),
            DisplacementAbsoluteMm);
        RequireClose($"{filename}/{combinationId}/maxAbsUy [mm]",
            expected.GetProperty("maxAbsUy_mm").GetDouble(),
            1000 * independent.Displacements.Values.Max(d => Math.Abs(d.Uy)),
            DisplacementAbsoluteMm);

        foreach (JsonProperty item in expected.GetProperty("supportReactions").EnumerateObject())
        {
            var actual = independent.Reactions[item.Name];
            RequireClose($"{filename}/{combinationId}/{item.Name}.Rx [kN]",
                item.Value.GetProperty("Rx_kN").GetDouble(), actual.Rx, ForceAbsoluteKn);
            RequireClose($"{filename}/{combinationId}/{item.Name}.Ry [kN]",
                item.Value.GetProperty("Ry_kN").GetDouble(), actual.Ry, ForceAbsoluteKn);
            RequireClose($"{filename}/{combinationId}/{item.Name}.Mz [kNm]",
                item.Value.GetProperty("Mz_kNm").GetDouble(), actual.Mz, MomentAbsoluteKnm);
        }
    }

    private static void RequireClose(string name, double expected,
        double actual, double absoluteTolerance)
    {
        double tolerance = absoluteTolerance + RelativeTolerance * Math.Abs(expected);
        if (double.IsFinite(expected) && double.IsFinite(actual) &&
            Math.Abs(expected - actual) <= tolerance)
            return;
        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
            "Historical golden mismatch at {0}: expected={1:R}, actual={2:R}, tolerance={3:R}.",
            name, expected, actual, tolerance));
    }

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void RequireHash(string name, string expected, byte[] bytes)
    {
        if (expected.Length != 64 || expected.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException(
                $"Invalid frozen SHA256 literal ({name}): expected 64 hexadecimal characters, found {expected.Length}.");

        string actual = Hash(bytes);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Frozen input changed ({name}): expected SHA256={expected}, actual={actual}.");
    }
}
