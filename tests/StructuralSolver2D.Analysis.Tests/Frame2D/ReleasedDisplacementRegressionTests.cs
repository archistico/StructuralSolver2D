using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Serviceability;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

public sealed class ReleasedDisplacementRegressionTests
{
    public static IEnumerable<object[]> ReleasesAndLoads()
    {
        foreach (bool start in new[] { false, true })
        foreach (bool end in new[] { false, true })
        {
            if (!start && !end) continue;
            foreach (string load in new[] { "Uniform", "Linear", "Point" })
            foreach (bool combination in new[] { false, true })
            foreach (bool plane in new[] { false, true })
                yield return new object[] { start, end, load, combination, plane };
        }
    }

    [Theory]
    [MemberData(nameof(ReleasesAndLoads))]
    public void ReleasedDisplacement_ShouldMatchEquivalentUnreleasedBeam(bool releaseStart, bool releaseEnd, string load, bool combination, bool plane)
    {
        var original = Model(load);
        var released = Model(load);
        released.Members[0] = released.Members[0] with { ReleaseStartMoment = releaseStart, ReleaseEndMoment = releaseEnd };

        StructuralSolver2D.Analysis.Results.StructuralAnalysisResult Analyze(StructuralModel model) => plane
            ? combination ? new PlaneStructureAnalyzer().AnalyzeCombination(model, "COMB") : new PlaneStructureAnalyzer().Analyze(model, "LC1")
            : combination ? new Frame2DAnalyzer().AnalyzeCombination(model, "COMB") : new Frame2DAnalyzer().Analyze(model, "LC1");
        var sampler = new Frame2DDisplacementSampler();
        var expected = sampler.SampleMember(original, Analyze(original), "M1", 11);
        var actual = sampler.SampleMember(released, Analyze(released), "M1", 11);

        Assert.True(expected.MaxAbsGlobalUy > 0);
        for (int i = 0; i < expected.Samples.Count; i++)
        {
            Assert.Equal(expected.Samples[i].GlobalUy, actual.Samples[i].GlobalUy, precision: 10);
            Assert.Equal(expected.Samples[i].LocalRz, actual.Samples[i].LocalRz, precision: 10);
        }
    }

    [Fact]
    public void Service_ShouldFailDeflectionLimitForPinEndedLoadedBeam()
    {
        var model = Model("Uniform");
        model.Members[0] = model.Members[0] with { ReleaseStartMoment = true, ReleaseEndMoment = true };
        var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1",
            new StructuralAnalysisOptions { DeflectionLimit = new DeflectionLimit(1000) });

        Assert.Equal(10 * Math.Pow(5, 4) / (96.0 * 210_000_000 * .00002), output.DisplacementDiagrams[0].MaxAbsGlobalUy, precision: 10);
        Assert.Equal(DeflectionCheckStatus.Fail, output.DeflectionChecks[0].Status);
    }

    [Fact]
    public void ReleasedMember_ShouldRecoverItsOwnRotationAtSharedFixedNode()
    {
        var model = Model("Uniform");
        model.Supports.Clear();
        model.AddSupport(StructuralSupport.Fixed("S1", "A"));
        model.AddSupport(StructuralSupport.Fixed("S2", "B"));
        model.Members[0] = model.Members[0] with { ReleaseStartMoment = true, ReleaseEndMoment = true };
        model.AddNode(new StructuralNode("C", 0, 3));
        model.AddMember(new StructuralMember("M2", "A", "C", "MAT", "SEC"));
        model.AddSupport(StructuralSupport.Fixed("SC", "C"));
        var result = new PlaneStructureAnalyzer().Analyze(model, "LC1");
        var sampler = new Frame2DDisplacementSampler();
        var released = sampler.SampleMember(model, result, "M1", 5);
        var rigid = sampler.SampleMember(model, result, "M2", 5);

        Assert.Equal(0, result.GetDisplacement("A").Rz, precision: 10);
        Assert.Equal(-10 * Math.Pow(5, 3) / (24.0 * 210_000_000 * .00002), released.Samples[0].LocalRz, precision: 10);
        Assert.Equal(0, rigid.Samples[0].LocalRz, precision: 10);
    }

    [Theory]
    [InlineData(30.0, StructuralLoadDirection.GlobalX)]
    [InlineData(30.0, StructuralLoadDirection.GlobalY)]
    [InlineData(75.0, StructuralLoadDirection.GlobalX)]
    [InlineData(75.0, StructuralLoadDirection.GlobalY)]
    public void ReleasedRotations_ShouldRespectLoadAndSupportCoordinateSystems(double angle, StructuralLoadDirection direction)
    {
        var original = Model("Linear");
        double radians = angle * Math.PI / 180;
        original.Nodes[1] = original.Nodes[1] with { X = 5 * Math.Cos(radians), Y = 5 * Math.Sin(radians) };
        original.Supports[1] = original.Supports[1] with { OrientationDegrees = angle };
        original.Loads[0] = original.Loads[0] with { Direction = direction };
        var analyzer = new Frame2DAnalyzer();
        var sampler = new Frame2DDisplacementSampler();
        var expected = sampler.SampleMember(original, analyzer.AnalyzeCombination(original, "COMB"), "M1", 5);
        original.Members[0] = original.Members[0] with { ReleaseStartMoment = true, ReleaseEndMoment = true };
        var actual = sampler.SampleMember(original, analyzer.AnalyzeCombination(original, "COMB"), "M1", 5);

        for (int i = 0; i < expected.Samples.Count; i++)
        {
            Assert.Equal(expected.Samples[i].GlobalUx, actual.Samples[i].GlobalUx, precision: 10);
            Assert.Equal(expected.Samples[i].GlobalUy, actual.Samples[i].GlobalUy, precision: 10);
            Assert.Equal(expected.Samples[i].LocalRz, actual.Samples[i].LocalRz, precision: 10);
        }
    }

    private static StructuralModel Model(string load)
    {
        var model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0)).AddNode(new StructuralNode("B", 5, 0))
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000))
            .AddSection(new StructuralSection("SEC", "Section", .003, .00002))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"))
            .AddSupport(StructuralSupport.Hinge("S1", "A")).AddSupport(StructuralSupport.SimpleSupport("S2", "B"))
            .AddLoadCase(new StructuralLoadCase("LC1", "First"))
            .AddLoadCase(new StructuralLoadCase("LC2", "Second"))
            .AddLoadCombination(new StructuralLoadCombination("COMB", "Combination",
                new[] { new StructuralLoadCombinationTerm("LC1", 1.5), new StructuralLoadCombinationTerm("LC2", -0.5) }));
        model.AddLoad(load switch
        {
            "Point" => StructuralLoad.PointLoadOnMember("Q", "LC1", "M1", StructuralLoadDirection.GlobalY, -10, .35),
            "Linear" => StructuralLoad.LinearDistributedLoad("Q", "LC1", "M1", StructuralLoadDirection.GlobalY, -3, -10),
            _ => StructuralLoad.UniformDistributedLoad("Q", "LC1", "M1", StructuralLoadDirection.GlobalY, -10)
        });
        model.AddLoad(StructuralLoad.PointLoadOnMember("OTHER", "LC2", "M1", StructuralLoadDirection.LocalY, -4, .7));
        return model;
    }
}
