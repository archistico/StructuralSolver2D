namespace StructuralSolver2D.Analysis.Dynamics.Seismic;

/// <summary>
/// M70: Complete Quadratic Combination for classically damped modes with ONE common
/// modal damping ratio. Uses signed real modal displacement contributions, NOT absolute
/// modal amplitudes. Does not perform direction, member force, or code combinations.
/// </summary>
internal static class EqualDampingCqc
{
    internal static void Validate(double? dampingRatio)
    {
        if (dampingRatio is not double zeta || !double.IsFinite(zeta) ||
            zeta < 1e-6 || zeta > 0.30)
            throw new ArgumentException(
                "M70_CQC_DAMPING: CQC requires explicit finite uniform modal damping ratio in [1e-6, 0.30].");
    }

    /// <summary>Correlation of two modes with equal damping. Frequencies in Hz (or any
    /// consistent frequency unit). Algebra is symmetric under swapping i and j.
    /// </summary>
    internal static double Correlation(double fi, double fj, double zeta)
    {
        Validate(zeta);
        if (!double.IsFinite(fi) || !double.IsFinite(fj) || fi <= 0 || fj <= 0)
            throw new ArgumentException("M70_CQC_FREQUENCY: Strictly positive finite natural frequencies required.");
        double r = Math.Min(fi, fj) / Math.Max(fi, fj);
        double z2 = zeta * zeta;
        double oneMinusR2 = (1 - r) * (1 + r);
        double onePlusR = 1 + r;
        double numerator = 8 * z2 * onePlusR * r * Math.Sqrt(r);
        double denominator = oneMinusR2 * oneMinusR2 + 4 * z2 * r * onePlusR * onePlusR;
        double rho = numerator / denominator;
        if (!double.IsFinite(rho) || rho < 0 || rho > 1 + 1e-12)
            throw new InvalidOperationException("M70_CQC_CORRELATION_RANGE: Invalid modal correlation.");
        return Math.Min(1, rho);
    }

    /// <summary>sqrt(sum_i sum_j rho_ij a_i a_j) for signed modal contributions.
    /// </summary>
    internal static double Combine(IReadOnlyList<double> modalContributions,
        IReadOnlyList<double> frequenciesHertz, double dampingRatio)
    {
        Validate(dampingRatio);
        ArgumentNullException.ThrowIfNull(modalContributions);
        ArgumentNullException.ThrowIfNull(frequenciesHertz);
        int n = modalContributions.Count;
        if (n == 0 || n != frequenciesHertz.Count)
            throw new ArgumentException("M70_CQC_MODES: Equal nonzero lengths required for modal amplitudes and frequencies.");
        // Normalize to reduce overflow and preserve accuracy over disparate amplitudes.
        double scale = 0;
        for (int i = 0; i < n; i++)
        {
            double amplitude = modalContributions[i];
            double frequency = frequenciesHertz[i];
            if (!double.IsFinite(amplitude))
                throw new ArgumentException("M70_CQC_AMPLITUDE: Nonfinite modal contribution.");
            if (!double.IsFinite(frequency) || frequency <= 0)
                throw new ArgumentException("M70_CQC_FREQUENCY: Strictly positive finite natural frequencies required.");
            scale = Math.Max(scale, Math.Abs(amplitude));
        }
        if (scale == 0) return 0;
        double[] normalized = new double[n];
        for (int i = 0; i < n; i++) normalized[i] = modalContributions[i] / scale;
        double sum = 0;
        double magnitude = 0;
        for (int i = 0; i < n; i++)
        {
            double term = normalized[i] * normalized[i];
            sum += term;
            magnitude += term;
            for (int j = 0; j < i; j++)
            {
                double cross = 2 * Correlation(frequenciesHertz[i], frequenciesHertz[j], dampingRatio)
                    * normalized[i] * normalized[j];
                sum += cross;
                magnitude += Math.Abs(cross);
            }
        }
        if (sum < -1e-12 * magnitude || !double.IsFinite(sum))
            throw new InvalidOperationException("M70_CQC_QUADRATIC_RANGE: Invalid CQC quadratic form.");
        double result = scale * Math.Sqrt(Math.Max(0, sum));
        if (!double.IsFinite(result))
            throw new ArgumentException("M70_CQC_RESULT_RANGE: Nonfinite CQC envelope.");
        return result;
    }
}
