using StructuralSolver2D.Analysis.Dynamics.Assembly;
using StructuralSolver2D.Analysis.Dynamics.Reduction;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Dynamics;

public sealed class DynamicBoundaryConditionReducerTests
{
    [Fact]
    public void Cantilever_GivesExactUnconstrainedFreeEndSubmatrices()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC"))
            .AddSupport(StructuralSupport.Fixed("SA", "A"));
        var assembly = DynamicMatrixAssembler.Assemble(model, new StructuralDynamicsDefinition());
        var reduced = DynamicBoundaryConditionReducer.Reduce(model, assembly);
        Assert.Equal(DynamicReductionStatus.Ready, reduced.Status);
        Assert.True(reduced.ReadyForModalSolver);
        Assert.Equal(3, reduced.IndependentSupportConstraints);
        Assert.Equal(0, reduced.RedundantSupportConstraints);
        Assert.Equal(3, reduced.Stiffness.GetLength(0));
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
            {
                Close(assembly.Stiffness[3+i, 3+j], reduced.Stiffness[i, j]);
                Close(assembly.Mass[3+i, 3+j], reduced.Mass[i, j]);
                Close(i == j ? 1 : 0, reduced.Basis[3+i, j]);
            }
    }

    [Fact]
    public void InclinedRoller_ProjectsKAndMIntoCorrectPhysicalDirection()
    {
        var model = new StructuralModel().AddNode(new StructuralNode("A", 0, 0))
            .AddSupport(new StructuralSupport("R", "A", false, true, false,
                OrientationDegrees: 45));
        var a = Synthetic(3, new[] { 10.0, 20, 30 }, new[] { 2.0, 4, 6 });
        var reduced = DynamicBoundaryConditionReducer.Reduce(model, a);
        Assert.Equal(DynamicReductionStatus.Ready, reduced.Status);
        Assert.Equal(2, reduced.Stiffness.GetLength(0));
        double[] ux = { 1 / Math.Sqrt(2), 1 / Math.Sqrt(2), 0 };
        double[] rz = { 0, 0, 1 };
        // Reconstruct the constrained physical vectors in the orthonormal Z basis.
        Close(15, Energy(reduced, ux, mass: false));
        Close(3, Energy(reduced, ux, mass: true));
        Close(30, Energy(reduced, rz, mass: false));
        Close(6, Energy(reduced, rz, mass: true));
        for (int col = 0; col < 2; col++)
        {
            double uxActual = reduced.Basis[0,col], uyActual = reduced.Basis[1,col];
            Close(0, -uxActual / Math.Sqrt(2) + uyActual / Math.Sqrt(2));
        }
    }

    [Fact]
    public void DuplicateInclinedSupports_AreCountedButNotTreatedAsExtraFreeDOFs()
    {
        var model = new StructuralModel().AddNode(new StructuralNode("A", 0, 0))
            .AddSupport(new StructuralSupport("R1", "A", false, true, false, OrientationDegrees: 35))
            .AddSupport(new StructuralSupport("R2", "A", false, true, false, OrientationDegrees: 35))
            .AddSupport(new StructuralSupport("R3", "A", true, false, false, OrientationDegrees: 125));
        var reduced = DynamicBoundaryConditionReducer.Reduce(model,
            Synthetic(3, new[] { 10.0, 20, 30 }, new[] { 2.0, 4, 6 }));
        Assert.Equal(DynamicReductionStatus.Ready, reduced.Status);
        Assert.Equal(1, reduced.IndependentSupportConstraints);
        Assert.Equal(2, reduced.RedundantSupportConstraints);
        Assert.Equal(2, reduced.Mass.GetLength(0));
    }

    [Fact]
    public void TrussOnlyRotations_AreEliminatedWithoutArtificialMass()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("T", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(new StructuralSupport("RB", "B", false, true, false));
        var raw = DynamicMatrixAssembler.Assemble(model, new StructuralDynamicsDefinition());
        var reduced = DynamicBoundaryConditionReducer.Reduce(model, raw);
        Assert.Equal(DynamicReductionStatus.Ready, reduced.Status);
        Assert.Equal(new[] { 2, 5 }, reduced.RemovedUnusedRotationDofs);
        Assert.Equal(1, reduced.Mass.GetLength(0));
        Close(raw.Stiffness[3, 3], reduced.Stiffness[0, 0]);
        Close(raw.Mass[3, 3], reduced.Mass[0, 0]);
        Assert.Equal(0, reduced.MassNullity);
    }

    [Fact]
    public void FreeFreeFrame_IsExplicitlyDeferredAndReportsThreeRigidModes()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC"));
        var result = DynamicBoundaryConditionReducer.AssembleAndReduce(model, new StructuralDynamicsDefinition());
        Assert.Equal(DynamicReductionStatus.FreeFreeDeferred, result.Status);
        Assert.Equal("M54_FREE_FREE_DEFERRED", result.DiagnosticCode);
        Assert.Equal(3, result.EstimatedRigidBodyModes);
        Assert.Equal(3, result.StiffnessNullity);
        Assert.False(result.ReadyForModalSolver);
    }

    [Fact]
    public void UnderrestrainedTruss_IsRigidMotionNotInternalMechanism()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("T", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"));
        var result = DynamicBoundaryConditionReducer.AssembleAndReduce(model, new StructuralDynamicsDefinition());
        Assert.Equal(DynamicReductionStatus.UnrestrainedRigidBodyModes, result.Status);
        Assert.Equal(1, result.EstimatedRigidBodyModes);
        Assert.Equal(1, result.StiffnessNullity);
    }

    [Fact]
    public void BracedOpenTruss_HasInternalMechanismBeyondRigidMotions()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddNode(new StructuralNode("C", 3, 3))
            .AddMember(new StructuralMember("T1", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddMember(new StructuralMember("T2", "B", "C", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(new StructuralSupport("SC", "C", false, true, false));
        var result = DynamicBoundaryConditionReducer.AssembleAndReduce(model, new StructuralDynamicsDefinition());
        Assert.Equal(DynamicReductionStatus.Mechanism, result.Status);
        Assert.Equal(0, result.EstimatedRigidBodyModes);
        Assert.Equal(1, result.StiffnessNullity);
    }

    [Fact]
    public void EducationalLumpedFrame_DoesNotConcealStiffnessCoupledMasslessRotation()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC"))
            .AddSupport(StructuralSupport.Fixed("SA", "A"));
        var result = DynamicBoundaryConditionReducer.AssembleAndReduce(model,
            new StructuralDynamicsDefinition(), ElementMassFormulation.EducationalLumped);
        Assert.Equal(DynamicReductionStatus.MasslessCoupledDofs, result.Status);
        Assert.Equal("M54_MASSLESS_STIFFNESS_COUPLING", result.DiagnosticCode);
    }

    [Fact]
    public void UnconnectedTranslation_CannotBeDeletedAsUnusedRotation()
    {
        var model = new StructuralModel().AddNode(new StructuralNode("A", 0, 0));
        var result = DynamicBoundaryConditionReducer.Reduce(model,
            Synthetic(3, new double[3], new double[3]));
        Assert.Equal(DynamicReductionStatus.SingularMass, result.Status);
        Assert.Equal(new[] { 2 }, result.RemovedUnusedRotationDofs);
        Assert.Equal(2, result.MassNullity);
    }

    [Fact]
    public void FixedAndPinnedRigidCases_RespectPhysicalDegreesOfFreedom()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC"))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.Fixed("SB", "B"));
        var result = DynamicBoundaryConditionReducer.AssembleAndReduce(model, new StructuralDynamicsDefinition());
        Assert.Equal(DynamicReductionStatus.FullyConstrained, result.Status);
        Assert.Equal(0, result.Stiffness.GetLength(0));
    }

    [Fact]
    public void InvalidOrIndefinitePhysicalMatrices_AreDiagnosedWithoutEigenSolve()
    {
        var model = new StructuralModel().AddNode(new StructuralNode("A", 0, 0));
        var indefinite = DynamicBoundaryConditionReducer.Reduce(model,
            Synthetic(3, new[] { 1.0, -2.0, 1 }, new[] { 1.0, 1, 1 }));
        Assert.Equal(DynamicReductionStatus.IndefiniteStiffness, indefinite.Status);
        var indefiniteMass = DynamicBoundaryConditionReducer.Reduce(model,
            Synthetic(3, new[] { 1.0, 2.0, 1 }, new[] { 1.0, -1, 1 }));
        Assert.Equal(DynamicReductionStatus.IndefiniteMass, indefiniteMass.Status);
        var invalid = DynamicBoundaryConditionReducer.Reduce(model,
            Synthetic(3, new[] { double.NaN, 2.0, 1 }, new[] { 1.0, 1, 1 }));
        Assert.Equal(DynamicReductionStatus.InvalidMatrices, invalid.Status);
    }

    [Fact]
    public void NearSingularPositiveDefiniteMassAndStiffness_UseStableWarningCodes()
    {
        var model = new StructuralModel().AddNode(new StructuralNode("A", 0, 0));
        var weakMass = Synthetic(3, new[] { 1.0, 1, 1 }, new[] { 1.0, 1, 1 });
        weakMass.Mass[0,1] = weakMass.Mass[1,0] = 1 - 2e-10;
        var mResult = DynamicBoundaryConditionReducer.Reduce(model, weakMass);
        Assert.Equal(DynamicReductionStatus.MassNearSingular, mResult.Status);
        Assert.Equal("M54_MASS_NEAR_SINGULAR", mResult.DiagnosticCode);

        var weakStiffness = Synthetic(3, new[] { 1.0, 1, 1 }, new[] { 1.0, 1, 1 });
        weakStiffness.Stiffness[0,1] = weakStiffness.Stiffness[1,0] = 1 - 2e-10;
        var kResult = DynamicBoundaryConditionReducer.Reduce(model, weakStiffness);
        Assert.Equal(DynamicReductionStatus.StiffnessNearSingular, kResult.Status);
        Assert.Equal("M54_STIFFNESS_NEAR_SINGULAR", kResult.DiagnosticCode);
    }

    [Fact]
    public void SingularMassWithPositiveDiagonals_IsStillRejected()
    {
        var model = new StructuralModel().AddNode(new StructuralNode("A", 0, 0));
        var system = Synthetic(3, new[] { 1.0, 2, 3 }, new[] { 1.0, 1, 1 });
        system.Mass[0,1] = system.Mass[1,0] = 1;
        var result = DynamicBoundaryConditionReducer.Reduce(model, system);
        Assert.Equal(DynamicReductionStatus.SingularMass, result.Status);
        Assert.Equal(1, result.MassNullity);
    }

    [Fact]
    public void ProjectionPreservesKineticAndStrainEnergyForAnyAdmissibleVector()
    {
        var model = Base().AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 3))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC"))
            .AddSupport(new StructuralSupport("SA", "A", true, false, true, OrientationDegrees: 27))
            .AddSupport(new StructuralSupport("SB", "B", false, true, false, OrientationDegrees: -15));
        var raw = DynamicMatrixAssembler.Assemble(model, new StructuralDynamicsDefinition());
        var result = DynamicBoundaryConditionReducer.Reduce(model, raw);
        Assert.Equal(DynamicReductionStatus.Ready, result.Status);
        double[] q = Enumerable.Range(0, result.Basis.GetLength(1)).Select(i => (i+1)*0.13).ToArray();
        var full = new double[raw.Stiffness.GetLength(0)];
        for (int i = 0; i < full.Length; i++)
            for (int j = 0; j < q.Length; j++) full[i] += result.Basis[i, j] * q[j];
        Close(Quadratic(raw.Stiffness, full), Quadratic(result.Stiffness, q));
        Close(Quadratic(raw.Mass, full), Quadratic(result.Mass, q));
    }

    private static StructuralModel Base() => new StructuralModel()
        .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 77))
        .AddSection(new StructuralSection("SEC", "Section", 0.02, 0.0001));

    private static DynamicMatrixAssembly Synthetic(int n, double[] k, double[] m)
    {
        var K = new double[n,n]; var M = new double[n,n];
        for (int i = 0; i < n; i++) { K[i,i] = k[i]; M[i,i] = m[i]; }
        return new(K, M, new Dictionary<string, int> { ["A"] = 0 },
            Array.Empty<int>(), 0, 0);
    }
    private static double Energy(DynamicReductionResult r, double[] full, bool mass)
    {
        int n = r.Basis.GetLength(1);
        var q = new double[n];
        for (int col = 0; col < n; col++)
            for (int row = 0; row < full.Length; row++) q[col] += r.Basis[row,col] * full[row];
        return Quadratic(mass ? r.Mass : r.Stiffness, q);
    }
    private static double Quadratic(double[,] m, double[] v)
    {
        double result = 0;
        for (int i = 0; i < v.Length; i++)
            for (int j = 0; j < v.Length; j++) result += v[i]*m[i,j]*v[j];
        return result;
    }
    private static void Close(double expected, double actual)
    {
        double tol = 1e-8 * Math.Max(1, Math.Abs(expected));
        Assert.True(double.IsFinite(actual) && Math.Abs(expected-actual) <= tol,
            $"Expected {expected:R}; actual {actual:R}; tolerance {tol:R}");
    }
}
