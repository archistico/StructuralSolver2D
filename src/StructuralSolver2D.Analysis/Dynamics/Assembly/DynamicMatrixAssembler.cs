using StructuralSolver2D.Analysis.Dynamics.Elements;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Dynamics.Assembly;

/// <summary>
/// Unconstrained raw stiffness/mass pair. Node DOFs are [Ux, Uy, Rz] in input order.
/// No boundary-condition projection, elimination of massless DOFs or eigenanalysis (M54+).
/// </summary>
internal sealed record DynamicMatrixAssembly(
    double[,] Stiffness,
    double[,] Mass,
    IReadOnlyDictionary<string, int> NodeIndices,
    IReadOnlyList<int> MasslessDofIndices,
    double MemberTranslationalMass,
    double AddedTranslationalMass);

internal enum ElementMassFormulation { Consistent, EducationalLumped }

/// <summary>
/// M53 opt-in independent assembler; does not affect any production static solve or CLI command.
/// End releases are intentionally unsupported until a dynamically valid reduction is established.
/// </summary>
internal static class DynamicMatrixAssembler
{
    public static DynamicMatrixAssembly Assemble(StructuralModel model,
        StructuralDynamicsDefinition dynamics,
        ElementMassFormulation formulation = ElementMassFormulation.Consistent)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(dynamics);
        var validation = new StructuralDynamicsValidator().Validate(model, dynamics);
        if (!validation.IsValid)
            throw new InvalidOperationException("M53 dynamics validation failed: " +
                string.Join("; ", validation.Errors.Take(8).Select(x => $"{x.Code}: {x.Message}")));
        if (!Enum.IsDefined(formulation))
            throw new ArgumentOutOfRangeException(nameof(formulation));
        foreach (var member in model.Members)
        {
            if (member.ReleaseStartMoment || member.ReleaseEndMoment)
                throw new NotSupportedException($"M53_RELEASE_UNSUPPORTED: Member '{member.Id}' has an end-moment release. " +
                    "Dynamic releases require internal inertia DOFs or proven frequency-dependent reduction (M54+). " +
                    "Static stiffness-only condensation cannot be reused for mass.");
        }

        var indices = model.Nodes.Select((node, index) => (node.Id, index))
            .ToDictionary(item => item.Id, item => item.index, StringComparer.OrdinalIgnoreCase);
        var nodes = model.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        var materials = model.Materials.ToDictionary(mat => mat.Id, StringComparer.OrdinalIgnoreCase);
        var sections = model.Sections.ToDictionary(section => section.Id, StringComparer.OrdinalIgnoreCase);
        int dofCount = checked(model.Nodes.Count * 3);
        var k = new double[dofCount, dofCount];
        var m = new double[dofCount, dofCount];
        double memberMass = 0;
        double additionalMass = 0;

        foreach (var member in model.Members)
        {
            var start = nodes[member.StartNodeId];
            var end = nodes[member.EndNodeId];
            var section = sections[member.SectionId];
            var material = materials[member.MaterialId];
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double length = StructuralMember.GetLength(start, end);
            double cosine = dx / length;
            double sine = dy / length;
            double mu = DynamicMassUnits.LineMassFromUnitWeight(material.UnitWeight!.Value, section.Area);
            double physicalMemberMass = DynamicMassUnits.MemberMassFromUnitWeight(material.UnitWeight.Value, section.Area, length);
            memberMass = CheckedSum(memberMass, physicalMemberMass, "total member mass");
            int a = indices[member.StartNodeId] * 3;
            int b = indices[member.EndNodeId] * 3;

            switch (member.Type)
            {
                case MemberType.Frame2D:
                    {
                        double[,] kl = Frame2DElementMatrices.BuildLocalStiffness(
                            material.ElasticModulus, section.Area, section.MomentOfInertia, length);
                        double[,] ml = formulation == ElementMassFormulation.Consistent
                            ? Frame2DMassMatrices.ConsistentLocal(mu, length)
                            : Frame2DMassMatrices.LumpedLocal(mu, length);
                        int[] dofs = { a, a + 1, a + 2, b, b + 1, b + 2 };
                        double[,] transform = Frame2DElementMatrices.BuildTransformation(cosine, sine);
                        AddMatrix(k, Frame2DElementMatrices.TransformStiffnessToGlobal(kl, transform), dofs);
                        AddMatrix(m, Frame2DElementMatrices.TransformStiffnessToGlobal(ml, transform), dofs);
                        break;
                    }
                case MemberType.Truss2D:
                    {
                        int[] dofs = { a, a + 1, b, b + 1 };
                        AddMatrix(k, Truss2DElementMatrices.BuildGlobalStiffness(
                            material.ElasticModulus, section.Area, length, cosine, sine), dofs);
                        AddMatrix(m, formulation == ElementMassFormulation.Consistent
                            ? Truss2DMassMatrices.Consistent(mu, length)
                            : Truss2DMassMatrices.Lumped(mu, length), dofs);
                        break;
                    }
                default:
                    throw new NotSupportedException($"M53_MEMBER_TYPE_UNSUPPORTED: {member.Type}");
            }
        }

        // Additional nodal mass is inserted ONCE per node, not per attached member.
        foreach (var nodeMass in dynamics.AdditionalNodalMasses)
        {
            int a = indices[nodeMass.NodeId] * 3;
            m[a, a] = CheckedSum(m[a, a], nodeMass.TranslationalMass, "nodal Ux mass");
            m[a + 1, a + 1] = CheckedSum(m[a + 1, a + 1], nodeMass.TranslationalMass, "nodal Uy mass");
            if (nodeMass.RotationalInertia.HasValue)
                m[a + 2, a + 2] = CheckedSum(m[a + 2, a + 2], nodeMass.RotationalInertia.Value, "nodal inertia");
            additionalMass = CheckedSum(additionalMass, nodeMass.TranslationalMass, "additional translational mass");
        }
        var zeroMassDofs = new List<int>();
        for (int i = 0; i < dofCount; i++)
            if (m[i, i] == 0) zeroMassDofs.Add(i);

        return new DynamicMatrixAssembly(k, m, indices, zeroMassDofs, memberMass, additionalMass);
    }

    private static double CheckedSum(double left, double right, string context)
    {
        double result = left + right;
        if (!double.IsFinite(result))
            throw new ArithmeticException($"M53_MASS_OVERFLOW: Non-finite {context}.");
        return result;
    }

    private static void AddMatrix(double[,] global, double[,] local, int[] dofs)
    {
        for (int i = 0; i < dofs.Length; i++)
            for (int j = 0; j < dofs.Length; j++)
            {
                int r = dofs[i];
                int c = dofs[j];
                double result = global[r, c] + local[i, j];
                if (!double.IsFinite(result))
                    throw new ArithmeticException($"M53_MATRIX_OVERFLOW: Matrix entry [{r},{c}] is not finite.");
                global[r, c] = result;
            }
    }
}
