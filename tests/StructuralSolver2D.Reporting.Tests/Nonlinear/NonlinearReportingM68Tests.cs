using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Reporting.Nonlinear;

namespace StructuralSolver2D.Reporting.Tests.Nonlinear;

public sealed class NonlinearReportingM68Tests
{
    [Fact]
    public void IncompleteStaticHistory_IsUnambiguouslyLabeledInReportAndContainsNoRejectedStep()
    {
        var output = new StructuralNonlinearStaticOutput(false, "M64_NONCONVERGENCE", new[]{"A","B"}, new[]{"AB"},
            new[]{new StructuralNonlinearStaticStep(0, 1, 2, 1e-12,
                new[]{0d,0d, .002,0d}, new[]{-2d,0d,0d,0d}, new[]{2d},
                new[]{new StructuralNonlinearMaterialState(0,0)}, 0, 0)},
            new[]{new StructuralNonlinearMaterialState(0,0)});
        var exporter = new NonlinearTruss2DReportExporter();
        string markdown = exporter.StaticMarkdown(output, "Research | case\nA");
        Assert.Contains("NO — partial accepted history only", markdown);
        Assert.Contains("M64_NONCONVERGENCE",markdown);
        Assert.Contains("Research \\| case A",markdown);
        Assert.Contains("Final accepted member states",markdown);
        Assert.Single(markdown.Split("| 0 | 1 |", StringSplitOptions.None).Skip(1));
        string csv = exporter.StaticMembersCsv(output);
        Assert.Contains("\"AB\",2,0,0,0,0",csv);
        Assert.DoesNotContain("NaN",csv);
    }

    [Fact]
    public void EmptyFailedResult_DoesNotIndexLastStep()
    {
        var output = new StructuralNonlinearStaticOutput(false, "NO_STEPS", new[]{"A"},new[]{"E"},
            Array.Empty<StructuralNonlinearStaticStep>(), new[]{new StructuralNonlinearMaterialState(0,0)});
        string report = new NonlinearTruss2DReportExporter().StaticMarkdown(output);
        Assert.Contains("NO_STEPS",report);
        Assert.DoesNotContain("Final accepted nodal response",report);
    }
}
