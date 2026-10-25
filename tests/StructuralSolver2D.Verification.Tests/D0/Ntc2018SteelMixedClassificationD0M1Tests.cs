using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D0;

public sealed class Ntc2018SteelMixedClassificationD0M1Tests
{
    private static readonly CommercialRolledSteelCatalogM88B2 Sections =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    private static DesignCheckRequest Request(double n = -100, double m = 25, double v = 0,
        string section = "IPE-200", NormativeProfile? profile = null) => new(
        new DesignMemberContext("M1", DesignMaterial.Steel, section, 4),
        DesignLimitState.Ultimate, profile ?? Ntc2018SteelTensionProfileD1.Create(),
        new DesignEffectSnapshot("FEM-A1", "ULS-1", DesignEffectOrigin.FemAnalysis,
            2, n, v, m) { MemberId = "M1", TargetKind = DesignEffectTargetKind.LoadCombination,
                         SectionSide = DesignEffectSectionSide.Continuous },
        Array.Empty<string>());

    private static Ntc2018SteelMixedClassificationEvidenceD0M1 Evidence() => new(
        UnperforatedConfirmed: true,
        HotRolledParallelFlangeDoublySymmetricConfirmed: true,
        StrongAxisMappedToFemMomentConfirmed: true,
        GeometryEvidence: "Designer-declared shop drawing; geometry independently checked",
        GradeAndYieldEvidence: "Declared steel grade/product evidence; M88-C strength band");

    private static Ntc2018SteelMixedStressParametersD0M1 Stress(double alpha = 0.6,
        double psi = -0.5) => new(alpha, psi,
            "Independently computed plastic stress-block alpha under this N+M",
            "Independently computed elastic end-web stress psi under this N+M",
            ConsistentWithSameFemActionConfirmed: true,
            SameActionAndStationEvidence: "Checked against signed N/M at M1, FEM-A1, ULS-1, x=2, continuous side");

    private static Ntc2018SteelMixedIClassificationD0M1 Geometry(
        Ntc2018SteelISectionDimensionsD0 dims,
        Ntc2018SteelMixedStressParametersD0M1? stress = null,
        Ntc2018SteelMixedClassificationEvidenceD0M1? evidence = null,
        DesignCheckRequest? request = null, double fy = 235) =>
        Ntc2018SteelMixedIClassifierD0M1.ClassifyGeometry(request ?? Request(), dims,
            "S235", fy, evidence ?? Evidence(), stress ?? Stress());

    private static Ntc2018SteelISectionDimensionsD0 WebRatio(double cOverT) =>
        // tw=10, tf=8, r=4 => h = 24 + c/t*10; cFlange=(100-10-8)/2.
        new(24 + 10*cOverT, 100, 10, 8, 4);

    [Fact]
    public void StrongAxisMixed_NtcTableBranchAlphaAboveHalfAndPsiAboveMinusOne()
    {
        var c = Geometry(WebRatio(70));
        Assert.Equal("NTC2018", c.Code.EditionId);
        Assert.Equal(0.6, c.Alpha);
        Assert.Equal(-0.5, c.Psi);
        Assert.Equal(396d / (13 * 0.6 - 1), c.Web.Class1Limit, 11);
        Assert.Equal(456d / (13 * 0.6 - 1), c.Web.Class2Limit, 11);
        Assert.Equal(42d / (0.67 + 0.33 * -0.5), c.Web.Class3Limit, 11);
        Assert.Equal(3, c.Web.Class);
        Assert.Equal(1, c.Flange.Class);
        Assert.Equal(3, c.Class);
        Assert.Contains("FEM-A1/ULS-1 x=2", c.Trace);
        Assert.Contains("alpha=", c.Trace);
        Assert.Contains("psi=", c.Trace);
        Assert.False(c.ProvidesResistanceOrNmInteractionCheck);
        Assert.False(c.EstablishesCompleteMemberCompliance);
    }

    [Theory]
    [InlineData(55, 1)]
    [InlineData(60, 2)]
    [InlineData(70, 3)]
    [InlineData(90, 4)]
    public void AlphaAboveHalf_CorrectWebClassBranches(double cOverT, int expected)
    {
        var c = Geometry(WebRatio(cOverT));
        Assert.Equal(expected, c.Web.Class);
        Assert.Equal(expected, c.Class);
        Assert.Equal(expected == 4, c.RequiresEffectiveSectionProperties);
    }

    [Fact]
    public void AlphaExactlyHalf_UsesSecondPlasticBranchAndDoesNotReusePureBending()
    {
        var c = Geometry(WebRatio(80), Stress(0.5, -0.6));
        Assert.Equal(72d, c.Web.Class1Limit, 11);
        Assert.Equal(83d, c.Web.Class2Limit, 11);
        Assert.Equal(42d / (0.67 - 0.198), c.Web.Class3Limit, 11);
        Assert.Equal(2, c.Web.Class);
    }

