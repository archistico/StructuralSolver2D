using System.Text.Json.Serialization;

namespace StructuralSolver2D.Core.Dynamics;

/// <summary>
/// Versioned opt-in dynamic metadata; the independent static StructuralModel is unchanged.
/// For schema version 1, member mass is ALWAYS obtained from material.UnitWeight
/// and additional nodal masses are additive. No dynamic calculation is triggered.
/// </summary>
public sealed record StructuralDynamicsDefinition(
    int SchemaVersion = 1,
    IReadOnlyList<StructuralNodalMass>? NodalMasses = null)
{
    /// <summary>Additional nodal masses; empty if not provided.</summary>
    [JsonIgnore]
    public IReadOnlyList<StructuralNodalMass> AdditionalNodalMasses =>
        NodalMasses ?? Array.Empty<StructuralNodalMass>();
}
