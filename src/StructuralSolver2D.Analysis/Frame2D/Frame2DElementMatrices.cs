namespace StructuralSolver2D.Analysis.Frame2D;

/// <summary>
/// Builds stiffness and transformation matrices for a two-dimensional frame element.
/// </summary>
internal static class Frame2DElementMatrices
{
    /// <summary>
    /// Builds the local 6x6 stiffness matrix of a 2D frame element.
    /// Units are coherent with kN, m and kNm.
    /// </summary>
    public static double[,] BuildLocalStiffness(double elasticModulus, double area, double momentOfInertia, double length)
    {
        double axial = elasticModulus * area / length;
        double flexural = elasticModulus * momentOfInertia;
        double l2 = length * length;
        double l3 = l2 * length;

        double[,] k = new double[6, 6];

        k[0, 0] = axial;
        k[0, 3] = -axial;
        k[3, 0] = -axial;
        k[3, 3] = axial;

        k[1, 1] = 12 * flexural / l3;
        k[1, 2] = 6 * flexural / l2;
        k[1, 4] = -12 * flexural / l3;
        k[1, 5] = 6 * flexural / l2;

        k[2, 1] = 6 * flexural / l2;
        k[2, 2] = 4 * flexural / length;
        k[2, 4] = -6 * flexural / l2;
        k[2, 5] = 2 * flexural / length;

        k[4, 1] = -12 * flexural / l3;
        k[4, 2] = -6 * flexural / l2;
        k[4, 4] = 12 * flexural / l3;
        k[4, 5] = -6 * flexural / l2;

        k[5, 1] = 6 * flexural / l2;
        k[5, 2] = 2 * flexural / length;
        k[5, 4] = -6 * flexural / l2;
        k[5, 5] = 4 * flexural / length;

        return k;
    }

    /// <summary>
    /// Builds the local-to-global displacement transformation matrix.
    /// The local displacement vector is obtained as uLocal = T · uGlobal.
    /// </summary>
    public static double[,] BuildTransformation(double cosine, double sine)
    {
        double[,] t = new double[6, 6];

        t[0, 0] = cosine;
        t[0, 1] = sine;
        t[1, 0] = -sine;
        t[1, 1] = cosine;
        t[2, 2] = 1;

        t[3, 3] = cosine;
        t[3, 4] = sine;
        t[4, 3] = -sine;
        t[4, 4] = cosine;
        t[5, 5] = 1;

        return t;
    }

    /// <summary>
    /// Transforms a local stiffness matrix into global coordinates.
    /// </summary>
    public static double[,] TransformStiffnessToGlobal(double[,] localStiffness, double[,] transformation)
    {
        double[,] temp = Multiply(localStiffness, transformation);
        double[,] transformationTranspose = Transpose(transformation);
        return Multiply(transformationTranspose, temp);
    }

    /// <summary>
    /// Transforms a local load vector into global coordinates.
    /// </summary>
    public static double[] TransformLoadToGlobal(double[] localLoad, double[,] transformation)
    {
        double[,] transformationTranspose = Transpose(transformation);
        return Multiply(transformationTranspose, localLoad);
    }

    /// <summary>
    /// Transforms a global displacement vector into local coordinates.
    /// </summary>
    public static double[] TransformDisplacementToLocal(double[] globalDisplacement, double[,] transformation) =>
        Multiply(transformation, globalDisplacement);

    /// <summary>
    /// Builds the consistent local equivalent nodal load vector for a uniform local load.
    /// </summary>
    public static double[] BuildUniformLocalLoad(double localXValue, double localYValue, double length)
    {
        double[] load = new double[6];

        load[0] += localXValue * length / 2;
        load[3] += localXValue * length / 2;

        load[1] += localYValue * length / 2;
        load[2] += localYValue * length * length / 12;
        load[4] += localYValue * length / 2;
        load[5] += -localYValue * length * length / 12;

        return load;
    }



