using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Rules;
using StructuralSolver2D.Verification.Timber;

namespace StructuralSolver2D.Verification.Tests.M92;

public sealed class Ntc2018TimberStabilityG4Tests
{
    private static readonly Ntc2018TimberCoefficientTablesG1 Tables =
        Ntc2018TimberCoefficientTablesG1.Load(Path.Combine(AppContext.BaseDirectory,
            "ntc2018-timber-coefficients-g1.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory,
            "characteristic-strength-classes.json"));
    private static readonly Ntc2018TimberCoefficientSelectorG1 Selector = new(Tables, Materials);

    private static Ntc2018TimberSelectedCoefficientsG1 Select(string grade = "C24",
        Ntc2018GammaColumnG1 column = Ntc2018GammaColumnG1.A,
        int service = 1, bool cracks = true,
        Ntc2018TimberSituationG1 situation = Ntc2018TimberSituationG1.FundamentalStr)
    {
        var assignment = new TimberRectangularAssignmentM92A(
            "W1", "RECT-100x200", grade, 100, 200, "TEST measured rectangle 100x200 mm",
            service, null, "TEST room environment", null, "AN-NTC-G2", "ULS-G2",
            DesignEffectTargetKind.LoadCombination, true, true, true, true, true);
        var exceptional = situation == Ntc2018TimberSituationG1.Exceptional;
        var actions = exceptional
            ? new[]
            {
                new Ntc2018CombinationActionG1("G", Ntc2018ActionCategoryG1.G1StructuralPermanent,
                    TimberLoadDurationM92A.Permanent, 1.0, "TEST G duration", "TEST G factor"),
                new Ntc2018CombinationActionG1("A", Ntc2018ActionCategoryG1.Exceptional,
                    TimberLoadDurationM92A.Instantaneous, 1.0, "TEST A duration", "TEST A factor")
            }
            : new[]
            {
                new Ntc2018CombinationActionG1("G", Ntc2018ActionCategoryG1.G1StructuralPermanent,
                    TimberLoadDurationM92A.Permanent, 1.3, "TEST G duration", "TEST G factor"),
                new Ntc2018CombinationActionG1("Q", Ntc2018ActionCategoryG1.QVariable,
                    TimberLoadDurationM92A.MediumTerm, 1.5, "TEST Q duration", "TEST Q factor")
            };
        var production = column == Ntc2018GammaColumnG1.B
            ? new Ntc2018ColumnBProductionEvidenceG1(true, true, true,
                "TEST certification", "TEST controlled production", 0.12, "TEST coefficient of variation")
            : null;
        var input = new Ntc2018TimberSelectionG1(
            Ntc2018TimberCoefficientSelectorG1.Edition, assignment, situation,
            "TEST applicable situation", column, production, actions, true,
            "TEST exhaustive manifest", Ntc2018TimberCoefficientSelectorG1.Fingerprint(
                assignment.AnalysisId, assignment.TargetId, actions),
            "TEST externally compared G2 FEM combination", "TEST NTC class and duration evidence",
            "TEST independently identified timber grade", cracks, "TEST crack-width choice");
        return Selector.Select(input);
    }

    private static Ntc2018TimberSectionEvidenceG2 Proof(
        Ntc2018TimberSelectedCoefficientsG1? s = null,
        double? net = 20000,
        bool? intact = true) =>
        new(s ?? Select(), "TEST independent proof: prismatic solid timber rectangular member",
            true, true, true, intact, true,
            net, "TEST measured net section including all holes",
            true, "TEST in-plane shear parallel to cross-section");



