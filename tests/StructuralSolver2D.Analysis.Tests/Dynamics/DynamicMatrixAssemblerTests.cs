using StructuralSolver2D.Analysis.Dynamics.Assembly;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using static StructuralSolver2D.Analysis.Tests.Dynamics.ElementMassMatrixTests;

namespace StructuralSolver2D.Analysis.Tests.Dynamics;

public sealed class DynamicMatrixAssemblerTests
{
    [Fact]
    public void SingleTrussGlobalMass_HasTrussOnlyMasslessRotationsAndFiniteStiffness()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 4))
            .AddMember(new StructuralMember("T", "A", "B", "MAT", "SEC", MemberType.Truss2D));
        var a = DynamicMatrixAssembler.Assemble(model, new StructuralDynamicsDefinition());
        Assert.Equal(new[] { 2, 5 }, a.MasslessDofIndices);
        double total = DynamicMassUnits.MemberMassFromUnitWeight(77, 0.02, 5);
        AssertClose(total, a.MemberTranslationalMass);
        AssertClose(total / 3, a.Mass[0, 0]);
        AssertClose(total / 6, a.Mass[0, 3]);
        AssertClose(total / 3, a.Mass[1, 1]);
        AssertClose(total / 6, a.Mass[1, 4]);
        AssertClose(0, a.Mass[2, 2]);
        AssertClose(0, a.Stiffness[2, 2]);
        AssertClose(210_000_000*0.02/5*0.6*0.6, a.Stiffness[0, 0]);
    }

    [Fact]
    public void MixedModel_AddsEachNodalMassOnceAndPreservesNonNegativeKineticEnergy()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddNode(new StructuralNode("C", 4, 3))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("T", "B", "C", "MAT", "SEC", MemberType.Truss2D));
        var baseline = DynamicMatrixAssembler.Assemble(model, new StructuralDynamicsDefinition());
        var data = new StructuralDynamicsDefinition(NodalMasses: new[] {
            new StructuralNodalMass("EQUIPMENT", "B", 2.5, 0.15) });
        var a = DynamicMatrixAssembler.Assemble(model, data);
        double expected = DynamicMassUnits.MemberMassFromUnitWeight(77, 0.02, 7);
        AssertClose(expected, a.MemberTranslationalMass);
        AssertClose(2.5, a.AddedTranslationalMass);
        Assert.Equal(new[] { 8 }, a.MasslessDofIndices); // C.Rz is a truss-only rotation.
        AssertClose(2.5, a.Mass[3, 3] - baseline.Mass[3, 3]);
        AssertClose(2.5, a.Mass[4, 4] - baseline.Mass[4, 4]);
        AssertClose(0.15, a.Mass[5, 5] - baseline.Mass[5, 5]);
        AssertClose(0, a.Mass[0, 0] - baseline.Mass[0, 0]);
        AssertClose(expected + 2.5, Quadratic(a.Mass,
            new[] { 1.0, 0, 0, 1, 0, 0, 1, 0, 0 }));
        AssertClose(expected + 2.5, Quadratic(a.Mass,
            new[] { 0.0, 1, 0, 0, 1, 0, 0, 1, 0 }));
        double[] probe = { 2, -1, 0.4, 0.1, 0.7, -0.25, 0.6, 1.2, 0 };
        Assert.True(Quadratic(a.Mass, probe) > 0);
        CompareMatrices(baseline.Stiffness, a.Stiffness);
    }

    [Theory]
    [InlineData(MemberType.Frame2D, 0, 5)]
    [InlineData(MemberType.Frame2D, 3, 4)]
    [InlineData(MemberType.Truss2D, 3, 4)]
    public void ReversedMemberOrientation_YieldsSameGlobalMatrices(MemberType type, double dx, double dy)
    {
        var forward = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", dx, dy))
            .AddMember(new StructuralMember("E", "A", "B", "MAT", "SEC", type));
        var reversed = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", dx, dy))
            .AddMember(new StructuralMember("E", "B", "A", "MAT", "SEC", type));
        var fa = DynamicMatrixAssembler.Assemble(forward, new StructuralDynamicsDefinition());
        var fb = DynamicMatrixAssembler.Assemble(reversed, new StructuralDynamicsDefinition());
        CompareMatrices(fa.Stiffness, fb.Stiffness, 2e-10);
        CompareMatrices(fa.Mass, fb.Mass, 2e-10);
    }

    [Fact]
    public void UniformSubdivision_KeepsTotalPhysicalMass()
    {
        var coarse = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 6, 0))
            .AddMember(new StructuralMember("E", "A", "B", "MAT", "SEC"));
        var fine = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("C", 2, 0))
            .AddNode(new StructuralNode("D", 4, 0))
            .AddNode(new StructuralNode("B", 6, 0))
            .AddMember(new StructuralMember("E1", "A", "C", "MAT", "SEC"))
            .AddMember(new StructuralMember("E2", "C", "D", "MAT", "SEC"))
            .AddMember(new StructuralMember("E3", "D", "B", "MAT", "SEC"));
        var c = DynamicMatrixAssembler.Assemble(coarse, new StructuralDynamicsDefinition());
        var f = DynamicMatrixAssembler.Assemble(fine, new StructuralDynamicsDefinition());
        AssertClose(c.MemberTranslationalMass, f.MemberTranslationalMass);
        AssertClose(c.MemberTranslationalMass, Quadratic(c.Mass,
            new double[] { 1, 0, 0, 1, 0, 0 }));
        AssertClose(f.MemberTranslationalMass, Quadratic(f.Mass,
            new double[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0 }));
    }

    [Fact]
    public void ReleasedFrame_IsRejectedBeforeAnyMassCondensation()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D,
                ReleaseStartMoment: true));
        var exception = Assert.Throws<NotSupportedException>(() =>
            DynamicMatrixAssembler.Assemble(model, new StructuralDynamicsDefinition()));
        Assert.Contains("M53_RELEASE_UNSUPPORTED", exception.Message);
    }

    [Fact]
    public void MissingMaterialUnitWeight_IsNotReplacedByNodalMass()
    {
        var model = new StructuralModel().AddMaterial(new StructuralMaterial("MAT", "No mass", 210_000_000))
            .AddSection(new StructuralSection("SEC", "Section", 0.02, 0.0001))
            .AddNode(new StructuralNode("A", 0, 0)).AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC"));
        var error = Assert.Throws<InvalidOperationException>(() =>
            DynamicMatrixAssembler.Assemble(model, new StructuralDynamicsDefinition()));
        Assert.Contains("DYNAMIC_MISSING_UNIT_WEIGHT", error.Message);
    }

    [Fact]
    public void EducationalLumpedFrame_MarksRotationalDofsAsMassless()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC"));
        var consistent = DynamicMatrixAssembler.Assemble(model, new StructuralDynamicsDefinition());
        var lumped = DynamicMatrixAssembler.Assemble(model, new StructuralDynamicsDefinition(),
            ElementMassFormulation.EducationalLumped);
        Assert.Empty(consistent.MasslessDofIndices);
        Assert.Equal(new[] { 2, 5 }, lumped.MasslessDofIndices);
        AssertClose(consistent.MemberTranslationalMass, lumped.MemberTranslationalMass);
    }

    private static StructuralModel Base() => new StructuralModel()
        .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 77))
        .AddSection(new StructuralSection("SEC", "Section", 0.02, 0.0001));
}
