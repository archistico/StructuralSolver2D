using StructuralSolver2D.Analysis.Dynamics.Elements;

namespace StructuralSolver2D.Analysis.Tests.Dynamics;

public sealed class ElementMassMatrixTests
{
    [Fact]
    public void TrussConsistentMass_ContainsTransverseInertiaButNoRotations()
    {
        const double mu = 2.0, length = 3.0;
        var m = Truss2DMassMatrices.Consistent(mu, length);
        Assert.Equal(4, m.GetLength(0));
        AssertClose(2.0, m[0, 0]);
        AssertClose(1.0, m[0, 2]);
        AssertClose(2.0, m[1, 1]);
        AssertClose(1.0, m[1, 3]);
        AssertClose(0.0, m[0, 1]);
        AssertClose(6.0, Quadratic(m, new[] { 1.0, 0.0, 1.0, 0.0 }));
        AssertClose(6.0, Quadratic(m, new[] { 0.0, 1.0, 0.0, 1.0 }));
        AssertClose(2.0, Quadratic(m, new[] { 1.0, 0.0, -1.0, 0.0 }));
        AssertSymmetricNonNegative(m);
    }

    [Fact]
    public void FrameConsistentMass_MatchesIndependentHermiteQuadrature()
    {
        const double mu = 1.7, length = 4.25;
        var m = Frame2DMassMatrices.ConsistentLocal(mu, length);
        // Independent five-point Gauss-Legendre integration of N_i*N_j.
        double[] points = { 0, -0.5384693101056831, 0.5384693101056831,
            -0.9061798459386640, 0.9061798459386640 };
        double[] weights = { 0.5688888888888889, 0.4786286704993665,
            0.4786286704993665, 0.2369268850561891, 0.2369268850561891 };
        var expected = new double[6, 6];
        for (int q = 0; q < points.Length; q++)
        {
            double x = (points[q] + 1.0) / 2.0;
            double x2 = x*x, x3 = x2*x;
            double[] na = { 1-x, 0, 0, x, 0, 0 };
            double[] nb = { 0, 1-3*x2+2*x3, length*(x-2*x2+x3), 0,
                3*x2-2*x3, length*(-x2+x3) };
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                    expected[i, j] += mu * length * weights[q] / 2.0 *
                        (na[i]*na[j] + nb[i]*nb[j]);
        }
        CompareMatrices(expected, m, 2e-11);
        AssertSymmetricNonNegative(m);
        AssertClose(mu * length, Quadratic(m, new[] { 1.0, 0, 0, 1.0, 0, 0 }));
        AssertClose(mu * length, Quadratic(m, new[] { 0.0, 1, 0, 0.0, 1, 0 }));
        AssertClose(mu * length * length * length / 3.0,
            Quadratic(m, new[] { 0.0, 0, 1, 0.0, length, 1 }));
    }

    [Theory]
    [InlineData(1.0, 0.0)]
    [InlineData(0.0, 1.0)]
    [InlineData(0.6, 0.8)]
    [InlineData(-0.8, 0.6)]
    public void FrameMassTransformation_PreservesKineticEnergy(double c, double s)
    {
        var local = Frame2DMassMatrices.ConsistentLocal(0.9, 2.5);
        var global = Frame2DMassMatrices.TransformToGlobal(local, c, s);
        double[] globalVelocity = { 0.4, -0.5, 0.7, 0.2, 1.1, -0.3 };
        double[] localVelocity = {
            c*globalVelocity[0]+s*globalVelocity[1], -s*globalVelocity[0]+c*globalVelocity[1], globalVelocity[2],
            c*globalVelocity[3]+s*globalVelocity[4], -s*globalVelocity[3]+c*globalVelocity[4], globalVelocity[5]
        };
        AssertClose(Quadratic(local, localVelocity), Quadratic(global, globalVelocity));
        AssertSymmetricNonNegative(global);
    }

    [Fact]
    public void EducationalLumping_HasExpectedTotalMassButZeroRotationalInertia()
    {
        var frame = Frame2DMassMatrices.LumpedLocal(3, 2);
        AssertClose(3, frame[0, 0]);
        AssertClose(3, frame[1, 1]);
        AssertClose(0, frame[2, 2]);
        AssertClose(0, frame[5, 5]);
        AssertClose(6, Quadratic(frame, new[] { 1.0, 0, 0, 1.0, 0, 0 }));
        var truss = Truss2DMassMatrices.Lumped(3, 2);
        AssertClose(6, Quadratic(truss, new[] { 1.0, 0, 1.0, 0 }));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(-1, 2)]
    [InlineData(double.NaN, 2)]
    [InlineData(1, 0)]
    [InlineData(1, double.PositiveInfinity)]
    public void InvalidInputsAreRejected(double mu, double length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Frame2DMassMatrices.ConsistentLocal(mu, length));
        Assert.Throws<ArgumentOutOfRangeException>(() => Truss2DMassMatrices.Consistent(mu, length));
    }

    internal static double Quadratic(double[,] m, double[] v)
    {
        double value = 0;
        for (int i = 0; i < v.Length; i++)
            for (int j = 0; j < v.Length; j++) value += v[i] * m[i, j] * v[j];
        return value;
    }

    internal static void AssertClose(double expected, double actual, double relativeTolerance = 1e-10)
    {
        double tolerance = relativeTolerance * Math.Max(1.0, Math.Abs(expected));
        Assert.True(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected:R}, found {actual:R}, tolerance {tolerance:R}");
    }

    internal static void CompareMatrices(double[,] expected, double[,] actual, double relativeTolerance = 1e-10)
    {
        Assert.Equal(expected.GetLength(0), actual.GetLength(0));
        Assert.Equal(expected.GetLength(1), actual.GetLength(1));
        for (int i = 0; i < expected.GetLength(0); i++)
            for (int j = 0; j < expected.GetLength(1); j++)
                AssertClose(expected[i, j], actual[i, j], relativeTolerance);
    }

    private static void AssertSymmetricNonNegative(double[,] matrix)
    {
        int n = matrix.GetLength(0);
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++) AssertClose(matrix[i, j], matrix[j, i]);
        // Deterministic diverse velocity probes. Does not replace the analytic PSD proof.
        for (int probe = 0; probe < 64; probe++)
        {
            var vector = Enumerable.Range(0, n).Select(i => Math.Sin((probe+1)*(i+2))).ToArray();
            Assert.True(Quadratic(matrix, vector) >= -1e-10, "Negative kinetic-energy quadratic form");
        }
    }
}
