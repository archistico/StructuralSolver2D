namespace StructuralSolver2D.Analysis.Dynamics.Nonlinear;

/// <summary>
/// Experimental one-dimensional elastoplastic spring (isotropic linear hardening).
/// Units: displacement [m], force [kN], stiffness [kN/m], work [kN*m].
/// A trial does NOT mutate the committed state.
/// </summary>
internal sealed class BilinearIsotropicSpring
{
    internal double ElasticStiffness { get; }
    internal double YieldForce { get; }
    internal double HardeningStiffness { get; }

    internal BilinearIsotropicSpring(double elasticStiffness, double yieldForce, double hardeningStiffness)
    {
        if (!double.IsFinite(elasticStiffness) || elasticStiffness <= 0 ||
            !double.IsFinite(yieldForce) || yieldForce <= 0 ||
            !double.IsFinite(hardeningStiffness) || hardeningStiffness < 0)
            throw new ArgumentException("M62_MATERIAL_INPUT: Require finite k>0, Fy>0 and H>=0.");
        ElasticStiffness = elasticStiffness;
        YieldForce = yieldForce;
        HardeningStiffness = hardeningStiffness;
    }

    internal BilinearSpringTrial Evaluate(double displacement, BilinearSpringState committed)
    {
        if (!double.IsFinite(displacement) ||
            !double.IsFinite(committed.PlasticDisplacement) ||
            !double.IsFinite(committed.AccumulatedPlasticDisplacement) ||
            committed.AccumulatedPlasticDisplacement < 0)
            throw new ArgumentException("M62_TRIAL_INPUT: Invalid displacement or committed material state.");

        double trialForce = ElasticStiffness * (displacement - committed.PlasticDisplacement);
        double limit = YieldForce + HardeningStiffness * committed.AccumulatedPlasticDisplacement;
        if (!double.IsFinite(trialForce) || !double.IsFinite(limit))
            throw new ArgumentException("M62_MATERIAL_RANGE: Trial force exceeds floating-point range.");
        double yieldFunction = Math.Abs(trialForce) - limit;
        if (yieldFunction <= 0)
            return CreateTrial(trialForce, ElasticStiffness, committed, 0, displacement);

        double increment = yieldFunction / (ElasticStiffness + HardeningStiffness);
        double sign = Math.Sign(trialForce);
        var next = new BilinearSpringState(
            committed.PlasticDisplacement + sign * increment,
            committed.AccumulatedPlasticDisplacement + increment);
        double force = trialForce - sign * ElasticStiffness * increment;
        double tangent = ElasticStiffness * HardeningStiffness / (ElasticStiffness + HardeningStiffness);
        return CreateTrial(force, tangent, next, YieldForce * increment, displacement);
    }

    private BilinearSpringTrial CreateTrial(double force, double tangent, BilinearSpringState next,
        double plasticDissipationIncrement, double displacement)
    {
        double elasticStrain = displacement - next.PlasticDisplacement;
        double recoverableElastic = .5 * ElasticStiffness * elasticStrain * elasticStrain;
        double storedHardening = .5 * HardeningStiffness * next.AccumulatedPlasticDisplacement * next.AccumulatedPlasticDisplacement;
        if (!double.IsFinite(force) || !double.IsFinite(tangent) ||
            !double.IsFinite(next.PlasticDisplacement) || !double.IsFinite(next.AccumulatedPlasticDisplacement) ||
            !double.IsFinite(plasticDissipationIncrement) || !double.IsFinite(recoverableElastic) ||
            !double.IsFinite(storedHardening))
            throw new ArgumentException("M62_MATERIAL_RANGE: Nonfinite constitutive response.");
        return new BilinearSpringTrial(force, tangent, next, plasticDissipationIncrement,
            recoverableElastic, storedHardening);
    }
}

internal readonly record struct BilinearSpringState(
    double PlasticDisplacement = 0,
    double AccumulatedPlasticDisplacement = 0);

internal readonly record struct BilinearSpringTrial(
    double InternalForce,
    double ConsistentTangent,
    BilinearSpringState TrialState,
    double PlasticDissipationIncrement,
    double RecoverableElasticEnergy,
    double HardeningEnergy);
