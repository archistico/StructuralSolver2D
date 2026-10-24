using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Timber;

/// <summary>Only the explicitly covered categories of NTC 2018 table 2.5.I.</summary>
public enum Ntc2018SleActionCategoryG5
{
    Permanent, ResidentialA, OfficeB, AssemblyC, CommercialD, StorageE,
    LightParkingF, HeavyParkingG, MaintenanceRoofH, Wind,
    SnowAtOrBelow1000Metres, SnowAbove1000Metres, Temperature
}
public enum Ntc2018SleLoadRoleG5 { Permanent, LeadingVariable, AccompanyingVariable }
public enum Ntc2018SleSupportG5 { SimplySupportedSpan, Cantilever }
public enum Ntc2018SleDeflectionEffectG5 { InstantaneousRareVariable, InstantaneousRareTotal, FinalTotal }

/// <summary>A signed, unfactored elastic deflection contribution, from one original FEM load case.</summary>
public sealed record Ntc2018SleLoadContributionG5(
    string LoadCaseId, Ntc2018SleLoadRoleG5 Role, Ntc2018SleActionCategoryG5 Category,
    double SignedElasticDeflectionMm, string SourceFemLoadCaseEvidence);

/// <summary>
/// G5 is an independent SLE profile; ULS gamma_M/kmod and G1 profiles are deliberately excluded.
/// Scope requires explicit near-saturation installation assessment (no implicit dry timber).
/// </summary>
public sealed record Ntc2018TimberSleScenarioG5(
    string MemberId, string SectionId, string AnalysisId, string RareCombinationId,
    string StrengthClassId, int ServiceClass, string ServiceClassEvidence,
    bool? DryOrStabilizedAtInstallationConfirmed, double AdditionalWetKdef,
    string InstallationMoistureEvidence, string ActionCategoryAndPsiEvidence,
    IReadOnlyList<Ntc2018SleLoadContributionG5> LoadCases);

public sealed record Ntc2018TimberSleSelectionG5(
    Ntc2018TimberSleScenarioG5 Scenario, NormativeProfile Profile,
    double Kdef, string Fingerprint);

/// <summary>
/// NTC 2018 §4.4.7 / Table 4.4.V and §2.5.3 / Table 2.5.I;
/// Circolare 2019 C4.4.7 [C4.4.3]. All numbers here are normative coefficients,
/// not a generator of characteristic FEM load combinations or a certification of supplied loads.
/// </summary>
public static class Ntc2018TimberSleSelectorG5
{
    public const string KdefSource = "NTC 2018 §4.4.7, Tabella 4.4.V";
    public const string PsiSource = "NTC 2018 §2.5.3, Tabella 2.5.I";
    public static DesignCodeEdition Edition => Ntc2018TimberCoefficientSelectorG1.Edition;

    public static double ReferenceKdef(int serviceClass) => serviceClass switch
    {
        1 => .6, 2 => .8, 3 => 2.0,
        _ => throw new ArgumentOutOfRangeException(nameof(serviceClass))
    };

    public static (double Psi0, double Psi2) Psi(Ntc2018SleActionCategoryG5 category) => category switch
    {
        Ntc2018SleActionCategoryG5.ResidentialA => (.7, .3),
        Ntc2018SleActionCategoryG5.OfficeB => (.7, .3),
        Ntc2018SleActionCategoryG5.AssemblyC => (.7, .6),
        Ntc2018SleActionCategoryG5.CommercialD => (.7, .6),
        Ntc2018SleActionCategoryG5.StorageE => (1.0, .8),
        Ntc2018SleActionCategoryG5.LightParkingF => (.7, .6),
        Ntc2018SleActionCategoryG5.HeavyParkingG => (.7, .3),
        Ntc2018SleActionCategoryG5.MaintenanceRoofH => (0, 0),
        Ntc2018SleActionCategoryG5.Wind => (.6, 0),
        Ntc2018SleActionCategoryG5.SnowAtOrBelow1000Metres => (.5, 0),
        Ntc2018SleActionCategoryG5.SnowAbove1000Metres => (.7, .2),
        Ntc2018SleActionCategoryG5.Temperature => (.6, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(category), "Not a supported variable action.")
    };

