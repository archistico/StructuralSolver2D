# F1.1-C — Riferimento FEM indipendente per N/V/M di tutte le aste G8B

**25 ottobre 2026 — P0-F1C.** Nuovo riferimento numerico **C#/.NET**, scritto esclusivamente nell'assembly dei test e distinto dal motore di produzione. **Aggiornamento successivo: F1C PASS riferito dall'autrice** («test passati»), senza log/numero di test allegato. Anche F1B era PASS; nell'ambiente di confezionamento non è disponibile l'SDK .NET.

## Problema risolto e confine di indipendenza

Il riferimento indipendente storico G8B contiene spostamenti scelti, massimi e reazioni per **4 modelli × 4 combinazioni = 16 scenari**, ma **non** presenta forze di estremità `N/V/M` numeriche per tutte le aste. [F1A](F1_1A_G8B_VALIDAZIONE_CSHARP_16_SCENARI.md) ne aveva verificato alcune per le colonne inferiori, [F1B](F1_1B_G8B_SOLLECITAZIONI_EQUILIBRIO_CSHARP.md) aveva aggiunto gli sbalzi analitici e l'equilibrio delle altre aste. Per dimostrare le forze delle travi, dei puntoni inclinati e delle diagonali serviva un **secondo calcolo della struttura**, non una semplice integrazione dei valori del solver che stiamo testando.

In `tests/StructuralSolver2D.Analysis.Tests/Benchmarks/G8BVariationalReferenceF1C.cs` il riferimento autonomo:

1. Legge l'**input G8B congelato** attraverso i contratti dati comuni, ma non chiama `PlaneStructureAnalyzer`, `Frame2DElementMatrices`, `SelfWeightStaticLoadExpander` o il post-processore produttivo.
2. Per ogni `Frame2D`, integra numericamente con quattro punti di Gauss l'energia assiale `EA ε²` e flessionale `EI κ²` della trave Euler–Bernoulli, con forma assiale lineare e funzioni di Hermite per la flessione.
3. Integra separatamente il lavoro dei carichi distribuiti `qx/qy`; rappresenta il peso proprio del `Truss2D` con due forze nodali uguali. Usa i carichi globali originali e i coefficienti delle quattro combinazioni illustrative.
4. Assembla in modo indipendente una matrice globale con **3 gradi di libertà per nodo**, applica i quattro incastri reali del JSON ed esegue una **fattorizzazione di Cholesky** autonoma, diversa dall'algoritmo produttivo.
5. Ricava spostamenti, reazioni e tutte le forze di estremità dalle proprie matrici/carichi; integra le equazioni di equilibrio sezionali per `N(x), V(x), M(x)` in ogni asta del modello, inclusi i puntoni inclinati e le diagonali assiali.

Questo realizza una **seconda implementazione numerica indipendente** del medesimo modello fisico idealizzato, non una prova sperimentale né un solver FEM generale sostitutivo. Condivide inevitabilmente la fisica Euler–Bernoulli, il modello geometrico e i materiali dell'input; il suo codice di assemblaggio, integrazione e soluzione è separato.

## Catena di validazione: prima l'oracolo, poi il motore

I test in `G8BIndependentMemberF1CTests.cs` hanno due passaggi indipendenti:

| Test | Verifica | Evidenza |
|---|---|---|
| `StandaloneReferenceMatchesPreviouslyFrozenIndependentDisplacementsAndReactions` | Il nuovo risolutore variazione/quadratura deve riprodurre **spostamenti, massimi e reazioni** già fissati prima di F1 | Oracolo storico `g8b-risultati-elastici-indipendenti.json` con SHA256 bloccato |
| `EveryMemberEndAndSectionForceMatchesIsolatedReference` | Il FEM di produzione deve riprodurre **tutti i gradi di libertà nodali**, le sei forze di estremità di ogni asta e `N/V/M` a **0, 1/4, 1/2, 3/4, L** | Nuova soluzione di riferimento C# distinta dal FEM produttivo |
| `MutatedWindLoad_IsDetectedByTheUnchangedIndependentReference` | Modifica intenzionale della forza al colmo: il solver mutato deve divergere dal riferimento caricato con l'input originale | Controprova negativa, senza rigenerazione degli attesi |

