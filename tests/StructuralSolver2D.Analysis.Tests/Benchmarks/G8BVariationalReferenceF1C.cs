using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// F1.1-C: isolated, test-only reference for G8B's first-order linear model.
/// Independently forms element stiffness by quadrature of axial strain and
/// Hermite curvature energy; independently integrates nodal load work; solves
/// the constrained system by Cholesky rather than production's element matrices
/// and linear-system solver. It calls NO production FEM/loads/sampling code.
/// Deliberately rejects unsupported input, so it cannot silently claim to
/// validate springs, hinges, settlements, tapered loads, dynamics or other
/// models beyond the four frozen G8B examples.
/// </summary>
internal static class G8BVariationalReferenceF1C
{
    // Four-point Gauss-Legendre rule transformed from [-1,1] to [0,1].
    private static readonly double[] Stations =
    {
        0.06943184420297371, 0.33000947820757187,
        0.6699905217924281, 0.9305681557970262
    };

    private static readonly double[] Weights =
    {
        0.17392742256872693, 0.32607257743127307,
        0.32607257743127307, 0.17392742256872693
    };

    internal sealed record Displacement(double Ux, double Uy, double Rz);
    internal sealed record Reaction(double Rx, double Ry, double Mz);

    internal sealed record MemberForce(
        double StartAxial, double StartShear, double StartMoment,
        double EndAxial, double EndShear, double EndMoment,
        double Length, double Qx, double Qy)
    {
        internal (double N, double V, double M) At(double x)
        {
            if (x < 0 || x > Length)
                throw new ArgumentOutOfRangeException(nameof(x));
            return (-StartAxial - Qx * x,
                StartShear + Qy * x,
                -StartMoment + StartShear * x + Qy * x * x / 2);
        }
    }

    internal sealed record Solution(
        IReadOnlyDictionary<string, Displacement> Displacements,
        IReadOnlyDictionary<string, Reaction> Reactions,
        IReadOnlyDictionary<string, MemberForce> Members);

    private sealed record ReferenceFrame(
        string MemberId, int[] Dofs, double[,] Transformation,
        double[,] LocalStiffness, double[] LocalLoads,
        double Length, double Qx, double Qy);

    private sealed record ReferenceTruss(
        string MemberId, int[] Dofs, double Cosine, double Sine,
        double Length, double AxialRigidity);

