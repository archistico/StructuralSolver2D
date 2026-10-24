using StructuralSolver2D.Analysis.Dynamics.Assembly;
using StructuralSolver2D.Analysis.Dynamics.Modal;
using StructuralSolver2D.Analysis.Dynamics.Reduction;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Dynamics;

/// <summary>
/// M56: comparison with continuous-structure analytical solutions, not merely
/// with another matrix solver. These tests exercise actual M53 assembly, M54
/// restraints and M55 eigensolution together. References: benchmarks/modal/.
/// No public static API behavior is changed.
/// </summary>
public sealed class ModalBenchmarkM56Tests
{
    private const double Length = 4.0;
    private const double Young = 210_000_000.0; // kN/m^2
    private const double Area = 0.02;           // m^2
    private const double Inertia = 0.0001;      // m^4
    private const double Weight = 77.0;        // kN/m^3
    private static readonly double[] CantileverRoots = { 1.875104068711961, 4.694091132974175, 7.854757438237613 };

    [Fact]
    public void ShearTwoStorey_IndependentEigenvaluesAndModeRatios()
    {
        const double k = 5600, m = 3.25;
        var spectrum = SymmetricGeneralizedModalSolver.Solve(Ready(
            new double[,] { { 2 * k, -k }, { -k, k } },
            new double[,] { { m, 0 }, { 0, m } }), 2);
        var exactLambda = new[] { (3 - Math.Sqrt(5)) * k / (2 * m), (3 + Math.Sqrt(5)) * k / (2 * m) };
        for (int i = 0; i < 2; i++)
        {
            var mode = spectrum.Modes[i];
            Near(exactLambda[i], mode.EigenvalueRadiansSquaredPerSecondSquared, 5e-10);
            // (2k - lambda*m)*v1 - k*v2 = 0; sign of a mode is arbitrary.
            double exactRatio = 2 - exactLambda[i] * m / k;
            Near(exactRatio, mode.ReducedMassNormalizedShape[1] / mode.ReducedMassNormalizedShape[0], 5e-10);
            Assert.True(mode.RelativeResidual < 1e-9);
        }
        Assert.True(spectrum.MaxMassOrthogonalityError < 1e-9);
        Assert.True(spectrum.MaxStiffnessOrthogonalityError < 1e-9);
    }

    [Theory]
    [InlineData(1, 0.12)]
    [InlineData(2, 0.005)]
    [InlineData(4, 0.0003)]
    [InlineData(8, 0.00002)]
    [InlineData(16, 0.000002)]
    public void PinnedBeam_FirstFlexuralFrequency_ConvergesToContinuousTheory(int elements, double limit)
    {
        var spectrum = Solve(BuildBeam(elements, EndCondition.Pinned), 1);
        RelativeErrorBelow(PinnedFrequency(1), spectrum.Modes[0].FrequencyHertz, limit);
        Quality(spectrum);
    }

    [Theory]
    [InlineData(1, 0.005)]
    [InlineData(2, 0.0005)]
    [InlineData(4, 0.00004)]
    [InlineData(8, 0.000003)]
    [InlineData(16, 0.0000003)]
    public void Cantilever_FirstFlexuralFrequency_ConvergesToContinuousTheory(int elements, double limit)
    {
        var spectrum = Solve(BuildBeam(elements, EndCondition.Fixed), 1);
        RelativeErrorBelow(CantileverFrequency(1), spectrum.Modes[0].FrequencyHertz, limit);
        Quality(spectrum);
    }

