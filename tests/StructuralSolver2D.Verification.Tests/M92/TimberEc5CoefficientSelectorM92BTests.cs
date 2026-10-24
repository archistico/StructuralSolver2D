using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Timber;

namespace StructuralSolver2D.Verification.Tests.M92;

public sealed class TimberEc5CoefficientSelectorM92BTests
{
    private static readonly DesignCodeEdition RecommendedCode = new("EN 1995-1-1",
        TimberRectangularSectionRuleM92A.Edition,
        TimberEc5CoefficientSelectorM92B.RecommendedNonNationalAnnexId);

    private static readonly TimberEc5CoefficientTablesM92B Table =
        TimberEc5CoefficientTablesM92B.Load(Path.Combine(AppContext.BaseDirectory, "ec5-first-generation-coefficients.json"));
    private static readonly CharacteristicMaterialCatalogM88C Catalog =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));
    private static readonly TimberEc5CoefficientSelectorM92B Selector = new(Table, Catalog);

    private static TimberRectangularAssignmentM92A Assignment(string grade = "C24", string target = "ULS-A",
        int service = 1, TimberLoadDurationM92A? duration = null) =>
        new("W1", "RECT-100x200", grade, 100, 200, "Measured rectangle 100x200",
            service, duration, "TEST external service-class evidence", null,
            "AN-1", target, DesignEffectTargetKind.LoadCombination,
            true, true, true, true, true);

    private static TimberCombinationActionM92B Action(string id, TimberLoadDurationM92A duration,
        double factor = 1) => new(id, duration, factor, $"TEST duration evidence for {id}");

    private static TimberCoefficientSelectionM92B Input(
        TimberRectangularAssignmentM92A? assignment = null,
        IReadOnlyList<TimberCombinationActionM92B>? actions = null,
        DesignCodeEdition? code = null,
        TimberGammaBasisM92B basis = TimberGammaBasisM92B.EurocodeRecommendedNotNationalAnnex,
        double? nationalGamma = null,
        string? nationalEvidence = null) =>
        new(code ?? RecommendedCode, assignment ?? Assignment(),
            actions ?? new[] { Action("G", TimberLoadDurationM92A.Permanent),
                              Action("Q", TimberLoadDurationM92A.ShortTerm, 1.5) },
            true, "TEST: complete nonzero contributions cross-checked with FEM combination",
            "TEST: checked applicability of first-generation EC5 Table 3.1 for this annex",
            basis, nationalGamma, nationalEvidence);

    [Fact]
    public void TableContainsExactlyTheDocumentedFamiliesAndServiceClasses()
    {
        Assert.Equal(6, Table.KmodRows.Count);
        Assert.Equal(2, Table.RecommendedGammaRows.Count);
        Assert.Equal(1, Table.SchemaVersion);
        Assert.Equal(TimberRectangularSectionRuleM92A.Edition, Table.Edition);
        Assert.Contains("not a national annex", Table.Basis);
    }

    [Theory]
    [InlineData(1, 0.60, 0.70, 0.80, 0.90, 1.10)]
    [InlineData(2, 0.60, 0.70, 0.80, 0.90, 1.10)]
    [InlineData(3, 0.50, 0.55, 0.65, 0.70, 0.90)]
    public void RecommendedKmod_MatchesEc5Table31_ForBothTimberFamilies(int service,
        double permanent, double longTerm, double medium, double shortTerm, double instant)
    {
        foreach (var kind in new[] { StrengthMaterialKindM88C.SolidSoftwood,
                                     StrengthMaterialKindM88C.HomogeneousGlulam })
        {
            Assert.Equal(permanent, Table.Kmod(kind, service, TimberLoadDurationM92A.Permanent), 12);
            Assert.Equal(longTerm, Table.Kmod(kind, service, TimberLoadDurationM92A.LongTerm), 12);
            Assert.Equal(medium, Table.Kmod(kind, service, TimberLoadDurationM92A.MediumTerm), 12);
            Assert.Equal(shortTerm, Table.Kmod(kind, service, TimberLoadDurationM92A.ShortTerm), 12);
            Assert.Equal(instant, Table.Kmod(kind, service, TimberLoadDurationM92A.Instantaneous), 12);
        }
    }

    [Fact]
    public void GammaRecommendations_DistinguishSolidAndGlulam()
    {
        Assert.Equal(1.30, Table.RecommendedGammaM(StrengthMaterialKindM88C.SolidSoftwood), 12);
        Assert.Equal(1.25, Table.RecommendedGammaM(StrengthMaterialKindM88C.HomogeneousGlulam), 12);
    }

    [Fact]
    public void SelectionChoosesShortestParticipatingDuration_NeverHighestCoefficientBlindly()
    {
        var selected = Selector.Select(Input());
        Assert.Equal(TimberLoadDurationM92A.ShortTerm, selected.GoverningDuration);
        Assert.Equal(TimberLoadDurationM92A.ShortTerm, selected.Assignment.LoadDuration);
        Assert.Equal(0.9, selected.Kmod, 12);
        Assert.Equal(1.3, selected.GammaM, 12);
        Assert.False(selected.EstablishesMemberCompliance);
        Assert.False(selected.AutomaticallyValidatesNationalAnnex);
        Assert.Contains("G(Permanent", selected.Audit);
        Assert.Contains("Q(ShortTerm", selected.Audit);
        Assert.Contains("ULS-A", selected.Audit);
    }

    [Fact]
    public void ServiceClass3ReducesKmodForMediumDuration()
    {
        var selected = Selector.Select(Input(assignment:Assignment(service:3),
            actions:new[] {Action("G",TimberLoadDurationM92A.Permanent),
                            Action("S",TimberLoadDurationM92A.MediumTerm)}));
        Assert.Equal(0.65, selected.Kmod, 12);
        Assert.Equal(TimberLoadDurationM92A.MediumTerm, selected.GoverningDuration);
    }

    [Fact]
    public void ShortestDurationUsesTheParticipatingList_EvenForNegativeFactor()
    {
        var selected = Selector.Select(Input(actions:new[]
        {
            Action("G",TimberLoadDurationM92A.Permanent),
            Action("Q",TimberLoadDurationM92A.Instantaneous,-0.4)
        }));
        Assert.Equal(TimberLoadDurationM92A.Instantaneous, selected.GoverningDuration);
        Assert.Equal(1.10, selected.Kmod, 12);
    }

    [Fact]
    public void PreparedProfileAndAssignment_AgreeWithRealM92ASectionRule()
    {
        var selected = Selector.Select(Input(actions:new[]
        {
            Action("Q", TimberLoadDurationM92A.MediumTerm)
        }));
        var rule = selected.CreateBoundRule(TimberPureEffectM92A.StrongAxisBending, Catalog);
        var check = new DesignCheckRequest(
            new("W1", DesignMaterial.Timber, "RECT-100x200", 3), DesignLimitState.Ultimate,
            selected.Profile,
            new("AN-1", "ULS-A", DesignEffectOrigin.FemAnalysis, 1.0, 0, 0, 6)
            {
                MemberId = "W1", TargetKind = DesignEffectTargetKind.LoadCombination,
                SectionSide = DesignEffectSectionSide.Continuous
            }, new[] { rule.RuleId });
        var result = Assert.Single(new DesignCheckEngine(new[] { rule }).Evaluate(check).Results);
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(24 * 0.8 / 1.3, Assert.IsType<double>(result.Resistance), 10);
        Assert.Contains("duration=MediumTerm", Assert.IsType<string>(result.CalculationTrace));
    }

    [Fact]
    public void GlulamProfileDoesNotSilentlyUseSolidTimberGamma()
    {
        var selected = Selector.Select(Input(assignment: Assignment(grade:"GL24h")));
        Assert.Equal(1.25, selected.GammaM, 12);
        Assert.Equal(0.9, selected.Kmod, 12);
    }

    [Fact]
    public void SeparateCombinationsHaveSeparateIdentifiersAndCoefficients()
    {
        var permanent = Selector.Select(Input(assignment: Assignment(target:"ULS-G"),
            actions:new[] {Action("G",TimberLoadDurationM92A.Permanent)}));
        var shortTerm = Selector.Select(Input(assignment: Assignment(target:"ULS-Q"),
            actions:new[] {Action("Q",TimberLoadDurationM92A.ShortTerm)}));
        Assert.NotEqual(permanent.Profile.ProfileId, shortTerm.Profile.ProfileId);
        Assert.Equal(0.6, permanent.Kmod, 12);
        Assert.Equal(0.9, shortTerm.Kmod, 12);
        var rule = permanent.CreateBoundRule(TimberPureEffectM92A.StrongAxisBending, Catalog);
        var request = new DesignCheckRequest(new("W1", DesignMaterial.Timber, "RECT-100x200", 3),
            DesignLimitState.Ultimate, shortTerm.Profile,
            new("AN-1", "ULS-Q", DesignEffectOrigin.FemAnalysis, 1.0, 0, 0, 6)
            {
                MemberId="W1",TargetKind=DesignEffectTargetKind.LoadCombination,
                SectionSide=DesignEffectSectionSide.Continuous
            },new[]{rule.RuleId});
        Assert.Equal(DesignCheckStatus.MissingData,
            Assert.Single(new DesignCheckEngine(new[]{rule}).Evaluate(request).Results).Status);
        // Even if the member and target are correct, swapping coefficients from ULS-Q must fail.
        var forged = request with
        {
            Profile = shortTerm.Profile with { ProfileId = permanent.Profile.ProfileId },
            Effects = request.Effects with { CombinationId = "ULS-G" }
        };
        Assert.Equal(DesignCheckStatus.MissingData,
            Assert.Single(new DesignCheckEngine(new[]{rule}).Evaluate(forged).Results).Status);
    }

    [Fact]
    public void NationalAnnexModeNeedsExplicitGammaAndEvidence()
    {
        var national = new DesignCodeEdition("EN 1995-1-1",
            TimberRectangularSectionRuleM92A.Edition,"IT-DECLARED-NOT-VALIDATED");
        var selected = Selector.Select(Input(code:national,
            basis:TimberGammaBasisM92B.ExternallyDeclaredNationalAnnex,
            nationalGamma:1.37,nationalEvidence:"Engineering record identifying actual applicable value"));
        Assert.Equal(1.37,selected.GammaM,12);
        Assert.Contains("NO automated verification of national annex",selected.Audit);
        Assert.Contains("NOT independently verified",selected.Profile.Coefficients.Single(c=>c.Id=="gamma_M").SourceClause);
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(code:national,
            basis:TimberGammaBasisM92B.ExternallyDeclaredNationalAnnex)));
    }

    [Fact]
    public void CannotPassRecommendedGammaAsIfItWereNationalAnnex()
    {
        var national = new DesignCodeEdition("EN 1995-1-1",
            TimberRectangularSectionRuleM92A.Edition,"IT");
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(code:national)));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(
            basis:TimberGammaBasisM92B.ExternallyDeclaredNationalAnnex,nationalGamma:1.3,
            nationalEvidence:"Unidentified")));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(
            basis:TimberGammaBasisM92B.EurocodeRecommendedNotNationalAnnex,
            nationalGamma:1.3)));
    }

    [Fact]
    public void IncompleteActionDeclarationCannotProduceAPlausibleKmod()
    {
        var incomplete = Input() with {ActionListCompletenessConfirmed = false};
        Assert.Throws<ArgumentException>(()=>Selector.Select(incomplete));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input() with {ActionListEvidence=""}));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input() with {Table31ApplicabilityEvidence=""}));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(actions:Array.Empty<TimberCombinationActionM92B>())));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void InvalidOrZeroParticipantFactorIsRejected(double factor)
    {
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(actions:new[]
        {
            Action("G",TimberLoadDurationM92A.Permanent, factor)
        })));
    }

    [Fact]
    public void DuplicateActionIdsAreRejectedCaseInsensitively()
    {
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(actions:new[]
        {
            Action("Snow",TimberLoadDurationM92A.ShortTerm),
            Action("snow",TimberLoadDurationM92A.ShortTerm)
        })));
    }

    [Fact]
    public void InvalidOrUndocumentedActionDurationIsRejected()
    {
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(actions:new[]
        {
            Action("Q",(TimberLoadDurationM92A)99)
        })));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(actions:new[]
        {
            Action("Q",TimberLoadDurationM92A.MediumTerm) with {ClassificationEvidence=""}
        })));
    }

    [Fact]
    public void ContradictoryManualAssignmentDurationFailsClosed()
    {
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(
            assignment:Assignment(duration:TimberLoadDurationM92A.Permanent))));
    }

    [Fact]
    public void UnknownMaterialAndUnsupportedServiceClassAreRejected()
    {
        Assert.Throws<KeyNotFoundException>(()=>Selector.Select(Input(assignment:Assignment(grade:"NOT-A-GRADE"))));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input(assignment:Assignment(service:4))));
        Assert.Throws<NotSupportedException>(()=>Table.Kmod(StrengthMaterialKindM88C.RolledSteel,
            1,TimberLoadDurationM92A.Permanent));
    }

    [Fact]
    public void OtherEditionsNeverFallBackToFirstGeneration()
    {
        Assert.Throws<NotSupportedException>(()=>Selector.Select(Input(
            code:new("EN 1995-1-1","2025","IT"))));
        Assert.Throws<NotSupportedException>(()=>Selector.Select(Input(
            code:new("NTC","2018","IT"))));
    }

    [Fact]
    public void ShearKcrIsNeverSelectedByDefault()
    {
        var without = Selector.Select(Input());
        Assert.DoesNotContain(without.Profile.Coefficients, c=>c.Id=="kcr");
        var with = Selector.Select(Input() with
        {
            DeclaredKcr=0.67, KcrEvidence="External document confirming crack allowance"
        });
        Assert.Equal(0.67,Assert.Single(with.Profile.Coefficients,c=>c.Id=="kcr").Value,12);
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input() with {DeclaredKcr=0.67}));
        Assert.Throws<ArgumentException>(()=>Selector.Select(Input() with {DeclaredKcr=1.2, KcrEvidence="TEST"}));
    }

    [Fact]
    public void MalformedOrIncompleteJsonNeverTurnsIntoReferenceCoefficients()
    {
        Assert.ThrowsAny<Exception>(()=>TimberEc5CoefficientTablesM92B.FromJson("{}"));
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"ec5-first-generation-coefficients.json"));
        Assert.Throws<ArgumentException>(()=>TimberEc5CoefficientTablesM92B.FromJson(
            json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99",StringComparison.Ordinal)));
        Assert.Throws<ArgumentException>(()=>TimberEc5CoefficientTablesM92B.FromJson(
            json.Replace("\"serviceClass\": 3", "\"serviceClass\": 2",StringComparison.Ordinal)));
    }
}
