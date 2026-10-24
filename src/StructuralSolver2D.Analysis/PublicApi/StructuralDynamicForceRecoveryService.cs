using StructuralSolver2D.Analysis.Dynamics.Assembly;
using StructuralSolver2D.Analysis.Dynamics.Modal;
using StructuralSolver2D.Analysis.Dynamics.Reduction;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Resultant constraint reaction in GLOBAL coordinates. Not a force allocation to individual
/// supports: such allocation is non-unique at redundant/coincident or inclined restraints.
/// </summary>
public sealed record StructuralDynamicNodeReaction(string NodeId, double Fx, double Fy, double Mz);

/// <summary>
/// Local ELASTIC generalized member nodal forces [N1,V1,M1,N2,V2,M2] (kN,kN,kNm).
/// This is K_element * u_element: it is NOT the exact spatial section force diagram
/// during acceleration, nor does it include consistent member inertia/damping loads.
/// </summary>
public sealed record StructuralDynamicMemberElasticEndForces(
    string MemberId, double StartAxial, double StartShear, double StartMoment,
    double EndAxial, double EndShear, double EndMoment);

/// <summary>Maximum absolute sampled value and the time of its first occurrence.</summary>
public sealed record StructuralDynamicExtremum(double MaxAbsolute, double TimeSeconds);
public sealed record StructuralDynamicReactionEnvelope(
    string NodeId, StructuralDynamicExtremum Fx, StructuralDynamicExtremum Fy,
    StructuralDynamicExtremum Mz);
public sealed record StructuralDynamicMemberEnvelope(
    string MemberId,
    StructuralDynamicExtremum StartAxial, StructuralDynamicExtremum StartShear,
    StructuralDynamicExtremum StartMoment, StructuralDynamicExtremum EndAxial,
    StructuralDynamicExtremum EndShear, StructuralDynamicExtremum EndMoment);

public sealed record StructuralDynamicForceStep(
    int StepIndex, double TimeSeconds,
    IReadOnlyList<StructuralDynamicNodeReaction> SupportNodeReactions,
    IReadOnlyList<StructuralDynamicMemberElasticEndForces> MemberElasticEndForces,
    double MaxAdmissibleEquilibriumResidual);

/// <summary>Separate, opt-in M67 output. Does not change M59 historical result contract.</summary>
public sealed record StructuralDynamicForceRecoveryOutput(
    StructuralTimeHistoryAnalysisOutput Motion,
    IReadOnlyList<StructuralDynamicForceStep> Steps,
    IReadOnlyList<StructuralDynamicReactionEnvelope> ReactionEnvelopes,
    IReadOnlyList<StructuralDynamicMemberEnvelope> MemberEnvelopes,
    double MaximumAdmissibleEquilibriumResidual,
    IReadOnlyList<string> Warnings);