    public static Ntc2018TimberSleSelectionG5 Select(Ntc2018TimberSleScenarioG5 input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.MemberId) || string.IsNullOrWhiteSpace(input.SectionId) ||
            string.IsNullOrWhiteSpace(input.AnalysisId) || string.IsNullOrWhiteSpace(input.RareCombinationId) ||
            string.IsNullOrWhiteSpace(input.StrengthClassId) || string.IsNullOrWhiteSpace(input.ServiceClassEvidence) ||
            string.IsNullOrWhiteSpace(input.InstallationMoistureEvidence) ||
            string.IsNullOrWhiteSpace(input.ActionCategoryAndPsiEvidence))
            throw new ArgumentException("G5 needs complete member, scenario and service-class evidence.");
        double kdef = ReferenceKdef(input.ServiceClass);
        if (input.DryOrStabilizedAtInstallationConfirmed is null || !double.IsFinite(input.AdditionalWetKdef) ||
            (input.DryOrStabilizedAtInstallationConfirmed == true && input.AdditionalWetKdef != 0) ||
            (input.DryOrStabilizedAtInstallationConfirmed == false && input.AdditionalWetKdef < 2))
            throw new ArgumentException("G5 installation moisture must be evidenced; near fibre saturation and drying under load requires additional kdef >=2.");
        kdef += input.AdditionalWetKdef;
        if (!double.IsFinite(kdef)) throw new ArgumentException("Nonfinite total creep coefficient.");
        if (input.LoadCases is null || input.LoadCases.Count < 2)
            throw new ArgumentException("Declare every participating permanent and variable FEM load case.");
        var cases = input.LoadCases.ToArray();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in cases)
        {
            if (c is null || string.IsNullOrWhiteSpace(c.LoadCaseId) ||
                string.IsNullOrWhiteSpace(c.SourceFemLoadCaseEvidence) ||
                !double.IsFinite(c.SignedElasticDeflectionMm) || !Enum.IsDefined(c.Role) ||
                !Enum.IsDefined(c.Category) || !ids.Add(c.LoadCaseId))
                throw new ArgumentException("Invalid, ambiguous, non-finite or undocumented SLE load case.");
            if ((c.Role == Ntc2018SleLoadRoleG5.Permanent) !=
                    (c.Category == Ntc2018SleActionCategoryG5.Permanent))
                throw new ArgumentException("Permanent FEM actions must be distinguished from variables.");
            if (c.Role != Ntc2018SleLoadRoleG5.Permanent) _ = Psi(c.Category);
        }
        if (!cases.Any(x => x.Role == Ntc2018SleLoadRoleG5.Permanent) ||
            cases.Count(x => x.Role == Ntc2018SleLoadRoleG5.LeadingVariable) != 1)
            throw new ArgumentException("First-generation G5 requires a permanent action and exactly one leading variable action.");
        // A cloned read-only list avoids later mutation of caller-owned scenario.LoadCases.
        var scenario = input with { LoadCases = Array.AsReadOnly(cases) };
        string fingerprint = Fingerprint(scenario);
        var profile = new NormativeProfile("NTC2018-TIMBER-SLE-G5:" + fingerprint, Edition,
            Array.AsReadOnly(new[] {
                new NormativeCoefficient("kdef", kdef, KdefSource,
                    "Solid timber / glulam serviceability creep coefficient; independently assessed installation moisture")
            }));
        return new Ntc2018TimberSleSelectionG5(scenario, profile, kdef, fingerprint);
    }

    public static string Fingerprint(Ntc2018TimberSleScenarioG5 scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        static string V(double d) => d.ToString("R", CultureInfo.InvariantCulture);
        var s = new StringBuilder("NTC2018-G5-SLE-v1|");
        // Length prefixes avoid ambiguities with user-provided delimiters.
        void Add(string value) { s.Append(value.Length).Append(':').Append(value); }
        foreach (var part in new[] { scenario.MemberId, scenario.SectionId, scenario.AnalysisId,
                     scenario.RareCombinationId, scenario.StrengthClassId,
                     scenario.ServiceClass.ToString(CultureInfo.InvariantCulture),
                     scenario.DryOrStabilizedAtInstallationConfirmed?.ToString() ?? "null",
                     V(scenario.AdditionalWetKdef) }) Add(part ?? "");
        if (scenario.LoadCases is not null)
            foreach (var c in scenario.LoadCases)
            {
                if (c is null) { Add("null"); continue; }
                foreach (var value in new[] { c.LoadCaseId, c.Role.ToString(), c.Category.ToString(),
                             V(c.SignedElasticDeflectionMm) }) Add(value ?? "");
            }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s.ToString()))).ToLowerInvariant();
    }
}

