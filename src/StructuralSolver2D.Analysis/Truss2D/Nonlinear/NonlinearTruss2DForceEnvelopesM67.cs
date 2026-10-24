namespace StructuralSolver2D.Analysis.Truss2D.Nonlinear;

/// <summary>Absolute sampled peak and first occurrence [kN or unit indicated].</summary>
internal sealed record NonlinearTruss2DExtremum(double Magnitude, double TimeSeconds);
internal sealed record NonlinearTruss2DMemberForceEnvelope(string MemberId,
    NonlinearTruss2DExtremum AxialForce,
    NonlinearTruss2DExtremum AccumulatedPlasticStrain);
internal sealed record NonlinearTruss2DNodeReactionEnvelope(string NodeId,
    NonlinearTruss2DExtremum Fx, NonlinearTruss2DExtremum Fy);
internal sealed record NonlinearTruss2DForceEnvelopeResult(
    IReadOnlyList<NonlinearTruss2DMemberForceEnvelope> Members,
    IReadOnlyList<NonlinearTruss2DNodeReactionEnvelope> NodeReactions,
    NonlinearTruss2DExtremum CumulativePlasticDissipation,
    bool IncludesOnlyAcceptedSteps);

/// <summary>
/// M67 research-only aggregation of ALREADY RECOVERED M65 internal forces and full
/// dynamic reactions. No recomputation with elastic stiffness and no inclusion of a
/// rejected Newton trial. Result is not part of the public API until M68.
/// </summary>
internal static class NonlinearTruss2DForceEnvelopesM67
{
    internal static NonlinearTruss2DForceEnvelopeResult FromAcceptedSteps(
        NonlinearTruss2DTimeRequest request, NonlinearTruss2DTimeResult result)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(result);
        if (result.Steps is null || result.Steps.Count == 0 ||
            result.Steps.Any(step => step.Reactions.Length != request.Nodes.Count*2 ||
                step.AxialForces.Length != request.Members.Count ||
                step.MaterialStates.Length != request.Members.Count))
            throw new ArgumentException("M67_NONLINEAR_HISTORY: Incomplete accepted step data.");
        var memberResult = new List<NonlinearTruss2DMemberForceEnvelope>();
        for (int j = 0; j < request.Members.Count; j++)
        {
            memberResult.Add(new NonlinearTruss2DMemberForceEnvelope(request.Members[j].Id,
                Peak(result.Steps, step => step.AxialForces[j]),
                Peak(result.Steps, step => step.MaterialStates[j].AccumulatedPlasticStrain)));
        }
        var nodeResult = new List<NonlinearTruss2DNodeReactionEnvelope>();
        for (int j = 0; j < request.Nodes.Count; j++)
        {
            int index = j*2;
            nodeResult.Add(new NonlinearTruss2DNodeReactionEnvelope(request.Nodes[j].Id,
                Peak(result.Steps, step => step.Reactions[index]),
                Peak(result.Steps, step => step.Reactions[index+1])));
        }
        return new NonlinearTruss2DForceEnvelopeResult(memberResult, nodeResult,
            Peak(result.Steps, step => step.CumulativePlasticDissipation), true);
    }

    private static NonlinearTruss2DExtremum Peak(IReadOnlyList<NonlinearTruss2DTimeStep> steps,
        Func<NonlinearTruss2DTimeStep, double> quantity)
    {
        double value = -1, time = 0;
        foreach (NonlinearTruss2DTimeStep step in steps)
        {
            double current = Math.Abs(quantity(step));
            if (!double.IsFinite(current))
                throw new ArgumentException("M67_NONLINEAR_RANGE: Nonfinite accepted step result.");
            if (current > value) { value = current; time = step.Time; }
        }
        return new NonlinearTruss2DExtremum(value, time);
    }
}
