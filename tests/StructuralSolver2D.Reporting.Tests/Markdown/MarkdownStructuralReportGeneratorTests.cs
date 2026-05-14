using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Markdown;

namespace StructuralSolver2D.Reporting.Tests.Markdown;

public sealed class MarkdownStructuralReportGeneratorTests
{
    [Fact]
    public void Generate_ShouldIncludeMainSectionsAndComputedResults()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        (StructuralAnalysisResult result, IReadOnlyList<MemberInternalForceDiagram> diagrams, StructuralAnalysisSummary summary) = Analyze(model);

        string markdown = new MarkdownStructuralReportGenerator().Generate(
            model,
            result,
            diagrams,
            summary,
            new MarkdownReportOptions
            {
                Title = "Test report",
                Description = "Simply supported benchmark.",
                SourceLabel = "unit-test",
                GeneratedAtUtc = new DateTime(2026, 05, 14, 12, 0, 0, DateTimeKind.Utc),
            });

        Assert.Contains("# Test report", markdown);
        Assert.Contains("**Source:** `unit-test`", markdown);
        Assert.Contains("## Units", markdown);
        Assert.Contains("## Model", markdown);
        Assert.Contains("### Nodes", markdown);
        Assert.Contains("### Members", markdown);
        Assert.Contains("## Results", markdown);
        Assert.Contains("### Support reactions", markdown);
        Assert.Contains("### Maximum absolute results", markdown);
        Assert.Contains("Max \\|M\\| [kNm]", markdown);
        Assert.Contains("31.250000", markdown);
        Assert.Contains("## Notes and limitations", markdown);
    }

    [Fact]
    public void Generate_ShouldWriteLocalMemberEndForcesSectionOnlyOnce()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        (StructuralAnalysisResult result, IReadOnlyList<MemberInternalForceDiagram> diagrams, StructuralAnalysisSummary summary) = Analyze(model);

        string markdown = new MarkdownStructuralReportGenerator().Generate(model, result, diagrams, summary);

        Assert.Equal(1, CountOccurrences(markdown, "### Local member end forces"));
    }

    [Fact]
    public void Generate_ShouldEscapePipeCharactersInUserText()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        model.Nodes[0] = model.Nodes[0] with { Label = "left | support" };
        (StructuralAnalysisResult result, IReadOnlyList<MemberInternalForceDiagram> diagrams, StructuralAnalysisSummary summary) = Analyze(model);

        string markdown = new MarkdownStructuralReportGenerator().Generate(model, result, diagrams, summary);

        Assert.Contains("left \\| support", markdown);
    }

    [Fact]
    public void Generate_ShouldOmitInternalForceSamplesWhenOptionIsDisabled()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        (StructuralAnalysisResult result, IReadOnlyList<MemberInternalForceDiagram> diagrams, StructuralAnalysisSummary summary) = Analyze(model);

        string markdown = new MarkdownStructuralReportGenerator().Generate(
            model,
            result,
            diagrams,
            summary,
            new MarkdownReportOptions { IncludeInternalForceSamples = false });

        Assert.Contains("Internal-force samples are omitted by report options.", markdown);
        Assert.DoesNotContain("| Position | x [m] | N [kN] | V [kN] | M [kNm] |", markdown);
    }

    [Fact]
    public void Generate_ShouldLimitInternalForceSamplesWhenMaxSamplesIsSet()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        (StructuralAnalysisResult result, IReadOnlyList<MemberInternalForceDiagram> diagrams, StructuralAnalysisSummary summary) = Analyze(model, sampleCount: 5);

        string markdown = new MarkdownStructuralReportGenerator().Generate(
            model,
            result,
            diagrams,
            summary,
            new MarkdownReportOptions { MaxSamplesPerMember = 2 });

        Assert.Contains("| ... | ... | ... | ... | ... |", markdown);
    }

    [Fact]
    public void Generate_ShouldIncludeDisplacementSamplesWhenProvidedAndEnabled()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        (StructuralAnalysisResult result, IReadOnlyList<MemberInternalForceDiagram> diagrams, StructuralAnalysisSummary summary) = Analyze(model, sampleCount: 5);
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams = new Frame2DDisplacementSampler().SampleAllMembers(model, result, sampleCount: 5);

        string markdown = new MarkdownStructuralReportGenerator().Generate(
            model,
            result,
            diagrams,
            displacementDiagrams,
            summary,
            new MarkdownReportOptions
            {
                IncludeDisplacementSamples = true,
                MaxDisplacementSamplesPerMember = 3,
            });

        Assert.Contains("## Deformed shape samples", markdown);
        Assert.Contains("| Position | x [m] | u local [m] | v local [m] | rz local [rad] | Ux global [m] | Uy global [m] |", markdown);
        Assert.Contains("finite-element interpolated displacements", markdown);
        Assert.Contains("| ... | ... | ... | ... | ... | ... | ... |", markdown);
    }

    [Fact]
    public void Generate_ShouldOmitDisplacementSamplesWhenOptionIsDisabled()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        (StructuralAnalysisResult result, IReadOnlyList<MemberInternalForceDiagram> diagrams, StructuralAnalysisSummary summary) = Analyze(model, sampleCount: 5);
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams = new Frame2DDisplacementSampler().SampleAllMembers(model, result, sampleCount: 5);

        string markdown = new MarkdownStructuralReportGenerator().Generate(
            model,
            result,
            diagrams,
            displacementDiagrams,
            summary,
            new MarkdownReportOptions { IncludeDisplacementSamples = false });

        Assert.Contains("Displacement samples are omitted by report options.", markdown);
        Assert.DoesNotContain("| Position | x [m] | u local [m] | v local [m] | rz local [rad] | Ux global [m] | Uy global [m] |", markdown);
    }

    private static (StructuralAnalysisResult Result, IReadOnlyList<MemberInternalForceDiagram> Diagrams, StructuralAnalysisSummary Summary) Analyze(
        StructuralModel model,
        int sampleCount = 21)
    {
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");
        IReadOnlyList<MemberInternalForceDiagram> diagrams = new Frame2DInternalForceSampler().SampleAllMembers(model, result, sampleCount);
        StructuralAnalysisSummary summary = new Frame2DResultSummarizer().Summarize(result, diagrams);

        return (result, diagrams, summary);
    }

    private static StructuralModel CreateSimpleSupportedBeam() =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic elastic material", 210_000_000.0))
            .AddSection(new StructuralSection("SEC", "Generic section", 0.003, 0.00002))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("LC1", "Default load case"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0));

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