    internal static Solution Analyze(StructuralModel model, string combinationId)
    {
        ArgumentNullException.ThrowIfNull(model);
        var combination = model.LoadCombinations.Single(c => c.Id == combinationId);
        var factors = combination.Terms.ToDictionary(t => t.LoadCaseId, t => t.Factor,
            StringComparer.Ordinal);
        var nodes = model.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        var nodeIndices = model.Nodes.Select((node, index) => (node.Id, index))
            .ToDictionary(e => e.Id, e => e.index, StringComparer.Ordinal);
        var sections = model.Sections.ToDictionary(s => s.Id, StringComparer.Ordinal);
        var materials = model.Materials.ToDictionary(m => m.Id, StringComparer.Ordinal);

        int totalDofs = model.Nodes.Count * 3;
        double[,] stiffness = new double[totalDofs, totalDofs];
        double[] applied = new double[totalDofs];
        List<ReferenceFrame> frames = new();
        List<ReferenceTruss> trusses = new();

        double selfWeightFactor = 0;
        foreach (StructuralLoad load in model.Loads.Where(l => l.Type == StructuralLoadType.SelfWeight))
        {
            if (load.TargetType != StructuralLoadTargetType.Model ||
                load.Direction != StructuralLoadDirection.GlobalY ||
                !string.IsNullOrEmpty(load.TargetId) || load.Value <= 0)
                throw new NotSupportedException("F1C accepts only explicit G8B whole-model self-weight.");
            if (factors.TryGetValue(load.LoadCaseId, out double factor))
                selfWeightFactor += load.Value * factor;
        }

        foreach (StructuralMember member in model.Members)
        {
            if (member.ReleaseStartMoment || member.ReleaseEndMoment)
                throw new NotSupportedException("F1C intentionally excludes end releases.");

            StructuralNode a = nodes[member.StartNodeId];
            StructuralNode b = nodes[member.EndNodeId];
            StructuralMaterial material = materials[member.MaterialId];
            StructuralSection section = sections[member.SectionId];
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (!double.IsFinite(length) || length <= 0 ||
                material.ElasticModulus <= 0 || section.Area <= 0 ||
                !material.UnitWeight.HasValue || material.UnitWeight <= 0)
                throw new NotSupportedException("F1C G8B invalid element geometry or material.");

            double cosine = dx / length;
            double sine = dy / length;
            int i = 3 * nodeIndices[a.Id];
            int j = 3 * nodeIndices[b.Id];
            double axialRigidity = material.ElasticModulus * section.Area;
            double lineGravity = -material.UnitWeight.Value * section.Area * selfWeightFactor;

            if (member.Type == MemberType.Truss2D)
            {
                if (model.Loads.Any(l => l.TargetId == member.Id))
                    throw new NotSupportedException("F1C does not support loads directly on G8B trusses.");

                // The truss self-weight is represented by a half-load at each
                // node, consistent with the ideal axial-only G8B idealization.
                applied[i + 1] += lineGravity * length / 2;
                applied[j + 1] += lineGravity * length / 2;

                int[] dofs = { i, i + 1, j, j + 1 };
                double[] projection = { -cosine, -sine, cosine, sine };
                double scale = axialRigidity / length;
                for (int p = 0; p < 4; p++)
                    for (int q = 0; q < 4; q++)
                        stiffness[dofs[p], dofs[q]] += scale * projection[p] * projection[q];

                trusses.Add(new ReferenceTruss(member.Id, dofs, cosine, sine,
                    length, axialRigidity));
                continue;
            }

            if (member.Type != MemberType.Frame2D || section.MomentOfInertia <= 0)
                throw new NotSupportedException("F1C only supports the G8B Frame2D/Truss2D elements.");

            double globalVerticalLoad = lineGravity;
            foreach (StructuralLoad load in model.Loads.Where(l => l.TargetId == member.Id))
            {
                if (load.Type != StructuralLoadType.UniformDistributedLoad ||
                    load.Direction != StructuralLoadDirection.GlobalY ||
                    load.TargetType != StructuralLoadTargetType.Member)
                    throw new NotSupportedException("F1C accepts only global-Y uniform G8B member loads.");
                if (factors.TryGetValue(load.LoadCaseId, out double factor))
                    globalVerticalLoad += load.Value * factor;
            }

            double qx = sine * globalVerticalLoad;
            double qy = cosine * globalVerticalLoad;
            (double[,] kLocal, double[] fLocal) = IntegrateFrame(
                axialRigidity, material.ElasticModulus * section.MomentOfInertia,
                length, qx, qy);
            double[,] transform = LocalTransformation(cosine, sine);
            int[] frameDofs = { i, i + 1, i + 2, j, j + 1, j + 2 };

            for (int p = 0; p < 6; p++)
            {
                for (int r = 0; r < 6; r++)
                    applied[frameDofs[p]] += transform[r, p] * fLocal[r];

                for (int q = 0; q < 6; q++)
                {
                    double transformed = 0;
                    for (int r = 0; r < 6; r++)
                        for (int t = 0; t < 6; t++)
                            transformed += transform[r, p] * kLocal[r, t] * transform[t, q];
                    stiffness[frameDofs[p], frameDofs[q]] += transformed;
                }
            }

            frames.Add(new ReferenceFrame(member.Id, frameDofs, transform, kLocal,
                fLocal, length, qx, qy));
        }

        foreach (StructuralLoad load in model.Loads.Where(l => l.Type == StructuralLoadType.NodalForce))
        {
            if (load.TargetType != StructuralLoadTargetType.Node)
                throw new NotSupportedException("F1C only supports nodal forces on G8B nodes.");
            if (!factors.TryGetValue(load.LoadCaseId, out double factor))
                continue;
            int index = 3 * nodeIndices[load.TargetId];
            if (load.Direction == StructuralLoadDirection.GlobalX)
                applied[index] += load.Value * factor;
            else if (load.Direction == StructuralLoadDirection.GlobalY)
                applied[index + 1] += load.Value * factor;
            else
                throw new NotSupportedException("F1C only supports global X/Y nodal forces.");
        }

        if (model.Loads.Any(l => l.Type != StructuralLoadType.SelfWeight &&
            l.Type != StructuralLoadType.UniformDistributedLoad &&
            l.Type != StructuralLoadType.NodalForce))
            throw new NotSupportedException("F1C encountered a non-G8B load type.");

        // Frozen G8B has four fixed bases, no spring/settlement/rotated support.
        bool[] restrained = new bool[totalDofs];
        foreach (StructuralSupport support in model.Supports)
        {
            if (!support.RestrainedUx || !support.RestrainedUy || !support.RestrainedRz ||
                support.OrientationDegrees != 0 || support.StiffnessUx != 0 ||
                support.StiffnessUy != 0 || support.StiffnessRz != 0)
                throw new NotSupportedException("F1C only supports G8B's four fixed base supports.");
            int index = 3 * nodeIndices[support.NodeId];
            restrained[index] = true;
            restrained[index + 1] = true;
            restrained[index + 2] = true;
        }

        int[] free = Enumerable.Range(0, totalDofs).Where(k => !restrained[k]).ToArray();
        double[,] reduced = new double[free.Length, free.Length];
        double[] rightHandSide = new double[free.Length];
        for (int p = 0; p < free.Length; p++)
        {
            rightHandSide[p] = applied[free[p]];
            for (int q = 0; q < free.Length; q++)
                reduced[p, q] = stiffness[free[p], free[q]];
        }

        double[] solved = SolveCholesky(reduced, rightHandSide);
        double[] displacementVector = new double[totalDofs];
        for (int p = 0; p < free.Length; p++)
            displacementVector[free[p]] = solved[p];

        var displacements = new Dictionary<string, Displacement>(StringComparer.Ordinal);
        foreach (StructuralNode node in model.Nodes)
        {
            int index = 3 * nodeIndices[node.Id];
            displacements[node.Id] = new Displacement(displacementVector[index],
                displacementVector[index + 1], displacementVector[index + 2]);
        }

        var reactions = new Dictionary<string, Reaction>(StringComparer.Ordinal);
        foreach (StructuralSupport support in model.Supports)
        {
            int index = 3 * nodeIndices[support.NodeId];
            double[] r = new double[3];
            for (int p = 0; p < 3; p++)
            {
                double sum = -applied[index + p];
                for (int k = 0; k < totalDofs; k++)
                    sum += stiffness[index + p, k] * displacementVector[k];
                r[p] = sum;
            }
            reactions[support.NodeId] = new Reaction(r[0], r[1], r[2]);
        }

        var memberForces = new Dictionary<string, MemberForce>(StringComparer.Ordinal);
        foreach (ReferenceFrame frame in frames)
        {
            double[] localDisplacements = new double[6];
            for (int p = 0; p < 6; p++)
                for (int q = 0; q < 6; q++)
                    localDisplacements[p] += frame.Transformation[p, q] *
                        displacementVector[frame.Dofs[q]];

            double[] f = new double[6];
            for (int p = 0; p < 6; p++)
            {
                f[p] = -frame.LocalLoads[p];
                for (int q = 0; q < 6; q++)
                    f[p] += frame.LocalStiffness[p, q] * localDisplacements[q];
            }

            memberForces[frame.MemberId] = new MemberForce(
                f[0], f[1], f[2], f[3], f[4], f[5],
                frame.Length, frame.Qx, frame.Qy);
        }

        foreach (ReferenceTruss truss in trusses)
        {
            double[] d = truss.Dofs.Select(k => displacementVector[k]).ToArray();
            double extension = truss.Cosine * (d[2] - d[0])
                + truss.Sine * (d[3] - d[1]);
            double axial = truss.AxialRigidity * extension / truss.Length;
            memberForces[truss.MemberId] = new MemberForce(
                -axial, 0, 0, axial, 0, 0, truss.Length, 0, 0);
        }

        return new Solution(displacements, reactions, memberForces);
    }