    /// <summary>
    /// Builds the consistent local equivalent nodal load vector for a linearly varying local load.
    /// Start values act at the start node side; end values act at the end node side.
    /// Axial and transverse components are expressed in kN/m.
    /// </summary>
    public static double[] BuildLinearLocalLoad(
        double startLocalXValue,
        double endLocalXValue,
        double startLocalYValue,
        double endLocalYValue,
        double length)
    {
        double[] load = new double[6];

        load[0] += length * ((2.0 * startLocalXValue) + endLocalXValue) / 6.0;
        load[3] += length * (startLocalXValue + (2.0 * endLocalXValue)) / 6.0;

        load[1] += length * ((7.0 * startLocalYValue) + (3.0 * endLocalYValue)) / 20.0;
        load[2] += length * length * ((startLocalYValue / 20.0) + (endLocalYValue / 30.0));
        load[4] += length * ((3.0 * startLocalYValue) + (7.0 * endLocalYValue)) / 20.0;
        load[5] += -length * length * ((startLocalYValue / 30.0) + (endLocalYValue / 20.0));

        return load;
    }

    /// <summary>
    /// Builds the consistent local equivalent nodal load vector for a concentrated local load.
    /// The position is normalized from 0.0 at the start node to 1.0 at the end node.
    /// </summary>
    public static double[] BuildPointLocalLoad(double localXValue, double localYValue, double length, double normalizedPosition)
    {
        if (normalizedPosition < 0.0 || normalizedPosition > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(normalizedPosition), normalizedPosition, "Point-load position must be between 0.0 and 1.0.");
        }

        double r = normalizedPosition;
        double r2 = r * r;
        double r3 = r2 * r;

        double[] load = new double[6];

        load[0] += localXValue * (1.0 - r);
        load[3] += localXValue * r;

        load[1] += localYValue * (1.0 - (3.0 * r2) + (2.0 * r3));
        load[2] += localYValue * length * (r - (2.0 * r2) + r3);
        load[4] += localYValue * ((3.0 * r2) - (2.0 * r3));
        load[5] += localYValue * length * (-r2 + r3);

