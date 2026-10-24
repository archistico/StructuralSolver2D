> **DOCUMENTO STORICO (23/10/2026).** Dopo la validazione M73, la visione di prodotto è cambiata: FEM 2D open source per acciaio e legno; **prima completamento motore FEM, poi verifiche NTC/Eurocodici SLU/SLE, e solo infine editor C#/Avalonia**. La vecchia integrazione OpenCad2D M74 non è più un prerequisito. Per prossimi milestone, copertura e limiti vedere **[Visione e roadmap 2D](vision-and-roadmap-2d.md)**. Il contenuto seguente è conservato a fini di tracciabilità, non come pianificazione vincolante.

> **23/10/2026 — M72 PROTOTIPO INTERNO, IN ATTESA DEI TEST WINDOWS.** M70-FIX1 validato dall’utente. M71-AUDIT-FIX1 non ancora testato su Windows; M72 prosegue come *branch sperimentale condizionale*, senza promuovere M71 a validato. Implementati legge momento–rotazione a incrudimento isotropo, cerniere agli estremi in serie con trave elastica Euler–Bernoulli, Newton locale, tangente condensata, forze locali/globali, energia e trial/commit. Benchmark C# indipendenti e `run-m72-tests.ps1` (pulizia `bin/obj`, build, M71+M72, regressione). Nessuna modifica al solver elastico, alle API/JSON/CLI o all’assemblaggio globale. [Nota tecnica M72](m72-first-concentrated-plastic-frame-element.md).

> **Stato corrente 23/10/2026 — M71 AUDIT-FIX1:** M51–M70-FIX1 hanno superato i test .NET confermati dall'utente. M71 è uno studio Frame2D con riferimenti C# e audit statico completato; il test Windows della suite e del nuovo wrapper PowerShell è ancora aperto. Nessun elemento Frame2D plastico di produzione è stato rilasciato. Il piano storico sottostante va letto con riferimento a questo stato; dettagli in [`m71-audit-pretest.md`](m71-audit-pretest.md).

> **23/10/2026 — M71 STUDIO DI FATTIBILITÀ IMPLEMENTATO, TEST .NET DA ESEGUIRE.** L’utente conferma la suite **M70-FIX1 superata** su Windows. M71 confronta plasticità flessionale concentrata/distribuita e propone un primo prototipo Frame2D con cerniere rotazionali alle estremità in serie con trave elastica, stato trial/commit, esclusione esplicita di N–M, geometric nonlinearities e collasso. Riferimenti indipendenti **solo C#/xUnit**, senza solver Frame2D plastico di produzione. Per correggere la console sovrapposta, tutti i runner dot-sourciano **dopo la pulizia** `scripts/stable-dotnet-console.ps1`, che normalizza stdout/stderr di dotnet, rimuove progress/ANSI e salva un log UTF-8 in TEMP. La funzionalità richiede collaudo su Windows. Vedere [`m71-frame2d-plasticity-feasibility.md`](m71-frame2d-plasticity-feasibility.md) e [`m71-console-streaming.md`](m71-console-streaming.md). **M72 condizionato al gate M71.**

> **Stato al 23/10/2026:** M63–M69 sono stati validati dall’utente mediante test .NET/C#; per M69 il FIX1 sul test HTML/JSON è incluso nella versione validata. M70 CQC è implementato, **in attesa di test Windows**. Il seguito M71–M75 resta pianificato. Le descrizioni sottostanti sono il piano originario, non uno stato di avanzamento automatico.

> **Aggiornamento 23/10/2026:** l’utente ha confermato la validazione M68. M69 viewer offline Truss2D non lineare è implementato ma richiede ancora compilazione/test .NET e ispezione manuale degli HTML generati. Vedere [`m69-nonlinear-viewer.md`](m69-nonlinear-viewer.md). M70–M75 restano programmati.

> **Stato operativo 23/10/2026:** M51–M67 validati dai test C# dell'utente. M68 API pubblica/JSON/CLI/report implementato, test Windows ancora da eseguire; M69 viewer e milestones successivi restano da implementare. Dettagli [`m68-public-nonlinear-api.md`](m68-public-nonlinear-api.md).

## Avanzamento confermato — 2026-10-23 (più recente)

