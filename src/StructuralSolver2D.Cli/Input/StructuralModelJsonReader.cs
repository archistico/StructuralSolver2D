using System.Text.Json;
using System.Text.Json.Serialization;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Cli.Input;

/// <summary>
/// Reads a structural model from a simple JSON file used by the command-line interface.
/// The JSON schema intentionally mirrors the current Core model to keep examples educational and transparent.
/// </summary>
public static class StructuralModelJsonReader
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    /// <summary>
    /// Reads a structural model JSON file from disk.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <returns>The loaded model and its CLI metadata.</returns>
    public static StructuralModelJsonFile Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Input JSON file path cannot be empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Input JSON file was not found.", filePath);
        }

        string json = File.ReadAllText(filePath);
        JsonStructuralModelFile? file = JsonSerializer.Deserialize<JsonStructuralModelFile>(json, SerializerOptions);

        if (file is null)
        {
            throw new InvalidOperationException("Input JSON file is empty or invalid.");
        }

        StructuralModel model = new();

        foreach (JsonNode node in Items(file.Nodes, "nodes"))
        {
            model.AddNode(new StructuralNode(
                Required(node.Id, "nodes[].id"),
                Required(node.X, "nodes[].x"),
                Required(node.Y, "nodes[].y"),
                node.Label));
        }

        foreach (JsonMaterial material in Items(file.Materials, "materials"))
        {
            model.AddMaterial(new StructuralMaterial(
                Required(material.Id, "materials[].id"),
                Required(material.Name, "materials[].name"),
                Required(material.ElasticModulus, "materials[].elasticModulus"),
                material.UnitWeight));
        }

        foreach (JsonSection section in Items(file.Sections, "sections"))
        {
            model.AddSection(new StructuralSection(
                Required(section.Id, "sections[].id"),
                Required(section.Name, "sections[].name"),
                Required(section.Area, "sections[].area"),
                Required(section.MomentOfInertia, "sections[].momentOfInertia"),
                section.Height,
                section.Width));
        }

        foreach (JsonMember member in Items(file.Members, "members"))
        {
            model.AddMember(new StructuralMember(
                Required(member.Id, "members[].id"),
                Required(member.StartNodeId, "members[].startNodeId"),
                Required(member.EndNodeId, "members[].endNodeId"),
                Required(member.MaterialId, "members[].materialId"),
                Required(member.SectionId, "members[].sectionId"),
                member.Type ?? MemberType.Frame2D,
                member.Label,
                member.ReleaseStartMoment,
                member.ReleaseEndMoment));
        }

        foreach (JsonSupport support in Items(file.Supports, "supports"))
        {
            model.AddSupport(new StructuralSupport(
                Required(support.Id, "supports[].id"),
                Required(support.NodeId, "supports[].nodeId"),
                support.RestrainedUx,
                support.RestrainedUy,
                support.RestrainedRz,
                support.Type ?? SupportType.Custom,
                support.Label,
                support.OrientationDegrees));
        }

        foreach (JsonLoadCase loadCase in Items(file.LoadCases, "loadCases"))
        {
            model.AddLoadCase(new StructuralLoadCase(
                Required(loadCase.Id, "loadCases[].id"),
                Required(loadCase.Name, "loadCases[].name"),
                loadCase.Description));
        }

        foreach (JsonLoad load in Items(file.Loads, "loads"))
        {
            model.AddLoad(new StructuralLoad(
                Required(load.Id, "loads[].id"),
                Required(load.LoadCaseId, "loads[].loadCaseId"),
                load.Type ?? throw new InvalidOperationException("loads[].type is required."),
                load.TargetType ?? throw new InvalidOperationException("loads[].targetType is required."),
                load.TargetId ?? string.Empty,
                load.Direction ?? throw new InvalidOperationException("loads[].direction is required."),
                Required(load.Value, "loads[].value"),
                load.Position,
                load.Label,
                load.EndValue));
        }

        int combinationIndex = 0;
        foreach (JsonLoadCombination combination in Items(file.LoadCombinations, "loadCombinations"))
        {
            string combinationPath = $"loadCombinations[{combinationIndex++}]";
            model.AddLoadCombination(new StructuralLoadCombination(
                Required(combination.Id, "loadCombinations[].id"),
                Required(combination.Name, "loadCombinations[].name"),
                Items(combination.Terms ?? throw new InvalidOperationException($"{combinationPath}.terms is required."), $"{combinationPath}.terms")
                    .Select(term => new StructuralLoadCombinationTerm(
                        Required(term.LoadCaseId, "loadCombinations[].terms[].loadCaseId"),
                        Required(term.Factor, $"{combinationPath}.terms[].factor"))),
                combination.Description));
        }

        string loadCaseId = !string.IsNullOrWhiteSpace(file.LoadCombinationId)
            ? file.LoadCombinationId!
            : !string.IsNullOrWhiteSpace(file.LoadCaseId)
                ? file.LoadCaseId!
                : model.LoadCases.FirstOrDefault()?.Id ?? throw new InvalidOperationException("The JSON model must define at least one load case.");

        return new StructuralModelJsonFile(
            file.Title ?? Path.GetFileNameWithoutExtension(filePath),
            file.Description ?? "Structural model loaded from JSON.",
            loadCaseId,
            model)
        {
            DefaultTargetIsCombination = !string.IsNullOrWhiteSpace(file.LoadCombinationId)
        };
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string Required(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required JSON property '{propertyName}' is missing or empty.");
        }

        return value;
    }

    private static double Required(double? value, string propertyName) => value
        ?? throw new InvalidOperationException($"Required JSON property '{propertyName}' is missing or null.");

    private static IEnumerable<T> Items<T>(IEnumerable<T?>? items, string propertyName) where T : class
    {
        if (items is null) yield break;
        int index = 0;
        foreach (T? item in items)
        {
            if (item is null) throw new InvalidOperationException($"JSON element '{propertyName}[{index}]' cannot be null.");
            yield return item;
            index++;
        }
    }

    private sealed class JsonStructuralModelFile
    {
        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? LoadCaseId { get; set; }

        public string? LoadCombinationId { get; set; }

        public List<JsonNode?>? Nodes { get; set; }

        public List<JsonMaterial?>? Materials { get; set; }

        public List<JsonSection?>? Sections { get; set; }

        public List<JsonMember?>? Members { get; set; }

        public List<JsonSupport?>? Supports { get; set; }

        public List<JsonLoadCase?>? LoadCases { get; set; }

        public List<JsonLoad?>? Loads { get; set; }

        public List<JsonLoadCombination?>? LoadCombinations { get; set; }
    }

    private sealed class JsonNode
    {
        public string? Id { get; set; }

        public double? X { get; set; }

        public double? Y { get; set; }

        public string? Label { get; set; }
    }

    private sealed class JsonMaterial
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public double? ElasticModulus { get; set; }

        public double? UnitWeight { get; set; }
    }

    private sealed class JsonSection
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public double? Area { get; set; }

        public double? MomentOfInertia { get; set; }

        public double? Height { get; set; }

        public double? Width { get; set; }
    }

    private sealed class JsonMember
    {
        public string? Id { get; set; }

        public string? StartNodeId { get; set; }

        public string? EndNodeId { get; set; }

        public string? MaterialId { get; set; }

        public string? SectionId { get; set; }

        public MemberType? Type { get; set; }

        public string? Label { get; set; }

        public bool ReleaseStartMoment { get; set; }

        public bool ReleaseEndMoment { get; set; }
    }

    private sealed class JsonSupport
    {
        public string? Id { get; set; }

        public string? NodeId { get; set; }

        public bool RestrainedUx { get; set; }

        public bool RestrainedUy { get; set; }

        public bool RestrainedRz { get; set; }

        public SupportType? Type { get; set; }

        public string? Label { get; set; }

        public double OrientationDegrees { get; set; }
    }

    private sealed class JsonLoadCase
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }
    }

    private sealed class JsonLoad
    {
        public string? Id { get; set; }

        public string? LoadCaseId { get; set; }

        public StructuralLoadType? Type { get; set; }

        public StructuralLoadTargetType? TargetType { get; set; }

        public string? TargetId { get; set; }

        public StructuralLoadDirection? Direction { get; set; }

        public double? Value { get; set; }

        public double? Position { get; set; }

        public double? EndValue { get; set; }

        public string? Label { get; set; }
    }

    private sealed class JsonLoadCombination
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public List<JsonLoadCombinationTerm?>? Terms { get; set; }
    }

    private sealed class JsonLoadCombinationTerm
    {
        public string? LoadCaseId { get; set; }

        public double? Factor { get; set; }
    }
}
