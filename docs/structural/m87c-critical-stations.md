# M87-C — Sezioni candidate per verifiche (24 ottobre 2026)

## Stato verificato

- **M86 RC1:** gate Release su Windows confermato (1.334 test, 0 errori e 0 avvisi).
- **M87-A:** 19 test di Verification superati.
- **M87-B:** 26 test di Verification complessivi e regressione storica 1.334/1.334 passati; compilazione 0 errori/avvisi (log del 24/10/2026, ore 15:24).
- **M87-C:** pronto per il gate Windows; i nuovi test .NET non sono ancora stati eseguiti nell'ambiente di consegna.

## Scopo e limiti (essenziali)

Il selettore M87-C individua automaticamente **sezioni candidate** per analisi elastica statica Frame2D **di primo ordine**, senza produrre un giudizio normativo.

- Riusa `Frame2DExactExtremaFinderM78` per i valori estremi **esatti nei limiti dell'aritmetica numerica e del modello a tratti** di N, V, M. Le loro posizioni non dipendono dai punti di un diagramma grafico.
- Considera l'inizio e la fine dell'asta e **entrambi i lati** degli eventuali carichi concentrati interni, anche quando il massimo di N, V o M non si trova lì.
- Aggiunge, facoltativamente, sezioni esplicite o una griglia di approfondimento. La griglia è solo un controllo supplementare e non dimostra completezza.
- I duplicati per stessa sezione e lato vengono consolidati; motivazioni multiple sono conservate.
- **Una sola soluzione FEM per ogni caso/combinazione**; le forze vengono rilette alla sezione tramite il bridge M87-B e preservano segni, membri, sezione, lato, analisi e target.
- **NON** è garantito che il massimo di un'interazione normativa non lineare (es. N+M o altre formule con capacità che variano lungo l'asta) si trovi fra questi candidati. Prima di dare un esito *globale* si dovrà disporre di un ricercatore della massima utilizzazione validato per la regola specifica, o di un criterio conservativo verificato. **Nessuna verifica normativa globale e nessun risultato "Passed" dell'intera asta sono calcolati in M87-C.**
- Non verifica comportamento fuori piano, instabilità flesso-torsionale, ritegni laterali, sezioni variabili, combinazioni normative automatiche, legno/acciaio EC3/EC5/NTC, plasticità, secondo ordine generale o dinamica.

## API C#

```csharp
var selector = new FemCandidateStationSelectorM87C();
var candidates = selector.CaptureCandidates(
    model,
    new DesignMemberContext("M", DesignMaterial.Steel, "SEC", 4),
    DesignLimitState.Ultimate,
    profile,
    new[] { "EC3-NOT-IMPLEMENTED" },
    new FemCandidateTargetM87C(StructuralAnalysisTargetKind.LoadCombination, "ULS1"),
    extraSections: new[] { new FemExtraSectionM87C(1.37) },
    gridIntervals: 20);

foreach (var candidate in candidates)
{
    // candidate.Request.Effects has N/V/M at ONE section, ONE side and ONE target;
    // candidate.Reasons records the source of this screening station.
    // Register real norm-specific rules ONLY after validated implementation.
}
```

Con `CaptureAcrossTargets` si possono richiedere più combinazioni; sono trattate indipendentemente e ordinate secondo la sequenza esplicita. Non viene eseguito un inviluppo che mescoli sollecitazioni di target diversi. `CaptureCandidates` rifiuta identificativi ambigui tra load case e load combination.

Carichi concentrati all'estremità: si considera il lato interno all'asta per le sezioni automatiche, senza inventare una continuazione esterna. Le sezioni esplicite a un punto di salto richiedono `BeforePointLoad` oppure `AfterPointLoad`.

## Test

Il file `tests/StructuralSolver2D.Verification.Tests/M87/FemCandidateStationSelectorM87CTests.cs` comprende otto scenari:

1. Massimo M esatto nel mezzo della trave sotto q uniforme, con taglio contemporaneamente nullo.
2. Salto di V a carico concentrato, prima e dopo il punto di applicazione.
3. N compressivo concomitante a M e V sotto carichi simultanei.
4. Combinazioni manuali differenti, anche con coefficienti negativi, senza mescolanza.
5. Sezioni esplicite fuori griglia e griglia opzionale.
6. Registro normativo vuoto: nessun Passed globale.
7. Reiezione input ambiguo o invalido.
8. Corrispondenza fra dati dichiarati e membro FEM (lunghezza, sezione).

Eseguire da PowerShell nella cartella StructuralSolver2D:

```powershell
.\scripts\verify-m87c-manifest.ps1
.\scripts\run-m87c-tests.ps1
```

Sono inclusi build Release con warning-as-error, test Verification e regressioni Core, Analysis, Reporting e CLI. L'esito è ancora da confermare in .NET su Windows.

## Roadmap / rischio / semplificazioni

- **M88** — catalogo comune di profili/materiali acciaio e legno (unità esplicite, proprietà geometriche e provenienza), in un modulo separato.
- **M89–M91** — regole di resistenza EC3/NTC acciaio, poi stabilità e interazioni; la gestione fuori piano è fra le parti più complesse.
- **M92–M94** — proprietà legno, durata carichi, classe di servizio, resistenza e stabilità EC5/NTC.
- **M95** — SLE, deformazioni ammissibili e combinazioni pertinenti.
- **M96–M99** — confronti con esempi indipendenti, matrici di conformità, report normativo, rilascio preview delle verifiche.
- **GUI Avalonia** — ultimo stadio.

Semplificazione: una famiglia di profili e una regola con benchmark indipendente alla volta. Non introdurre ottimizzazione numerica delle interazioni né esiti globali di verifica finché non sono formalizzati e testati.
