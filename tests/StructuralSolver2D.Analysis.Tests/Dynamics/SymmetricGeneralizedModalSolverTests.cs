using StructuralSolver2D.Analysis.Dynamics.Modal;
using StructuralSolver2D.Analysis.Dynamics.Reduction;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Dynamics;

public sealed class SymmetricGeneralizedModalSolverTests
{
    [Fact]
    public void ShearTwoStory_RecoversTwoAnalyticalEigenvaluesAndOrthogonality()
    {
        const double k = 5600, m = 3.25;
        var pair = Ready(new double[,] { { 2*k, -k }, { -k, k } },
            new double[,] { { m, 0 }, { 0, m } });
        var result = SymmetricGeneralizedModalSolver.Solve(pair, 2);
        Assert.Equal(2, result.AvailableModeCount);
        Assert.Equal(2, result.Modes.Count);
        Close((3 - Math.Sqrt(5)) / 2 * k / m, result.Modes[0].EigenvalueRadiansSquaredPerSecondSquared, 2e-10);
        Close((3 + Math.Sqrt(5)) / 2 * k / m, result.Modes[1].EigenvalueRadiansSquaredPerSecondSquared, 2e-10);
        Assert.True(result.MaxMassOrthogonalityError < 1e-10);
        Assert.True(result.MaxStiffnessOrthogonalityError < 1e-10);
        foreach (var mode in result.Modes)
        {
            Close(mode.AngularFrequencyRadiansPerSecond / (2 * Math.PI), mode.FrequencyHertz);
            Close(1 / mode.FrequencyHertz, mode.PeriodSeconds);
            Close(mode.EigenvalueRadiansSquaredPerSecondSquared, mode.RayleighQuotient, 1e-10);
            Assert.True(mode.RelativeResidual < 1e-10);
            Assert.True(mode.GlobalMassNormalizedShape.Max(x => Math.Abs(x)) > 0);
        }
    }

    [Fact]
    public void ConsistentOffDiagonalMass_TransformsAndMassNormalizesModes()
    {
        var pair = Ready(new double[,] { { 32, -4 }, { -4, 21 } },
            new double[,] { { 4, 0.85 }, { 0.85, 2.5 } });
        var result = SymmetricGeneralizedModalSolver.Solve(pair, 2);
        Assert.True(result.Modes[0].FrequencyHertz < result.Modes[1].FrequencyHertz);
        foreach (var mode in result.Modes)
        {
            var q = mode.ReducedMassNormalizedShape;
            Close(1, Dot(q, pair.Mass, q), 1e-10);
            Close(mode.EigenvalueRadiansSquaredPerSecondSquared,
                Dot(q, pair.Stiffness, q), 1e-10);
            Assert.True(mode.RelativeResidual < 1e-10);
        }
        Close(0, Dot(result.Modes[0].ReducedMassNormalizedShape,
            pair.Mass, result.Modes[1].ReducedMassNormalizedShape), 1e-10);
    }

    [Fact]
    public void RepeatedEigenvalue_IsAllowedWithoutAssumingUniqueModeShape()
    {
        var mass = new double[,] { { 4, 1 }, { 1, 3 } };
        var k = new double[,] { { 48, 12 }, { 12, 36 } };
        var spectrum = SymmetricGeneralizedModalSolver.Solve(Ready(k, mass), 2);
        foreach (var mode in spectrum.Modes)
        {
            Close(12, mode.EigenvalueRadiansSquaredPerSecondSquared, 1e-10);
            Assert.True(mode.RelativeResidual < 1e-10);
        }
        Assert.True(spectrum.MaxMassOrthogonalityError < 1e-10);
    }

    [Fact]
    public void ScaleConsistentStiffnessAndMass_ProduceExpectedFrequencyScaling()
    {
        var k = new double[,] { { 8, -2 }, { -2, 5 } };
        var m = new double[,] { { 2, 0.2 }, { 0.2, 3 } };
        var first = SymmetricGeneralizedModalSolver.Solve(Ready(k, m), 2);
        var stiffer = SymmetricGeneralizedModalSolver.Solve(Ready(Scale(k, 1e7), m), 2);
        var heavier = SymmetricGeneralizedModalSolver.Solve(Ready(k, Scale(m, 1e7)), 2);
        for (int i = 0; i < 2; i++)
        {
            Close(first.Modes[i].FrequencyHertz * Math.Sqrt(1e7), stiffer.Modes[i].FrequencyHertz, 1e-9);
            Close(first.Modes[i].FrequencyHertz / Math.Sqrt(1e7), heavier.Modes[i].FrequencyHertz, 1e-9);
        }
    }