M63–M66 hanno superato le suite C# confermate dall’utente. **M67 implementato, in attesa dei test**: post-processing lineare reazioni dinamiche, azioni elastiche di estremità e inviluppi; aggregazione interna M65. Rimangono differiti diagrammi sezionali dinamici distribuiti, ripartizione fra supporti ridondanti e API non lineare pubblica (M68). M69–M75 restano programmati. Usare solo C# per benchmark/fixture numerici, e PowerShell per pulizia/build/test.

> **Aggiornamento operativo 23/10/2026 — M66:** l'utente conferma i test C# di **M65 superati**. La suite di verifica **M66 è implementata, ma attende i test Windows .NET 8**. Tutti i nuovi benchmark C#/xUnit sono indipendenti dagli algoritmi di produzione, senza script Python. Il dettaglio corrente prevale sulle etichette storiche riportate più sotto: [`m66-nonlinear-validation-gate.md`](m66-nonlinear-validation-gate.md).

> **Aggiornamento operativo 23/10/2026:** M63 e M64 hanno superato i test C# confermati dall'utente. **M65 è implementato come modulo interno, ma deve ancora essere compilato e testato su Windows .NET 8**; M66–M75 restano pianificati. Il README è stato aggiornato in italiano, con badge corrispondenti alla versione `0.1.0`, a .NET 8 e alla licenza GPLv3. Per i nuovi benchmark numerici usare soltanto C#/xUnit e PowerShell per eseguirli, senza Python. Vedere [`m65-nonlinear-truss-dynamics.md`](m65-nonlinear-truss-dynamics.md).

