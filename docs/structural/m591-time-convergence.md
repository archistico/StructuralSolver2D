# M59.1 — Studio di convergenza dell'integrazione temporale

**Stato:** implementato; da validare con SDK .NET 8. **Base:** M59, validato dall'utente nei test automatici. Il programma è uno strumento diagnostico di analisi lineare, non una verifica di sicurezza né un controllo normativo.

## Perché

Lo schema Newmark a accelerazione media (β=1/4, γ=1/2) è incondizionatamente stabile per modelli lineari appropriati, ma può produrre risultati imprecisi con passo temporale grande. Anche un bilancio energetico con residuo quasi nullo **non** dimostra la convergenza temporale. Il report M59 può avvisare quando `dt * highest natural frequency > 0.1`. M59.1 rende sistematico il controllo.

## Procedura

Il servizio `StructuralTimeConvergenceAnalysisService` ripete M59 con *gli stessi* nodi, aste, vincoli, massa, smorzamento, carichi fisici con gli stessi istanti dei nodi di interpolazione e gli stessi stati iniziali. Mantiene fisso l'intervallo `[0,T]` e usa tre griglie:

| Griglia | Δt | Numero passi |
|---|---:|---:|
| Base | dt | N |
| Intermedia | dt/2 | 2N |
| Fine | dt/4 | 4N |

Il massimo di M59 (2.000 passi) resta invariato: per M59.1 **N ≤ 500**. Un modello che supera tale limite non viene tagliato e il tempo di analisi non viene cambiato in silenzio. Ridurre la durata fisica, definire un passo base appropriato oppure effettuare verifiche separate su finestre più brevi.

Per ogni componente `Ux, Uy, Rz, Vx, Vy, VrZ, Ax, Ay, ArZ` si misurano i massimi scarti assoluti fra griglie ad **istanti coincidenti**. Ogni scarto è diviso per il massimo modulo della stessa componente calcolato sulla griglia fine, considerando tutti i nodi e tutti gli istanti. Una componente è considerata senza segnale soltanto se tutte e tre le griglie hanno massimo ≤ 1e-12 nell'unità della componente. Se la griglia fine è quasi nulla ma altre non lo sono, il canale resta attivo (evitando un falso esito di convergenza); il denominatore usa un pavimento di 1e-12. Le componenti realmente inattive sono escluse dal giudizio. Il massimo scelto evita che componenti con unità differenti siano sommate tra loro, ma la soglia di zero è comunque una scelta numerica da tenere presente.

Se entrambi gli scarti sono sufficientemente lontani dal rumore numerico, viene riportato un ordine empirico `p=log2(E_coarse/medium / E_medium/fine)`. Questo ordine non è garantito né significativo vicino a zero o in presenza di forzanti poco regolari o modalità scarsamente campionate.

Il nodo finale del modello e `Uy` sono i valori predefiniti per la **diagnostica dei picchi** tramite API. Sono confrontati massimi assoluti campionati, tempi in cui si verificano e differenze relative. Questi massimi possono cadere tra campioni e gli istanti sono quantizzati: il confronto dei picchi non sostituisce il confronto delle risposte negli stessi istanti.

### Frequenze e avvisi

L'elenco comprende **tutti** i modi disponibili nell'analisi completa, con frequenza `f` e `dt*f` per ciascuna delle tre griglie. `dt*f > 0.1` equivale a meno di dieci campioni per ciclo ed è una soglia di avvertimento **euristica**, non una garanzia di accuratezza né un requisito normativo. Alcuni modi potrebbero essere eccitati debolmente: non si attribuisce una partecipazione senza analizzarla. Se la griglia fine viola ancora il criterio, il report dice che la risoluzione **rimane da migliorare**, anche qualora gli scarti siano piccoli. Il nuovo avviso `M591_STEP_BUDGET` indica se la durata totale e il limite M59 di 2.000 passi rendono impossibile rispettare tale euristica per tutti i modi: non va interpretato come una bocciatura progettuale, perché la partecipazione dei modi al carico può essere molto diversa.