    [Fact]
    public void NonAxisGlobalBasis_IsUsedWhenReconstructingShapes()
    {
        const double theta = Math.PI / 4;
        var z = new double[,] { { Math.Cos(theta) }, { Math.Sin(theta) }, { 0 } };
        var pair = Ready(new double[,] { { 18 } }, new double[,] { { 2 } }, z);
        var result = SymmetricGeneralizedModalSolver.Solve(pair);
        Assert.Single(result.Modes);
        var shape = result.Modes[0].GlobalMassNormalizedShape;
        Close(shape[0], shape[1]);
        Close(0, shape[2]);
        Close(3, result.Modes[0].AngularFrequencyRadiansPerSecond);
        Assert.True(shape[0] > 0);
    }

    [Fact]
    public void NumberOfReturnedModes_IsCappedByNumberOfPhysicalCoordinates()
    {
        var result = SymmetricGeneralizedModalSolver.Solve(Ready(
            new double[,] { { 5, 0 }, { 0, 12 } },
            new double[,] { { 1, 0 }, { 0, 2 } }), 99);
        Assert.Equal(2, result.AvailableModeCount);
        Assert.Equal(2, result.Modes.Count);
        Assert.Equal(new[] { 1, 2 }, result.Modes.Select(x => x.ModeNumber));
    }

    [Fact]
    public void RayleighEstimate_IsInvariantToAmplitudeAndNotBelowLowestEigenvalue()
    {
        var pair = Ready(new double[,] { { 4, -1 }, { -1, 3 } },
            new double[,] { { 2, 0 }, { 0, 2 } });
        var result = SymmetricGeneralizedModalSolver.Solve(pair, 2);
        var trial = new[] { 0.3, 0.8 };
        double quotient = SymmetricGeneralizedModalSolver.RayleighQuotient(pair.Stiffness, pair.Mass, trial);
        Close(quotient, SymmetricGeneralizedModalSolver.RayleighQuotient(pair.Stiffness,
            pair.Mass, trial.Select(x => 10 * x).ToArray()));
        Assert.True(quotient >= result.Modes[0].EigenvalueRadiansSquaredPerSecondSquared - 1e-12);
        Assert.True(quotient <= result.Modes[1].EigenvalueRadiansSquaredPerSecondSquared + 1e-12);
    }