    private static Ntc2018TimberStabilityEvidenceG4 ProofG4(
        Ntc2018TimberSectionEvidenceG2? section = null,
        double? strongLength = 3, double? weakLength = 3, double? critStress = 12,
        bool? fullConcentric = true, bool? twoLengths = true, bool? restraints = true,
        bool? stiffness = true, bool? maxMoment = true, bool? shearCheck = true,
        bool? outOfPlane = true, bool? straight = true) =>
        new(section ?? Proof(), 2, DesignEffectSectionSide.Continuous,
            "TEST verified FEM section x=2 m", true, "TEST intact single prismatic member",
            straight, "TEST independent E0,05 and EN straightness <= L/300 solid / L/500 glulam",
            outOfPlane, "TEST no lateral N/V/M outside FEM plane",
            strongLength, weakLength, twoLengths, "TEST independent effective length buckling y and z restrained ends",
            fullConcentric, "TEST constant concentric pure N throughout complete member",
            1, 3, critStress, restraints, "TEST verified torsional end restraints x=1 and x=3 m",
            stiffness, "TEST external sigma_mcrit from 5% elastic/torsional properties",
            maxMoment, "TEST segment governing M exactly at x=2 m",
            shearCheck, "TEST separate NTC shear resistance with exact concomitant V");

    private static DesignCheckReport Check(Ntc2018TimberStabilityEvidenceG4 p,
        Ntc2018TimberStabilityEffectG4 effect, double n, double v, double m,
        double position = 2, string member = "W1", string combination = "ULS-G2",
        double memberLength = 4, NormativeProfile? profile = null,
        DesignEffectSectionSide side = DesignEffectSectionSide.Continuous,
        string section = "RECT-100x200", DesignEffectOrigin origin = DesignEffectOrigin.FemAnalysis)
    {
        var rule = new Ntc2018TimberStabilityRuleG4(effect, Materials, Tables, new[] { p });
        var request = new DesignCheckRequest(new DesignMemberContext(member, DesignMaterial.Timber,
                section, memberLength), DesignLimitState.Ultimate,
            profile ?? p.Section.Selection.Profile,
            new DesignEffectSnapshot(p.Section.Selection.Assignment.AnalysisId, combination, origin,
                position, n, v, m)
            { MemberId = member, TargetKind = DesignEffectTargetKind.LoadCombination, SectionSide = side },
            new[] { rule.RuleId });
        return new DesignCheckEngine(new[] { rule }).Evaluate(request);
    }

    private static DesignCheckResult One(Ntc2018TimberStabilityEvidenceG4 p,
        Ntc2018TimberStabilityEffectG4 effect, double n, double v, double m,
        double position = 2, string member = "W1", string combination = "ULS-G2",
        double memberLength = 4, NormativeProfile? profile = null,
        DesignEffectSectionSide side = DesignEffectSectionSide.Continuous,
        string section = "RECT-100x200", DesignEffectOrigin origin = DesignEffectOrigin.FemAnalysis) =>
        Assert.Single(Check(p, effect, n, v, m, position, member, combination,
            memberLength, profile, side, section, origin).Results);

    private const Ntc2018TimberStabilityEffectG4 Column = Ntc2018TimberStabilityEffectG4.PureAxialColumn;
    private const Ntc2018TimberStabilityEffectG4 Beam = Ntc2018TimberStabilityEffectG4.StrongAxisBendingBeam;

    [Fact]
    public void IndependentC24TwoAxisColumnGolden_NonPassingCompressionBucklingIsFailed()
    {
        var r = One(ProofG4(), Column, -100, 0, 0);
        double fcD = 21.0 * .8 / 1.5;
        double ly = 3000.0 / (200.0 / Math.Sqrt(12)) / Math.PI * Math.Sqrt(21.0 / 7400.0);
        double lz = 3000.0 / (100.0 / Math.Sqrt(12)) / Math.PI * Math.Sqrt(21.0 / 7400.0);
        static double IndependentK(double lambda)
        {
            var z = .5 * (1 + .2 * (lambda - .3) + lambda * lambda);
            return 1 / (z + Math.Sqrt(z * z - lambda * lambda));
        }
        double expected = 5 / (IndependentK(lz) * fcD);
        Assert.True(IndependentK(lz) < IndependentK(ly));
        Assert.Equal(DesignCheckStatus.Failed, r.Status);
        Assert.Equal(1.568802318624587, Assert.IsType<double>(r.Utilization), 10);
        Assert.Equal(expected, Assert.IsType<double>(r.Utilization), 10);
        Assert.Contains("lambda_rel_y=", r.CalculationTrace);
        Assert.Contains("lambda_rel_z=", r.CalculationTrace);
        Assert.Contains("E0,05=7400", r.CalculationTrace);
        Assert.Contains("NOT VERIFIED:", r.CalculationTrace);
    }

