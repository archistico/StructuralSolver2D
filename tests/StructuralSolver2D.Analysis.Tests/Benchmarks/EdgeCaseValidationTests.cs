using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Equilibrium;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// Guards edge cases that are intentionally outside the normal benchmark catalog:
/// singular systems, mechanisms, near-axis transformations and extreme load scales.
/// </summary>
public sealed class EdgeCaseValidationTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    public static TheoryData<string> SingularModelFiles => new()
    {
        "E01-unstable-free-frame.json",
        "E02-insufficient-horizontal-restraint.json",
        "E05-release-mechanism-cantilever.json",
    };

    public static TheoryData<string> StableNearAxisModelFiles => new()
    {
        "E03-nearly-vertical-frame-member.json",
        "E04-nearly-horizontal-frame-member.json",
    };

    [Theory]
    [MemberData(nameof(SingularModelFiles))]
    public void EdgeCaseSingularModels_ShouldFailWithClearDiagnostic(string fileName)
    {
        StructuralModelJsonFile input = ReadEdgeCase(fileName);

        StructuralAnalysisException exception = Assert.Throws<StructuralAnalysisException>(() =>
            Analyze(input.Model, input.LoadCaseId));

        Assert.True(exception.Message.Contains("singular", StringComparison.OrdinalIgnoreCase), exception.Message);
        Assert.True(exception.Message.Contains("unstable", StringComparison.OrdinalIgnoreCase), exception.Message);
    }

    [Theory]
    [MemberData(nameof(StableNearAxisModelFiles))]
    public void EdgeCaseNearAxisMembers_ShouldRemainFiniteAndInEquilibrium(string fileName)
    {
        StructuralModelJsonFile input = ReadEdgeCase(fileName);

        StructuralAnalysisResult result = Analyze(input.Model, input.LoadCaseId);

        AssertFiniteDisplacements(result);
        AssertFiniteReactions(result);
        AssertFiniteMemberEndForces(result);
        AssertGlobalEquilibrium(input.Model, result, tolerance: 1e-6);
    }

    [Fact]
    public void EdgeCaseNegativeLoadCombinationFactor_ShouldBeValidAndAnalyzeWithSignedFactor()
    {
        StructuralModelJsonFile input = ReadEdgeCase("E06-negative-load-combination.json");

        StructuralModelValidationResult validation = new StructuralModelValidator().Validate(input.Model);
        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Issues.Select(issue => issue.Message)));

        StructuralAnalysisResult result = Analyze(input.Model, input.LoadCaseId);

        Assert.Equal("COMB_NEG", result.LoadCaseId);
        Assert.Equal(15.0, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(15.0, result.GetReaction("SB").Fy, precision: 6);
        AssertGlobalEquilibrium(input.Model, result, tolerance: 1e-6);
    }

    [Fact]
    public void EdgeCaseZeroLoadCombinationFactor_ShouldBeRejectedByValidator()
    {
        StructuralModelJsonFile input = ReadEdgeCase("E07-zero-load-combination-factor-invalid.json");

        StructuralModelValidationResult validation = new StructuralModelValidator().Validate(input.Model);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, issue => issue.Code == "LOAD_COMBINATION_INVALID_FACTOR");
    }

    [Theory]
    [InlineData(1e-9)]
    [InlineData(1e9)]
    public void Analyze_SimplySupportedBeamWithVerySmallOrLargeLoad_ShouldRemainFiniteAndLinear(double loadMagnitude)
    {
        StructuralModel model = CreateSimplySupportedBeam(loadMagnitude);

        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");

        double expectedReaction = loadMagnitude * 5.0 / 2.0;
        AssertClose(expectedReaction, result.GetReaction("SA").Fy, RelativeTolerance(expectedReaction));
        AssertClose(expectedReaction, result.GetReaction("SB").Fy, RelativeTolerance(expectedReaction));
        AssertFiniteDisplacements(result);
        AssertFiniteReactions(result);
        AssertFiniteMemberEndForces(result);
        AssertGlobalEquilibrium(model, result, tolerance: Math.Max(1e-6, Math.Abs(loadMagnitude) * 1e-10));
    }

    [Fact]
    public void Analyze_LoadScaling_ShouldProduceProportionalDisplacementsAndReactions()
    {
        StructuralAnalysisResult reference = new Frame2DAnalyzer().Analyze(CreateSimplySupportedBeam(2.0), "LC1");
        StructuralAnalysisResult scaled = new Frame2DAnalyzer().Analyze(CreateSimplySupportedBeam(2000.0), "LC1");

        const double scale = 1000.0;
        AssertClose(reference.GetReaction("SA").Fy * scale, scaled.GetReaction("SA").Fy, 1e-6);
        AssertClose(reference.GetReaction("SB").Fy * scale, scaled.GetReaction("SB").Fy, 1e-6);
        AssertClose(reference.GetDisplacement("B").Rz * scale, scaled.GetDisplacement("B").Rz, 1e-9);
    }

    private static void AssertClose(double expected, double actual, double tolerance)
    {
        double difference = Math.Abs(expected - actual);
        Assert.True(
            difference <= tolerance,
            $"Expected {expected}, actual {actual}, tolerance {tolerance}, difference {difference}.");
    }

    private static double RelativeTolerance(double expected) => Math.Max(1e-12, Math.Abs(expected) * 1e-10);

    private static StructuralModelJsonFile ReadEdgeCase(string fileName)
    {
        string repositoryRoot = BenchmarkRepository.FindRoot();
        string path = Path.Combine(repositoryRoot, "benchmarks", "edge-cases", fileName);
        return StructuralModelJsonReader.Read(path);
    }

    private static StructuralAnalysisResult Analyze(StructuralModel model, string analysisId)
    {
        bool isTrussOnly = model.Members.All(member => member.Type == MemberType.Truss2D);
        bool isFrameOnly = model.Members.All(member => member.Type == MemberType.Frame2D);
        bool isCombination = model.LoadCombinations.Any(combination =>
            string.Equals(combination.Id, analysisId, StringComparison.OrdinalIgnoreCase));

        if (isTrussOnly)
        {
            Truss2DAnalyzer analyzer = new();
            return isCombination ? analyzer.AnalyzeCombination(model, analysisId) : analyzer.Analyze(model, analysisId);
        }

        if (isFrameOnly)
        {
            Frame2DAnalyzer analyzer = new();
            return isCombination ? analyzer.AnalyzeCombination(model, analysisId) : analyzer.Analyze(model, analysisId);
        }

        PlaneStructureAnalyzer planeAnalyzer = new();
        return isCombination ? planeAnalyzer.AnalyzeCombination(model, analysisId) : planeAnalyzer.Analyze(model, analysisId);
    }

    private static StructuralModel CreateSimplySupportedBeam(double loadMagnitude) =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("AB", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "Default load case"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "AB", StructuralLoadDirection.GlobalY, -loadMagnitude));

    private static void AssertFiniteDisplacements(StructuralAnalysisResult result)
    {
        Assert.All(result.Displacements, displacement =>
        {
            Assert.True(double.IsFinite(displacement.Ux), $"Non-finite Ux at node {displacement.NodeId}.");
            Assert.True(double.IsFinite(displacement.Uy), $"Non-finite Uy at node {displacement.NodeId}.");
            Assert.True(double.IsFinite(displacement.Rz), $"Non-finite Rz at node {displacement.NodeId}.");
        });
    }

    private static void AssertFiniteReactions(StructuralAnalysisResult result)
    {
        Assert.All(result.Reactions, reaction =>
        {
            Assert.True(double.IsFinite(reaction.Fx), $"Non-finite Fx at support {reaction.SupportId}.");
            Assert.True(double.IsFinite(reaction.Fy), $"Non-finite Fy at support {reaction.SupportId}.");
            Assert.True(double.IsFinite(reaction.Mz), $"Non-finite Mz at support {reaction.SupportId}.");
        });
    }

    private static void AssertFiniteMemberEndForces(StructuralAnalysisResult result)
    {
        Assert.All(result.MemberEndForces, force =>
        {
            Assert.True(double.IsFinite(force.StartAxial), $"Non-finite start axial force in member {force.MemberId}.");
            Assert.True(double.IsFinite(force.StartShear), $"Non-finite start shear force in member {force.MemberId}.");
            Assert.True(double.IsFinite(force.StartMoment), $"Non-finite start moment in member {force.MemberId}.");
            Assert.True(double.IsFinite(force.EndAxial), $"Non-finite end axial force in member {force.MemberId}.");
            Assert.True(double.IsFinite(force.EndShear), $"Non-finite end shear force in member {force.MemberId}.");
            Assert.True(double.IsFinite(force.EndMoment), $"Non-finite end moment in member {force.MemberId}.");
        });
    }

    private static void AssertGlobalEquilibrium(StructuralModel model, StructuralAnalysisResult result, double tolerance)
    {
        GlobalEquilibriumResult equilibrium = new GlobalEquilibriumChecker().Check(model, result);

        Assert.True(
            equilibrium.IsInEquilibrium(tolerance),
            $"Global equilibrium residual too large. " +
            $"Residual Fx={equilibrium.ResidualFx}, Fy={equilibrium.ResidualFy}, Mz={equilibrium.ResidualMz}, tolerance={tolerance}.");
    }
}
