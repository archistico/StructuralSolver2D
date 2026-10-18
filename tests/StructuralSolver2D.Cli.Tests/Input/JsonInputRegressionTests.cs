using System.Text.Json.Nodes;
using StructuralSolver2D.Cli.Input;

namespace StructuralSolver2D.Cli.Tests.Input;

public sealed class JsonInputRegressionTests
{
    public static IEnumerable<object[]> RequiredNumbers() => new[]
    {
        ("nodes", "x"), ("nodes", "y"), ("materials", "elasticModulus"),
        ("sections", "area"), ("sections", "momentOfInertia"), ("loads", "value"), ("terms", "factor")
    }.SelectMany(field => new[] { false, true }.Select(explicitNull => new object[] { field.Item1, field.Item2, explicitNull }));

    [Theory]
    [MemberData(nameof(RequiredNumbers))]
    public void Reader_ShouldRejectMissingOrNullRequiredNumber(string collection, string property, bool explicitNull)
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model("combinations/load-combination.json");
        JsonObject item = collection == "terms"
            ? model["loadCombinations"]![0]!["terms"]![0]!.AsObject()
            : model[collection]![0]!.AsObject();
        if (explicitNull) item[property] = null;
        else item.Remove(property);

        var error = Assert.Throws<InvalidOperationException>(() => StructuralModelJsonReader.Read(workspace.Save(model)));
        Assert.Contains(collection, error.Message);
        Assert.Contains(property, error.Message);
    }

    public static IEnumerable<object[]> NullItems() => new[]
        { "nodes", "materials", "sections", "members", "supports", "loadCases", "loads", "loadCombinations", "terms" }
        .SelectMany(collection => new[] { 0, 1 }.Select(index => new object[] { collection, index }));

    [Theory]
    [MemberData(nameof(NullItems))]
    public void Reader_ShouldIdentifyNullItemByPathAndIndex(string collection, int index)
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model("combinations/load-combination.json");
        var items = collection == "terms" ? model["loadCombinations"]![0]!["terms"]!.AsArray() : model[collection]!.AsArray();
        items.Insert(index, null);

        var error = Assert.Throws<InvalidOperationException>(() => StructuralModelJsonReader.Read(workspace.Save(model)));
        string expected = collection == "terms" ? $"loadCombinations[0].terms[{index}]" : $"{collection}[{index}]";
        Assert.Contains(expected, error.Message);
        Assert.Contains("null", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reader_ShouldKeepExplicitZerosAndOptionalDefaults()
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model();
        model["loads"]![0]!["type"] = "LinearDistributedLoad";
        model["loads"]![0]!["targetType"] = "Member";
        model["loads"]![0]!["targetId"] = "M1";
        model["loads"]![0]!["value"] = 0;
        model["loads"]![0]!["endValue"] = -12;
        model["members"]![0]!.AsObject().Remove("type");
        var file = StructuralModelJsonReader.Read(workspace.Save(model));

        Assert.Equal(0, file.Model.Nodes[0].X);
        Assert.Equal(0, file.Model.Nodes[0].Y);
        Assert.Equal(0, file.Model.Loads[0].Value);
        Assert.Equal(0, file.Model.Supports[0].OrientationDegrees);
        Assert.False(file.Model.Members[0].ReleaseStartMoment);
        Assert.Equal(Core.Model.Enums.MemberType.Frame2D, file.Model.Members[0].Type);
    }
}
