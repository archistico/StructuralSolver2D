using StructuralSolver2D.Cli.Input;

namespace StructuralSolver2D.Cli.Tests.Input;

public sealed class ModalJsonM57Tests
{
    [Fact]
    public void ModalOnlyJson_LoadsOnlyWithExplicitModalOption()
    {
        string path = Path.Combine(Path.GetTempPath(), "modal-input-" + Guid.NewGuid().ToString("N") + ".json");
        const string json = """
            {
              "nodes": [{"id":"A","x":0,"y":0},{"id":"B","x":4,"y":0}],
              "materials": [{"id":"MAT","name":"steel","elasticModulus":210000000,"unitWeight":77}],
              "sections": [{"id":"SEC","name":"beam","area":0.01,"momentOfInertia":0.0001}],
              "members": [{"id":"E","startNodeId":"A","endNodeId":"B","materialId":"MAT","sectionId":"SEC"}],
              "supports": [{"id":"S","nodeId":"A","restrainedUx":true,"restrainedUy":true,"restrainedRz":true}],
              "dynamics": {"schemaVersion":1}
            }
            """;
        try
        {
            File.WriteAllText(path, json);
            var modal = StructuralModelJsonReader.Read(path, allowModalWithoutLoadCase: true);
            Assert.Empty(modal.LoadCaseId);
            Assert.NotNull(modal.Dynamics);
            Assert.Throws<InvalidOperationException>(() => StructuralModelJsonReader.Read(path));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
