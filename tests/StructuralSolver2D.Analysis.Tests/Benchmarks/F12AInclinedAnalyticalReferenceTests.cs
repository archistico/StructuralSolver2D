using System.Globalization;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// F1.2-A: first-order 2D frame checks against CLOSED-FORM beam theory and
/// global statics; no production FEM matrices, solver or sampling for expected
/// values. Rotated frame axes, local load signs, pin releases and a negative
/// manual-combination factor are covered separately from the frozen G8B set.
/// </summary>
public sealed class F12AInclinedAnalyticalReferenceTests
{
    private const double E = 210_000_000.0; // kN/m2
    private const double A = 0.003; // m2
    private const double I = 0.00002; // m4
    private const double L = 5.0; // m

    private static StructuralAnalysisOptions FiveStations() => new()
    {
        InternalForceSampleCount = 5,
        DisplacementSampleCount = 5
    };

    [Theory]
    [InlineData(0.0, -12.0, 50.0)]
    [InlineData(30.0, -12.0, 50.0)]
    [InlineData(90.0, -12.0, 50.0)]
    [InlineData(145.0, -12.0, 50.0)]
    [InlineData(-65.0, 7.5, -20.0)]
    [InlineData(-20.0, 7.5, -20.0)]
    public void InclinedCantilever_TipAxialAndTransverseForce_MatchesClosedFormAtFiveStations(
        double degrees, double pLocalY, double hLocalX)
    {
        StructuralModel model = Cantilever(degrees)
            .AddLoadCase(new StructuralLoadCase("LC", "Tip forces"));
        (double c, double s) = Direction(degrees);
        double px = c * hLocalX - s * pLocalY;
        double py = s * hLocalX + c * pLocalY;
        model.AddLoad(StructuralLoad.NodalForce("PX", "LC", "B", StructuralLoadDirection.GlobalX, px));
        model.AddLoad(StructuralLoad.NodalForce("PY", "LC", "B", StructuralLoadDirection.GlobalY, py));

        StructuralAnalysisOutput output = new StructuralSolver2DService()
            .AnalyzeLoadCase(model, "LC", FiveStations());
        CheckCantilever(output, degrees, pLocalY, hLocalX);
    }

    [Theory]
    [InlineData(0.0, 8.0)]
    [InlineData(30.0, 8.0)]
    [InlineData(-35.0, 8.0)]
    [InlineData(60.0, 3.0)]
    public void InclinedPinEndedBeam_NegativeLocalYUniformLoad_MatchesStaticsAndSimpleBeamMoments(
        double degrees, double qMagnitude)
    {
        (double c, double s) = Direction(degrees);
        StructuralModel model = Base(degrees, released: true)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("LC", "Uniform transverse"))
            .AddLoad(StructuralLoad.UniformDistributedLoad(
                "Q", "LC", "AB", StructuralLoadDirection.LocalY, -qMagnitude));

        StructuralAnalysisOutput output = new StructuralSolver2DService()
            .AnalyzeLoadCase(model, "LC", FiveStations());
        var result = output.Result;
        var start = result.GetReaction("SA");
        var end = result.GetReaction("SB");

        // Distributed resultant: (+q L sin(theta), -q L cos(theta)).
        // Hinge A carries all horizontal force; roller B takes the moment.
        double by = qMagnitude * L / (2 * c);
        double ay = qMagnitude * L * c - by;
        Close(-qMagnitude * L * s, start.Fx, 1e-6, "A.Rx [kN]");
        Close(ay, start.Fy, 1e-6, "A.Ry [kN]");
        Close(0, start.Mz, 1e-6, "A.Mz [kNm]");
        Close(0, end.Fx, 1e-6, "B.Rx [kN]");
        Close(by, end.Fy, 1e-6, "B.Ry [kN]");

        MemberEndForceResult endForce = Assert.Single(result.MemberEndForces);
        Close(0, endForce.StartMoment, 1e-6, "M1 released [kNm]");
        Close(0, endForce.EndMoment, 1e-6, "M2 released [kNm]");

