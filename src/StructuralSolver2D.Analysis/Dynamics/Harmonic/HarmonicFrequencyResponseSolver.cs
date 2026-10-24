using System.Numerics;

namespace StructuralSolver2D.Analysis.Dynamics.Harmonic;

/// <summary>Exact complex steady-state transfer of a positive-stiffness SDOF oscillator.
/// Input is F sin(Omega*t), output is |U| sin(Omega*t + phase).</summary>
internal static class HarmonicFrequencyResponseSolver
{
    internal static Complex SolveSdof(double mass, double damping, double stiffness,
        double excitationRadiansPerSecond, double force)
    {
        if (!(mass > 0) || !(stiffness > 0) || !(damping >= 0) ||
            !(excitationRadiansPerSecond >= 0) || !double.IsFinite(mass) ||
            !double.IsFinite(stiffness) || !double.IsFinite(damping) ||
            !double.IsFinite(excitationRadiansPerSecond) || !double.IsFinite(force))
            throw new ArgumentException("M58_SDOF_INPUT: Finite positive mass/stiffness, nonnegative frequency/damping and finite force required.");
        double real = stiffness - mass * excitationRadiansPerSecond * excitationRadiansPerSecond;
        double imaginary = damping * excitationRadiansPerSecond;
        if (!double.IsFinite(real) || !double.IsFinite(imaginary))
            throw new InvalidOperationException("M58_DYNAMIC_STIFFNESS_RANGE: Non-finite dynamic stiffness.");
        double scale = Math.Max(stiffness, mass * excitationRadiansPerSecond * excitationRadiansPerSecond);
        if (Math.Sqrt((real / scale) * (real / scale) + (imaginary / scale) * (imaginary / scale)) < 1e-12)
            throw new InvalidOperationException("M58_UNDAMPED_RESONANCE: No finite steady-state amplitude exists at undamped resonance.");
        Complex displacement = new Complex(force, 0) / new Complex(real, imaginary);
        if (!double.IsFinite(displacement.Real) || !double.IsFinite(displacement.Imaginary))
            throw new InvalidOperationException("M58_RESPONSE_RANGE: Non-finite displacement phasor.");
        return displacement;
    }
}
