using System.Text.Json;
using System.Text.Json.Serialization;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Cli.Input;

/// <summary>
/// M75 canonical writer for the standard structural model + optional dynamics JSON contract.
/// This deliberately does not represent analysis-specific extension roots (e.g. seismic or
/// nonlinear requests); those have independent schemas and must not be silently rewritten.
/// </summary>
public static class StructuralModelJsonWriter
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>
    /// Returns an indented, human-readable JSON representation that can be read by
    /// <see cref="StructuralModelJsonReader.Read"/>. Values remain in Core units.
    /// The caller may use <see cref="StructuralModelJsonContractValidator"/> for preflight
    /// before writing; this method only performs JSON serialization.
    /// </summary>
    public static string Serialize(StructuralModelJsonFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(file.Model);
        StructuralModel model = file.Model;

        var document = new
        {
            title = file.Title,
            description = file.Description,
            loadCaseId = file.DefaultTargetIsCombination ? null : file.LoadCaseId,
            loadCombinationId = file.DefaultTargetIsCombination ? file.LoadCaseId : null,
            nodes = model.Nodes.Select(node => new
            {
                node.Id, node.X, node.Y, node.Label,
            }).ToArray(),
            materials = model.Materials.Select(material => new
            {
                material.Id, material.Name, material.ElasticModulus, material.UnitWeight,
            }).ToArray(),
            sections = model.Sections.Select(section => new
            {
                section.Id, section.Name, section.Area, section.MomentOfInertia,
                section.Height, section.Width,
            }).ToArray(),
            members = model.Members.Select(member => new
            {
                member.Id, member.StartNodeId, member.EndNodeId, member.MaterialId,
                member.SectionId, member.Type, member.Label,
                member.ReleaseStartMoment, member.ReleaseEndMoment,
            }).ToArray(),
            supports = model.Supports.Select(support => new
            {
                support.Id, support.NodeId, support.RestrainedUx, support.RestrainedUy,
                support.RestrainedRz, support.Type, support.Label,
                support.OrientationDegrees,
                support.StiffnessUx, support.StiffnessUy, support.StiffnessRz,
            }).ToArray(),
            loadCases = model.LoadCases.Select(loadCase => new
            {
                loadCase.Id, loadCase.Name, loadCase.Description,
            }).ToArray(),
            loads = model.Loads.Select(load => new
            {
                load.Id, load.LoadCaseId, load.Type, load.TargetType, load.TargetId,
                load.Direction, load.Value, load.Position, load.Label, load.EndValue,
            }).ToArray(),
            loadCombinations = model.LoadCombinations.Select(combination => new
            {
                combination.Id, combination.Name, combination.Description,
                terms = combination.Terms.Select(term => new { term.LoadCaseId, term.Factor }).ToArray(),
            }).ToArray(),
            dynamics = file.Dynamics is null ? null : new
            {
                schemaVersion = file.Dynamics.SchemaVersion,
                nodalMasses = file.Dynamics.AdditionalNodalMasses.Select(mass => new
                {
                    mass.Id, mass.NodeId, mass.TranslationalMass, mass.RotationalInertia,
                }).ToArray(),
            },
        };

        return JsonSerializer.Serialize(document, Options);
    }

    /// <summary>Writes a standard structural JSON document, creating its directory if necessary.</summary>
    public static void Write(string filePath, StructuralModelJsonFile file)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Output JSON file path cannot be empty.", nameof(filePath));
        string json = Serialize(file);
        string? directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(filePath, json);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        return options;
    }
}
