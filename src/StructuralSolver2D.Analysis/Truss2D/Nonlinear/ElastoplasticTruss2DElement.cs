namespace StructuralSolver2D.Analysis.Truss2D.Nonlinear;

/// <summary>
/// Research-only linear-geometry Truss2D with an isotropically hardening axial law.
/// Initial coordinates and directions remain FIXED (no P-delta or geometric stiffness).
/// DOFs: [Ux1, Uy1, Ux2, Uy2]; N [kN], f_int [kN], K_t [kN/m].
/// No global assembly, releases, failure/collapse, or public nonlinear solver.
/// </summary>
internal sealed class ElastoplasticTruss2DElement
{
    private readonly BilinearIsotropicAxialMaterial material;
    private readonly double[] direction;
    private int stateVersion;

    internal double Length { get; }
    internal double Area { get; }
    internal double Cosine { get; }
    internal double Sine { get; }
    internal AxialMaterialState CommittedState { get; private set; }

    internal ElastoplasticTruss2DElement(double x1, double y1, double x2, double y2,
        double area, double elasticModulus, double yieldStress, double hardeningModulus)
    {
        if (!double.IsFinite(x1) || !double.IsFinite(y1) ||
            !double.IsFinite(x2) || !double.IsFinite(y2) ||
            !double.IsFinite(area) || area <= 0)
            throw new ArgumentException("M63_ELEMENT_INPUT: Coordinates must be finite and area > 0 m².");
        double dx = x2 - x1, dy = y2 - y1;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (!double.IsFinite(length) || length <= 0)
            throw new ArgumentException("M63_ELEMENT_LENGTH: Zero, unresolvable or overflowing member length.");
        material = new BilinearIsotropicAxialMaterial(elasticModulus, yieldStress, hardeningModulus);
        Length = length;
        Area = area;
        Cosine = dx / length;
        Sine = dy / length;
        direction = new[] { -Cosine, -Sine, Cosine, Sine };
        // A*L, A*E/L and A*sigmaY must be representable in the selected units.
        if (!double.IsFinite(area * length) ||
            !double.IsFinite(area * elasticModulus / length) ||
            !double.IsFinite(area * yieldStress) ||
            !double.IsFinite(area * hardeningModulus))
            throw new ArgumentException("M63_ELEMENT_RANGE: Element geometry/material scales overflow.");
    }

    /// <summary>
    /// Creates a pure trial from the last committed state. Repeating this call does not
    /// accumulate plasticity. Global displacement vector has exactly four finite entries.
    /// </summary>
    internal ElastoplasticTruss2DTrial EvaluateTrial(double[] displacements)
    {
        if (displacements is null || displacements.Length != 4 ||
            displacements.Any(x => !double.IsFinite(x)))
            throw new ArgumentException("M63_DISPLACEMENTS: Expected four finite global translations.");
        double axialExtension = 0;
        for (int i = 0; i < 4; i++) axialExtension += direction[i] * displacements[i];
        double strain = axialExtension / Length;
        var materialTrial = material.Evaluate(strain, CommittedState);
        double axialForce = Area * materialTrial.Stress;
        double axialTangent = Area * materialTrial.TangentModulus / Length;
        double volume = Area * Length;
        double dissipation = volume * materialTrial.PlasticDissipationDensityIncrement;
        double elasticEnergy = volume * materialTrial.ElasticEnergyDensity;
        double hardeningEnergy = volume * materialTrial.HardeningEnergyDensity;
        var forces = new double[4];
        var stiffness = new double[4, 4];
        for (int i = 0; i < 4; i++)
        {
            forces[i] = direction[i] * axialForce;
            for (int j = 0; j < 4; j++) stiffness[i, j] = axialTangent * direction[i] * direction[j];
        }
        if (!double.IsFinite(axialForce) || !double.IsFinite(axialTangent) ||
            !double.IsFinite(strain) || !double.IsFinite(dissipation) ||
            !double.IsFinite(elasticEnergy) || !double.IsFinite(hardeningEnergy) ||
            forces.Any(x => !double.IsFinite(x)))
            throw new ArgumentException("M63_ELEMENT_RANGE: Nonfinite trial force, tangent or energy.");
        return new ElastoplasticTruss2DTrial(this, stateVersion, strain, materialTrial.Stress,
            axialForce, materialTrial.TangentModulus, forces, stiffness,
            materialTrial.TrialState, dissipation, elasticEnergy, hardeningEnergy);
    }

    /// <summary>
    /// Explicit commit. Rejects stale trials and trials from another element; a failed
    /// EvaluateTrial never changes the committed state. No commit occurs on failed Newton.
    /// </summary>
    internal void Commit(ElastoplasticTruss2DTrial trial)
    {
        if (trial is null || !ReferenceEquals(trial.Owner, this) || trial.SourceVersion != stateVersion)
            throw new InvalidOperationException("M63_COMMIT_STATE: Foreign or stale trial; no state committed.");
        CommittedState = trial.TrialState;
        stateVersion++;
    }
}

internal sealed record ElastoplasticTruss2DTrial(
    ElastoplasticTruss2DElement Owner,
    int SourceVersion,
    double AxialStrain,
    double AxialStress,
    double AxialForce,
    double TangentModulus,
    double[] InternalForces,
    double[,] TangentStiffness,
    AxialMaterialState TrialState,
    double PlasticDissipationIncrement,
    double RecoverableElasticEnergy,
    double HardeningEnergy);