    [Fact]
    public void C24ShortColumnReductionIsSmallerAndLocallyPasses()
    {
        var r = One(ProofG4(strongLength: .7, weakLength: .7), Column, -100, 0, 0);
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
        Assert.True(Assert.IsType<double>(r.Utilization) < 1);
        Assert.False(new Ntc2018TimberStabilityRuleG4(Column, Materials, Tables,
            new[] { ProofG4() }).EstablishesCompleteMemberCompliance);
    }

    [Fact]
    public void GlulamImperfectionFactorAndCharacteristicModulusAreDistinct()
    {
        var r = One(ProofG4(section: Proof(Select("GL24h"))), Column, -100, 0, 0);
        Assert.Contains("beta_c=", r.CalculationTrace);
        Assert.Contains("E0,05=9600", r.CalculationTrace);
        Assert.NotEqual(DesignCheckStatus.MissingData, r.Status);
    }

    [Fact]
    public void C24BeamCriticalStressTwelveMpa_IndependentLtbRatio()
    {
        var r = One(ProofG4(), Beam, 0, 0, 3);
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
        Assert.Equal(.703125, Assert.IsType<double>(r.Utilization), 10);
        Assert.Contains("sigma_m_crit=12", r.CalculationTrace);
        Assert.Contains("kcrit_m=", r.CalculationTrace);
        Assert.Contains("NOT VERIFIED:", r.CalculationTrace);
    }

    [Fact]
    public void StrongAxisLtbFailsForHighMoment_RegardlessOfValidCriticalStress()
    {
        var r = One(ProofG4(), Beam, 0, 0, 5);
        Assert.Equal(DesignCheckStatus.Failed, r.Status);
        Assert.Equal(1.171875, Assert.IsType<double>(r.Utilization), 10);
    }

