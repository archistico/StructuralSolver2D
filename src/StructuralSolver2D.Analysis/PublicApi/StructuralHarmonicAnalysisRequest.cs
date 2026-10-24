using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>A physical sinusoidal nodal force/moment: Fx,Fy in kN, Mz in kN*m.
/// All specified forces share the same sin(Omega*t) time origin and phase.</summary>
public sealed record StructuralHarmonicNodalForce(string NodeId, double Fx = 0, double Fy = 0, double Mz = 0);

/// <summary>One explicit, physically distinct damping model; units alpha [s^-1], beta [s].</summary>
public sealed record StructuralHarmonicDamping
{
    /// <summary>When set, applies this damping ratio to every mode; range [0,+infinity).</summary>
    public double? UniformModalRatio { get; init; }
    /// <summary>Rayleigh damping alpha: C=alpha*M+beta*K.</summary>
    public double? RayleighAlphaInverseSeconds { get; init; }
    /// <summary>Rayleigh damping beta: C=alpha*M+beta*K.</summary>
    public double? RayleighBetaSeconds { get; init; }

    public double Ratio(double naturalRadiansPerSecond)
    {
        if (!(naturalRadiansPerSecond > 0) || !double.IsFinite(naturalRadiansPerSecond))
            throw new ArgumentException("M58_DAMPING_FREQUENCY: Natural frequency must be positive.");
        bool modal = UniformModalRatio.HasValue;
        bool rayleigh = RayleighAlphaInverseSeconds.HasValue || RayleighBetaSeconds.HasValue;
        if (modal == rayleigh)
            throw new ArgumentException("M58_DAMPING_MODEL: Specify either a uniform modal ratio or both Rayleigh coefficients.");
        double ratio;
        if (modal) ratio = UniformModalRatio!.Value;
        else
        {
            if (!RayleighAlphaInverseSeconds.HasValue || !RayleighBetaSeconds.HasValue)
                throw new ArgumentException("M58_DAMPING_MODEL: Both Rayleigh coefficients are required.");
            double alpha = RayleighAlphaInverseSeconds.Value, beta = RayleighBetaSeconds.Value;
            if (!double.IsFinite(alpha) || !double.IsFinite(beta) || alpha < 0 || beta < 0)
                throw new ArgumentException("M58_DAMPING_RANGE: Rayleigh coefficients must be finite and nonnegative.");
            ratio = .5 * (alpha / naturalRadiansPerSecond + beta * naturalRadiansPerSecond);
        }
        if (!double.IsFinite(ratio) || ratio < 0)
            throw new ArgumentException("M58_DAMPING_RANGE: Damping ratio must be finite and nonnegative.");
        return ratio;
    }
}

/// <summary>Finite-frequency sweep of steady-state linear response; not transient analysis.</summary>
public sealed record StructuralHarmonicAnalysisRequest(
    StructuralModel Model,
    StructuralDynamicsDefinition Dynamics,
    IReadOnlyList<StructuralHarmonicNodalForce> Forces,
    IReadOnlyList<double> FrequenciesHertz,
    StructuralHarmonicDamping Damping);
