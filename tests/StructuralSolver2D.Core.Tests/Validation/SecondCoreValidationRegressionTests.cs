using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Model.Generators;
using StructuralSolver2D.Core.Model.Sections;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Validation;

public sealed class SecondCoreValidationRegressionTests
{
    public static IEnumerable<object[]> Dimensions() => new[] { "Height", "Width" }
        .SelectMany(property => new double?[] { null, .2, 0, -.2, double.NaN, double.PositiveInfinity, double.NegativeInfinity }
            .Select(value => new object[] { property, value! }));

    [Theory]
    [MemberData(nameof(Dimensions))]
    public void OptionalDimensions_ShouldAgreeWithCatalog(string property, double? value)
    {
        var model = Beam();
        var section = property == "Height" ? model.Sections[0] with { Height = value } : model.Sections[0] with { Width = value };
        model.Sections[0] = section;
        bool expected = value is null || value > 0 && double.IsFinite(value.Value);
        var validation = new StructuralModelValidator().Validate(model);
        Assert.Equal(expected, validation.IsValid);
        if (expected) Assert.Single(StructuralSectionCatalog.FromSections(model.Sections).Sections);
        else
        {
            Assert.Contains(validation.Errors, issue => issue.Code == "SECTION_INVALID_" + property.ToUpperInvariant() && issue.EntityId == section.Id);
            Assert.Throws<ArgumentOutOfRangeException>(() => StructuralSectionCatalog.FromSections(model.Sections));
        }
    }

    public static IEnumerable<object[]> NullContainers() => new[] { "Nodes", "Members", "Materials", "Sections", "Supports", "LoadCases", "Loads", "LoadCombinations", "Terms" }
        .SelectMany(name => new[] { 0, 1 }.Select(index => new object[] { name, index }));

    [Theory]
    [MemberData(nameof(NullContainers))]
    public void MutableCollections_ShouldReportNullEntriesAndOtherErrorsWithoutMutation(string collection, int index)
    {
        var model = Beam();
        var combination = new StructuralLoadCombination("ULS", "ULS", new[] { new StructuralLoadCombinationTerm("LC1", 1.5) });
        model.AddLoadCombination(combination);
        var list = collection == "Terms" ? (System.Collections.IList)combination.Terms
            : (System.Collections.IList)typeof(StructuralModel).GetProperty(collection)!.GetValue(model)!;
        list.Insert(index, null);
        int count = list.Count;
        var material = model.Materials.First(item => item is not null);
        model.Materials[model.Materials.IndexOf(material)] = material with { Name = "" };
        var result = new StructuralModelValidator().Validate(model);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Message.Contains(collection + "[" + index + "]", StringComparison.Ordinal));
        Assert.Contains(result.Errors, issue => issue.Code == "MATERIAL_EMPTY_NAME");
        Assert.Equal(count, list.Count);
        Assert.Null(list[index]);
    }

    [Theory]
    [InlineData("AddNode")]
    [InlineData("AddMember")]
    [InlineData("AddMaterial")]
    [InlineData("AddSection")]
    [InlineData("AddSupport")]
    [InlineData("AddLoadCase")]
    [InlineData("AddLoad")]
    [InlineData("AddLoadCombination")]
    public void FluentAdd_ShouldRejectNullImmediately(string method)
    {
        var model = Beam();
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => typeof(StructuralModel).GetMethod(method)!.Invoke(model, new object?[] { null }));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
    }

    [Fact]
    public void CombinationConstructor_ShouldRejectNullTerm()
    {
        Assert.Throws<ArgumentException>(() => new StructuralLoadCombination("ULS", "ULS", new StructuralLoadCombinationTerm[] { null! }));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Support_ShouldRejectUndefinedType(int type)
    {
        var model = Beam(); model.Supports[0] = model.Supports[0] with { Type = (SupportType)type };
        Assert.Contains(new StructuralModelValidator().Validate(model).Errors, issue => issue.Code == "SUPPORT_INVALID_TYPE");
    }

    [Theory]
    [InlineData(SupportType.Custom)]
    [InlineData(SupportType.Free)]
    [InlineData(SupportType.SimpleSupport)]
    [InlineData(SupportType.Hinge)]
    [InlineData(SupportType.Roller)]
    [InlineData(SupportType.Fixed)]
    public void SupportType_ShouldPreserveExplicitMechanicalFlags(SupportType type)
    {
        var model = Beam(); var support = model.Supports[0] with { Type = type }; model.Supports[0] = support;
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
        Assert.Same(support, model.Supports[0]);
    }

    private static StructuralModel Beam() => ParametricBeamGenerator.SimplySupportedBeam(5, 1)
        .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10));
}