    private static (double[,] Stiffness, double[] Loads) IntegrateFrame(
        double ea, double ei, double length, double qx, double qy)
    {
        double[,] k = new double[6, 6];
        double[] f = new double[6];
        for (int station = 0; station < Stations.Length; station++)
        {
            double t = Stations[station];
            double multiplier = Weights[station] * length;
            double t2 = t * t;
            double t3 = t2 * t;

            double[] axialStrain = { -1 / length, 0, 0, 1 / length, 0, 0 };
            double[] curvature =
            {
                0, (-6 + 12 * t) / (length * length), (-4 + 6 * t) / length,
                0, (6 - 12 * t) / (length * length), (-2 + 6 * t) / length
            };
            double[] axialShape = { 1 - t, 0, 0, t, 0, 0 };
            double[] transverseShape =
            {
                0, 1 - 3 * t2 + 2 * t3,
                length * (t - 2 * t2 + t3), 0,
                3 * t2 - 2 * t3, length * (-t2 + t3)
            };

            for (int p = 0; p < 6; p++)
            {
                f[p] += multiplier * (qx * axialShape[p] + qy * transverseShape[p]);
                for (int q = 0; q < 6; q++)
                    k[p, q] += multiplier * (ea * axialStrain[p] * axialStrain[q]
                        + ei * curvature[p] * curvature[q]);
            }
        }
        return (k, f);
    }

