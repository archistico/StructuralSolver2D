using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Timber;

namespace StructuralSolver2D.Verification.Tests.M92;

public sealed class Ntc2018TimberCoefficientSelectorG1Tests
{
    private static readonly Ntc2018TimberCoefficientTablesG1 Tables =
        Ntc2018TimberCoefficientTablesG1.Load(Path.Combine(AppContext.BaseDirectory,
            "ntc2018-timber-coefficients-g1.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory,
            "characteristic-strength-classes.json"));
    private static readonly Ntc2018TimberCoefficientSelectorG1 Selector = new(Tables, Materials);

    private static TimberRectangularAssignmentM92A Assignment(string grade = "C24",
        string target = "ULS-STR-1", int service = 1,
        TimberLoadDurationM92A? duration = null) =>
        new("W1", "RECT-100x200", grade, 100, 200, "TEST known 100x200 rectangle",
            service, duration, "TEST room temperature/humidity evidence", null,
            "AN-NTC-1", target, DesignEffectTargetKind.LoadCombination,
            true, true, true, true, true);

    private static Ntc2018CombinationActionG1 Action(string id, TimberLoadDurationM92A duration,
        double factor = 1.3,
        Ntc2018ActionCategoryG1 category = Ntc2018ActionCategoryG1.G1StructuralPermanent) =>
        new(id, category, duration, factor, $"TEST duration source {id}", $"TEST factor source {id}");

    private static Ntc2018TimberSelectionG1 Input(
        TimberRectangularAssignmentM92A? assignment = null,
        IReadOnlyList<Ntc2018CombinationActionG1>? actions = null,
        Ntc2018TimberSituationG1 situation = Ntc2018TimberSituationG1.FundamentalStr,
        Ntc2018GammaColumnG1 column = Ntc2018GammaColumnG1.A,
        Ntc2018ColumnBProductionEvidenceG1? production = null,
        bool? crack = true,
        string? crackEvidence = "TEST: rectangular solid/glulam shear crack provision applies")
    {
        var a = assignment ?? Assignment();
        var list = actions ?? new[]
        {
            Action("G", TimberLoadDurationM92A.Permanent),
            Action("Q", TimberLoadDurationM92A.MediumTerm, 1.5, Ntc2018ActionCategoryG1.QVariable)
        };
        return new Ntc2018TimberSelectionG1(
            Ntc2018TimberCoefficientSelectorG1.Edition, a, situation,
            "TEST: source of declared STR or exceptional situation", column, production,
            list, true, "TEST: complete external action list", 
            Ntc2018TimberCoefficientSelectorG1.Fingerprint(a.AnalysisId, a.TargetId, list),
            "TEST: separately compared fingerprint to FEM load combination snapshot",
            "TEST: NTC timber table applicability independently checked",
            "TEST: real construction grade and documents checked externally", crack, crackEvidence);
    }

    private static Ntc2018ColumnBProductionEvidenceG1 Certified(double cov = 0.12) =>
        new(true, true, true,
            "TEST: NTC11.1 A/ETA-C certificate of continuous production",
            "TEST: NTC11.7 certified production quality system",
            cov, "TEST: batch resistance statistics, tested coefficient of variation");

    [Theory]
    [InlineData(1, 0.60, 0.70, 0.80, 0.90, 1.10)]
    [InlineData(2, 0.60, 0.70, 0.80, 0.90, 1.10)]
    [InlineData(3, 0.50, 0.55, 0.65, 0.70, 0.90)]
    public void Table44IV_KmodBothSupportedFamilies(int service, double p, double l,
        double m, double s, double instant)
    {
        foreach (var kind in new[] { StrengthMaterialKindM88C.SolidSoftwood,
                                     StrengthMaterialKindM88C.HomogeneousGlulam })
        {
            Assert.Equal(p, Tables.Kmod(kind, service, TimberLoadDurationM92A.Permanent), 12);
            Assert.Equal(l, Tables.Kmod(kind, service, TimberLoadDurationM92A.LongTerm), 12);
            Assert.Equal(m, Tables.Kmod(kind, service, TimberLoadDurationM92A.MediumTerm), 12);
            Assert.Equal(s, Tables.Kmod(kind, service, TimberLoadDurationM92A.ShortTerm), 12);
            Assert.Equal(instant, Tables.Kmod(kind, service, TimberLoadDurationM92A.Instantaneous), 12);
        }
    }