In totale sono **32 casi parametrizzati** (due famiglie × 16 scenari) e **una controprova**. Il golden storico resta lo stesso file e lo stesso SHA256:

`db41c1afbe08614ac62d1dcb7ef7ba2b0aa8fdc5af5fba7c3e94d9099021de47`

I **quattro file JSON di input** sono anch'essi protetti con SHA256 testati prima di qualsiasi calcolo. I relativi hash sono dichiarati nella classe di test e consultabili anche attraverso il manifest di release. Le combinazioni rimangono **illustrative**, non una generazione completa NTC 2018.

## Tolleranze e limitazioni

- Spostamenti: `0,0005 mm + 10⁻⁴ × |atteso|`; rotazioni: `10⁻⁷ rad + 10⁻⁴ × |atteso|`.
- Forze: `0,005 kN + 10⁻⁴ × |atteso|`; momenti: `0,005 kNm + 10⁻⁴ × |atteso|`.
- Il risolutore di riferimento rifiuta esplicitamente **release**, vincoli elastici/ruotati, cedimenti, carichi non uniformi o non appartenenti al dominio G8B e altri tipi di analisi. Non deve essere applicato ad altri modelli senza sviluppare e validare il relativo dominio.
- **Non sono ancora congelati su disco i valori numerici N/V/M per ogni asta e scenario**: F1C li ricalcola indipendentemente a ogni test partendo da JSON con hash bloccati. Dopo il PASS, una milestone successiva può esportare un golden statico completo **con strumento C#**, completo di provenienza e hash, senza copiarlo dall'analizzatore produttivo.
- L'indipendenza della soluzione del problema elastico non risolve i temi di **controventi bilateri ideali, capacità delle connessioni, instabilità fuori piano, vincoli reali, copertura delle verifiche normative**. `Passed` resta un'affermazione locale ai controlli richiesti.

## Come collaudare

Estrarre lo ZIP FULL **in cartella nuova**, quindi:

```powershell
.\scripts\verify-g7-manifest.ps1
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln -c Release
dotnet test tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --filter "FullyQualifiedName~G8BIndependentMemberF1CTests" --logger "console;verbosity=normal"
dotnet test StructuralSolver2D.sln -c Release
```

Se il primo gruppo di test fallisce, **non usare il nuovo riferimento N/V/M come oracolo**: diagnosticare per primi carichi, convenzioni dei segni, vincoli e precisione della quadratura, senza allargare le tolleranze per nascondere discrepanze. Se il primo PASS è confermato ma il secondo fallisce, localizzare membro, scenario, stazione e differenza prima di modificare il FEM.

## Dopo F1C

1. **F1C PASS riferito dall'autrice**; conservare tale conferma distinta dalla mancanza del log numerico e dall'esecuzione non disponibile in questo ambiente.
2. **F1.1-D (in preparazione):** è stato aggiunto un tool C# per esportare il golden statico delle forze di estremità e delle sezioni dalla sola implementazione indipendente. Eseguire il tool su Windows, revisionare e congelare il file e il suo hash; quindi validare viewer/report dei diagrammi contro i risultati congelati, compresi versi e visualizzazione dei carichi. [Procedura F1D](F1_1D_G8B_ESPORTAZIONE_GOLDEN_NVM_CSHARP.md).
3. **F1.2–F1.3:** ulteriori aste inclinate/orizzontali, scale numeriche, carichi e release fuori dal caso G8B, meccanismi e robustezza.
4. A seguire **F2–F9** con applicabilità fisica, combinazioni, massimi e norme **NTC 2018 / EC3 / EC5 separati**; **F10 GUI C#/Avalonia** solo dopo API e verifiche mature.

**I sorgenti produttivi `src/`, i benchmark storici e i JSON G8B restano invariati.** Nessun nuovo script Python, PowerShell o Bash nel progetto.
