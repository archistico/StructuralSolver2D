using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Serviceability;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.PublicApi;

/// <summary>
/// Guards the stable public analysis facade used by external applications.
/// </summary>
public sealed class StructuralSolver2DServiceTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    [Fact]
    public void AnalyzeLoadCase_ShouldReturnCompleteResultBundle()
    {
        StructuralModel model = CreateSimplySupportedBeam();
        var service = new StructuralSolver2DService();

        StructuralAnalysisOutput output = service.AnalyzeLoadCase(
            model,
            "LC1",
            new StructuralAnalysisOptions
            {
                InternalForceSampleCount = 11,
                DisplacementSampleCount = 9,
            });

        Assert.Equal("LC1", output.Result.LoadCaseId);
        Assert.Equal(2, output.Result.Displacements.Count);
        Assert.Equal(2, output.Result.Reactions.Count);
        Assert.Single(output.Result.MemberEndForces);
        Assert.Single(output.InternalForceDiagrams);
        Assert.Single(output.DisplacementDiagrams);
        Assert.Empty(output.DeflectionChecks);
        Assert.Equal("LC1", output.Summary.LoadCaseId);
        Assert.Equal(11, output.InternalForceDiagrams[0].Samples.Count);
        Assert.Equal(9, output.DisplacementDiagrams[0].Samples.Count);
    }

    [Fact]
    public void AnalyzeLoadCombination_ShouldUseCombinationIdAsResultId()
    {
        StructuralModel model = CreateSimplySupportedBeam()
            .AddLoadCombination(new StructuralLoadCombination(
                "SLS1",
                "Serviceability combination",
                new[] { new StructuralLoadCombinationTerm("LC1", 1.0) }));

        StructuralAnalysisOutput output = new StructuralSolver2DService().AnalyzeLoadCombination(model, "SLS1");

        Assert.Equal("SLS1", output.Result.LoadCaseId);
        Assert.Equal("SLS1", output.Summary.LoadCaseId);
        Assert.Single(output.InternalForceDiagrams);
    }

    [Fact]
    public void Analyze_WithDeflectionLimit_ShouldReturnPreliminaryChecksEvenWhenDisplacementDiagramsAreDisabled()
    {
        StructuralModel model = CreateSimplySupportedBeam();

        StructuralAnalysisOutput output = new StructuralSolver2DService().AnalyzeLoadCase(
            model,
            "LC1",
            new StructuralAnalysisOptions
            {
                IncludeDisplacementDiagrams = false,
                DisplacementSampleCount = 7,
                DeflectionLimit = new DeflectionLimit(250.0, DeflectionCheckDirection.GlobalY),
            });

        Assert.Single(output.DisplacementDiagrams);
        Assert.Single(output.DeflectionChecks);
        Assert.Equal("M1", output.DeflectionChecks[0].MemberId);
    }

    [Fact]
    public void Analyze_WithDisplacementDiagramsDisabled_ShouldReturnOnlyCoreResultAndInternalForcePostProcessing()
    {
        StructuralModel model = CreateSimplySupportedBeam();

        StructuralAnalysisOutput output = new StructuralSolver2DService().AnalyzeLoadCase(
            model,
            "LC1",
            new StructuralAnalysisOptions
            {
                IncludeDisplacementDiagrams = false,
            });

        Assert.NotEmpty(output.Result.Displacements);
        Assert.Single(output.InternalForceDiagrams);
        Assert.Empty(output.DisplacementDiagrams);
        Assert.Empty(output.DeflectionChecks);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void Analyze_WithInvalidSampleCounts_ShouldThrowClearException(int internalForceSamples, int displacementSamples)
    {
        StructuralModel model = CreateSimplySupportedBeam();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StructuralSolver2DService().AnalyzeLoadCase(
                model,
                "LC1",
                new StructuralAnalysisOptions
                {
                    InternalForceSampleCount = internalForceSamples,
                    DisplacementSampleCount = displacementSamples,
                }));
    }

    private static StructuralModel CreateSimplySupportedBeam() =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "First load case"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0));
}
