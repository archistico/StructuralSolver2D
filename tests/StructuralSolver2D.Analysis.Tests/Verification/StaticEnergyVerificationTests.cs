using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Analysis.Verification;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Verification;

public sealed class StaticEnergyVerificationTests
{
    private const double E = 210_000_000;
    private const double A = 0.003;
    private const double I = 0.00002;

    [Fact]
    public void CantileverTipForce_HasAnalyticStrainEnergy()
    {
        const double length = 3;
        const double force = 10;
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", length, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Fixed("FIX", "A"))
            .AddLoad(StructuralLoad.NodalForce("P", "LC", "B", StructuralLoadDirection.GlobalY, -force));

        var (analysis, check) = new PlaneStructureAnalyzer().AnalyzeWithStaticVerification(model, "LC");
        double expectedTip = -force * Math.Pow(length, 3) / (3 * E * I);
        double expectedEnergy = force * force * Math.Pow(length, 3) / (6 * E * I);
        Assert.True(check.Passed, check.Diagnostic);
        Assert.Equal(expectedTip, analysis.GetDisplacement("B").Uy, precision: 10);
        Assert.Equal(analysis.GetDisplacement("B").Uy,
            new Frame2DAnalyzer().Analyze(model, "LC").GetDisplacement("B").Uy, precision: 11);
        Assert.Equal(expectedEnergy, check.StrainEnergy, precision: 9);
        Assert.Equal(2 * check.StrainEnergy, check.ExternalWork, precision: 9);
    }

    [Theory]
    [InlineData(21_000_000.0, 1.0)]
    [InlineData(210_000_000.0, 3.0)]
    [InlineData(2_100_000_000.0, 8.0)]
    public void ScaledCantilever_PreservesEnergyAndAnalyticTipDeflection(double modulus, double length)
    {
        const double force = 10;
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Material", modulus))
            .AddSection(new StructuralSection("SEC", "Section", A, I))
            .AddLoadCase(new StructuralLoadCase("LC", "Load case"))
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", length, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Fixed("FIX", "A"))
            .AddLoad(StructuralLoad.NodalForce("P", "LC", "B", StructuralLoadDirection.GlobalY, -force));
        var (result, audit) = new PlaneStructureAnalyzer().AnalyzeWithStaticVerification(model, "LC");
        Assert.True(audit.Passed, audit.Diagnostic);
        double expected = -force * length * length * length / (3 * modulus * I);
        double actual = result.GetDisplacement("B").Uy;
        Assert.InRange(Math.Abs((actual - expected) / expected), 0, 1e-7);
    }

    [Fact]
    public void TrussAxialLoad_HasAnalyticStrainEnergy()
    {
        const double length = 5;
        const double force = 100;
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", length, 0))
            .AddMember(new StructuralMember("T", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.NodalForce("P", "LC", "B", StructuralLoadDirection.GlobalX, force));

        var (analysis, check) = new PlaneStructureAnalyzer().AnalyzeWithStaticVerification(model, "LC");
        Assert.True(check.Passed, check.Diagnostic);
        Assert.Equal(analysis.GetDisplacement("B").Ux,
            new Truss2DAnalyzer().Analyze(model, "LC").GetDisplacement("B").Ux, precision: 11);
        Assert.Equal(force * force * length / (2 * E * A), check.StrainEnergy, precision: 9);
    }

    [Fact]
    public void UniformMemberLoad_UsesEquivalentNodalLoads_NotOnlyNodalForces()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 5, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q", "LC", "F", StructuralLoadDirection.GlobalY, -10));
        var (_, check) = new PlaneStructureAnalyzer().AnalyzeWithStaticVerification(model, "LC");
        Assert.True(check.Passed, check.Diagnostic);
        Assert.True(check.StrainEnergy > 0);
        Assert.Equal(2 * check.StrainEnergy, check.ExternalWork, precision: 9);
    }

