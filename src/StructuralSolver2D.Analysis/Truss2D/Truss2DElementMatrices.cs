namespace StructuralSolver2D.Analysis.Truss2D;

/// <summary>
/// Builds stiffness matrices for a two-dimensional axial-only truss element.
/// </summary>
internal static class Truss2DElementMatrices
{
    /// <summary>
    /// Builds the 4x4 global stiffness matrix of a 2D truss element.
    /// Degrees of freedom are Ux1, Uy1, Ux2 and Uy2.
    /// </summary>
    /// <param name="elasticModulus">Elastic modulus E in kN/m².</param>
    /// <param name="area">Section area A in m².</param>
    /// <param name="length">Member length L in m.</param>
    /// <param name="cosine">Direction cosine.</param>
    /// <param name="sine">Direction sine.</param>
    /// <returns>Global 4x4 truss stiffness matrix.</returns>
    public static double[,] BuildGlobalStiffness(
        double elasticModulus,
        double area,
        double length,
        double cosine,
        double sine)
    {
        double axial = elasticModulus * area / length;
        double c2 = cosine * cosine;
        double s2 = sine * sine;
        double cs = cosine * sine;

        return new[,]
        {
            { axial * c2, axial * cs, -axial * c2, -axial * cs },
            { axial * cs, axial * s2, -axial * cs, -axial * s2 },
            { -axial * c2, -axial * cs, axial * c2, axial * cs },
            { -axial * cs, -axial * s2, axial * cs, axial * s2 },
        };
    }
}
