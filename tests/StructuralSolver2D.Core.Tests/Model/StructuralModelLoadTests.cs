using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Core.Tests.Model;

public sealed class StructuralModelLoadTests
{
    [Fact]
    public void AddLoadCaseAndLoad_ShouldAppendEntitiesAndReturnModel()
    {
        StructuralModel model = new();
        StructuralLoadCase loadCase = new("LC1", "Permanent loads");
        StructuralLoad load = StructuralLoad.NodalForce("L1", "LC1", "A", StructuralLoadDirection.GlobalY, -10.0);

        StructuralModel returnedAfterLoadCase = model.AddLoadCase(loadCase);
        StructuralModel returnedAfterLoad = model.AddLoad(load);

        Assert.Same(model, returnedAfterLoadCase);
        Assert.Same(model, returnedAfterLoad);
        Assert.Single(model.LoadCases);
        Assert.Single(model.Loads);
        Assert.Equal(loadCase, model.LoadCases[0]);
        Assert.Equal(load, model.Loads[0]);
    }
}
