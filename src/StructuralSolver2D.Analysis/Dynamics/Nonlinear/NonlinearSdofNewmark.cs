namespace StructuralSolver2D.Analysis.Dynamics.Nonlinear;

/// <summary>
/// Research-only 1-DOF m*u''+c*u'+f_s(u,z)=F(t), solved with constant-average-acceleration
/// Newmark (beta=1/4, gamma=1/2) and a consistent-tangent Newton corrector.
/// NOT an element/structure solver, seismic design check, or public API.
/// Mechanical units: m [kN*s^2/m], c [kN*s/m], k/H [kN/m], F [kN], time [s].
/// </summary>
internal sealed record NonlinearSdofParameters(
    double Mass, double ElasticStiffness, double YieldForce, double HardeningStiffness,
    double ViscousDamping, double TimeStep,
    int MaximumNewtonIterations = 20,
    double AbsoluteForceTolerance = 1e-10,
    double RelativeForceTolerance = 1e-10);

internal sealed record NonlinearSdofStep(
    double Time, double Displacement, double Velocity, double Acceleration,
    double AppliedForce, double InternalForce, double TangentStiffness,
    BilinearSpringState MaterialState, int NewtonIterations, double EquilibriumResidual,
    double KineticEnergy, double RecoverableElasticEnergy, double HardeningEnergy,
    double CumulativePlasticDissipation, double CumulativeViscousDissipation,
    double CumulativeExternalWork, double CumulativeInternalTrapezoidWork,
    double DiscreteWorkBalanceResidual, double PhysicalEnergyBalanceResidual);

internal sealed class NonlinearSdofNewmark
{
    private readonly NonlinearSdofParameters parameters;
    private readonly BilinearIsotropicSpring spring;
    private readonly double initialStoredEnergy;
    private readonly double initialKineticEnergy;
    internal NonlinearSdofStep Current { get; private set; }

    internal NonlinearSdofNewmark(NonlinearSdofParameters settings,
        double initialDisplacement = 0, double initialVelocity = 0, double initialForce = 0)
    {
        if (settings is null || !double.IsFinite(settings.Mass) || settings.Mass <= 0 ||
            !double.IsFinite(settings.ViscousDamping) || settings.ViscousDamping < 0 ||
            !double.IsFinite(settings.TimeStep) || settings.TimeStep <= 0 ||
            settings.MaximumNewtonIterations < 1 || settings.MaximumNewtonIterations > 100 ||
            !double.IsFinite(settings.AbsoluteForceTolerance) || settings.AbsoluteForceTolerance <= 0 ||
            !double.IsFinite(settings.RelativeForceTolerance) || settings.RelativeForceTolerance <= 0 ||
            !double.IsFinite(initialDisplacement) || !double.IsFinite(initialVelocity) ||
            !double.IsFinite(initialForce))
            throw new ArgumentException("M62_SDOF_INPUT: Invalid mass, damping, timestep, tolerances or initial state.");

        parameters = settings;
        spring = new BilinearIsotropicSpring(settings.ElasticStiffness, settings.YieldForce, settings.HardeningStiffness);
        // Starting at an already-yielded displacement would need a supplied, equilibrated history.
        // Refuse it rather than silently creating plastic history at t=0.
        var material = spring.Evaluate(initialDisplacement, new BilinearSpringState());
        if (material.TrialState.AccumulatedPlasticDisplacement != 0)
            throw new ArgumentException("M62_INITIAL_PLASTIC_STATE: Initial displacement is outside the virgin elastic domain.");

        double a0 = (initialForce - settings.ViscousDamping * initialVelocity - material.InternalForce) / settings.Mass;
        double kinetic = .5 * settings.Mass * initialVelocity * initialVelocity;
        initialKineticEnergy = kinetic;
        initialStoredEnergy = kinetic + material.RecoverableElasticEnergy + material.HardeningEnergy;
        if (!double.IsFinite(a0) || !double.IsFinite(initialStoredEnergy))
            throw new ArgumentException("M62_INITIAL_RANGE: Nonfinite initial acceleration or energy.");
        Current = new NonlinearSdofStep(0, initialDisplacement, initialVelocity, a0, initialForce,
            material.InternalForce, material.ConsistentTangent, material.TrialState, 0, 0,
            kinetic, material.RecoverableElasticEnergy, material.HardeningEnergy,
            0, 0, 0, 0, 0, 0);
    }

