# M91-D — Audit della copertura delle verifiche in acciaio

**24 ottobre 2026 — modulo di reportistica tecnica, NON una nuova formula EC3 e NON una dichiarazione di conformità.**

## Obiettivo

Le regole M89–M91 forniscono esiti **locali** e **parziali**. Una trave con flessione elastica soddisfatta potrebbe non soddisfare l'instabilità latero-torsionale; una colonna con sezione resistente potrebbe fallire per instabilità. Inoltre, alcune regole accettano soltanto azioni pure, classi e schemi di vincolo molto limitati.

`SteelMemberCoverageAuditorM91D` riceve una **checklist dichiarata dall'utilizzatore** e i `DesignCheckReport` già calcolati da M87/M89/M90/M91. L'audit non esegue calcoli di resistenza; confronta le evidenze ricevute con ogni obbligo atteso.

## Perimetro dell'audit

La singola voce `SteelCoverageObligationM91D` identifica univocamente:

- `ObligationId` e famiglia di controllo (`SteelCheckAreaM91D`);
- `RuleId` corrispondente a una delle sette regole M89–M91 già implementate;
- `AnalysisId`, `CombinationId`, `TargetKind`;
- `StationMetres` e `SectionSide` (`Continuous`, `BeforePointLoad`, `AfterPointLoad`).

L'audit richiede inoltre **stessa asta, sezione, lunghezza, norma, edizione, annesso e stato limite**. Per una sezione con carico concentrato, non scambia il lato prima con quello dopo; due sezioni a quote quasi uguali non sono considerate identiche, perché un arrotondamento potrebbe mescolare limiti monolaterali differenti.

Il piano richiede motivazione esplicita della selezione di combinazioni e sezioni critiche e conferma di revisione **esterna**. Queste affermazioni sono metadati forniti dal progettista, **non sono verificate dal software**.

## Classi di controllo previste

- Snervamento lordo a trazione; rottura della sezione netta (non implementata).
- Compressione della sezione; flessione elastica secondo asse forte; taglio nel piano.
- Interazione elastica N–M–V entro l'ambito ristretto di M90-C.
- Instabilità flessionale; instabilità latero-torsionale.
- Collegamenti; esercizio SLE; altri requisiti da valutare (NON coperti da M91-D).

Ogni famiglia deve essere presente nel programma di controllo oppure in una **esclusione motivata con riferimento esterno**. L'esclusione dichiara solo che il controllo è fuori dal perimetro scelto; non prova che esso sia fisicamente o normativamente non necessario. Sono sempre possibili prescrizioni ulteriori non elencate (incendio, sismica, fatica, torsione, instabilità locali, concentrazioni di carico, fasi costruttive, durabilità, ecc.).

## Esiti

| Esito | Significato | Può dichiarare conformità EC3/NTC? |
|---|---|---|
| `ChecksPassedWithinDeclaredScope` | Evidenze numeriche valide per tutte le voci del piano; nessuna categoria esclusa | **No** |
| `ChecksPassedWithExplicitExclusions` | Evidenze numeriche valide per i controlli previsti; altri controlli esplicitamente esclusi | **No** |
| `FailedWithinDeclaredScope` | Almeno un risultato non soddisfa la verifica richiesta, o emerge un risultato fallito aggiuntivo | **No** |
| `Incomplete` | Mancano risultati, identificativi, dati, review esterna, categorie dichiarate o vi sono duplicazioni/contraddizioni | **No** |

**La priorità è:** non soddisfatto > incompleto > controlli dichiarati soddisfatti. Nessun esito è chiamato `MemberApproved`, `NormativeCompliance`, `StructurePassed` o simili.

## Sicurezze specifiche

