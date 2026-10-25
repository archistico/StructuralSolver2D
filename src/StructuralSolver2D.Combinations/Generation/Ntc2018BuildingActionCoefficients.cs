using StructuralSolver2D.Combinations.Actions;

namespace StructuralSolver2D.Combinations.Generation;

/// <summary>
/// D.M. 17/01/2018, NTC 2018 table 2.5.I and table 2.6.I (A1).
/// Only ordinary civil/industrial buildings, non-geotechnical STR design.
/// The selected code edition is frozen; coefficients of other national
/// annexes, EQU/A2/GEO or special chapter rules are NOT interchangeable.
/// </summary>
public static class Ntc2018BuildingActionCoefficients
{
    public const string CodeSource = "NTC 2018 DM 17/01/2018, §2.5.3 Tab.2.5.I; §2.6.1 Tab.2.6.I A1";

    public static (decimal Psi0, decimal Psi1, decimal Psi2) Psi(VariableActionCategory category) => category switch
    {
        VariableActionCategory.ResidentialA => (0.7m, 0.5m, 0.3m),
        VariableActionCategory.OfficeB => (0.7m, 0.5m, 0.3m),
        VariableActionCategory.AssemblyC => (0.7m, 0.7m, 0.6m),
        VariableActionCategory.CommercialD => (0.7m, 0.7m, 0.6m),
        VariableActionCategory.StorageE => (1.0m, 0.9m, 0.8m),
        VariableActionCategory.LightParkingF => (0.7m, 0.7m, 0.6m),
        VariableActionCategory.HeavyParkingG => (0.7m, 0.5m, 0.3m),
        VariableActionCategory.MaintenanceRoofH => (0m, 0m, 0m),
        VariableActionCategory.Wind => (0.6m, 0.2m, 0m),
        VariableActionCategory.SnowAtOrBelow1000Metres => (0.5m, 0.2m, 0m),
        VariableActionCategory.SnowAbove1000Metres => (0.7m, 0.5m, 0.2m),
        VariableActionCategory.Temperature => (0.6m, 0.5m, 0m),
        _ => throw new NotSupportedException(
            $"No automatic NTC 2018 building psi table for {category}; I/K and undetermined snow require engineering input.")
    };

    public static decimal GammaStrA1(ActionNature nature, bool favorable, bool treatG2AsG1ByEvidence = false)
    {
        if (treatG2AsG1ByEvidence && nature != ActionNature.G2)
            throw new ArgumentException("G2 coefficient exception cannot be applied to another action type.");
        return nature switch
        {
            ActionNature.G1 => favorable ? 1.0m : 1.3m,
            ActionNature.G2 when treatG2AsG1ByEvidence => favorable ? 1.0m : 1.3m,
            ActionNature.G2 => favorable ? 0.8m : 1.5m,
            ActionNature.Q => favorable ? 0m : 1.5m,
            _ => throw new ArgumentOutOfRangeException(nameof(nature))
        };
    }
}