    [Theory]
    [InlineData(4, 0.0003, 0.005, 0.020)]
    [InlineData(8, 0.00002, 0.0003, 0.0015)]
    [InlineData(16, 0.000002, 0.00002, 0.00012)]
    public void PinnedBeam_LowThreeFlexuralModes_ConvergeAndShapesAgreeWithSines(
        int elements, double limit1, double limit2, double limit3)
    {
        // Axial modes can interleave with bending modes. At four elements,
        // higher bending modes can also ALIAS lower ones at nodal Uy samples.
        // Compare Hermite-interpolated deflections inside the elements and
        // reject predominantly axial modes rather than trusting nodal MAC.
        var spectrum = Solve(BuildBeam(elements, EndCondition.Pinned), 6);
        double[] limits = { limit1, limit2, limit3 };
        for (int modeIndex = 0; modeIndex < 3; modeIndex++)
        {
            int modeNumber = modeIndex + 1;
            // This classification uses the full interpolated bending shape,
            // not the ordering of eigenvalues or potentially aliased nodal Uy.
            var selected = spectrum.Modes.Select(mode => new
            {
                Mode = mode,
                Mac = InterpolatedFlexuralMac(mode.GlobalMassNormalizedShape, elements,
                    x => Math.Sin(modeNumber * Math.PI * x / Length))
            }).OrderByDescending(candidate => candidate.Mac).First();
            RelativeErrorBelow(PinnedFrequency(modeNumber), selected.Mode.FrequencyHertz, limits[modeIndex]);
            Assert.True(selected.Mac > 0.995, $"Pinned flexural mode {modeNumber}, mesh {elements}, MAC={selected.Mac:R}");
        }
        Quality(spectrum);
    }

    [Fact]
    public void FourElementPinnedFrame_AxialModeInterleavesWithThirdFlexuralMode()
    {
        const int elements = 4;
        var spectrum = Solve(BuildBeam(elements, EndCondition.Pinned), 5);
        double axialFraction = 0;
        double bendingFraction = 0;
        for (int i = 0; i <= elements; i++)
        {
            axialFraction += Math.Pow(spectrum.Modes[2].GlobalMassNormalizedShape[3 * i], 2);
            bendingFraction += Math.Pow(spectrum.Modes[2].GlobalMassNormalizedShape[3 * i + 1], 2);
        }
        Assert.True(axialFraction > 1_000 * bendingFraction,
            "The third global eigenmode at N=4 is longitudinal, not the third flexural mode.");
        double thirdBendingMac = DisplacementMac(spectrum.Modes[3].GlobalMassNormalizedShape, elements, 1,
            x => Math.Sin(3 * Math.PI * x / Length));
        Assert.True(thirdBendingMac > 0.99);
    }

    [Fact]
    public void FourElementPinnedFrame_InterpolatedShapesRejectHighModeNodalAliasing()
    {
        // Modes 4 and 6 (1-based, including axial modes) have almost identical
        // nodal transverse MAC against sin(3*pi*x/L), although the latter has
        // extra oscillations INSIDE the elements. This once led to selecting
        // 1013 Hz instead of the third bending frequency near 329 Hz.
        const int elements = 4;
        var spectrum = Solve(BuildBeam(elements, EndCondition.Pinned), 6);
        var target = new Func<double, double>(x => Math.Sin(3 * Math.PI * x / Length));
        var trueBending = spectrum.Modes[3].GlobalMassNormalizedShape;
        var aliasedHigherBending = spectrum.Modes[5].GlobalMassNormalizedShape;
        var axial = spectrum.Modes[2].GlobalMassNormalizedShape;

        // Reproduce the old ambiguity rather than only checking the new answer.
        Assert.True(DisplacementMac(trueBending, elements, 1, target) > 0.999);
        Assert.True(DisplacementMac(aliasedHigherBending, elements, 1, target) > 0.999);

        Assert.True(InterpolatedFlexuralMac(trueBending, elements, target) > 0.995);
        Assert.True(InterpolatedFlexuralMac(aliasedHigherBending, elements, target) < 0.5);
        Assert.Equal(0, InterpolatedFlexuralMac(axial, elements, target));
        RelativeErrorBelow(PinnedFrequency(3), spectrum.Modes[3].FrequencyHertz, 0.02);
        Quality(spectrum);
    }

