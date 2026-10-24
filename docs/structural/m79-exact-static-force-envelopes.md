# M79 — Inviluppi globali delle sollecitazioni Frame2D e combinazioni governanti

**Stato: implementato, test C#/.NET 8 Windows pendenti.** M73 è l'ultimo milestone confermato dall'utente; anche M75–M78 sono pendenti. Questo documento descrive il perimetro implementato, non una certificazione normativa.

## Scopo

M78 trova in modo analitico minimo, massimo e massimo assoluto N/V/M per una singola analisi statica elastica. M79 confronta **più scenari alternativi** (casi di carico e/o combinazioni manuali esplicitamente indicati) e seleziona per ciascuna asta Frame2D e grandezza il risultato governante. Non si sommano tra loro i valori massimi di scenari differenti: **si confrontano risultati strutturali completi**, ciascuno calcolato con il solver di produzione.

Il riferimento ai carichi è preservato nel risultato: identificatore del caso/combinazione, tipo, valore firmato, ascissa esatta [m], ascissa normalizzata, limite sinistro o destro in prossimità di carichi concentrati. Le unità sono kN per N/V e kNm per M.

## Contratto pubblico

```csharp
var service = new StructuralSolver2DService();
var envelope = service.AnalyzeStaticForceEnvelope(model, new[]
{
    new StaticForceEnvelopeScenarioM79(StructuralAnalysisTargetKind.LoadCase, "G"),
    new StaticForceEnvelopeScenarioM79(StructuralAnalysisTargetKind.LoadCombination, "ULS_1"),
    new StaticForceEnvelopeScenarioM79(StructuralAnalysisTargetKind.LoadCombination, "SLE_1"),
});
var governing = envelope.GetMember("BEAM_1")
    .MaximumAbsolute(InternalForceQuantity.BendingMoment);
// governing.Value, .Distance, .ResultId, .TargetKind, .Side
```

`StructuralSolver2DService` risolve ogni scenario **una sola volta** tramite `PlaneStructureAnalyzer`, e `Frame2DStaticForceEnvelopeBuilderM79` recupera gli estremi delle sole aste Frame2D tramite `Frame2DExactExtremaFinderM78`. Per ciascuna grandezza si prendono il minimo fra i minimi dei singoli scenari, il massimo fra i massimi e il massimo in valore assoluto; quest'ultimo conserva il segno reale. In caso di parità numerica esatta viene mantenuto **il primo scenario nell'ordine passato**.

È possibile mescolare `LoadCase` e `LoadCombination`. Sono rifiutati selezioni vuote, casi inesistenti, identificatori ripetuti (senza distinzione tra maiuscole e minuscole), tipi sconosciuti e ID ambigui condivisi da un caso e una combinazione: il sampler M78 identifica i fattori da `result.LoadCaseId` e la collisione potrebbe corrompere il diagramma se tollerata. Per un modello senza aste Frame2D il modulo segnala esplicitamente `M79_NO_FRAME_MEMBERS`.

## Export

`StaticForceEnvelopeReportM79.ToCsv(envelope)` genera righe `MemberId,Quantity,Extreme,Value,Unit,Distance_m,Position,Side,TargetKind,ResultId` con formattazione invariant culture, doppi apici e protezione dei nomi da formule interpretate dai fogli elettronici. `ToMarkdown` produce una tabella leggibile e l'elenco degli scenari utilizzati. L'esportazione è attualmente una API C#: **nessun comando CLI M79** è stato aggiunto e i comandi precedenti restano invariati.

## Test e riferimenti

- Trave appoggiata con combinazioni positive e negative: **Mmax = 65,625 kNm**, **Mmin = -31,25 kNm**; combinazione governante identificata. La combinazione invertita comprende esclusivamente `G × -1`: il fattore esplicito `Q × 0` è correttamente respinto dal validatore globale `LOAD_COMBINATION_INVALID_FACTOR` e non va incluso.
- Paragone del risultato M79 con tre analisi indipendenti di M78 (N/V/M).
- Carico concentrato: limiti di taglio su entrambi i lati del salto e ascissa del massimo momento.
- Parità fra combinazioni, ordine deterministico.
- Due aste in una mensola; confronti con riferimento elastico.
- Target duplicati, errati e ambigui, nessuna asta Frame2D.
- CSV/Markdown, caratteri speciali nei titoli e prevenzione delle formule CSV.

I test numerici sono esclusivamente **C#/xUnit**. `scripts/run-m79-tests.ps1` avvia come prima azione la pulizia `bin`/`obj`, quindi Release build, gate M78–M79, test reporting e regressione completa inclusiva dei test M75–M77.

## Limiti deliberati

- Solo **Frame2D**, elasticità lineare, analisi statica di primo ordine. Le reticolari non hanno V/M di trave; qui non viene calcolato il loro inviluppo assiale.
- Sono restituiti **estremi globali per asta e grandezza**, non la funzione di inviluppo continua in ogni sezione x; non è possibile dedurre l'intero diagramma d'inviluppo dalla sola terna min/max/max assoluto.
- Nessuna generazione automatica di combinazioni SLU/SLE, nessuna verifica NTC/Eurocodici, nessuna resistenza o stabilità delle sezioni.
- Non copre secondo ordine, cerniere plastiche, dinamica, spettri, variabilità di proprietà o cedimenti non lineari.
- In futuro occorreranno anche le **sollecitazioni concomitanti** nella stessa sezione e nella stessa combinazione, per verificare interazioni N-M-V: i massimi indipendenti non possono essere combinati come se si verificassero simultaneamente.

**Prossimo sviluppo previsto:** M80 — proprietà geometriche delle sezioni, prerequisito dei cataloghi di acciaio e legno. Conservare la distinzione tra il modulo FEM e il futuro modulo normativo.