/// <summary>All physical assertions are external and must be verified by an engineer, not by planar FEM.</summary>
public sealed record Ntc2018TimberSleEvidenceG5(
    Ntc2018TimberSleSelectionG5 Selection, double VerifiedStationMetres,
    DesignEffectSectionSide VerifiedSide, string FemStationEvidence,
    Ntc2018SleSupportG5 Support, string SpanAndSupportEvidence,
    bool? ElasticFemUsesCatalogMeanEAndG, string MeanStiffnessEvidence,
    bool? EachLoadCaseUsesSameLinearModelAndSignedChordDeflection, string LoadCaseConsistencyEvidence,
    bool? CompleteRareCombinationAndPsiValidated, string CombinationEvidence,
    bool? AllJointShearAndSlipEffectsIncludedOrProvenNegligible, string DeformationScopeEvidence,
    bool? GoverningDeflectionStationForRequestedCheckConfirmed, string PeakEvidence,
    bool? HighSustainedBendingRequiresNoFurtherAssessment, string SustainedLoadEvidence,
    double UpwardCamberMm, string CamberEvidence,
    double AdoptedLimitDenominator, string ServiceabilityLimitEvidence);

/// <summary>
/// Three local NTC 2018 SLE deflection screens: rare variable instant, rare total instant,
/// and Circolare [C4.4.3] total final, with a distinct check at a documented station.
/// No deflection envelope, full member design approval, load combination generator or FEM analysis.
/// </summary>
public sealed class Ntc2018TimberSleDeflectionRuleG5 : IDesignCheckRule
{
    public const string VariableId = "NTC2018-TIMBER-SLE-RARE-VARIABLE-DEFLECTION-G5";
    public const string RareTotalId = "NTC2018-TIMBER-SLE-RARE-TOTAL-DEFLECTION-G5";
    public const string FinalId = "NTC2018-TIMBER-SLE-TOTAL-FINAL-DEFLECTION-G5";
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string,Ntc2018TimberSleEvidenceG5> declarations;
    public Ntc2018SleDeflectionEffectG5 Effect { get; }
    public string RuleId => Effect switch
    {
        Ntc2018SleDeflectionEffectG5.InstantaneousRareVariable => VariableId,
        Ntc2018SleDeflectionEffectG5.InstantaneousRareTotal => RareTotalId,
        Ntc2018SleDeflectionEffectG5.FinalTotal => FinalId,
        _ => throw new InvalidOperationException("Unsupported deflection effect")
    };
    public DesignCodeEdition Code => Ntc2018TimberSleSelectorG5.Edition;
    public DesignMaterial Material => DesignMaterial.Timber;
    public DesignLimitState LimitState => DesignLimitState.Serviceability;
    public bool EstablishesCompleteMemberCompliance => false;
    public string SourceClause => Effect == Ntc2018SleDeflectionEffectG5.FinalTotal
        ? "NTC 2018 §4.4.7, Tab. 4.4.V; Circolare 2019 C4.4.7 [C4.4.3]"
        : "NTC 2018 §4.4.7, §2.5.3 [2.5.2]; Circolare 2019 C4.4.7";