        MemberInternalForceDiagram diagram = Assert.Single(output.InternalForceDiagrams);
        Assert.Equal(5, diagram.Samples.Count);
        for (int i = 0; i < diagram.Samples.Count; i++)
        {
            double x = i * L / 4;
            MemberInternalForceSample sample = diagram.Samples[i];
            // N is constant: the roller reaction has component s*By along the axis.
            Close(s * by, sample.NormalForce, 1e-6, $"N({i}/4L) [kN]");
            Close(qMagnitude * (L / 2 - x), sample.ShearForce, 1e-6, $"V({i}/4L) [kN]");
            Close(qMagnitude * x * (L - x) / 2, sample.BendingMoment, 1e-6,
                $"M({i}/4L) [kNm]");
        }
    }

    [Fact]
    public void NegativeManualFactor_ReversesTransverseMomentButRetainsAxialTension()
    {
        const double degrees = 40;
        (double c, double s) = Direction(degrees);
        const double h = 42;
        const double p = -8;
        const double axialFactor = 1.2;
        const double transverseFactor = -0.75;

        StructuralModel model = Cantilever(degrees)
            .AddLoadCase(new StructuralLoadCase("AX", "Axial action"))
            .AddLoadCase(new StructuralLoadCase("TR", "Transverse action"))
            .AddLoad(StructuralLoad.NodalForce("AX_X", "AX", "B", StructuralLoadDirection.GlobalX, h * c))
            .AddLoad(StructuralLoad.NodalForce("AX_Y", "AX", "B", StructuralLoadDirection.GlobalY, h * s))
            .AddLoad(StructuralLoad.NodalForce("TR_X", "TR", "B", StructuralLoadDirection.GlobalX, -p * s))
            .AddLoad(StructuralLoad.NodalForce("TR_Y", "TR", "B", StructuralLoadDirection.GlobalY, p * c))
            .AddLoadCombination(new StructuralLoadCombination("COMB", "Signed manual example", new[]
            {
                new StructuralLoadCombinationTerm("AX", axialFactor),
                new StructuralLoadCombinationTerm("TR", transverseFactor)
            }));

        StructuralAnalysisOutput output = new StructuralSolver2DService()
            .AnalyzeLoadCombination(model, "COMB", FiveStations());
        CheckCantilever(output, degrees, p * transverseFactor, h * axialFactor);
        Assert.True(output.InternalForceDiagrams[0].Samples[0].BendingMoment > 0,
            "The negative coefficient must reverse the signed root moment.");
    }

    [Fact]
    public void FlippingOnlyLocalTransverseAction_IsDetectedByFixedAnalyticalExpectation()
    {
        const double angle = 30;
        const double p = -12;
        (double c, double s) = Direction(angle);

        StructuralModel original = Cantilever(angle)
            .AddLoadCase(new StructuralLoadCase("LC", "Transverse"))
            .AddLoad(StructuralLoad.NodalForce("PX", "LC", "B", StructuralLoadDirection.GlobalX, -p * s))
            .AddLoad(StructuralLoad.NodalForce("PY", "LC", "B", StructuralLoadDirection.GlobalY, p * c));
        StructuralModel inverted = Cantilever(angle)
            .AddLoadCase(new StructuralLoadCase("LC", "Transverse"))
            .AddLoad(StructuralLoad.NodalForce("PX", "LC", "B", StructuralLoadDirection.GlobalX, p * s))
            .AddLoad(StructuralLoad.NodalForce("PY", "LC", "B", StructuralLoadDirection.GlobalY, -p * c));

        var service = new StructuralSolver2DService();
        double signedExpectedRootMoment = p * L;
        double originalMoment = Assert.Single(service.AnalyzeLoadCase(original, "LC", FiveStations())
            .InternalForceDiagrams).Samples[0].BendingMoment;
        double invertedMoment = Assert.Single(service.AnalyzeLoadCase(inverted, "LC", FiveStations())
            .InternalForceDiagrams).Samples[0].BendingMoment;
        Close(signedExpectedRootMoment, originalMoment, 1e-6, "Untampered root M");
        Assert.True(Math.Abs(invertedMoment - signedExpectedRootMoment) > 2 * Math.Abs(p) * L - 0.001,
            "Inverting the global components must disagree with the fixed local-Y moment oracle.");
    }

    private static void CheckCantilever(StructuralAnalysisOutput output,
        double degrees, double pLocalY, double hLocalX)
    {
        (double c, double s) = Direction(degrees);
        double axialExtension = hLocalX * L / (E * A);
        double transverseDeflection = pLocalY * L * L * L / (3 * E * I);
        double endRotation = pLocalY * L * L / (2 * E * I);
        var tip = output.Result.GetDisplacement("B");
        var fixedSupport = output.Result.GetReaction("SA");

        Close(c * axialExtension - s * transverseDeflection, tip.Ux, 1e-9, "tip Ux [m]");
        Close(s * axialExtension + c * transverseDeflection, tip.Uy, 1e-9, "tip Uy [m]");
        Close(endRotation, tip.Rz, 1e-9, "tip Rz [rad]");
        Close(-c * hLocalX + s * pLocalY, fixedSupport.Fx, 1e-6, "root Rx [kN]");
        Close(-s * hLocalX - c * pLocalY, fixedSupport.Fy, 1e-6, "root Ry [kN]");
        Close(-pLocalY * L, fixedSupport.Mz, 1e-6, "root Mz [kNm]");

        MemberInternalForceDiagram diagram = Assert.Single(output.InternalForceDiagrams);
        Assert.Equal(5, diagram.Samples.Count);
        for (int i = 0; i < diagram.Samples.Count; i++)
        {
            double x = i * L / 4;
            MemberInternalForceSample sample = diagram.Samples[i];
            Close(hLocalX, sample.NormalForce, 1e-6, $"N({i}/4L) [kN]");
            Close(-pLocalY, sample.ShearForce, 1e-6, $"V({i}/4L) [kN]");
            Close(pLocalY * (L - x), sample.BendingMoment, 1e-6, $"M({i}/4L) [kNm]");
        }
    }

    private static StructuralModel Cantilever(double degrees) =>
        Base(degrees, released: false)
            .AddSupport(StructuralSupport.Fixed("SA", "A"));

    private static StructuralModel Base(double degrees, bool released)
    {
        (double c, double s) = Direction(degrees);
        return new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", L * c, L * s))
            .AddMaterial(new StructuralMaterial("MAT", "Elastic idealization", E))
            .AddSection(new StructuralSection("SEC", "Uniform section", A, I))
            .AddMember(new StructuralMember("AB", "A", "B", "MAT", "SEC",
                MemberType.Frame2D, null, released, released));
    }

    private static (double Cosine, double Sine) Direction(double degrees)
    {
        double radians = degrees * Math.PI / 180;
        return (Math.Cos(radians), Math.Sin(radians));
    }

    private static void Close(double expected, double actual, double absoluteTolerance, string label)
    {
        double tolerance = absoluteTolerance + 1e-8 * Math.Abs(expected);
        Assert.True(double.IsFinite(actual) && Math.Abs(actual - expected) <= tolerance,
            string.Format(CultureInfo.InvariantCulture,
                "{0}: expected {1:R}, observed {2:R}, tolerance {3:R}",
                label, expected, actual, tolerance));
    }
}