    /// <summary>
    /// Full-step atomic commit. On failed Newton convergence (or nonfinite data), Current is unchanged.
    /// No adaptive substepping and no post-failure material-state mutation.
    /// </summary>
    internal NonlinearSdofStep Advance(double nextForce)
    {
        if (!double.IsFinite(nextForce))
            throw new ArgumentException("M62_FORCE_INPUT: Nonfinite next-step force.");
        const double beta = .25, gamma = .5;
        var old = Current;
        double dt = parameters.TimeStep;
        double predictorU = old.Displacement + dt * old.Velocity + dt * dt * (.5 - beta) * old.Acceleration;
        double predictorV = old.Velocity + dt * (1 - gamma) * old.Acceleration;
        double massFactor = parameters.Mass / (beta * dt * dt);
        double viscousFactor = parameters.ViscousDamping * gamma / (beta * dt);
        if (!double.IsFinite(predictorU) || !double.IsFinite(predictorV) ||
            !double.IsFinite(massFactor) || !double.IsFinite(viscousFactor))
            throw new InvalidOperationException("M62_STEP_RANGE: Nonfinite predictor or effective mass.");

        double u = predictorU;
        BilinearSpringTrial trial = default;
        double a = 0, v = 0, residual = 0;
        int iterations = 0;
        bool converged = false;
        for (int iteration = 0; iteration < parameters.MaximumNewtonIterations; iteration++)
        {
            a = (u - predictorU) / (beta * dt * dt);
            v = predictorV + gamma * dt * a;
            trial = spring.Evaluate(u, old.MaterialState); // ALWAYS the committed state
            residual = parameters.Mass * a + parameters.ViscousDamping * v + trial.InternalForce - nextForce;
            double scale = Math.Max(1.0, Math.Max(Math.Abs(nextForce),
                Math.Max(Math.Abs(trial.InternalForce),
                    Math.Max(Math.Abs(parameters.Mass * a), Math.Abs(parameters.ViscousDamping * v)))));
            if (!double.IsFinite(residual) || !double.IsFinite(scale))
                throw new InvalidOperationException("M62_NONFINITE_RESIDUAL: Newton evaluation overflowed.");
            iterations = iteration + 1;
            if (Math.Abs(residual) <= parameters.AbsoluteForceTolerance + parameters.RelativeForceTolerance * scale)
            {
                converged = true;
                break;
            }
            double effectiveTangent = massFactor + viscousFactor + trial.ConsistentTangent;
            if (!double.IsFinite(effectiveTangent) || effectiveTangent <= 0)
                throw new InvalidOperationException("M62_INVALID_TANGENT: Effective Newton stiffness is invalid.");
            u -= residual / effectiveTangent;
            if (!double.IsFinite(u))
                throw new InvalidOperationException("M62_NONFINITE_INCREMENT: Newton displacement overflowed.");
        }
        if (!converged)
            throw new InvalidOperationException($"M62_NEWTON_NOT_CONVERGED: No equilibrium after {iterations} iterations; state rolled back.");

        double du = u - old.Displacement;
        double meanVelocity = .5 * (old.Velocity + v);
        double kinetic = .5 * parameters.Mass * v * v;
        double cumulativePlastic = old.CumulativePlasticDissipation + trial.PlasticDissipationIncrement;
        double cumulativeViscous = old.CumulativeViscousDissipation +
            parameters.ViscousDamping * meanVelocity * meanVelocity * dt;
        double externalWork = old.CumulativeExternalWork + .5 * (old.AppliedForce + nextForce) * du;
        double internalWork = old.CumulativeInternalTrapezoidWork + .5 * (old.InternalForce + trial.InternalForce) * du;
        // Newmark's midpoint/trapezoidal *discrete* work identity is separate from the
        // *physical* (recoverable+plastic) energy balance across a yielding step.
        double discreteResidual = externalWork - (kinetic - initialKineticEnergy) -
            cumulativeViscous - internalWork;
        double physicalResidual = externalWork -
            (kinetic + trial.RecoverableElasticEnergy + trial.HardeningEnergy - initialStoredEnergy) -
            cumulativePlastic - cumulativeViscous;
        if (!double.IsFinite(kinetic) || !double.IsFinite(cumulativePlastic) ||
            !double.IsFinite(cumulativeViscous) || !double.IsFinite(externalWork) ||
            !double.IsFinite(internalWork) || !double.IsFinite(discreteResidual) ||
            !double.IsFinite(physicalResidual))
            throw new InvalidOperationException("M62_NONFINITE_ENERGY: No state committed.");
        var next = new NonlinearSdofStep(old.Time + dt, u, v, a, nextForce,
            trial.InternalForce, trial.ConsistentTangent, trial.TrialState, iterations, residual,
            kinetic, trial.RecoverableElasticEnergy, trial.HardeningEnergy,
            cumulativePlastic, cumulativeViscous, externalWork, internalWork,
            discreteResidual, physicalResidual);
        Current = next;
        return next;
    }

}
