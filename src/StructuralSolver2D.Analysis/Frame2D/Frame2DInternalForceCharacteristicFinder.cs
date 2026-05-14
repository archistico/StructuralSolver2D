using StructuralSolver2D.Analysis.Results;

namespace StructuralSolver2D.Analysis.Frame2D;

/// <summary>
/// Finds characteristic points on sampled Frame2D internal-force diagrams.
/// </summary>
/// <remarks>
/// The finder works on already sampled diagrams. It therefore identifies characteristic points
/// from available samples and from linear interpolation between adjacent samples. Exact analytical
/// locations may require dedicated closed-form post-processing for each load configuration.
/// </remarks>
public sealed class Frame2DInternalForceCharacteristicFinder
{
    private const double DefaultTolerance = 1e-9;

    /// <summary>
    /// Finds characteristic points for one internal-force diagram.
    /// </summary>
    /// <param name="diagram">Sampled internal-force diagram.</param>
    /// <param name="tolerance">Numerical tolerance used for zero detection and duplicate removal.</param>
    /// <returns>Detected characteristic points ordered by distance.</returns>
    public MemberInternalForceCharacteristics Find(
        MemberInternalForceDiagram diagram,
        double tolerance = DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(diagram);

        if (tolerance <= 0 || double.IsNaN(tolerance) || double.IsInfinity(tolerance))
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "Tolerance must be a positive finite value.");
        }

        if (diagram.Samples.Count == 0)
        {
            return new MemberInternalForceCharacteristics(diagram.MemberId, diagram.Length, Array.Empty<InternalForceCharacteristicPoint>());
        }

        List<InternalForceCharacteristicPoint> points = new();

        AddEndPoints(diagram, points);
        AddSampledExtrema(diagram, points);
        AddZeroCrossings(diagram, points, tolerance);
        AddBendingMomentExtremumCandidatesFromZeroShear(diagram, points, tolerance);
        AddDiscontinuityCandidates(diagram, points, tolerance);

        IReadOnlyList<InternalForceCharacteristicPoint> distinctPoints = points
            .OrderBy(point => point.Distance)
            .ThenBy(point => point.Kind)
            .ThenBy(point => point.Quantity)
            .Aggregate(new List<InternalForceCharacteristicPoint>(), (items, point) =>
            {
                bool alreadyPresent = items.Any(existing =>
                    existing.Kind == point.Kind &&
                    existing.Quantity == point.Quantity &&
                    string.Equals(existing.MemberId, point.MemberId, StringComparison.OrdinalIgnoreCase) &&
                    Math.Abs(existing.Distance - point.Distance) <= tolerance * Math.Max(1.0, diagram.Length));

                if (!alreadyPresent)
                {
                    items.Add(point);
                }

                return items;
            });

        return new MemberInternalForceCharacteristics(diagram.MemberId, diagram.Length, distinctPoints);
    }

    /// <summary>
    /// Finds characteristic points for all provided diagrams.
    /// </summary>
    /// <param name="diagrams">Sampled internal-force diagrams.</param>
    /// <param name="tolerance">Numerical tolerance used for zero detection and duplicate removal.</param>
    /// <returns>Characteristic points grouped by member.</returns>
    public IReadOnlyList<MemberInternalForceCharacteristics> FindAll(
        IReadOnlyList<MemberInternalForceDiagram> diagrams,
        double tolerance = DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(diagrams);

        return diagrams.Select(diagram => Find(diagram, tolerance)).ToList();
    }

    private static void AddEndPoints(
        MemberInternalForceDiagram diagram,
        List<InternalForceCharacteristicPoint> points)
    {
        MemberInternalForceSample start = diagram.Samples.First();
        MemberInternalForceSample end = diagram.Samples.Last();

        points.Add(new InternalForceCharacteristicPoint(
            diagram.MemberId,
            InternalForceCharacteristicPointKind.EndPoint,
            InternalForceQuantity.Multiple,
            start.Position,
            start.Distance,
            0.0,
            "member start"));

        if (!ReferenceEquals(start, end))
        {
            points.Add(new InternalForceCharacteristicPoint(
                diagram.MemberId,
                InternalForceCharacteristicPointKind.EndPoint,
                InternalForceQuantity.Multiple,
                end.Position,
                end.Distance,
                0.0,
                "member end"));
        }
    }

    private static void AddSampledExtrema(
        MemberInternalForceDiagram diagram,
        List<InternalForceCharacteristicPoint> points)
    {
        AddExtremaForQuantity(
            diagram,
            points,
            InternalForceQuantity.NormalForce,
            sample => sample.NormalForce,
            "N");

        AddExtremaForQuantity(
            diagram,
            points,
            InternalForceQuantity.ShearForce,
            sample => sample.ShearForce,
            "V");

        AddExtremaForQuantity(
            diagram,
            points,
            InternalForceQuantity.BendingMoment,
            sample => sample.BendingMoment,
            "M");
    }

    private static void AddExtremaForQuantity(
        MemberInternalForceDiagram diagram,
        List<InternalForceCharacteristicPoint> points,
        InternalForceQuantity quantity,
        Func<MemberInternalForceSample, double> valueSelector,
        string symbol)
    {
        MemberInternalForceSample min = diagram.Samples.MinBy(valueSelector)!;
        MemberInternalForceSample max = diagram.Samples.MaxBy(valueSelector)!;
        MemberInternalForceSample maxAbs = diagram.Samples.MaxBy(sample => Math.Abs(valueSelector(sample)))!;

        points.Add(ToPoint(diagram.MemberId, min, InternalForceCharacteristicPointKind.SampledMinimum, quantity, valueSelector(min), $"sampled minimum {symbol}"));
        points.Add(ToPoint(diagram.MemberId, max, InternalForceCharacteristicPointKind.SampledMaximum, quantity, valueSelector(max), $"sampled maximum {symbol}"));
        points.Add(ToPoint(diagram.MemberId, maxAbs, InternalForceCharacteristicPointKind.SampledMaximumAbsolute, quantity, valueSelector(maxAbs), $"sampled maximum absolute {symbol}"));
    }

    private static void AddZeroCrossings(
        MemberInternalForceDiagram diagram,
        List<InternalForceCharacteristicPoint> points,
        double tolerance)
    {
        AddZeroCrossingsForQuantity(diagram, points, InternalForceQuantity.NormalForce, sample => sample.NormalForce, "N", tolerance);
        AddZeroCrossingsForQuantity(diagram, points, InternalForceQuantity.ShearForce, sample => sample.ShearForce, "V", tolerance);
        AddZeroCrossingsForQuantity(diagram, points, InternalForceQuantity.BendingMoment, sample => sample.BendingMoment, "M", tolerance);
    }

    private static void AddZeroCrossingsForQuantity(
        MemberInternalForceDiagram diagram,
        List<InternalForceCharacteristicPoint> points,
        InternalForceQuantity quantity,
        Func<MemberInternalForceSample, double> valueSelector,
        string symbol,
        double tolerance)
    {
        if (diagram.Samples.All(sample => Math.Abs(valueSelector(sample)) <= tolerance))
        {
            MemberInternalForceSample first = diagram.Samples.First();
            points.Add(ToPoint(
                diagram.MemberId,
                first,
                InternalForceCharacteristicPointKind.ZeroCrossing,
                quantity,
                0.0,
                $"zero {symbol} throughout sampled member"));
            return;
        }

        for (int index = 0; index < diagram.Samples.Count; index++)
        {
            MemberInternalForceSample current = diagram.Samples[index];
            double currentValue = valueSelector(current);

            if (Math.Abs(currentValue) <= tolerance)
            {
                points.Add(ToPoint(diagram.MemberId, current, InternalForceCharacteristicPointKind.ZeroCrossing, quantity, 0.0, $"zero {symbol}"));
            }

            if (index == diagram.Samples.Count - 1)
            {
                continue;
            }

            MemberInternalForceSample next = diagram.Samples[index + 1];
            double nextValue = valueSelector(next);

            if (currentValue * nextValue < 0.0)
            {
                double ratio = Math.Abs(currentValue) / (Math.Abs(currentValue) + Math.Abs(nextValue));
                double position = current.Position + ((next.Position - current.Position) * ratio);
                double distance = current.Distance + ((next.Distance - current.Distance) * ratio);

                points.Add(new InternalForceCharacteristicPoint(
                    diagram.MemberId,
                    InternalForceCharacteristicPointKind.ZeroCrossing,
                    quantity,
                    position,
                    distance,
                    0.0,
                    $"interpolated zero {symbol}"));
            }
        }
    }

    private static void AddBendingMomentExtremumCandidatesFromZeroShear(
        MemberInternalForceDiagram diagram,
        List<InternalForceCharacteristicPoint> points,
        double tolerance)
    {
        for (int index = 0; index < diagram.Samples.Count - 1; index++)
        {
            MemberInternalForceSample current = diagram.Samples[index];
            MemberInternalForceSample next = diagram.Samples[index + 1];

            if (Math.Abs(current.ShearForce) <= tolerance)
            {
                points.Add(ToPoint(
                    diagram.MemberId,
                    current,
                    InternalForceCharacteristicPointKind.BendingMomentExtremumCandidate,
                    InternalForceQuantity.BendingMoment,
                    current.BendingMoment,
                    "bending moment extremum candidate from zero shear"));
            }
            else if (current.ShearForce * next.ShearForce < 0.0)
            {
                double ratio = Math.Abs(current.ShearForce) / (Math.Abs(current.ShearForce) + Math.Abs(next.ShearForce));
                double position = current.Position + ((next.Position - current.Position) * ratio);
                double distance = current.Distance + ((next.Distance - current.Distance) * ratio);
                double moment = current.BendingMoment + ((next.BendingMoment - current.BendingMoment) * ratio);

                points.Add(new InternalForceCharacteristicPoint(
                    diagram.MemberId,
                    InternalForceCharacteristicPointKind.BendingMomentExtremumCandidate,
                    InternalForceQuantity.BendingMoment,
                    position,
                    distance,
                    moment,
                    "interpolated bending moment extremum candidate from zero shear"));
            }
        }

        MemberInternalForceSample last = diagram.Samples.Last();
        if (Math.Abs(last.ShearForce) <= tolerance)
        {
            points.Add(ToPoint(
                diagram.MemberId,
                last,
                InternalForceCharacteristicPointKind.BendingMomentExtremumCandidate,
                InternalForceQuantity.BendingMoment,
                last.BendingMoment,
                "bending moment extremum candidate from zero shear"));
        }
    }

    private static void AddDiscontinuityCandidates(
        MemberInternalForceDiagram diagram,
        List<InternalForceCharacteristicPoint> points,
        double tolerance)
    {
        if (diagram.Samples.Count < 3)
        {
            return;
        }

        List<double> jumps = new();
        for (int index = 0; index < diagram.Samples.Count - 1; index++)
        {
            double jump = Math.Abs(diagram.Samples[index + 1].ShearForce - diagram.Samples[index].ShearForce);
            if (jump > tolerance)
            {
                jumps.Add(jump);
            }
        }

        if (jumps.Count == 0)
        {
            return;
        }

        jumps.Sort();
        double maxShearJump = jumps[^1];
        double medianJump = jumps[jumps.Count / 2];

        // Smooth distributed loads may create regular sample-to-sample shear changes.
        // Treat a jump as a discontinuity candidate only when the largest jump is clearly
        // dominant compared with the regular sampled variation.
        if (jumps.Count > 1 && maxShearJump <= medianJump * 3.0)
        {
            return;
        }

        double threshold = Math.Max(tolerance * 100.0, maxShearJump * 0.75);

        for (int index = 0; index < diagram.Samples.Count - 1; index++)
        {
            MemberInternalForceSample current = diagram.Samples[index];
            MemberInternalForceSample next = diagram.Samples[index + 1];
            double jump = next.ShearForce - current.ShearForce;

            if (Math.Abs(jump) >= threshold)
            {
                points.Add(new InternalForceCharacteristicPoint(
                    diagram.MemberId,
                    InternalForceCharacteristicPointKind.DiscontinuityCandidate,
                    InternalForceQuantity.ShearForce,
                    next.Position,
                    next.Distance,
                    next.ShearForce,
                    "candidate shear discontinuity between adjacent samples"));
            }
        }
    }

    private static InternalForceCharacteristicPoint ToPoint(
        string memberId,
        MemberInternalForceSample sample,
        InternalForceCharacteristicPointKind kind,
        InternalForceQuantity quantity,
        double value,
        string description) =>
        new(
            memberId,
            kind,
            quantity,
            sample.Position,
            sample.Distance,
            value,
            description);
}