/// <summary>
/// M67 linear transient reaction and elastic end-force recovery. Requires the same homogeneous
/// boundary conditions and consistent mass as M59. The global residual M*a+C*v+K*u-F
/// is the NET constrained nodal reaction. Rayleigh C is physical full-space alpha*M+beta*K;
/// uniform modal C uses the explicitly defined extension M*Phi*diag(2*zeta*omega)*Phi^T*M.
/// No splitting among redundant support objects and no nonlinear force-history recovery.
/// </summary>
public sealed class StructuralDynamicForceRecoveryService
{
    public StructuralDynamicForceRecoveryOutput Analyze(StructuralTimeHistoryAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        // M59 validates input, initial constraints and load histories before this post-processing.
        StructuralTimeHistoryAnalysisOutput motion = new StructuralTimeHistoryAnalysisService().Analyze(request);
        StructuralModel model = request.Model;
        DynamicMatrixAssembly assembly = DynamicMatrixAssembler.Assemble(model, request.Dynamics);
        DynamicReductionResult reduced = DynamicBoundaryConditionReducer.Reduce(model, assembly);
        if (!reduced.ReadyForModalSolver)
            throw new InvalidOperationException($"M67_REDUCTION: {reduced.DiagnosticCode}");
        int dofs = model.Nodes.Count * 3;
        // Construct the force model with the same modal convention used by M59.
        double[,]? modalDamping = null;
        double alpha = 0, beta = 0;
        if (request.Damping.UniformModalRatio.HasValue)
        {
            ExperimentalModalSpectrum spectrum = SymmetricGeneralizedModalSolver.Solve(
                reduced, reduced.Stiffness.GetLength(0));
            modalDamping = BuildModalDamping(assembly.Mass, spectrum, dofs,
                request.Damping.UniformModalRatio.Value);
        }
        else
        {
            alpha = request.Damping.RayleighAlphaInverseSeconds!.Value;
            beta = request.Damping.RayleighBetaSeconds!.Value;
        }
        var modelMembers = PrepareMembers(model, assembly.NodeIndices);
        var reactionNodes = model.Supports.Select(s => s.NodeId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id => (Id: id, Index: assembly.NodeIndices[id]))
            .OrderBy(entry => entry.Index).ToArray();
        var result = new List<StructuralDynamicForceStep>(motion.Steps.Count);
        var reactionEnv = new Dictionary<string, StructuralDynamicExtremum[]>(StringComparer.OrdinalIgnoreCase);
        var memberEnv = new Dictionary<string, StructuralDynamicExtremum[]>(StringComparer.OrdinalIgnoreCase);
        double overallResidual = 0;

        foreach (StructuralTimeStepResult step in motion.Steps)
        {
            var u = new double[dofs]; var v = new double[dofs]; var a = new double[dofs];
            var f = new double[dofs];
            for (int i = 0; i < model.Nodes.Count; i++)
            {
                StructuralTimeNodeResult n = step.Nodes[i];
                if (!string.Equals(n.NodeId, model.Nodes[i].Id, StringComparison.Ordinal))
                    throw new InvalidOperationException("M67_NODE_ORDER: M59 output node ordering changed.");
                int j = i * 3;
                u[j] = n.Ux; u[j+1] = n.Uy; u[j+2] = n.Rz;
                v[j] = n.Vx; v[j+1] = n.Vy; v[j+2] = n.VrZ;
                a[j] = n.Ax; a[j+1] = n.Ay; a[j+2] = n.ArZ;
                f[j] = n.Fx; f[j+1] = n.Fy; f[j+2] = n.Mz;
            }
            double[] ku = Multiply(assembly.Stiffness, u);
            double[] ma = Multiply(assembly.Mass, a);
            double[] cv;
            if (modalDamping is not null) cv = Multiply(modalDamping, v);
            else
            {
                double[] mv = Multiply(assembly.Mass, v);
                double[] kv = Multiply(assembly.Stiffness, v);
                cv = new double[dofs];
                for (int j = 0; j < dofs; j++) cv[j] = alpha * mv[j] + beta * kv[j];
            }
            var residual = new double[dofs];
            for (int j = 0; j < dofs; j++)
            {
                residual[j] = ku[j] + ma[j] + cv[j] - f[j];
                if (!double.IsFinite(residual[j]))
                    throw new InvalidOperationException("M67_NONFINITE_REACTION: Force residual overflow.");
            }
            double freeResidual = 0;
            for (int col = 0; col < reduced.Basis.GetLength(1); col++)
            {
                double value = 0;
                for (int row = 0; row < dofs; row++) value += reduced.Basis[row,col] * residual[row];
                freeResidual = Math.Max(freeResidual, Math.Abs(value));
            }
            overallResidual = Math.Max(overallResidual, freeResidual);
            var reactions = reactionNodes.Select(node =>
            {
                int j = node.Index * 3;
                var reaction = new StructuralDynamicNodeReaction(node.Id,
                    residual[j], residual[j+1], residual[j+2]);
                UpdateEnvelope(reactionEnv, node.Id,
                    new[] { reaction.Fx, reaction.Fy, reaction.Mz }, step.TimeSeconds);
                return reaction;
            }).ToArray();
            var members = modelMembers.Select(member =>
            {
                StructuralDynamicMemberElasticEndForces forces = member.Evaluate(u);
                UpdateEnvelope(memberEnv, member.Id, new[] { forces.StartAxial, forces.StartShear,
                    forces.StartMoment, forces.EndAxial, forces.EndShear, forces.EndMoment }, step.TimeSeconds);
                return forces;
            }).ToArray();
            result.Add(new StructuralDynamicForceStep(step.StepIndex, step.TimeSeconds,
                reactions, members, freeResidual));
        }
        var reactionEnvelopes = reactionNodes.Select(node =>
        {
            var x = reactionEnv[node.Id];
            return new StructuralDynamicReactionEnvelope(node.Id, x[0], x[1], x[2]);
        }).ToArray();
        var memberEnvelopes = modelMembers.Select(member =>
        {
            var x = memberEnv[member.Id];
            return new StructuralDynamicMemberEnvelope(member.Id, x[0], x[1], x[2], x[3], x[4], x[5]);
        }).ToArray();
        var warnings = new List<string>
        {
            "M67_LINEAR_ONLY: This output applies to M59 LINEAR nodal time histories; nonlinear M65 internal states are not reconstructed.",
            "M67_REACTION: M*a+C*v+K*u-F gives the NET GLOBAL nodal constraint reaction. Redundant support shares are not uniquely determined.",
            "M67_MEMBER: Member forces are local ELASTIC GENERALIZED END actions K_local*u_local; no inertia/damping-adjusted section N/V/M(x,t) diagrams are claimed.",
            "M67_ENVELOPES: Absolute extrema are sampled at time-grid points only; their occurrence times are not necessarily simultaneous."
        };
        if (modalDamping is not null && request.Damping.UniformModalRatio.GetValueOrDefault() != 0)
            warnings.Add("M67_MODAL_DAMPING: Reaction damping uses the specified mass-orthogonal full-space modal extension; support damping reactions are model-dependent. Rayleigh C provides an unambiguous assembled-element alternative.");
        double loadScale = motion.Steps.SelectMany(s => s.Nodes)
            .SelectMany(n => new[] { Math.Abs(n.Fx), Math.Abs(n.Fy), Math.Abs(n.Mz) })
            .DefaultIfEmpty(0).Max();
        if (overallResidual > 1e-7 * Math.Max(1, loadScale))
            warnings.Add($"M67_EQUILIBRIUM_RESIDUAL: Max reduced free-DOF residual {overallResidual:G7} exceeds the indicative 1e-7 relative-to-load check; inspect timestep, scaling and conditioning.");
        return new StructuralDynamicForceRecoveryOutput(motion, result, reactionEnvelopes,
            memberEnvelopes, overallResidual, warnings);
    }

