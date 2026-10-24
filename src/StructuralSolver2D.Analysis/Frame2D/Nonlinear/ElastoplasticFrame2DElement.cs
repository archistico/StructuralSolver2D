using StructuralSolver2D.Analysis.Frame2D;

namespace StructuralSolver2D.Analysis.Frame2D.Nonlinear;

/// <summary>
/// M72 isolated research prototype: an Euler-Bernoulli elastic beam with optional
/// history-dependent rotational hinge at each END, connected in SERIES.
/// External DOFs: [ux_i,uy_i,rz_i,ux_j,uy_j,rz_j] (global), small geometry.
/// The internal beam end rotation is solved iteratively; the consistent 6x6
/// tangent is condensed only AFTER local equilibrium. No distributed plasticity,
/// no axial/bending interaction, no geometric nonlinearities or releases.
/// Neither the existing linear frame solver nor the public model is modified.
/// </summary>
internal sealed class ElastoplasticFrame2DElement
{
    private static readonly int[] RotationIndices = { 2, 5 };
    private readonly double[,] beamStiffness;
    private readonly double[,] transformation;
    private readonly BilinearRotationalHinge?[] hinges;
    private readonly RotationalHingeState[] committed = new RotationalHingeState[2];
    private readonly int maxLocalIterations;
    private int version;

    internal double Length { get; }
    internal double Cosine { get; }
    internal double Sine { get; }
    internal RotationalHingeState StartCommittedState => committed[0];
    internal RotationalHingeState EndCommittedState => committed[1];

    internal ElastoplasticFrame2DElement(double x1, double y1, double x2, double y2,
        double elasticModulus, double area, double momentOfInertia,
        BilinearRotationalHinge? startHinge = null, BilinearRotationalHinge? endHinge = null,
        int maxLocalIterations = 40)
    {
        if (!double.IsFinite(x1) || !double.IsFinite(y1) ||
            !double.IsFinite(x2) || !double.IsFinite(y2) ||
            !double.IsFinite(elasticModulus) || elasticModulus <= 0 ||
            !double.IsFinite(area) || area <= 0 ||
            !double.IsFinite(momentOfInertia) || momentOfInertia <= 0 ||
            maxLocalIterations < 1 || maxLocalIterations > 1000)
            throw new ArgumentException("M72_FRAME_INPUT: Finite coordinates, E>0, A>0, I>0, 1<=iterations<=1000 required.");

        double dx = x2 - x1, dy = y2 - y1;
        Length = Math.Sqrt(dx * dx + dy * dy);
        if (!double.IsFinite(Length) || Length <= 0)
            throw new ArgumentException("M72_FRAME_LENGTH: Invalid member length.");
        Cosine = dx / Length;
        Sine = dy / Length;
        beamStiffness = Frame2DElementMatrices.BuildLocalStiffness(elasticModulus, area, momentOfInertia, Length);
        if (Enumerable.Range(0, 6).Any(i => Enumerable.Range(0, 6).Any(j =>
                !double.IsFinite(beamStiffness[i, j]))))
            throw new ArgumentException("M72_FRAME_RANGE: Stiffness overflow.");
        transformation = Frame2DElementMatrices.BuildTransformation(Cosine, Sine);
        hinges = new[] { startHinge, endHinge };
        this.maxLocalIterations = maxLocalIterations;
    }

