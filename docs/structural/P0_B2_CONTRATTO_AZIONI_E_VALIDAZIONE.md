# P0-B2 — Azioni tipizzate e validazione separata dal FEM

**25 ottobre 2026. Stato aggiornato dopo la consegna: GATE P0-B2 dichiarato PASS dall'utente («test passati»), senza contatori di log allegati.** Lo stato di preparazione precedente sotto è storico; non è una verifica della completezza SLU/SLE.

Prima integrazione concreta di P0-B1, senza generazione automatica di coefficienti o nuove analisi. Il codice aggiunto è isolato in `src/StructuralSolver2D.Combinations/`, con una **sola dipendenza di produzione** verso `StructuralSolver2D.Core`. Nessun riferimento inverso in Core/Analysis/Verification/Reporting/CLI. La suite xUnit aggiunta è nel progetto Core.Tests per essere eseguita anche da `scripts/run-g7-tests.ps1` dopo l'inclusione del nuovo riferimento nella soluzione.

## Interfaccia pubblica effettiva

| Oggetto/metodo C# | Compito | Cosa **non** fa |
|---|---|---|
| `ActionNature` | G1, G2, Q espliciti | Non sceglie γ favorevole/sfavorevole |
| `VariableActionCategory` | Categorie tabella ψ, vento, neve con fascia altimetrica, I/K speciali | Non determina la categoria dal nome dell'asta/edificio |
| `ActionValueBasis` | Distingue valori caratteristici, nominali e di progetto | Non trasforma un valore nominale in caratteristico |
| `ActionDefinition` | Identità univoca, Core `LoadCaseId`, natura, categoria, evidenza origine/omogeneità, alternative, metadati per legno e deroga G2 documentata | Non garantisce che l'evidenza esterna sia vera o che il caso sia effettivamente omogeneo |
| `ActionInventory` | Sidecar versionato `ntc2018-actions-v1` / `NTC2018:2018-01-17` | Non modifica il modello FEM, non inventa combinazioni normative |
| `ActionInventoryValidator.Validate(inventory, structuralModel)` | Controlla schema, edizione, duplicati, rispondenza ID FEM, copertura inventario, categoria, basi caratteristiche, anomalie e gruppi | Non certifica tutte le distribuzioni di carico e non produce risultati FEM |
| `ActionInventoryJsonSerializer` | Round-trip JSON sidecar *separato* dal vecchio input della CLI; nomi di proprietà camelCase ed enum stringa | Non cambia il formato JSON dei quattro modelli G8B e non registra combinazioni in Core |

Gli stati dell'inventario sono `Valid`, `Incomplete`, `Unsupported`, `Invalid`, con precedenza conservativa **Invalid > Unsupported > Incomplete > Valid**. Sono **stati di validazione del contratto**, distinti da `Passed`/`Failed` delle regole verificatrici. Un input `Valid` non equivale a `CombinationSetComplete` o `MemberPassed`.

La presenza di un `ActionDefinition` per ogni `LoadCaseId` nel Core non prova la completezza delle azioni fisiche su un edificio: l'operatore deve ancora dichiarare le azioni pertinenti, il loro valore caratteristico e scenari alternativi. Non si può derivare favorevolezza dal segno numerico di un carico né riconoscere in modo affidabile G1/G2/Q da un'etichetta: il codice non lo tenta.

## Esempio C# minimale, solo inventario

```csharp
using StructuralSolver2D.Combinations.Actions;
using StructuralSolver2D.Core.Model;

StructuralModel model = new StructuralModel()
    .AddLoadCase(new StructuralLoadCase("G_SELF", "Peso proprio"))
    .AddLoadCase(new StructuralLoadCase("Q_HOME", "Uso residenziale"));

ActionInventory sidecar = new(
    ActionInventoryProfiles.SchemaVersion,
    ActionInventoryProfiles.CodeEdition,
    new[]
    {
        new ActionDefinition("G1_SELF", "G_SELF", ActionNature.G1, null,
            ActionValueBasis.Characteristic, "CALCOLO_PESI", "OMOGENEITA_G_SELF"),
        new ActionDefinition("Q_A", "Q_HOME", ActionNature.Q,
            VariableActionCategory.ResidentialA,
            ActionValueBasis.Characteristic, "DESTINAZIONE_RESIDENZIALE", "OMOGENEITA_Q_HOME")
    });

ActionInventoryValidationResult validation = ActionInventoryValidator.Validate(sidecar, model);
// validation.IsValid: true = metadata coerenti nel perimetro B2, NON combinazioni complete.
string json = ActionInventoryJsonSerializer.Serialize(sidecar);
```

