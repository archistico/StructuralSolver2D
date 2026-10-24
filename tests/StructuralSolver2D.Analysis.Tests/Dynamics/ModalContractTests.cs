using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Tests.Dynamics;

public sealed class ModalContractTests
{
    [Fact]
    public void Request_SeparatesDynamicMetadataFromStaticAnalysisTarget()
    {
        var model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 1, 0))
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 77))
            .AddSection(new StructuralSection("SEC", "Rectangular", 0.01, 1e-4))
            .AddMember(new StructuralMember("M", "A", "B", "MAT", "SEC"));
        var request = new StructuralModalAnalysisRequest(model, new StructuralDynamicsDefinition(), 2);
        Assert.True(request.Validate().IsValid);
        Assert.Equal(2, request.RequestedModeCount);
        Assert.Empty(model.Loads);
    }

    [Fact]
    public void InvalidRequestedModeCount_IsRejected()
    {
        var request = new StructuralModalAnalysisRequest(new StructuralModel(), new StructuralDynamicsDefinition(), 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => request.Validate());
    }

    [Fact]
    public void OutputContract_HasConsistentFrequencyAndPeriod_AndDoesNotImplyAmplitude()
    {
        var mode = new StructuralModalModeResult(1, 2 * Math.PI,
            new[] { new StructuralModalNodeShape("A", 0, 0.3, 0) });
        Assert.Equal(1, mode.FrequencyHertz, 12);
        Assert.Equal(1, mode.PeriodSeconds, 12);
        Assert.Single(new StructuralModalAnalysisOutput(new[] { mode }).Modes);
    }
}
