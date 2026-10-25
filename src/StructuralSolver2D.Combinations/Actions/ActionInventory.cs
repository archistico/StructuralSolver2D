namespace StructuralSolver2D.Combinations.Actions;

/// <summary>
/// Type of characteristic action; intentionally not an ultimate-limit-state factor.
/// G1 = structural permanent, G2 = non-structural permanent, Q = variable.
/// </summary>
public enum ActionNature { G1, G2, Q }

/// <summary>Only the declared NTC 2018 table 2.5.I categories; I/K require engineering input.</summary>
public enum VariableActionCategory
{
    ResidentialA, OfficeB, AssemblyC, CommercialD, StorageE,
    LightParkingF, HeavyParkingG, MaintenanceRoofH,
    RoofI, RoofK, Wind, SnowAtOrBelow1000Metres,
    SnowAbove1000Metres, SnowElevationUndetermined, Temperature
}

/// <summary>Load cases in P0-B2 must contain unfactored characteristic values.</summary>
public enum ActionValueBasis { Characteristic, Nominal, Design }

/// <summary>Optional evidence for timber-specific duration-dependent checks; not a kmod selector.</summary>
public enum TimberActionDuration { Permanent, LongTerm, MediumTerm, ShortTerm, Instantaneous }

/// <summary>
/// Immutable metadata describing ONE homogeneous Core load case. All signed loads,
/// their coordinate directions and units remain exclusively in the Core model.
/// Positive/negative effects cannot be deduced from this metadata.
/// </summary>
public sealed record ActionDefinition(
    string ActionId,
    string LoadCaseId,
    ActionNature Nature,
    VariableActionCategory? VariableCategory,
    ActionValueBasis Basis,
    string SourceEvidenceId,
    string HomogeneityEvidenceId,
    string? AlternativeGroupId = null,
    TimberActionDuration? TimberDuration = null,
    string? TimberDurationEvidenceId = null,
    bool TreatG2AsG1ByEvidence = false,
    string? G2CoefficientExceptionEvidenceId = null);

/// <summary>
/// An opt-in action inventory, separate from StructuralModel and all legacy
/// StructuralLoadCombination entries. A versioned normative inventory is NOT
/// a generated set of combinations and cannot establish normative completeness.
/// </summary>
public sealed record ActionInventory(
    string SchemaVersion,
    string CodeEdition,
    IReadOnlyList<ActionDefinition> Actions);

public static class ActionInventoryProfiles
{
    public const string SchemaVersion = "ntc2018-actions-v1";
    public const string CodeEdition = "NTC2018:2018-01-17";
}
