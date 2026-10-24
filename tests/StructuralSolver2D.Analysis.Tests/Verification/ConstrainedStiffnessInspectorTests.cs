using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Verification;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Verification;

public sealed class ConstrainedStiffnessInspectorTests
{
    [Theory]
    [InlineData(1e-100)]
    [InlineData(1.0)]
    [InlineData(1e100)]
    public void PositiveDefiniteStiffness_IsHealthyAtUniformScales(double scale)
    {
        var k = new[,] { { 2.0 * scale, -scale }, { -scale, 2.0 * scale } };
        StiffnessInspectionResult result = ConstrainedStiffnessInspector.Inspect(k, Array.Empty<double[]>());
        Assert.Equal(StiffnessInspectionStatus.Healthy, result.Status);
        Assert.Equal(2, result.FreeDofCount);
        Assert.InRange(result.ScaledEigenvalueRatio!.Value, 0.33333333 - 1e-7, 0.33333333 + 1e-7);
    }

    [Fact]
    public void DuplicateConstraints_AreNotMisdiagnosedAsAnInstability()
    {
        var k = new[,] { { 3.0, -1.0 }, { -1.0, 2.0 } };
        IReadOnlyList<double[]> constraints = new[]
        {
            new[] { 1.0, 0.0 }, new[] { -2.0, 0.0 }, new[] { 1.0, 0.0 }
        };
        var result = ConstrainedStiffnessInspector.Inspect(k, constraints);
        Assert.Equal(StiffnessInspectionStatus.Healthy, result.Status);
        Assert.Equal(1, result.FreeDofCount);
        Assert.Equal(2, result.RedundantConstraintRows);
    }

    [Fact]
    public void InclinedConstraint_UsesProjectedNotPrincipalSubmatrix()
    {
        // u_x + u_y = 0, so admissible vector has equal/opposite components.
        // The projected stiffness has just one positive value.
        var k = new[,] { { 4.0, 1.0 }, { 1.0, 4.0 } };
        var result = ConstrainedStiffnessInspector.Inspect(k,
            new[] { new[] { 1.0, 1.0 } }, new[] { "N.Ux", "N.Uy" });
        Assert.Equal(StiffnessInspectionStatus.Healthy, result.Status);
        Assert.Equal(1, result.FreeDofCount);
        Assert.Equal(1.0, result.ScaledEigenvalueRatio!.Value, precision: 12);
    }

    [Fact]
    public void RigidTranslationOrInternalMechanism_IsDetectedBeforeSolve()
    {
        var k = new[,] { { 2.0, -2.0 }, { -2.0, 2.0 } };
        var result = ConstrainedStiffnessInspector.Inspect(k, Array.Empty<double[]>(),
            new[] { "A.Ux", "B.Ux" });
        Assert.Equal(StiffnessInspectionStatus.PossibleMechanism, result.Status);
        Assert.Contains("Ux", result.DominantDof);
        Assert.True(result.ScaledEigenvalueRatio <= 1e-10);
    }

    [Fact]
    public void ExtremelySoftMode_IsDistinguishedFromSeverelySmallButPositiveMode()
    {
        var soft = new[,] { { 1.0, 1 - 1e-7 }, { 1 - 1e-7, 1.0 } };
        var almostSingular = new[,] { { 1.0, 1 - 1e-13 }, { 1 - 1e-13, 1.0 } };
        Assert.Equal(StiffnessInspectionStatus.PoorlyConditioned,
            ConstrainedStiffnessInspector.Inspect(soft, Array.Empty<double[]>()).Status);
        Assert.Equal(StiffnessInspectionStatus.PossibleMechanism,
            ConstrainedStiffnessInspector.Inspect(almostSingular, Array.Empty<double[]>()).Status);
    }

    [Fact]
    public void IndefiniteMatrix_DoesNotPassStabilityInspection()
    {
        var k = new[,] { { 1.0, 0.0 }, { 0.0, -2.0 } };
        var result = ConstrainedStiffnessInspector.Inspect(k, Array.Empty<double[]>());
        Assert.Equal(StiffnessInspectionStatus.PossibleMechanism, result.Status);
        Assert.Contains("non-positive", result.Diagnostic);
    }

