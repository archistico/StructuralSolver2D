using StructuralSolver2D.Analysis.Truss2D.Nonlinear;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// M68 opt-in public adapter around the M64/M65 research solvers.
/// No Frame2D bending, P-Delta, large rotations, damage, collapse or code compliance.
/// Failed increments are returned explicitly and never represented as completed analyses.
/// </summary>
public sealed class StructuralNonlinearTruss2DService
{
    public StructuralNonlinearStaticOutput AnalyzeStatic(StructuralNonlinearStaticRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateModel(request.Model);
        ArgumentNullException.ThrowIfNull(request.LoadFactors);
        IncrementalTrussResult result = IncrementalElastoplasticTruss2DSolver.Solve(new IncrementalTrussRequest(
            Nodes(request.Model), StaticMembers(request.Model), Supports(request.Model), Loads(request.Model),
            request.LoadFactors.ToArray(), request.MaxIterations,
            request.AbsoluteForceTolerance, request.RelativeForceTolerance));
        return new StructuralNonlinearStaticOutput(result.Completed, result.Failure,
            NodeIds(request.Model), MemberIds(request.Model),
            result.Steps.Select((s, i) => new StructuralNonlinearStaticStep(i, s.LoadFactor,
                s.NewtonIterations, s.ResidualNorm, (double[])s.Displacements.Clone(),
                (double[])s.Reactions.Clone(), (double[])s.AxialForces.Clone(),
                States(s.MaterialStates), s.IncrementalPlasticDissipation,
                s.CumulativePlasticDissipation)).ToArray(), States(result.FinalStates));
    }

    public StructuralNonlinearTimeOutput AnalyzeTime(StructuralNonlinearTimeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateModel(request.Model);
        ArgumentNullException.ThrowIfNull(request.LoadFactors);
        NonlinearTruss2DTimeResult result = NonlinearTruss2DNewmark.Solve(new NonlinearTruss2DTimeRequest(
            Nodes(request.Model), TimeMembers(request.Model), Supports(request.Model), Loads(request.Model),
            request.LoadFactors.ToArray(), request.TimeStep, request.MassDamping,
            request.InitialStiffnessDamping, request.InitialVelocity?.ToArray(),
            request.MaximumNewtonIterations, request.AbsoluteForceTolerance,
            request.RelativeForceTolerance));
        return new StructuralNonlinearTimeOutput(result.Completed, result.Failure,
            NodeIds(request.Model), MemberIds(request.Model),
            result.Steps.Select((s, i) => new StructuralNonlinearTimeStep(i, s.Time,
                s.LoadFactor, s.NewtonIterations, s.ReducedResidualNorm,
                (double[])s.Displacements.Clone(), (double[])s.Velocities.Clone(),
                (double[])s.Accelerations.Clone(), (double[])s.Reactions.Clone(),
                (double[])s.AxialForces.Clone(), States(s.MaterialStates),
                s.KineticEnergy, s.RecoverableEnergy, s.HardeningEnergy,
                s.CumulativePlasticDissipation, s.CumulativeViscousDissipation,
                s.CumulativeExternalWork, s.CumulativeInternalTrapezoidWork,
                s.DiscreteWorkResidual, s.PhysicalEnergyResidual)).ToArray(), States(result.FinalStates));
    }

    private static void ValidateModel(NonlinearTruss2DModel? model)
    {
        if (model is null || model.Nodes is null || model.Members is null || model.Supports is null ||
            model.Loads is null || model.Nodes.Any(n => n is null) || model.Members.Any(m => m is null) ||
            model.Supports.Any(s => s is null) || model.Loads.Any(l => l is null))
            throw new ArgumentException("M68_MODEL: Nonlinear Truss2D requires nodes, members, supports and loads.");
    }

    private static IncrementalTrussNode[] Nodes(NonlinearTruss2DModel m) =>
        m.Nodes.Select(n => new IncrementalTrussNode(n.Id, n.X, n.Y)).ToArray();
    private static IncrementalTrussMember[] StaticMembers(NonlinearTruss2DModel m) =>
        m.Members.Select(x => new IncrementalTrussMember(x.Id, x.StartNodeId, x.EndNodeId,
            x.Area, x.ElasticModulus, x.YieldStress, x.HardeningModulus)).ToArray();
    private static NonlinearDynamicTrussMember[] TimeMembers(NonlinearTruss2DModel m) =>
        m.Members.Select(x => new NonlinearDynamicTrussMember(x.Id, x.StartNodeId, x.EndNodeId,
            x.Area, x.ElasticModulus, x.YieldStress, x.HardeningModulus,
            x.MassPerLength ?? throw new ArgumentException($"M68_MASS: Member '{x.Id}' requires massPerLength for time integration."))).ToArray();
    private static IncrementalTrussSupport[] Supports(NonlinearTruss2DModel m) =>
        m.Supports.Select(x => new IncrementalTrussSupport(x.NodeId, x.NormalX, x.NormalY)).ToArray();
    private static IncrementalTrussLoad[] Loads(NonlinearTruss2DModel m) =>
        m.Loads.Select(x => new IncrementalTrussLoad(x.NodeId, x.Fx, x.Fy)).ToArray();
    private static string[] NodeIds(NonlinearTruss2DModel m) => m.Nodes.Select(n => n.Id).ToArray();
    private static string[] MemberIds(NonlinearTruss2DModel m) => m.Members.Select(x => x.Id).ToArray();
    private static StructuralNonlinearMaterialState[] States(IEnumerable<AxialMaterialState> states) =>
        states.Select(x => new StructuralNonlinearMaterialState(x.PlasticStrain, x.AccumulatedPlasticStrain)).ToArray();
}
