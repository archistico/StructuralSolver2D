namespace StructuralSolver2D.Verification.Timber;

public enum Ntc2018ActionCategoryG1 { G1StructuralPermanent, G2NonStructuralPermanent, QVariable, Exceptional }
public enum Ntc2018ActionEffectG1 { Favorable, Unfavorable }

/// <summary>
/// NTC 2018 §2.6.1 Table 2.6.I, A1 (ordinary non-geotechnical STR).
/// Only the gamma_F baseline is selected here: psi0, lead/companion position,
/// signed action contributions and full combination generation are NOT implemented.
/// </summary>
public static class Ntc2018StrA1ActionFactorsG1
{
    public const string Source = "NTC 2018, 2.6.1, Tabella 2.6.I, colonna A1";

    public static double Factor(Ntc2018ActionCategoryG1 category, Ntc2018ActionEffectG1 effect,
        bool g2WellDefinedLikeG1 = false, string? g2ExceptionEvidence = null)
    {
        if (!Enum.IsDefined(category) || !Enum.IsDefined(effect))
            throw new ArgumentOutOfRangeException(nameof(category), "Unknown A1 action type or effect.");
        if (category == Ntc2018ActionCategoryG1.Exceptional)
            throw new NotSupportedException("Exceptional action is not an ordinary STR A1 variable action.");
        if (g2WellDefinedLikeG1 &&
            (category != Ntc2018ActionCategoryG1.G2NonStructuralPermanent ||
             string.IsNullOrWhiteSpace(g2ExceptionEvidence)))
            throw new ArgumentException("G2 defined-value exception must be explicitly evidenced and used only for G2.");
        if (!g2WellDefinedLikeG1 && !string.IsNullOrWhiteSpace(g2ExceptionEvidence))
            throw new ArgumentException("Inconsistent G2 exception evidence.");
        if (g2WellDefinedLikeG1) category = Ntc2018ActionCategoryG1.G1StructuralPermanent;
        return (category, effect) switch
        {
            (Ntc2018ActionCategoryG1.G1StructuralPermanent, Ntc2018ActionEffectG1.Favorable) => 1.00,
            (Ntc2018ActionCategoryG1.G1StructuralPermanent, Ntc2018ActionEffectG1.Unfavorable) => 1.30,
            (Ntc2018ActionCategoryG1.G2NonStructuralPermanent, Ntc2018ActionEffectG1.Favorable) => 0.80,
            (Ntc2018ActionCategoryG1.G2NonStructuralPermanent, Ntc2018ActionEffectG1.Unfavorable) => 1.50,
            (Ntc2018ActionCategoryG1.QVariable, Ntc2018ActionEffectG1.Favorable) => 0.00,
            (Ntc2018ActionCategoryG1.QVariable, Ntc2018ActionEffectG1.Unfavorable) => 1.50,
            _ => throw new NotSupportedException("Unrepresented action factor.")
        };
    }
}

/// <summary>NTC 2018 Tab. 4.2.VII, steel partial factors; not an EC3 Annex identity.</summary>
public static class Ntc2018SteelPartialFactorsG1
{
    public const double GammaM0 = 1.05;
    public const double GammaM1OrdinaryMembers = 1.05;
    public const double GammaM1BridgeMembers = 1.10; // Outside current product scope.
    public const double GammaM2NetTensionFracture = 1.25;
    public const string Source = "NTC 2018, 4.2.4.1.1, Tabella 4.2.VII";
}
