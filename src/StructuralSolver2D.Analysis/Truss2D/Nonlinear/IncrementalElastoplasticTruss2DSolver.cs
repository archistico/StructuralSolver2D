using StructuralSolver2D.Analysis.Solvers;

namespace StructuralSolver2D.Analysis.Truss2D.Nonlinear;

/// <summary>A research-only M64 nodal model, in metres and kN. No public StructuralModel contract is changed.</summary>
internal sealed record IncrementalTrussNode(string Id, double X, double Y);

/// <summary>E, yield stress and H in kN/m²; area in m².</summary>
internal sealed record IncrementalTrussMember(string Id, string StartNodeId, string EndNodeId,
    double Area, double ElasticModulus, double YieldStress, double HardeningModulus);

/// <summary>Homogeneous fixed support: NormalX*Ux + NormalY*Uy = 0 at the named node.</summary>
internal sealed record IncrementalTrussSupport(string NodeId, double NormalX, double NormalY);

/// <summary>One reference pattern, multiplied by each load factor of the prescribed history.</summary>
internal sealed record IncrementalTrussLoad(string NodeId, double Fx, double Fy);

internal sealed record IncrementalTrussRequest(
    IReadOnlyList<IncrementalTrussNode> Nodes,
    IReadOnlyList<IncrementalTrussMember> Members,
    IReadOnlyList<IncrementalTrussSupport> Supports,
    IReadOnlyList<IncrementalTrussLoad> Loads,
    IReadOnlyList<double> LoadFactors,
    int MaxIterations = 35,
    double AbsoluteForceTolerance = 1e-9,
    double RelativeForceTolerance = 1e-9);

/// <summary>Committed converged step, with physical global DOFs [Ux0,Uy0,...], forces [kN] and energy [kN*m].</summary>
internal sealed record IncrementalTrussStep(
    double LoadFactor,
    int NewtonIterations,
    double ResidualNorm,
    double[] Displacements,
    double[] Reactions,
    double[] AxialForces,
    AxialMaterialState[] MaterialStates,
    double IncrementalPlasticDissipation,
    double CumulativePlasticDissipation);

/// <summary>Failure is explicit; Steps and FinalStates contain ONLY committed increments.</summary>
internal sealed record IncrementalTrussResult(
    bool Completed,
    string? Failure,
    IReadOnlyList<IncrementalTrussStep> Steps,
    AxialMaterialState[] FinalStates);

/// <summary>
/// M64 research-only load-controlled, small-strain elastoplastic 2D truss.
/// Uses a kinematic nullspace of homogeneous supports, Newton with consistent tangent,
/// and commits all element states only after global equilibrium converges.
/// No geometric stiffness, arc-length, frame bending, displacement control, or collapse assessment.
/// </summary>
internal static class IncrementalElastoplasticTruss2DSolver
{
    private sealed record MemberEntry(string Id, ElastoplasticTruss2DElement Element, int[] Dofs);
    private sealed record Assembly(double[,] Tangent, double[] Internal, ElastoplasticTruss2DTrial[] Trials);

