# StructuralSolver2D — Handoff di ripresa

**25 ottobre 2026 — P0-F1.2-A, verifiche analitiche indipendenti di orientamento, carichi e release in C#.** Archivio FULL da usare: `StructuralSolver2D_P0F12A_ORIENTAMENTI_RELEASE_CSHARP_2026-10-25_FULL.zip` (estrarre **pulito**). I sorgenti di produzione sono identici a **P0-D2C2**; F0 era solo documentazione, F1A/F1B/F1C hanno aggiunto test e riferimento variazionale, F1D un programma C# per export statico, F1E test viewer/report e F1.2-A altri test analitici più la correzione di un warning xUnit.

## Leggere in quest'ordine

1. [`docs/structural/CURRENT_STATE.md`](docs/structural/CURRENT_STATE.md): situazione verificabile, ultimo test confermato e limiti.
2. [`docs/structural/roadmap.md`](docs/structural/roadmap.md): sequenza e gate F0–F10.
3. [`docs/structural/ENGINEERING_AND_VALIDATION_RULES.md`](docs/structural/ENGINEERING_AND_VALIDATION_RULES.md): regole di sviluppo vincolanti.
4. [`docs/structural/PIANO_DETTAGLIATO_POST_D2C2.md`](docs/structural/PIANO_DETTAGLIATO_POST_D2C2.md): valutazione approfondita dei gap e delle priorità.
5. [`docs/structural/F1_2A_ORIENTAMENTO_CARICHI_RELEASE_ANALITICI_CSHARP.md`](docs/structural/F1_2A_ORIENTAMENTO_CARICHI_RELEASE_ANALITICI_CSHARP.md): 12 nuovi benchmark, formule attese, comandi Windows e limiti.
6. [`docs/structural/F1_1E_G8B_VALIDAZIONE_VIEWER_REPORT_CSHARP.md`](docs/structural/F1_1E_G8B_VALIDAZIONE_VIEWER_REPORT_CSHARP.md): suite viewer/report, ora PASS con warning risolto.
7. [`docs/structural/F1_1D_G8B_ESPORTAZIONE_GOLDEN_NVM_CSHARP.md`](docs/structural/F1_1D_G8B_ESPORTAZIONE_GOLDEN_NVM_CSHARP.md): tool C#, comandi e file JSON da restituire.
8. [`docs/structural/F1_1C_G8B_RIFERIMENTO_VARIAZIONALE_CSHARP.md`](docs/structural/F1_1C_G8B_RIFERIMENTO_VARIAZIONALE_CSHARP.md), [`F1B`](docs/structural/F1_1B_G8B_SOLLECITAZIONI_EQUILIBRIO_CSHARP.md) e [`F1A`](docs/structural/F1_1A_G8B_VALIDAZIONE_CSHARP_16_SCENARI.md): prove precedenti.
9. [`CHANGELOG.md`](CHANGELOG.md) e documentazione specifica di milestone per lo storico.

## Situazione

