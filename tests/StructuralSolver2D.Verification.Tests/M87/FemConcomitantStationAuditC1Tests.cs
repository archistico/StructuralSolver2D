using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Integration;

namespace StructuralSolver2D.Verification.Tests.M87;

public sealed class FemConcomitantStationAuditC1Tests
{
    private static readonly NormativeProfile Profile = new("SYNTHETIC-C1",
        new DesignCodeEdition("NOT-A-NORM", "TEST", "NONE"), Array.Empty<NormativeCoefficient>());
    private static DesignMemberContext Member() => new("M", DesignMaterial.Steel, "SEC", 4);

    [Fact]
    public void TwoCombinationsAndBothPointLoadSides_MatchSignedConcomitantFemTriples()
    {
        var (model, plan, requests) = Fixture();
        FemConcomitantStationAuditResultC1 audit = FemConcomitantStationAuditorC1.Audit(model, plan, requests);

        Assert.Equal(FemStationAuditStatusC1.MatchedDeclaredStations, audit.Status);
        Assert.Equal(6, audit.MatchedStations);
        Assert.Empty(audit.Issues);
        Assert.False(audit.MemberAdequacyEstablished);
        Assert.False(audit.ContinuousUtilizationMaximumEstablished);
        Assert.False(audit.StationSetExhaustivenessEstablished);
    }

