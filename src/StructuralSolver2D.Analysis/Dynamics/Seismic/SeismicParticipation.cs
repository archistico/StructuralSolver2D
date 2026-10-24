using StructuralSolver2D.Analysis.Dynamics.Assembly;
using StructuralSolver2D.Analysis.Dynamics.Modal;
using StructuralSolver2D.Analysis.Dynamics.Reduction;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.Dynamics.Seismic;

/// <summary>Finite-element influence vector with zero rotation. No tributary-mass
/// shortcut: use FULL assembled consistent mass, including coupling to rotations.</summary>
internal static class SeismicParticipation
{
    internal sealed record Prepared(
        double[] Influence, double[] EffectiveInertialForcesPerAcceleration,
        double GroundMass, ExperimentalModalSpectrum Spectrum,
        IReadOnlyList<StructuralSeismicModeParticipation> Modes,
        IReadOnlyList<string> Warnings);

    internal static Prepared Calculate(StructuralModel model, StructuralDynamicsDefinition dynamics,
        SeismicGroundDirection direction, int requestedModes = 0)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(dynamics);
        if (!Enum.IsDefined(direction)) throw new ArgumentException("M60_DIRECTION: Choose GlobalX or GlobalY.");
        if (requestedModes < 0) throw new ArgumentException("M60_MODE_COUNT: Nonnegative count required (0 = all). ");
        StructuralModelValidationResult validation = new StructuralHarmonicInputValidator().Validate(model, dynamics);
        if (!validation.IsValid) throw new StructuralAnalysisException("M60 seismic dynamic model invalid.", validation.Issues);
        DynamicMatrixAssembly assembly;
        DynamicReductionResult reduced;
        try
        {
            assembly = DynamicMatrixAssembler.Assemble(model, dynamics);
            reduced = DynamicBoundaryConditionReducer.Reduce(model, assembly);
        }
        catch (NotSupportedException ex) { throw new StructuralAnalysisException($"M60_UNSUPPORTED: {ex.Message}"); }
        if (!reduced.ReadyForModalSolver)
            throw new StructuralAnalysisException($"M60_REDUCTION: {reduced.DiagnosticCode}: {reduced.Message}");

        int globalDofs = checked(model.Nodes.Count * 3);
        var r = new double[globalDofs];
        for (int j = 0; j < model.Nodes.Count; j++)
            r[j * 3 + (direction == SeismicGroundDirection.GlobalX ? 0 : 1)] = 1;
        // b=M*r. The inertial action for positive ground acceleration is -b*a_g.
        var b = new double[globalDofs];
        for (int i = 0; i < globalDofs; i++)
            for (int j = 0; j < globalDofs; j++) b[i] += assembly.Mass[i, j] * r[j];
        double groundMass = 0;
        for (int j = 0; j < globalDofs; j++) groundMass += r[j] * b[j];
        if (!(groundMass > 0) || !double.IsFinite(groundMass) || b.Any(x => !double.IsFinite(x)))
            throw new ArgumentException("M60_GROUND_MASS: Finite positive total influenced mass is required.");
        int n = reduced.Stiffness.GetLength(0);
        int modesToRetain = requestedModes == 0 ? n : Math.Min(requestedModes, n);
        ExperimentalModalSpectrum eigen = SymmetricGeneralizedModalSolver.Solve(reduced, modesToRetain);
        double cumulative = 0;
        var result = new List<StructuralSeismicModeParticipation>();
        foreach (ModalEigenpair mode in eigen.Modes)
        {
            double gamma = 0;
            for (int i = 0; i < globalDofs; i++) gamma += mode.GlobalMassNormalizedShape[i] * b[i];
            double effectiveMass = gamma * gamma;
            if (!double.IsFinite(effectiveMass)) throw new InvalidOperationException("M60_EFFECTIVE_MASS_RANGE: Nonfinite participation.");
            cumulative += effectiveMass;
            result.Add(new StructuralSeismicModeParticipation(mode.ModeNumber, mode.FrequencyHertz,
                mode.PeriodSeconds, gamma, effectiveMass, 100 * effectiveMass / groundMass,
                100 * cumulative / groundMass));
        }
        if (cumulative > groundMass * (1 + 1e-7))
            throw new InvalidOperationException("M60_MASS_IDENTITY: Retained effective mass exceeds total ground mass.");
        var warnings = new List<string> {
            "NON-NORMATIVE exploratory earthquake analysis: no NTC/Eurocode spectrum, soil model, behavior factor or safety certification.",
            "Gamma=phi^T M r for unit base translation; effective mass=Gamma². Ground mass=r^T M r before supports; retained percentage may be below 100% even with every admissible mode.",
            "Imposed uniform global-axis ground motion only. No support differential motion, SSI, torsional ground excitation or nonlinear behavior."
        };
        if (modesToRetain < n)
            warnings.Add($"M60_TRUNCATED_MODES: {modesToRetain}/{n} modes retained; analyze sensitivity to truncation.");
        if (cumulative / groundMass < .9)
            warnings.Add($"M60_LOW_MASS: Only {100*cumulative/groundMass:F2}% of total ground mass participates in retained admissible modes; inspect supports, direction and mode count. Low ratio is not automatically numerical error.");
        return new Prepared(r, b, groundMass, eigen, result, warnings);
    }
}