    [Fact]
    public void NtcTableIsNotAnEc5AnnexAndHasExactRows()
    {
        Assert.Equal(6, Tables.KmodRows.Count);
        Assert.Equal(2, Tables.GammaRows.Count);
        Assert.Equal(1, Tables.SchemaVersion);
        Assert.Equal("DM 17 gennaio 2018", Ntc2018TimberCoefficientSelectorG1.Edition.DocumentId);
        Assert.NotEqual("EN 1995-1-1", Ntc2018TimberCoefficientSelectorG1.Edition.DocumentId);
    }

    [Theory]
    [InlineData(StrengthMaterialKindM88C.SolidSoftwood,1.50,1.45)]
    [InlineData(StrengthMaterialKindM88C.HomogeneousGlulam,1.45,1.35)]
    public void Table44III_FundamentalAndExceptionalGamma(StrengthMaterialKindM88C kind,
        double a, double b)
    {
        Assert.Equal(a, Tables.Gamma(kind, Ntc2018TimberSituationG1.FundamentalStr,
            Ntc2018GammaColumnG1.A), 12);
        Assert.Equal(b, Tables.Gamma(kind, Ntc2018TimberSituationG1.FundamentalStr,
            Ntc2018GammaColumnG1.B), 12);
        Assert.Equal(1.0, Tables.Gamma(kind, Ntc2018TimberSituationG1.Exceptional,
            Ntc2018GammaColumnG1.NotApplicableExceptional), 12);
    }

    [Fact]
    public void FundamentalColumnA_SelectsRealNtcCoefficientsNeverEc5Recommendations()
    {
        var selected = Selector.Select(Input());
        Assert.Equal(0.8, selected.Kmod, 12);
        Assert.Equal(1.5, selected.GammaM, 12);
        Assert.Equal(0.5, Assert.IsType<double>(selected.Kcr), 12); // 2 MPa / 4 MPa
        Assert.Equal(TimberLoadDurationM92A.MediumTerm, selected.GoverningDuration);
        Assert.Equal(selected.GoverningDuration, selected.Assignment.LoadDuration);
        Assert.Equal(1.5, Assert.Single(selected.Profile.Coefficients, x => x.Id == "gamma_M").Value, 12);
        Assert.Equal("DM 17 gennaio 2018", selected.Profile.Code.DocumentId);
        Assert.False(selected.IsDesignCheck);
        Assert.False(selected.EstablishesMemberCompliance);
        Assert.False(selected.ConfirmsWholeCombinationIsNormative);
        Assert.False(selected.IndependentlyConfirmsColumnBDocumentation);
        Assert.Contains("COEFFICIENTS ONLY", selected.Audit);
    }

    [Fact]
    public void FundamentalColumnB_DemandsCompleteEvidence_NoSilentDowngrade()
    {
        var valid = Selector.Select(Input(column:Ntc2018GammaColumnG1.B, production:Certified()));
        Assert.Equal(1.45, valid.GammaM, 12);
        Assert.Throws<ArgumentException>(() => Selector.Select(Input(column:Ntc2018GammaColumnG1.B)));
        Assert.Throws<ArgumentException>(() => Selector.Select(Input(column:Ntc2018GammaColumnG1.B,
            production:Certified(0.150001))));
        Assert.Equal(1.45, Selector.Select(Input(column:Ntc2018GammaColumnG1.B,
            production:Certified(0.15))).GammaM, 12);
        Assert.Throws<ArgumentException>(() => Selector.Select(Input(column:Ntc2018GammaColumnG1.B,
            production:Certified() with { Ntc11_7QualitySystemConfirmed = null })));
        Assert.Throws<ArgumentException>(() => Selector.Select(Input(column:Ntc2018GammaColumnG1.B,
            production:Certified() with { ProductCertificationReference = "" })));
        Assert.Throws<ArgumentException>(() => Selector.Select(Input(column:Ntc2018GammaColumnG1.A,
            production:Certified())));
    }