    [Theory]
    [InlineData(4, 0.00004, 0.0013, 0.008)]
    [InlineData(8, 0.000003, 0.0001, 0.0007)]
    [InlineData(16, 0.0000003, 0.00001, 0.00005)]
    public void Cantilever_LowThreeFlexuralModes_AgreeWithExactRootsAndShapes(
        int elements, double limit1, double limit2, double limit3)
    {
        var spectrum = Solve(BuildBeam(elements, EndCondition.Fixed), 3);
        double[] limits = { limit1, limit2, limit3 };
        for (int modeIndex = 0; modeIndex < 3; modeIndex++)
        {
            int modeNumber = modeIndex + 1;
            RelativeErrorBelow(CantileverFrequency(modeNumber), spectrum.Modes[modeIndex].FrequencyHertz, limits[modeIndex]);
            double beta = CantileverRoots[modeIndex];
            double eta = (Math.Cosh(beta) + Math.Cos(beta)) / (Math.Sinh(beta) + Math.Sin(beta));
            double mac = DisplacementMac(spectrum.Modes[modeIndex].GlobalMassNormalizedShape, elements, 1,
                x => Math.Cosh(beta * x / Length) - Math.Cos(beta * x / Length) -
                     eta * (Math.Sinh(beta * x / Length) - Math.Sin(beta * x / Length)));
            Assert.True(mac > 0.995, $"Cantilever mode {modeNumber}, mesh {elements}, MAC={mac:R}");
        }
        Quality(spectrum);
    }

    [Theory]
    [InlineData(2, 0.11)]
    [InlineData(4, 0.027)]
    [InlineData(8, 0.0066)]
    [InlineData(16, 0.0017)]
    public void FixedFixedAxialTruss_FirstLongitudinalMode_Converges(int elements, double tolerance)
    {
        var spectrum = Solve(BuildAxialBar(elements), 1);
        RelativeErrorBelow(AxialFrequency(1), spectrum.Modes[0].FrequencyHertz, tolerance);
        Assert.True(DisplacementMac(spectrum.Modes[0].GlobalMassNormalizedShape, elements, 0,
            x => Math.Sin(Math.PI * x / Length)) > 0.99);
        Quality(spectrum);
    }

    [Fact]
    public void FixedFixedAxialTruss_ThreeModes_MatchTheoryOnRefinedMesh()
    {
        const int elements = 16;
        var spectrum = Solve(BuildAxialBar(elements), 3);
        double[] limits = { 0.0017, 0.0066, 0.016 };
        for (int index = 0; index < 3; index++)
        {
            int modeNumber = index + 1;
            RelativeErrorBelow(AxialFrequency(modeNumber), spectrum.Modes[index].FrequencyHertz, limits[index]);
            Assert.True(DisplacementMac(spectrum.Modes[index].GlobalMassNormalizedShape, elements, 0,
                x => Math.Sin(modeNumber * Math.PI * x / Length)) > 0.99);
        }
        Quality(spectrum);
    }

    [Fact]
    public void MeshRefinement_DecreasesFirstModeErrorForAllThreeReferenceFamilies()
    {
        foreach (var family in new[] { EndCondition.Pinned, EndCondition.Fixed, EndCondition.Axial })
        {
            double previous = double.PositiveInfinity;
            foreach (int count in family == EndCondition.Axial ? new[] { 2, 4, 8, 16 } : new[] { 1, 2, 4, 8, 16 })
            {
                StructuralModel model = family == EndCondition.Axial ? BuildAxialBar(count) : BuildBeam(count, family);
                double exact = family switch
                {
                    EndCondition.Pinned => PinnedFrequency(1),
                    EndCondition.Fixed => CantileverFrequency(1),
                    _ => AxialFrequency(1)
                };
                double frequency = Solve(model, 1).Modes[0].FrequencyHertz;
                double relativeError = Math.Abs(frequency / exact - 1);
                Assert.True(relativeError < previous, $"{family} N={count}: {relativeError:R} did not improve over {previous:R}");
                previous = relativeError;
            }
        }
    }