    [Fact]
    public void ExplicitNegativeCombinationFactor_PreservesSignedFemValues()
    {
        var (model, plan, requests) = Fixture();
        model.AddLoadCombination(new StructuralLoadCombination("REVERSE", "Signed reverse", new[]
        {
            new StructuralLoadCombinationTerm("G", 1), new StructuralLoadCombinationTerm("Q", -2)
        }));
        plan = plan with { Stations = plan.Stations.Concat(new[]
        {
            new FemDeclaredStationC1("NEG_FACTOR", DesignEffectTargetKind.LoadCombination, "REVERSE", 0,
                DesignEffectSectionSide.Continuous, DesignLimitState.Ultimate, Profile.ProfileId,
                new[] { "SYNTH-NM" })
        }).ToArray() };
        requests.Add(new FemStationVerificationBridgeM87B().Capture(model, Member(), DesignLimitState.Ultimate,
            Profile, new[] { "SYNTH-NM" },
            new FemStationSelectionM87B(StructuralAnalysisTargetKind.LoadCombination, "REVERSE", 0)));

        var audit = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.MatchedDeclaredStations, audit.Status);
        Assert.Equal(7, audit.MatchedStations);
        Assert.False(audit.MemberAdequacyEstablished);
    }

    [Theory]
    [InlineData("N")]
    [InlineData("V")]
    [InlineData("M")]
    public void TamperingExactlyOneConcomitantForce_IsInvalidEvenIfOtherTwoMatch(string component)
    {
        var (model, plan, requests) = Fixture();
        DesignCheckRequest original = requests[0];
        DesignEffectSnapshot changed = component switch
        {
            "N" => original.Effects with { AxialForceKn = original.Effects.AxialForceKn + 4 },
            "V" => original.Effects with { ShearForceYKn = original.Effects.ShearForceYKn - 9 },
            _ => original.Effects with { BendingMomentZKnm = original.Effects.BendingMomentZKnm + 15 }
        };
        requests[0] = original with { Effects = changed };

        FemConcomitantStationAuditResultC1 audit = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.Invalid, audit.Status);
        Assert.Contains(audit.Issues, x => x.Code == "C1_NONCONCOMITANT_FORCE");
    }

    [Fact]
    public void AxialMaximumCopiedFromDifferentCombination_CannotBeCombinedWithMoment()
    {
        var (model, plan, requests) = Fixture();
        DesignCheckRequest first = requests[0];
        DesignCheckRequest differentCombination = requests[3];
        Assert.NotEqual(first.Effects.AxialForceKn, differentCombination.Effects.AxialForceKn);
        requests[0] = first with
        {
            Effects = first.Effects with { AxialForceKn = differentCombination.Effects.AxialForceKn }
        };
        FemConcomitantStationAuditResultC1 audit = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.Invalid, audit.Status);
        Assert.Contains(audit.Issues, x => x.Code == "C1_NONCONCOMITANT_FORCE");
    }

    [Fact]
    public void MissingAnObligatoryPointLoadSide_IsIncomplete()
    {
        var (model, plan, requests) = Fixture();
        requests.RemoveAt(1);
        FemConcomitantStationAuditResultC1 audit = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.Incomplete, audit.Status);
        Assert.Equal(5, audit.MatchedStations);
        Assert.Contains(audit.Issues, x => x.Code == "C1_REQUIRED_SNAPSHOT_MISSING");
    }

    [Fact]
    public void AdditionalUnplannedStation_IsRejected()
    {
        var (model, plan, requests) = Fixture();
        requests.Add(new FemStationVerificationBridgeM87B().Capture(model, Member(), DesignLimitState.Ultimate,
            Profile, new[] { "SYNTH-NM" },
            new FemStationSelectionM87B(StructuralAnalysisTargetKind.LoadCombination, "C1", 1)));
        FemConcomitantStationAuditResultC1 audit = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.Invalid, audit.Status);
        Assert.Contains(audit.Issues, x => x.Code == "C1_UNSCHEDULED_SNAPSHOT");
    }

    [Fact]
    public void DuplicateRequest_OrDuplicateDeclaredStation_IsInvalid()
    {
        var (model, plan, requests) = Fixture();
        requests.Add(requests[0]);
        var duplicatedResult = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.Invalid, duplicatedResult.Status);
        Assert.Contains(duplicatedResult.Issues, x => x.Code == "C1_SNAPSHOT_DUPLICATED");

        requests.RemoveAt(requests.Count - 1);
        plan = plan with { Stations = plan.Stations.Concat(new[] { plan.Stations[0] with { StationId = "ANOTHER" } }).ToArray() };
        var planResult = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.Invalid, planResult.Status);
        Assert.Contains(planResult.Issues, x => x.Code == "C1_STATION_DUPLICATED");
    }

    [Fact]
    public void RuleSetOrEditionDrift_InvalidatesEvidence()
    {
        var (model, plan, requests) = Fixture();
        requests[0] = requests[0] with { RuleIds = new[] { "DIFFERENT_RULE" } };
        var changedRules = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.Invalid, changedRules.Status);
        Assert.Contains(changedRules.Issues, x => x.Code == "C1_PROVENANCE_OR_RULE_SET_MISMATCH");

        var second = Fixture();
        second.Requests[0] = second.Requests[0] with
        {
            Profile = Profile with { Code = Profile.Code with { EditionId = "OTHER" } }
        };
        var changedEdition = FemConcomitantStationAuditorC1.Audit(second.Model, second.Plan, second.Requests);
        Assert.Equal(FemStationAuditStatusC1.Invalid, changedEdition.Status);
        Assert.Contains(changedEdition.Issues, x => x.Code == "C1_PROVENANCE_OR_RULE_SET_MISMATCH");
    }

    [Fact]
    public void MissingSignedComponent_IsNotAcceptedAsAConcomitantTriple()
    {
        var (model, plan, requests) = Fixture();
        requests[0] = requests[0] with
        {
            Effects = requests[0].Effects with { ShearForceYKn = null }
        };
        var audit = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.Incomplete, audit.Status);
        Assert.Contains(audit.Issues, x => x.Code == "C1_FORCE_TRIPLE_INCOMPLETE");
    }

    [Fact]
    public void MissingExternalEvidence_DoesNotUpgradeDeclaredStationSetToComplete()
    {
        var (model, plan, requests) = Fixture();
        plan = plan with { StationSelectionEvidenceId = "" };
        var audit = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.Incomplete, audit.Status);
        Assert.False(audit.StationSetExhaustivenessEstablished);
        Assert.Contains(audit.Issues, x => x.Code == "C1_EXTERNAL_REVIEW_MISSING");
    }

    [Fact]
    public void AContinuousSectionAtAConcentratedMemberLoad_CannotBeValidated()
    {
        var (model, plan, requests) = Fixture();
        // The fixture starts with x=0 Continuous; the FIRST POINT-LOAD limit
        // is x=2 BeforePointLoad at index 1. Mutating index 0 is a no-op and
        // had incorrectly asserted rejection of a valid smooth section.
        const int pointLoadIndex = 1;
        FemDeclaredStationC1 original = plan.Stations[pointLoadIndex];
        Assert.Equal(2d, original.DistanceMetres);
        Assert.Equal(DesignEffectSectionSide.BeforePointLoad, original.SectionSide);
        plan = plan with
        {
            Stations = plan.Stations.Select(s => s.StationId == original.StationId
                ? s with { SectionSide = DesignEffectSectionSide.Continuous } : s).ToArray()
        };
        requests[pointLoadIndex] = requests[pointLoadIndex] with
        {
            Effects = requests[pointLoadIndex].Effects with
            {
                SectionSide = DesignEffectSectionSide.Continuous
            }
        };
        var audit = FemConcomitantStationAuditorC1.Audit(model, plan, requests);
        Assert.Equal(FemStationAuditStatusC1.Invalid, audit.Status);
        Assert.Contains(audit.Issues, x => x.Code == "C1_FEM_STATION_UNAVAILABLE");
    }

    [Fact]
    public void ContinuousAtSmoothSection_AndOneSidedAtJump_AreBothValid()
    {
        var (model, plan, requests) = Fixture();
        Assert.Equal(0d, plan.Stations[0].DistanceMetres);
        Assert.Equal(DesignEffectSectionSide.Continuous, plan.Stations[0].SectionSide);
        Assert.Equal(2d, plan.Stations[1].DistanceMetres);
        Assert.Equal(DesignEffectSectionSide.BeforePointLoad, plan.Stations[1].SectionSide);
        Assert.Equal(2d, plan.Stations[2].DistanceMetres);
        Assert.Equal(DesignEffectSectionSide.AfterPointLoad, plan.Stations[2].SectionSide);
        Assert.Equal(FemStationAuditStatusC1.MatchedDeclaredStations,
            FemConcomitantStationAuditorC1.Audit(model, plan, requests).Status);
    }

    [Fact]
    public void Truss2D_IsExplicitlyUnsupportedByThisFrameOnlyAudit()
    {
        StructuralModel truss = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0)).AddNode(new StructuralNode("B", 4, 0))
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000))
            .AddSection(new StructuralSection("SEC", "Axial", .003, 0))
            .AddMember(new StructuralMember("M", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("G", "Axial"))
            .AddLoad(StructuralLoad.NodalForce("L", "G", "B", StructuralLoadDirection.GlobalX, 5));
        var plan = new FemMemberStationPlanC1("TRUSS", Member(), Profile.Code,
            new[] { new FemDeclaredStationC1("ONLY", DesignEffectTargetKind.LoadCase, "G", 2,
                DesignEffectSectionSide.Continuous, DesignLimitState.Ultimate, Profile.ProfileId, new[] { "R" }) },
            "EVIDENCE_STATIONS", "EVIDENCE_ACTIONS");

        var audit = FemConcomitantStationAuditorC1.Audit(truss, plan, Array.Empty<DesignCheckRequest>());
        Assert.Equal(FemStationAuditStatusC1.Unsupported, audit.Status);
        Assert.Contains(audit.Issues, x => x.Code == "C1_NON_FRAME_UNSUPPORTED");
    }

    [Fact]
    public void DiscreteSamplesCannotProveInteriorMaximumOfNonlinearUtilization()
    {
        // Independent algebraic counterexample: N(x)=x, M(x)=1-x on [0,1].
        // N and M have their individual maxima at opposite member ends,
        // but eta(x)=|N(x)*M(x)| has its true maximum of 1/4 at x=1/2.
        double Eta(double x) => Math.Abs(x * (1 - x));
        Assert.Equal(0, Math.Max(Eta(0), Eta(1)));
        Assert.Equal(.25, Eta(.5), 12);

        var (model, plan, requests) = Fixture();
        Assert.Equal(FemStationAuditStatusC1.MatchedDeclaredStations,
            FemConcomitantStationAuditorC1.Audit(model, plan, requests).Status);
        Assert.False(FemConcomitantStationAuditorC1.Audit(model, plan, requests)
            .ContinuousUtilizationMaximumEstablished);
    }

    private static (StructuralModel Model, FemMemberStationPlanC1 Plan,
        List<DesignCheckRequest> Requests) Fixture()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000))
            .AddSection(new StructuralSection("SEC", "Beam", .003, .00002))
            .AddMember(new StructuralMember("M", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("G", "Permanent"))
            .AddLoadCase(new StructuralLoadCase("Q", "Variable"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("qg", "G", "M", StructuralLoadDirection.GlobalY, -8))
            .AddLoad(StructuralLoad.PointLoadOnMember("pg", "G", "M", StructuralLoadDirection.GlobalY, -12, .5))
            .AddLoad(StructuralLoad.NodalForce("ng", "G", "B", StructuralLoadDirection.GlobalX, -9))
            .AddLoad(StructuralLoad.UniformDistributedLoad("qq", "Q", "M", StructuralLoadDirection.GlobalY, 4))
            .AddLoad(StructuralLoad.NodalForce("nq", "Q", "B", StructuralLoadDirection.GlobalX, 3))
            .AddLoadCombination(new StructuralLoadCombination("C1", "First", new[]
            {
                new StructuralLoadCombinationTerm("G", 1), new StructuralLoadCombinationTerm("Q", .5)
            }))
            .AddLoadCombination(new StructuralLoadCombination("C2", "Second", new[]
            {
                new StructuralLoadCombinationTerm("G", .8), new StructuralLoadCombinationTerm("Q", 1.5)
            }));

        var stations = new List<FemDeclaredStationC1>();
        var requests = new List<DesignCheckRequest>();
        var bridge = new FemStationVerificationBridgeM87B();
        foreach (string target in new[] { "C1", "C2" })
            foreach ((double x, DesignEffectSectionSide side) in new[]
            {
                (0d, DesignEffectSectionSide.Continuous),
                (2d, DesignEffectSectionSide.BeforePointLoad),
                (2d, DesignEffectSectionSide.AfterPointLoad)
            })
            {
                string id = $"{target}_{x}_{side}";
                stations.Add(new FemDeclaredStationC1(id, DesignEffectTargetKind.LoadCombination, target,
                    x, side, DesignLimitState.Ultimate, Profile.ProfileId, new[] { "SYNTH-NM" }));
                requests.Add(bridge.Capture(model, Member(), DesignLimitState.Ultimate, Profile,
                    new[] { "SYNTH-NM" }, new FemStationSelectionM87B(
                        StructuralAnalysisTargetKind.LoadCombination, target, x, side switch
                        {
                            DesignEffectSectionSide.BeforePointLoad => StructuralSolver2D.Analysis.Results.SectionLimitM78.BeforePointLoad,
                            DesignEffectSectionSide.AfterPointLoad => StructuralSolver2D.Analysis.Results.SectionLimitM78.AfterPointLoad,
                            _ => StructuralSolver2D.Analysis.Results.SectionLimitM78.Continuous
                        })));
            }
        FemMemberStationPlanC1 plan = new("FRAME_M_C1", Member(), Profile.Code, stations,
            "EXTERNAL_REVIEWED_SCREENING_STATIONS", "EXTERNAL_REVIEWED_COMBINATION_PLAN");
        return (model, plan, requests);
    }
}
