using StructuralSolver2D.Analysis.Frame2D;

namespace StructuralSolver2D.Analysis.Dynamics.Elements;

/// <summary>
/// Euler-Bernoulli 2D frame mass, DOFs [u1, v1, theta1, u2, v2, theta2].
/// Axial fields use linear shape functions, transverse fields cubic Hermite functions.
/// Rotary inertia due to a separate beam cross-section rotation/shear theory is NOT included;
/// theta kinetic terms here follow from interpolating transverse displacement.
/// No end releases may be statically condensed into these matrices.
/// </summary>
internal static class Frame2DMassMatrices
{
    public static double[,] ConsistentLocal(double massPerLength, double length)
    {
        Validate(massPerLength, length);
        double lengthSquared = length * length;
        double scale = massPerLength * length / 420.0;
        if (!double.IsFinite(lengthSquared) || !double.IsFinite(scale) || scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Element mass matrix overflow/underflow.");
        double[,] coefficients =
        {
            { 140,       0,              0,       70,     0,              0 },
            {   0,     156,     22 * length,        0,    54,   -13 * length },
            {   0, 22 * length, 4 * lengthSquared,  0, 13 * length, -3 * lengthSquared },
            {  70,       0,              0,      140,     0,              0 },
            {   0,      54,     13 * length,        0,   156,   -22 * length },
            {   0,-13 * length,-3 * lengthSquared,  0,-22 * length, 4 * lengthSquared },
        };
        for (int i = 0; i < 6; i++)
            for (int j = 0; j < 6; j++)
            {
                coefficients[i, j] *= scale;
                if (!double.IsFinite(coefficients[i, j]))
                    throw new ArgumentOutOfRangeException(nameof(length), "Element mass matrix overflow/underflow.");
            }
        return coefficients;
    }

    /// <summary>
    /// Educational translational nodal lumping, no nodal rotational inertia.
    /// This yields a singular rotational mass block; do NOT silently feed it to a modal solver.
    /// </summary>
    public static double[,] LumpedLocal(double massPerLength, double length)
    {
        Validate(massPerLength, length);
        double half = massPerLength * length / 2.0;
        if (!double.IsFinite(half) || half <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Element mass matrix overflow/underflow.");
        var m = new double[6, 6];
        foreach (int i in new[] { 0, 1, 3, 4 }) m[i, i] = half;
        return m;
    }

    public static double[,] TransformToGlobal(double[,] localMass, double cosine, double sine) =>
        Frame2DElementMatrices.TransformStiffnessToGlobal(
            localMass, Frame2DElementMatrices.BuildTransformation(cosine, sine));

    private static void Validate(double massPerLength, double length)
    {
        if (!double.IsFinite(massPerLength) || massPerLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(massPerLength));
        if (!double.IsFinite(length) || length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
    }
}