Una diagnostica illustrativa propone scarti medium/fine inferiori a 1% per spostamenti/rotazioni, 2% per velocità e 5% per accelerazioni, con controllo non monotono. Se il focus selezionato è un DOF vincolato o sostanzialmente nullo, M59.1 emette `M591_FOCUS_ZERO`. Se il picco varia oltre la soglia di confronto illustrativa, emette `M591_PEAK_VARIATION`; se l'istante di picco si sposta oltre un passo della griglia intermedia, emette `M591_PEAK_TIME_SHIFT`. Il giudizio sui picchi resta distinto da quello sui nove canali a tempi coincidenti. Queste soglie **non** autorizzano l'uso progettuale dei risultati. La convergenza di un carico discontinuo richiede attenzione particolare; un avviso segnala i nodi di carico interni al dominio temporale non coincidenti con la griglia di base.

### Stato inconcludente

Una risposta nulla può avere scarti esattamente nulli su tutte le griglie: M59.1 la segnala con `M591_ZERO_RESPONSE` e `HasMeaningfulResponse=false`. Dopo l'audit FIX1, anche `MeetsIllustrativeTolerances` e `DifferencesDecrease` valgono `false` in questo caso: un segnale assente non giustifica un giudizio positivo. Gli esiti `MeetsIllustrativeTolerances`, `DifferencesDecrease` e `FineGridResolvesAllModes` sono indipendenti e vanno letti congiuntamente, **non** trattati come una certificazione sintetica.

## Come usarlo

Si riutilizza il blocco `timeHistory` già presente nel modello M59. Non vengono aggiunte proprietà obbligatorie al JSON.

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- time-convergence examples/dynamics/m59-time-cantilever.json
dotnet run --project src/StructuralSolver2D.Cli -- time-convergence-report examples/dynamics/m59-time-cantilever.json convergence.md
dotnet run --project src/StructuralSolver2D.Cli -- time-convergence-csv examples/dynamics/m59-time-cantilever.json convergence.csv
```

L'API fornisce `StructuralSolver2DService.AnalyzeTimeHistoryConvergence(new StructuralTimeConvergenceRequest(existingM59Request, FocusNodeId: "N8", FocusComponent: "Uy"))`. I comandi CLI usano l'ultimo nodo e `Uy` per il picco. Le esportazioni testuali riportano dettagli e segnalazioni; non sono stati introdotti nuovi effetti visivi o modifiche al viewer M59.

## Test e limiti

Test M59.1: vibrazione libera con confronto dei passi, risposta nulla, limite 2.000 passi, componente/nodo di osservazione invalidi, nodi dei carichi non allineati, output CSV e Markdown, CLI e regressione completa M51–M59. Lanciare `scripts/run-m591-tests.ps1` in PowerShell da una directory estratta pulita.

Questa fase **non** implementa reazioni dinamiche, sollecitazioni delle aste, scosse sismiche, plasticità, adattività automatica, nuove matrici di massa o supporto a release. Senza confronto con un riferimento indipendente e una selezione ragionata del passo, gli scarti fra griglie restano **indicatori di sensibilità**, non una prova di verità fisica.

### Verifica indipendente dopo l’audit FIX1

Per il modello distribuito della mensola da 4 m, un FEM indipendente in Python/SciPy riproduce il valore N8 Uy M59 a Δt=0,002 s con scarto circa 7×10⁻¹⁵ m. Raffinando a 0,001 e 0,0005 s, gli scarti medium/fine nel nodo N8 risultano circa 0,98% (Uy), 1,09% (Vy) e 13,24% (Ay): l’accelerazione richiede ulteriore studio. La frequenza più alta è circa 19.713 Hz, e la condizione dt·f≤0,1 su 0,6 s richiederebbe circa 118.280 passi. Metodo, ipotesi e limiti sono documentati in `reports/m591-full-audit-2026-10-23.md`.
