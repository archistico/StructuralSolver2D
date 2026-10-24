using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Reporting.Envelope;

namespace StructuralSolver2D.Reporting.Tests.Csv;

public sealed class StaticForceEnvelopeReportM79Tests
{
    [Fact]
    public void CsvAndMarkdown_PreserveGoverningScenarioSignsAndUnits()
    {
        var extreme = new GoverningForceExtremeM79(
            "B1", StructuralAnalysisTargetKind.LoadCombination, "ULS",
            InternalForceQuantity.BendingMoment, -12.5, 2, .5,
            SectionLimitM78.AfterPointLoad);
        // The exporter writes all three quantities; preserve the same synthetic value for each.
        var entries = new[]
        {
            extreme with { Quantity = InternalForceQuantity.NormalForce },
            extreme with { Quantity = InternalForceQuantity.ShearForce },
            extreme,
        };
        var result = new StructuralStaticForceEnvelopeM79(
            new[] { new StaticForceEnvelopeScenarioM79(StructuralAnalysisTargetKind.LoadCombination, "ULS") },
            new[] { new FrameMemberForceEnvelopeM79("B1", entries, entries, entries) });
        StaticForceEnvelopeReportM79 exporter = new();
        string csv = exporter.ToCsv(result);
        string markdown = exporter.ToMarkdown(result);
        Assert.Contains("\"B1\",BendingMoment,Minimum,-12.5,kNm,2,0.5,AfterPointLoad,LoadCombination,\"ULS\"", csv);
        Assert.Contains("-12.5", markdown);
        Assert.Contains("ULS", markdown);
        Assert.Contains("Non sono curve", markdown);
    }

    [Fact]
    public void CsvAndMarkdown_EscapeUserControlledIdentifiers()
    {
        var entries = new[]
        {
            new GoverningForceExtremeM79("=1+1", StructuralAnalysisTargetKind.LoadCase, "@evil",
                InternalForceQuantity.NormalForce, 0, 0, 0, SectionLimitM78.Continuous),
            new GoverningForceExtremeM79("=1+1", StructuralAnalysisTargetKind.LoadCase, "@evil",
                InternalForceQuantity.ShearForce, 0, 0, 0, SectionLimitM78.Continuous),
            new GoverningForceExtremeM79("=1+1", StructuralAnalysisTargetKind.LoadCase, "@evil",
                InternalForceQuantity.BendingMoment, 0, 0, 0, SectionLimitM78.Continuous),
        };
        var envelope = new StructuralStaticForceEnvelopeM79(
            new[] { new StaticForceEnvelopeScenarioM79(StructuralAnalysisTargetKind.LoadCase, "@evil") },
            new[] { new FrameMemberForceEnvelopeM79("=1+1", entries, entries, entries) });
        string csv = new StaticForceEnvelopeReportM79().ToCsv(envelope);
        Assert.Contains("\"'=1+1\"", csv);
        Assert.Contains("\"'@evil\"", csv);
        string md = new StaticForceEnvelopeReportM79().ToMarkdown(envelope, "Danger|Title");
        Assert.Contains("Danger\\|Title", md);
    }
}
