using System.Text.Json;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Cli.Tests.Input;

public sealed class StructuralModelJsonContractM75Tests
{
    [Fact]
    public void CompleteModel_RoundTripPreservesAllCorePropertiesAndDynamics()
    {
        StructuralModelJsonFile input = Example();
        string json = StructuralModelJsonWriter.Serialize(input);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("C1", document.RootElement.GetProperty("loadCombinationId").GetString());
        Assert.False(document.RootElement.TryGetProperty("loadCaseId", out _));
        Assert.Equal("Frame2D", document.RootElement.GetProperty("members")[0].GetProperty("type").GetString());
        Assert.Equal("GlobalY", document.RootElement.GetProperty("loads")[0].GetProperty("direction").GetString());
        Assert.Equal(-0.5, document.RootElement.GetProperty("loadCombinations")[0]
            .GetProperty("terms")[1].GetProperty("factor").GetDouble(), 12);

        string path = TempFile(json);
        try
        {
            StructuralModelJsonFile parsed = StructuralModelJsonReader.ReadValidated(path);
            Assert.True(parsed.DefaultTargetIsCombination);
            Assert.Equal("C1", parsed.LoadCaseId);
            Assert.Equal("M75 esempio", parsed.Title);
            Assert.Equal("Descrizione utf-8: trave e legno", parsed.Description);
            Assert.Equal(input.Model.Nodes.ToArray(), parsed.Model.Nodes.ToArray());
            Assert.Equal(input.Model.Materials.ToArray(), parsed.Model.Materials.ToArray());
            Assert.Equal(input.Model.Sections.ToArray(), parsed.Model.Sections.ToArray());
            Assert.Equal(input.Model.Members.ToArray(), parsed.Model.Members.ToArray());
            Assert.Equal(input.Model.Supports.ToArray(), parsed.Model.Supports.ToArray());
            Assert.Equal(input.Model.LoadCases.ToArray(), parsed.Model.LoadCases.ToArray());
            Assert.Equal(input.Model.Loads.ToArray(), parsed.Model.Loads.ToArray());
            Assert.Equal(input.Model.LoadCombinations[0].Id, parsed.Model.LoadCombinations[0].Id);
            Assert.Equal(input.Model.LoadCombinations[0].Name, parsed.Model.LoadCombinations[0].Name);
            Assert.Equal(input.Model.LoadCombinations[0].Terms.ToArray(), parsed.Model.LoadCombinations[0].Terms.ToArray());
            Assert.Equal(input.Dynamics!.SchemaVersion, parsed.Dynamics!.SchemaVersion);
            Assert.Equal(input.Dynamics.AdditionalNodalMasses.ToArray(), parsed.Dynamics.AdditionalNodalMasses.ToArray());
            Assert.Equal(json, StructuralModelJsonWriter.Serialize(parsed));
        }
        finally { DeleteTemp(path); }
    }

