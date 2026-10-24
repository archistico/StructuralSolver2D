using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Frame2D;

/// <summary>
/// Post-processes independently solved first-order static scenarios using M78 exact
/// piecewise section-force extrema. Never interpolates sample diagrams or mixes
/// simultaneous actions with alternative combination results.
/// </summary>
internal sealed class Frame2DStaticForceEnvelopeBuilderM79
{
    private static readonly InternalForceQuantity[] Quantities =
    {
        InternalForceQuantity.NormalForce,
        InternalForceQuantity.ShearForce,
        InternalForceQuantity.BendingMoment,
    };

    public StructuralStaticForceEnvelopeM79 Build(
        StructuralModel model,
        IReadOnlyList<(StaticForceEnvelopeScenarioM79 Scenario, StructuralAnalysisResult Result)> solutions)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(solutions);
        if (solutions.Count == 0)
            throw new StructuralAnalysisException("M79_EMPTY_TARGETS: At least one static analysis target is required.");

        string[] frameIds = model.Members.Where(member => member.Type == MemberType.Frame2D)
            .Select(member => member.Id).ToArray();
        if (frameIds.Length == 0)
            throw new StructuralAnalysisException("M79_NO_FRAME_MEMBERS: No Frame2D members exist in the model.");

        Frame2DExactExtremaFinderM78 finder = new();
        var entries = solutions.Select(solution =>
        {
            var byMember = frameIds.ToDictionary(
                id => id,
                id => finder.Find(model, solution.Result, id),
                StringComparer.OrdinalIgnoreCase);
            return (solution.Scenario, ByMember: byMember);
        }).ToArray();

        List<FrameMemberForceEnvelopeM79> members = new();
        foreach (string memberId in frameIds)
        {
            List<GoverningForceExtremeM79> minima = new();
            List<GoverningForceExtremeM79> maxima = new();
            List<GoverningForceExtremeM79> magnitudes = new();

            foreach (InternalForceQuantity quantity in Quantities)
            {
                GoverningForceExtremeM79? min = null;
                GoverningForceExtremeM79? max = null;
                GoverningForceExtremeM79? abs = null;
                foreach (var entry in entries)
                {
                    ExactMemberInternalForceExtremaM78 candidate = entry.ByMember[memberId];
                    GoverningForceExtremeM79 cMin = Project(entry.Scenario, candidate.Minimum(quantity));
                    GoverningForceExtremeM79 cMax = Project(entry.Scenario, candidate.Maximum(quantity));
                    GoverningForceExtremeM79 cAbs = Project(entry.Scenario, candidate.MaximumAbsolute(quantity));

                    // Strict comparisons intentionally retain the first input scenario on exact ties.
                    if (min is null || cMin.Value < min.Value) min = cMin;
                    if (max is null || cMax.Value > max.Value) max = cMax;
                    if (abs is null || Math.Abs(cAbs.Value) > Math.Abs(abs.Value)) abs = cAbs;
                }
                minima.Add(min!);
                maxima.Add(max!);
                magnitudes.Add(abs!);
            }

            members.Add(new FrameMemberForceEnvelopeM79(memberId,
                minima.AsReadOnly(), maxima.AsReadOnly(), magnitudes.AsReadOnly()));
        }

        return new StructuralStaticForceEnvelopeM79(
            Array.AsReadOnly(solutions.Select(s => s.Scenario).ToArray()), members.AsReadOnly());
    }

    private static GoverningForceExtremeM79 Project(
        StaticForceEnvelopeScenarioM79 scenario, ExactInternalForceExtremeM78 extreme) =>
        new(extreme.MemberId, scenario.Kind, scenario.Id,
            extreme.Quantity, extreme.Value, extreme.Distance, extreme.Position, extreme.Side);
}
