namespace StructuralSolver2D.Core.Model.Sections;

using StructuralSolver2D.Core.Model;

/// <summary>
/// Symmetric prismatic section data for the 2D solver and future design checks.
/// The section's MomentOfInertia is the IN-PLANE bending inertia (m^4);
/// OutOfPlaneInertia is complementary information only, NOT a 3D FEM capability.
/// All values use m, m^2, m^3 and m^4 respectively.
/// </summary>
public sealed record SectionGeometricPropertiesM80(
    StructuralSection Section,
    string Shape,
    double OutOfPlaneInertia,
    double InPlaneElasticSectionModulus,
    double OutOfPlaneElasticSectionModulus,
    double InPlaneRadiusOfGyration,
    double OutOfPlaneRadiusOfGyration)
{
    public double Area => Section.Area;
    public double InPlaneInertia => Section.MomentOfInertia;

    /// <summary>Retains the existing FEM input model unchanged.</summary>
    public StructuralSection ToSolverSection() => Section;
}
