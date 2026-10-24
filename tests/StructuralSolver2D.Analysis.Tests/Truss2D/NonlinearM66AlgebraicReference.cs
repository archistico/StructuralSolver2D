namespace StructuralSolver2D.Analysis.Tests.Truss2D;

/// <summary>
/// M66 independent scalar reference. No calls to Analysis production constitutive
/// or Newton solvers: candidate roots are obtained by direct branchwise algebra.
/// Units: kN, m, seconds. One bar, left end fixed, right end axially free.
/// </summary>
internal static class NonlinearM66AlgebraicReference
{
    internal const double Length = 2;
    internal const double Area = .01;
    internal const double E = 200_000;
    internal const double H = 10_000;
    internal const double Yield = 250;
    internal const double ReferenceLoad = 4;
    internal const double EffectiveMass = 1; // rho*L/3 = 1, rho=1.5 kN*s^2/m^2
    internal const double InitialStiffness = E * Area / Length;

    internal readonly record struct State(double PlasticStrain, double AccumulatedPlasticStrain);
    internal readonly record struct ScalarStep(double Time, double U, double V, double A,
        double N, State Material, double PlasticEnergy, double ElasticEnergy, double HardeningEnergy);

    internal static double VirginForceAtPositiveExtension(double extension, double area,
        double elasticModulus, double yieldStress, double hardeningModulus, double length)
    {
        if (extension < 0) throw new ArgumentOutOfRangeException(nameof(extension));
        double strain = extension / length;
        double elasticLimit = yieldStress / elasticModulus;
        return area * (strain <= elasticLimit ? elasticModulus * strain :
            yieldStress + (elasticModulus * hardeningModulus / (elasticModulus + hardeningModulus)) *
            (strain - elasticLimit));
    }

    internal static double VirginTwoBarExtension(double appliedForce,
        double yield1 = 150, double yield2 = 300)
    {
        if (appliedForce < 0) throw new ArgumentOutOfRangeException(nameof(appliedForce));
        double Total(double u) => VirginForceAtPositiveExtension(u, Area, E, yield1, H, Length) +
                                  VirginForceAtPositiveExtension(u, Area, E, yield2, H, Length);
        double lo = 0, hi = 1;
        for (int i = 0; i < 90; ++i)
        {
            double mid = .5 * (lo + hi);
            if (Total(mid) < appliedForce) lo = mid;
            else hi = mid;
        }
        return .5 * (lo + hi);
    }

    /// <summary>Direct exact solution of each Newmark average-acceleration scalar step,
    /// choosing elastic/positive-plastic/negative-plastic affine branches.
    /// Not an iterative tangent/Newton method.</summary>
    internal static IReadOnlyList<ScalarStep> Solve(
        IReadOnlyList<double> factors, double dt, double initialVelocity = 0,
        double massDamping = 0, double stiffnessDamping = 0,
        double hardening = H, double yieldStress = Yield)
    {
        // Reference supports selectable H and Y. Keep all formulas local, independent
        // of the production return-mapping classes.
        double m = EffectiveMass, k = InitialStiffness;
        double c = massDamping * m + stiffnessDamping * k;
        double u = 0, v = initialVelocity;
        double acc = (ReferenceLoad * factors[0] - c * v) / m;
        State state = default;
        double cumulativePlastic = 0;
        var steps = new List<ScalarStep>();
        steps.Add(new ScalarStep(0, u, v, acc, 0, state, 0, 0, 0));
        double ma = 4 / (dt * dt);
        for (int i = 1; i < factors.Count; i++)
        {
            double predictedU = u + dt * v + dt * dt * acc / 4;
            double predictedV = v + dt * acc / 2;
            double effective = m * ma + c * ma * dt / 2;
            double rhs = ReferenceLoad * factors[i] + effective * predictedU - c * predictedV;
            double limit = yieldStress + hardening * state.AccumulatedPlasticStrain;
            double et = E * hardening / (E + hardening);
            double nextU = double.NaN;
            foreach (int branch in new[] { 0, 1, -1 })
            {
                double tangent = branch == 0 ? E : et;
                double intercept = -Area * tangent * state.PlasticStrain +
                    (branch == 0 ? 0 : Area * branch * E * limit / (E + hardening));
                double candidate = (rhs - intercept) / (effective + Area * tangent / Length);
                double trial = E * (candidate / Length - state.PlasticStrain);
                bool valid = branch switch
                {
                    0 => Math.Abs(trial) <= limit + 1e-8,
                    1 => trial > limit - 1e-8,
                    -1 => trial < -limit + 1e-8,
                    _ => false
                };
                if (!valid) continue;
                nextU = candidate;
                break;
            }
            if (!double.IsFinite(nextU)) throw new InvalidOperationException("No admissible scalar branch");
            double trialStress = E * (nextU / Length - state.PlasticStrain);
            double excess = Math.Max(0, Math.Abs(trialStress) - limit);
            double increment = excess / (E + hardening);
            double updatedPlasticStrain = state.PlasticStrain + Math.Sign(trialStress) * increment;
            state = new State(updatedPlasticStrain, state.AccumulatedPlasticStrain + increment);
            double nextN = Area * E * (nextU / Length - state.PlasticStrain);
            double nextA = ma * (nextU - predictedU);
            double nextV = predictedV + dt * nextA / 2;
            cumulativePlastic += Area * Length * yieldStress * increment;
            double elasticEnergy = Area * Length * E / 2 *
                Math.Pow(nextU / Length - state.PlasticStrain, 2);
            double hardeningEnergy = Area * Length * hardening / 2 *
                state.AccumulatedPlasticStrain * state.AccumulatedPlasticStrain;
            steps.Add(new ScalarStep(i * dt, nextU, nextV, nextA, nextN, state,
                cumulativePlastic, elasticEnergy, hardeningEnergy));
            u = nextU; v = nextV; acc = nextA;
        }
        return steps;
    }
}
