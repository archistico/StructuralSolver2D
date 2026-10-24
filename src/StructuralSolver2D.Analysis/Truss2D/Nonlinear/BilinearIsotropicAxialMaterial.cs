namespace StructuralSolver2D.Analysis.Truss2D.Nonlinear;

/// <summary>
/// Experimental small-strain, one-dimensional elastoplastic law with LINEAR ISOTROPIC
/// hardening. Stresses and E/H/Y are in kN/m²; strains are dimensionless.
/// The return mapping is based only on the committed state: Evaluate never commits.
/// This is not the M62 spring (whose parameters have force/displacement units).
/// </summary>
internal sealed class BilinearIsotropicAxialMaterial
{
    internal double ElasticModulus { get; }
    internal double YieldStress { get; }
    internal double HardeningModulus { get; }

    internal BilinearIsotropicAxialMaterial(double elasticModulus, double yieldStress, double hardeningModulus)
    {
        if (!double.IsFinite(elasticModulus) || elasticModulus <= 0 ||
            !double.IsFinite(yieldStress) || yieldStress <= 0 ||
            !double.IsFinite(hardeningModulus) || hardeningModulus < 0 ||
            !double.IsFinite(elasticModulus + hardeningModulus))
            throw new ArgumentException("M63_MATERIAL_INPUT: Require finite E>0, sigmaY>0 and H>=0 [kN/m²].");

        ElasticModulus = elasticModulus;
        YieldStress = yieldStress;
        HardeningModulus = hardeningModulus;
    }

    internal AxialMaterialTrial Evaluate(double totalStrain, AxialMaterialState committed)
    {
        if (!double.IsFinite(totalStrain) ||
            !double.IsFinite(committed.PlasticStrain) ||
            !double.IsFinite(committed.AccumulatedPlasticStrain) ||
            committed.AccumulatedPlasticStrain < 0 ||
            Math.Abs(committed.PlasticStrain) > committed.AccumulatedPlasticStrain +
                1e-12 * Math.Max(1, committed.AccumulatedPlasticStrain))
            throw new ArgumentException("M63_MATERIAL_STATE: Nonfinite or inadmissible committed plastic strain.");

        double trialStress = ElasticModulus * (totalStrain - committed.PlasticStrain);
        double yieldLimit = YieldStress + HardeningModulus * committed.AccumulatedPlasticStrain;
        if (!double.IsFinite(trialStress) || !double.IsFinite(yieldLimit))
            throw new ArgumentException("M63_MATERIAL_RANGE: Trial stress or yield limit overflow.");

        double overstress = Math.Abs(trialStress) - yieldLimit;
        if (overstress <= 0)
            return Build(trialStress, ElasticModulus, committed, 0, totalStrain);

        double deltaGamma = overstress / (ElasticModulus + HardeningModulus);
        double sign = Math.Sign(trialStress);
        var trialState = new AxialMaterialState(
            committed.PlasticStrain + sign * deltaGamma,
            committed.AccumulatedPlasticStrain + deltaGamma);
        double stress = trialStress - sign * ElasticModulus * deltaGamma;
        double tangent = ElasticModulus * (HardeningModulus / (ElasticModulus + HardeningModulus));
        return Build(stress, tangent, trialState, YieldStress * deltaGamma, totalStrain);
    }

    private AxialMaterialTrial Build(double stress, double tangent, AxialMaterialState state,
        double dissipatedEnergyDensity, double totalStrain)
    {
        double elasticStrain = totalStrain - state.PlasticStrain;
        double elasticEnergyDensity = .5 * ElasticModulus * elasticStrain * elasticStrain;
        double hardeningEnergyDensity = .5 * HardeningModulus * state.AccumulatedPlasticStrain * state.AccumulatedPlasticStrain;
        if (!double.IsFinite(stress) || !double.IsFinite(tangent) ||
            !double.IsFinite(state.PlasticStrain) || !double.IsFinite(state.AccumulatedPlasticStrain) ||
            !double.IsFinite(elasticEnergyDensity) || !double.IsFinite(hardeningEnergyDensity) ||
            !double.IsFinite(dissipatedEnergyDensity) ||
            state.AccumulatedPlasticStrain < 0 || dissipatedEnergyDensity < 0)
            throw new ArgumentException("M63_MATERIAL_RANGE: Nonfinite constitutive response.");
        return new AxialMaterialTrial(stress, tangent, state, dissipatedEnergyDensity,
            elasticEnergyDensity, hardeningEnergyDensity);
    }
}

/// <summary>Plastic strain and accumulated absolute plastic strain (both dimensionless).</summary>
internal readonly record struct AxialMaterialState(
    double PlasticStrain = 0,
    double AccumulatedPlasticStrain = 0);

/// <summary>Stress/tangent [kN/m²], strain energy densities [kN/m²].</summary>
internal readonly record struct AxialMaterialTrial(
    double Stress,
    double TangentModulus,
    AxialMaterialState TrialState,
    double PlasticDissipationDensityIncrement,
    double ElasticEnergyDensity,
    double HardeningEnergyDensity);