        return load;
    }



    /// <summary>
    /// Applies end moment releases to a local frame stiffness matrix by static condensation.
    /// Released rotational DOFs are local indexes 2 (start) and 5 (end).
    /// </summary>
    public static double[,] ApplyMomentReleasesToStiffness(
        double[,] localStiffness,
        bool releaseStartMoment,
        bool releaseEndMoment) =>
        ApplyMomentReleases(localStiffness, new double[6], releaseStartMoment, releaseEndMoment).Stiffness;

    /// <summary>
    /// Applies end moment releases to both local stiffness and equivalent local load vector.
    /// This keeps the released end moments equal to zero in the element contribution.
    /// </summary>
    public static (double[,] Stiffness, double[] Load) ApplyMomentReleases(
        double[,] localStiffness,
        double[] localLoad,
        bool releaseStartMoment,
        bool releaseEndMoment)
    {
        ArgumentNullException.ThrowIfNull(localStiffness);
        ArgumentNullException.ThrowIfNull(localLoad);

        if (localStiffness.GetLength(0) != 6 || localStiffness.GetLength(1) != 6)
        {
            throw new ArgumentException("Frame2D local stiffness must be a 6x6 matrix.", nameof(localStiffness));
        }

        if (localLoad.Length != 6)
        {
            throw new ArgumentException("Frame2D local load vector must have six components.", nameof(localLoad));
        }

        List<int> released = new();
        if (releaseStartMoment)
        {
            released.Add(2);
        }

        if (releaseEndMoment)
        {
            released.Add(5);
        }

        double[,] stiffness = CloneMatrix(localStiffness);
        double[] load = (double[])localLoad.Clone();

        if (released.Count == 0)
        {
            return (stiffness, load);
        }

        int[] retained = Enumerable.Range(0, 6).Where(index => !released.Contains(index)).ToArray();
        double[,] kcc = ExtractMatrix(localStiffness, released, released);
        double[,] inverseKcc = InvertSmallMatrix(kcc);

        double[,] condensedStiffness = new double[6, 6];
        double[] condensedLoad = new double[6];

        foreach (int row in retained)
        {
            foreach (int column in retained)
            {
                double correction = 0;
                for (int r1 = 0; r1 < released.Count; r1++)
                {
                    for (int r2 = 0; r2 < released.Count; r2++)
                    {
                        correction += localStiffness[row, released[r1]] * inverseKcc[r1, r2] * localStiffness[released[r2], column];
                    }
                }

                condensedStiffness[row, column] = localStiffness[row, column] - correction;
            }

            double loadCorrection = 0;
            for (int r1 = 0; r1 < released.Count; r1++)
            {
                for (int r2 = 0; r2 < released.Count; r2++)
                {
                    loadCorrection += localStiffness[row, released[r1]] * inverseKcc[r1, r2] * localLoad[released[r2]];
                }
            }

            condensedLoad[row] = localLoad[row] - loadCorrection;
        }

        return (condensedStiffness, condensedLoad);
    }

    /// <summary>
    /// Multiplies a matrix by a matrix.
    /// </summary>
    public static double[,] Multiply(double[,] left, double[,] right)
    {
        int rows = left.GetLength(0);
        int inner = left.GetLength(1);
        int columns = right.GetLength(1);

        if (inner != right.GetLength(0))
        {
            throw new ArgumentException("Matrix dimensions are not compatible.", nameof(right));
        }

        double[,] result = new double[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                double sum = 0;
                for (int index = 0; index < inner; index++)
                {
                    sum += left[row, index] * right[index, column];
                }

                result[row, column] = sum;
            }
        }

        return result;
    }

    /// <summary>
    /// Multiplies a matrix by a vector.
    /// </summary>
    public static double[] Multiply(double[,] matrix, double[] vector)
    {
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);

        if (columns != vector.Length)
        {
            throw new ArgumentException("Matrix and vector dimensions are not compatible.", nameof(vector));
        }

        double[] result = new double[rows];

        for (int row = 0; row < rows; row++)
        {
            double sum = 0;
            for (int column = 0; column < columns; column++)
            {
                sum += matrix[row, column] * vector[column];
            }

            result[row] = sum;
        }

        return result;
    }

    /// <summary>
    /// Transposes a matrix.
    /// </summary>
    public static double[,] Transpose(double[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);
        double[,] result = new double[columns, rows];

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                result[column, row] = matrix[row, column];
            }
        }

        return result;
    }

    private static double[,] CloneMatrix(double[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);
        double[,] clone = new double[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                clone[row, column] = matrix[row, column];
            }
        }

        return clone;
    }

    private static double[,] ExtractMatrix(double[,] matrix, IReadOnlyList<int> rows, IReadOnlyList<int> columns)
    {
        double[,] result = new double[rows.Count, columns.Count];

        for (int row = 0; row < rows.Count; row++)
        {
            for (int column = 0; column < columns.Count; column++)
            {
                result[row, column] = matrix[rows[row], columns[column]];
            }
        }

        return result;
    }

    private static double[,] InvertSmallMatrix(double[,] matrix)
    {
        int size = matrix.GetLength(0);
        if (size != matrix.GetLength(1) || size is < 1 or > 2)
        {
            throw new ArgumentException("Only 1x1 and 2x2 release matrices are supported.", nameof(matrix));
        }

        if (size == 1)
        {
            if (Math.Abs(matrix[0, 0]) < 1e-20)
            {
                throw new InvalidOperationException("Release condensation matrix is singular.");
            }

            return new[,] { { 1.0 / matrix[0, 0] } };
        }

        double determinant = (matrix[0, 0] * matrix[1, 1]) - (matrix[0, 1] * matrix[1, 0]);
        if (Math.Abs(determinant) < 1e-20)
        {
            throw new InvalidOperationException("Release condensation matrix is singular.");
        }

        return new[,]
        {
            { matrix[1, 1] / determinant, -matrix[0, 1] / determinant },
            { -matrix[1, 0] / determinant, matrix[0, 0] / determinant },
        };
    }

}
