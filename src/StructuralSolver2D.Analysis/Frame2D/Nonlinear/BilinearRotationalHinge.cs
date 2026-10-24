namespace StructuralSolver2D.Analysis.Frame2D.Nonlinear;

/// <summary>
/// M72 research-only concentrated rotational hinge, isotropic hardening, small rotations.
/// Rotational stiffness and hardening [kN*m/rad]; yield moment [kN*m].
/// An elastic compliance remains even before yield: this is NOT a rigid-plastic hinge.
/// </summary>
internal sealed class BilinearRotationalHinge
{
    internal double RotationalStiffness { get; }
    internal double YieldMoment { get; }
    internal double Hardening { get; }

    internal BilinearRotationalHinge(double rotationalStiffness, double yieldMoment, double hardening)
    {
        if (!double.IsFinite(rotationalStiffness) || rotationalStiffness <= 0 ||
            !double.IsFinite(yieldMoment) || yieldMoment <= 0 ||
            !double.IsFinite(hardening) || hardening < 0 ||
            !double.IsFinite(rotationalStiffness + hardening))
            throw new ArgumentException("M72_HINGE_INPUT: Require finite kTheta>0, My>0, H>=0.");

        RotationalStiffness = rotationalStiffness;
        YieldMoment = yieldMoment;
        Hardening = hardening;
    }

    /// <summary>Pure trial: NEVER changes the supplied committed state.</summary>
    internal RotationalHingeTrial Evaluate(double relativeRotation, RotationalHingeState committed)
    {
        if (!double.IsFinite(relativeRotation) ||
            !double.IsFinite(committed.PlasticRotation) ||
            !double.IsFinite(committed.AccumulatedPlasticRotation) ||
            committed.AccumulatedPlasticRotation < 0 ||
            Math.Abs(committed.PlasticRotation) > committed.AccumulatedPlasticRotation +
                1e-12 * Math.Max(1d, committed.AccumulatedPlasticRotation))
            throw new ArgumentException("M72_HINGE_STATE: Invalid relative rotation or committed plastic state.");

        double momentTrial = RotationalStiffness * (relativeRotation - committed.PlasticRotation);
        double threshold = YieldMoment + Hardening * committed.AccumulatedPlasticRotation;
        if (!double.IsFinite(momentTrial) || !double.IsFinite(threshold))
            throw new ArgumentException("M72_HINGE_RANGE: Trial moment or yield threshold overflow.");

        double excess = Math.Abs(momentTrial) - threshold;
        if (excess <= 0)
            return Make(momentTrial, RotationalStiffness, committed, 0, relativeRotation);

        double gamma = excess / (RotationalStiffness + Hardening);
        double sign = Math.Sign(momentTrial);
        var next = new RotationalHingeState(
            committed.PlasticRotation + sign * gamma,
            committed.AccumulatedPlasticRotation + gamma);
        double moment = momentTrial - sign * RotationalStiffness * gamma;
        double tangent = RotationalStiffness * (Hardening / (RotationalStiffness + Hardening));
        return Make(moment, tangent, next, YieldMoment * gamma, relativeRotation);
    }

    private RotationalHingeTrial Make(double moment, double tangent, RotationalHingeState next,
        double dissipation, double relativeRotation)
    {
        double elasticRotation = relativeRotation - next.PlasticRotation;
        double stored = 0.5 * RotationalStiffness * elasticRotation * elasticRotation +
                        0.5 * Hardening * next.AccumulatedPlasticRotation * next.AccumulatedPlasticRotation;
        if (!double.IsFinite(moment) || !double.IsFinite(tangent) ||
            !double.IsFinite(next.PlasticRotation) || !double.IsFinite(next.AccumulatedPlasticRotation) ||
            !double.IsFinite(dissipation) || !double.IsFinite(stored) || dissipation < 0 || stored < 0)
            throw new ArgumentException("M72_HINGE_RANGE: Invalid moment, tangent or energy.");
        return new RotationalHingeTrial(moment, tangent, next, dissipation, stored);
    }
}

internal readonly record struct RotationalHingeState(
    double PlasticRotation = 0,
    double AccumulatedPlasticRotation = 0);

internal readonly record struct RotationalHingeTrial(
    double Moment,
    double Tangent,
    RotationalHingeState TrialState,
    double PlasticDissipationIncrement,
    double StoredEnergy);
