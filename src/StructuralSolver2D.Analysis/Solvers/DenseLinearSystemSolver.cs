namespace StructuralSolver2D.Analysis.Solvers;

/// <summary>
/// Solves small dense linear systems with Gaussian elimination and partial pivoting.
/// This solver is intentionally simple and will be replaced by a sparse solver when needed.
/// </summary>
internal static class DenseLinearSystemSolver
{
    private const double PivotTolerance = 1e-12;

    /// <summary>
    /// Solves the linear system A · x = b.
    /// </summary>
    public static double[] Solve(double[,] matrix, double[] rightHandSide)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(rightHandSide);

        int rowCount = matrix.GetLength(0);
        int columnCount = matrix.GetLength(1);

        if (rowCount != columnCount)
        {
            throw new ArgumentException("The coefficient matrix must be square.", nameof(matrix));
        }

        if (rightHandSide.Length != rowCount)
        {
            throw new ArgumentException("The right-hand side vector size does not match the matrix size.", nameof(rightHandSide));
        }

        double[,] a = (double[,])matrix.Clone();
        double[] b = (double[])rightHandSide.Clone();

        for (int pivotIndex = 0; pivotIndex < rowCount; pivotIndex++)
        {
            int bestRow = pivotIndex;
            double bestPivot = Math.Abs(a[pivotIndex, pivotIndex]);

            for (int row = pivotIndex + 1; row < rowCount; row++)
            {
                double candidate = Math.Abs(a[row, pivotIndex]);
                if (candidate > bestPivot)
                {
                    bestPivot = candidate;
                    bestRow = row;
                }
            }

            if (bestPivot < PivotTolerance || !double.IsFinite(bestPivot))
            {
                throw new StructuralAnalysisException("The reduced stiffness matrix is singular. The model is probably unstable or insufficiently constrained.");
            }

            if (bestRow != pivotIndex)
            {
                SwapRows(a, pivotIndex, bestRow);
                (b[pivotIndex], b[bestRow]) = (b[bestRow], b[pivotIndex]);
            }

            for (int row = pivotIndex + 1; row < rowCount; row++)
            {
                double factor = a[row, pivotIndex] / a[pivotIndex, pivotIndex];
                if (factor == 0)
                {
                    continue;
                }

                a[row, pivotIndex] = 0;

                for (int column = pivotIndex + 1; column < rowCount; column++)
                {
                    a[row, column] -= factor * a[pivotIndex, column];
                }

                b[row] -= factor * b[pivotIndex];
            }
        }

        double[] x = new double[rowCount];

        for (int row = rowCount - 1; row >= 0; row--)
        {
            double sum = b[row];

            for (int column = row + 1; column < rowCount; column++)
            {
                sum -= a[row, column] * x[column];
            }

            x[row] = sum / a[row, row];
        }

        return x;
    }

    private static void SwapRows(double[,] matrix, int firstRow, int secondRow)
    {
        int columnCount = matrix.GetLength(1);

        for (int column = 0; column < columnCount; column++)
        {
            (matrix[firstRow, column], matrix[secondRow, column]) = (matrix[secondRow, column], matrix[firstRow, column]);
        }
    }
}
