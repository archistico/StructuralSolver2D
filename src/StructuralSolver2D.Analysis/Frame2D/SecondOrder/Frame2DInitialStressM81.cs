using StructuralSolver2D.Analysis.Frame2D;

namespace StructuralSolver2D.Analysis.Frame2D.SecondOrder;

/// <summary>
/// Linearized, small-displacement, Euler-Bernoulli beam-column initial-stress stiffness.
/// LOCAL DOFs: [u1, v1, theta1, u2, v2, theta2]. Tension N &gt; 0 stiffens;
/// compression N &lt; 0 softens. The axial force is a CONSTANT, prescribed reference
/// force, not inferred from the analyzed structure; no global P-Delta iteration.
/// Units: N [kN], L [m], E [kN/m2], A [m2], I [m4].
/// Supported here only for a prismatic, unreleased Frame2D member.
/// </summary>
internal static class Frame2DInitialStressM81
{
    private static readonly int[] FlexuralDofs = { 1, 2, 4, 5 };

    /// <summary>
    /// Consistent geometric stiffness from (N/2) integral_0^L (dv/dx)^2 dx,
    /// using cubic Hermite interpolation for transverse displacement.
    /// Does not incorporate the nonlinear strain-displacement relation beyond the
    /// linearized initial-stress term, shear deformation or follower loads.
    /// </summary>
    public static double[,] BuildLocalGeometricStiffness(double axialForce, double length)
    {
        RequireFinite(axialForce, nameof(axialForce));
        RequirePositive(length, nameof(length));

        double[,] result = new double[6, 6];
        if (axialForce == 0d)
        {
            return result;
        }

        double l2 = length * length;
        double[,] coefficients =
        {
            { 36d, 3d * length, -36d, 3d * length },
            { 3d * length, 4d * l2, -3d * length, -l2 },
            { -36d, -3d * length, 36d, -3d * length },
            { 3d * length, -l2, -3d * length, 4d * l2 }
        };

        double factor = axialForce / (30d * length);
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                double value = factor * coefficients[row, column];
                if (!double.IsFinite(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(axialForce), "Geometric stiffness overflowed the supported numerical range.");
                }

                result[FlexuralDofs[row], FlexuralDofs[column]] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Reference elastic tangent Kt = Kelastic + Kgeometric at the prescribed N.
    /// This is NOT a nonlinear member state update or an equilibrium solution.
    /// </summary>
    public static double[,] BuildLocalLinearizedTangent(
        double elasticModulus, double area, double momentOfInertia,
        double length, double axialForce)
    {
        RequirePositive(elasticModulus, nameof(elasticModulus));
        RequirePositive(area, nameof(area));
        RequirePositive(momentOfInertia, nameof(momentOfInertia));
        double[,] material = Frame2DElementMatrices.BuildLocalStiffness(
            elasticModulus, area, momentOfInertia, length);
        double[,] geometric = BuildLocalGeometricStiffness(axialForce, length);
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                material[i, j] += geometric[i, j];
                if (!double.IsFinite(material[i, j]))
                {
                    throw new ArgumentOutOfRangeException(nameof(axialForce), "Linearized tangent overflowed the supported numerical range.");
                }
            }
        }

        return material;
    }

    public static double[,] BuildGlobalLinearizedTangent(
        double elasticModulus, double area, double momentOfInertia,
        double length, double cosine, double sine, double axialForce)
    {
        RequireFinite(cosine, nameof(cosine));
        RequireFinite(sine, nameof(sine));
        if (Math.Abs(cosine * cosine + sine * sine - 1d) > 1e-10)
        {
            throw new ArgumentException("M81_DIRECTION: cos/sin must form a unit direction.", nameof(cosine));
        }

        double[,] local = BuildLocalLinearizedTangent(
            elasticModulus, area, momentOfInertia, length, axialForce);
        double[,] t = Frame2DElementMatrices.BuildTransformation(cosine, sine);
        return Frame2DElementMatrices.TransformStiffnessToGlobal(local, t);
    }

    /// <summary>
    /// Small demonstrator: cantilever with fixed start, free end, constant axial
    /// preforce and transverse end force/moment. Solves the 2x2 flexural tangent
    /// block, not a global frame. Rejects a non-positive or nearly singular
    /// flexural tangent (incipient linearized buckling).
    /// </summary>
    public static CantileverSecondOrderResultM81 SolveCantileverTipLoad(
        double elasticModulus, double area, double momentOfInertia,
        double length, double axialForce, double transverseTipForce, double tipMoment = 0d)
    {
        RequireFinite(transverseTipForce, nameof(transverseTipForce));
        RequireFinite(tipMoment, nameof(tipMoment));
        double[,] k = BuildLocalLinearizedTangent(
            elasticModulus, area, momentOfInertia, length, axialForce);
        double a = k[4, 4];
        double b = k[4, 5];
        double c = k[5, 5];
        double det = a * c - b * b;
        // Sylvester criterion for the symmetric, unrestrained 2-DOF tip block.
        // Scale-relative criterion avoids silently reporting huge, unreliable
        // deformations at the linearized critical load.
        if (!(a > 0d) || !(c > 0d) || !double.IsFinite(det) ||
            det <= 1e-11 * a * c)
        {
            throw new InvalidOperationException(
                "M81_LINEARIZED_INSTABILITY: cantilever tangent is singular, indefinite or too close to its critical load.");
        }

        double displacement = (c * transverseTipForce - b * tipMoment) / det;
        double rotation = (a * tipMoment - b * transverseTipForce) / det;
        if (!double.IsFinite(displacement) || !double.IsFinite(rotation))
        {
            throw new InvalidOperationException("M81_NONFINITE_RESPONSE: cantilever response is not finite.");
        }

        return new CantileverSecondOrderResultM81(
            displacement, rotation, axialForce, 0.5d * (transverseTipForce * displacement + tipMoment * rotation));
    }

    private static void RequireFinite(double value, string name)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, "A finite value is required.");
        }
    }

    private static void RequirePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0d)
        {
            throw new ArgumentOutOfRangeException(name, "A finite, strictly positive value is required.");
        }
    }
}

internal sealed record CantileverSecondOrderResultM81(
    double TipTransverseDisplacement,
    double TipRotation,
    double ReferenceAxialForce,
    double LinearizedStrainEnergy);
