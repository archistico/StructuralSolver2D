using System.Text.Json;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Dynamics;

public sealed class StructuralDynamicsValidatorTests
{
    [Fact]
    public void ValidFrame_WithMasses_IsAccepted_AndStaticDefinitionUnchanged()
    {
        StructuralModel model = Beam();
        var definition = new StructuralDynamicsDefinition(1,
            new[] { new StructuralNodalMass("ADD", "B", 0.25, 0.03) });

        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
        Assert.True(new StructuralDynamicsValidator().Validate(model, definition).IsValid);
        Assert.Equal(1, definition.SchemaVersion);
        Assert.Single(definition.AdditionalNodalMasses);
        Assert.Empty(model.Loads); // UnitWeight does not create a static self-weight load.
    }

    [Fact]
    public void MissingUnitWeight_IsAllowedStatically_ButRejectedForDynamicsEvenWithNodalMass()
    {
        StructuralModel model = Beam(unitWeight: null);
        var definition = new StructuralDynamicsDefinition(NodalMasses: new[] { new StructuralNodalMass("N", "B", 3) });

        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
        CheckCode(model, definition, "DYNAMIC_MISSING_UNIT_WEIGHT");
    }

    [Fact]
    public void NoMembers_IsRejectedForFutureModalRequest()
    {
        var empty = new StructuralModel();
        CheckCode(empty, new StructuralDynamicsDefinition(), "DYNAMIC_NO_MEMBERS");
    }

    [Fact]
    public void NullNodalEntry_IsReportedWithItsIndex()
    {
        StructuralNodalMass[] masses = { null! };
        CheckCode(Beam(), new StructuralDynamicsDefinition(NodalMasses: masses), "DYNAMIC_NODAL_MASS_NULL");
    }

    [Fact]
    public void UnsupportedVersion_RequiresExplicitMigration()
    {
        CheckCode(Beam(), new StructuralDynamicsDefinition(2), "DYNAMIC_SCHEMA_UNSUPPORTED");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void BadNodalTranslationalMass_IsRejected(double mass)
    {
        CheckCode(Beam(), new StructuralDynamicsDefinition(NodalMasses:
            new[] { new StructuralNodalMass("X", "B", mass) }), "DYNAMIC_NODAL_MASS_INVALID");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    public void BadRotationalInertia_IsRejected(double inertia)
    {
        CheckCode(Beam(), new StructuralDynamicsDefinition(NodalMasses:
            new[] { new StructuralNodalMass("X", "B", 0.1, inertia) }), "DYNAMIC_NODAL_INERTIA_INVALID");
    }

    [Fact]
    public void UnknownNodeAndDuplicateEntries_AreRejected()
    {
        var definition = new StructuralDynamicsDefinition(NodalMasses: new[]
        {
            new StructuralNodalMass("mass", "A", 0.1),
            new StructuralNodalMass("MASS", "a", 0.2),
            new StructuralNodalMass("Other", "Missing", 0.3)
        });
        StructuralModelValidationResult result = new StructuralDynamicsValidator().Validate(Beam(), definition);
        Assert.Contains(result.Errors, issue => issue.Code == "DYNAMIC_NODAL_MASS_DUPLICATE_ID");
        Assert.Contains(result.Errors, issue => issue.Code == "DYNAMIC_NODAL_MASS_DUPLICATE_NODE");
        Assert.Contains(result.Errors, issue => issue.Code == "DYNAMIC_NODAL_MASS_NODE_NOT_FOUND");
    }

    [Fact]
    public void TrussOnlyNode_CannotAcceptRotationalInertia()
    {
        StructuralModel model = Beam();
        model.Members[0] = model.Members[0] with { Type = MemberType.Truss2D };
        var dynamics = new StructuralDynamicsDefinition(NodalMasses: new[] { new StructuralNodalMass("J", "A", 0.2, 0.1) });
        CheckCode(model, dynamics, "DYNAMIC_NODAL_INERTIA_TRUSS_ONLY");
    }

    [Fact]
    public void DisconnectedNodalMass_IsRejected()
    {
        StructuralModel model = Beam();
        model.AddNode(new StructuralNode("C", 20, 0));
        CheckCode(model, new StructuralDynamicsDefinition(NodalMasses:
            new[] { new StructuralNodalMass("J", "C", 0.2) }), "DYNAMIC_NODAL_MASS_DISCONNECTED");
    }

    [Fact]
    public void MissingGeometryOrMaterial_ProducesStructuralValidationIssueNotCrash()
    {
        StructuralModel model = Beam();
        model.Materials.Clear();
        StructuralModelValidationResult result = new StructuralDynamicsValidator().Validate(model, new StructuralDynamicsDefinition());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "MEMBER_MATERIAL_NOT_FOUND");
    }

    [Fact]
    public void DynamicDefinition_RoundTripsJson_WithoutSerializingComputedAlias()
    {
        var definition = new StructuralDynamicsDefinition(1,
            new[] { new StructuralNodalMass("T1", "A", 2.5, 0.4) });
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string json = JsonSerializer.Serialize(definition, options);
        Assert.Contains("\"nodalMasses\"", json);
        Assert.DoesNotContain("additionalNodalMasses", json);
        StructuralDynamicsDefinition? restored = JsonSerializer.Deserialize<StructuralDynamicsDefinition>(json, options);
        Assert.NotNull(restored);
        Assert.Equal(definition.SchemaVersion, restored.SchemaVersion);
        Assert.Equal(definition.AdditionalNodalMasses[0], Assert.Single(restored.AdditionalNodalMasses));
    }

    private static void CheckCode(StructuralModel model, StructuralDynamicsDefinition definition, string expectedCode)
    {
        StructuralModelValidationResult result = new StructuralDynamicsValidator().Validate(model, definition);
        Assert.Contains(result.Errors, issue => issue.Code == expectedCode);
    }

    private static StructuralModel Beam(double? unitWeight = 25.0)
    {
        return new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddMaterial(new StructuralMaterial("CONC", "Concrete", 30_000_000, unitWeight))
            .AddSection(new StructuralSection("SEC", "Beam", 0.12, 0.0012))
            .AddMember(new StructuralMember("M1", "A", "B", "CONC", "SEC"))
            .AddSupport(StructuralSupport.Fixed("S", "A"));
    }
}