- **P0-F1E .NET: PASS comunicato dall'autrice** («test passati»), senza log allegato; comprende F1A/F1B/F1C e F1D-FIX1. Il primo F1D era fallito per un hash atteso copiato incompleto, risolto nel FIX1. **F1.2-A nuovo: compilazione/test .NET PENDING**, non attribuirgli il PASS precedente.
- La regressione indipendente **4.203/4.203** di D2C2 è un **dato storico Python**, non un conteggio xUnit e non un nuovo gate F0.
- Motore FEM 2D già ampio ma copertura normativa **sezionale/parziale**; attenzione a stazioni, concomitanze, ritegni fisici, completamento combinazioni e `Passed` soltanto delle regole richieste. Nessuna GUI desktop ancora.
- **NTC 2018, EC3 ed EC5 separati** per norma/edizione/annesso e coefficienti; nessuna conversione implicita fra esiti.
- **Nuovo codice, test, benchmark harness, runner e script soltanto C#/.NET.** Non scrivere/eseguire nuovo Python; i file Python e PowerShell preesistenti sono storici/compatibilità e non devono entrare nel flusso futuro.
- `G8BIndependentReferenceF1ATests.cs` confronta **16 scenari** con il golden statico congelato, per spostamenti, reazioni, equilibrio e N/V/M delle colonne inferiori. Sono presenti controprove e SHA256 del golden; gate Windows riferito PASS.
- `G8BMemberForceF1BTests.cs` aggiunge **64 casi parametrizzati**, più una controprova, per sbalzi, equilibrio travi/colonne/puntoni di copertura e nodi, e diagonali assiali: PASS confermato dall'autrice.
- `G8BVariationalReferenceF1C.cs` e `G8BIndependentMemberF1CTests.cs` hanno introdotto un **secondo risolutore elastico, esclusivamente test-only**, con quadratura di energia/carichi e Cholesky. Riproducono **prima** il golden storico indipendente, poi confrontano tutti i nodi e N/V/M a cinque stazioni di tutte le aste (**due famiglie da 16 casi, 32 + 1 controprova; PASS riferito dall'autrice**).
- **F1D** registra nella soluzione `tools/StructuralSolver2D.G8BReferenceF1D`, che collega **esattamente il sorgente del riferimento F1C**, non usa il FEM produttivo e genera un JSON **proposto** per **336 membro-scenari / 1.680 stazioni**. Prima verifica hash di input e golden storico; rifiuta sovrascritture. In FIX1 corretto l'hash incompleto. **Il JSON proposto non è stato ricevuto**, quindi non esiste ancora un golden numerico completo approvato.
- **F1E** aggiunge `G8BViewerReportF1ETests.cs` e riferimento al progetto Reporting nei test Analysis. Per 16 scenari confronta **coordinate delle polilinee SVG N/V/M e tabelle Markdown realmente esportate** con il risolutore F1C. Verifica frecce di gravità e vento, payload HTML e controprova di inversione di segno M. **37 nuovi test C# PASS comunicato dall'autrice**. L'unico warning `xUnit2031` segnalato è corretto in F1.2-A cambiando soltanto l'overload di `Assert.Single`.
- **F1.2-A** aggiunge `F12AInclinedAnalyticalReferenceTests.cs`: sei mensole ruotate con carichi firmati, quattro travi inclinate e rilasciate con carrello verticale globale, una combinazione a coefficiente negativo, una controprova d'inversione. **12 nuovi test C# .NET PENDING**, attesi da formule chiuse ed equilibrio; **nessuna modifica a `src/`, golden o norme**.

## Prossimo passo autorizzato

**Eseguire F1.2-A sul PC Windows** dal FULL pulito. Il golden proposto F1D si può esportare dalla stessa cartella se non già presente:

```powershell
dotnet build StructuralSolver2D.sln -c Release
dotnet test tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --filter FullyQualifiedName~F12AInclinedAnalyticalReferenceTests
dotnet test tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --filter FullyQualifiedName~G8BViewerReportF1ETests
dotnet test StructuralSolver2D.sln -c Release
dotnet run --project tools/StructuralSolver2D.G8BReferenceF1D -c Release -- --output benchmarks/independent/g8b-house/g8b-member-nvm-f1d.proposed.json
dotnet run --project tools/StructuralSolver2D.G8BReferenceF1D -c Release -- --verify benchmarks/independent/g8b-house/g8b-member-nvm-f1d.proposed.json
```

**Restituire alla chat l'esito test F1.2-A e `g8b-member-nvm-f1d.proposed.json` con SHA256 della console.** Se il JSON è già stato creato, **non ripetere `--output` sul medesimo percorso**: usare soltanto `--verify`. Prossimo checkpoint: rivedere il JSON, congelare hash e attesi xUnit C#, confrontare viewer/report anche con il golden statico. F1.1 ancora aperto. Poi completamento F1.2/F1.3; **nessuna nuova formula normativa o GUI prima della chiusura numerica.**

## Disciplina dei checkpoint

Ogni nuovo avanzamento deve indicare: **stato/milestone e test confermati; modifica in corso; passi ancora da fare; parti non implementate o fisicamente non dimostrate**. Aggiornare i documenti di competenza e confezionare uno ZIP FULL con manifest SHA256 verificato. Mantenere il README essenziale. Le versioni precedenti di roadmap e handoff sono conservate in [`docs/archive/`](docs/archive/README.md).