    [Fact]
    public void BeamCanHaveConcomitantShearOnlyWithIndependentShearVerification()
    {
        Assert.Equal(DesignCheckStatus.Passed, One(ProofG4(), Beam, 0, 10, -3).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(shearCheck: null), Beam, 0, 10, -3).Status);
        Assert.Equal(DesignCheckStatus.Passed,
            One(ProofG4(shearCheck: null), Beam, 0, 0, 3).Status);
    }

    [Theory]
    [InlineData(.3, .2, 1.0)]
    [InlineData(.3, .1, 1.0)]
    [InlineData(.2, .2, 1.0)]
    public void ColumnShortSlendernessBoundaryHasUnity(double lambda, double beta, double expected) =>
        Assert.Equal(expected, Ntc2018TimberStabilityRuleG4.ColumnReduction(lambda, beta), 12);

    [Fact]
    public void ColumnAbovePointThreeAndGlulamHaveCorrectContinuousReductions()
    {
        var solid = Ntc2018TimberStabilityRuleG4.ColumnReduction(1.2, .2);
        var glued = Ntc2018TimberStabilityRuleG4.ColumnReduction(1.2, .1);
        Assert.InRange(solid, 0, glued);
        Assert.True(glued < 1);
        Assert.True(Ntc2018TimberStabilityRuleG4.ColumnReduction(.300001, .2) < 1);
    }

    [Theory]
    [InlineData(.5, 1.0)]
    [InlineData(.75, 1.0)]
    [InlineData(1.0, .81)]
    [InlineData(1.4, .51)]
    [InlineData(2.0, .25)]
    public void BeamKcritPiecewiseMatchesNtc412(double lambda, double expected) =>
        Assert.Equal(expected, Ntc2018TimberStabilityRuleG4.BeamReduction(lambda), 12);

    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidSlendernessIsRejected(double lambda)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Ntc2018TimberStabilityRuleG4.BeamReduction(lambda));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Ntc2018TimberStabilityRuleG4.ColumnReduction(lambda, .2));
    }

    [Theory]
    [InlineData(-100, 0, 1)]
    [InlineData(-100, 1, 0)]
    [InlineData(100, 0, 0)]
    [InlineData(0, 0, 0)]
    public void ColumnMixedOrWrongSignedActionsAreNotPureStabilityPass(double n, double v, double m) =>
        Assert.Equal(DesignCheckStatus.NotApplicable, One(ProofG4(), Column, n, v, m).Status);

    [Theory]
    [InlineData(10, 0, 2)]
    [InlineData(-10, 0, 2)]
    [InlineData(0, 0, 0)]
    public void BeamAxialInteractionCannotBeApproximatedByPureLtb(double n, double v, double m) =>
        Assert.Equal(DesignCheckStatus.NotApplicable, One(ProofG4(), Beam, n, v, m).Status);

    [Fact]
    public void MissingWeakLengthIsNotSilentlyInferredFromPlanarFem()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(weakLength: null), Column, -40, 0, 0).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(twoLengths: false), Column, -40, 0, 0).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(fullConcentric: null), Column, -40, 0, 0).Status);
    }

    [Fact]
    public void BeamUnverifiedLateralBracingCriticalStressOrMaxMomentCannotPass()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(restraints: null), Beam, 0, 0, 2).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(critStress: null), Beam, 0, 0, 2).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(stiffness: false), Beam, 0, 0, 2).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(maxMoment: null), Beam, 0, 0, 2).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(critStress: double.NaN), Beam, 0, 0, 2).Status);
    }

    [Fact]
    public void OutOfPlaneAndStraightnessAreExternallyRequired()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(outOfPlane: null), Column, -20, 0, 0).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(ProofG4(straight: false), Beam, 0, 0, 3).Status);
    }

    [Fact]
    public void WrongMemberCombinationStationOrSideCannotReuseStabilityEvidence()
    {
        var p = ProofG4();
        Assert.Equal(DesignCheckStatus.MissingData,
            One(p, Column, -20, 0, 0, member: "W2").Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(p, Column, -20, 0, 0, combination: "ULS-OTHER").Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(p, Column, -20, 0, 0, position: 2.0000000001).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(p, Column, -20, 0, 0, side: DesignEffectSectionSide.AfterPointLoad).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            One(p, Column, -20, 0, 0, section: "OTHER").Status);
    }

    [Fact]
    public void FalseOrUnknownCriticalSegmentAndNonFemProvenanceDoNotPass()
    {
        var proof = ProofG4() with { SegmentStartMetres = 3, SegmentEndMetres = 4 };
        Assert.Equal(DesignCheckStatus.MissingData, One(proof, Beam, 0, 0, 3).Status);
        Assert.Equal(DesignCheckStatus.MissingData, One(ProofG4(), Column, -10, 0, 0,
            origin: DesignEffectOrigin.ExternalManual).Status);
        Assert.Equal(DesignCheckStatus.MissingData, One(ProofG4(), Beam, 0, 0, 3,
            memberLength: 2).Status);
    }

    [Fact]
    public void WrongProfileOrForgedNtcCoefficientsCannotBecomeCompliant()
    {
        var proof = ProofG4();
        var forged = proof.Section.Selection.Profile with
        {
            Coefficients = proof.Section.Selection.Profile.Coefficients.Select(c =>
                c.Id == TimberRectangularSectionRuleM92A.GammaId ? c with { Value = .2 } : c).ToArray()
        };
        Assert.NotEqual(DesignCheckStatus.Passed,
            One(proof, Column, -10, 0, 0, profile: forged).Status);
        Assert.False(new Ntc2018TimberStabilityRuleG4(Beam, Materials, Tables, new[] { proof })
            .EstablishesCompleteMemberCompliance);
    }

    [Fact]
    public void ExceptionalGammaOneWithInstantaneousKmodIsExplicit()
    {
        var r = One(ProofG4(section: Proof(Select(situation: Ntc2018TimberSituationG1.Exceptional,
            column: Ntc2018GammaColumnG1.NotApplicableExceptional))), Beam, 0, 0, 3);
        Assert.Equal(4.5 / (.5 * (1.1 * 24.0)), Assert.IsType<double>(r.Utilization), 10);
        Assert.Contains("gammaM=1", r.CalculationTrace);
    }
}
