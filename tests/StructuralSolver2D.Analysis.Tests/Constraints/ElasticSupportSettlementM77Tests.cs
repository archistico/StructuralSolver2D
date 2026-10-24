using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Equilibrium;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.Tests.Constraints;

/// <summary>Analytical checks: series springs, imposed displacements and factored superposition.</summary>
public sealed class ElasticSupportSettlementM77Tests
{
    private const double BarStiffness = 1000;  // EA/L [kN/m]
    private const double SpringStiffness = 2000; // [kN/m]

    private static StructuralModel Model(MemberType type = MemberType.Truss2D)
    {
        return new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 0))
            .AddMaterial(new StructuralMaterial("MAT", "elastic", 200000))
            .AddSection(new StructuralSection("SEC", "area", 0.01, 0.000001))
            .AddMember(new StructuralMember("BAR", "A", "B", "MAT", "SEC", type))
            .AddSupport(StructuralSupport.Hinge("A_FIXED", "A"))
            .AddSupport(new StructuralSupport("B_VERTICAL", "B", false, true, type == MemberType.Frame2D))
            .AddSupport(new StructuralSupport("B_SPRING", "B", false, false, false,
                StiffnessUx: SpringStiffness))
            .AddLoadCase(new StructuralLoadCase("Q", "point load"))
            .AddLoadCase(new StructuralLoadCase("S", "settlement"));
    }

    [Theory]
    [InlineData(MemberType.Truss2D)]
    [InlineData(MemberType.Frame2D)]
    public void AxialSupportSpring_ActsInSeriesAndReportsGroundReaction(MemberType type)
    {
        StructuralModel model = Model(type).AddLoad(StructuralLoad.NodalForce(
            "F", "Q", "B", StructuralLoadDirection.GlobalX, 30));
        var result = type == MemberType.Truss2D
            ? new Truss2DAnalyzer().Analyze(model, "Q")
            : new Frame2DAnalyzer().Analyze(model, "Q");
        double expected = 30.0 / (BarStiffness + SpringStiffness);
        Assert.Equal(expected, result.GetDisplacement("B").Ux, 10);
        Assert.Equal(-SpringStiffness * expected, result.GetReaction("B_SPRING").Fx, 8);
        Assert.Equal(-BarStiffness * expected, result.GetReaction("A_FIXED").Fx, 8);
    }

    [Fact]
    public void PrescribedSupportSettlement_LoadCaseProducesReactionsWithoutAppliedForces()
    {
        StructuralModel model = Model().AddLoad(StructuralLoad.SupportSettlementLoad(
            "SET", "S", "A_FIXED", StructuralLoadDirection.LocalX, 0.012));
        var result = new Truss2DAnalyzer().Analyze(model, "S");
        double expectedB = (BarStiffness * 0.012) / (BarStiffness + SpringStiffness);
        Assert.Equal(0.012, result.GetDisplacement("A").Ux, 10);
        Assert.Equal(expectedB, result.GetDisplacement("B").Ux, 10);
        Assert.Equal(-SpringStiffness * expectedB, result.GetReaction("B_SPRING").Fx, 8);
        Assert.Equal(SpringStiffness * expectedB, result.GetReaction("A_FIXED").Fx, 8);
        var equilibrium = new GlobalEquilibriumChecker().Check(model, result);
        Assert.True(equilibrium.IsInEquilibrium(1e-7),
            $"M77 settlement reactions must balance: Fx={equilibrium.ResidualFx}, Fy={equilibrium.ResidualFy}, Mz={equilibrium.ResidualMz}");
    }

    [Fact]
    public void ManualCombination_SettlementAndForceUseTheirOwnFactorsIncludingNegative()
    {
        StructuralModel model = Model()
            .AddLoad(StructuralLoad.SupportSettlementLoad("SET", "S", "A_FIXED", StructuralLoadDirection.LocalX, 0.012))
            .AddLoad(StructuralLoad.NodalForce("F", "Q", "B", StructuralLoadDirection.GlobalX, 30))
            .AddLoadCombination(new StructuralLoadCombination("COMB", "combined", new[]
            {
                new StructuralLoadCombinationTerm("S", 1.5),
                new StructuralLoadCombinationTerm("Q", -0.5),
            }));
        var result = new Truss2DAnalyzer().AnalyzeCombination(model, "COMB");
        double expected = (BarStiffness * 0.018 - 15.0) / (BarStiffness + SpringStiffness);
        Assert.Equal(0.018, result.GetDisplacement("A").Ux, 10);
        Assert.Equal(expected, result.GetDisplacement("B").Ux, 10);
        Assert.Equal(-SpringStiffness * expected, result.GetReaction("B_SPRING").Fx, 8);
    }

    [Fact]
    public void ConflictingSettlementsOnRedundantRestraints_AreRejected()
    {
        StructuralModel model = Model();
        model.AddSupport(new StructuralSupport("A_REPEAT", "A", true, false, false));
        model.AddLoad(StructuralLoad.SupportSettlementLoad("SET", "S", "A_FIXED", StructuralLoadDirection.LocalX, 0.01));
        var error = Assert.ThrowsAny<Exception>(() => new Truss2DAnalyzer().Analyze(model, "S"));
        Assert.Contains("M77_INCOMPATIBLE_SETTLEMENT", error.Message);
    }

    [Fact]
    public void SpringAndRigidRestraintOnSameAxis_AreRejectedBeforeSolve()
    {
        StructuralModel model = Model();
        model.Supports[0] = model.Supports[0] with { StiffnessUx = 100 };
        var validation = new StructuralModelValidator().Validate(model);
        Assert.Contains(validation.Issues, issue => issue.Code == "SUPPORT_SPRING_AND_RESTRAINT");
    }

    [Fact]
    public void RotationalSpringOnFrame2D_MomentRotationMatchesClosedForm()
    {
        const double moment = 1.0;
        const double spring = 0.6; // kNm/rad
        var model = Model(MemberType.Frame2D);
        model.Supports.Clear();
        model.AddSupport(StructuralSupport.Fixed("SA", "A"));
        model.AddSupport(new StructuralSupport("SB", "B", false, true, false,
            StiffnessRz: spring));
        model.AddLoad(StructuralLoad.NodalMoment("M", "Q", "B", moment));
        var result = new Frame2DAnalyzer().Analyze(model, "Q");
        double ei = 200000.0 * .000001;
        double rotationalStiffness = 4.0 * ei / 2.0;
        double rotation = moment / (rotationalStiffness + spring);
        Assert.Equal(rotation, result.GetDisplacement("B").Rz, 9);
        Assert.Equal(-spring * rotation, result.GetReaction("SB").Mz, 9);
    }

    [Fact]
    public void RepeatedAnalysis_DoesNotAccumulateElasticSupportStiffness()
    {
        var model = Model().AddLoad(StructuralLoad.NodalForce("F", "Q", "B", StructuralLoadDirection.GlobalX, 30));
        var analyzer = new Truss2DAnalyzer();
        var first = analyzer.Analyze(model, "Q");
        var second = analyzer.Analyze(model, "Q");
        Assert.Equal(first.GetDisplacement("B").Ux, second.GetDisplacement("B").Ux, 12);
        Assert.Equal(first.GetReaction("B_SPRING").Fx, second.GetReaction("B_SPRING").Fx, 12);
    }

    [Fact]
    public void InclinedElasticSupport_HasSymmetricProjectedStiffness()
    {
        StructuralModel model = new StructuralModel().AddNode(new StructuralNode("A", 0, 0))
            .AddSupport(new StructuralSupport("S", "A", false, false, false,
                OrientationDegrees: 45, StiffnessUx: 100));
        var k = new double[2, 2];
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["A"] = 0 };
        StructuralSolver2D.Analysis.Constraints.SupportConstraintSystem.AddSupportSpringsForVerification(
            model, index, 2, k, includeRotationalDof: false);
        Assert.Equal(50.0, k[0, 0], 9);
        Assert.Equal(50.0, k[1, 1], 9);
        Assert.Equal(50.0, k[0, 1], 9);
        Assert.Equal(k[0, 1], k[1, 0], 12);
    }

    [Fact]
    public void MixedStructure_UsesTheSameSpringAndSettlementContracts()
    {
        StructuralModel model = Model(MemberType.Frame2D)
            .AddLoad(StructuralLoad.SupportSettlementLoad("SET", "S", "A_FIXED", StructuralLoadDirection.LocalX, 0.01));
        var result = new PlaneStructureAnalyzer().Analyze(model, "S");
        Assert.Equal(0.01, result.GetDisplacement("A").Ux, 10);
        Assert.Equal(BarStiffness * 0.01 / (BarStiffness + SpringStiffness), result.GetDisplacement("B").Ux, 9);
    }
}
