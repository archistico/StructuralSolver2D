using StructuralSolver2D.Analysis.Solvers;

namespace StructuralSolver2D.Analysis.Truss2D.Nonlinear;

/// <summary>Mass per length is in kN*s^2/m^2; its product with L is kN*s^2/m.
/// Material properties and geometry use the M63 kN-m convention.</summary>
internal sealed record NonlinearDynamicTrussMember(
    string Id, string StartNodeId, string EndNodeId, double Area,
    double ElasticModulus, double YieldStress, double HardeningModulus,
    double MassPerLength);

/// <summary>Research-only small-displacement nonlinear time integration, without public API changes.
/// LoadFactors[i] multiplies the same nodal load pattern at t=i*TimeStep.
/// Initial displacement is zero (virgin constitutive state); initial velocity optional.
/// Viscous damping C = alpha*M + beta*K_initial, fixed throughout plastic evolution.</summary>
internal sealed record NonlinearTruss2DTimeRequest(
    IReadOnlyList<IncrementalTrussNode> Nodes,
    IReadOnlyList<NonlinearDynamicTrussMember> Members,
    IReadOnlyList<IncrementalTrussSupport> Supports,
    IReadOnlyList<IncrementalTrussLoad> Loads,
    IReadOnlyList<double> LoadFactors,
    double TimeStep,
    double MassDamping = 0,
    double InitialStiffnessDamping = 0,
    double[]? InitialVelocity = null,
    int MaximumNewtonIterations = 30,
    double AbsoluteForceTolerance = 1e-9,
    double RelativeForceTolerance = 1e-9);

internal sealed record NonlinearTruss2DTimeStep(
    double Time, double LoadFactor, double[] Displacements, double[] Velocities,
    double[] Accelerations, double[] Reactions, double[] AxialForces,
    AxialMaterialState[] MaterialStates, int NewtonIterations, double ReducedResidualNorm,
    double KineticEnergy, double RecoverableEnergy, double HardeningEnergy,
    double CumulativePlasticDissipation, double CumulativeViscousDissipation,
    double CumulativeExternalWork, double CumulativeInternalTrapezoidWork,
    double DiscreteWorkResidual, double PhysicalEnergyResidual);

internal sealed record NonlinearTruss2DTimeResult(
    bool Completed, string? Failure, IReadOnlyList<NonlinearTruss2DTimeStep> Steps,
    IReadOnlyList<AxialMaterialState> FinalStates);

/// <summary>
/// M65: physical-coordinate Newmark average acceleration with consistent truss mass
/// and full Newton updates. No modal reduction or fictitious rotary DOFs.
/// Each accepted step commits every element exactly once; failed steps never commit.
/// Only supported for sufficiently restrained, small dense research models.
/// </summary>
internal static class NonlinearTruss2DNewmark
{
    private const double Beta = .25;
    private const double Gamma = .5;
    private const double ConstraintTolerance = 1e-10;
    private sealed record ElementEntry(ElastoplasticTruss2DElement Element, int[] Dofs);
    private sealed record Assembly(double[] Internal, double[,] Tangent, ElastoplasticTruss2DTrial[] Trials);

