using StructuralSolver2D.Analysis.Solvers;

namespace StructuralSolver2D.Analysis.Tests.Solvers;

public sealed class DenseLinearSystemSolverRegressionTests
{
    [Theory]
    [InlineData(1e-100)]
    [InlineData(1e-20)]
    [InlineData(1.0)]
    [InlineData(1e20)]
    [InlineData(1e100)]
    public void Solve_ShouldPreserveSolutionWhenAllEquationsAreScaled(double scale)
    {
        double[,] matrix = { { 2 * scale, scale }, { scale, 3 * scale } };
        double[] rightHandSide = { scale, 2 * scale };

        double[] solution = DenseLinearSystemSolver.Solve(matrix, rightHandSide);

        Assert.Equal(.2, solution[0], precision: 12);
        Assert.Equal(.6, solution[1], precision: 12);
    }

    [Theory]
    [InlineData(1e-100)]
    [InlineData(1.0)]
    [InlineData(1e100)]
    public void Solve_ShouldRejectRankDeficiencyEvenWithZeroLoads(double scale)
    {
        double[,] matrix = { { scale, 2 * scale }, { 2 * scale, 4 * scale } };
        Assert.Throws<StructuralAnalysisException>(() => DenseLinearSystemSolver.Solve(matrix, new double[2]));
    }

    [Fact]
    public void Solve_ShouldHandleDifferentEquationAndUnknownScales()
    {
        double[,] matrix = { { 1e-15, 1e5 }, { 2e-15, -1e5 } };
        double[] solution = DenseLinearSystemSolver.Solve(matrix, new[] { 2.0, 1.0 });
        Assert.InRange(Math.Abs(solution[0] / 1e15 - 1), 0, 1e-12);
        Assert.InRange(Math.Abs(solution[1] / 1e-5 - 1), 0, 1e-12);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Solve_ShouldRejectNonFiniteInput(double invalid)
    {
        Assert.Throws<StructuralAnalysisException>(() => DenseLinearSystemSolver.Solve(new[,] { { invalid } }, new[] { 1.0 }));
        Assert.Throws<StructuralAnalysisException>(() => DenseLinearSystemSolver.Solve(new[,] { { 1.0 } }, new[] { invalid }));
    }
}
