# F1.1-E — Validazione dei report e dei diagrammi G8B con riferimento indipendente C#

> **Esito successivo, 25 ottobre 2026:** l'autrice ha confermato **P0-F1E PASS** («test passati») sul proprio PC. Ha segnalato soltanto `xUnit2031` alla riga 190 di `G8BViewerReportF1ETests.cs`. Il checkpoint **P0-F1.2-A** sostituisce `Assert.Single(Where(...))` con l'overload `Assert.Single(collection, predicate)` equivalente. Le voci «PENDING» qui sotto documentano lo **stato al rilascio F1E**, non l'esito attuale; per quello vedere [stato corrente](CURRENT_STATE.md).

**25 ottobre 2026 — P0-F1E, nuovo gate .NET PENDING.** L'autrice ha confermato **P0-F1D-FIX1 PASS** («test passati»), senza allegare log o JSON. F1E introduce solo prove C# di *post-processing*, SVG e HTML/Markdown; non cambia il solutore FEM, le formule normative, i dati G8B o i generatori di report in produzione.

## Obiettivo e catena delle evidenze

Il riferimento [F1C](F1_1C_G8B_RIFERIMENTO_VARIAZIONALE_CSHARP.md) integra autonomamente energia e carichi e usa Cholesky, con modelli originali verificati per SHA256 e confronto con il golden storico. Da quei risultati `N/V/M` ci si attende che la rappresentazione grafica e tabellare mantenga **modulo, segno, orientamento locale e unità**:

1. Si apre uno dei quattro modelli G8B bloccati per SHA256.
2. Il riferimento indipendente F1C produce gli attesi per una delle quattro combinazioni.
3. Il servizio pubblico FEM calcola la soluzione e campiona cinque stazioni `0, ¼, ½, ¾, 1` per ogni asta.
4. F1E esporta i dati tramite i componenti **reali** `StructuralVisualizationModelBuilder`, `SvgStructuralResultExporter`, `MarkdownStructuralReportGenerator` e, per un caso rappresentativo, `InteractiveHtmlStructuralViewerExporter`.
5. Il test confronta l'output **effettivamente esportato** con i numeri indipendenti F1C. Non considera il risultato FEM produttivo un oracolo di se stesso.

## Matrice prove aggiunta

| Prova C# | Casi | Oggetto di confronto |
|---|---:|---|
| `AllSixteenScenarios_SvgPolylinePositionsMatchIndependentSignedNvm` | 16 | Tutti i membri, tre polilinee N/V/M, cinque punti; coordinate SVG lette dall'XML; offset secondo normale locale e convenzione dello schermo Y invertito |
| `AllSixteenScenarios_MarkdownEndForcesAndFiveStationTablesMatchIndependentReference` | 16 | Sei forze locali di estremità per membro, N/V/M a cinque stazioni e ascisse in metri, lette dalle tabelle Markdown |
| `SvgWindArrow_RetainsTheCorrectGlobalXDirection` | 2 | Freccia del vento `W3_RIDGE` (+X) e `WN3_RIDGE` (−X) nello SVG reale |
| `SvgGravityDistributedArrows_PointDownwardForNegativeGlobalY` | 1 | Frecce dei carichi distribuiti del caso gravitazionale, negative in GlobalY e quindi rivolte verso il basso nello schermo SVG |
| `HtmlViewer_EmbedsIndependentNvmMaximaAndLoadVisibilitySwitch` | 1 | Payload JSON nel viewer HTML: massimi assoluti N/V/M di tutte le aste; presenza dello switch di visibilità dei carichi e delle polilinee |
| `TamperedMomentSign_IsDetectedAgainstIndependentReference` | 1 | Controprova: inversione artificiale del segno del momento nel solo dato di visualizzazione; il valore ricavato dalla polilinea deve divergere dall'oracolo |

Sono **37 esecuzioni teoriche** (16+16+2+1+1+1), salvo il conteggio aggiuntivo degli altri test già esistenti. Le 16 combinazioni coprono i quattro modelli G8B; nessun nuovo risultato è stato presunto PASS in assenza dell'SDK .NET qui.

Le tolleranze quantitative partono da `0,005 kN / 0,005 kNm` e `10⁻⁴` relativi di F1C. La posizione SVG ammette anche `0,025 px` per l'arrotondamento a tre decimali del formato esportato. Nel Markdown vengono letti i valori arrotondati a sei decimali; la soglia fisica rimane quella di F1C. L'offset di un diagramma è positivo secondo la normale locale **(−dy/L, dx/L)**; nel SVG il segno di Y è invertito.

## Verifica su Windows

Dalla directory del FULL, estratto in una cartella pulita:

```powershell
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln -c Release
dotnet test tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --filter FullyQualifiedName~G8BViewerReportF1ETests
dotnet test StructuralSolver2D.sln -c Release
```

In caso di errore, inviare la **prima eccezione completa** e il nome del test. Il progetto Analysis.Tests ha ora un riferimento esplicito a `StructuralSolver2D.Reporting`, ma non sono stati introdotti nuovi pacchetti NuGet.

## Stato e limiti

| Verifica | Esito corretto al rilascio F1E |
|---|---|
| F1A, F1B, F1C, **F1D-FIX1** | **PASS riferito dall'autrice** sul suo PC, senza log allegato |
| Codice C# F1E / suite aggiunta | **Realizzata; build e test .NET PENDING**, SDK .NET non disponibile in questo ambiente |
| JSON proposto `g8b-member-nvm-f1d.proposed.json` | **Non ricevuto**: impossibile verificare i valori, fissarne SHA256 o approvarlo come golden definitivo |
| Prove SVG/HTML/Markdown | Test automatici nuovi da confermare; **non** verificano rendering di un browser reale né usabilità grafica |
| Fonti di produzione, esempi G8B e algoritmi FEM | **Invariati** in F1E |

Questo insieme di prove valida, nel perimetro G8B, la trasformazione dei risultati elastici 2D nei formati di presentazione. **Non** dimostra la completezza dei massimi tra le cinque stazioni, né stabilità, controventamento reale, connessioni, verifiche SLU/SLE normative o sicurezza delle strutture. L'uso di scale di disegno è grafico, non modifica le grandezze fisiche.

## Decisione successiva

Ricevere il JSON F1D proposto e il suo SHA256, controllarlo, conservarne una versione **immutabile** e aggiungere test C# che confrontino i calcoli e, dove opportuno, i report con **quel golden statico**. Soltanto dopo risultati F1E PASS sul PC e riferimento statico congelato può essere valutata la chiusura di F1.1. Seguono **F1.2** orientamenti/release/carichi e **F1.3** robustezza, quindi F2–F10 nell'ordine [roadmap](roadmap.md). Nuovo codice e runner soltanto C#/.NET; NTC 2018, EC3 ed EC5 restano separati; GUI Avalonia soltanto alla fine.
