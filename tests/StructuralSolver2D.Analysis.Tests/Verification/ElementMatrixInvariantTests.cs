using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Truss2D;

namespace StructuralSolver2D.Analysis.Tests.Verification;

public sealed class ElementMatrixInvariantTests
{
    [Theory]
    [InlineData(0.6, 0.8)]
    [InlineData(-0.6, -0.8)]
    [InlineData(1.0, 0.0)]
    [InlineData(0.0, -1.0)]
    public void Frame_TransformPreservesElasticEnergy_AndRigidMotionProducesNoForces(double c, double s)
    {
        const double length = 5;
        double[,] local = Frame2DElementMatrices.BuildLocalStiffness(210_000_000, .003, .00002, length);
        double[,] transformation = Frame2DElementMatrices.BuildTransformation(c, s);
        double[,] global = Frame2DElementMatrices.TransformStiffnessToGlobal(local, transformation);
        double[] arbitrary = { .0003, -.0005, .001, -.0012, .0007, -.0004 };
        double[] inLocal = Frame2DElementMatrices.TransformDisplacementToLocal(arbitrary, transformation);
        double localEnergy = Bilinear(inLocal, local);
        double globalEnergy = Bilinear(arbitrary, global);
        Assert.True(Math.Abs(localEnergy - globalEnergy) < 1e-10 * Math.Max(1.0, Math.Abs(localEnergy)));
        AssertSymmetricFinite(global);
        AssertKernel(global, new[] { 1.0, 0, 0, 1, 0, 0 });
        AssertKernel(global, new[] { 0.0, 1, 0, 0, 1, 0 });
        AssertKernel(global, new[] { 0.0, 0, 1, -s * length, c * length, 1 });
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ReleasedFrame_RetainsSymmetryAndRigidBodyKernel(bool start, bool end)
    {
        const double length = 5;
        const double c = .6;
        const double s = .8;
        double[,] raw = Frame2DElementMatrices.BuildLocalStiffness(210_000_000, .003, .00002, length);
        double[,] condensed = Frame2DElementMatrices.ApplyMomentReleasesToStiffness(raw, start, end);
        double[,] global = Frame2DElementMatrices.TransformStiffnessToGlobal(
            condensed, Frame2DElementMatrices.BuildTransformation(c, s));
        AssertSymmetricFinite(global);
        AssertKernel(global, new[] { 1.0, 0, 0, 1, 0, 0 });
        AssertKernel(global, new[] { 0.0, 1, 0, 0, 1, 0 });
        AssertKernel(global, new[] { 0.0, 0, 1, -s * length, c * length, 1 });
    }

    [Theory]
    [InlineData(.6, .8)]
    [InlineData(-.6, .8)]
    public void Truss_HasThreeIndependentRigidBodyMotions(double c, double s)
    {
        const double length = 5;
        double[,] k = Truss2DElementMatrices.BuildGlobalStiffness(210_000_000, .003, length, c, s);
        AssertSymmetricFinite(k);
        AssertKernel(k, new[] { 1.0, 0, 1, 0 });
        AssertKernel(k, new[] { 0.0, 1, 0, 1 });
        AssertKernel(k, new[] { 0.0, 0, -s * length, c * length });
    }

    private static void AssertSymmetricFinite(double[,] k)
    {
        for (int i = 0; i < k.GetLength(0); i++)
        for (int j = 0; j < k.GetLength(1); j++)
        {
            Assert.True(double.IsFinite(k[i, j]));
            Assert.True(Math.Abs(k[i, j] - k[j, i]) <= 1e-10 * Math.Max(1, Math.Abs(k[i, j])));
        }
    }

    private static void AssertKernel(double[,] k, double[] displacement)
    {
        double scale = 0;
        for (int i = 0; i < displacement.Length; i++)
        for (int j = 0; j < displacement.Length; j++)
            scale = Math.Max(scale, Math.Abs(k[i, j]) * Math.Max(1, Math.Abs(displacement[j])));
        for (int i = 0; i < displacement.Length; i++)
        {
            double residual = 0;
            for (int j = 0; j < displacement.Length; j++) residual += k[i, j] * displacement[j];
            Assert.True(Math.Abs(residual) <= 1e-9 * Math.Max(1, scale), $"DOF {i}: {residual}");
        }
    }

    private static double Bilinear(double[] displacement, double[,] k)
    {
        double total = 0;
        for (int i = 0; i < displacement.Length; i++)
        for (int j = 0; j < displacement.Length; j++)
            total += displacement[i] * k[i, j] * displacement[j];
        return total;
    }
}