    private static void UpdateEnvelope(Dictionary<string, StructuralDynamicExtremum[]> envelopes,
        string id, double[] values, double time)
    {
        if (!envelopes.TryGetValue(id, out StructuralDynamicExtremum[]? current))
        {
            current = values.Select(x => new StructuralDynamicExtremum(Math.Abs(x), time)).ToArray();
            envelopes.Add(id, current);
            return;
        }
        for (int i = 0; i < values.Length; i++)
            if (Math.Abs(values[i]) > current[i].MaxAbsolute)
                current[i] = new StructuralDynamicExtremum(Math.Abs(values[i]), time);
    }

    private static double[] Multiply(double[,] matrix, double[] vector)
    {
        var result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
            for (int j = 0; j < vector.Length; j++) result[i] += matrix[i,j] * vector[j];
        return result;
    }

    private static double[,] BuildModalDamping(double[,] mass, ExperimentalModalSpectrum spectrum,
        int n, double ratio)
    {
        var c = new double[n,n];
        if (ratio == 0) return c;
        foreach (var mode in spectrum.Modes)
        {
            double[] mp = Multiply(mass, mode.GlobalMassNormalizedShape);
            double factor = 2 * ratio * mode.AngularFrequencyRadiansPerSecond;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++) c[i,j] += factor * mp[i] * mp[j];
        }
        return c;
    }

    private sealed record PreparedMember(string Id, Func<double[], StructuralDynamicMemberElasticEndForces> Evaluate);
    private static IReadOnlyList<PreparedMember> PrepareMembers(StructuralModel model,
        IReadOnlyDictionary<string,int> indices)
    {
        var nodes = model.Nodes.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var materials = model.Materials.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var sections = model.Sections.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var result = new List<PreparedMember>();
        foreach (StructuralMember member in model.Members)
        {
            StructuralNode a = nodes[member.StartNodeId], b = nodes[member.EndNodeId];
            double length = StructuralMember.GetLength(a,b);
            double c = (b.X-a.X)/length, s = (b.Y-a.Y)/length;
            double e = materials[member.MaterialId].ElasticModulus;
            StructuralSection section = sections[member.SectionId];
            int ia = 3 * indices[member.StartNodeId], ib = 3 * indices[member.EndNodeId];
            if (member.Type == MemberType.Truss2D)
            {
                result.Add(new PreparedMember(member.Id, u =>
                {
                    double delta = (u[ib]-u[ia])*c + (u[ib+1]-u[ia+1])*s;
                    double n = e*section.Area/length * delta;
                    return new StructuralDynamicMemberElasticEndForces(member.Id, -n,0,0,n,0,0);
                }));
            }
            else if (member.Type == MemberType.Frame2D)
            {
                double[,] localK = Frame2DElementMatrices.BuildLocalStiffness(e,section.Area,section.MomentOfInertia,length);
                double[,] t = Frame2DElementMatrices.BuildTransformation(c,s);
                result.Add(new PreparedMember(member.Id, u =>
                {
                    double[] ug = {u[ia],u[ia+1],u[ia+2],u[ib],u[ib+1],u[ib+2]};
                    double[] fl = Multiply(localK,Multiply(t,ug));
                    return new StructuralDynamicMemberElasticEndForces(member.Id,
                        fl[0],fl[1],fl[2],fl[3],fl[4],fl[5]);
                }));
            }
            else throw new NotSupportedException($"M67_MEMBER_TYPE_UNSUPPORTED: {member.Type}");
        }
        return result;
    }
}
