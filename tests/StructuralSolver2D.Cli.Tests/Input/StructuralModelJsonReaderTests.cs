using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Cli.Tests.Input;

public sealed class StructuralModelJsonReaderTests
{
    [Fact]
    public void Read_ShouldLoadValidStructuralModelFile()
    {
        string path = WriteTempJson("""
        {
          "title": "JSON benchmark",
          "description": "Simply supported beam loaded from JSON.",
          "loadCaseId": "LC1",
          "nodes": [
            { "id": "A", "x": 0.0, "y": 0.0 },
            { "id": "B", "x": 5.0, "y": 0.0 }
          ],
          "materials": [
            { "id": "MAT", "name": "Generic elastic material", "elasticModulus": 210000000.0 }
          ],
          "sections": [
            { "id": "SEC", "name": "Generic section", "area": 0.003, "momentOfInertia": 0.00002 }
          ],
          "members": [
            { "id": "M1", "startNodeId": "A", "endNodeId": "B", "materialId": "MAT", "sectionId": "SEC", "type": "Frame2D" }
          ],
          "supports": [
            { "id": "SA", "nodeId": "A", "restrainedUx": true, "restrainedUy": true, "restrainedRz": false, "type": "Hinge" },
            { "id": "SB", "nodeId": "B", "restrainedUx": false, "restrainedUy": true, "restrainedRz": false, "type": "SimpleSupport" }
          ],
          "loadCases": [
            { "id": "LC1", "name": "Default load case" }
          ],
          "loads": [
            { "id": "Q1", "loadCaseId": "LC1", "type": "UniformDistributedLoad", "targetType": "Member", "targetId": "M1", "direction": "GlobalY", "value": -10.0 }
          ]
        }
        """);

        StructuralModelJsonFile file = StructuralModelJsonReader.Read(path);

        Assert.Equal("JSON benchmark", file.Title);
        Assert.Equal("Simply supported beam loaded from JSON.", file.Description);
        Assert.Equal("LC1", file.LoadCaseId);
        Assert.Equal(2, file.Model.Nodes.Count);
        Assert.Single(file.Model.Members);
        Assert.Equal(2, file.Model.Supports.Count);
        Assert.Single(file.Model.LoadCases);
        Assert.Single(file.Model.Loads);
        Assert.Equal(StructuralLoadDirection.GlobalY, file.Model.Loads[0].Direction);
    }

    [Fact]
    public void Read_ShouldUseFirstLoadCaseAsDefaultWhenLoadCaseIdIsMissing()
    {
        string path = WriteTempJson("""
        {
          "nodes": [],
          "materials": [],
          "sections": [],
          "members": [],
          "supports": [],
          "loadCases": [
            { "id": "G1", "name": "Permanent" },
            { "id": "Q1", "name": "Variable" }
          ],
          "loads": []
        }
        """);

        StructuralModelJsonFile file = StructuralModelJsonReader.Read(path);

        Assert.Equal("G1", file.LoadCaseId);
    }

    [Fact]
    public void Read_ShouldThrowWhenRequiredPropertyIsMissing()
    {
        string path = WriteTempJson("""
        {
          "nodes": [
            { "x": 0.0, "y": 0.0 }
          ],
          "loadCases": [
            { "id": "LC1", "name": "Default" }
          ]
        }
        """);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => StructuralModelJsonReader.Read(path));

        Assert.Contains("nodes[].id", exception.Message);
    }

    [Fact]
    public void Read_ShouldThrowWhenFileDoesNotExist()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json");

        Assert.Throws<FileNotFoundException>(() => StructuralModelJsonReader.Read(missingPath));
    }

    private static string WriteTempJson(string json)
    {
        string directory = Path.Combine(Path.GetTempPath(), "StructuralSolver2D.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, "model.json");
        File.WriteAllText(path, json);

        return path;
    }
}