    internal static IncrementalTrussResult Solve(IncrementalTrussRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBasic(request);
        var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int n = 0; n < request.Nodes.Count; n++)
        {
            var node = request.Nodes[n];
            if (string.IsNullOrWhiteSpace(node.Id) || !double.IsFinite(node.X) || !double.IsFinite(node.Y) ||
                !ids.TryAdd(node.Id, n))
                throw new ArgumentException($"M64_NODE: Duplicate, blank or nonfinite node at index {n}.");
        }
        int dofCount = 2 * request.Nodes.Count;
        var members = new List<MemberEntry>();
        var memberNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var member in request.Members)
        {
            if (member is null || string.IsNullOrWhiteSpace(member.Id) || !memberNames.Add(member.Id) ||
                !ids.TryGetValue(member.StartNodeId ?? "", out int start) ||
                !ids.TryGetValue(member.EndNodeId ?? "", out int end) || start == end)
                throw new ArgumentException($"M64_MEMBER: Invalid/duplicate member or missing/coincident node reference '{member?.Id}'.");
            var a = request.Nodes[start];
            var b = request.Nodes[end];
            var element = new ElastoplasticTruss2DElement(a.X, a.Y, b.X, b.Y,
                member.Area, member.ElasticModulus, member.YieldStress, member.HardeningModulus);
            members.Add(new MemberEntry(member.Id, element,
                new[] { 2 * start, 2 * start + 1, 2 * end, 2 * end + 1 }));
        }
        var supports = new List<double[]>();
        foreach (var support in request.Supports)
        {
            if (support is null || !ids.TryGetValue(support.NodeId ?? "", out int node) ||
                !double.IsFinite(support.NormalX) || !double.IsFinite(support.NormalY))
                throw new ArgumentException("M64_SUPPORT: Support node/direction invalid.");
            double length = Math.Sqrt(support.NormalX * support.NormalX + support.NormalY * support.NormalY);
            if (length <= 0 || !double.IsFinite(length))
                throw new ArgumentException("M64_SUPPORT: A support must have a nonzero finite direction.");
            var constraint = new double[dofCount];
            constraint[2 * node] = support.NormalX / length;
            constraint[2 * node + 1] = support.NormalY / length;
            supports.Add(constraint);
        }
        double[,] basis = BuildNullspace(dofCount, supports);
        int reducedCount = basis.GetLength(1);
        if (reducedCount == 0)
            throw new ArgumentException("M64_CONSTRAINTS: All nodal degrees of freedom are constrained.");
        double[] pattern = new double[dofCount];
        foreach (var load in request.Loads)
        {
            if (load is null || !ids.TryGetValue(load.NodeId ?? "", out int node) ||
                !double.IsFinite(load.Fx) || !double.IsFinite(load.Fy))
                throw new ArgumentException("M64_LOAD: Unknown node or nonfinite load.");
            pattern[2 * node] += load.Fx;
            pattern[2 * node + 1] += load.Fy;
        }
        if (pattern.Any(v => !double.IsFinite(v)))
            throw new ArgumentException("M64_LOAD_RANGE: Assembled load overflow.");
        if (request.LoadFactors.Any(factor => !double.IsFinite(factor)))
            throw new ArgumentException("M64_FACTOR: Load factors must be finite.");
        double[] loadReduced = Project(basis, pattern);
        var snapshots = new List<IncrementalTrussStep>();
        double[] committedU = new double[dofCount];
        double dissipated = 0;
        foreach (double factor in request.LoadFactors)
        {
            double[] currentU = (double[])committedU.Clone();
            double[] target = pattern.Select(f => f * factor).ToArray();
            double[] reducedTarget = loadReduced.Select(f => f * factor).ToArray();
            if (target.Any(v => !double.IsFinite(v)) || reducedTarget.Any(v => !double.IsFinite(v)))
                return Failure("M64_LOAD_RANGE: Incremental load overflow.");
            double forceScale = Math.Max(1, EuclideanNorm(reducedTarget));
            if (!double.IsFinite(forceScale)) return Failure("M64_LOAD_RANGE: Load norm overflow.");
            bool success = false;
            Assembly? finalAssembly = null;
            double norm = double.NaN;
            int iterations = 0;
            string? reason = null;
            for (int iteration = 0; iteration <= request.MaxIterations; iteration++)
            {
                Assembly assembly;
                try { assembly = Assemble(members, currentU, dofCount); }
                catch (ArgumentException error) { reason = $"M64_ELEMENT: {error.Message}"; break; }
                double[] residual = new double[dofCount];
                for (int i = 0; i < dofCount; i++) residual[i] = target[i] - assembly.Internal[i];
                double[] reducedResidual = Project(basis, residual);
                norm = EuclideanNorm(reducedResidual);
                if (!double.IsFinite(norm)) { reason = "M64_NONFINITE: Newton residual overflow."; break; }
                iterations = iteration;
                if (norm <= request.AbsoluteForceTolerance + request.RelativeForceTolerance * forceScale)
                {
                    // Even an unloaded mechanism has zero residual. Never accept it as stable.
                    try
                    {
                        var reducedTangentAtEquilibrium = ProjectMatrix(basis, assembly.Tangent);
                        _ = DenseLinearSystemSolver.Solve(reducedTangentAtEquilibrium, new double[reducedCount]);
                    }
                    catch (StructuralAnalysisException error)
                    {
                        reason = $"M64_SINGULAR: Converged force residual but tangent is singular at factor {factor:G17}: {error.Message}";
                        break;
                    }
                    success = true;
                    finalAssembly = assembly;
                    break;
                }
                if (iteration == request.MaxIterations)
                {
                    reason = $"M64_NONCONVERGENCE: Step factor {factor:G17}, residual {norm:G6} kN after {iteration} iterations.";
                    break;
                }
                double[,] reducedTangent = ProjectMatrix(basis, assembly.Tangent);
                double[] correction;
                try { correction = DenseLinearSystemSolver.Solve(reducedTangent, reducedResidual); }
                catch (StructuralAnalysisException error)
                {
                    reason = $"M64_SINGULAR: Step factor {factor:G17}, iteration {iteration}, {error.Message}";
                    break;
                }
                for (int i = 0; i < dofCount; i++)
                    for (int j = 0; j < reducedCount; j++) currentU[i] += basis[i, j] * correction[j];
                if (currentU.Any(v => !double.IsFinite(v)))
                {
                    reason = "M64_NONFINITE: Newton displacement overflow.";
                    break;
                }
            }
            if (!success || finalAssembly is null) return Failure(reason ?? "M64_NONCONVERGENCE: Failed to converge.");

            // Trial calls above never changed element states. This is the sole commit point.
            // Pre-validate all trials before committing a complete step.
            var trials = finalAssembly.Trials;
            if (trials.Length != members.Count ||
                trials.Where((trial, i) => !ReferenceEquals(trial.Owner, members[i].Element)).Any())
                throw new InvalidOperationException("M64_COMMIT: Inconsistent element trial ownership.");
            double deltaDissipation = trials.Sum(t => t.PlasticDissipationIncrement);
            if (!double.IsFinite(deltaDissipation) || deltaDissipation < 0 ||
                !double.IsFinite(dissipated + deltaDissipation))
                return Failure("M64_ENERGY: Invalid plastic dissipation.");
            // Snapshot result values before the atomic state transition.
            var reactions = new double[dofCount];
            for (int i = 0; i < dofCount; i++) reactions[i] = finalAssembly.Internal[i] - target[i];
            var snapshot = new IncrementalTrussStep(factor, iterations, norm,
                (double[])currentU.Clone(), reactions,
                trials.Select(t => t.AxialForce).ToArray(),
                trials.Select(t => t.TrialState).ToArray(),
                deltaDissipation, dissipated + deltaDissipation);
            foreach (var trial in trials) trial.Owner.Commit(trial);
            committedU = currentU;
            dissipated += deltaDissipation;
            snapshots.Add(snapshot);
        }
        return new IncrementalTrussResult(true, null, snapshots, members.Select(m => m.Element.CommittedState).ToArray());

        IncrementalTrussResult Failure(string message) => new(false, message, snapshots,
            members.Select(m => m.Element.CommittedState).ToArray());
    }

    private static Assembly Assemble(IReadOnlyList<MemberEntry> members, double[] u, int dofs)
    {
        var stiffness = new double[dofs, dofs];
        var force = new double[dofs];
        var trials = new ElastoplasticTruss2DTrial[members.Count];
        for (int m = 0; m < members.Count; m++)
        {
            var member = members[m];
            var elementU = member.Dofs.Select(index => u[index]).ToArray();
            ElastoplasticTruss2DTrial trial;
            try { trial = member.Element.EvaluateTrial(elementU); }
            catch (ArgumentException error)
            {
                throw new ArgumentException($"M64_MEMBER_{member.Id}: {error.Message}", error);
            }
            trials[m] = trial;
            for (int i = 0; i < 4; i++)
            {
                int a = member.Dofs[i];
                force[a] += trial.InternalForces[i];
                for (int j = 0; j < 4; j++) stiffness[a, member.Dofs[j]] += trial.TangentStiffness[i, j];
            }
        }
        if (force.Any(v => !double.IsFinite(v)))
            throw new ArgumentException("M64_ASSEMBLY_RANGE: Assembled internal forces overflow.");
        return new Assembly(stiffness, force, trials);
    }

    private static double[,] BuildNullspace(int dofs, IReadOnlyList<double[]> constraints)
    {
        const double tolerance = 1e-10;
        var normals = new List<double[]>();
        foreach (var row in constraints)
        {
            double[] vector = (double[])row.Clone();
            Orthogonalize(vector, normals);
            double norm = EuclideanNorm(vector);
            if (norm < tolerance) continue; // redundant constraints
            for (int i = 0; i < dofs; i++) vector[i] /= norm;
            normals.Add(vector);
        }
        var free = new List<double[]>();
        for (int dof = 0; dof < dofs; dof++)
        {
            var vector = new double[dofs];
            vector[dof] = 1;
            Orthogonalize(vector, normals);
            Orthogonalize(vector, free);
            double norm = EuclideanNorm(vector);
            if (norm < tolerance) continue;
            for (int i = 0; i < dofs; i++) vector[i] /= norm;
            free.Add(vector);
        }
        var basis = new double[dofs, free.Count];
        for (int j = 0; j < free.Count; j++)
            for (int i = 0; i < dofs; i++) basis[i, j] = free[j][i];
        return basis;
    }

    private static void Orthogonalize(double[] vector, IReadOnlyList<double[]> against)
    {
        // Two passes improve accuracy for duplicate/inclined constraints.
        for (int pass = 0; pass < 2; pass++)
            foreach (var unit in against)
            {
                double dot = 0;
                for (int i = 0; i < vector.Length; i++) dot += vector[i] * unit[i];
                for (int i = 0; i < vector.Length; i++) vector[i] -= dot * unit[i];
            }
    }

    private static double[] Project(double[,] basis, double[] vector)
    {
        int n = basis.GetLength(0), r = basis.GetLength(1);
        var projected = new double[r];
        for (int j = 0; j < r; j++)
            for (int i = 0; i < n; i++) projected[j] += basis[i, j] * vector[i];
        return projected;
    }

    private static double[,] ProjectMatrix(double[,] basis, double[,] matrix)
    {
        int n = basis.GetLength(0), r = basis.GetLength(1);
        var temp = new double[n, r];
        for (int i = 0; i < n; i++)
            for (int k = 0; k < n; k++)
                for (int j = 0; j < r; j++) temp[i, j] += matrix[i, k] * basis[k, j];
        var projected = new double[r, r];
        for (int a = 0; a < r; a++)
            for (int i = 0; i < n; i++)
                for (int b = 0; b < r; b++) projected[a, b] += basis[i, a] * temp[i, b];
        return projected;
    }

    private static double EuclideanNorm(double[] vector)
    {
        // Scale before squaring to avoid overflow for large but finite vectors.
        double scale = vector.Length == 0 ? 0 : vector.Max(v => Math.Abs(v));
        if (scale == 0) return 0;
        double square = 0;
        foreach (double v in vector) square += (v / scale) * (v / scale);
        return scale * Math.Sqrt(square);
    }

    private static void ValidateBasic(IncrementalTrussRequest r)
    {
        if (r.Nodes is null || r.Members is null || r.Supports is null || r.Loads is null || r.LoadFactors is null ||
            r.Nodes.Count < 2 || r.Nodes.Count > 256 || r.Members.Count == 0 || r.Members.Count > 1024 ||
            r.LoadFactors.Count == 0 || r.LoadFactors.Count > 1000 ||
            r.MaxIterations < 1 || r.MaxIterations > 1000 ||
            !double.IsFinite(r.AbsoluteForceTolerance) || r.AbsoluteForceTolerance <= 0 ||
            !double.IsFinite(r.RelativeForceTolerance) || r.RelativeForceTolerance < 0)
            throw new ArgumentException("M64_INPUT: Invalid model size, iteration count or force tolerances.");
    }
}
