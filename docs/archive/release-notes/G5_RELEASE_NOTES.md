# G5 — Legno NTC 2018: deformazioni SLE (gate Windows ancora da eseguire)

**Baseline:** G4 completo, con `G4 GATE PASSED` confermato dall'utente. 24 ottobre 2026.

## Risultato

Aggiunge il modulo **nativo NTC 2018 SLE per le deformazioni**, separato sia dalle verifiche SLU G1–G4 sia dal selettore EC5 M92-B. Riferimenti: NTC 2018 §2.5.3 (combinazione rara [2.5.2]), tabella 2.5.I (`ψ0`, `ψ2`), §4.4.7 e tabella 4.4.V (`kdef`); Circolare 2019 C4.4.7 formule [C4.4.1]–[C4.4.3]. Le parti numeriche delle formule sono state ricontrollate sulle pagine originali dei PDF forniti.

### Regole locali nuove

1. `NTC2018-TIMBER-SLE-RARE-VARIABLE-DEFLECTION-G5`: freccia istantanea della **sola quota variabile** sotto combinazione rara: `uQ1 + Σψ0,i uQi` (criterio raccomandato NTC L/300, adottato **esplicitamente**, o più restrittivo).
2. `NTC2018-TIMBER-SLE-RARE-TOTAL-DEFLECTION-G5`: freccia istantanea totale rara `uG + uQ1 + Σψ0,i uQi - u0`. Nessuna soglia automatica: richiede un limite approvato e documentato per il progetto, come consigliato nella Circolare.
3. `NTC2018-TIMBER-SLE-TOTAL-FINAL-DEFLECTION-G5`: freccia finale da [C4.4.3]:

   `uG*(1+kdef) + uQ1*(1+ψ2,1*kdef) + Σ[uQi*(ψ0,i+ψ2,i*kdef)] - u0`

   (criterio raccomandato NTC L/200, adottato **esplicitamente**, o più restrittivo).

Le formule conservano **il segno dei contributi elastici** e sottraggono la controfreccia `u0` solo una volta, ove pertinente. Usano il valore medio dei moduli `Emean`, `Gmean`, non `E0,05`. I singoli `u` **non sono inventati dal selettore**: provengono da analisi elastiche FEM per i rispettivi casi di carico e devono essere dichiarati e documentati. Il programma confronta la loro ricomposizione rara con `DeflectionMetres` del risultato FEM della combinazione corrispondente, alla stessa sezione e con convenzione concordata. Non è ancora presente un'integrazione automatica che estrae autonomamente tutte le frecce FEM: G5 usa attestazioni e risultati di provenienza esterna al modulo.

### Selettore italiano SLE

`Ntc2018TimberSleSelectorG5` imposta `kdef` pari a **0,60 / 0,80 / 2,00** (classi di servizio 1 / 2 / 3) per legno massiccio e lamellare; con posa prossima alla saturazione delle fibre ed essiccazione sotto carico richiede incremento **non inferiore a 2,0** e una valutazione documentata. Niente assunzione implicita di legno secco. La selezione include `ψ0` e `ψ2` per la porzione supportata della tabella NTC 2.5.I. Le categorie non gestite non sono approssimate. Non si riutilizzano `γM`, `kmod` né il profilo SLU G1.

### Ambito limitato / condizioni da attestare

- Solo **legno massiccio di conifera C** e **legno lamellare omogeneo GL*h** da catalogo M88-C; dati di catalogo solo indicativi, non attestati per la fornitura reale.
- Deformazioni elastiche singole dalla **stessa analisi FEM lineare**, stessa asta, sezione esatta, lato del salto, identificativi sorgente e combinazione rara completa; nessun inviluppo con effetti non concomitanti.
- Freccia per ogni singolo caso coerente con la distanza rispetto alla corda fra appoggi. `DesignEffectSnapshot.DeflectionMetres` deve riferirsi alla stessa definizione fisica; il modello FEM 2D non lo garantisce automaticamente.
- Devono risultare soddisfatti o trattati tramite valutazione indipendente: deformazione a taglio, scorrimenti/unioni, eventuali finiture fragili, scelta della sezione critica, appoggi fisici e inflessioni iniziali.
- Per la freccia finale serve evidenza della verifica del caso di tensioni da carico permanente superiori, indicativamente, al **30% della resistenza caratteristica a flessione** richiamato in Circolare C4.4.7.
- Per le mensole il confronto usa `L=2*sbalzo` per i due limiti raccomandati delle NTC; per l'istantanea totale resta il limite scelto dal progettista.

### Mancanze esplicite

Il modulo NON implementa un generatore completo di combinazioni NTC, la simulazione dei fenomeni reologici nel FEM, le oscillazioni dei solai, la verifica degli scorrimenti o della reale rigidezza delle connessioni, un inviluppo automatico della freccia massima lungo tutta l'asta, modifiche igrometriche, effetti su elementi fragili, e nessuna conformità complessiva del membro/struttura. Un singolo `Passed` vale **solo nel criterio e punto attestati**.

## File aggiunti

- `src/StructuralSolver2D.Verification.Timber/Ntc2018TimberServiceabilityG5.cs`
- `tests/StructuralSolver2D.Verification.Tests/M92/Ntc2018TimberServiceabilityG5Tests.cs`
- `benchmarks/G5_NTC2018_SLE_GOLDEN.json`
- `scripts/verify-g5-manifest.ps1`, `scripts/run-g5-tests.ps1`
- `G5_RELEASE_NOTES.md`

**I file C# esistenti in G4 sono invariati byte per byte.**

## Benchmark indipendente

Trave rettangolare C24 `b=100 mm`, `h=200 mm`, `L=4 m`, `Emean=11000 MPa` e carichi uniformemente distribuiti *fittizi* `G=0,5`, `Q1=0,7`, `W=0,2 kN/m`. Con `u(q)=5qL^4/(384EI)`, in mm: `uG=2,272727273`, `uQ1=3,181818182`, `uW=0,909090909`. `ψ0,W=0,6`, `ψ2,W=0`, `ψ2,Q1=0,3`, `kdef=0,6`, `u0=1 mm`:

- combinazione rara: **6,000000000 mm**;
- variabile istantanea rara: **3,727272727 mm** (limite `4000/300=13,333333333 mm`);
- istantanea totale netta: **5,000000000 mm**;
- finale totale netta: **6,936363636 mm** (limite `4000/200=20 mm`).

Questo benchmark è volutamente elastico Euler-Bernoulli e NON comprende direttamente il taglio o lo scorrimento delle unioni. La presenza di diverse categorie d'azione è un esempio aritmetico per l'algoritmo, non la convalida fisica di uno scenario edilizio. Vedi `benchmarks/G5_NTC2018_SLE_GOLDEN.json`.

## Collaudo richiesto su Windows / .NET 8

```powershell
.\scripts\verify-g5-manifest.ps1
.\scripts\run-g5-tests.ps1
```

Il runner effettua build Release con avvisi trattati come errori, test Verification e Catalogs, poi regressioni Core/FEM/Reporting/CLI. **I test xUnit non sono stati eseguiti nell'ambiente di preparazione, privo di dotnet SDK.** G4 è passato sul computer dell'utente, non qui.

## Roadmap

Con `G5 GATE PASSED` procedere a **G6**: audit tracciabilità delle verifiche locali, copertura dell'asta e completezza delle combinazioni/rapporti; poi **G7**, collaudo documentato finale. UI Avalonia soltanto alla fine.
