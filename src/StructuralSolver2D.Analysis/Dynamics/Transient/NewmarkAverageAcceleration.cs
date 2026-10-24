namespace StructuralSolver2D.Analysis.Dynamics.Transient;

/// <summary>Single real, mass-normalized mode; private state and an independently testable numerical kernel.</summary>
internal sealed class NewmarkAverageAcceleration
{
    private readonly double stiffness, damping;
    private readonly double timeStep;
    internal double Displacement { get; private set; }
    internal double Velocity { get; private set; }
    internal double Acceleration { get; private set; }
    internal double Stiffness => stiffness;
    internal double Damping => damping;

    internal NewmarkAverageAcceleration(double angularFrequency, double dampingRatio, double dt,
        double initialDisplacement, double initialVelocity, double initialForce)
    {
        if (!double.IsFinite(angularFrequency) || angularFrequency <= 0 ||
            !double.IsFinite(dampingRatio) || dampingRatio < 0 ||
            !double.IsFinite(dt) || dt <= 0 ||
            !double.IsFinite(initialDisplacement) || !double.IsFinite(initialVelocity) ||
            !double.IsFinite(initialForce))
            throw new ArgumentException("M59_NEWMARK_INPUT: Initial state, damping, mass-normalized frequency and dt must be valid.");
        stiffness = angularFrequency * angularFrequency;
        damping = 2 * dampingRatio * angularFrequency;
        timeStep = dt;
        if (!double.IsFinite(stiffness) || !double.IsFinite(damping))
            throw new ArgumentException("M59_NEWMARK_RANGE: Mode parameters exceed double range.");
        Displacement = initialDisplacement;
        Velocity = initialVelocity;
        Acceleration = initialForce - damping * Velocity - stiffness * Displacement;
    }

    /// <summary>Constant-average-acceleration step, mass=1. Force at END of step is required.</summary>
    internal void Advance(double nextForce)
    {
        const double beta = 0.25, gamma = 0.5;
        if (!double.IsFinite(nextForce)) throw new ArgumentException("M59_FORCE_RANGE: Nonfinite generalized force.");
        double predU = Displacement + timeStep * Velocity +
            timeStep * timeStep * (0.5 - beta) * Acceleration;
        double predV = Velocity + timeStep * (1 - gamma) * Acceleration;
        double denominator = 1 + gamma * timeStep * damping + beta * timeStep * timeStep * stiffness;
        double nextA = (nextForce - damping * predV - stiffness * predU) / denominator;
        double nextU = predU + beta * timeStep * timeStep * nextA;
        double nextV = predV + gamma * timeStep * nextA;
        if (!double.IsFinite(nextU) || !double.IsFinite(nextV) || !double.IsFinite(nextA))
            throw new InvalidOperationException("M59_NONFINITE_RESPONSE: Integration produced nonfinite output.");
        Displacement = nextU;
        Velocity = nextV;
        Acceleration = nextA;
    }
}