    /// <summary>Pure trial from committed hinge states. No modifications on failure.</summary>
    internal ElastoplasticFrame2DTrial EvaluateTrial(double[] globalDisplacements)
    {
        if (globalDisplacements is null || globalDisplacements.Length != 6 ||
            globalDisplacements.Any(d => !double.IsFinite(d)))
            throw new ArgumentException("M72_FRAME_DISPLACEMENTS: Require six finite global values.");

        double[] u = Frame2DElementMatrices.TransformDisplacementToLocal(globalDisplacements, transformation);
        int[] active = Enumerable.Range(0, 2).Where(i => hinges[i] is not null).ToArray();
        int m = active.Length;
        double[] q = new double[m];
        for (int j = 0; j < m; j++)
            q[j] = u[RotationIndices[active[j]]] - committed[active[j]].PlasticRotation;

        double[] beamDofs = new double[6];
        double[] beamForces = new double[6];
        var hingeResults = new RotationalHingeTrial[2];
        double[,] a = new double[m, m];
        int iterations = 0;
        bool converged = false;

        for (int iteration = 0; iteration < maxLocalIterations; iteration++)
        {
            iterations = iteration + 1;
            Array.Copy(u, beamDofs, 6);
            for (int j = 0; j < m; j++) beamDofs[RotationIndices[active[j]]] = q[j];
            for (int row = 0; row < 6; row++)
            {
                double f = 0;
                for (int col = 0; col < 6; col++) f += beamStiffness[row, col] * beamDofs[col];
                beamForces[row] = f;
            }
            if (beamForces.Any(f => !double.IsFinite(f)))
                throw new StructuralAnalysisException("M72_LOCAL_RANGE: Nonfinite elastic beam force; trial rejected.");

            double[] residual = new double[m];
            double residualNorm = 0;
            double scale = 1;
            for (int j = 0; j < m; j++)
            {
                int end = active[j];
                int r = RotationIndices[end];
                var h = hinges[end]!;
                hingeResults[end] = h.Evaluate(u[r] - q[j], committed[end]);
                residual[j] = beamForces[r] - hingeResults[end].Moment;
                residualNorm = Math.Max(residualNorm, Math.Abs(residual[j]));
                scale = Math.Max(scale, Math.Max(Math.Abs(beamForces[r]), Math.Abs(hingeResults[end].Moment)));
                for (int k = 0; k < m; k++)
                    a[j, k] = beamStiffness[r, RotationIndices[active[k]]] +
                              (j == k ? hingeResults[end].Tangent : 0);
            }
            if (residualNorm <= 1e-11 * scale)
            {
                converged = true;
                break;
            }
            if (iteration + 1 == maxLocalIterations) break;
            double[] step = SolveSmall(a, residual.Select(x => -x).ToArray());
            for (int j = 0; j < m; j++)
            {
                q[j] += step[j];
                if (!double.IsFinite(q[j]))
                    throw new StructuralAnalysisException("M72_LOCAL_RANGE: Nonfinite beam-end rotation; trial rejected.");
            }
        }
        if (!converged)
            throw new StructuralAnalysisException("M72_LOCAL_NEWTON: Internal hinge equilibrium did not converge; committed states unchanged.");

        // Transformation T maps external local DOFs to beam DOFs when internal rotations
        // are held fixed: active rotational columns are zero. P selects internal rotations.
        // Hinge force on the external rotational DOF is +M; on beam DOF it is -M.
        double[,] kuu = new double[6, 6];
        double[,] kuq = new double[6, m];
        double[] localForces = new double[6];
        for (int i = 0; i < 6; i++)
        {
            bool iInternal = IsActiveRotation(i, active);
            localForces[i] = iInternal ? 0 : beamForces[i];
            for (int j = 0; j < 6; j++)
                kuu[i, j] = iInternal || IsActiveRotation(j, active) ? 0 : beamStiffness[i, j];
            for (int j = 0; j < m; j++)
                kuq[i, j] = iInternal ? 0 : beamStiffness[i, RotationIndices[active[j]]];
        }
        for (int j = 0; j < m; j++)
        {
            int end = active[j];
            int r = RotationIndices[end];
            double tangent = hingeResults[end].Tangent;
            localForces[r] = hingeResults[end].Moment;
            kuu[r, r] += tangent;
            kuq[r, j] -= tangent;
        }

        // Schur complement K = Kuu - Kuq * Kqq^-1 * Kqu, Kqu = Kuq^T.
        // The same consistent hinge tangent was used in the local Newton solve.
        double[,] condensed = (double[,])kuu.Clone();
        for (int j = 0; j < 6; j++)
        {
            double[] rhs = new double[m];
            for (int k = 0; k < m; k++) rhs[k] = kuq[j, k];
            double[] solved = SolveSmall(a, rhs);
            for (int i = 0; i < 6; i++)
                for (int k = 0; k < m; k++)
                    condensed[i, j] -= kuq[i, k] * solved[k];
        }

        var globalForce = Frame2DElementMatrices.TransformLoadToGlobal(localForces, transformation);
        var globalTangent = Frame2DElementMatrices.TransformStiffnessToGlobal(condensed, transformation);
        if (localForces.Any(f => !double.IsFinite(f)) || globalForce.Any(f => !double.IsFinite(f)) ||
            Enumerable.Range(0, 6).Any(i => Enumerable.Range(0, 6).Any(j =>
                !double.IsFinite(condensed[i, j]) || !double.IsFinite(globalTangent[i, j]))))
            throw new StructuralAnalysisException("M72_LOCAL_RANGE: Nonfinite condensed force/tangent; trial rejected.");

        double elasticEnergy = 0;
        for (int i = 0; i < 6; i++) elasticEnergy += 0.5 * beamDofs[i] * beamForces[i];
        double stored = elasticEnergy;
        double plasticDissipation = 0;
        foreach (int end in active)
        {
            stored += hingeResults[end].StoredEnergy;
            plasticDissipation += hingeResults[end].PlasticDissipationIncrement;
        }
        if (!double.IsFinite(stored) || !double.IsFinite(plasticDissipation) || stored < -1e-8 || plasticDissipation < 0)
            throw new StructuralAnalysisException("M72_LOCAL_ENERGY: Invalid stored or dissipated energy; trial rejected.");

        return new ElastoplasticFrame2DTrial(this, version,
            localForces, condensed, globalForce, globalTangent,
            beamDofs, hingeResults[0].TrialState, hingeResults[1].TrialState,
            plasticDissipation, Math.Max(0, stored), iterations);
    }