    internal static NonlinearTruss2DTimeResult Solve(NonlinearTruss2DTimeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Nodes is null || request.Members is null || request.Supports is null ||
            request.Loads is null || request.LoadFactors is null || request.Nodes.Count < 2 ||
            request.Members.Count < 1 || request.Nodes.Count > 100 || request.Members.Count > 300 ||
            request.LoadFactors.Count < 2 || request.LoadFactors.Count > 2001 ||
            !double.IsFinite(request.TimeStep) || request.TimeStep <= 0 ||
            !double.IsFinite(request.MassDamping) || request.MassDamping < 0 ||
            !double.IsFinite(request.InitialStiffnessDamping) || request.InitialStiffnessDamping < 0 ||
            request.MaximumNewtonIterations is < 1 or > 100 ||
            !double.IsFinite(request.AbsoluteForceTolerance) || request.AbsoluteForceTolerance <= 0 ||
            !double.IsFinite(request.RelativeForceTolerance) || request.RelativeForceTolerance <= 0)
            throw new ArgumentException("M65_INPUT: Invalid model, timestep, damping or iteration settings.");

        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < request.Nodes.Count; i++)
        {
            var node = request.Nodes[i];
            if (node is null || string.IsNullOrWhiteSpace(node.Id) ||
                !double.IsFinite(node.X) || !double.IsFinite(node.Y) || !ids.TryAdd(node.Id, i))
                throw new ArgumentException("M65_NODE: Duplicate/invalid node or nonfinite coordinates.");
        }
        int n = 2 * request.Nodes.Count;
        var elements = new List<ElementEntry>();
        var memberIds = new HashSet<string>(StringComparer.Ordinal);
        double[,] mass = new double[n, n];
        foreach (var member in request.Members)
        {
            if (member is null || string.IsNullOrWhiteSpace(member.Id) || !memberIds.Add(member.Id) ||
                !ids.TryGetValue(member.StartNodeId ?? "", out int ia) ||
                !ids.TryGetValue(member.EndNodeId ?? "", out int ib) || ia == ib ||
                !double.IsFinite(member.MassPerLength) || member.MassPerLength <= 0)
                throw new ArgumentException("M65_MEMBER: Invalid member reference, id or mass per length.");
            var a = request.Nodes[ia];
            var b = request.Nodes[ib];
            var element = new ElastoplasticTruss2DElement(a.X, a.Y, b.X, b.Y, member.Area,
                member.ElasticModulus, member.YieldStress, member.HardeningModulus);
            int[] dofs = { 2 * ia, 2 * ia + 1, 2 * ib, 2 * ib + 1 };
            elements.Add(new ElementEntry(element, dofs));
            // Consistent two-node axial-line translation mass: mL/6*[2I I; I 2I].
            double m = member.MassPerLength * element.Length / 6;
            if (!double.IsFinite(m) || m <= 0)
                throw new ArgumentException("M65_MASS_RANGE: Invalid consistent element mass.");
            for (int axis = 0; axis < 2; axis++)
            {
                int i = dofs[axis], j = dofs[axis + 2];
                mass[i, i] += 2 * m; mass[j, j] += 2 * m;
                mass[i, j] += m; mass[j, i] += m;
            }
        }
        if (!IsFinite(mass)) throw new ArgumentException("M65_MASS_RANGE: Global mass overflow.");

        var constraints = new List<double[]>();
        foreach (var support in request.Supports)
        {
            if (support is null || !ids.TryGetValue(support.NodeId ?? "", out int ix) ||
                !double.IsFinite(support.NormalX) || !double.IsFinite(support.NormalY))
                throw new ArgumentException("M65_SUPPORT: Invalid node or constraint direction.");
            double length = Math.Sqrt(support.NormalX * support.NormalX + support.NormalY * support.NormalY);
            if (!double.IsFinite(length) || length <= 0)
                throw new ArgumentException("M65_SUPPORT: Zero/invalid support direction.");
            var row = new double[n];
            row[2 * ix] = support.NormalX / length;
            row[2 * ix + 1] = support.NormalY / length;
            constraints.Add(row);
        }
        var z = ConstraintNullspace(n, constraints);
        int r = z.GetLength(1);
        if (r == 0) throw new ArgumentException("M65_SUPPORT: No free degrees of freedom.");
        var reducedMass = ProjectMatrix(z, mass);
        try { _ = DenseLinearSystemSolver.Solve(reducedMass, new double[r]); }
        catch (StructuralAnalysisException ex) { throw new ArgumentException("M65_MASS_SINGULAR: " + ex.Message, ex); }

        var pattern = new double[n];
        foreach (var load in request.Loads)
        {
            if (load is null || !ids.TryGetValue(load.NodeId ?? "", out int ix) ||
                !double.IsFinite(load.Fx) || !double.IsFinite(load.Fy))
                throw new ArgumentException("M65_LOAD: Invalid load or node.");
            pattern[2 * ix] += load.Fx; pattern[2 * ix + 1] += load.Fy;
        }
        if (!Finite(pattern) || request.LoadFactors.Any(x => !double.IsFinite(x)))
            throw new ArgumentException("M65_LOAD_RANGE: Load overflow or invalid factor.");
        var projectedPattern = Project(z, pattern);
        var zeros = new double[n];
        var zeroAssembly = Assemble(elements, zeros, n);
        var initialK = ProjectMatrix(z, zeroAssembly.Tangent);
        // Detect a mechanism even if inertia would mask a static singularity.
        try { _ = DenseLinearSystemSolver.Solve(initialK, new double[r]); }
        catch (StructuralAnalysisException ex) { throw new ArgumentException("M65_MECHANISM: " + ex.Message, ex); }
        var fullDamping = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                fullDamping[i, j] = request.MassDamping * mass[i, j] +
                                    request.InitialStiffnessDamping * zeroAssembly.Tangent[i, j];
        var damping = ProjectMatrix(z, fullDamping);
        if (!IsFinite(fullDamping) || !IsFinite(damping))
            throw new ArgumentException("M65_DAMPING_RANGE: Damping overflow.");

        var q = new double[r];
        var vel = new double[r];
        if (request.InitialVelocity is not null)
        {
            if (request.InitialVelocity.Length != n || !Finite(request.InitialVelocity))
                throw new ArgumentException("M65_INITIAL_VELOCITY: Expected two finite components per node.");
            vel = Project(z, request.InitialVelocity);
            var compatible = Lift(z, vel);
            for (int i = 0; i < n; i++)
                if (Math.Abs(compatible[i] - request.InitialVelocity[i]) >
                    1e-9 * Math.Max(1, Math.Abs(request.InitialVelocity[i])))
                    throw new ArgumentException("M65_INITIAL_CONSTRAINT: Initial velocity violates supports.");
        }
        double[] load0 = Scale(projectedPattern, request.LoadFactors[0]);
        double[] acceleration;
        try { acceleration = DenseLinearSystemSolver.Solve(reducedMass,
            Subtract(load0, MatrixVector(damping, vel))); }
        catch (StructuralAnalysisException ex) { throw new ArgumentException("M65_INITIAL_ACCELERATION: " + ex.Message, ex); }
        if (!Finite(acceleration)) throw new ArgumentException("M65_INITIAL_ACCELERATION: Nonfinite.");

        double initialKinetic = .5 * Dot(vel, MatrixVector(reducedMass, vel));
        var steps = new List<NonlinearTruss2DTimeStep>();
        double plastic = 0, viscous = 0, externalWork = 0, internalWork = 0;
        double[] lastInternal = Project(z, zeroAssembly.Internal);
        var initialReactions = FullResidual(mass, z, acceleration, fullDamping, vel,
            zeroAssembly.Internal, pattern, request.LoadFactors[0]);
        steps.Add(new NonlinearTruss2DTimeStep(0, request.LoadFactors[0],
            zeros, Lift(z, vel), Lift(z, acceleration), initialReactions,
            zeroAssembly.Trials.Select(t => t.AxialForce).ToArray(),
            zeroAssembly.Trials.Select(t => t.TrialState).ToArray(), 0, 0,
            initialKinetic, 0, 0, 0, 0, 0, 0, 0, 0));

        NonlinearTruss2DTimeResult Fail(string message) => new(false, message, steps,
            elements.Select(e => e.Element.CommittedState).ToArray());
        double dt = request.TimeStep;
        if (!double.IsFinite(dt * (request.LoadFactors.Count - 1)))
            throw new ArgumentException("M65_TIME_RANGE: Timeline overflow.");
        double ma = 1 / (Beta * dt * dt), cv = Gamma / (Beta * dt);
        if (!double.IsFinite(ma) || !double.IsFinite(cv))
            throw new ArgumentException("M65_STEP_RANGE: Time step produces overflow.");
        for (int step = 1; step < request.LoadFactors.Count; step++)
        {
            double factor = request.LoadFactors[step];
            double[] qPredict = new double[r], vPredict = new double[r];
            for (int i = 0; i < r; i++)
            {
                qPredict[i] = q[i] + dt * vel[i] + dt * dt * (.5 - Beta) * acceleration[i];
                vPredict[i] = vel[i] + dt * (1 - Gamma) * acceleration[i];
            }
            double[] guess = (double[])qPredict.Clone();
            double[] target = Scale(projectedPattern, factor);
            double[] nextAcceleration = new double[r], nextVelocity = new double[r];
            Assembly? final = null;
            double norm = double.PositiveInfinity;
            int used = 0;
            string? reason = null;
            for (int iteration = 0; iteration < request.MaximumNewtonIterations; iteration++)
            {
                for (int i = 0; i < r; i++)
                {
                    nextAcceleration[i] = (guess[i] - qPredict[i]) * ma;
                    nextVelocity[i] = vPredict[i] + Gamma * dt * nextAcceleration[i];
                }
                if (!Finite(nextAcceleration) || !Finite(nextVelocity) || !Finite(guess))
                { reason = "M65_NONFINITE: Predicted response overflow."; break; }
                Assembly assembly;
                try { assembly = Assemble(elements, Lift(z, guess), n); }
                catch (ArgumentException ex) { reason = "M65_ELEMENT: " + ex.Message; break; }
                var fint = Project(z, assembly.Internal);
                var inertial = MatrixVector(reducedMass, nextAcceleration);
                var dampingForce = MatrixVector(damping, nextVelocity);
                var residual = new double[r];
                for (int i = 0; i < r; i++)
                    residual[i] = target[i] - fint[i] - inertial[i] - dampingForce[i];
                norm = Norm(residual);
                double scale = Math.Max(1, Math.Max(Norm(target),
                    Math.Max(Norm(fint), Math.Max(Norm(inertial), Norm(dampingForce)))));
                used = iteration + 1;
                if (!double.IsFinite(norm) || !double.IsFinite(scale))
                { reason = "M65_NONFINITE: Residual overflow."; break; }
                if (norm <= request.AbsoluteForceTolerance + request.RelativeForceTolerance * scale)
                { final = assembly; break; }
                var tangent = ProjectMatrix(z, assembly.Tangent);
                for (int i = 0; i < r; i++)
                    for (int j = 0; j < r; j++)
                        tangent[i, j] += ma * reducedMass[i, j] + cv * damping[i, j];
                double[] correction;
                try { correction = DenseLinearSystemSolver.Solve(tangent, residual); }
                catch (StructuralAnalysisException ex) { reason = "M65_TANGENT: " + ex.Message; break; }
                for (int i = 0; i < r; i++) guess[i] += correction[i];
            }
            if (final is null)
                return Fail(reason ?? $"M65_NONCONVERGENCE: Step {step} failed after {used} Newton evaluations.");

            var du = Subtract(guess, q);
            var midv = Scale(Add(nextVelocity, vel), .5);
            var physicalV = Lift(z, nextVelocity);
            double kinetic = .5 * Dot(nextVelocity, MatrixVector(reducedMass, nextVelocity));
            double trialPlastic = final.Trials.Sum(t => t.PlasticDissipationIncrement);
            double nextPlastic = plastic + trialPlastic;
            double nextViscous = viscous + dt * Dot(midv, MatrixVector(damping, midv));
            double nextWork = externalWork + .5 * (steps[^1].LoadFactor + factor) * Dot(projectedPattern, du);
            // Previous committed internal force is retained between accepted steps.
            var newInternal = Project(z, final.Internal);
            double nextInternalWork = internalWork + .5 * Dot(Add(lastInternal, newInternal), du);
            double elastic = final.Trials.Sum(t => t.RecoverableElasticEnergy);
            double hardening = final.Trials.Sum(t => t.HardeningEnergy);
            double discrete = nextWork - (kinetic - initialKinetic) - nextViscous - nextInternalWork;
            double physical = nextWork - (kinetic - initialKinetic) - elastic - hardening - nextPlastic - nextViscous;
            if (!double.IsFinite(kinetic) || !double.IsFinite(nextPlastic) || trialPlastic < -1e-10 ||
                !double.IsFinite(nextViscous) || !double.IsFinite(nextWork) ||
                !double.IsFinite(nextInternalWork) || !double.IsFinite(discrete) ||
                !double.IsFinite(physical))
                return Fail("M65_ENERGY: Nonfinite or nonphysical trial energy; state not committed.");
            // Validate all ownerships before *any* commit of this increment.
            if (final.Trials.Length != elements.Count ||
                final.Trials.Where((t, i) => !ReferenceEquals(t.Owner, elements[i].Element)).Any())
                return Fail("M65_COMMIT: Invalid ownership of an element trial.");
            double[] disp = Lift(z, guess);
            double[] acc = Lift(z, nextAcceleration);
            double[] reactions = FullResidual(mass, z, nextAcceleration, fullDamping, nextVelocity,
                final.Internal, pattern, factor);
            if (!Finite(disp) || !Finite(acc) || !Finite(reactions))
                return Fail("M65_RANGE: Nonfinite physical response; state not committed.");
            var snapshot = new NonlinearTruss2DTimeStep(step * dt, factor, disp,
                physicalV, acc, reactions, final.Trials.Select(t => t.AxialForce).ToArray(),
                final.Trials.Select(t => t.TrialState).ToArray(), used, norm,
                kinetic, elastic, hardening, nextPlastic, nextViscous,
                nextWork, nextInternalWork, discrete, physical);
            foreach (var trial in final.Trials) trial.Owner.Commit(trial);
            q = guess; vel = (double[])nextVelocity.Clone();
            acceleration = (double[])nextAcceleration.Clone();
            plastic = nextPlastic; viscous = nextViscous; externalWork = nextWork;
            internalWork = nextInternalWork; lastInternal = newInternal;
            steps.Add(snapshot);
        }
        return new NonlinearTruss2DTimeResult(true, null, steps,
            elements.Select(e => e.Element.CommittedState).ToArray());
    }

    private static Assembly Assemble(IReadOnlyList<ElementEntry> elements, double[] u, int size)
    {
        var fint = new double[size];
        var tangent = new double[size, size];
        var trials = new ElastoplasticTruss2DTrial[elements.Count];
        for (int m = 0; m < elements.Count; m++)
        {
            var e = elements[m];
            var local = e.Dofs.Select(i => u[i]).ToArray();
            var t = e.Element.EvaluateTrial(local);
            trials[m] = t;
            for (int i = 0; i < 4; i++)
            {
                int a = e.Dofs[i];
                fint[a] += t.InternalForces[i];
                for (int j = 0; j < 4; j++) tangent[a, e.Dofs[j]] += t.TangentStiffness[i, j];
            }
        }
        if (!Finite(fint) || !IsFinite(tangent))
            throw new ArgumentException("M65_ASSEMBLY_RANGE: Overflow in internal forces or tangent.");
        return new Assembly(fint, tangent, trials);
    }

    private static double[,] ConstraintNullspace(int n, IReadOnlyList<double[]> constraints)
    {
        var normals = new List<double[]>();
        foreach (var row in constraints)
        {
            var v = (double[])row.Clone();
            Orthogonalize(v, normals);
            double norm = Norm(v);
            if (norm < ConstraintTolerance) continue;
            normals.Add(Scale(v, 1 / norm));
        }
        var free = new List<double[]>();
        for (int dof = 0; dof < n; dof++)
        {
            var v = new double[n]; v[dof] = 1;
            Orthogonalize(v, normals); Orthogonalize(v, free);
            double norm = Norm(v);
            if (norm < ConstraintTolerance) continue;
            free.Add(Scale(v, 1 / norm));
        }
        var z = new double[n, free.Count];
        for (int j = 0; j < free.Count; j++)
            for (int i = 0; i < n; i++) z[i, j] = free[j][i];
        return z;
    }
    private static void Orthogonalize(double[] v, IReadOnlyList<double[]> basis)
    {
        for (int pass = 0; pass < 2; pass++)
            foreach (var q in basis)
            {
                double dot = Dot(v, q);
                for (int i = 0; i < v.Length; i++) v[i] -= dot * q[i];
            }
    }
    private static double[,] ProjectMatrix(double[,] z, double[,] a)
    {
        int n = z.GetLength(0), r = z.GetLength(1);
        var result = new double[r, r];
        for (int i = 0; i < r; i++) for (int j = 0; j < r; j++)
            for (int x = 0; x < n; x++) for (int y = 0; y < n; y++)
                result[i, j] += z[x, i] * a[x, y] * z[y, j];
        return result;
    }
    private static double[] Project(double[,] z, double[] u)
    {
        int n = z.GetLength(0), r = z.GetLength(1);
        var result = new double[r];
        for (int j = 0; j < r; j++) for (int i = 0; i < n; i++) result[j] += z[i, j] * u[i];
        return result;
    }
    private static double[] Lift(double[,] z, double[] u)
    {
        int n = z.GetLength(0), r = z.GetLength(1);
        var result = new double[n];
        for (int i = 0; i < n; i++) for (int j = 0; j < r; j++) result[i] += z[i, j] * u[j];
        return result;
    }
    private static double[] MatrixVector(double[,] a, double[] x)
    {
        var result = new double[a.GetLength(0)];
        for (int i = 0; i < result.Length; i++)
            for (int j = 0; j < x.Length; j++) result[i] += a[i, j] * x[j];
        return result;
    }
    private static double[] FullResidual(double[,] mass, double[,] z, double[] redAcceleration,
        double[,] fullDamping, double[] redVelocity, double[] internalForces,
        double[] loadPattern, double factor)
    {
        // Full-space inertial and viscous nodal forces are needed to recover support reactions.
        var ma = MatrixVector(mass, Lift(z, redAcceleration));
        var cv = MatrixVector(fullDamping, Lift(z, redVelocity));
        var result = new double[internalForces.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = ma[i] + cv[i] + internalForces[i] - factor * loadPattern[i];
        return result;
    }
    private static double[] Scale(double[] a, double s) => a.Select(v => v * s).ToArray();
    private static double[] Add(double[] a, double[] b) => a.Zip(b, (x, y) => x + y).ToArray();
    private static double[] Subtract(double[] a, double[] b) => a.Zip(b, (x, y) => x - y).ToArray();
    private static double Dot(double[] a, double[] b) { double sum = 0; for (int i = 0; i < a.Length; i++) sum += a[i] * b[i]; return sum; }
    private static double Norm(double[] a) => Math.Sqrt(Dot(a, a));
    private static bool Finite(double[] a) => a.All(double.IsFinite);
    private static bool IsFinite(double[,] a) => a.Cast<double>().All(double.IsFinite);
}
