namespace StructuralSolver2D.Analysis.PublicApi;

// M68 opt-in, small-displacement/small-strain research-only contracts.
// kN, m, s; E, sigmaY, H in kN/m²; mass per length in kN*s²/m².
public sealed record NonlinearTruss2DNode(string Id, double X, double Y);
public sealed record NonlinearTruss2DMember(string Id, string StartNodeId, string EndNodeId,
    double Area, double ElasticModulus, double YieldStress, double HardeningModulus,
    double? MassPerLength = null);
public sealed record NonlinearTruss2DSupport(string NodeId, double NormalX, double NormalY);
public sealed record NonlinearTruss2DLoad(string NodeId, double Fx, double Fy);
public sealed record NonlinearTruss2DModel(
    IReadOnlyList<NonlinearTruss2DNode> Nodes,
    IReadOnlyList<NonlinearTruss2DMember> Members,
    IReadOnlyList<NonlinearTruss2DSupport> Supports,
    IReadOnlyList<NonlinearTruss2DLoad> Loads);

public sealed record StructuralNonlinearStaticRequest(
    NonlinearTruss2DModel Model,
    IReadOnlyList<double> LoadFactors,
    int MaxIterations = 35,
    double AbsoluteForceTolerance = 1e-9,
    double RelativeForceTolerance = 1e-9);

public sealed record StructuralNonlinearTimeRequest(
    NonlinearTruss2DModel Model,
    IReadOnlyList<double> LoadFactors,
    double TimeStep,
    double MassDamping = 0,
    double InitialStiffnessDamping = 0,
    IReadOnlyList<double>? InitialVelocity = null,
    int MaximumNewtonIterations = 30,
    double AbsoluteForceTolerance = 1e-9,
    double RelativeForceTolerance = 1e-9);

public sealed record StructuralNonlinearMaterialState(double PlasticStrain, double AccumulatedPlasticStrain);

public sealed record StructuralNonlinearStaticStep(
    int Index, double LoadFactor, int NewtonIterations, double ResidualNorm,
    IReadOnlyList<double> Displacements, IReadOnlyList<double> Reactions,
    IReadOnlyList<double> AxialForces, IReadOnlyList<StructuralNonlinearMaterialState> MaterialStates,
    double IncrementalPlasticDissipation, double CumulativePlasticDissipation);

public sealed record StructuralNonlinearStaticOutput(
    bool Completed, string? Failure, IReadOnlyList<string> NodeIds, IReadOnlyList<string> MemberIds,
    IReadOnlyList<StructuralNonlinearStaticStep> Steps,
    IReadOnlyList<StructuralNonlinearMaterialState> FinalStates);

public sealed record StructuralNonlinearTimeStep(
    int Index, double Time, double LoadFactor, int NewtonIterations, double ReducedResidualNorm,
    IReadOnlyList<double> Displacements, IReadOnlyList<double> Velocities, IReadOnlyList<double> Accelerations,
    IReadOnlyList<double> Reactions, IReadOnlyList<double> AxialForces,
    IReadOnlyList<StructuralNonlinearMaterialState> MaterialStates,
    double KineticEnergy, double RecoverableEnergy, double HardeningEnergy,
    double CumulativePlasticDissipation, double CumulativeViscousDissipation,
    double CumulativeExternalWork, double CumulativeInternalTrapezoidWork,
    double DiscreteWorkResidual, double PhysicalEnergyResidual);

public sealed record StructuralNonlinearTimeOutput(
    bool Completed, string? Failure, IReadOnlyList<string> NodeIds, IReadOnlyList<string> MemberIds,
    IReadOnlyList<StructuralNonlinearTimeStep> Steps,
    IReadOnlyList<StructuralNonlinearMaterialState> FinalStates);