    [Fact]
    public void ReleasedMember_WithUniformLoad_UsesCondensedStiffnessAndLoad()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D,
                ReleaseStartMoment: true, ReleaseEndMoment: true))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q", "LC", "F", StructuralLoadDirection.GlobalY, -8));
        var (_, check) = new PlaneStructureAnalyzer().AnalyzeWithStaticVerification(model, "LC");
        Assert.True(check.Passed, check.Diagnostic);
        // With both moments released and only end translations restrained, all nodal displacements vanish.
        Assert.Equal(0, check.StrainEnergy, precision: 9);
        Assert.Equal(0, check.ExternalWork, precision: 9);
    }

    [Fact]
    public void MixedPortal_WithInclinedBrace_SatisfiesIndependentResidualAndWork()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddNode(new StructuralNode("C", 0, 3))
            .AddNode(new StructuralNode("D", 4, 3))
            .AddMember(new StructuralMember("F1", "A", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("F2", "C", "D", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("F3", "B", "D", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("T", "A", "D", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.Fixed("SB", "B"))
            .AddLoad(StructuralLoad.NodalForce("PX", "LC", "D", StructuralLoadDirection.GlobalX, 20))
            .AddLoad(StructuralLoad.NodalForce("PY", "LC", "D", StructuralLoadDirection.GlobalY, -10));
        var (_, check) = new PlaneStructureAnalyzer().AnalyzeWithStaticVerification(model, "LC");
        Assert.True(check.Passed, check.Diagnostic);
        Assert.True(check.StrainEnergy > 0);
    }

    [Fact]
    public void InclinedAndRedundantSupports_DoNotCreateFalseResiduals()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 4))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Fixed("S1", "A", orientationDegrees: 30))
            .AddSupport(StructuralSupport.SimpleSupport("S2", "A", orientationDegrees: 30))
            .AddLoad(StructuralLoad.NodalForce("P", "LC", "B", StructuralLoadDirection.GlobalY, -12));
        var (_, check) = new PlaneStructureAnalyzer().AnalyzeWithStaticVerification(model, "LC");
        Assert.True(check.Passed, check.Diagnostic);
        Assert.True(check.StrainEnergy > 0);
    }

    [Fact]
    public void ReverseFrameMemberOrientation_PreservesGlobalEnergyForGlobalLoad()
    {
        StructuralModel Build(bool reversed) => Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 4))
            .AddMember(new StructuralMember("F", reversed ? "B" : "A", reversed ? "A" : "B",
                "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Fixed("S", "A"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q", "LC", "F", StructuralLoadDirection.GlobalY, -4));
        var (forward, forwardCheck) = new PlaneStructureAnalyzer().AnalyzeWithStaticVerification(Build(false), "LC");
        var (reverse, reverseCheck) = new PlaneStructureAnalyzer().AnalyzeWithStaticVerification(Build(true), "LC");
        Assert.True(forwardCheck.Passed, forwardCheck.Diagnostic);
        Assert.True(reverseCheck.Passed, reverseCheck.Diagnostic);
        Assert.Equal(forwardCheck.StrainEnergy, reverseCheck.StrainEnergy, precision: 9);
        Assert.Equal(forward.GetDisplacement("B").Uy, reverse.GetDisplacement("B").Uy, precision: 9);
    }

    [Fact]
    public void CombinedLoads_WithNegativeFactor_PreserveEnergyAndEquilibrium()
    {
        var model = Base()
            .AddLoadCase(new StructuralLoadCase("LC2", "Opposing force"))
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Fixed("S", "A"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q", "LC", "F", StructuralLoadDirection.GlobalY, -4))
            .AddLoad(StructuralLoad.NodalForce("P", "LC2", "B", StructuralLoadDirection.GlobalY, -5))
            .AddLoadCombination(new StructuralLoadCombination("COMB", "Combined",
                new[] { new StructuralLoadCombinationTerm("LC", 1.3),
                    new StructuralLoadCombinationTerm("LC2", -0.4) }));
        var (analysis, check) = new PlaneStructureAnalyzer().AnalyzeCombinationWithStaticVerification(model, "COMB");
        Assert.True(check.Passed, check.Diagnostic);
        Assert.Equal(analysis.GetDisplacement("B").Uy, new PlaneStructureAnalyzer().AnalyzeCombination(model, "COMB").GetDisplacement("B").Uy, precision: 12);
        Assert.True(check.StrainEnergy > 0);
    }

    [Fact]
    public void ZeroLoads_HaveZeroEnergyAndNoDivisionByZero()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Fixed("S1", "A"));
        var (_, check) = new PlaneStructureAnalyzer().AnalyzeWithStaticVerification(model, "LC");
        Assert.True(check.Passed, check.Diagnostic);
        Assert.Equal(0, check.StrainEnergy);
        Assert.Equal(0, check.ExternalWork);
    }

    [Fact]
    public void TamperedStiffnessAndLoad_ProduceIndependentDiagnostics()
    {
        double[,] k = { { 2, -1 }, { -1, 2 } };
        double[] u = { 0, 0.5 };
        double[] f = { 0, 1 }; // first DOF is constrained, with reaction -0.5
        IReadOnlyList<double[]> restraints = new[] { new double[] { 1, 0 } };
        Assert.True(StaticSystemVerifier.Verify(k, f, u, restraints).Passed);

        double[,] nonsymmetric = { { 2, 3 }, { -1, 2 } };
        var symmetry = StaticSystemVerifier.Verify(nonsymmetric, f, u, restraints);
        Assert.False(symmetry.Passed);
        Assert.False(symmetry.SymmetryPassed);

        var wrongLoad = StaticSystemVerifier.Verify(k, new[] { 0.0, 2.0 }, u, restraints);
        Assert.False(wrongLoad.Passed);
        Assert.False(wrongLoad.EnergyPassed);
        Assert.False(wrongLoad.EquilibriumPassed);

        var wrongConstraint = StaticSystemVerifier.Verify(k, f, new[] { 0.2, 0.5 }, restraints);
        Assert.False(wrongConstraint.ConstraintsPassed);
    }

    [Fact]
    public void NonfiniteInput_IsReportedWithoutNaNComparisons()
    {
        var report = StaticSystemVerifier.Verify(new[,] { { double.NaN } },
            new[] { 0.0 }, new[] { 0.0 }, Array.Empty<double[]>());
        Assert.False(report.Passed);
        Assert.Contains("Nonfinite", report.Diagnostic);
    }

    private static StructuralModel Base() => new StructuralModel()
        .AddMaterial(new StructuralMaterial("MAT", "Material", E))
        .AddSection(new StructuralSection("SEC", "Section", A, I))
        .AddLoadCase(new StructuralLoadCase("LC", "Load case"));
}