    [Fact]
    public void GlulamGammaKcrAndMediumDurationKmod_AreNationalNotRecommended()
    {
        var selected = Selector.Select(Input(assignment:Assignment(grade:"GL24h", service:3)));
        Assert.Equal(1.45, selected.GammaM, 12);
        Assert.Equal(0.65, selected.Kmod, 12);
        Assert.Equal(2.5 / Materials.FindTimber("GL24h").ShearStrengthMpa,
            Assert.IsType<double>(selected.Kcr), 12);
        Assert.Equal(1.35, Selector.Select(Input(assignment:Assignment(grade:"GL24h"),
            column:Ntc2018GammaColumnG1.B, production:Certified())).GammaM, 12);
    }

    [Fact]
    public void GoldenC24Bending_NtcDesignResistanceIs12Point8Mpa_WithoutClaimingFullCheck()
    {
        var coefficients = Selector.Select(Input());
        var grade = Materials.FindTimber("C24");
        var fmd = grade.BendingStrengthMpa * coefficients.Kmod / coefficients.GammaM;
        var stress = 6e6 / (100.0 * 200.0 * 200.0 / 6.0);
        Assert.Equal(12.8, fmd, 10);
        Assert.Equal(0.703125, stress / fmd, 10);
        Assert.False(coefficients.IsDesignCheck);
    }

    [Fact]
    public void ZeroFactorActionDoesNotInfluenceGoverningDurationButIsKeptInManifest()
    {
        var selected = Selector.Select(Input(actions:new[]
        {
            Action("G", TimberLoadDurationM92A.Permanent),
            Action("W", TimberLoadDurationM92A.Instantaneous,0,Ntc2018ActionCategoryG1.QVariable)
        }));
        Assert.Equal(TimberLoadDurationM92A.Permanent, selected.GoverningDuration);
        Assert.Equal(0.6, selected.Kmod, 12);
        Assert.Equal(2, selected.Manifest.Count);
    }

    [Fact]
    public void NegativeNonzeroCombinationFactorStillParticipates()
    {
        var selected = Selector.Select(Input(actions:new[]
        {
            Action("G", TimberLoadDurationM92A.Permanent),
            Action("W", TimberLoadDurationM92A.ShortTerm,-0.5,Ntc2018ActionCategoryG1.QVariable)
        }));
        Assert.Equal(TimberLoadDurationM92A.ShortTerm, selected.GoverningDuration);
        Assert.Equal(0.9, selected.Kmod, 12);
    }

    [Fact]
    public void ExceptionalRequiresParticipatingExceptionalActionAndNoColumnAB()
    {
        var actions = new[]
        {
            Action("G", TimberLoadDurationM92A.Permanent),
            Action("Fire", TimberLoadDurationM92A.Instantaneous,1,
                Ntc2018ActionCategoryG1.Exceptional)
        };
        var selected = Selector.Select(Input(actions:actions,
            situation:Ntc2018TimberSituationG1.Exceptional,
            column:Ntc2018GammaColumnG1.NotApplicableExceptional));
        Assert.Equal(1.0, selected.GammaM, 12);
        Assert.Equal(1.1, selected.Kmod, 12);
        Assert.Throws<ArgumentException>(() => Selector.Select(Input(actions:actions)));
        Assert.Throws<ArgumentException>(() => Selector.Select(Input(
            situation:Ntc2018TimberSituationG1.Exceptional,
            column:Ntc2018GammaColumnG1.NotApplicableExceptional)));
        Assert.Throws<ArgumentException>(() => Selector.Select(Input(actions:actions,
            situation:Ntc2018TimberSituationG1.Exceptional)));
    }

    [Fact]
    public void WrongEditionCannotBePassedOffAsNtc2018()
    {
        var valid = Input();
        Assert.Throws<NotSupportedException>(() => Selector.Select(valid with
        {
            Code = new("EN 1995-1-1", TimberRectangularSectionRuleM92A.Edition, "IT")
        }));
        Assert.Throws<NotSupportedException>(() => Selector.Select(valid with
        {
            Code = new("DM 17 gennaio 2018", "2025", "IT-NTC2018-NATIONAL-CODE")
        }));
    }