    [Fact]
    public void ActualCantileverAssembly_ProducesThreePhysicalModesWithSmallResiduals()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC"))
            .AddSupport(StructuralSupport.Fixed("SA", "A"));
        var reduction = DynamicBoundaryConditionReducer.AssembleAndReduce(model, new StructuralDynamicsDefinition());
        Assert.Equal(DynamicReductionStatus.Ready, reduction.Status);
        var result = SymmetricGeneralizedModalSolver.Solve(reduction, 3);
        Assert.Equal(3, result.Modes.Count);
        Assert.Equal(6, result.Modes[0].GlobalMassNormalizedShape.Length);
        Assert.All(result.Modes, mode =>
        {
            Assert.True(mode.FrequencyHertz > 0);
            Assert.True(mode.RelativeResidual < 1e-8);
            Close(0, mode.GlobalMassNormalizedShape[0]);
            Close(0, mode.GlobalMassNormalizedShape[1]);
            Close(0, mode.GlobalMassNormalizedShape[2]);
        });
    }

    [Fact]
    public void ActualInclinedSupport_RestoredShapeSatisfiesConstraint()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 2))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC"))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(new StructuralSupport("RB", "B", false, true, false, OrientationDegrees: 25));
        var reduced = DynamicBoundaryConditionReducer.AssembleAndReduce(model, new StructuralDynamicsDefinition());
        Assert.Equal(DynamicReductionStatus.Ready, reduced.Status);
        var result = SymmetricGeneralizedModalSolver.Solve(reduced, 2);
        foreach (var mode in result.Modes)
        {
            var g = mode.GlobalMassNormalizedShape;
            Close(0, -Math.Sin(25 * Math.PI / 180) * g[3] + Math.Cos(25 * Math.PI / 180) * g[4], 1e-9);
        }
    }

    [Fact]
    public void MediumDenseSystem_ConvergesAndMaintainsEigenpairQuality()
    {
        const int n = 24;
        var k = new double[n, n];
        var m = new double[n, n];
        for (int i = 0; i < n; i++)
        {
            m[i, i] = 1 + (i % 4) * 0.25;
            k[i, i] = 200 + 0.5 * i;
            if (i > 0)
                k[i, i - 1] = k[i - 1, i] = -15;
        }
        var spectrum = SymmetricGeneralizedModalSolver.Solve(Ready(k, m), 5);
        Assert.Equal(24, spectrum.AvailableModeCount);
        Assert.Equal(5, spectrum.Modes.Count);
        for (int i = 0; i < spectrum.Modes.Count; i++)
        {
            Assert.True(spectrum.Modes[i].RelativeResidual < 1e-8);
            if (i > 0)
                Assert.True(spectrum.Modes[i].FrequencyHertz > spectrum.Modes[i - 1].FrequencyHertz);
        }
        Assert.True(spectrum.MaxMassOrthogonalityError < 1e-8);
        Assert.True(spectrum.MaxStiffnessOrthogonalityError < 1e-8);
    }

    [Fact]
    public void UnsupportedMasslessFrameAndFreeFree_AreRejectedRatherThanReportingFrequencies()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC"));
        var free = DynamicBoundaryConditionReducer.AssembleAndReduce(model, new StructuralDynamicsDefinition());
        var exception = Assert.Throws<InvalidOperationException>(() => SymmetricGeneralizedModalSolver.Solve(free));
        Assert.Contains("M54_FREE_FREE_DEFERRED", exception.Message);
    }

    [Fact]
    public void InvalidInputEigenpair_ReportsFailureCodesWithoutProducingValues()
    {
        var k = new double[,] { { 2, 0 }, { 0, 3 } };
        var mass = new double[,] { { 1, 0 }, { 0, 2 } };
        Assert.Throws<ArgumentOutOfRangeException>(() => SymmetricGeneralizedModalSolver.Solve(Ready(k, mass), 0));
        var singular = Ready(k, new double[,] { { 1, 0 }, { 0, 0 } });
        Assert.Contains("M55_MASS_NOT_POSITIVE", Assert.Throws<InvalidOperationException>(
            () => SymmetricGeneralizedModalSolver.Solve(singular)).Message);
        var negative = Ready(new double[,] { { -1, 0 }, { 0, 3 } }, mass);
        Assert.Contains("M55_NONPOSITIVE_MODE", Assert.Throws<InvalidOperationException>(
            () => SymmetricGeneralizedModalSolver.Solve(negative)).Message);
        var nonFinite = Ready(new double[,] { { double.NaN, 0 }, { 0, 3 } }, mass);
        Assert.Contains("M55_NONFINITE", Assert.Throws<InvalidOperationException>(
            () => SymmetricGeneralizedModalSolver.Solve(nonFinite)).Message);
        var nonsymmetric = Ready(new double[,] { { 2, 2 }, { 0, 3 } }, mass);
        Assert.Contains("M55_NONSYMMETRIC", Assert.Throws<InvalidOperationException>(
            () => SymmetricGeneralizedModalSolver.Solve(nonsymmetric)).Message);
        Assert.Throws<ArgumentException>(() => SymmetricGeneralizedModalSolver.RayleighQuotient(k, mass, new[] { 1.0 }));
    }

    private static StructuralModel Base() => new StructuralModel()
        .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 77))
        .AddSection(new StructuralSection("SEC", "Section", 0.02, 0.0001));

    private static DynamicReductionResult Ready(double[,] k, double[,] m, double[,]? z = null)
    {
        int n = k.GetLength(0);
        z ??= new double[n, n];
        if (z.GetLength(0) == n && z.GetLength(1) == n)
            for (int i = 0; i < n; i++) z[i, i] = 1;
        // Global vectors must have 3 entries per node; synthetic pairs use a
        // padded three-component global basis. Never synthesize fake masses.
        if (z.GetLength(0) % 3 != 0)
        {
            var padded = new double[3 * ((n + 2) / 3), n];
            for (int i = 0; i < n; i++) padded[i, i] = 1;
            z = padded;
        }
        return new DynamicReductionResult(DynamicReductionStatus.Ready, "M54_READY", "Ready",
            k, m, z, 0, 0, Array.Empty<int>(), 0, 0, 0);
    }

    private static double[,] Scale(double[,] matrix, double scale)
    {
        int n = matrix.GetLength(0);
        var result = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++) result[i, j] = matrix[i, j] * scale;
        return result;
    }

    private static double Dot(double[] left, double[,] matrix, double[] right)
    {
        double result = 0;
        for (int i = 0; i < left.Length; i++)
            for (int j = 0; j < right.Length; j++) result += left[i] * matrix[i,j] * right[j];
        return result;
    }

    private static void Close(double expected, double actual, double tolerance = 1e-9)
    {
        Assert.True(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance * Math.Max(1, Math.Abs(expected)),
            $"Expected {expected:R}; actual {actual:R}; relative tolerance {tolerance:R}");
    }
}
