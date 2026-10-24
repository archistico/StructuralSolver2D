namespace StructuralSolver2D.Analysis.Dynamics.Elements;

/// <summary>
/// Consistent inertia of a two-node pin-jointed bar with linearly interpolated
/// GLOBAL displacements [Ux1, Uy1, Ux2, Uy2]. A truss has no rotational DOFs.
/// Transverse inertia remains physical even though transverse *stiffness* is absent.
/// Units: translational mass in kN*s²/m.
/// </summary>
internal static class Truss2DMassMatrices
{
    public static double[,] Consistent(double massPerLength, double length)
    {
        Validate(massPerLength, length);
        double a = massPerLength * length / 6.0;
        if (!double.IsFinite(a) || a <= 0)
            throw new ArgumentOutOfRangeException(nameof(massPerLength), "Element mass matrix overflow/underflow.");
        return new[,]
        {
            { 2*a, 0.0, a, 0.0 },
            { 0.0, 2*a, 0.0, a },
            { a, 0.0, 2*a, 0.0 },
            { 0.0, a, 0.0, 2*a },
        };
    }

    /// <summary>Educational row-sum lumping; never a substitute for the default consistent matrix.</summary>
    public static double[,] Lumped(double massPerLength, double length)
    {
        Validate(massPerLength, length);
        double half = massPerLength * length / 2.0;
        if (!double.IsFinite(half) || half <= 0)
            throw new ArgumentOutOfRangeException(nameof(massPerLength), "Element mass matrix overflow/underflow.");
        return new[,]
        {
            { half, 0.0, 0.0, 0.0 },
            { 0.0, half, 0.0, 0.0 },
            { 0.0, 0.0, half, 0.0 },
            { 0.0, 0.0, 0.0, half },
        };
    }

    private static void Validate(double massPerLength, double length)
    {
        if (!double.IsFinite(massPerLength) || massPerLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(massPerLength));
        if (!double.IsFinite(length) || length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
    }
}