    [Fact]
    public void RigidRotationTranslationAndReversedEndpoints_PreserveAllLowFrequencies()
    {
        const int segments = 8;
        var baseline = Solve(BuildBeam(segments, EndCondition.Pinned), 3);
        var rotated = Solve(BuildBeam(segments, EndCondition.Pinned, degrees: 33, offsetX: 13, offsetY: -4), 3);
        var reversed = Solve(BuildBeam(segments, EndCondition.Pinned, reverseAlternate: true), 3);
        for (int i = 0; i < 3; i++)
        {
            Near(baseline.Modes[i].FrequencyHertz, rotated.Modes[i].FrequencyHertz, 2e-8);
            Near(baseline.Modes[i].FrequencyHertz, reversed.Modes[i].FrequencyHertz, 2e-8);
        }
        Quality(rotated);
        Quality(reversed);
    }

    [Fact]
    public void SectionStiffnessAndDensityScaling_FollowSquareRootFrequencyLaw()
    {
        var baseline = Solve(BuildBeam(8, EndCondition.Fixed), 3);
        var stiffer = Solve(BuildBeam(8, EndCondition.Fixed, youngFactor: 9), 3);
        var heavier = Solve(BuildBeam(8, EndCondition.Fixed, weightFactor: 4), 3);
        for (int i = 0; i < 3; i++)
        {
            Near(3 * baseline.Modes[i].FrequencyHertz, stiffer.Modes[i].FrequencyHertz, 2e-8);
            Near(0.5 * baseline.Modes[i].FrequencyHertz, heavier.Modes[i].FrequencyHertz, 2e-8);
        }
    }

    [Fact]
    public void BeamLengthScaling_FirstBendingFrequencyFollowsInverseSquareLength()
    {
        var normal = Solve(BuildBeam(8, EndCondition.Fixed), 1);
        var doubleLength = Solve(BuildBeam(8, EndCondition.Fixed, length: 2 * Length), 1);
        Near(normal.Modes[0].FrequencyHertz / 4, doubleLength.Modes[0].FrequencyHertz, 2e-8);
    }

    [Fact]
    public void UnsupportedCases_FailExplicitlyWithoutInventingModes()
    {
        var missingMass = BuildBeam(2, EndCondition.Fixed);
        missingMass.Materials[0] = missingMass.Materials[0] with { UnitWeight = null };
        Assert.Contains("DYNAMIC_MISSING_UNIT_WEIGHT", Assert.Throws<InvalidOperationException>(() =>
            Solve(missingMass, 1)).Message);

        var freeFree = BuildBeam(2, EndCondition.Fixed);
        freeFree.Supports.Clear();
        Assert.Contains("M54_FREE_FREE_DEFERRED", Assert.Throws<InvalidOperationException>(() =>
            Solve(freeFree, 1)).Message);

        var released = BuildBeam(2, EndCondition.Pinned);
        released.Members[0] = released.Members[0] with { ReleaseStartMoment = true };
        Assert.Contains("M53_RELEASE_UNSUPPORTED", Assert.Throws<NotSupportedException>(() =>
            Solve(released, 1)).Message);

        var invalidSection = BuildBeam(2, EndCondition.Fixed);
        invalidSection.Sections[0] = invalidSection.Sections[0] with { MomentOfInertia = 0 };
        Assert.Throws<InvalidOperationException>(() => Solve(invalidSection, 1));

        // The educational diagonal mass approximation leaves frame rotations
        // massless but stiffness-coupled: never produce plausible frequencies.
        var massless = DynamicBoundaryConditionReducer.AssembleAndReduce(
            BuildBeam(2, EndCondition.Fixed), new StructuralDynamicsDefinition(),
            ElementMassFormulation.EducationalLumped);
        Assert.Equal(DynamicReductionStatus.MasslessCoupledDofs, massless.Status);
        Assert.Contains("M54_MASSLESS_STIFFNESS_COUPLING", Assert.Throws<InvalidOperationException>(() =>
            SymmetricGeneralizedModalSolver.Solve(massless, 1)).Message);
    }