    public Ntc2018TimberSleDeflectionRuleG5(Ntc2018SleDeflectionEffectG5 effect,
        CharacteristicMaterialCatalogM88C materials, IEnumerable<Ntc2018TimberSleEvidenceG5> evidence)
    {
        if (!Enum.IsDefined(effect)) throw new ArgumentOutOfRangeException(nameof(effect));
        Effect = effect;
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(evidence);
        var data = new Dictionary<string, Ntc2018TimberSleEvidenceG5>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in evidence)
        {
            var s = e?.Selection?.Scenario;
            if (s is null || !double.IsFinite(e!.VerifiedStationMetres) || e.VerifiedStationMetres < 0 ||
                !Enum.IsDefined(e.VerifiedSide) || string.IsNullOrWhiteSpace(e.FemStationEvidence))
                throw new ArgumentException("G5 requires exact SLE evidence bound to a FEM station.");
            if (!data.TryAdd(Key(s.MemberId,s.AnalysisId,s.RareCombinationId,e.VerifiedStationMetres,e.VerifiedSide),e))
                throw new ArgumentException("Duplicate SLE declarations at the exact same member, target and FEM section.");
        }
        declarations = data;
    }

    public DesignRuleEvaluation Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Member is null || request.Profile is null || request.Effects is null)
            return DesignRuleEvaluation.MissingData("SLE member, profile and FEM result required.");
        if (request.Member.Material != DesignMaterial.Timber || request.LimitState != DesignLimitState.Serviceability)
            return DesignRuleEvaluation.NotApplicable("G5 evaluates timber SLE, not SLU.");
        if (request.Profile.Code != Code)
            return DesignRuleEvaluation.NotApplicable("G5 covers native NTC 2018 only, never EC5.");
        var e = request.Effects;
        if (e.SectionSide is null || !Enum.IsDefined(e.SectionSide.Value) ||
            !declarations.TryGetValue(Key(request.Member.MemberId,e.AnalysisId,e.CombinationId,
                    e.PositionMetres,e.SectionSide.Value), out var proof))
            return DesignRuleEvaluation.MissingData("Matching SLE evidence for exact member / analysis / rare combination / station / side is missing.");
        var selection = proof.Selection;
        Ntc2018TimberSleSelectionG5 reselected;
        try { reselected = Ntc2018TimberSleSelectorG5.Select(selection.Scenario); }
        catch (ArgumentException)
        { return DesignRuleEvaluation.MissingData("Invalid or inconsistent NTC SLE kdef, psi or action selection."); }
        var scenario = reselected.Scenario;
        if (selection.Profile.Code != Code || selection.Fingerprint != reselected.Fingerprint ||
            selection.Kdef != reselected.Kdef || selection.Profile.ProfileId != reselected.Profile.ProfileId ||
            !MatchingProfile(selection.Profile,reselected.Profile) ||
            !MatchingProfile(request.Profile,reselected.Profile) ||
            e.Origin != DesignEffectOrigin.FemAnalysis || e.TargetKind != DesignEffectTargetKind.LoadCombination ||
            !string.Equals(e.MemberId,scenario.MemberId,StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Member.SectionId,scenario.SectionId,StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(e.AnalysisId,scenario.AnalysisId,StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(e.CombinationId,scenario.RareCombinationId,StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Original rare FEM combination and independently selected NTC SLE profile must match.");
        if (!Enum.IsDefined(proof.Support) ||
            string.IsNullOrWhiteSpace(proof.SpanAndSupportEvidence) ||
            proof.ElasticFemUsesCatalogMeanEAndG != true || string.IsNullOrWhiteSpace(proof.MeanStiffnessEvidence) ||
            proof.EachLoadCaseUsesSameLinearModelAndSignedChordDeflection != true ||
            string.IsNullOrWhiteSpace(proof.LoadCaseConsistencyEvidence) ||
            proof.CompleteRareCombinationAndPsiValidated != true ||
            string.IsNullOrWhiteSpace(proof.CombinationEvidence) ||
            proof.AllJointShearAndSlipEffectsIncludedOrProvenNegligible != true ||
            string.IsNullOrWhiteSpace(proof.DeformationScopeEvidence) ||
            proof.GoverningDeflectionStationForRequestedCheckConfirmed != true ||
            string.IsNullOrWhiteSpace(proof.PeakEvidence) ||
            string.IsNullOrWhiteSpace(proof.CamberEvidence) ||
            !double.IsFinite(proof.UpwardCamberMm) || proof.UpwardCamberMm < 0 ||
            !double.IsFinite(proof.AdoptedLimitDenominator) || proof.AdoptedLimitDenominator <= 0 ||
            string.IsNullOrWhiteSpace(proof.ServiceabilityLimitEvidence))
            return DesignRuleEvaluation.MissingData("Mean E/G, linear per-load FEM deflections, joint slip, maximum deflection, support, camber and criterion need independent evidence.");
        if (Effect == Ntc2018SleDeflectionEffectG5.FinalTotal &&
            (proof.HighSustainedBendingRequiresNoFurtherAssessment != true ||
             string.IsNullOrWhiteSpace(proof.SustainedLoadEvidence)))
            return DesignRuleEvaluation.MissingData("Circolare C4.4.7 sustained permanent bending >30% of characteristic resistance requires independent assessment.");
        if (Effect == Ntc2018SleDeflectionEffectG5.InstantaneousRareVariable &&
            proof.AdoptedLimitDenominator < 300 ||
            Effect == Ntc2018SleDeflectionEffectG5.FinalTotal &&
            proof.AdoptedLimitDenominator < 200)
            return DesignRuleEvaluation.MissingData("G5 requires explicit project adoption of NTC recommended L/300 or L/200 or a stricter limit.");
        if (e.DeflectionMetres is not double combinedMetres || !double.IsFinite(combinedMetres))
            return DesignRuleEvaluation.MissingData("FEM rare combination signed chord-relative deflection is required in metres.");
        TimberStrengthClassM88C grade;
        try { grade = materials.FindTimber(scenario.StrengthClassId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown timber strength class for SLE mean stiffness."); }
        if (grade.Kind is not (StrengthMaterialKindM88C.SolidSoftwood or StrengthMaterialKindM88C.HomogeneousGlulam) ||
            !double.IsFinite(grade.ElasticModulusParallelMeanMpa) || grade.ElasticModulusParallelMeanMpa <= 0 ||
            !double.IsFinite(grade.ShearModulusMeanMpa) || grade.ShearModulusMeanMpa <= 0)
            return DesignRuleEvaluation.NotApplicable("G5 supports only intact solid softwood and homogeneous glulam with mean E/G.");
        var cases = scenario.LoadCases;
        double permanent = cases.Where(x => x.Role == Ntc2018SleLoadRoleG5.Permanent)
            .Sum(x => x.SignedElasticDeflectionMm);
        var leading = cases.Single(x => x.Role == Ntc2018SleLoadRoleG5.LeadingVariable);
        double leadingPsi2 = Ntc2018TimberSleSelectorG5.Psi(leading.Category).Psi2;
        var companions = cases.Where(x => x.Role == Ntc2018SleLoadRoleG5.AccompanyingVariable).ToArray();
        double accompaniedRare = companions.Sum(x =>
            x.SignedElasticDeflectionMm * Ntc2018TimberSleSelectorG5.Psi(x.Category).Psi0);
        double rare = permanent + leading.SignedElasticDeflectionMm + accompaniedRare;
        double rareFemMm = combinedMetres * 1000.0;
        if (!double.IsFinite(rare) || !double.IsFinite(rareFemMm) ||
            Math.Abs(rareFemMm - rare) > Math.Max(.000001, 1e-9 * Math.Max(Math.Abs(rare),Math.Abs(rareFemMm))))
            return DesignRuleEvaluation.MissingData("Declared individual FEM load-case deflections do not reproduce the same rare SLE FEM combination.");
        double variable = leading.SignedElasticDeflectionMm + accompaniedRare;
        double final = permanent * (1 + selection.Kdef) +
            leading.SignedElasticDeflectionMm * (1 + leadingPsi2 * selection.Kdef) +
            companions.Sum(x => x.SignedElasticDeflectionMm *
                (Ntc2018TimberSleSelectorG5.Psi(x.Category).Psi0 +
                 Ntc2018TimberSleSelectorG5.Psi(x.Category).Psi2 * selection.Kdef)) - proof.UpwardCamberMm;
        double evaluated = Effect switch
        {
            Ntc2018SleDeflectionEffectG5.InstantaneousRareVariable => variable,
            Ntc2018SleDeflectionEffectG5.InstantaneousRareTotal => rare - proof.UpwardCamberMm,
            Ntc2018SleDeflectionEffectG5.FinalTotal => final,
            _ => throw new InvalidOperationException("Unknown G5 rule effect.")
        };
        double comparableSpanMm = request.Member.MemberLengthMetres * 1000 *
            (proof.Support == Ntc2018SleSupportG5.Cantilever ? 2 : 1);
        double limitMm = comparableSpanMm / proof.AdoptedLimitDenominator;
        double demandMm = Math.Abs(evaluated);
        if (!double.IsFinite(demandMm) || !double.IsFinite(limitMm) || limitMm <= 0)
            return DesignRuleEvaluation.MissingData("Invalid signed deflection or adopted geometric span/limit.");
        static string F(double x) => x.ToString("R",CultureInfo.InvariantCulture);
        string trace = $"G5 NTC 2018 PARTIAL SLE DEFLECTION, NOT MEMBER COMPLIANCE; " +
            $"rule={RuleId}; reference={SourceClause}; code={Code.DocumentId}/{Code.EditionId}; " +
            $"profile={selection.Profile.ProfileId}; sha256={selection.Fingerprint}; member={scenario.MemberId}; " +
            $"section={scenario.SectionId}; analysis={scenario.AnalysisId}; combination={scenario.RareCombinationId}; " +
            $"station={F(e.PositionMetres)} m; side={e.SectionSide}; class={grade.Id}; " +
            $"material_source={grade.ReferenceStandard}/{grade.DataStatus}; Emean={F(grade.ElasticModulusParallelMeanMpa)} MPa; " +
            $"Gmean={F(grade.ShearModulusMeanMpa)} MPa; serviceClass={scenario.ServiceClass}; " +
            $"kdef={F(selection.Kdef)}; dryOrStabilized={scenario.DryOrStabilizedAtInstallationConfirmed}; " +
            $"uPermanent={F(permanent)} mm; uLeading={F(leading.SignedElasticDeflectionMm)} mm; " +
            $"psi2Leading={F(leadingPsi2)}; uAccompanyingRare={F(accompaniedRare)} mm; " +
            $"uRareFEM={F(rareFemMm)} mm; uRareReconstructed={F(rare)} mm; " +
            $"uVariableInstant={F(variable)} mm; uFinalNet={F(final)} mm; " +
            $"camber={F(proof.UpwardCamberMm)} mm; assessedSigned={F(evaluated)} mm; " +
            $"spanForLimit={F(comparableSpanMm)} mm; criterion=L/{F(proof.AdoptedLimitDenominator)}; " +
            $"limitMm={F(limitMm)}; load_sources={string.Join(",",cases.Select(x=>x.LoadCaseId+":"+x.Category))}; " +
            $"limit_evidence={proof.ServiceabilityLimitEvidence}; max_deflection={proof.PeakEvidence}; " +
            $"joint_scope={proof.DeformationScopeEvidence}; sustained_load={proof.SustainedLoadEvidence}. " +
            "NOT VERIFIED: source FEM inputs and load classification, actual joint stiffness, maximum along all spans, " +
            "vibrations, connection details, shrinkage/hygrometric variations, design combination completeness, " +
            "SLU, whole member or construction code compliance.";
        return DesignRuleEvaluation.Calculated(demandMm, limitMm, "mm", trace);
    }

    private static bool MatchingProfile(NormativeProfile a, NormativeProfile b) =>
        a.Code == b.Code && a.ProfileId == b.ProfileId && a.Coefficients?.Count == 1 &&
        b.Coefficients.Count == 1 && a.Coefficients[0] == b.Coefficients[0];

    private static string Key(string member,string analysis,string combination,double x,DesignEffectSectionSide side) =>
        string.Join("\u001f",member,analysis,combination,x.ToString("R",CultureInfo.InvariantCulture),side.ToString());
}
