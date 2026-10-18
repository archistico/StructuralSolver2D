namespace StructuralSolver2D.Analysis.Solvers;

/// <summary>
/// Solves small dense linear systems with Gaussian elimination and partial pivoting.
/// This solver is intentionally simple and targets small/medium educational and prototype models.
/// It stores the full matrix in memory and has cubic computational complexity, so large production
/// models should use a future sparse solver implementation.
/// </summary>
internal static class DenseLinearSystemSolver
{
    private const double RelativePivotTolerance = 1e-12;

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

        // Equilibrate rows and columns before testing pivots. In a constrained system,
        // stiffness entries and constraint coefficients have very different scales.
        double[,] scaled = new double[rowCount, rowCount];
        double[] scaledRightHandSide = new double[rowCount];
        double[] columnScales = new double[rowCount];
        for (int row = 0; row < rowCount; row++)
        {
            double rowScale = 0;
            for (int column = 0; column < rowCount; column++)
            {
                if (!double.IsFinite(matrix[row, column]))
                    throw new StructuralAnalysisException("The coefficient matrix contains non-finite values.");
                rowScale = Math.Max(rowScale, Math.Abs(matrix[row, column]));
            }
            if (rowScale == 0) throw Singular(row);
            if (!double.IsFinite(rightHandSide[row]))
                throw new StructuralAnalysisException("The load vector contains non-finite values.");
            scaledRightHandSide[row] = rightHandSide[row] / rowScale;
            for (int column = 0; column < rowCount; column++)
            {
                scaled[row, column] = matrix[row, column] / rowScale;
                columnScales[column] = Math.Max(columnScales[column], Math.Abs(scaled[row, column]));
            }
        }
        for (int column = 0; column < rowCount; column++)
        {
            if (columnScales[column] == 0) throw Singular(column);
            for (int row = 0; row < rowCount; row++) scaled[row, column] /= columnScales[column];
        }

        double[,] a = (double[,])scaled.Clone();
        double[] b = (double[])scaledRightHandSide.Clone();

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

            if (bestPivot <= RelativePivotTolerance || !double.IsFinite(bestPivot))
            {
                throw Singular(pivotIndex);
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

        double maxSolution = x.Length == 0 ? 0 : x.Max(value => Math.Abs(value));
        for (int row = 0; row < rowCount; row++)
        {
            double product = 0, rowNorm = 0;
            for (int column = 0; column < rowCount; column++)
            {
                product += scaled[row, column] * x[column];
                rowNorm += Math.Abs(scaled[row, column]);
            }
            double residual = Math.Abs(product - scaledRightHandSide[row]);
            double scale = rowNorm * maxSolution + Math.Abs(scaledRightHandSide[row]);
            if (!double.IsFinite(product) || !double.IsFinite(x[row]) || !double.IsFinite(scale) ||
                residual > 1e-9 * scale)
                throw new StructuralAnalysisException("The linear system solution failed the residual check.");
        }
        for (int row = 0; row < rowCount; row++)
        {
            x[row] /= columnScales[row];
            if (!double.IsFinite(x[row]))
                throw new StructuralAnalysisException("The linear system produced a non-finite solution.");
        }
        return x;
    }

    private static StructuralAnalysisException Singular(int pivot) => new(
        $"The constrained stiffness matrix is singular or numerically singular at pivot {pivot}. " +
        "The model is probably unstable, insufficiently constrained, or contains a mechanism.");

    private static void SwapRows(double[,] matrix, int firstRow, int secondRow)
    {
        int columnCount = matrix.GetLength(1);

        for (int column = 0; column < columnCount; column++)
        {
            (matrix[firstRow, column], matrix[secondRow, column]) = (matrix[secondRow, column], matrix[firstRow, column]);
        }
    }
}
