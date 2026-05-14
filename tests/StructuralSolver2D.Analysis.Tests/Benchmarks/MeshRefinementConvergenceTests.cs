using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// Validates mesh-refinement behavior on simple beam problems with closed-form reference values.
/// These tests intentionally distinguish between nodal FEM results and interpolated internal values.
/// </summary>
public sealed class MeshRefinementConvergenceTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 2e-05;

    [Fact]
    public void SimplySupportedBeam_Udl_MidspanSample_ShouldImproveWhenMidspanIsExplicitNode()
    {
        const double length = 5.0;
        const double load = 10.0;
        double expectedMidspanDeflection = -(5.0 * load * Math.Pow(length, 4)) / (384.0 * ElasticModulus * Inertia);

        StructuralModel coarseModel = CreateSimplySupportedBeamWithUniformLoad(length, load, divisions: 1);
        StructuralAnalysisResult coarseResult = new Frame2DAnalyzer().Analyze(coarseModel, "LC1");
        double coarseMidspanUy = SampleGlobalUyAtX(coarseModel, coarseResult, length / 2.0);

        StructuralModel refinedModel = CreateSimplySupportedBeamWithUniformLoad(length, load, divisions: 2);
        StructuralAnalysisResult refinedResult = new Frame2DAnalyzer().Analyze(refinedModel, "LC1");
        double refinedMidspanUy = refinedResult.GetDisplacement("N1").Uy;

        double coarseError = Math.Abs(coarseMidspanUy - expectedMidspanDeflection);
        double refinedError = Math.Abs(refinedMidspanUy - expectedMidspanDeflection);

        Assert.True(
            refinedError < coarseError,
            $"Expected the two-element model with an explicit midspan node to improve the midspan deflection. Coarse error={coarseError}, refined error={refinedError}.");

        Assert.Equal(expectedMidspanDeflection, refinedMidspanUy, precision: 9);
    }

    [Fact]
    public void SimplySupportedBeam_Udl_MidspanDeflection_ShouldRemainStableAfterFurtherRefinement()
    {
        const double length = 5.0;
        const double load = 10.0;
        double expectedMidspanDeflection = -(5.0 * load * Math.Pow(length, 4)) / (384.0 * ElasticModulus * Inertia);

        foreach (int divisions in new[] { 2, 4, 8 })
        {
            StructuralModel model = CreateSimplySupportedBeamWithUniformLoad(length, load, divisions);
            StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");
            double midspanUy = result.GetDisplacement($"N{divisions / 2}").Uy;

            Assert.Equal(expectedMidspanDeflection, midspanUy, precision: 9);
        }
    }

    [Fact]
    public void SimplySupportedBeam_PointLoad_NotAtNode_ShouldImproveWhenLoadPointIsExplicitNode()
    {
        const double length = 6.0;
        const double load = 30.0;
        const double loadPosition = 2.0;

        double a = loadPosition;
        double b = length - loadPosition;
        double expectedLoadPointDeflection = -(load * a * a * b * b) / (3.0 * ElasticModulus * Inertia * length);

        StructuralModel coarseModel = CreateSimplySupportedBeamWithMemberPointLoad(length, load, loadPosition);
        StructuralAnalysisResult coarseResult = new Frame2DAnalyzer().Analyze(coarseModel, "LC1");
        double coarseUyAtLoadPoint = SampleGlobalUyAtX(coarseModel, coarseResult, loadPosition);

        StructuralModel splitModel = CreateSimplySupportedBeamWithExplicitPointLoadNode(length, load, loadPosition);
        StructuralAnalysisResult splitResult = new Frame2DAnalyzer().Analyze(splitModel, "LC1");
        double splitUyAtLoadPoint = splitResult.GetDisplacement("P").Uy;

        double coarseError = Math.Abs(coarseUyAtLoadPoint - expectedLoadPointDeflection);
        double splitError = Math.Abs(splitUyAtLoadPoint - expectedLoadPointDeflection);

        Assert.True(
            splitError < coarseError,
            $"Expected the model with an explicit load node to improve the load-point deflection. Coarse error={coarseError}, split error={splitError}.");

        Assert.Equal(expectedLoadPointDeflection, splitUyAtLoadPoint, precision: 9);
    }

    [Fact]
    public void SimplySupportedBeam_Udl_ReactionsAndMoment_ShouldRemainConsistentAcrossMeshRefinement()
    {
        const double length = 5.0;
        const double load = 10.0;
        const double expectedReaction = 25.0;
        const double expectedMaxMoment = 31.25;

        foreach (int divisions in new[] { 1, 2, 4, 8 })
        {
            StructuralModel model = CreateSimplySupportedBeamWithUniformLoad(length, load, divisions);
            StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");

            Assert.Equal(expectedReaction, result.Reactions.Single(reaction => reaction.NodeId == "N0").Fy, precision: 9);
            Assert.Equal(expectedReaction, result.Reactions.Single(reaction => reaction.NodeId == $"N{divisions}").Fy, precision: 9);

            IReadOnlyList<MemberInternalForceDiagram> diagrams = new Frame2DInternalForceSampler().SampleAllMembers(model, result, sampleCount: 101);
            StructuralAnalysisSummary summary = new Frame2DResultSummarizer().Summarize(result, diagrams);

            Assert.Equal(expectedMaxMoment, Math.Abs(summary.MaxAbsBendingMoment.Value), precision: 6);
        }
    }

    private static StructuralModel CreateSimplySupportedBeamWithUniformLoad(double length, double load, int divisions)
    {
        StructuralModel model = CreateBaseFrameModel();
        double step = length / divisions;

        for (int index = 0; index <= divisions; index++)
        {
            model.AddNode(new StructuralNode($"N{index}", index * step, 0.0));
        }

        for (int index = 0; index < divisions; index++)
        {
            string memberId = $"M{index + 1}";
            model.AddMember(new StructuralMember(memberId, $"N{index}", $"N{index + 1}", "MAT", "SEC", MemberType.Frame2D));
            model.AddLoad(StructuralLoad.UniformDistributedLoad($"Q{index + 1}", "LC1", memberId, StructuralLoadDirection.GlobalY, -load));
        }

        model.AddSupport(StructuralSupport.Hinge("S0", "N0"));
        model.AddSupport(StructuralSupport.SimpleSupport($"S{divisions}", $"N{divisions}"));

        return model;
    }

    private static StructuralModel CreateSimplySupportedBeamWithMemberPointLoad(double length, double load, double loadPosition)
    {
        StructuralModel model = CreateBaseFrameModel();

        model.AddNode(new StructuralNode("A", 0.0, 0.0));
        model.AddNode(new StructuralNode("B", length, 0.0));
        model.AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D));
        model.AddSupport(StructuralSupport.Hinge("SA", "A"));
        model.AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
        model.AddLoad(StructuralLoad.PointLoadOnMember("P1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load, loadPosition / length));

        return model;
    }

    private static StructuralModel CreateSimplySupportedBeamWithExplicitPointLoadNode(double length, double load, double loadPosition)
    {
        StructuralModel model = CreateBaseFrameModel();

        model.AddNode(new StructuralNode("A", 0.0, 0.0));
        model.AddNode(new StructuralNode("P", loadPosition, 0.0));
        model.AddNode(new StructuralNode("B", length, 0.0));
        model.AddMember(new StructuralMember("M1", "A", "P", "MAT", "SEC", MemberType.Frame2D));
        model.AddMember(new StructuralMember("M2", "P", "B", "MAT", "SEC", MemberType.Frame2D));
        model.AddSupport(StructuralSupport.Hinge("SA", "A"));
        model.AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
        model.AddLoad(StructuralLoad.NodalForce("P1", "LC1", "P", StructuralLoadDirection.GlobalY, -load));

        return model;
    }

    private static StructuralModel CreateBaseFrameModel() =>
        new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Benchmark elastic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Benchmark frame section", Area, Inertia))
            .AddLoadCase(new StructuralLoadCase("LC1", "Benchmark load case"));

    private static double SampleGlobalUyAtX(StructuralModel model, StructuralAnalysisResult result, double x)
    {
        StructuralMember member = model.Members.Single(candidate =>
        {
            StructuralNode start = model.Nodes.Single(node => node.Id == candidate.StartNodeId);
            StructuralNode end = model.Nodes.Single(node => node.Id == candidate.EndNodeId);
            double minX = Math.Min(start.X, end.X) - 1e-9;
            double maxX = Math.Max(start.X, end.X) + 1e-9;

            return x >= minX && x <= maxX;
        });

        StructuralNode memberStart = model.Nodes.Single(node => node.Id == member.StartNodeId);
        StructuralNode memberEnd = model.Nodes.Single(node => node.Id == member.EndNodeId);
        double memberLength = Math.Abs(memberEnd.X - memberStart.X);
        double normalizedPosition = Math.Abs(x - memberStart.X) / memberLength;

        MemberDisplacementDiagram diagram = new Frame2DDisplacementSampler().SampleMember(model, result, member.Id, sampleCount: 301);
        MemberDisplacementSample sample = diagram.Samples.MinBy(sample => Math.Abs(sample.NormalizedPosition - normalizedPosition))
            ?? throw new InvalidOperationException("No displacement sample was generated.");

        return sample.GlobalUy;
    }
}