    internal void Commit(ElastoplasticFrame2DTrial trial)
    {
        if (trial is null || !ReferenceEquals(trial.Owner, this) || trial.SourceVersion != version)
            throw new InvalidOperationException("M72_COMMIT: Foreign or stale trial; committed states unchanged.");
        committed[0] = trial.StartTrialState;
        committed[1] = trial.EndTrialState;
        version++;
    }

    private static bool IsActiveRotation(int index, int[] active) =>
        active.Any(end => RotationIndices[end] == index);

    /// <summary>Pivot-checked, small symmetric 0x0/1x1/2x2 solver. Never regularizes a mechanism.</summary>
    private static double[] SolveSmall(double[,] a, double[] b)
    {
        int n = b.Length;
        if (n == 0) return Array.Empty<double>();
        double scale = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++) scale = Math.Max(scale, Math.Abs(a[i, j]));
        if (!(scale > 0) || !double.IsFinite(scale))
            throw new StructuralAnalysisException("M72_LOCAL_SINGULAR: Nonfinite/singular internal hinge tangent.");
        double tol = 1e-13 * scale;
        if (n == 1)
        {
            if (Math.Abs(a[0, 0]) <= tol)
                throw new StructuralAnalysisException("M72_LOCAL_SINGULAR: Internal hinge tangent is singular.");
            return new[] { b[0] / a[0, 0] };
        }
        // Symmetric 2x2, scaled determinant prevents overflow for large kTheta.
        double a00 = a[0, 0] / scale, a01 = a[0, 1] / scale;
        double a10 = a[1, 0] / scale, a11 = a[1, 1] / scale;
        double det = a00 * a11 - a01 * a10;
        if (!double.IsFinite(det) || Math.Abs(det) < 1e-13)
            throw new StructuralAnalysisException("M72_LOCAL_SINGULAR: Two-hinge internal tangent is singular.");
        return new[] { (a11 * b[0] - a01 * b[1]) / (det * scale),
                       (-a10 * b[0] + a00 * b[1]) / (det * scale) };
    }
}

internal sealed record ElastoplasticFrame2DTrial(
    ElastoplasticFrame2DElement Owner,
    int SourceVersion,
    double[] LocalInternalForces,
    double[,] LocalTangent,
    double[] GlobalInternalForces,
    double[,] GlobalTangent,
    double[] InternalBeamDisplacements,
    RotationalHingeState StartTrialState,
    RotationalHingeState EndTrialState,
    double PlasticDissipationIncrement,
    double RecoverableEnergy,
    int LocalIterations);
