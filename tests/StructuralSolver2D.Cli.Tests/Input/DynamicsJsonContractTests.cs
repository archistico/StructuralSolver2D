using System.Text.Json;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Cli.Tests.Input;

public sealed class DynamicsJsonContractTests
{
    private const string Sample = """
    {
      "title": "Mass contract",
      "loadCaseId": "LC1",
      "nodes": [{"id":"A","x":0,"y":0},{"id":"B","x":4,"y":0}],
      "materials": [{"id":"MAT","name":"Concrete","elasticModulus":30000000,"unitWeight":25}],
      "sections": [{"id":"SEC","name":"Beam","area":0.12,"momentOfInertia":0.0012}],
      "members": [{"id":"M","startNodeId":"A","endNodeId":"B","materialId":"MAT","sectionId":"SEC"}],
      "supports": [{"id":"SA","nodeId":"A","restrainedUx":true,"restrainedUy":true,"restrainedRz":true}],
      "loadCases": [{"id":"LC1","name":"Loaded case"}],
      "loads": [{"id":"P","loadCaseId":"LC1","type":"NodalForce","targetType":"Node","targetId":"B","direction":"GlobalY","value":-10}]
    }
    """;

    [Fact]
    public void LegacyJsonWithoutDynamics_StillLoadsAsStaticModel()
    {
        WithFile(Sample, path =>
        {
            StructuralModelJsonFile model = StructuralModelJsonReader.Read(path);
            Assert.Null(model.Dynamics);
            Assert.True(new StructuralModelValidator().Validate(model.Model).IsValid);
            Assert.Equal("LC1", model.LoadCaseId);
        });
    }

    [Fact]
    public void VersionedDynamicsJson_ReadsAndRoundTripsDynamicBlock()
    {
        var definition = new StructuralDynamicsDefinition(1,
            new[] { new StructuralNodalMass("EXTRA", "B", 0.5, 0.02) });
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string json = Sample.Replace("\n}", ",\n  \"dynamics\": " + JsonSerializer.Serialize(definition, options) + "\n}");
        WithFile(json, path =>
        {
            StructuralModelJsonFile parsed = StructuralModelJsonReader.Read(path);
            Assert.NotNull(parsed.Dynamics);
            Assert.Equal(definition.AdditionalNodalMasses[0], Assert.Single(parsed.Dynamics.AdditionalNodalMasses));
            Assert.True(new StructuralDynamicsValidator().Validate(parsed.Model, parsed.Dynamics).IsValid);
        });
    }

    [Fact]
    public void OptionalMassMetadata_DoesNotChangeStaticSolverNumbers()
    {
        string augmentedJson = Sample.Replace("\n}",
            ",\n  \"dynamics\": {\"schemaVersion\": 1, \"nodalMasses\": [{\"id\":\"MASS\",\"nodeId\":\"B\",\"translationalMass\":1}]}\n}");
        WithFile(Sample, baselinePath => WithFile(augmentedJson, augmentedPath =>
        {
            var baseline = StructuralModelJsonReader.Read(baselinePath);
            var augmented = StructuralModelJsonReader.Read(augmentedPath);
            var solver = new StructuralSolver2DService();
            var baseResult = solver.AnalyzeLoadCase(baseline.Model, baseline.LoadCaseId).Result;
            var dynamicMetadataResult = solver.AnalyzeLoadCase(augmented.Model, augmented.LoadCaseId).Result;
            Assert.Equal(baseResult.Displacements.ToArray(), dynamicMetadataResult.Displacements.ToArray());
            Assert.Equal(baseResult.Reactions.ToArray(), dynamicMetadataResult.Reactions.ToArray());
            Assert.Equal(baseResult.MemberEndForces.ToArray(), dynamicMetadataResult.MemberEndForces.ToArray());
        }));
    }

    [Theory]
    [InlineData("\"nodalMasses\":[{\"id\":\"X\",\"nodeId\":\"A\"}]", "translationalMass")]
    [InlineData("\"nodalMasses\":[null]", "cannot be null")]
    public void MalformedMassEntries_FailWithActionableJsonPath(string contents, string message)
    {
        string json = Sample.Replace("\n}", ",\n\"dynamics\": {" + contents + "}\n}");
        WithFile(json, path =>
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => StructuralModelJsonReader.Read(path));
            Assert.Contains(message, exception.Message);
        });
    }

    [Fact]
    public void UnknownDynamicField_IsRejectedRatherThanIgnored()
    {
        string json = Sample.Replace("\n}", ",\n\"dynamics\": {\"schemaVersion\": 1, \"unmodeledForcing\": []}\n}");
        WithFile(json, path => Assert.Throws<JsonException>(() => StructuralModelJsonReader.Read(path)));
    }

    [Fact]
    public void FutureSchema_IsParsedButRejectedByValidator()
    {
        string json = Sample.Replace("\n}", ",\n\"dynamics\": {\"schemaVersion\": 2}\n}");
        WithFile(json, path =>
        {
            var parsed = StructuralModelJsonReader.Read(path);
            Assert.NotNull(parsed.Dynamics);
            var result = new StructuralDynamicsValidator().Validate(parsed.Model, parsed.Dynamics);
            Assert.Contains(result.Errors, issue => issue.Code == "DYNAMIC_SCHEMA_UNSUPPORTED");
        });
    }

    private static void WithFile(string content, Action<string> action)
    {
        string directory = Path.Combine(Path.GetTempPath(), "StructuralSolver2D.M52", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "input.json");
        try
        {
            File.WriteAllText(path, content);
            action(path);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
