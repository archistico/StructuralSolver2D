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

        foreach (JsonNode node in file.Nodes ?? Enumerable.Empty<JsonNode>())
        {
            model.AddNode(new StructuralNode(
                Required(node.Id, "nodes[].id"),
                node.X,
                node.Y,
                node.Label));
        }

        foreach (JsonMaterial material in file.Materials ?? Enumerable.Empty<JsonMaterial>())
        {
            model.AddMaterial(new StructuralMaterial(
                Required(material.Id, "materials[].id"),
                Required(material.Name, "materials[].name"),
                material.ElasticModulus,
                material.UnitWeight));
        }

        foreach (JsonSection section in file.Sections ?? Enumerable.Empty<JsonSection>())
        {
            model.AddSection(new StructuralSection(
                Required(section.Id, "sections[].id"),
                Required(section.Name, "sections[].name"),
                section.Area,
                section.MomentOfInertia,
                section.Height,
                section.Width));
        }

        foreach (JsonMember member in file.Members ?? Enumerable.Empty<JsonMember>())
        {
            model.AddMember(new StructuralMember(
                Required(member.Id, "members[].id"),
                Required(member.StartNodeId, "members[].startNodeId"),
                Required(member.EndNodeId, "members[].endNodeId"),
                Required(member.MaterialId, "members[].materialId"),
                Required(member.SectionId, "members[].sectionId"),
                member.Type ?? MemberType.Frame2D,
                member.Label));
        }

        foreach (JsonSupport support in file.Supports ?? Enumerable.Empty<JsonSupport>())
        {
            model.AddSupport(new StructuralSupport(
                Required(support.Id, "supports[].id"),
                Required(support.NodeId, "supports[].nodeId"),
                support.RestrainedUx,
                support.RestrainedUy,
                support.RestrainedRz,
                support.Type ?? SupportType.Custom,
                support.Label));
        }

        foreach (JsonLoadCase loadCase in file.LoadCases ?? Enumerable.Empty<JsonLoadCase>())
        {
            model.AddLoadCase(new StructuralLoadCase(
                Required(loadCase.Id, "loadCases[].id"),
                Required(loadCase.Name, "loadCases[].name"),
                loadCase.Description));
        }

        foreach (JsonLoad load in file.Loads ?? Enumerable.Empty<JsonLoad>())
        {
            model.AddLoad(new StructuralLoad(
                Required(load.Id, "loads[].id"),
                Required(load.LoadCaseId, "loads[].loadCaseId"),
                load.Type ?? throw new InvalidOperationException("loads[].type is required."),
                load.TargetType ?? throw new InvalidOperationException("loads[].targetType is required."),
                load.TargetId ?? string.Empty,
                load.Direction ?? throw new InvalidOperationException("loads[].direction is required."),
                load.Value,
                load.Position,
                load.Label,
                load.EndValue));
        }

        foreach (JsonLoadCombination combination in file.LoadCombinations ?? Enumerable.Empty<JsonLoadCombination>())
        {
            model.AddLoadCombination(new StructuralLoadCombination(
                Required(combination.Id, "loadCombinations[].id"),
                Required(combination.Name, "loadCombinations[].name"),
                (combination.Terms ?? throw new InvalidOperationException("loadCombinations[].terms is required."))
                    .Select(term => new StructuralLoadCombinationTerm(
                        Required(term.LoadCaseId, "loadCombinations[].terms[].loadCaseId"),
                        term.Factor)),
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
            model);
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

    private sealed class JsonStructuralModelFile
    {
        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? LoadCaseId { get; set; }

        public string? LoadCombinationId { get; set; }

        public List<JsonNode>? Nodes { get; set; }

        public List<JsonMaterial>? Materials { get; set; }

        public List<JsonSection>? Sections { get; set; }

        public List<JsonMember>? Members { get; set; }

        public List<JsonSupport>? Supports { get; set; }

        public List<JsonLoadCase>? LoadCases { get; set; }

        public List<JsonLoad>? Loads { get; set; }

        public List<JsonLoadCombination>? LoadCombinations { get; set; }
    }

    private sealed class JsonNode
    {
        public string? Id { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public string? Label { get; set; }
    }

    private sealed class JsonMaterial
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public double ElasticModulus { get; set; }

        public double? UnitWeight { get; set; }
    }

    private sealed class JsonSection
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public double Area { get; set; }

        public double MomentOfInertia { get; set; }

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

        public double Value { get; set; }

        public double? Position { get; set; }

        public double? EndValue { get; set; }

        public string? Label { get; set; }
    }

    private sealed class JsonLoadCombination
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public List<JsonLoadCombinationTerm>? Terms { get; set; }
    }

    private sealed class JsonLoadCombinationTerm
    {
        public string? LoadCaseId { get; set; }

        public double Factor { get; set; }
    }
}
