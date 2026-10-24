using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Frame2D.SecondOrder;

namespace StructuralSolver2D.Analysis.Tests.Frame2D.M81;

/// <summary>
/// M81 isolated linearized beam-column benchmarks. The reference integration,
/// cantilever closed form and pinned-column mode are independent of the code
/// under test. No global second-order frame solver is implied.
/// </summary>
public sealed class Frame2DInitialStressM81Tests
{
    private const double E = 210_000_000d; // kN/m2
    private const double A = 0.006d;       // m2
    private const double I = 0.00002d;     // m4
    private const double L = 4d;
    private static double EI => E * I;

    [Theory]
    [InlineData(4d, -120d)]
    [InlineData(3.25d, 40d)]
    [InlineData(9d, 0d)]
    public void GeometricMatrix_MatchesIndependentHermiteGradientQuadrature(double length, double axialForce)
    {
        double[,] actual = Frame2DInitialStressM81.BuildLocalGeometricStiffness(axialForce, length);
        double[,] reference = IndependentHermiteIntegral(length, axialForce);
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                Close(reference[i, j], actual[i, j], 2e-12);
                Close(actual[i, j], actual[j, i], 2e-14);
                if (i is 0 or 3 || j is 0 or 3)
                {
                    Assert.Equal(0d, actual[i, j]);
                }
            }
        }
    }

    [Fact]
    public void TensionAndCompression_ProduceOppositeSignedEnergyWithoutArtificialAxialCoupling()
    {
        double[,] tension = Frame2DInitialStressM81.BuildLocalGeometricStiffness(100, L);
        double[,] compression = Frame2DInitialStressM81.BuildLocalGeometricStiffness(-100, L);
        double[] bending = { 0d, .003d, .004d, 0d, -.002d, .002d };
        Assert.True(Energy(tension, bending) > 0);
        Close(-Energy(tension, bending), Energy(compression, bending), 1e-12);
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                Close(-tension[i, j], compression[i, j], 1e-13);
            }
        }
        // A uniform transverse translation has zero gradient, hence zero
        // initial-stress geometric energy (without imposing artificial supports).
        double[] shift = { 0d, .012d, 0d, 0d, .012d, 0d };
        Close(0d, Energy(tension, shift), 1e-11);
    }

    [Fact]
    public void ZeroAxialForce_ExactlyRecoversLegacyFrameElasticStiffness()
    {
        double[,] tangent = Frame2DInitialStressM81.BuildLocalLinearizedTangent(E, A, I, L, 0);
        double[,] original = Frame2DElementMatrices.BuildLocalStiffness(E, A, I, L);
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                Assert.Equal(original[i, j], tangent[i, j]);
            }
        }
    }

    [Theory]
    [InlineData(0.6d, 0.8d)]
    [InlineData(-0.8d, 0.6d)]
    [InlineData(0d, 1d)]
    public void RotatedMember_PreservesQuadraticFormAndSymmetry(double cosine, double sine)
    {
        const double force = -240;
        double[,] local = Frame2DInitialStressM81.BuildLocalLinearizedTangent(E, A, I, L, force);
        double[,] global = Frame2DInitialStressM81.BuildGlobalLinearizedTangent(E, A, I, L, cosine, sine, force);
        double[] u = { .002, -.003, .015, -.001, .004, -.01 };
        double[,] transformation = Frame2DElementMatrices.BuildTransformation(cosine, sine);
        double[] localU = Frame2DElementMatrices.TransformDisplacementToLocal(u, transformation);
        Close(Energy(local, localU), Energy(global, u), 1e-10);
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                Close(global[i, j], global[j, i], 1e-12);
            }
        }
    }

    [Fact]
    public void CantileverWithNoPreload_RecoversClosedFormEndDisplacementRotationAndEnergy()
    {
        const double tipForce = 2d;
        var result = Frame2DInitialStressM81.SolveCantileverTipLoad(E, A, I, L, 0, tipForce);
        Close(tipForce * L * L * L / (3d * EI), result.TipTransverseDisplacement, 1e-12);
        Close(tipForce * L * L / (2d * EI), result.TipRotation, 1e-12);
        Close(tipForce * result.TipTransverseDisplacement / 2d, result.LinearizedStrainEnergy, 1e-12);
    }

    [Fact]
    public void CompressionAmplifiesAndTensionReducesCantileverResponse_BeforeBuckling()
    {
        double halfEuler = 0.5d * Math.PI * Math.PI / 4d * EI / (L * L);
        const double lateralForce = 1.5d;
        var reference = Frame2DInitialStressM81.SolveCantileverTipLoad(E, A, I, L, 0d, lateralForce);
        var compression = Frame2DInitialStressM81.SolveCantileverTipLoad(E, A, I, L, -halfEuler, lateralForce);
        var tension = Frame2DInitialStressM81.SolveCantileverTipLoad(E, A, I, L, +halfEuler, lateralForce);
        Assert.True(compression.TipTransverseDisplacement > reference.TipTransverseDisplacement);
        Assert.True(reference.TipTransverseDisplacement > tension.TipTransverseDisplacement);
        Assert.True(compression.TipRotation > reference.TipRotation);
    }

    [Fact]
    public void CantileverTwoDof_DiscreteCriticalForceMatchesIndependentQuadratic_AndFailsClosed()
    {
        // Closed-form det[Kff - P*Gff] in x=P*L^2/EI:
        // 0.15*x^2 - 5.2*x + 12 = 0. The lower root is first buckling.
        double dimensionless = (5.2d - Math.Sqrt((5.2d * 5.2d) - (4d * 0.15d * 12d))) / (2d * 0.15d);
        double critical = dimensionless * EI / (L * L);
        double eulerContinuum = Math.PI * Math.PI * EI / (4d * L * L);
        Assert.InRange(critical / eulerContinuum, 1d, 1.01d);

        var moderate = Frame2DInitialStressM81.SolveCantileverTipLoad(E, A, I, L, -0.5d * critical, 1d);
        var near = Frame2DInitialStressM81.SolveCantileverTipLoad(E, A, I, L, -0.99d * critical, 1d);
        Assert.True(near.TipTransverseDisplacement > moderate.TipTransverseDisplacement * 10d);
        InvalidOperationException atCritical = Assert.Throws<InvalidOperationException>(() =>
            Frame2DInitialStressM81.SolveCantileverTipLoad(E, A, I, L, -critical, 1d));
        Assert.Contains("M81_LINEARIZED_INSTABILITY", atCritical.Message);
        Assert.Throws<InvalidOperationException>(() =>
            Frame2DInitialStressM81.SolveCantileverTipLoad(E, A, I, L, -1.01d * critical, 1d));
    }

    [Fact]
    public void PinnedColumnOneElement_ExactDiscreteAntisymmetricModeBifurcatesAt12EIOverL2()
    {
        // Pinned/pinned end translations fixed, rotations free. The eigenvector
        // [theta_start, theta_end]=[1,-1] has critical P=12*EI/L^2.
        double critical = 12d * EI / (L * L);
        double[] oppositeEndRotations = { 0d, 0d, 1d, 0d, 0d, -1d };
        var tangent = Frame2DInitialStressM81.BuildLocalLinearizedTangent(E, A, I, L, -critical);
        Close(0d, Energy(tangent, oppositeEndRotations), 1e-10);
        // Discretization overestimates the Euler continuum pinned/pinned load;
        // no claim of a validated global eigenbuckling solver at M81.
        Assert.True(critical > Math.PI * Math.PI * EI / (L * L));
    }

    [Fact]
    public void InvalidParameters_AndNonunitMemberAxis_AreExplicitlyRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Frame2DInitialStressM81.BuildLocalGeometricStiffness(5d, 0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => Frame2DInitialStressM81.BuildLocalGeometricStiffness(double.NaN, L));
        Assert.Throws<ArgumentOutOfRangeException>(() => Frame2DInitialStressM81.BuildLocalLinearizedTangent(E, A, -I, L, 1d));
        Assert.Throws<ArgumentException>(() => Frame2DInitialStressM81.BuildGlobalLinearizedTangent(E, A, I, L, .5d, .5d, 1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => Frame2DInitialStressM81.SolveCantileverTipLoad(E, A, I, L, 0d, double.PositiveInfinity));
    }

    private static double[,] IndependentHermiteIntegral(double length, double axialForce)
    {
        // Three Gauss-Legendre stations integrate the quartic N'i N'j exactly.
        double[] points = { -Math.Sqrt(3d / 5d), 0d, Math.Sqrt(3d / 5d) };
        double[] weights = { 5d / 9d, 8d / 9d, 5d / 9d };
        int[] map = { 1, 2, 4, 5 };
        double[,] result = new double[6, 6];
        for (int k = 0; k < 3; k++)
        {
            double r = (points[k] + 1d) / 2d;
            double[] derivative =
            {
                (-6d * r + 6d * r * r) / length,
                1d - 4d * r + 3d * r * r,
                (6d * r - 6d * r * r) / length,
                -2d * r + 3d * r * r
            };
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result[map[i], map[j]] += axialForce * (length / 2d) * weights[k] * derivative[i] * derivative[j];
                }
            }
        }
        return result;
    }

    private static double Energy(double[,] k, double[] u)
    {
        double result = 0;
        for (int i = 0; i < u.Length; i++)
        {
            for (int j = 0; j < u.Length; j++)
            {
                result += u[i] * k[i, j] * u[j];
            }
        }
        return result;
    }

    private static void Close(double expected, double actual, double relativeTolerance)
    {
        double scale = Math.Max(1d, Math.Abs(expected));
        Assert.True(double.IsFinite(actual) && Math.Abs(actual - expected) <= relativeTolerance * scale,
            $"Expected {expected:G17}, actual {actual:G17}; relative tolerance {relativeTolerance:G17}.");
    }
}