    [Fact]
    public void SymmetricMatrixWithNegativeEigenvalueAndPositiveDiagonal_IsIndefinite()
    {
        var k = new[,] { { 1.0, 2.0 }, { 2.0, 1.0 } };
        Assert.Equal(StiffnessInspectionStatus.Indefinite,
            ConstrainedStiffnessInspector.Inspect(k, Array.Empty<double[]>()).Status);
    }

    [Fact]
    public void FullyConstrainedModel_HasNoFreeModes()
    {
        var k = new[,] { { 2.0, -1.0 }, { -1.0, 3.0 } };
        var result = ConstrainedStiffnessInspector.Inspect(k,
            new[] { new[] { 1.0, 0.0 }, new[] { 0.0, 1.0 } });
        Assert.Equal(StiffnessInspectionStatus.FullyConstrained, result.Status);
        Assert.Equal(0, result.FreeDofCount);
    }

    [Fact]
    public void InvalidOrOversizeMatrix_HasExplicitNonMisleadingStatus()
    {
        Assert.Equal(StiffnessInspectionStatus.InvalidInput,
            ConstrainedStiffnessInspector.Inspect(new[,] { { double.NaN } }, Array.Empty<double[]>()).Status);
        Assert.Equal(StiffnessInspectionStatus.InvalidInput,
            ConstrainedStiffnessInspector.Inspect(new[,] { { 1.0, 4.0 }, { 0.0, 1.0 } }, Array.Empty<double[]>()).Status);
        int n = ConstrainedStiffnessInspector.MaximumFreeDofs + 1;
        var k = new double[n, n];
        for (int i = 0; i < n; i++) k[i, i] = 1;
        Assert.Equal(StiffnessInspectionStatus.TooLarge,
            ConstrainedStiffnessInspector.Inspect(k, Array.Empty<double[]>()).Status);
    }

    [Fact]
    public void PlaneFrameWithoutSupports_IsFlaggedByRealAssemblyEvenAtZeroLoad()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D));
        var result = new PlaneStructureAnalyzer().InspectStaticStability(model, "LC");
        Assert.Equal(StiffnessInspectionStatus.PossibleMechanism, result.Status);
        Assert.NotNull(result.DominantDof);
    }

    [Fact]
    public void SupportedAxialTruss_IsStableAndInactiveRotationsAreExcluded()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddMember(new StructuralMember("T", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("PIN", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("ROLL", "B"));
        var result = new PlaneStructureAnalyzer().InspectStaticStability(model, "LC");
        Assert.Equal(StiffnessInspectionStatus.Healthy, result.Status);
        Assert.Equal(1, result.FreeDofCount);
    }

    [Fact]
    public void RedundantInclinedSupport_OnFixedEnd_DoesNotBecomeMechanism()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 4))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Fixed("FIX", "A", orientationDegrees: 30))
            .AddSupport(StructuralSupport.SimpleSupport("RED", "A", orientationDegrees: 30));
        var result = new PlaneStructureAnalyzer().InspectStaticStability(model, "LC");
        Assert.Equal(StiffnessInspectionStatus.Healthy, result.Status);
        Assert.Equal(3, result.FreeDofCount);
    }

    [Fact]
    public void PinnedFrameWithUnrestrainedHorizontalTranslation_IsMechanism()
    {
        var model = Base()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.SimpleSupport("RA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("RB", "B"));
        var result = new PlaneStructureAnalyzer().InspectStaticStability(model, "LC");
        Assert.Equal(StiffnessInspectionStatus.PossibleMechanism, result.Status);
    }

    private static StructuralModel Base() => new StructuralModel()
        .AddMaterial(new StructuralMaterial("MAT", "Material", 210_000_000))
        .AddSection(new StructuralSection("SEC", "Section", 0.003, 0.00002))
        .AddLoadCase(new StructuralLoadCase("LC", "Static load case"));
}