    private static double[,] LocalTransformation(double cosine, double sine)
    {
        double[,] transform = new double[6, 6];
        transform[0, 0] = cosine;
        transform[0, 1] = sine;
        transform[1, 0] = -sine;
        transform[1, 1] = cosine;
        transform[2, 2] = 1;
        transform[3, 3] = cosine;
        transform[3, 4] = sine;
        transform[4, 3] = -sine;
        transform[4, 4] = cosine;
        transform[5, 5] = 1;
        return transform;
    }

    /// <summary>Independent SPD factorization, without production LU or pivot code.</summary>
    private static double[] SolveCholesky(double[,] a, double[] rhs)
    {
        int n = rhs.Length;
        double[,] lower = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j <= i; j++)
            {
                double sum = a[i, j];
                for (int k = 0; k < j; k++)
                    sum -= lower[i, k] * lower[j, k];

                if (i == j)
                {
                    if (!(sum > 0) || !double.IsFinite(sum))
                        throw new InvalidOperationException(
                            $"Independent G8B reference matrix is not SPD (pivot {i}, {sum:R}).");
                    lower[i, j] = Math.Sqrt(sum);
                }
                else
                    lower[i, j] = sum / lower[j, j];
            }

        double[] intermediate = new double[n];
        for (int i = 0; i < n; i++)
        {
            double sum = rhs[i];
            for (int j = 0; j < i; j++)
                sum -= lower[i, j] * intermediate[j];
            intermediate[i] = sum / lower[i, i];
        }

        double[] solved = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = intermediate[i];
            for (int j = i + 1; j < n; j++)
                sum -= lower[j, i] * solved[j];
            solved[i] = sum / lower[i, i];
        }
        return solved;
    }
}
