using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Timber;

namespace StructuralSolver2D.Verification.Tests.M92;

public sealed class Ntc2018TimberServiceabilityG5Tests
{
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory,
            "characteristic-strength-classes.json"));

    // Independent Euler-Bernoulli reference for a simply supported 4 m C24
    // rectangle 100 x 200 mm; Emean=11000 MPa. Each signed load contribution
    // derives from delta=5*q*L^4/(384*E*I), q in N/mm, I in mm^4.
    private static double Elastic(double qKnPerMetre) =>
        5.0 * qKnPerMetre * Math.Pow(4000,4) /
        (384.0 * 11000.0 * (100.0 * Math.Pow(200,3) / 12.0));
    private static readonly double G = Elastic(.5);
    private static readonly double Q = Elastic(.7);
    private static readonly double W = Elastic(.2);

    private static Ntc2018TimberSleScenarioG5 Scenario(int service=1, string grade="C24",
        bool? dry=true, double additionalKdef=0, IReadOnlyList<Ntc2018SleLoadContributionG5>? loads=null) =>
        new("W1", "RECT-100x200", "AN-G5", "SLE-RARE-01", grade, service,
            "TEST verified humidity conditions for service class",dry,additionalKdef,
            "TEST installation moisture measured and drying assessed",
            "TEST independent NTC 2.5.I action categories and psi coefficients; complete rare manifest",
            loads ?? new[] {
                new Ntc2018SleLoadContributionG5("G1", Ntc2018SleLoadRoleG5.Permanent,
                    Ntc2018SleActionCategoryG5.Permanent,G,"TEST signed FEM load case G1"),
                new Ntc2018SleLoadContributionG5("Q1", Ntc2018SleLoadRoleG5.LeadingVariable,
                    Ntc2018SleActionCategoryG5.ResidentialA,Q,"TEST signed FEM live action Q1"),
                new Ntc2018SleLoadContributionG5("W", Ntc2018SleLoadRoleG5.AccompanyingVariable,
                    Ntc2018SleActionCategoryG5.Wind,W,"TEST signed FEM wind action W")
            });
    private static Ntc2018TimberSleEvidenceG5 Evidence(Ntc2018TimberSleScenarioG5? scenario=null,
        double limit=300, double camber=1, bool? mean=true, bool? cases=true,
        bool? complete=true, bool? joints=true, bool? peak=true, bool? sustained=true,
        double station=2, DesignEffectSectionSide side=DesignEffectSectionSide.Continuous,
        Ntc2018SleSupportG5 support=Ntc2018SleSupportG5.SimplySupportedSpan)
    {
        var sel=Ntc2018TimberSleSelectorG5.Select(scenario ?? Scenario());
        return new Ntc2018TimberSleEvidenceG5(sel,station,side,"TEST same signed FEM station and side",
            support,"TEST verified physical support and span",
            mean,"TEST Emean/Gmean including bending and shear",cases,
            "TEST same linear FEM model individual load cases; chord-relative signed deflection",
            complete,"TEST all applied cases; psi0/psi2 and combination rare checked from NTC 2.5.3",
            joints,"TEST joint slip and shear contributions included/independently negligible",
            peak,"TEST maximum over relevant span signed and verified at the station",
            sustained,"TEST sustained permanent stress below Circolare 30% threshold or independently evaluated",
            camber,"TEST measured upward precamber",
            limit,"TEST project engineer adopted serviceability criterion and fragile finish requirements");
    }
    private static DesignCheckResult One(Ntc2018SleDeflectionEffectG5 effect,
        Ntc2018TimberSleEvidenceG5? evidence=null,
        double? femMm=null, double station=2, DesignEffectSectionSide side=DesignEffectSectionSide.Continuous,
        string member="W1", string section="RECT-100x200",string combo="SLE-RARE-01",
        NormativeProfile? profile=null, DesignLimitState state=DesignLimitState.Serviceability,
        DesignEffectOrigin origin=DesignEffectOrigin.FemAnalysis,
        DesignEffectTargetKind targetKind=DesignEffectTargetKind.LoadCombination, double span=4)
    {
        var proof=evidence ?? Evidence();
        var rule=new Ntc2018TimberSleDeflectionRuleG5(effect,Materials,new[]{proof});
        var snapshot = new DesignEffectSnapshot("AN-G5",combo,origin,station,0,0,0,
            (femMm ?? 6.0)/1000.0) { MemberId=member, TargetKind=targetKind,SectionSide=side };
        return Assert.Single(new DesignCheckEngine(new[]{rule}).Evaluate(
            new DesignCheckRequest(new DesignMemberContext(member,DesignMaterial.Timber,section,span),
                state,profile ?? proof.Selection.Profile,snapshot,new[]{rule.RuleId})).Results);
    }

    [Theory]
    [InlineData(1,.6)]
    [InlineData(2,.8)]
    [InlineData(3,2)]
    public void Table44V_ForBothSupportedFamilies_UsesExactlyNtcKdef(int service,double expected)
    {
        Assert.Equal(expected,Ntc2018TimberSleSelectorG5.ReferenceKdef(service));
        Assert.Equal(expected,Ntc2018TimberSleSelectorG5.Select(Scenario(service)).Kdef);
        Assert.Equal(expected,Ntc2018TimberSleSelectorG5.Select(Scenario(service,"GL24h")).Kdef);
    }

    [Theory]
    [InlineData(Ntc2018SleActionCategoryG5.ResidentialA,.7,.3)]
    [InlineData(Ntc2018SleActionCategoryG5.OfficeB,.7,.3)]
    [InlineData(Ntc2018SleActionCategoryG5.AssemblyC,.7,.6)]
    [InlineData(Ntc2018SleActionCategoryG5.CommercialD,.7,.6)]
    [InlineData(Ntc2018SleActionCategoryG5.StorageE,1,.8)]
    [InlineData(Ntc2018SleActionCategoryG5.LightParkingF,.7,.6)]
    [InlineData(Ntc2018SleActionCategoryG5.HeavyParkingG,.7,.3)]
    [InlineData(Ntc2018SleActionCategoryG5.MaintenanceRoofH,0,0)]
    [InlineData(Ntc2018SleActionCategoryG5.Wind,.6,0)]
    [InlineData(Ntc2018SleActionCategoryG5.SnowAtOrBelow1000Metres,.5,0)]
    [InlineData(Ntc2018SleActionCategoryG5.SnowAbove1000Metres,.7,.2)]
    [InlineData(Ntc2018SleActionCategoryG5.Temperature,.6,0)]
    public void NtcTable25I_IsIndependentlyVerified(Ntc2018SleActionCategoryG5 kind,double psi0,double psi2)
    {
        var values=Ntc2018TimberSleSelectorG5.Psi(kind);
        Assert.Equal(psi0,values.Psi0,12);
        Assert.Equal(psi2,values.Psi2,12);
    }

    [Fact]
    public void GoldenC24_RareVariable_ReproducesAnalyticalEulerBernoulliAndAdoptedLimit()
    {
        Assert.Equal(25.0/11.0,G,11);
        Assert.Equal(35.0/11.0,Q,11);
        Assert.Equal(10.0/11.0,W,11);
        var r=One(Ntc2018SleDeflectionEffectG5.InstantaneousRareVariable);
        Assert.Equal(DesignCheckStatus.Passed,r.Status);
        Assert.Equal(Q+.6*W,Assert.IsType<double>(r.Demand),10);
        Assert.Equal(4000.0/300,Assert.IsType<double>(r.Resistance),10);
        Assert.Contains("uVariableInstant=",r.CalculationTrace);
        Assert.Contains("uRareFEM=6",r.CalculationTrace);
    }

    [Fact]
    public void GoldenC24_FinalTotal_UsesDifferentCreepForPermanentLeadingAndAccompanying()
    {
        var r=One(Ntc2018SleDeflectionEffectG5.FinalTotal,Evidence(limit:200));
        var expected=G*1.6 + Q*(1+.3*.6) + W*(.6+0*.6)-1;
        Assert.Equal(6.936363636363636,expected,10);
        Assert.Equal(DesignCheckStatus.Passed,r.Status);
        Assert.Equal(expected,Assert.IsType<double>(r.Demand),10);
        Assert.Equal(20.0,Assert.IsType<double>(r.Resistance),10);
        Assert.Contains("uFinalNet=",r.CalculationTrace);
        Assert.Contains("psi2Leading=0.3",r.CalculationTrace);
        Assert.Contains("NOT VERIFIED:",r.CalculationTrace);
    }

    [Fact]
    public void InstantTotal_IsSeparateProposedCriterion_NotAutomaticNtcL300()
    {
        var r=One(Ntc2018SleDeflectionEffectG5.InstantaneousRareTotal,Evidence(limit:400));
        Assert.Equal(DesignCheckStatus.Passed,r.Status);
        Assert.Equal(5.0,Assert.IsType<double>(r.Demand),10);
        Assert.Equal(10.0,Assert.IsType<double>(r.Resistance),10);
    }

    [Fact]
    public void StrongerOrLargerLoadsFail_WithoutPretendingThatSleIsUltimateResistance()
    {
        var large=Scenario(loads: new[] {
            new Ntc2018SleLoadContributionG5("G1",Ntc2018SleLoadRoleG5.Permanent,Ntc2018SleActionCategoryG5.Permanent,Elastic(2),"G source"),
            new Ntc2018SleLoadContributionG5("Q1",Ntc2018SleLoadRoleG5.LeadingVariable,Ntc2018SleActionCategoryG5.ResidentialA,Elastic(3),"Q source"),
            new Ntc2018SleLoadContributionG5("W",Ntc2018SleLoadRoleG5.AccompanyingVariable,Ntc2018SleActionCategoryG5.Wind,Elastic(1),"W source")
        });
        var r=One(Ntc2018SleDeflectionEffectG5.FinalTotal,Evidence(large,limit:200),
            femMm:Elastic(2)+Elastic(3)+.6*Elastic(1));
        Assert.Equal(DesignCheckStatus.Failed,r.Status);
        Assert.True(Assert.IsType<double>(r.Utilization)>1);
    }

    [Fact]
    public void ReversingWindSign_ChangesTheSignedRareAndFinalContributions()
    {
        var reversed=Scenario(loads:new[] {
            new Ntc2018SleLoadContributionG5("G1",Ntc2018SleLoadRoleG5.Permanent,Ntc2018SleActionCategoryG5.Permanent,G,"G source"),
            new Ntc2018SleLoadContributionG5("Q1",Ntc2018SleLoadRoleG5.LeadingVariable,Ntc2018SleActionCategoryG5.ResidentialA,Q,"Q source"),
            new Ntc2018SleLoadContributionG5("W",Ntc2018SleLoadRoleG5.AccompanyingVariable,Ntc2018SleActionCategoryG5.Wind,-W,"W upward source")
        });
        var r=One(Ntc2018SleDeflectionEffectG5.FinalTotal,Evidence(reversed,limit:200),
            femMm:G+Q-.6*W);
        Assert.Equal(DesignCheckStatus.Passed,r.Status);
        Assert.Equal(G*1.6+Q*(1+.3*.6)-.6*W-1,Assert.IsType<double>(r.Demand),10);
    }

    [Fact]
    public void SnowAbove1000MetresAccompanimentUsesBothPsi0AndPsi2InFinal()
    {
        var snow=Scenario(loads: new[] {
            new Ntc2018SleLoadContributionG5("G1",Ntc2018SleLoadRoleG5.Permanent,
                Ntc2018SleActionCategoryG5.Permanent,G,"TEST G"),
            new Ntc2018SleLoadContributionG5("Q1",Ntc2018SleLoadRoleG5.LeadingVariable,
                Ntc2018SleActionCategoryG5.ResidentialA,Q,"TEST Q1"),
            new Ntc2018SleLoadContributionG5("S",Ntc2018SleLoadRoleG5.AccompanyingVariable,
                Ntc2018SleActionCategoryG5.SnowAbove1000Metres,W,"TEST site altitude >1000 m")
        });
        var rare=G+Q+.7*W;
        var expected=G*1.6+Q*1.18+W*(.7+.2*.6)-1;
        var r=One(Ntc2018SleDeflectionEffectG5.FinalTotal,Evidence(snow,limit:200),femMm:rare);
        Assert.Equal(DesignCheckStatus.Passed,r.Status);
        Assert.Equal(expected,Assert.IsType<double>(r.Demand),10);
    }

    [Fact]
    public void ZeroPsi0CompanionIsPreservedInSleFingerprintButNotRareDeflection()
    {
        var roof=Scenario(loads: new[] {
            new Ntc2018SleLoadContributionG5("G1",Ntc2018SleLoadRoleG5.Permanent,
                Ntc2018SleActionCategoryG5.Permanent,G,"TEST G"),
            new Ntc2018SleLoadContributionG5("Q1",Ntc2018SleLoadRoleG5.LeadingVariable,
                Ntc2018SleActionCategoryG5.ResidentialA,Q,"TEST Q1"),
            new Ntc2018SleLoadContributionG5("H",Ntc2018SleLoadRoleG5.AccompanyingVariable,
                Ntc2018SleActionCategoryG5.MaintenanceRoofH,W,"TEST roof maintenance category H")
        });
        var r=One(Ntc2018SleDeflectionEffectG5.FinalTotal,Evidence(roof,limit:200),femMm:G+Q);
        Assert.Equal(DesignCheckStatus.Passed,r.Status);
        Assert.Equal(G*1.6+Q*1.18-1,Assert.IsType<double>(r.Demand),10);
        Assert.Contains("H:MaintenanceRoofH",r.CalculationTrace);
    }

    [Fact]
    public void Class3CreepAndWetInstallationIncreaseFinalDeformation()
    {
        var dry3=One(Ntc2018SleDeflectionEffectG5.FinalTotal,Evidence(Scenario(service:3),limit:200));
        var wet=One(Ntc2018SleDeflectionEffectG5.FinalTotal,
            Evidence(Scenario(dry:false,additionalKdef:2.0),limit:200));
        var baseCase=One(Ntc2018SleDeflectionEffectG5.FinalTotal,Evidence(limit:200));
        Assert.True(Assert.IsType<double>(dry3.Demand)>Assert.IsType<double>(baseCase.Demand));
        Assert.True(Assert.IsType<double>(wet.Demand)>Assert.IsType<double>(baseCase.Demand));
        Assert.Equal(2.6,Ntc2018TimberSleSelectorG5.Select(Scenario(dry:false,additionalKdef:2)).Kdef,12);
    }

    [Theory]
    [InlineData(1, true, .1)]
    [InlineData(1, false, 0)]
    [InlineData(1, false, 1.99)]
    [InlineData(1, null, 0)]
    [InlineData(3, false, double.NaN)]
    public void InvalidInstallationMoistureFailsClosed(int service,bool? dry,double addition)
    {
        Assert.Throws<ArgumentException>(()=>Ntc2018TimberSleSelectorG5.Select(
            Scenario(service,dry:dry,additionalKdef:addition)));
    }

    [Fact]
    public void MissingOrDuplicateLoadcasesAndBadRoleCannotGenerateSleProfile()
    {
        var p=Scenario();
        Assert.Throws<ArgumentException>(()=>Ntc2018TimberSleSelectorG5.Select(p with {LoadCases=Array.Empty<Ntc2018SleLoadContributionG5>()}));
        Assert.Throws<ArgumentException>(()=>Ntc2018TimberSleSelectorG5.Select(p with {LoadCases=new[] {p.LoadCases[0],p.LoadCases[0],p.LoadCases[1]}}));
        Assert.Throws<ArgumentException>(()=>Ntc2018TimberSleSelectorG5.Select(p with {LoadCases=new[] {p.LoadCases[0],p.LoadCases[1] with{Role=Ntc2018SleLoadRoleG5.Permanent}}}));
        Assert.Throws<ArgumentException>(()=>Ntc2018TimberSleSelectorG5.Select(p with {LoadCases=new[] {p.LoadCases[0],p.LoadCases[1],p.LoadCases[1] with{LoadCaseId="Q2"}}}));
    }

    [Fact]
    public void SLESelectorDoesNotUseG1UlsGammaMOrKmod()
    {
        var selected=Ntc2018TimberSleSelectorG5.Select(Scenario());
        Assert.Single(selected.Profile.Coefficients);
        Assert.Equal("kdef",selected.Profile.Coefficients[0].Id);
        Assert.Equal(Ntc2018TimberSleSelectorG5.Edition, selected.Profile.Code);
        Assert.DoesNotContain(selected.Profile.Coefficients,x=>x.Id=="gammaM" || x.Id=="kmod");
        Assert.False(new Ntc2018TimberSleDeflectionRuleG5(
            Ntc2018SleDeflectionEffectG5.FinalTotal,Materials,new[]{Evidence()}).EstablishesCompleteMemberCompliance);
    }

    [Fact]
    public void AlteredFingerprintProfileOrCoefficientCannotProducePass()
    {
        var sel=Ntc2018TimberSleSelectorG5.Select(Scenario());
        var forged=sel.Profile with {Coefficients=new[]{new NormativeCoefficient("kdef",.01,
            Ntc2018TimberSleSelectorG5.KdefSource,"forged")}};
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,
            Evidence(limit:200),profile:forged).Status);
        var e=Evidence(limit:200);
        var bad=e with {Selection=e.Selection with {Kdef=.01}};
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,bad).Status);
        var mutable=e.Selection.Scenario.LoadCases.ToArray();
        mutable[1]=mutable[1] with {SignedElasticDeflectionMm=777};
        var mutated=e with {Selection=e.Selection with {Scenario=e.Selection.Scenario with {LoadCases=mutable}}};
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,mutated).Status);
    }

    [Fact]
    public void SLECannotReuseWrongFemCombinationStationMemberSectionOrSide()
    {
        var p=Evidence(limit:200);
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,p,station:1.999).Status);
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,p,member:"W2").Status);
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,p,combo:"SLE-RARE-02").Status);
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,p,
            side:DesignEffectSectionSide.BeforePointLoad).Status);
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,p,section:"WRONG").Status);
    }

    [Theory]
    [InlineData(false,true,true,true,true)]
    [InlineData(true,false,true,true,true)]
    [InlineData(true,true,false,true,true)]
    [InlineData(true,true,true,false,true)]
    [InlineData(true,true,true,true,false)]
    public void MissingMeanStiffnessLoadcasesPsiJointSlipOrPeak_IsNotPass(
        bool mean,bool cases,bool complete,bool joints,bool peak)
    {
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.InstantaneousRareVariable,
            Evidence(mean:mean,cases:cases,complete:complete,joints:joints,peak:peak)).Status);
    }

    [Fact]
    public void SignificantSustainedBendingNeedsAssessment_BeforeFinalDeflection()
    {
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,
            Evidence(limit:200,sustained:false)).Status);
        Assert.Equal(DesignCheckStatus.Passed,One(Ntc2018SleDeflectionEffectG5.InstantaneousRareVariable,
            Evidence(limit:300,sustained:false)).Status);
    }

    [Fact]
    public void InvalidOrUnapprovedLimitNeverBecomesPassingSle()
    {
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.InstantaneousRareVariable,
            Evidence(limit:299)).Status);
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,
            Evidence(limit:199)).Status);
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,
            Evidence(limit:double.NaN)).Status);
    }

    [Fact]
    public void RareFemSumMustMatchIndividualLoadcases_NotIndependentEnvelope()
    {
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,
            Evidence(limit:200),femMm:6.001).Status);
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.InstantaneousRareVariable,
            Evidence(),femMm:-6).Status);
    }

    [Fact]
    public void SourceAnalysisAndKindMustBeFemSleRare_NotExternalOrUls()
    {
        var proof=Evidence(limit:200);
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,proof,
            origin:DesignEffectOrigin.ExternalManual).Status);
        Assert.Equal(DesignCheckStatus.MissingData,One(Ntc2018SleDeflectionEffectG5.FinalTotal,proof,
            targetKind:DesignEffectTargetKind.LoadCase).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,One(Ntc2018SleDeflectionEffectG5.FinalTotal,proof,
            state:DesignLimitState.Ultimate).Status);
    }

    [Fact]
    public void DifferentMeanModulusWoodFamiliesDoNotSilentlyReplaceFEMChecks()
    {
        var gl=One(Ntc2018SleDeflectionEffectG5.FinalTotal,
            Evidence(Scenario(grade:"GL24h"),limit:200));
        Assert.Equal(DesignCheckStatus.Passed,gl.Status);
        Assert.Contains("class=GL24h",gl.CalculationTrace);
        Assert.Contains("Emean=11500",gl.CalculationTrace);
        var unknown=One(Ntc2018SleDeflectionEffectG5.FinalTotal,
            Evidence(Scenario(grade:"XXX"),limit:200));
        Assert.Equal(DesignCheckStatus.MissingData,unknown.Status);
    }

    [Fact]
    public void CantileverRequiresAdoptedDoubleProjectionAsDenominatorSpan()
    {
        var p=Evidence(limit:300,support:Ntc2018SleSupportG5.Cantilever);
        var r=One(Ntc2018SleDeflectionEffectG5.InstantaneousRareVariable,p);
        Assert.Equal(8000.0/300,Assert.IsType<double>(r.Resistance),10);
        Assert.Contains("spanForLimit=8000",r.CalculationTrace);
    }

    [Fact]
    public void ProfileFingerprintIsDeterministicAndDifferentiatesActions()
    {
        var a=Ntc2018TimberSleSelectorG5.Select(Scenario());
        var b=Ntc2018TimberSleSelectorG5.Select(Scenario());
        Assert.Equal(a.Fingerprint,b.Fingerprint);
        var c=Ntc2018TimberSleSelectorG5.Select(Scenario(service:2));
        Assert.NotEqual(a.Fingerprint,c.Fingerprint);
        var q=Scenario();
        var changed=q with {LoadCases=q.LoadCases.Select(x=>x.LoadCaseId=="Q1"
            ? x with {SignedElasticDeflectionMm=x.SignedElasticDeflectionMm+.00001}:x).ToArray()};
        Assert.NotEqual(a.Fingerprint,Ntc2018TimberSleSelectorG5.Select(changed).Fingerprint);
    }

    [Fact]
    public void FailedSleAloneDoesNotClaimMemberCompliance_AndAbsentChecksRemainIncomplete()
    {
        var rule=new Ntc2018TimberSleDeflectionRuleG5(Ntc2018SleDeflectionEffectG5.FinalTotal,
            Materials,new[]{Evidence(limit:200)});
        var e=new DesignEffectSnapshot("AN-G5","SLE-RARE-01",DesignEffectOrigin.FemAnalysis,2,0,0,0,.006)
        {MemberId="W1",TargetKind=DesignEffectTargetKind.LoadCombination,
        SectionSide=DesignEffectSectionSide.Continuous};
        var report=new DesignCheckEngine(new[]{rule}).Evaluate(new DesignCheckRequest(
            new DesignMemberContext("W1",DesignMaterial.Timber,"RECT-100x200",4),
            DesignLimitState.Serviceability,Evidence(limit:200).Selection.Profile,e,
            new[]{rule.RuleId,"NTC-UNIMPLEMENTED-VIBRATION"}));
        Assert.Equal(DesignOverallStatus.Incomplete,report.OverallStatus);
        Assert.Contains(report.Results,x=>x.Status==DesignCheckStatus.Unsupported);
        Assert.False(rule.EstablishesCompleteMemberCompliance);
    }
}
