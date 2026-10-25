# P0-B3 — Generatore di combinazioni candidate NTC 2018

**25 ottobre 2026 — gate .NET dichiarato superato dall'utente nel checkpoint successivo («tutti i test passati», senza output numerico allegato).**

Implementazione incrementale della specifica P0-B1, sopra l'inventario tipizzato P0-B2. Fonte primaria fissata al [D.M. 17 gennaio 2018, Gazzetta Ufficiale, §§ 2.5.3 e 2.6.1, tabelle 2.5.I e 2.6.I](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf).

## Cosa fa effettivamente

- Modulo separato `StructuralSolver2D.Combinations`, sola dipendenza da `Core`, nessun cambiamento al risolutore `Analysis`.
- `Ntc2018BuildingActionCoefficients.Psi`: 12 categorie supportate (A–H, vento, neve per due fasce altimetriche, temperatura), con `decimal` e rifiuto esplicito I/K/neve indeterminata.
- `Ntc2018BuildingActionCoefficients.GammaStrA1`: valori `G1` favorevole 1,00 / sfavorevole 1,30; `G2` 0,80 / 1,50; `Q` 0 / 1,50; deroga G2→G1 **solo se** l'inventario attesta esplicitamente la condizione prevista dalla nota alla Tab. 2.6.I.
- `Ntc2018BuildingCombinationGenerator.Generate`: da un inventario già valido e da **uno scenario ingegneristico esplicito** produce una combinazione `StructuralLoadCombination` (fattori non nulli) e un manifesto di ogni contributo effettivo o omesso.
- Famiglie `UlsStrA1`, `SlsRare`, `SlsFrequent`, `SlsQuasiPermanent` da equazioni [2.5.1]–[2.5.4]. La frequente usa ψ₁ della guida e ψ₂ delle accompagnatrici; la quasi permanente usa ψ₂ di tutte senza una guida unica.
- Traccia, per ogni termine, `ActionId`, `LoadCaseId`, ruolo, γ, ψ, fattore risultante, fonte coefficienti, evidenza origine e eventuale deroga G2. I segni dei carichi **rimangono quelli originali del FEM**.
- Omissioni distinte: `FavorableEffect`, `ZeroPsi`, `PhysicallyAbsentByEvidence`, `NotSelectedInScenario`. Queste ultime non attestano completezza: tutti i manifesti del generatore hanno **`FullCombinationSetAudited = false`**.

Il generatore rifiuta `ActionInventory` non valido, ID sconosciuti o duplicati, collisioni di ID FEM, due varianti mutuamente esclusive presenti insieme, scelta guida incompatibile, favorevolezza senza identità dell'effetto e relativa evidenza, dichiarazioni di assenza contraddittorie. Un contributo con ψ=0 si registra nel **manifesto di omissione**, non come termine zero del Core: resta così protetta la regressione E07.

## Esempio sintetico

```csharp
using StructuralSolver2D.Combinations.Generation;

// model e inventory sono gli oggetti validi dell'esempio P0-B2.
Ntc2018CombinationScenario scenario = new(
    CombinationId: "ULS_STR_A1_Q_RES",
    EffectId: "M_POS_MEMBER_AB_STATION_2M",
    Family: Ntc2018CombinationFamily.UlsStrA1,
    PresentActionIds: new[] { "G1_SELF", "Q_A" },
    LeadingActionId: "Q_A",
    FavorableActionIds: Array.Empty<string>(),
    FavorabilityEvidenceId: "ENGINEER_EFFECT_CLASSIFICATION_001");

Ntc2018GeneratedCombination candidate =
    Ntc2018BuildingCombinationGenerator.Generate(inventory, model, scenario);

// candidate.FemCombination: fattori per analisi elastica 2D.
// candidate.Manifest: fattori/evidenze/omissioni, NON un gate normativo.
```

Il generatore **non modifica** `StructuralModel.LoadCombinations`: la scelta se aggiungere, valutare o rifiutare un candidato resta al chiamante. La CLI esistente continua a trattare le sue vecchie combinazioni manuali senza interpretarne i nomi `ULS_*`/`SLS_*` come profili certificati.

## Test e indipendenza numerica

Nuovo file xUnit `tests/StructuralSolver2D.Core.Tests/Combinations/Ntc2018CombinationGeneratorB3Tests.cs`:

1. Carica come risorsa del progetto test **lo stesso** `benchmarks/independent/p0b/P0B1_NTC2018_ACTION_GOLDEN.json` immutato e verifica separatamente i **14 scenari** per fattori, omissioni e somma dell'effetto scalare, confrontando i `decimal` esatti.
2. Confronta la tabella ψ su **12 categorie** con coefficienti indipendentemente fissati dalla fonte primaria, più la tabella γ STR A1.
3. Controlla varianti vento incompatibili, guida mancante, quasi permanente con guida erronea, evidenza di favorevolezza assente, assenza contraddittoria, ID collisione, zero ψ registrato come omissione e categorie I/K non supportate.
4. Controlla che una combinazione generata **non muti** il modello e dichiari sempre la completezza normativa come non ancora valutata.

```powershell
dotnet test .\tests\StructuralSolver2D.Core.Tests\StructuralSolver2D.Core.Tests.csproj -c Release --filter 'FullyQualifiedName~Ntc2018CombinationGeneratorB3Tests' --logger 'console;verbosity=normal'
.\scripts\run-g7-tests.ps1
```

La suite G7 include i test B2 e B3 attraverso Core.Tests. **B3 è stato confermato passato dall'utente dopo la consegna**; l'ambiente che genera gli ZIP continua a non disporre dell'SDK dotnet. Il successivo pacchetto B4 aggiunge altri test, che richiedono ancora una nuova regressione Windows. Il verificatore indipendente Python dei 14 golden e delle 10 perturbazioni può essere rieseguito dal pacchetto.

## Distinzione essenziale: candidato vs inviluppo normativo

Questo codice produce **una combinazione per uno specifico effetto e scenario fornito**, non cerca autonomamente il caso più gravoso di un'asta. La favorevolezza di un carico permanente può cambiare per momento, assiale, spostamento o reazione; non si può dedurre dal segno `GlobalY` del carico. L'insieme delle guide, delle alternative, delle disposizioni spaziali e delle situazioni fisicamente possibili non viene garantito qui: è la responsabilità esplicita di **P0-B4**. P0-C completa successivamente il controllo su stazioni e sollecitazioni concomitanti.

**Non supportato:** EQU/GEO/A2, ponti, geotecnica, sisma, incendio, accidentali complessi, fatica, combinazioni oltre i casi statici delimitati; non esistono 3D, collegamenti, contatto, diaframmi o tiranti tension-only. Nessun risultato viene promosso a `MemberPassed` o a conformità globale. P0-D/E rimangono in roadmap; GUI Avalonia soltanto P3.