    [Theory]
    [InlineData(70, 1)]
    [InlineData(76, 2)]
    [InlineData(85, 3)]
    [InlineData(90, 4)]
    public void AlphaAtHalf_SecondPlasticBranchCoversEveryClass(double ratio, int expected)
    {
        Assert.Equal(expected, Geometry(WebRatio(ratio), Stress(0.5, -0.6)).Class);
    }

    [Fact]
    public void BoundaryClass1AndClass2AreInclusive()
    {
        // Exactly representable NTC thresholds at alpha=.5 (left branch).
        var e = Stress(0.5, -0.6);
        Assert.Equal(1, Geometry(WebRatio(72), e).Web.Class);
        Assert.Equal(2, Geometry(WebRatio(83), e).Web.Class);
        Assert.Equal(2, Geometry(WebRatio(72.00001), e).Web.Class);
        Assert.Equal(3, Geometry(WebRatio(83.00001), e).Web.Class);
    }

    [Fact]
    public void CatalogUsesSteelThicknessBandAndOnlyParallelFlangeIh()
    {
        var c = Ntc2018SteelMixedIClassifierD0M1.ClassifyCatalogProfile(
            Request(), Sections, Materials, "S235", Evidence(), Stress());
        Assert.Equal("IPE-200", c.SectionId);
        Assert.Equal(235d, c.YieldStrengthMpa);
        Assert.Contains("OPPO=", c.Trace);
        Assert.Contains("NOT CERTIFIED", c.Trace);
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedIClassifierD0M1.ClassifyCatalogProfile(
                Request(section: "INP-200"), Sections, Materials, "S235", Evidence(), Stress()));
    }

    [Fact]
    public void PairedStressEvidenceMustBeConsistentAndComplete()
    {
        var d = WebRatio(70);
        Assert.Throws<NotSupportedException>(() => Geometry(d,
            Stress() with { PlasticStressFieldEvidence = "" }));
        Assert.Throws<NotSupportedException>(() => Geometry(d,
            Stress() with { ElasticStressFieldEvidence = null }));
        Assert.Throws<NotSupportedException>(() => Geometry(d,
            Stress() with { ConsistentWithSameFemActionConfirmed = false }));
        Assert.Throws<NotSupportedException>(() => Geometry(d,
            Stress() with { SameActionAndStationEvidence = "" }));
        Assert.Throws<NotSupportedException>(() => Geometry(d,
            evidence: Evidence() with { UnperforatedConfirmed = null }));
        Assert.Throws<NotSupportedException>(() => Geometry(d,
            evidence: Evidence() with { StrongAxisMappedToFemMomentConfirmed = false }));
    }

    [Theory]
    [InlineData(0.0, -0.5)]
    [InlineData(1.0, -0.5)]
    [InlineData(0.6, -1.0)]
    [InlineData(0.6, -1.5)]
    [InlineData(0.6, 1.0)]
    [InlineData(double.NaN, -0.5)]
    [InlineData(0.6, double.PositiveInfinity)]
    public void UnsupportedAlphaPsiDomainsAreNotExtrapolated(double alpha, double psi)
    {
        Assert.Throws<NotSupportedException>(() => Geometry(WebRatio(70), Stress(alpha,psi)));
    }

    [Fact]
    public void NonOrderedClassThresholdsMustFailClosed()
    {
        // Contradictory independently declared stress fields: class3< class2.
        Assert.Throws<NotSupportedException>(() =>
            Geometry(WebRatio(70), Stress(alpha: 0.6, psi: 0.8)));
    }

    [Theory]
    [InlineData(100, 25, 0)]   // tension, not compression
    [InlineData(0, 25, 0)]     // pure bending handled by D0, not D0M1
    [InlineData(-100, 0, 0)]   // pure compression handled by D0
    [InlineData(-100, 25, 0.001)] // concurrent shear excluded
    public void NotAConcomitantPureNmActionIsRejected(double n, double m, double v)
    {
        Assert.Throws<NotSupportedException>(() => Geometry(WebRatio(70), request: Request(n,m,v)));
    }

    [Fact]
    public void MissingFemProvenanceAndWrongNormativeIdentityAreRejected()
    {
        var r = Request();
        Assert.Throws<ArgumentException>(() => Geometry(WebRatio(70), request: r with
        { Effects = r.Effects with { MemberId = "WRONG" } }));
        Assert.Throws<ArgumentException>(() => Geometry(WebRatio(70), request: r with
        { Effects = r.Effects with { SectionSide = null } }));
        Assert.Throws<ArgumentException>(() => Geometry(WebRatio(70), request: r with
        { Effects = r.Effects with { TargetKind = DesignEffectTargetKind.LoadCase } }));
        Assert.Throws<NotSupportedException>(() => Geometry(WebRatio(70), request: Request(
            profile: Ntc2018SteelTensionProfileD1.Create() with
            { Code = new("EN 1993-1-1", "2005+A1:2014", "IT") })));
    }

    [Fact]
    public void InvalidGeometryOrMaterialCannotBeUsed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Geometry(WebRatio(70), fy: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Geometry(WebRatio(70), fy: 0));
        Assert.Throws<ArgumentException>(() => Geometry(new(30,100,10,20,12)));
    }
}