    private enum EndCondition { Pinned, Fixed, Axial }

    private static StructuralModel BuildBeam(int elements, EndCondition ends, double length = Length,
        double degrees = 0, double offsetX = 0, double offsetY = 0,
        bool reverseAlternate = false, double youngFactor = 1, double weightFactor = 1)
    {
        var result = new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "M56 reference", Young * youngFactor, Weight * weightFactor))
            .AddSection(new StructuralSection("SEC", "M56 reference", Area, Inertia));
        double angle = degrees * Math.PI / 180;
        for (int i = 0; i <= elements; i++)
        {
            double t = length * i / elements;
            result.AddNode(new StructuralNode($"N{i}", offsetX + t * Math.Cos(angle), offsetY + t * Math.Sin(angle)));
        }
        for (int i = 0; i < elements; i++)
        {
            bool reverse = reverseAlternate && i % 2 == 0;
            result.AddMember(new StructuralMember($"E{i}", $"N{(reverse ? i + 1 : i)}",
                $"N{(reverse ? i : i + 1)}", "MAT", "SEC"));
        }
        if (ends == EndCondition.Pinned)
        {
            result.AddSupport(StructuralSupport.Hinge("LEFT", "N0"));
            result.AddSupport(StructuralSupport.SimpleSupport("RIGHT", $"N{elements}", orientationDegrees: degrees));
        }
        else if (ends == EndCondition.Fixed) result.AddSupport(StructuralSupport.Fixed("LEFT", "N0"));
        return result;
    }

    private static StructuralModel BuildAxialBar(int elements)
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "M56 axial", Young, Weight))
            .AddSection(new StructuralSection("SEC", "M56 axial", Area, Inertia));
        for (int i = 0; i <= elements; i++) model.AddNode(new StructuralNode($"N{i}", Length * i / elements, 0));
        for (int i = 0; i < elements; i++)
            model.AddMember(new StructuralMember($"E{i}", $"N{i}", $"N{i + 1}", "MAT", "SEC", MemberType.Truss2D));
        model.AddSupport(StructuralSupport.Hinge("A", "N0"));
        model.AddSupport(StructuralSupport.Hinge("B", $"N{elements}"));
        for (int i = 1; i < elements; i++)
            model.AddSupport(StructuralSupport.SimpleSupport($"Y{i}", $"N{i}"));
        return model;
    }

    private static ExperimentalModalSpectrum Solve(StructuralModel model, int modes) =>
        SymmetricGeneralizedModalSolver.AssembleAndSolve(model, new StructuralDynamicsDefinition(), modes);

    private static double LineMass => Weight * Area / DynamicMassUnits.StandardGravityMetersPerSecondSquared;
    private static double PinnedFrequency(int mode) => Math.Pow(mode * Math.PI, 2) / (Length * Length) *
        Math.Sqrt(Young * Inertia / LineMass) / (2 * Math.PI);
    private static double CantileverFrequency(int mode) => CantileverRoots[mode - 1] * CantileverRoots[mode - 1] /
        (Length * Length) * Math.Sqrt(Young * Inertia / LineMass) / (2 * Math.PI);
    private static double AxialFrequency(int mode) => mode * Math.PI / Length *
        Math.Sqrt(Young * Area / LineMass) / (2 * Math.PI);

    // Discrete nodal MAC: sign and arbitrary modal amplitude cancel. Do not
    // confuse this with a mass-weighted continuous-function MAC.
    private static double DisplacementMac(double[] global, int elements, int component,
        Func<double, double> reference)
    {
        double cross = 0, squareFe = 0, squareExact = 0;
        for (int i = 0; i <= elements; i++)
        {
            double numerical = global[3 * i + component];
            double theoretical = reference(Length * i / elements);
            cross += numerical * theoretical;
            squareFe += numerical * numerical;
            squareExact += theoretical * theoretical;
        }
        return squareFe > 0 && squareExact > 0 ? (cross * cross) / (squareFe * squareExact) : 0;
    }

    // This check deliberately uses more information than nodal Uy alone.
    // Cubic Euler-Bernoulli Hermite interpolation includes nodal rotations;
    // at N=4, the 3rd and 5th bending shapes alias at the nodes but differ
    // at interior samples. The straight horizontal fixture also has separate
    // axial (Ux) and transverse (Uy) fields: ignore predominantly axial modes.
    // This is a shape-comparison test, not a continuous mass-weighted MAC.
    private static double InterpolatedFlexuralMac(double[] global, int elements,
        Func<double, double> reference)
    {
        if (elements < 1 || global.Length != 3 * (elements + 1))
            throw new ArgumentException("Expected three global degrees of freedom per beam node.");
        double cross = 0, bendSquare = 0, axialSquare = 0, exactSquare = 0;
        double span = Length / elements;
        foreach (int element in Enumerable.Range(0, elements))
        {
            double v0 = global[3 * element + 1];
            double rotation0 = global[3 * element + 2];
            double v1 = global[3 * (element + 1) + 1];
            double rotation1 = global[3 * (element + 1) + 2];
            double u0 = global[3 * element];
            double u1 = global[3 * (element + 1)];
            foreach (double t in new[] { 0.25, 0.5, 0.75 })
            {
                double t2 = t * t, t3 = t2 * t;
                double bending = (1 - 3 * t2 + 2 * t3) * v0
                    + (t - 2 * t2 + t3) * span * rotation0
                    + (3 * t2 - 2 * t3) * v1
                    + (-t2 + t3) * span * rotation1;
                double axial = (1 - t) * u0 + t * u1;
                double exact = reference(span * (element + t));
                cross += bending * exact;
                bendSquare += bending * bending;
                axialSquare += axial * axial;
                exactSquare += exact * exact;
            }
        }
        // A numerically tiny transverse component of a pure axial mode must
        // not produce an artificial MAC of ~1 after normalization.
        if (bendSquare <= 0 || bendSquare <= 1e-8 * axialSquare || exactSquare <= 0)
            return 0;
        return cross * cross / (bendSquare * exactSquare);
    }

    private static void Quality(ExperimentalModalSpectrum spectrum)
    {
        Assert.True(spectrum.MaxMassOrthogonalityError < 1e-7,
            $"Mass orthogonality: {spectrum.MaxMassOrthogonalityError:R}");
        Assert.True(spectrum.MaxStiffnessOrthogonalityError < 1e-7,
            $"Stiffness orthogonality: {spectrum.MaxStiffnessOrthogonalityError:R}");
        foreach (var mode in spectrum.Modes)
            Assert.True(mode.RelativeResidual < 1e-7, $"Mode {mode.ModeNumber}: residual {mode.RelativeResidual:R}");
    }

    private static DynamicReductionResult Ready(double[,] k, double[,] m)
    {
        int n = k.GetLength(0);
        var basis = new double[3 * ((n + 2) / 3), n];
        for (int i = 0; i < n; i++) basis[i, i] = 1;
        return new DynamicReductionResult(DynamicReductionStatus.Ready, "M54_READY", "M56 reference",
            k, m, basis, 0, 0, Array.Empty<int>(), 0, 0, 0);
    }

    private static void RelativeErrorBelow(double expected, double actual, double tolerance)
    {
        Assert.True(double.IsFinite(actual) && Math.Abs(actual / expected - 1) < tolerance,
            $"Reference={expected:R} Hz, observed={actual:R} Hz, error={Math.Abs(actual / expected - 1):P6}, limit={tolerance:P6}");
    }

    private static void Near(double expected, double actual, double relativeTolerance)
    {
        Assert.True(double.IsFinite(actual) && Math.Abs(actual - expected) <= relativeTolerance * Math.Max(1, Math.Abs(expected)),
            $"Reference={expected:R}; observed={actual:R}; tolerance={relativeTolerance:R}");
    }
}
