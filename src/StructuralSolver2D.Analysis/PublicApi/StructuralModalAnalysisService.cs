using StructuralSolver2D.Analysis.Dynamics.Modal;
using StructuralSolver2D.Analysis.Dynamics.Reduction;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Public, opt-in linear elastic 2D modal analysis (M57). Does not run static loads.
/// Supports only the models admitted by the M53–M56 matrix/reduction pipeline.
/// </summary>
public sealed class StructuralModalAnalysisService
{
    public StructuralModalAnalysisOutput Analyze(StructuralModalAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        StructuralModelValidationResult validity = request.Validate();
        if (!validity.IsValid)
            throw new StructuralAnalysisException("Modal input validation failed.", validity.Issues);

        DynamicReductionResult reduced;
        try
        {
            reduced = DynamicBoundaryConditionReducer.AssembleAndReduce(request.Model, request.Dynamics);
        }
        catch (NotSupportedException ex)
        {
            throw new StructuralAnalysisException($"Modal analysis not supported: {ex.Message}");
        }
        if (!reduced.ReadyForModalSolver)
            throw new StructuralAnalysisException($"Modal analysis not supported: {reduced.DiagnosticCode}: {reduced.Message}");

        ExperimentalModalSpectrum spectrum = SymmetricGeneralizedModalSolver.Solve(reduced, request.RequestedModeCount);
        var modes = spectrum.Modes.Select(mode =>
        {
            var nodes = request.Model.Nodes.Select((node, index) =>
            {
                int i = 3 * index;
                return new StructuralModalNodeShape(node.Id, mode.GlobalMassNormalizedShape[i],
                    mode.GlobalMassNormalizedShape[i + 1], mode.GlobalMassNormalizedShape[i + 2]);
            }).ToArray();
            return new StructuralModalModeResult(mode.ModeNumber, mode.AngularFrequencyRadiansPerSecond, nodes)
            {
                EigenvalueRadiansSquaredPerSecondSquared = mode.EigenvalueRadiansSquaredPerSecondSquared,
                RayleighQuotient = mode.RayleighQuotient,
                RelativeResidual = mode.RelativeResidual
            };
        }).ToArray();

        var warnings = new List<string>();
        if (request.RequestedModeCount > spectrum.AvailableModeCount)
            warnings.Add($"Requested {request.RequestedModeCount} modes; only {spectrum.AvailableModeCount} admissible DOFs exist.");
        if (reduced.RedundantSupportConstraints > 0)
            warnings.Add($"Redundant support directions: {reduced.RedundantSupportConstraints}.");
        if (reduced.RemovedUnusedRotationDofs.Count > 0)
            warnings.Add($"Removed {reduced.RemovedUnusedRotationDofs.Count} unused, uncoupled massless rotations.");
        warnings.Add("Mode amplitudes are mass-normalized, not real displacements. Modal sign is arbitrary; repeated modes have non-unique bases.");
        return new StructuralModalAnalysisOutput(modes)
        {
            AvailableModeCount = spectrum.AvailableModeCount,
            MaximumMassOrthogonalityError = spectrum.MaxMassOrthogonalityError,
            MaximumStiffnessOrthogonalityError = spectrum.MaxStiffnessOrthogonalityError,
            Warnings = warnings
        };
    }
}
