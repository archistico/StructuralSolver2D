using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Rules;
using StructuralSolver2D.Verification.Timber;

namespace StructuralSolver2D.Verification.Tests.M92;

public sealed class Ntc2018TimberCombinedSectionG3Tests
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


    private static Ntc2018TimberCombinedEvidenceG3 Combined(
        Ntc2018TimberSectionEvidenceG2? section = null,
        bool? noWeakAxis = true, bool? simultaneous = true, bool? linearElastic = true) =>
        new(section ?? Proof(), simultaneous, "TEST FEM station signed concomitant values",
            noWeakAxis, "TEST independent My=0 lateral loading review",
            linearElastic, "TEST verified linear elastic load action domain",
            2, DesignEffectSectionSide.Continuous, "TEST independently documented station x=2m, continuous side");

    private static DesignCheckRequest Request(Ntc2018TimberCombinedEvidenceG3 p,
        Ntc2018TimberCombinedEffectG3 effect, double n, double v, double m,
        NormativeProfile? profile = null, string? member = null,
        string? combo = null, DesignEffectSectionSide? side = DesignEffectSectionSide.Continuous,
        string? section = null, double? shear = null, double? moment = null,
        DesignEffectOrigin origin = DesignEffectOrigin.FemAnalysis) =>
        new(new DesignMemberContext(member ?? "W1", DesignMaterial.Timber,
                section ?? "RECT-100x200", 4),
            DesignLimitState.Ultimate, profile ?? p.Section.Selection.Profile,
            new DesignEffectSnapshot(p.Section.Selection.Assignment.AnalysisId,
                combo ?? p.Section.Selection.Assignment.TargetId, origin, 2,
                n, shear ?? v, moment ?? m)
            {
                MemberId = member ?? "W1", TargetKind = DesignEffectTargetKind.LoadCombination,
                SectionSide = side
            }, new[] { Id(effect) });

    private static string Id(Ntc2018TimberCombinedEffectG3 effect) => effect switch
    {
        Ntc2018TimberCombinedEffectG3.TensionBending => Ntc2018TimberCombinedSectionRuleG3.TensionBendingId,
        Ntc2018TimberCombinedEffectG3.CompressionBending => Ntc2018TimberCombinedSectionRuleG3.CompressionBendingId,
        Ntc2018TimberCombinedEffectG3.BendingAndShear => Ntc2018TimberCombinedSectionRuleG3.BendingShearId,
        _ => throw new ArgumentOutOfRangeException(nameof(effect))
    };

    private static DesignCheckReport Check(Ntc2018TimberCombinedEvidenceG3 proof,
        Ntc2018TimberCombinedEffectG3 effect, double n, double v, double m,
        NormativeProfile? profile = null, string? member = null,
        string? combo = null, DesignEffectSectionSide? side = DesignEffectSectionSide.Continuous,
        string? section = null, DesignEffectOrigin origin = DesignEffectOrigin.FemAnalysis) =>
        new DesignCheckEngine(new[]
        {
            new Ntc2018TimberCombinedSectionRuleG3(effect, Materials, Tables, new[] { proof })
        }).Evaluate(Request(proof, effect, n, v, m, profile, member, combo, side, section,
            origin: origin));

    [Theory]
    [InlineData(Ntc2018TimberCombinedEffectG3.TensionBending, 20, 0, 3, 1.0 / (14.5 * .8 / 1.5) + 4.5 / 12.8)]
    [InlineData(Ntc2018TimberCombinedEffectG3.CompressionBending, -20, 0, 3, (1.0 / (21 * .8 / 1.5)) * (1.0 / (21 * .8 / 1.5)) + 4.5 / 12.8)]
    [InlineData(Ntc2018TimberCombinedEffectG3.BendingAndShear, 0, 10, 3, 1.5 / (4 * .8 / 1.5))]
    public void CoreNtcG3Equations_MatchIndependentC24HandCalculation(
        Ntc2018TimberCombinedEffectG3 effect, double n, double v, double m, double utilization)
    {
        var result = Assert.Single(Check(Combined(),effect,n,v,m).Results);
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(utilization, Assert.IsType<double>(result.Utilization), 10);
        Assert.Equal(utilization, Assert.IsType<double>(result.Demand), 10);
        Assert.Equal(1.0, Assert.IsType<double>(result.Resistance));
        Assert.Equal("ratio", result.Unit);
        Assert.Contains("equation a=", result.CalculationTrace);
        Assert.Contains("equation b=", result.CalculationTrace);
        Assert.Contains("NOT VERIFIED:", result.CalculationTrace);
    }

    [Fact]
    public void BothNtcEquationsAreCalculated_KmOnlyOnSecondEquation()
    {
        var result = Assert.Single(Check(Combined(),
            Ntc2018TimberCombinedEffectG3.TensionBending, 20, 0, 3).Results);
        var trace = Assert.IsType<string>(result.CalculationTrace);
        Assert.Contains("km=", trace);
        var eq1 = 1.0 / (14.5 * .8 / 1.5) + 4.5 / 12.8;
        var eq2 = 1.0 / (14.5 * .8 / 1.5) + .7 * 4.5 / 12.8;
        // Trace values use the production calculation order, which may differ by an ULP
        // from this independent hand expression. Check the NUMBERS, not G17 text identity.
        var match = System.Text.RegularExpressions.Regex.Match(trace,
            @"equation a=(?<a>[-+0-9.Ee]+); equation b=(?<b>[-+0-9.Ee]+);");
        Assert.True(match.Success, "Both NTC equations must appear in the calculation trace.");
        var actualA = double.Parse(match.Groups["a"].Value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture);
        var actualB = double.Parse(match.Groups["b"].Value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(eq1, actualA, 12);
        Assert.Equal(eq2, actualB, 12);
        Assert.True(actualB < actualA,
            "The k_m=0.7 reduction belongs only to NTC equation (b).");
    }

    [Theory]
    [InlineData(Ntc2018TimberCombinedEffectG3.TensionBending, 20, 10, 3)]
    [InlineData(Ntc2018TimberCombinedEffectG3.CompressionBending, -20, 10, 3)]
    public void ConcomitantShearIsCheckedIndependentlyWithoutInventedNVmInteraction(
        Ntc2018TimberCombinedEffectG3 effect, double n, double v, double m)
    {
        var report = Check(Combined(), effect, n, v, m);
        var result = Assert.Single(report.Results);
        var axial = n > 0 ? 1.0 / (14.5 * .8 / 1.5) : Math.Pow(1.0 / (21 * .8 / 1.5), 2);
        var bendingRatio = axial + 4.5 / 12.8;
        var shearRatio = 1.5 * 10000 / (.5 * 20000) / (4 * .8 / 1.5);
        Assert.Equal(Math.Max(bendingRatio, shearRatio), Assert.IsType<double>(result.Utilization), 10);
        Assert.Contains("separate shear ratio=", result.CalculationTrace);
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
    }

    [Fact]
    public void HighShearFails_EvenWithPassingNtcTensoflessione()
    {
        var result = Assert.Single(Check(Combined(),
            Ntc2018TimberCombinedEffectG3.TensionBending, 20, 20, 3).Results);
        Assert.Equal(DesignCheckStatus.Failed, result.Status);
        Assert.Equal(1.40625, Assert.IsType<double>(result.Utilization), 10);
    }

    [Fact]
    public void HighBendingFails_EvenWithPassingShear()
    {
        var result = Assert.Single(Check(Combined(),
            Ntc2018TimberCombinedEffectG3.BendingAndShear, 0, 10, 12).Results);
        Assert.Equal(DesignCheckStatus.Failed, result.Status);
        Assert.Equal(18.0 / 12.8, Assert.IsType<double>(result.Utilization), 10);
    }

    [Fact]
    public void JustBelowAndAboveCapacity_AreDistinctAndTraceable()
    {
        var m = 12.8 * (100 * 200 * 200 / 6.0) / 1e6 * (1.0 - 1e-9);
        var passed = Assert.Single(Check(Combined(), Ntc2018TimberCombinedEffectG3.BendingAndShear,
            0, 1, m).Results);
        var failed = Assert.Single(Check(Combined(), Ntc2018TimberCombinedEffectG3.BendingAndShear,
            0, 1, m * 1.0001).Results);
        Assert.Equal(DesignCheckStatus.Passed, passed.Status);
        Assert.Equal(DesignCheckStatus.Failed, failed.Status);
    }

    [Fact]
    public void MaterialKindAndNTCColumnBChangeTheResistanceCorrectly()
    {
        var pA = Combined();
        var pB = Combined(Proof(Select(column:Ntc2018GammaColumnG1.B)));
        var rA = Assert.Single(Check(pA,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3).Results);
        var rB = Assert.Single(Check(pB,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3).Results);
        Assert.True(rB.Utilization < rA.Utilization);
        Assert.Equal(1 / (14.5*.8/1.45) + 4.5 / (24*.8/1.45),
            Assert.IsType<double>(rB.Utilization), 10);
        var gl = Combined(Proof(Select(grade:"GL24h")));
        var rg = Assert.Single(Check(gl,Ntc2018TimberCombinedEffectG3.BendingAndShear,0,10,3).Results);
        var gt = Materials.FindTimber("GL24h");
        Assert.Equal(Math.Max(4.5/(gt.BendingStrengthMpa*.8/1.45),
            (1.5*10000/( (2.5/gt.ShearStrengthMpa)*20000))/(gt.ShearStrengthMpa*.8/1.45)),
            Assert.IsType<double>(rg.Utilization),10);
    }

    [Fact]
    public void ExceptionalSituationAndKmodRemainExplicit()
    {
        var p = Combined(Proof(Select(column:Ntc2018GammaColumnG1.NotApplicableExceptional,
            situation:Ntc2018TimberSituationG1.Exceptional)));
        var r = Assert.Single(Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3).Results);
        var expect = 1/(14.5*1.10)+4.5/(24*1.10);
        Assert.Equal(expect, Assert.IsType<double>(r.Utilization), 10);
        Assert.Contains("gamma_M=1",r.CalculationTrace);
    }

    [Theory]
    [InlineData(Ntc2018TimberCombinedEffectG3.TensionBending, -20,0,3)]
    [InlineData(Ntc2018TimberCombinedEffectG3.CompressionBending, 20,0,3)]
    [InlineData(Ntc2018TimberCombinedEffectG3.BendingAndShear, 20,10,3)]
    [InlineData(Ntc2018TimberCombinedEffectG3.BendingAndShear, 0,0,3)]
    [InlineData(Ntc2018TimberCombinedEffectG3.TensionBending, 20,0,0)]
    [InlineData(Ntc2018TimberCombinedEffectG3.CompressionBending, -20,0,0)]
    public void WrongEffectBranchMustNotPass(
        Ntc2018TimberCombinedEffectG3 effect, double n, double v, double m)
    {
        var report=Check(Combined(),effect,n,v,m);
        Assert.Equal(DesignCheckStatus.NotApplicable,Assert.Single(report.Results).Status);
        Assert.Equal(DesignOverallStatus.Incomplete,report.OverallStatus);
    }

    [Theory]
    [InlineData(true,false,true)]
    [InlineData(false,true,true)]
    [InlineData(true,true,false)]
    public void MissingExternalEvidenceReturnsIncomplete(bool concomitant,bool noWeakAxis,bool linear)
    {
        var p=Combined(simultaneous:concomitant,noWeakAxis:noWeakAxis,linearElastic:linear);
        var r=Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3);
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(r.Results).Status);
        Assert.Equal(DesignOverallStatus.Incomplete,r.OverallStatus);
    }

    [Fact]
    public void NetAreaReducedByHolesCannotBeUsedForCombinedBending()
    {
        var p=Combined(Proof(net:16000));
        var r=Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3);
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(r.Results).Status);
    }

    [Fact]
    public void MissingIntactSectionOrNoSizeEnhancementNeverPasses()
    {
        var p=Combined(Proof(intact:false));
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(Check(p,
            Ntc2018TimberCombinedEffectG3.CompressionBending,-20,0,3).Results).Status);
        var p2=Combined(Proof() with { NoBeneficialSizeFactorConfirmed = false });
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(Check(p2,
            Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3).Results).Status);
    }

    [Fact]
    public void NonzeroShearWithoutCircolareKcrOrDirectionIsRejected()
    {
        var p=Combined(Proof(Select(cracks:false)));
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(Check(p,
            Ntc2018TimberCombinedEffectG3.TensionBending,20,10,3).Results).Status);
        var p2=Combined(Proof() with {ShearDirectionConfirmed=false});
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(Check(p2,
            Ntc2018TimberCombinedEffectG3.BendingAndShear,0,10,3).Results).Status);
        Assert.Equal(DesignCheckStatus.Passed,Assert.Single(Check(p,
            Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3).Results).Status);
    }

    [Fact]
    public void ProfileIdAndValuesCannotBeForged()
    {
        var p=Combined();
        var profile=p.Section.Selection.Profile;
        var badId=profile with {ProfileId="FAKE-NTC"};
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(Check(p,
            Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3,profile:badId).Results).Status);
        var coeff=profile.Coefficients.ToArray();
        coeff[0]=coeff[0] with {Value=1.1};
        var badValue=profile with {Coefficients=coeff};
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(Check(p,
            Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3,profile:badValue).Results).Status);
    }

    [Fact]
    public void ForgedG1SelectedKmodWithCorrespondingProfileMustBeRejected()
    {
        var p=Combined();var s=p.Section.Selection;
        var list=s.Profile.Coefficients.Select(c=>c.Id=="kmod" ? c with {Value=1.1} : c).ToArray();
        var fake=s with {Kmod=1.1,Profile=s.Profile with {Coefficients=list}};
        var bad=p with {Section=p.Section with {Selection=fake}};
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(Check(bad,
            Ntc2018TimberCombinedEffectG3.CompressionBending,-20,0,3).Results).Status);
    }

    [Fact]
    public void DifferentMemberSectionCombinationAndStationSideNeverReuseEvidence()
    {
        var p=Combined();
        foreach(var report in new[] {
            Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3,member:"W2"),
            Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3,combo:"OTHER"),
            Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3,section:"OTHER"),
            Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3,side:null),
            Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3,origin:DesignEffectOrigin.ExternalManual)
        })
            Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(report.Results).Status);
    }

    [Fact]
    public void StationAndSectionSideEvidenceMustBeExactWithoutRounding()
    {
        var p=Combined();
        var engine=new DesignCheckEngine(new[]{new Ntc2018TimberCombinedSectionRuleG3(
            Ntc2018TimberCombinedEffectG3.TensionBending,Materials,Tables,new[]{p})});
        var request=Request(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3);
        var otherStation=engine.Evaluate(request with { Effects=request.Effects with {PositionMetres=2.0000001} });
        var otherSide=engine.Evaluate(request with { Effects=request.Effects with {SectionSide=DesignEffectSectionSide.BeforePointLoad} });
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(otherStation.Results).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(otherSide.Results).Status);
    }

    [Fact]
    public void Ec5ProfileIsNeverAcceptedByNativeNtcRule()
    {
        var p=Combined();
        var fake=new NormativeProfile("EC5",new DesignCodeEdition("EN 1995-1-1",
            TimberRectangularSectionRuleM92A.Edition,"EN-RECOMMENDED-NOT-NATIONAL-ANNEX"),
            p.Section.Selection.Profile.Coefficients);
        var r=Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3,profile:fake);
        Assert.Equal(DesignCheckStatus.Unsupported,Assert.Single(r.Results).Status);
    }

    [Fact]
    public void ConcurrentG2PureCheckDoesNotApproveG3Interaction()
    {
        var p=Combined();
        var engine=new DesignCheckEngine(new IDesignCheckRule[]
        {
            new Ntc2018TimberCombinedSectionRuleG3(Ntc2018TimberCombinedEffectG3.TensionBending,Materials,Tables,new[]{p}),
            new Ntc2018TimberRectangularSectionRuleG2(Ntc2018TimberPureEffectG2.TensionParallel,
                Materials,Tables,new[]{p.Section})
        });
        var req=Request(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3) with
        {RuleIds=new[]{Ntc2018TimberCombinedSectionRuleG3.TensionBendingId,
            Ntc2018TimberRectangularSectionRuleG2.TensionId}};
        var r=engine.Evaluate(req);
        Assert.Equal(DesignCheckStatus.Passed,r.Results[0].Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,r.Results[1].Status);
        Assert.Equal(DesignOverallStatus.Incomplete,r.OverallStatus);
    }

    [Fact]
    public void ZeroOrMissingLoadComponentsAreNotTreatedAsVerifiedInteractions()
    {
        var p=Combined();
        var r=new DesignCheckEngine(new[]{new Ntc2018TimberCombinedSectionRuleG3(
            Ntc2018TimberCombinedEffectG3.TensionBending,Materials,Tables,new[]{p})});
        var valid=Request(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,0,3);
        foreach(var e in new[] {
            valid.Effects with {ShearForceYKn=null},
            valid.Effects with {AxialForceKn=null},
            valid.Effects with {BendingMomentZKnm=null}
        })
        {
            var result=r.Evaluate(valid with {Effects=e});
            Assert.Equal(DesignCheckStatus.MissingData,Assert.Single(result.Results).Status);
        }
    }

    [Fact]
    public void BendingSignsAndShearSignsAffectTraceButNotTheStressUtilization()
    {
        var p=Combined();
        var r1=Assert.Single(Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,10,3).Results);
        var r2=Assert.Single(Check(p,Ntc2018TimberCombinedEffectG3.TensionBending,20,-10,-3).Results);
        Assert.Equal(r1.Utilization,r2.Utilization);
        Assert.Contains("V=-10 kN",r2.CalculationTrace);
        Assert.Contains("Mz=-3 kNm",r2.CalculationTrace);
    }

    [Fact]
    public void RuleIsNeverAnOverallMemberComplianceStatement()
    {
        var rule=new Ntc2018TimberCombinedSectionRuleG3(Ntc2018TimberCombinedEffectG3.CompressionBending,
            Materials,Tables,new[]{Combined()});
        Assert.False(rule.EstablishesCompleteMemberCompliance);
        Assert.Equal(Ntc2018TimberCoefficientSelectorG1.Edition,rule.Code);
        Assert.Equal(DesignMaterial.Timber,rule.Material);
        Assert.Contains("4.4.7",rule.SourceClause);
    }
}