## Diagnosi principali e test previsti

| Diagnosi | Stato | Esempio |
|---|---|---|
| `B2_DUPLICATE_ACTION_ID`, `B2_DUPLICATE_LOAD_CASE` | Invalid | Due azioni anche solo diverse per maiuscole associate allo stesso ID/caso |
| `B2_UNKNOWN_LOAD_CASE` | Invalid | ID non presente nel modello FEM |
| `B2_LOAD_CASE_UNCLASSIFIED` | Incomplete | Caso FEM privo di tipizzazione/attestazione |
| `B2_SOURCE_EVIDENCE_MISSING`, `B2_HOMOGENEITY_EVIDENCE_MISSING` | Incomplete | Carico senza origine o senza dichiarazione di omogeneità |
| `B2_VARIABLE_CATEGORY_MISSING` | Invalid | Azione Q senza categoria ψ determinabile |
| `B2_CATEGORY_CASE_BY_CASE` | Unsupported | Destinazioni I/K richiedono coefficienti definiti caso per caso |
| `B2_SNOW_ELEVATION_MISSING` | Incomplete | Neve senza fascia altimetrica |
| `B2_NONCHARACTERISTIC_BASIS` | Unsupported | Si tenta di trattare un caso già di progetto come caratteristico |
| `B2_ALTERNATIVE_INCOMPLETE` | Incomplete | Solo vento + di un gruppo vento ± dichiarato |
| `B2_ALTERNATIVE_CATEGORY_MISMATCH` | Invalid | Vento e neve forzati a essere alternative dello stesso fenomeno |
| `B2_G2_EXCEPTION_EVIDENCE_MISSING` | Incomplete | Deroga al coefficiente G2 richiesta senza evidenza |

Test xUnit aggiunti in `tests/StructuralSolver2D.Core.Tests/Combinations/ActionInventoryB2Tests.cs`: casi positivi e negativi per tutti i punti principali, round-trip JSON stretto, enum sconosciuti/numerici rifiutati e tutela del comportamento **manuale** preesistente (fattore negativo permesso, zero rifiutato). Il progetto nuovo viene incluso nella soluzione e nel progetto Core.Tests; lo script G7 esistente **non viene alterato**.

## Gate di accettazione e limiti dichiarati

Da Windows, nella root dopo estrazione pulita:

```powershell
dotnet test .\tests\StructuralSolver2D.Core.Tests\StructuralSolver2D.Core.Tests.csproj -c Release --filter 'FullyQualifiedName~ActionInventoryB2Tests' --logger 'console;verbosity=normal'
.\scripts\run-g7-tests.ps1
```

**Aggiornamento:** l'utente ha confermato che i test richiesti per P0-B2 sono passati. Non sono disponibili contatori dettagliati del log Windows; l'ambiente di preparazione continua a non disporre dell'SDK .NET. P0-B3 richiede un nuovo gate separato e P0-B4 completerà l'audit degli insiemi di combinazioni. La prova P0-B1 indipendente Python rimane valida nel proprio perimetro.

**Fuori ambito B2:** tabelle γ/ψ dentro C#, selezione delle guide e delle accompagnatrici, rami favorevoli/sfavorevoli, omit/fingerprint normativo, patch della CLI esistente, avvisi sulla stabilità spaziale/collegamenti, verifica completa EC3/EC5/NTC, soluzione FEM 3D, contatto, tension-only e GUI. Tutto ciò resta in B3/B4, C/E/D o fuori scopo. Il prossimo incremento deve usare i golden P0-B1 senza modificarli per accomodare l'implementazione.