    [Fact]
    public void LoadCaseTargetAndOptionalFields_RoundTripWithoutChangingSelection()
    {
        StructuralModelJsonFile input = Example() with { LoadCaseId = "LC", DefaultTargetIsCombination = false, Dynamics = null };
        string json = StructuralModelJsonWriter.Serialize(input);
        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("loadCombinationId", out _));
        Assert.False(document.RootElement.TryGetProperty("dynamics", out _));
        Assert.Equal("LC", document.RootElement.GetProperty("loadCaseId").GetString());
        string path = TempFile(json);
        try
        {
            StructuralModelJsonFile parsed = StructuralModelJsonReader.ReadValidated(path);
            Assert.False(parsed.DefaultTargetIsCombination);
            Assert.Equal("LC", parsed.LoadCaseId);
            Assert.Null(parsed.Dynamics);
            Assert.Equal(json, StructuralModelJsonWriter.Serialize(parsed));
        }
        finally { DeleteTemp(path); }
    }

    [Theory]
    [InlineData("beams/simple-supported-beam.json")]
    [InlineData("beams/released-beam.json")]
    [InlineData("combinations/load-combination.json")]
    [InlineData("loads/inclined-nodal-force.json")]
    [InlineData("mixed/mixed-frame-truss.json")]
    [InlineData("trusses/simple-truss.json")]
    public void HistoricStandardExamples_PreserveCanonicalStructuralPayload(string example)
    {
        using var workspace = new CliTestWorkspace();
        StructuralModelJsonFile original = StructuralModelJsonReader.Read(workspace.Save(CliTestWorkspace.Model(example)));
        string canonical = StructuralModelJsonWriter.Serialize(original);
        string path = TempFile(canonical);
        try
        {
            StructuralModelJsonFile restored = StructuralModelJsonReader.Read(path);
            Assert.Equal(original.LoadCaseId, restored.LoadCaseId);
            Assert.Equal(original.DefaultTargetIsCombination, restored.DefaultTargetIsCombination);
            Assert.Equal(canonical, StructuralModelJsonWriter.Serialize(restored));
        }
        finally { DeleteTemp(path); }
    }

    [Fact]
    public void Write_CreatesOutputDirectoryAndReadableJson()
    {
        string directory = Path.Combine(Path.GetTempPath(), "StructuralSolver2D-M75", Guid.NewGuid().ToString("N"));
        string file = Path.Combine(directory, "subdir", "input.json");
        try
        {
            StructuralModelJsonWriter.Write(file, Example());
            Assert.True(File.Exists(file));
            Assert.Equal("C1", StructuralModelJsonReader.ReadValidated(file).LoadCaseId);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("LC-UNKNOWN", false, "INPUT_LOAD_CASE_NOT_FOUND")]
    [InlineData("COMBO-UNKNOWN", true, "INPUT_COMBINATION_NOT_FOUND")]
    public void Preflight_DetectsSelectedTargetThatDoesNotExist(string target, bool isCombination, string code)
    {
        var input = Example() with { LoadCaseId = target, DefaultTargetIsCombination = isCombination, Dynamics = null };
        var result = new StructuralModelJsonContractValidator().Validate(input);
        Assert.Contains(result.Errors, issue => issue.Code == code && issue.EntityId == target);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ReadValidated_ReportsStableIssueCodesWithoutRunningSolver()
    {
        StructuralModelJsonFile input = Example() with { LoadCaseId = "UNKNOWN", Dynamics = null };
        string path = TempFile(StructuralModelJsonWriter.Serialize(input));
        try
        {
            var ex = Assert.Throws<StructuralModelJsonValidationException>(
                () => StructuralModelJsonReader.ReadValidated(path));
            Assert.Contains(ex.Validation.Errors, issue => issue.Code == "INPUT_COMBINATION_NOT_FOUND");
            Assert.Contains("INPUT_COMBINATION_NOT_FOUND", ex.Message);
        }
        finally { DeleteTemp(path); }
    }

    [Fact]
    public void Preflight_InvalidGeometryAndNonFiniteLoad_AreReportedWithoutMutation()
    {
        StructuralModelJsonFile input = Example() with { Dynamics = null };
        StructuralLoad original = input.Model.Loads[0];
        input.Model.Loads[0] = original with { Value = double.NaN };
        input.Model.Nodes[1] = input.Model.Nodes[1] with { X = 0 }; // zero-length member
        var result = new StructuralModelJsonContractValidator().Validate(input);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_INVALID_VALUE");
        Assert.Contains(result.Errors, issue => issue.Code == "MEMBER_ZERO_LENGTH");
        Assert.True(double.IsNaN(input.Model.Loads[0].Value));
    }

    [Fact]
    public void Preflight_RejectsInvalidDynamicMetadataButAcceptsNoDynamics()
    {
        var input = Example() with { Dynamics = new StructuralDynamicsDefinition(99) };
        var failure = new StructuralModelJsonContractValidator().Validate(input);
        Assert.Contains(failure.Errors, issue => issue.Code == "DYNAMIC_SCHEMA_UNSUPPORTED");
        var ordinary = new StructuralModelJsonContractValidator().Validate(input with { Dynamics = null });
        Assert.True(ordinary.IsValid);
    }

    [Fact]
    public void ModalOptIn_AllowsEmptySelectedLoadCase_WithoutPretendingItIsAStaticTarget()
    {
        var model = Example().Model;
        var input = new StructuralModelJsonFile("Modal", "Solo modi", "", model);
        var validator = new StructuralModelJsonContractValidator();
        Assert.Contains(validator.Validate(input).Errors, issue => issue.Code == "INPUT_TARGET_REQUIRED");
        Assert.True(validator.Validate(input, allowModalWithoutLoadCase: true).IsValid);
    }

    private static StructuralModelJsonFile Example()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0, "inizio"))
            .AddNode(new StructuralNode("B", 3.5, 0, "fine"))
            .AddMaterial(new StructuralMaterial("Steel", "Steel S", 210000000, 78.5))
            .AddSection(new StructuralSection("S", "Rectangular", 0.009, 0.00006, 0.30, 0.03))
            .AddMember(new StructuralMember("AB", "A", "B", "Steel", "S", MemberType.Frame2D,
                "trave", ReleaseStartMoment: false, ReleaseEndMoment: true))
            .AddSupport(new StructuralSupport("SA", "A", true, true, true, SupportType.Fixed, "incastro", 15))
            .AddSupport(new StructuralSupport("SB", "B", false, true, false, SupportType.SimpleSupport, "rullo", 0))
            .AddLoadCase(new StructuralLoadCase("LC", "Permanente", "G"))
            .AddLoadCase(new StructuralLoadCase("Q", "Variabile", "Q"))
            .AddLoad(new StructuralLoad("UDL", "LC", StructuralLoadType.UniformDistributedLoad,
                StructuralLoadTargetType.Member, "AB", StructuralLoadDirection.GlobalY, -3.5, Label: "-Y"))
            .AddLoad(new StructuralLoad("P", "Q", StructuralLoadType.PointLoadOnMember,
                StructuralLoadTargetType.Member, "AB", StructuralLoadDirection.LocalY, -6, 0.35,
                "Punto a 35%"))
            .AddLoad(new StructuralLoad("LINEAR", "Q", StructuralLoadType.LinearDistributedLoad,
                StructuralLoadTargetType.Member, "AB", StructuralLoadDirection.GlobalX, 1.3,
                Label: "lineare", EndValue: -1.1))
            .AddLoadCombination(new StructuralLoadCombination("C1", "Manuale", new[]
            {
                new StructuralLoadCombinationTerm("LC", 1.35),
                new StructuralLoadCombinationTerm("Q", -0.5),
            }, "Combinazione non normativa"));
        return new StructuralModelJsonFile("M75 esempio", "Descrizione utf-8: trave e legno", "C1", model)
        {
            DefaultTargetIsCombination = true,
            Dynamics = new StructuralDynamicsDefinition(1, new[]
            {
                new StructuralNodalMass("NM1", "B", 0.12, 0.009),
            }),
        };
    }

    private static string TempFile(string json)
    {
        string directory = Path.Combine(Path.GetTempPath(), "StructuralSolver2D-M75", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string file = Path.Combine(directory, "input.json");
        File.WriteAllText(file, json);
        return file;
    }

    private static void DeleteTemp(string file)
    {
        string directory = Path.GetDirectoryName(file)!;
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
