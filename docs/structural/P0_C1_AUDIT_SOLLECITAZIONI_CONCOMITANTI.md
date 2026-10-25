# P0-C1 — Controllo FEM delle sollecitazioni concomitanti nelle stazioni dichiarate

**25 ottobre 2026 — P0-C1 FIX1 dichiarato GATE PASS dall'utente («test passati») dopo correzione del solo xUnit errato; nessun nuovo log numerico allegato.**

**Nota FIX1 (log `dotnet-20261025-014719-25108-2fab4f16.log`):** il test negativo sul lato `Continuous` selezionava erroneamente il primo elemento del fixture (x=0, che ha correttamente lato continuo). Il salto del carico puntuale è a x=2, secondo elemento; ora il test usa esplicitamente `pointLoadIndex = 1`, verifica distanza/lato attesi e aggiunge una controprova di legittimità per x=0 continuo. M87-B aveva già la corretta validazione dei lati al punto di salto: **nessun cambiamento ai sorgenti FEM o all'auditor C1**. La suite Windows FIX1 non è ancora stata eseguita.

## Premessa: cosa esiste già e cosa NON basta

- **M78** trova gli estremi esatti dei singoli diagrammi elastici N, V e M (quando l'elemento e i carichi sono supportati), tenendo conto delle discontinuità e dei due limiti ai carichi concentrati.
- **M87-B** passa a una verifica puntuale le tre risultanti **firmate e concomitanti** della stessa stazione, analisi e combinazione, con lato e provenienza.
- **M87-C** propone capi asta, estremi individuali, limiti ai carichi puntuali e stazioni extra/griglie, senza attestare un massimo dell'utilizzo normativo lungo l'asta.

Una regola combinata può raggiungere il massimo lontano dagli estremi di N, V e M presi separatamente. Inoltre un report `Passed` su una stazione non può trasformarsi in `MemberPassed`. **P0-C1 presidia l'identità dei dati N/V/M ricevuti dalle verifiche; P0-C2/C3 devono affrontare massimi di utilizzo e copertura degli obblighi.**

## Il nuovo controllo

Modulo esistente `src/StructuralSolver2D.Verification.Integration/`, nuova classe `FemConcomitantStationAuditorC1` (nessun cambiamento a FEM, M78, M87-B/C, formule EC3/EC5/NTC, CLI o schema JSON):

1. Il chiamante dichiara un **piano finito** `FemMemberStationPlanC1` per un membro, con codice/edizione e riferimenti esterni a copertura delle combinazioni e criteri di scelta delle stazioni.
2. Ogni `FemDeclaredStationC1` specifica `StationId`, tipo di target (`LoadCase` o `LoadCombination`), relativo ID, distanza in metri, lato (`Continuous`, `BeforePointLoad`, `AfterPointLoad`), stato limite, profilo e **insieme delle regole richieste**.
3. L'auditor riceve gli effettivi `DesignCheckRequest` prodotti dal bridge M87-B o dal selettore M87-C, verifica che corrispondano *uno a uno* al piano per combinazione/stazione/lato/profilo e che non siano duplicati o estranei.
4. Il controllo **risolve nuovamente con il solver reale**, una volta per ciascun target distinto, e usa `Frame2DInternalForceSampler.EvaluateMemberSection` per confrontare **tutti e tre i valori firmati N/V/M** a quel punto e a quel lato. Lo scambio di `Nmax` da una combinazione con `Mmax` di un'altra fallisce anche se uno dei due numeri sembra plausibile.
5. Eventuali stazioni mancanti, input senza i tre valori, discrepanze di edizione/regole, lato impossibile e altre incongruenze producono esiti tipizzati. Un errore del FEM non diventa `Passed`.

Unità: **N/V kN, M kNm, ascissa m**. Tolleranza numerica del confronto dei valori locali: `|a−b| ≤ 10⁻⁸ + 10⁻¹⁰·max(|a|,|b|)` nelle unità appena indicate. Non viene mai assunto il valore assoluto o mescolato un inviluppo di sorgenti differenti. Questo è un test di corrispondenza con il solver, **non** una verifica indipendente della correttezza matematica del solutore.

## Esiti distinti

| `FemStationAuditStatusC1` | Significato |
|---|---|
| `MatchedDeclaredStations` | Tutte le stazioni *dichiarate* hanno una richiesta non ambigua e una terna N/V/M coerente col FEM. **Nessun pass di regola o asta**. |
| `Incomplete` | Almeno una stazione/terna/evidenza richiesta manca; non ammettere idoneità. |
| `Unsupported` | Membro non `Frame2D` o altro comportamento estraneo a questo audit. Un eventuale bridge assiale per `Truss2D` avrà un contratto distinto. |
| `Invalid` | Stazioni, dati, target, edizione o forze non corrispondono; un'analisi impossibile è anch'essa `Invalid`. |

La prevalenza è `Invalid > Unsupported > Incomplete`. Anche nel caso perfetto, le proprietà `MemberAdequacyEstablished`, `ContinuousUtilizationMaximumEstablished` e `StationSetExhaustivenessEstablished` sono **sempre `false`**.

## Esempio d'uso

```csharp
using StructuralSolver2D.Verification.Integration;

// model: StructuralModel valido e non modificato; member: DesignMemberContext;
// code: DesignCodeEdition già selezionato; requests: risultati M87-B/C.
var plan = new FemMemberStationPlanC1(
    PlanId: "MEMBER_AB_STATIONS_V1",
    Member: member,
    Code: code,
    Stations: declaredStations,
    StationSelectionEvidenceId: "ENGINEER_STATION_REVIEW_001",
    CombinationSetEvidenceId: "B4_DECLARED_PLAN_001");

FemConcomitantStationAuditResultC1 audit =
    FemConcomitantStationAuditorC1.Audit(model, plan, requests);

// audit.Status == MatchedDeclaredStations: SOLO provenienza e concomitanza.
// audit.MemberAdequacyEstablished è SEMPRE false.
```

L'API confronta richieste FEM, non risultati normativi: la presenza di `RuleIds` **non dimostra** che quelle regole siano state realmente calcolate. I report con esiti, regole applicabili, evidenze fisiche ed esclusioni motivate richiedono P0-C3 ed il raccordo con G6/M91-D.

## Test e controesempi

Nuovo file xUnit: `tests/StructuralSolver2D.Verification.Tests/M87/FemConcomitantStationAuditC1Tests.cs`.

- Positivo: due combinazioni elastiche differenti di una trave con q, assiale e carico puntuale a metà asta; due valori laterali del taglio nella medesima posizione e valori N/V/M coerenti; ulteriore combinazione con **fattore negativo** senza invertire artificialmente i segni del FEM.
- Negativi: N, V o M alterati separatamente; N estratto dall'altra combinazione; lato di carico concentrato omesso, sostituito o ambiguo; stazione mancante, extra o duplicata; insieme di regole o edizione modificati; tripla incompleta; assenza di evidenza esterna.
- `Truss2D` rimane `Unsupported`, senza forzare un diagramma di momento inesistente.
- Controesempio indipendente di massimo interno: su `x ∈ [0,1]`, `N(x)=x`, `M(x)=1−x` hanno massimi individuali agli estremi, mentre `η(x)=|N(x)M(x)|` assume il massimo **0,25 a x=0,5**. Una verifica che campioni soltanto gli estremi non può certificare l'utilizzo massimo.

Da una directory Windows estratta **pulita**:

```powershell
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~FemConcomitantStationAuditC1Tests' --logger 'console;verbosity=normal'
.\scripts\run-g7-tests.ps1
```

Nell'ambiente di assemblaggio **manca .NET SDK**: nessun xUnit nuovo è stato eseguito. Il precedente **P0-B4 è gate PASS su conferma dell'utente**, senza log numerico allegato. I test e i golden B1 e G7 non sono stati modificati.

## Roadmap da qui

1. **P0-C2 — massimo utilizzo:** per ciascuna regola, trattare il problema continuo su tratti a leggi regolari, discontinuità e estremi; richiedere un certificato di massimizzazione entro ipotesi esplicite o classificare `SCREENING_INCOMPLETE`. Nessuna trasformazione della griglia M87-C in prova esaustiva.
2. **P0-C3 — copertura delle verifiche:** inventario di obblighi per *membro × combinazione × stazione/lato × regola × edizione*; esiti `Failed` non mascherati da lacune, `NotApplicable` solo con prova fisica, `Unsupported` espliciti; report separato dal `DesignCheckReport` puntuale.
3. **P0-C4 — reporting:** separare estremi FEM esatti, campioni di diagrammi e sezioni usate nelle verifiche normative; includere mancate dimostrazioni dell'intervallo.
4. **P0-C5 / P0-E:** eventuale bridge assiale `Truss2D` specifico, registrazione di ritegni e nodi fisici senza attribuire al 2D capacità fuori piano. Poi ampliamenti P0-D; **GUI Avalonia soltanto P3**.
