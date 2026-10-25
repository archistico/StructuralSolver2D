using System.Text.Json;
using System.Text.Json.Serialization;

namespace StructuralSolver2D.Combinations.Actions;

/// <summary>
/// Strict, opt-in sidecar JSON for typed actions. Does not change the CLI
/// StructuralModel JSON schema or deserialize/write manual FEM combinations.
/// Domain validation remains an explicit mandatory second step.
/// </summary>
public static class ActionInventoryJsonSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(ActionInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return JsonSerializer.Serialize(inventory, Options);
    }

    public static ActionInventory Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<ActionInventory>(json, Options)
            ?? throw new JsonException("Action inventory JSON cannot be null.");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        return options;
    }
}