> **Stato corrente 23/10/2026:** confermata validazione M71 e M72-FIX2 sul Windows dell’utente. M73 implementa un benchmark/prestazioni riproducibile (C#/.NET, CSV/JSON) in attesa della verifica finale. Il piano M74–M75 non è ancora implementato. Le vecchie righe con “pendente” sono note storiche.

# StructuralSolver2D — Piano operativo dopo M62 (M63–M75)

**Data di pianificazione:** 23 ottobre 2026  
**Base sorgente:** `StructuralSolver2D_M62_FIX1_PYTHON_RUNNER_2026-10-23_FULL.zip`  
**Stato corrente (2026-10-23):** M63, M64 e M65 validati dall’utente nei test C# Windows; **M66 implementato, in attesa dei test Windows .NET 8**; M67–M75 rimangono pianificati. Da M64 i benchmark e i riferimenti eseguibili devono essere solo C#/.NET, senza Python obbligatorio o opzionale. Questo documento è il piano prospettico, non una attestazione di test.

## 0. Baseline e vincoli non negoziabili

- M51–M61 e M59.1: l'utente ha confermato il superamento dei test Windows .NET 8; M62: l'utente ha confermato `M62 automated checks PASSED`, inclusi i test C# e i valori indipendenti incorporati. Il benchmark Python esterno è stato **saltato** perché `python` richiama l'alias Microsoft Store e `py -3` non è utilizzabile: non registrarlo come eseguito su Windows.
- Il prototipo M62 è **solo SDOF**; nessuna plasticità di Truss2D/Frame2D, nessuna capacità di collasso e nessuna certificazione normativa.
- Le verifiche visive reali nei browser dei viewer precedenti rimangono distinte dai test JavaScript automatici.
- Conservare il progetto .NET 8 e la separazione `Core` / `Analysis` / `Reporting` / `Cli`. Non accoppiare `Analysis` a OpenCad2D né ad alcuna libreria UI.
- Ogni `scripts/run-m*-tests.ps1` deve invocare **come prima azione operativa** `scripts/clean-project-build.ps1`, il quale rimuove solamente le cartelle `bin`/`obj` appartenenti ai progetti MSBuild, poi build Release + test mirati + intera regressione.
- Nessuna tolleranza deve essere allargata soltanto per far passare un benchmark. Ogni calcolo nuovo richiede scala fisica, controlli su valori non finiti, casi degeneri e almeno un riferimento indipendente.
- Salvare ad ogni incremento il **progetto ZIP completo** e aggiornare `docs/structural/roadmap.md`, `ai-handoff.md`, `CHANGELOG.md`, `development-plan.md` se pertinente. Gli script di test non devono introdurre carichi statici fittizi negli esempi dinamici.

## 1. Priorità e dipendenze

| Priorità | Milestone | Deliverable principale | Dipende da | Gate |
|---|---|---|---|---|
| P0 | **M63** | Elemento Truss2D elasto-plastico isolato (**test C# validati**) | M62 | Materiale, orientamento, tangente e stato verificati |
| P0 | **M64** | Solutore statico 2D incrementale per reticolari plastici (**test C# validati dall’utente**) | M63 | Equilibrio, passi, vincoli, rollback e riferimenti |
| P0 | **M65** | Solutore transitorio non lineare per reticolari 2D (**test C# confermati dall’utente**) | M64, M53–M54, M62 | Newmark–Newton, massa, damping, energia e convergenza |
| P0 | **M66** | Catalogo di validazione non lineare 2D (**implementato, test .NET in attesa**) | M63–M65 | Benchmark analitici/indipendenti e regressione completa |
| P1 | **M67** ✅ test C# validati | Reazioni e sforzi interni dipendenti dal tempo | M59–M60; M66 per test incrociati | Equilibrio dinamico e inviluppi corretti |
| P1 | **M68** implementato; test da eseguire | API/JSON/CLI/report opt-in per reticolari non lineari | M66–M67 | Contratti stabili, round-trip, reporting, compatibilità |
| P1 | **M69** | Viewer dinamico e non lineare, QA visivo | M67–M68 | Browser reali, grafici corretti, playback e convenzioni |
| P2 | **M70** | Rafforzamento analisi spettrale e risposta sismica lineare | M60, M67 | SRSS/CQC verificati, modalità e limiti espliciti |
| P2 | **M71** | Progetto verificato di plasticità flessionale Frame2D | M66–M69 | Decisione tecnica su modello di cerniera/sezione e N–M |
| P2, condizionale | **M72** | Primo elemento Frame2D con plasticità flessionale | Accettazione M71 | Benchmark momenti–rotazioni e oggettività del modello |
| P2 | **M73** | Robustezza e prestazioni del backend numerico | Profiling M65/M67/M72 | Benchmark performance + riproducibilità |
| P2 | **M74** | Boundary/adapter OpenCad2D e viewer labels (M49–M50) | API consolidate, M68–M69 | Integrazione senza riferimenti UI nel core |
| P2 | **M75** | Consolidamento rilascio e Validation Manual | Milestone accettati | Suite completa, matrice di copertura e limitazioni |

**Regola di avanzamento:** ogni milestone passa da *pianificato* a *implementato in attesa di test*, poi *test C# superati*, infine *validato nel perimetro dichiarato*. Gli step successivi non acquisiscono automaticamente la validità ingegneristica del precedente.

## 2. Fase A — Non linearità 2D: M63–M66

### M63 — Elemento Truss2D elasto-plastico

**Obiettivo:** passare dalla molla M62 a una singola asta assiale 2D a piccole deformazioni, con geometria iniziale fissa; **nessun** risolutore globale e nessuna plasticità flessionale.

**Attività:**
1. Definire `E` [kN/m²], `A` [m²], `L₀` [m], tensione di snervamento [kN/m²] e modulo di incrudimento [kN/m²]. Conversione rigorosa tra deformazione `ε`, forza assiale `N=Aσ`, rigidezza tangente `A E_t/L₀` ed energia [kN·m].
2. Proiettare gli spostamenti globali `Ux1,Uy1,Ux2,Uy2` sulla direzione dell'asta, usando `B=[-c,-s,c,s]/L₀`. Calcolare `f_int=A σ [-c,-s,c,s]` e `K_t=(A E_t/L₀) d dᵀ` con `d=[-c,-s,c,s]`.
3. Riutilizzare *la legge costitutiva*, non la classe molla con unità incompatibili, e definire `StateCommitted` / `TrialUpdate` senza mutazione fino al commit.
4. Escludere esplicitamente grandi rotazioni, rigidezza geometrica, instabilità e rottura; nessuna rigidezza artificiale nelle rotazioni nodali.

**Test di accettazione:** asta orizzontale, verticale, 30°/45°, inversione nodi, traslazione rigida, ciclo carico–scarico, `H=0`, sensibilità agli incrementi, derivata numerica della tangente, confronto con soluzione assiale chiusa e conservazione del lavoro. Errori dedicati per lunghezza/area/moduli e stato non fisico. **Consegna:** elemento interno + xUnit + nota matematica.

### M64 — Equilibrio statico incrementale di reticolari plastiche

**Obiettivo:** assemblare forze interne e rigidezza tangente dei soli elementi Truss2D in un sistema 2D multi-asta, senza sostituire l'attuale analizzatore lineare.

**Attività:**
1. Stato plastico indipendente per elemento e confronto tra forze esterne, forze interne e reazioni.
2. Applicazione incrementale dei carichi con Newton–Raphson, tolleranza assoluta e relativa fisicamente scalata, numero massimo di iterazioni, rollback **atomico di tutto il passo**, messaggi con nodo/elemento.
3. Gestione dei vincoli, inclusi supporti inclinati e ridondanti, mediante riduzione cinematica coerente; nessuna trasposizione ingenua del solver SPD modale.
4. Carico controllato nelle fasi iniziali; per plasticità perfetta sotto carico limite dichiarare la non unicità/singolarità. Pianificare un controllo di spostamento mirato per superare il limite, anziché inserire regolarizzazioni silenziose.
5. Restituire traiettorie carico–spostamento, sforzi assiali e stati storici ad ogni incremento.

**Test di accettazione:** 1 asta, due aste in parallelo, triangolo stabile e aste con snervamenti diversi; residuo globale, storia di carico monotona e ciclica, non convergenza e rollback, degenerazioni/elementi inutilizzati, confronto indipendente con equilibrio assiale e benchmark lineare prima dello snervamento.

### M65 — Dinamica non lineare di reticolari 2D

**Obiettivo:** risolvere negli spostamenti **fisici** `M a+C v+f_int(u,z)=F(t)`, senza ridurlo ai modi propri del sistema lineare.

**Attività:**
1. Usare matrici di massa M53, gestione dei vincoli M54 e Newmark medio M59, ma con rigidezza tangente aggiornata per iterazione.
2. Damping esplicito (`C=αM+βK₀` con rigidezza elastica iniziale, o altro modello dichiarato). Non sostituire `K₀` con `K_t` durante l'analisi senza una decisione tecnica esplicita.
3. Stato trial derivato sempre dal passo n; commit solo al raggiungimento delle condizioni su residuo e incremento; rollback di spostamenti, velocità, accelerazioni ed energie in caso di fallimento.
4. Definire limite iterazioni, controllo `dt`, eventuale substepping controllato e risolutore adeguato a tangenti non necessariamente SPD; non iniettare masse nodali fittizie.
5. Separare residuo di equilibrio, bilancio discreto e bilancio energetico fisico con dissipazione plastica.

**Test di accettazione:** limite lineare uguale a M59; singola asta vincolata equivalente a M62 con conversione dimensionale coerente; storia impulsiva finita, inversioni di segno, campionamento carico, limiti al passo, convergenza in spostamento/velocità/accelerazione, energia non negativa, rollback multi-asta.

### M66 — Gate di verifica non lineare 2D

**Obiettivo:** vietare l'esposizione dell'API pubblica prima di una validazione indipendente documentata.

**Attività e test:** fixture chiuse e soluzioni algebriche indipendenti, confronto di programmi alternativi soltanto a parità di ipotesi, prove di orientamento e rotazione, energia, piccoli/grandi incrementi di carico, `dt` raffinato, ricerca di singolarità, traslazioni rigide, test con matrici mal condizionate e stati storici corrotti. Confrontare anche momenti di snervamento, energia dissipata e residui, non solo spostamenti finali.

**Gate:** documentare tolleranze fissate prima dei risultati, test ripetibili e intera regressione. **Se M66 fallisce, M68 pubblico resta bloccato**; le API lineari continuano a funzionare.

## 3. Fase B — Risultati utili e strumenti: M67–M69

### M67 — Post-processing dinamico delle strutture lineari e non lineari

**Problema attuale:** M59–M60 pubblicano grandezze e massimi nodali ma non storie/inviluppi di reazioni vincolari e sollecitazioni interne di ogni asta.

**Attività:** ricostruzione coerente di `N`, `V`, `M` e reazioni per ogni campione, tenendo conto dell'equilibrio **dinamico** (massa e smorzamento), dei carichi effettivi e della convenzione dei segni. Per reticolari non lineari includere forza interna, deformazione plastica e dissipazione per elemento. Distingue forze interne elastiche da forze inerziali e da reazioni; non usare soltanto `K u-F`.

**Test:** limiti statici M51, travi con carichi nodali e casi temporali, oscillazioni libere, vincoli inclinati, reticolare plastica, bilanci alle interfacce, valori estremi ed istanti, conservazione delle unità. Disabilitare esplicitamente grandezze non ancora calcolabili (e.g. estremi con release non supportate in dinamica).

### M68 — Contratti pubblici e CLI per non linearità opt-in

**Attività:** nuovo schema JSON facoltativo con proprietà di plasticità e storia incrementale; validazione distinta dallo schema lineare; facciata `AnalyzeNonlinear...` solo dopo M66; report su snervamento, stati, iterazioni, picchi, dissipazione e limitazioni. Comandi CLI e CSV/Markdown solo quando risultati e significato fisico sono stabili.

**Test:** JSON legacy immutati, rifiuto input incompleti, conversioni/unità, round-trip, CSV invariant, errori significativi, limiti modello, suite end-to-end senza mischiare esempi statici e dinamici.

### M69 — Viewer e ispezione browser

**Attività:** animazione delle deformate dipendenti dal tempo, confronto elastico/plastico, stato di snervamento delle aste, grafici temporali per `Ux/Uy`, sforzi ed energia, slider e grafica non interpretata come scala fisica reale; aggiunta/chiusura dei tooltip M49 dove utile.

**Test:** JavaScript sintassi e DOM mock + **Playwright/browser reale o verifica manuale documentata** con screenshot, zoom, pan, layer, ampiezze, velocità, NaN/overflow, esportazione viewer HTML standalone; coerenza grafica dei versi. Non dichiarare verificata la UI tramite soli test unitari.

## 4. Fase C — Capacità avanzate, con nuovi gate: M70–M72

### M70 — Rafforzamento sismico lineare non normativo

**Attività:** CQC per modi ravvicinati e confronto SRSS/CQC su esempi adatti; diagnostica della massa partecipante e della troncatura; controllo campionamento/interpolazione degli accelerogrammi e dello spettro, convenzioni pseudospettrali e relativa/assoluta, raccordo a M67 per le sollecitazioni. Valutare combinazioni di direzioni e forze equivalenti **solo** con definizioni teoriche e verifiche indipendenti, non come automatismo normativo.

**Gate:** caso SDOF, due modi vicini e due ben separati, simmetria e correlazioni di combinazione, confronto con integrazione temporale quando giustificabile, output sempre `NON-NORMATIVE`.

### M71 — Fattibilità dettagliata della plasticità flessionale Frame2D

**Prima decisione, non codice:** scegliere fra cerniere plastiche concentrate e formulazione a plasticità distribuita. Definire relazione momento–rotazione, rigidezza e lunghezza delle cerniere, comportamento combinato sforzo normale–momento, reversibilità, sezioni e parametri del materiale. Valutare dipendenza dalla mesh, spostamenti grandi e rilascio di estremità **come problemi distinti**.

**Gate:** modello e test univoci, ipotesi fisiche e limitazioni scritte, scelta approvata. Nessuna promessa di collasso realistico.

### M72 — Primo elemento Frame2D non lineare (condizionale)

Implementare **solo se** il gate M71 è superato; elemento e stato di cerniera isolati, assemblaggio statico incrementale, Newton, ciclo momento–rotazione, limiti plastici e rollback. La dinamica del telaio plastico è un ulteriore milestone e **non è compresa automaticamente** in M72.

**Gate:** trave con cerniera nota, portale semplice, momento di snervamento verificabile, forze di estremità, controllo dei vincoli e delle release, variazione mesh e regressioni.

## 5. Fase D — Industrializzazione controllata: M73–M75

### M73 — Prestazioni, robustezza e diagnostica

**Attività:** misurare numero DOF, costo di assemblaggio/fattorizzazioni, occupazione memoria, iterazioni Newton e accuratezza per modelli progressivamente grandi; confronto denso/sparso e solver appropriati alla tangente soltanto dove misure reali lo giustifichino; gestione esplicita di sistemi indefiniti/singolari e scala numerica. Niente refactoring radicale senza baseline di benchmark.

**Gate:** soglie di performance esplicite, nessun peggioramento di accuratezza, suite numerica identica e diagnostiche utilizzabili.

### M74 — Integrazione OpenCad2D e M49–M50

**Attività:** completare la frontiera già pianificata in M50 tramite adapter esterno: CAD→`StructuralModel` (unità, geometrie, orientamento assi, vincoli, release, carichi), soluzione, conversione risultati→layer/tooltip UI. Isolare versioni schema e import/export. Chiudere M49 tooltips solo con verifica reale nel viewer.

**Gate:** demo end-to-end su 3–5 modelli geometricamente diversi, mapping bidirezionale controllato, assenza di dipendenze CAD dentro i 4 progetti `Core/Analysis/Reporting/Cli`, test di segno dei carichi e carichi distribuiti.

### M75 — Consolidamento e Validation Manual

**Attività:** inventario ufficiale delle funzionalità `stabile`, `sperimentale`, `esclusa`; indice di benchmark con fonte e tolleranze; esempi pubblici e casi falliti; manuale per l'utente con convenzioni di segno, unità e limiti di calcolo; script riproducibili, changelog versionato, pacchetto ZIP pulito; revisione browser reale e documentazione del limite di responsabilità.

**Gate:** build .NET 8 da pulizia `bin/obj`, test delle 9 librerie/progetti, esportazioni HTML/PDF/XLSX dove previste, benchmark indipendenti, una matrice di tracciabilità requisiti→test, nessuna regressione nei casi statici lineari e dinamici.

## 6. Decisioni aperte prima di M63–M65

1. **Unità del materiale:** snervamento in `kN/m²`, esposto eventualmente in MPa solo con conversioni esplicite; in M62 la molla usa invece `kN` e `kN/m`. Vietato copiare direttamente i parametri senza conversione.
2. **Geometria:** solo `small-displacement` e `small-strain` in M63–M66; niente termini `P–Δ` o rigidezza geometrica implicita.
3. **Legge plastica:** primo incremento isotropo come M62. Una futura componente di incrudimento cinematico deve essere distinta e validata a parte.
4. **Vincoli:** modello Truss2D con solo DOF traslazionali attivi; rotazioni prive di rigidezza/massa da non trattare come meccanismi fisici. Supporti inclinati e vincoli ridondanti mantenuti.
5. **Solver:** la tangente diventa semidefinita o indefinita in casi limite; servono solver e diagnostiche appropriati, non affidarsi a Cholesky M55.
6. **Carichi:** prima solo nodali e controllati; azioni distribuite/moto del supporto o geometria variabile richiedono milestone indipendenti.
7. **Criteri di chiusura:** test C# su Windows obbligatori; benchmark Python esterno opzionale, e se salta scriverlo sempre come `SKIPPED`, mai `PASSED`.

## 7. Primo sprint eseguibile: M63

**Obiettivo unico:** elemento elastoplastico Truss2D valutabile isolatamente, senza assemblaggio globale.

| Sequenza | Output | Check di uscita |
|---|---|---|
| 63.1 | Modello dati e validazione dimensionale | Errori materiali/area/lunghezza/unità |
| 63.2 | Stato committed + return mapping 1D | Recupero elastico e dissipazione coerenti |
| 63.3 | `f_int` globale + `K_t` 4×4 | Trasformazioni e derivata numerica |
| 63.4 | Test soluzione assiale e rotazioni elementi | Casi 0°, 45°, 90°, inversione e moto rigido |
| 63.5 | Cicli e fallimenti | `H=0`, scarico, commit/rollback, overflow |
| 63.6 | Documentazione + script | Primo comando di pulizia, build, xUnit, regressione completa |

**Definizione di 'M63 completato':** l'utente esegue `scripts/run-m63-tests.ps1`, la suite passa e il risultato è ancora dichiarato **elemento interno sperimentale**, non `AnalyzeNonlinear` pubblico. Consegnare anche ZIP completo, nuova roadmap e changelog.

## 8. Attività trasversali e non prioritarie

- La vecchia roadmap **M43–M45** dispone già di XLSX, PDF e catalogo sezioni JSON; **non** vanno ripianificati come se fossero mancanti. M49 (tooltip) e M50 (boundary con OpenCad2D) conservano invece lavoro residuo.
- I viewer M57–M59 hanno superato i controlli JS disponibili, ma la revisione grafica reale è un gate aperto; inserirla in una piccola sessione dedicata appena possibile.
- L'aggiornamento a norme sismiche vigenti, stati limite strutturali, fenomeni di collasso e materiale RC/acciaio avanzato è un programma distinto che non segue automaticamente da M70 o M72.
- Se la priorità dell'utente diventa l'impiego quotidiano con OpenCad2D, è legittimo **anticipare M67 e M74** senza cambiare la matematica dei milestone M63–M66; mantenere però gate separati.

**Conclusione operativa:** iniziare con **M63**. Non cambiare codice lineare durante M63; validare bene l'elemento prima di progettare M64. Aggiornare la roadmap dopo ogni milestone con esito reale e non soltanto con intenzioni.