    [Fact]
    public void FingerprintIsDeterministicAndChangesWithCombinationContent()
    {
        var a = Action("G", TimberLoadDurationM92A.Permanent);
        var b = Action("Q", TimberLoadDurationM92A.MediumTerm,1.5,Ntc2018ActionCategoryG1.QVariable);
        var fp = Ntc2018TimberCoefficientSelectorG1.Fingerprint("AN-NTC-1","ULS-STR-1",new[]{a,b});
        Assert.Equal(fp,Ntc2018TimberCoefficientSelectorG1.Fingerprint("AN-NTC-1","ULS-STR-1",new[]{b,a}));
        Assert.NotEqual(fp,Ntc2018TimberCoefficientSelectorG1.Fingerprint("AN-NTC-1","ULS-STR-2",new[]{a,b}));
        Assert.NotEqual(fp,Ntc2018TimberCoefficientSelectorG1.Fingerprint("AN-NTC-1","ULS-STR-1",new[]{a,b with {CombinationFactor=1.4}}));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input() with {ExpectedCombinationFingerprint = "UNMATCHED"}));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input() with {FingerprintMatchEvidence = ""}));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input() with {CompleteActionListConfirmed = false}));
    }

    [Fact]
    public void DifferentMemberAndCombinationProduceDifferentProfileIdentity()
    {
        var a = Selector.Select(Input());
        var b = Selector.Select(Input(assignment:Assignment(target:"ULS-STR-2")));
        Assert.NotEqual(a.Profile.ProfileId,b.Profile.ProfileId);
        Assert.NotEqual(a.CombinationFingerprint,b.CombinationFingerprint);
    }

    [Fact]
    public void MaterialGradeOrServiceEvidenceMustBePresent()
    {
        Assert.Throws<KeyNotFoundException>(()=>Selector.Select(Input(assignment:Assignment(grade:"UNLISTED"))));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(assignment:Assignment(service:4))));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(assignment:Assignment() with {ServiceClassEvidence=""})));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input() with {MaterialEvidence=""}));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input() with {KmodApplicabilityEvidence=""}));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input() with {SituationEvidence=""}));
    }

    [Fact]
    public void MissingOrInvalidActionsFailClosed()
    {
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(actions:new[]
        {
            Action("G",TimberLoadDurationM92A.Permanent,0)
        })));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(actions:new[]
        {
            Action("G",TimberLoadDurationM92A.Permanent),
            Action("g",TimberLoadDurationM92A.ShortTerm)
        })));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(actions:new[]
        {
            Action("G",TimberLoadDurationM92A.Permanent,double.NaN)
        })));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(actions:new[]
        {
            Action("G",(TimberLoadDurationM92A)987)
        })));
    }

    [Fact]
    public void IncorrectManualDurationIsRejected()
    {
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(
            assignment:Assignment(duration:TimberLoadDurationM92A.Permanent))));
    }

    [Fact]
    public void KcrIsOptionalOnlyAfterExplicitJustification()
    {
        var selected = Selector.Select(Input(crack:false,
            crackEvidence:"TEST: bending-only coefficient selection, crack reduction not requested"));
        Assert.Null(selected.Kcr);
        Assert.DoesNotContain(selected.Profile.Coefficients,c=>c.Id=="kcr");
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(crack:null)));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(crack:false,crackEvidence:null)));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(crack:true,crackEvidence:null)));
    }

    [Fact]
    public void NtcCoefficientProfileCannotBeReusedAsAnEc5M92AResult()
    {
        var selected = Selector.Select(Input());
        var rule = new TimberRectangularSectionRuleM92A(new("EN 1995-1-1",
                TimberRectangularSectionRuleM92A.Edition,"IT-DECLARED-NOT-VALIDATED"),
            TimberPureEffectM92A.StrongAxisBending,Materials,new[]{Assignment()});
        var request = new DesignCheckRequest(
            new("W1",DesignMaterial.Timber,"RECT-100x200",3),DesignLimitState.Ultimate,
            selected.Profile,
            new("AN-NTC-1","ULS-STR-1",DesignEffectOrigin.FemAnalysis,1,0,0,6)
            {
                MemberId="W1",TargetKind=DesignEffectTargetKind.LoadCombination,
                SectionSide=DesignEffectSectionSide.Continuous
            },new[]{rule.RuleId});
        var result = Assert.Single(new DesignCheckEngine(new[]{rule}).Evaluate(request).Results);
        // The engine rejects a normative edition/annex mismatch before rule dispatch.
        // Unsupported is correct; NotApplicable denotes a compatible rule outside its physical scope.
        Assert.Equal(DesignCheckStatus.Unsupported,result.Status);
        Assert.Null(result.Utilization);
        Assert.Null(result.Resistance);
    }

    [Fact]
    public void ModifiedOrBrokenReferenceTableCannotSilentlyChangeNormativeValues()
    {
        var path = Path.Combine(AppContext.BaseDirectory,"ntc2018-timber-coefficients-g1.json");
        var json = File.ReadAllText(path);
        Assert.ThrowsAny<Exception>(()=>Ntc2018TimberCoefficientTablesG1.FromJson("{}"));
        Assert.Throws<ArgumentException>(()=>Ntc2018TimberCoefficientTablesG1.FromJson(
            json.Replace("\"fundamentalColumnA\": 1.5", "\"fundamentalColumnA\": 1.3",StringComparison.Ordinal)));
        Assert.Throws<ArgumentException>(()=>Ntc2018TimberCoefficientTablesG1.FromJson(
            json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 90",StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(Ntc2018ActionCategoryG1.G1StructuralPermanent,1.0,1.3)]
    [InlineData(Ntc2018ActionCategoryG1.G2NonStructuralPermanent,0.8,1.5)]
    [InlineData(Ntc2018ActionCategoryG1.QVariable,0.0,1.5)]
    public void NtcStrA1Baseline_IsCorrect(Ntc2018ActionCategoryG1 category,
        double favorable,double unfavorable)
    {
        Assert.Equal(favorable, Ntc2018StrA1ActionFactorsG1.Factor(
            category,Ntc2018ActionEffectG1.Favorable),12);
        Assert.Equal(unfavorable, Ntc2018StrA1ActionFactorsG1.Factor(
            category,Ntc2018ActionEffectG1.Unfavorable),12);
    }

    [Fact]
    public void G2KnownValueExceptionRequiresDocumentation()
    {
        Assert.Equal(1.3, Ntc2018StrA1ActionFactorsG1.Factor(
            Ntc2018ActionCategoryG1.G2NonStructuralPermanent,Ntc2018ActionEffectG1.Unfavorable,
            true,"TEST: fully defined permanent load"),12);
        Assert.Throws<ArgumentException>(()=>Ntc2018StrA1ActionFactorsG1.Factor(
            Ntc2018ActionCategoryG1.G2NonStructuralPermanent,Ntc2018ActionEffectG1.Unfavorable,true));
        Assert.Throws<ArgumentException>(()=>Ntc2018StrA1ActionFactorsG1.Factor(
            Ntc2018ActionCategoryG1.QVariable,Ntc2018ActionEffectG1.Unfavorable,true,"Wrong"));
        Assert.Throws<NotSupportedException>(()=>Ntc2018StrA1ActionFactorsG1.Factor(
            Ntc2018ActionCategoryG1.Exceptional,Ntc2018ActionEffectG1.Unfavorable));
    }

    [Fact]
    public void SteelNtcfactorsRemainDistinctFromEurocodeRecommended()
    {
        Assert.Equal(1.05,Ntc2018SteelPartialFactorsG1.GammaM0,12);
        Assert.Equal(1.05,Ntc2018SteelPartialFactorsG1.GammaM1OrdinaryMembers,12);
        Assert.Equal(1.10,Ntc2018SteelPartialFactorsG1.GammaM1BridgeMembers,12);
        Assert.Equal(1.25,Ntc2018SteelPartialFactorsG1.GammaM2NetTensionFracture,12);
    }
}