1. Controlla la corrispondenza di norma, **edizione** e **annesso nazionale**. Solo `EN 1993-1-1:2005+A1:2014` è censita con le regole correnti.
2. Abbina soltanto risultati FEM, non quelli inseriti manualmente come se avessero provenienza FEM.
3. Rifiuta conferme duplicate di una medesima obbligazione, senza selezionare arbitrariamente il risultato migliore.
4. Non perde esiti non previsti dal piano: un risultato aggiuntivo non soddisfatto rimane un fallimento da approfondire; un risultato aggiuntivo soddisfatto impedisce comunque di dichiarare completa la lista fornita.
5. Verifica numeri finiti, domanda >= 0, resistenza > 0, sfruttamento coerente con il loro rapporto, riferimento e traccia numerica.
6. Controlla il corretto accoppiamento tra la famiglia di verifica e l'identificativo della **regola già nota**, senza ammettere un `RuleId` arbitrario.
7. Convalida la lista, i duplicati e le esclusioni; nessuna categoria omessa è interpretata come non applicabile.

## API

```csharp
// Il progetto chiamante ha già elaborato il FEM e i report DesignCheckEngine.
var plan = new SteelMemberCoveragePlanM91D(
    PlanId: "TRAVE1-REV01", Member: member, Code: code,
    LimitState: DesignLimitState.Ultimate,
    Obligations: obligations,
    Exclusions: exclusions,
    AnalysisTargetSelectionEvidence: "Revisione esterna delle combinazioni: ...",
    CriticalStationSelectionEvidence: "Revisione esterna delle sezioni: ...",
    CompletenessExternallyReviewed: true);

var audit = SteelMemberCoverageAuditorM91D.Audit(plan, designReports);
string markdown = SteelCoverageMarkdownM91D.Render(audit);
// Scrittura file lasciata al componente chiamante / futura reportistica GUI.
```

Nel progetto `tests/StructuralSolver2D.Verification.Tests/M91/SteelMemberCoverageAuditorM91DTests.cs` è presente anche un percorso reale: `DesignCheckEngine` con una regola M89-A genera il `DesignCheckReport`, letto successivamente dall'auditor M91-D. Gli altri test esercitano i casi negativi, comprese evidenze volutamente contraddittorie o sintetiche.

## Limitazioni importanti

- È **un audit di corrispondenza** rispetto a una checklist dichiarata, non una diagnosi automatica della struttura.
- Non determina i carichi sfavorevoli, le combinazioni SLU/SLE prescritte, le sezioni governanti per ogni formula o i coefficienti nazionali.
- Non dimostra la validità fisica dei vincoli, dei ritegni, delle proprietà del materiale o dei valori di `Mcr` e `Lcr`.
- Non sostituisce verifiche non implementate: sezione netta, collegamenti, instabilità in pressoflessione generale, classe 4, taglio elevato, instabilità delle anime e fenomeni fuori piano non coperti dalle specifiche regole.
- I `DesignCheckReport` sono oggetti forniti dal chiamante: il controllo numerico dell'audit **non certifica autenticità/completezza** della loro origine. Occorre una catena di audit/verifica del progetto e del calcolo reale.
- I dati tabellati OPPO hanno valore indicativo: l'audit non li promuove a dati di prodotto certificati.
- Non applica autonomamente NTC 2018 o Eurocodici di seconda generazione.

## Gate M91-D

```powershell
.\scripts\verify-m91d-manifest.ps1
.\scripts\run-m91d-tests.ps1
```

Il runner esegue build `Release -warnaserror`, l'intera suite Verification (compresi test M91-D), i cataloghi e la regressione Core, Analysis, Reporting, CLI. Il gate va eseguito su Windows con .NET 8.

## Prossime tappe

- M92: prime verifiche EC5 legno con resistenze caratteristiche già in M88-C, classi di servizio, durata di carico e coefficienti `kmod`, `gamma_M` espliciti.
- M93–M94: interazioni, instabilità, deformazioni nel tempo e validazione legno.
- M95–M99: SLE, report aggregati, confronto intersoftware e release normativa **sempre a perimetro dichiarato**.
- GUI Avalonia: alla fine, senza mescolare interfaccia e calcolo.
