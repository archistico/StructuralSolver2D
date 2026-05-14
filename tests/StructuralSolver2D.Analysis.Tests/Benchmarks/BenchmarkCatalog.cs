using System.Text.Json;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// Describes the external benchmark catalog stored in benchmarks/expected/expected-results.json.
/// The catalog is intentionally test-side infrastructure: production solver projects do not depend on it.
/// </summary>
internal sealed class BenchmarkCatalog
{
    public List<BenchmarkCase> Benchmarks { get; set; } = new();

    public static BenchmarkCatalog Load(string filePath)
    {
        string json = File.ReadAllText(filePath);
        BenchmarkCatalog? catalog = JsonSerializer.Deserialize<BenchmarkCatalog>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        return catalog ?? throw new InvalidOperationException("Benchmark expected-results.json is empty or invalid.");
    }

    public IReadOnlyList<string> Validate(string repositoryRoot)
    {
        List<string> issues = new();

        if (Benchmarks.Count == 0)
        {
            issues.Add("The benchmark catalog does not contain any benchmark.");
            return issues;
        }

        foreach (var group in Benchmarks.GroupBy(benchmark => benchmark.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() > 1)
            {
                issues.Add($"Duplicate benchmark id '{group.Key}'.");
            }
        }

        foreach (BenchmarkCase benchmark in Benchmarks)
        {
            if (string.IsNullOrWhiteSpace(benchmark.Id))
            {
                issues.Add("A benchmark has an empty id.");
            }

            if (string.IsNullOrWhiteSpace(benchmark.Name))
            {
                issues.Add($"Benchmark '{benchmark.Id}' has an empty name.");
            }

            if (string.IsNullOrWhiteSpace(benchmark.ModelPath))
            {
                issues.Add($"Benchmark '{benchmark.Id}' has an empty modelPath.");
            }
            else
            {
                string modelPath = Path.Combine(repositoryRoot, benchmark.ModelPath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(modelPath))
                {
                    issues.Add($"Benchmark '{benchmark.Id}' references missing model file '{benchmark.ModelPath}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(benchmark.AnalysisId))
            {
                issues.Add($"Benchmark '{benchmark.Id}' has an empty analysisId.");
            }

            if (!double.IsFinite(benchmark.Tolerance) || benchmark.Tolerance <= 0.0)
            {
                issues.Add($"Benchmark '{benchmark.Id}' has an invalid tolerance '{benchmark.Tolerance}'.");
            }
        }

        return issues;
    }
}

internal sealed class BenchmarkCase
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public string AnalysisId { get; set; } = string.Empty;
    public double Tolerance { get; set; } = 1e-6;
    public BenchmarkExpected Expected { get; set; } = new();
}

internal sealed class BenchmarkExpected
{
    public List<ExpectedSupportReaction> SupportReactions { get; set; } = new();
    public double? MaxAbsShear { get; set; }
    public double? MaxAbsMoment { get; set; }
    public double? TipDisplacementY { get; set; }
    public double? TipRotationZ { get; set; }
    public List<ExpectedMemberAxialForce> MemberAxialForces { get; set; } = new();
    public List<ExpectedNodalDisplacement> NodalDisplacements { get; set; } = new();
    public List<string> Checks { get; set; } = new();
}

internal sealed class ExpectedSupportReaction
{
    public string NodeId { get; set; } = string.Empty;
    public double? Fx { get; set; }
    public double? Fy { get; set; }
    public double? Mz { get; set; }
}

internal sealed class ExpectedNodalDisplacement
{
    public string NodeId { get; set; } = string.Empty;
    public double? Ux { get; set; }
    public double? Uy { get; set; }
    public double? Rz { get; set; }
}

internal sealed class ExpectedMemberAxialForce
{
    public string MemberId { get; set; } = string.Empty;
    public double NormalForce { get; set; }
}
