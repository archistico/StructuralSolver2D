using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Timber;

namespace StructuralSolver2D.Verification.Tests.M92;

public sealed class Ntc2018TimberRectangularSectionG2Tests
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

    private static DesignCheckRequest Request(Ntc2018TimberSectionEvidenceG2 p,
        Ntc2018TimberPureEffectG2 effect, double n, double v, double m,
        NormativeProfile? profile = null, string? member = null,
        string? combo = null, DesignEffectSectionSide? side = DesignEffectSectionSide.Continuous,
        string? section = null)
    {
        var s = p.Selection;
        return new DesignCheckRequest(
            new DesignMemberContext(member ?? "W1", DesignMaterial.Timber,
                section ?? "RECT-100x200", 4),
            DesignLimitState.Ultimate, profile ?? s.Profile,
            new DesignEffectSnapshot(s.Assignment.AnalysisId, combo ?? s.Assignment.TargetId,
                DesignEffectOrigin.FemAnalysis, 2, n, v, m)
            {
                MemberId = member ?? "W1", TargetKind = DesignEffectTargetKind.LoadCombination,
                SectionSide = side
            }, new[] { Id(effect) });
    }

    private static string Id(Ntc2018TimberPureEffectG2 effect) => effect switch
    {
        Ntc2018TimberPureEffectG2.TensionParallel => Ntc2018TimberRectangularSectionRuleG2.TensionId,
        Ntc2018TimberPureEffectG2.CompressionParallel => Ntc2018TimberRectangularSectionRuleG2.CompressionId,
        Ntc2018TimberPureEffectG2.StrongAxisBending => Ntc2018TimberRectangularSectionRuleG2.BendingId,
        Ntc2018TimberPureEffectG2.InPlaneShear => Ntc2018TimberRectangularSectionRuleG2.ShearId,
        _ => throw new ArgumentOutOfRangeException(nameof(effect))
    };

    private static DesignCheckReport Check(Ntc2018TimberSectionEvidenceG2 proof,
        Ntc2018TimberPureEffectG2 effect, double n, double v, double m,
        NormativeProfile? profile = null, string? member = null,
        string? combo = null, DesignEffectSectionSide? side = DesignEffectSectionSide.Continuous,
        string? section = null) =>
        new DesignCheckEngine(new[]
        {
            new Ntc2018TimberRectangularSectionRuleG2(effect, Materials, Tables, new[] { proof })
        }).Evaluate(Request(proof, effect, n, v, m, profile, member, combo, side, section));

    [Theory]
    [InlineData(Ntc2018TimberPureEffectG2.TensionParallel, 100, 0, 0, 5, 14.5 * 0.8 / 1.5)]
    [InlineData(Ntc2018TimberPureEffectG2.CompressionParallel, -100, 0, 0, 5, 21 * 0.8 / 1.5)]
    [InlineData(Ntc2018TimberPureEffectG2.StrongAxisBending, 0, 0, 6, 9, 24 * 0.8 / 1.5)]
    [InlineData(Ntc2018TimberPureEffectG2.InPlaneShear, 0, 10, 0, 1.5, 4 * 0.8 / 1.5)]
    public void NativeNtcSectionPureActions_MatchIndependentC24Stress(
        Ntc2018TimberPureEffectG2 effect, double n, double v, double m,
        double expectedStress, double expectedCapacity)
    {
        var report = Check(Proof(), effect, n, v, m);
        var result = Assert.Single(report.Results);
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(expectedStress, Assert.IsType<double>(result.Demand), 10);
        Assert.Equal(expectedCapacity, Assert.IsType<double>(result.Resistance), 10);
        Assert.Contains("G2 NTC2018", result.CalculationTrace);
        Assert.Contains("NOT VERIFIED:", result.CalculationTrace);
        Assert.Contains("kh=1", result.CalculationTrace);
    }

    [Fact]
    public void ShearOverCapacity_IsFailedRatherThanIncomplete()
    {
        var r = Check(Proof(), Ntc2018TimberPureEffectG2.InPlaneShear, 0, 20, 0);
        Assert.Equal(DesignCheckStatus.Failed, Assert.Single(r.Results).Status);
        Assert.Equal(1.40625, Assert.IsType<double>(Assert.Single(r.Results).Utilization), 12);
        Assert.Equal(DesignOverallStatus.Failed, r.OverallStatus);
    }

    [Fact]
    public void NetTensionArea_IsRequiredAndUsedInsteadOfGross()
    {
        var p = Proof(net:16000);
        var r = Check(p, Ntc2018TimberPureEffectG2.TensionParallel, 100, 0, 0);
        Assert.Equal(6.25, Assert.IsType<double>(Assert.Single(r.Results).Demand), 10);
        Assert.Contains("A_net_tension=16000", Assert.Single(r.Results).CalculationTrace);
        Assert.Equal(DesignCheckStatus.MissingData, Assert.Single(Check(
            Proof(net:null), Ntc2018TimberPureEffectG2.TensionParallel, 100, 0, 0).Results).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Assert.Single(Check(
            Proof(net:21000), Ntc2018TimberPureEffectG2.TensionParallel, 100, 0, 0).Results).Status);
    }

    [Fact]
    public void G1ColumnB_IsDistinctAndCannotBeSilentlyInferred()
    {
        var a = Check(Proof(), Ntc2018TimberPureEffectG2.StrongAxisBending, 0, 0, 6);
        var b = Check(Proof(Select(column:Ntc2018GammaColumnG1.B)),
            Ntc2018TimberPureEffectG2.StrongAxisBending, 0, 0, 6);
        Assert.Equal(12.8, Assert.IsType<double>(Assert.Single(a.Results).Resistance), 10);
        Assert.Equal(24 * 0.8 / 1.45, Assert.IsType<double>(Assert.Single(b.Results).Resistance), 10);
    }

    [Fact]
    public void G1ExceptionalSituationUsesDeclaredGammaOne_NotFundamentalCoefficient()
    {
        var p = Proof(Select(column:Ntc2018GammaColumnG1.NotApplicableExceptional,
            situation:Ntc2018TimberSituationG1.Exceptional));
        var report = Check(p, Ntc2018TimberPureEffectG2.StrongAxisBending, 0, 0, 6);
        Assert.Equal(24 * 1.10, Assert.IsType<double>(Assert.Single(report.Results).Resistance), 10);
    }

    [Fact]
    public void GlulamHasDifferentGammaAndShearCrackFactor()
    {
        var p = Proof(Select(grade:"GL24h"));
        var grade = Materials.FindTimber("GL24h");
        var report = Check(p, Ntc2018TimberPureEffectG2.InPlaneShear, 0, 10, 0);
        var expectedKcr = 2.5 / grade.ShearStrengthMpa;
        var demand = 1.5 * 10000 / (expectedKcr * 20000);
        Assert.Equal(demand, Assert.IsType<double>(Assert.Single(report.Results).Demand), 10);
        Assert.Equal(grade.ShearStrengthMpa * 0.8 / 1.45,
            Assert.IsType<double>(Assert.Single(report.Results).Resistance), 10);
    }

    [Theory]
    [InlineData(Ntc2018TimberPureEffectG2.TensionParallel, 10, 1, 0)]
    [InlineData(Ntc2018TimberPureEffectG2.CompressionParallel, -10, 0, 1)]
    [InlineData(Ntc2018TimberPureEffectG2.StrongAxisBending, 1, 0, 6)]
    [InlineData(Ntc2018TimberPureEffectG2.InPlaneShear, 0, 10, 1)]
    public void MixedComponentsAreNotFalselyCheckedAsPure(
        Ntc2018TimberPureEffectG2 effect, double n, double v, double m)
    {
        var r = Check(Proof(), effect, n, v, m);
        Assert.Equal(DesignCheckStatus.NotApplicable, Assert.Single(r.Results).Status);
        Assert.Equal(DesignOverallStatus.Incomplete, r.OverallStatus);
    }

    [Theory]
    [InlineData(Ntc2018TimberPureEffectG2.TensionParallel, -10, 0, 0)]
    [InlineData(Ntc2018TimberPureEffectG2.CompressionParallel, 10, 0, 0)]
    [InlineData(Ntc2018TimberPureEffectG2.StrongAxisBending, 0, 0, 0)]
    [InlineData(Ntc2018TimberPureEffectG2.InPlaneShear, 0, 0, 0)]
    public void WrongSignOrZeroIsNotAPass(
        Ntc2018TimberPureEffectG2 effect, double n, double v, double m) =>
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Assert.Single(Check(Proof(), effect, n, v, m).Results).Status);

    [Fact]
    public void ExternalNtcProfileCannotBeConvertedToEc5Approval()
    {
        var p = Proof();
        var ec5 = new NormativeProfile("EC5-FAKE",
            new DesignCodeEdition("EN 1995-1-1", TimberRectangularSectionRuleM92A.Edition,
                "EN-RECOMMENDED-NOT-NATIONAL-ANNEX"), p.Selection.Profile.Coefficients);
        var r = Check(p, Ntc2018TimberPureEffectG2.StrongAxisBending, 0, 0, 6, ec5);
        Assert.Equal(DesignCheckStatus.Unsupported, Assert.Single(r.Results).Status);
    }

    [Fact]
    public void ProfileIdOrCoefficientsMayNotBeChangedAfterSelection()
    {
        var p = Proof();
        var fakeId = p.Selection.Profile with { ProfileId = "OTHER" };
        Assert.Equal(DesignCheckStatus.MissingData, Assert.Single(Check(p,
            Ntc2018TimberPureEffectG2.StrongAxisBending, 0, 0, 6, fakeId).Results).Status);
        var list = p.Selection.Profile.Coefficients.ToArray();
        list[0] = list[0] with { Value = 0.2 };
        var fakeValue = p.Selection.Profile with { Coefficients = list };
        Assert.Equal(DesignCheckStatus.MissingData, Assert.Single(Check(p,
            Ntc2018TimberPureEffectG2.StrongAxisBending, 0, 0, 6, fakeValue).Results).Status);
    }

    [Fact]
    public void ForgedG1SelectionWithBothProfileAndCoefficientModifiedIsRejectedByNtcTables()
    {
        var selected = Select();
        var coefficients = selected.Profile.Coefficients.Select(c => c.Id == "kmod"
            ? c with { Value = 1.1 } : c).ToArray();
        var fake = selected with { Kmod = 1.1, Profile = selected.Profile with { Coefficients = coefficients } };
        Assert.Equal(DesignCheckStatus.MissingData,
            Assert.Single(Check(Proof(fake), Ntc2018TimberPureEffectG2.StrongAxisBending, 0, 0, 6).Results).Status);
    }

    [Fact]
    public void WrongCombinationMemberSectionOrSideCannotUseOtherEvidence()
    {
        var p = Proof();
        foreach (var r in new[]
        {
            Check(p,Ntc2018TimberPureEffectG2.StrongAxisBending,0,0,6,combo:"ULS-OTHER"),
            Check(p,Ntc2018TimberPureEffectG2.StrongAxisBending,0,0,6,member:"W2"),
            Check(p,Ntc2018TimberPureEffectG2.StrongAxisBending,0,0,6,section:"WRONG"),
            Check(p,Ntc2018TimberPureEffectG2.StrongAxisBending,0,0,6,side:null)
        })
        {
            Assert.Equal(DesignCheckStatus.MissingData, Assert.Single(r.Results).Status);
            Assert.Equal(DesignOverallStatus.Incomplete, r.OverallStatus);
        }
    }

    [Fact]
    public void MissingShearCrackSelectionCannotPassShear()
    {
        var p = Proof(Select(cracks:false));
        Assert.Equal(DesignCheckStatus.MissingData,
            Assert.Single(Check(p,Ntc2018TimberPureEffectG2.InPlaneShear,0,10,0).Results).Status);
    }

    [Fact]
    public void MissingShearDirectionCannotPassShear()
    {
        var p = Proof() with { ShearDirectionConfirmed = null };
        Assert.Equal(DesignCheckStatus.MissingData,
            Assert.Single(Check(p,Ntc2018TimberPureEffectG2.InPlaneShear,0,10,0).Results).Status);
    }

    [Fact]
    public void UnknownSectionOrMissingNoSizeIncreaseIsNotApproved()
    {
        var p = Proof() with { NoBeneficialSizeFactorConfirmed = false };
        Assert.Equal(DesignCheckStatus.MissingData,
            Assert.Single(Check(p,Ntc2018TimberPureEffectG2.StrongAxisBending,0,0,6).Results).Status);
        var p2 = Proof(intact:null);
        Assert.Equal(DesignCheckStatus.MissingData,
            Assert.Single(Check(p2,Ntc2018TimberPureEffectG2.InPlaneShear,0,10,0).Results).Status);
    }

    [Fact]
    public void FiniteConcomitantActionsMandatory()
    {
        var p = Proof();
        var effect = Ntc2018TimberPureEffectG2.StrongAxisBending;
        var request = Request(p, effect,0,0,6);
        var missing = request with { Effects = request.Effects with { ShearForceYKn = null } };
        var engine = new DesignCheckEngine(new[]
        {
            new Ntc2018TimberRectangularSectionRuleG2(effect, Materials, Tables, new[] { p })
        });
        Assert.Equal(DesignCheckStatus.MissingData, Assert.Single(engine.Evaluate(missing).Results).Status);
        var infinity = request with { Effects = request.Effects with { BendingMomentZKnm = double.PositiveInfinity } };
        Assert.Equal(DesignCheckStatus.InvalidInput, Assert.Single(engine.Evaluate(infinity).Results).Status);
    }

    [Fact]
    public void NoOverallMemberApprovalFromOnePassedRule()
    {
        var p = Proof();
        var effect = Ntc2018TimberPureEffectG2.StrongAxisBending;
        var rule = new Ntc2018TimberRectangularSectionRuleG2(effect, Materials, Tables, new[] { p });
        var report = new DesignCheckEngine(new[] { rule }).Evaluate(Request(p,effect,0,0,6));
        Assert.False(rule.EstablishesCompleteMemberCompliance);
        Assert.Equal(DesignCheckStatus.Passed, Assert.Single(report.Results).Status);
        // Engine OverallStatus is a REQUESTED-RULES summary, not a full-member NTC compliance certificate.
        Assert.Equal(DesignOverallStatus.Passed, report.OverallStatus);
        Assert.Contains("overall member/code compliance", Assert.Single(report.Results).CalculationTrace);
    }

    [Fact]
    public void DuplicateG2EvidenceForSameTargetIsRejected()
    {
        var p = Proof();
        Assert.Throws<ArgumentException>(() => new Ntc2018TimberRectangularSectionRuleG2(
            Ntc2018TimberPureEffectG2.StrongAxisBending, Materials, Tables, new[] { p, p }));
    }

    [Fact]
    public void G1ProfileOnlyCannotProduceNTCCheckWithoutRegisteredRule()
    {
        var p = Proof();
        var r = new DesignCheckEngine(Array.Empty<StructuralSolver2D.Verification.Rules.IDesignCheckRule>())
            .Evaluate(Request(p,Ntc2018TimberPureEffectG2.StrongAxisBending,0,0,6));
        Assert.Equal(DesignCheckStatus.Unsupported, Assert.Single(r.Results).Status);
        Assert.Equal(DesignOverallStatus.Incomplete,r.OverallStatus);
    }
}
