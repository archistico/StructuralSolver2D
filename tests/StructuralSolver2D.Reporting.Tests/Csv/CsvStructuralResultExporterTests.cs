using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Csv;

namespace StructuralSolver2D.Reporting.Tests.Csv;

public sealed class CsvStructuralResultExporterTests
{
    [Fact]
    public void ExportNodalDisplacements_ShouldWriteHeaderAndInvariantNumbers()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        (StructuralAnalysisResult result, _, _) = Analyze(model);

        string csv = new CsvStructuralResultExporter().ExportNodalDisplacements(result);

        Assert.StartsWith("LoadCaseId,NodeId,Ux_m,Uy_m,Rz_rad", csv);
        Assert.Contains("LC1,A,0.000000,0.000000", csv);
        Assert.Contains("LC1,B,0.000000,0.000000", csv);
    }

    [Fact]
    public void ExportSupportReactions_ShouldWriteReactionRows()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        (StructuralAnalysisResult result, _, _) = Analyze(model);

        string csv = new CsvStructuralResultExporter().ExportSupportReactions(result);

        Assert.StartsWith("LoadCaseId,SupportId,NodeId,Fx_kN,Fy_kN,Mz_kNm", csv);
        Assert.Contains("LC1,SA,A,0.000000,25.000000,0.000000", csv);
        Assert.Contains("LC1,SB,B,0.000000,25.000000,0.000000", csv);
    }

    [Fact]
    public void ExportMemberEndForces_ShouldWriteLocalEndForceRows()
    {
        var result = new StructuralAnalysisResult(
            "LC1",
            Array.Empty<NodalDisplacementResult>(),
            Array.Empty<SupportReactionResult>(),
            new[]
            {
                new MemberEndForceResult(
                    "M1",
                    1.25,
                    -2.50,
                    3.75,
                    -4.25,
                    5.50,
                    -6.75),
            });

        string csv = new CsvStructuralResultExporter().ExportMemberEndForces(result);

        Assert.StartsWith("LoadCaseId,MemberId,StartAxial_kN,StartShear_kN,StartMoment_kNm,EndAxial_kN,EndShear_kN,EndMoment_kNm", csv);
        Assert.Contains("LC1,M1,1.250000,-2.500000,3.750000,-4.250000,5.500000,-6.750000", csv);
    }

    [Fact]
    public void ExportInternalForceSamples_ShouldWriteAllSamples()
    {
        var diagrams = new[]
        {
            new MemberInternalForceDiagram(
                "M1",
                length: 5.0,
                new[]
                {
                    new MemberInternalForceSample("M1", 0.0, 0.0, 1.0, -2.0, 3.0),
                    new MemberInternalForceSample("M1", 0.5, 2.5, -4.0, 5.0, -6.0),
                    new MemberInternalForceSample("M1", 1.0, 5.0, 7.0, -8.0, 9.0),
                }),
        };

        string csv = new CsvStructuralResultExporter().ExportInternalForceSamples("LC1", diagrams);

        Assert.StartsWith("AnalysisId,MemberId,Position,Distance_m,NormalForce_kN,ShearForce_kN,BendingMoment_kNm", csv);
        Assert.Equal(4, CountLines(csv));
        Assert.Contains("LC1,M1,0.500000,2.500000,-4.000000,5.000000,-6.000000", csv);
    }

    [Fact]
    public void ExportDisplacementSamples_ShouldWriteAllSamples()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        (StructuralAnalysisResult result, _, _) = Analyze(model, sampleCount: 3);
        IReadOnlyList<MemberDisplacementDiagram> diagrams = new Frame2DDisplacementSampler().SampleAllMembers(model, result, sampleCount: 3);

        string csv = new CsvStructuralResultExporter().ExportDisplacementSamples(result.LoadCaseId, diagrams);

        Assert.StartsWith("AnalysisId,MemberId,Position,Distance_m,LocalUx_m,LocalUy_m,LocalRz_rad,GlobalUx_m,GlobalUy_m", csv);
        Assert.Equal(4, CountLines(csv));
        Assert.Contains("LC1,M1,0.500000,2.500000", csv);
    }

    [Fact]
    public void ExportSummary_ShouldWriteGoverningValues()
    {
        StructuralModel model = CreateSimpleSupportedBeam();
        (StructuralAnalysisResult result, _, StructuralAnalysisSummary summary) = Analyze(model);

        string csv = new CsvStructuralResultExporter().ExportSummary(summary);

        Assert.StartsWith("AnalysisId,Quantity,EntityId,MemberId,Position,Distance_m,Value,Unit", csv);
        Assert.Contains("LC1,MaxAbsUy,A,,,,0.000000,m", csv);
        Assert.Contains("LC1,MaxAbsBendingMoment,,M1,0.500000,2.500000,31.250000,kNm", csv);
        Assert.DoesNotContain(';', csv);
        Assert.Equal(result.LoadCaseId, summary.LoadCaseId);
    }

    [Fact]
    public void ExportNodalDisplacements_ShouldEscapeCsvSpecialCharacters()
    {
        var result = new StructuralAnalysisResult(
            "LC,1",
            new[] { new NodalDisplacementResult("A\"1", 1.0, 2.0, 3.0) },
            Array.Empty<SupportReactionResult>(),
            Array.Empty<MemberEndForceResult>());

        string csv = new CsvStructuralResultExporter().ExportNodalDisplacements(result);

        Assert.Contains("\"LC,1\",\"A\"\"1\",1.000000,2.000000,3.000000", csv);
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

    private static int CountLines(string text) =>
        text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length;
}
