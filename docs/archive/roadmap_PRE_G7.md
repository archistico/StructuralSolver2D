> **STATO CORRENTE 24/10/2026 — M85-B pronto per test Windows:** l'utente conferma il superamento di M85-A. I benchmark M83-A originali sono ora inclusi e commentati in `docs/M85B_VALIDAZIONE_INTEGRATA.md`: le prestazioni osservate non motivano ancora un solutore sparso. M85-B aggiunge test end-to-end JSON/API/CLI/CSV/Markdown/XLSX/PDF, senza modifiche al codice FEM. Gate: `scripts/run-m85b-tests.ps1`; opzionale `-WithBenchmark` per nuova misura con 25 ripetizioni. Dopo i gate: M85-C audit esterno/casi negativi e M86 release Core elastico. M84 plasticità globale differibile. Le note di seguito sono checkpoint storici.

> **STATO CORRENTE 24/10/2026 — M85-A da testare:** M83-A e precedenti superati sul Windows dell'utente; dati numerici CSV M83 non allegati. M85-A (formule statiche indipendenti, reciprocità e covarianza) pronto per `scripts/run-m85-tests.ps1`; [matrice di validazione](m85a-independent-validation.md). M85-B completerà riferimenti esterni e verifiche JSON/API/CLI/report, poi M86 Core FEM. Matrici sparse M83-B e plasticità globale M84 rinviabili. I checkpoint precedenti riportati di seguito restano riferiti alle date storiche in cui furono scritti.

> **Stato aggiornato 24/10/2026:** M82 con test Windows superati dopo FIX CS0246. M83-A (benchmark Frame2D reali e M82) implementato e in attesa di test Windows. Il solver sparso resta condizionato a misure, M84 opzionale, M85/M86 prioritari per completare la release del motore FEM. [Dettagli M83-A](m83a-frame2d-scalability-decision.md).

> **STATO PREVALENTE 24/10/2026 — M82 IMPLEMENTATO, GATE WINDOWS PENDENTE.** L'utente conferma M81 PASS su Windows (.NET 8). Pertanto M75–M81 validati. M82 aggiunge un'analisi globale **linearizzata** Frame2D con N del primo ordine congelati, `Kt=Ke+Kg`, reazioni e moltiplicatore critico da perdita di definitezza positiva. Ambito ristretto: aste senza release, carichi nodali, supporti rigidi non inclinati; no secondo ordine iterativo, modi critici, normative o GUI. Runner `scripts/run-m82-tests.ps1`, nota [M82](m82-global-linearized-second-order.md). L'integrazione generale, il modo critico e le estensioni d'ambito restano aperti.

> **24/10/2026 — M81 IMPLEMENTATO, TEST WINDOWS PENDENTI:** l’utente ha confermato superato l'intero runner M80-FIX3: M75–M80 risultano validati dai test Windows. M81 introduce `Frame2DInitialStressM81` come componente **isolato**: `Kgeo` 6×6 di trave Euler–Bernoulli sotto N costante prescritta, `Kt=Kel+Kgeo` e piccolo benchmark di mensola precompressa con avviso di instabilità. Nessun solver globale P–Δ, nessuna verifica normativa, nessuna modifica ai solver esistenti. Test xUnit solo C#, runner con pulizia e regressione completa; [nota tecnica M81](m81-frame2d-initial-stress.md). M82 resta pianificato per assemblaggio globale e modi critici.

> **M80-FIX3 — 24/10/2026 (gate Windows ancora aperto):** log utente M80-FIX2: build OK, 0 avvisi/0 errori, 17/17 geometria, 25/25 cataloghi e unità, gate M78/M79 13/14. Ultimo fallimento dovuto a pareggio del **minimo** taglio in M78: V=−3 kN sul tratto `(2,8]`, il finder usa `x=8 / Continuous`, non `x=2 / AfterPointLoad`. Corretta asserzione; aggiunti controlli diretti del salto `+9 → −3 kN` e continuità del momento. Nessun codice FEM o PowerShell cambiato. [Nota FIX3](m80-fix3-minimum-shear-tie.md). M75–M80 restano da validare con `run-m80-tests.ps1` intero.

> **M80-FIX2 — 24/10/2026 (gate Windows ancora aperto):** il controllo dettagliato M80-FIX1 ha localizzato due fallimenti in `Analysis.Tests` (539/541 passati): una parità corretta ma attesa male in M78 e un coefficiente zero vietato nel test M79. Sono state modificate solo le asserzioni/fixture dei test e il runner (gate mirato, log meno verboso); nessun solutore o validatore FEM modificato. [Nota FIX2](m80-fix2-m78-m79-tests.md). Eseguire `scripts/run-m80-tests.ps1` e verificare i quattro progetti prima di accettare M75–M80.

> **M80-FIX1 (24/10/2026):** `run-m80-tests.ps1` ora riporta separatamente gli esiti Core/Analysis/Reporting/Cli per chiarire l’origine di eventuali exit code non-zero. Il riepilogo positivo di `Cli.Tests` (231/231) non basta a validare la regressione cumulativa. In attesa di riesecuzione su Windows. [Nota tecnica](m80-fix1-regression-diagnostics.md).

> **23/10/2026 — M80 IMPLEMENTATO / GATE WINDOWS PENDENTE.** Geometrie ideali rettangolari, di legno, circolari, RHS a spigoli vivi e doppio T simmetrico; calcolo A, I nel piano/fuori piano, W elastici e raggi d’inerzia; catalogo JSON dimensionale in mm con `Illustrative`/`SourceDocumented`, ponte alla `StructuralSection` classica senza cambiare FEM. Tutte le prove numeriche in C#/xUnit; [nota tecnica M80](m80-section-geometric-properties.md). **Ultimo gate confermato M73; M75–M80 restano in attesa della suite cumulativa `scripts/run-m80-tests.ps1`.** Segue M81: secondo ordine nel piano. Nessuna verifica normativa o dato commerciale certificato.

> **2026-10-23 — Checkpoint M79 (stato corrente):** estremi globali N/V/M per le aste Frame2D su casi/combinazioni statiche esplicite, identificazione della combinazione governante, API .NET + esportazioni CSV/Markdown, test xUnit C#. Nessuna modifica al solver e nessuna verifica normativa. **M73 ultimo milestone testato dall’utente; M75–M79 in attesa di test Windows.** Eseguire `scripts/run-m79-tests.ps1` che comprende l’intera regressione. [Nota M79](m79-exact-static-force-envelopes.md). M80: proprietà geometriche delle sezioni.

> **CHECKPOINT 23/10/2026 — M78 implementato, TEST WINDOWS PENDENTI.**
> Post-processing analitico N/V/M per asta Frame2D e singolo caso/combinazione statica; conservati i salti dei carichi concentrati e i segni. Non altera il solver FEM e non sostituisce il vecchio summary campionato. [Nota M78](m78-exact-frame-force-extrema.md).
> **M73 ultimo milestone con test Windows confermati. M75–M78 da verificare con `scripts/run-m78-tests.ps1`.** Prossimo M79: inviluppi tra casi e combinazioni governanti. La roadmap di prodotto autorevole resta [qui](vision-and-roadmap-2d.md).

> **CHECKPOINT OPERATIVO 23/10/2026 — M77 implementato, test WINDOWS PENDENTI.**
> M77: molle nodali in assi locali del supporto (`StiffnessUx`, `StiffnessUy`, `StiffnessRz`), cedimenti imposti `SupportSettlement` per caso di carico e combinazione, vincoli non omogenei, reazioni separate delle molle. Ambito: statica lineare Frame2D/Truss2D/mista; dinamica/non linearità NON estese. [Nota M77](m77-elastic-supports-and-settlements.md).
> **M73 rimane l'ultimo milestone con test Windows .NET confermati.** M75, M76 e M77 sono implementati senza esecuzione .NET in questo ambiente; gate futuro: `scripts/run-m77-tests.ps1` con regressione completa. Prossimo M78 *solo dopo i gate M75–M77*.

> **CHECKPOINT OPERATIVO 23/10/2026 — M76 implementato; M75/M76 TEST WINDOWS PENDENTI.**
> M76: peso proprio esplicito statico Frame2D/Truss2D/misti, combinazioni, diagrammi e equilibrio; [nota tecnica](m76-automatic-self-weight.md).
> **M73 è l'ultimo milestone con suite .NET 8 confermata dall'utente.** Prossimo M77 vincoli elastici/cedimenti, dopo esito dei test M75/M76.

> **CHECKPOINT OPERATIVO 23/10/2026 — M75 implementato / test Windows pendenti.**
> La roadmap di prodotto autorevole per M74–M107 è [vision-and-roadmap-2d.md](vision-and-roadmap-2d.md).
> M75 aggiunge writer JSON canonico, preflight opt-in e regressione di round-trip;
> [nota tecnica M75](m75-json-model-contract.md). M73 è l'ultimo milestone validato dai test dell'utente.
> Il vecchio calendario M74–M75/OpenCad2D riportato in passaggi storici qui sotto è archiviato.

> **DECISIONE DI PRODOTTO 23/10/2026 — STATO PREVALENTE.** L’utente conferma M73 **testato con successo in .NET 8**. L’obiettivo è un software 2D open source per travi/telai/reticolari in **acciaio e legno**, con ordine inderogabile di sviluppo: **motore di calcolo completo e validato → verifiche NTC/Eurocodici SLU/SLE → editor desktop C#/Avalonia**. Esclusi 3D, c.a., muratura, piastre/pareti, BIM, X-LAM. Le vecchie indicazioni M74 integrazione OpenCad2D e M75 consolidamento come punto d’arrivo sono **superate**; le note precedenti si conservano per tracciabilità storica. **Roadmap corrente:** [Visione e roadmap 2D](vision-and-roadmap-2d.md). Nessuna verifica normativa attualmente disponibile.

> **23/10/2026 — M72-FIX1 CONSOLE:** la prima esecuzione M72 su Windows si è arrestata prima della build con `MSB1008` a causa dell'opzione PowerShell `-v:minimal` applicata alla funzione wrapper `dotnet` (interpretata come `-Verbose` / argomento `minimal` spurio). L'opzione abbreviata è stata eliminata dai runner M71 e M72; normalizzata anche la decodifica UTF-8 temporanea con ripristino. **I test M71/M72 NON sono ancora passati: ripetere `scripts/run-m72-tests.ps1` dal pacchetto M72-FIX1.** Il codice C# rimane invariato.

> **23/10/2026 — M72 PROTOTIPO INTERNO, IN ATTESA DEI TEST WINDOWS.** M70-FIX1 validato dall’utente. M71-AUDIT-FIX1 non ancora testato su Windows; M72 prosegue come *branch sperimentale condizionale*, senza promuovere M71 a validato. Implementati legge momento–rotazione a incrudimento isotropo, cerniere agli estremi in serie con trave elastica Euler–Bernoulli, Newton locale, tangente condensata, forze locali/globali, energia e trial/commit. Benchmark C# indipendenti e `run-m72-tests.ps1` (pulizia `bin/obj`, build, M71+M72, regressione). Nessuna modifica al solver elastico, alle API/JSON/CLI o all’assemblaggio globale. [Nota tecnica M72](m72-first-concentrated-plastic-frame-element.md).

> **23/10/2026 — M71 AUDIT-FIX1 PRE-TEST (stato più recente).** M70-FIX1 validato dall'utente. L'audit statico di M71 è completato e ha corretto un leak delle variabili di ambiente nel wrapper PowerShell. Il runner M71 include ora controlli su codici di uscita, log e isolamento dell'ambiente, ma **M71 non è ancora validato sul Windows dell'utente**. I solver numerici restano invariati; M72 resta condizionato al gate. Dettagli: [audit pre-test](m71-audit-pretest.md).

> **23/10/2026 — M71 STUDIO DI FATTIBILITÀ IMPLEMENTATO, TEST .NET DA ESEGUIRE.** L’utente conferma la suite **M70-FIX1 superata** su Windows. M71 confronta plasticità flessionale concentrata/distribuita e propone un primo prototipo Frame2D con cerniere rotazionali alle estremità in serie con trave elastica, stato trial/commit, esclusione esplicita di N–M, geometric nonlinearities e collasso. Riferimenti indipendenti **solo C#/xUnit**, senza solver Frame2D plastico di produzione. Per correggere la console sovrapposta, tutti i runner dot-sourciano **dopo la pulizia** `scripts/stable-dotnet-console.ps1`, che normalizza stdout/stderr di dotnet, rimuove progress/ANSI e salva un log UTF-8 in TEMP. La funzionalità richiede collaudo su Windows. Vedere [`m71-frame2d-plasticity-feasibility.md`](m71-frame2d-plasticity-feasibility.md) e [`m71-console-streaming.md`](m71-console-streaming.md). **M72 condizionato al gate M71.**

> **23/10/2026 — M70-FIX1 implementato, test Windows da rieseguire.** Corretto il benchmark CQC a due campate: il nodo intermedio B di una catena di sole aste Truss2D era libero in direzione trasversale, creando un reale `M54_INTERNAL_MECHANISM`. Aggiunto l'appoggio B.Uy preservando i due gradi di libertà assiali, più un test regressivo che conferma il rifiuto del modello instabile. Per rendere l'output PowerShell leggibile, tutti i 21 runner `run-m*-tests.ps1` usano `dotnet build/test --tl:off` per disabilitare il terminal logger animato di .NET. Nessun cambiamento ai solver o alle formule CQC. Vedere [`m70-fix1-cqc-benchmark-console.md`](m70-fix1-cqc-benchmark-console.md).

> **23/10/2026 — M70 IMPLEMENTATO, TEST .NET 8 ANCORA DA ESEGUIRE.** L’utente conferma la suite M69-FIX1 superata sul proprio Windows. M70 aggiunge alla risposta sismica spettrale M60 la **CQC opzionale** per modi ravvicinati, con smorzamento modale uniforme esplicito e combinazione dei contributi **con segno**. **SRSS rimane il predefinito** per compatibilità API/JSON/CSV. API `SeismicModalCombination`, opzioni `combination` e `modalDampingRatio` nel blocco `seismic.spectrum`, intestazioni report/CSV distinte e benchmark C#/xUnit. `scripts/run-m70-tests.ps1` richiama come prima operazione la pulizia centralizzata `bin/obj`, poi build Release, test mirati, regressione e CLI E2E. Nessun Python da eseguire. **M70 sperimentale fino all’esito Windows; non è una verifica sismica normativa.** Vedere [`m70-cqc-modal-combination.md`](m70-cqc-modal-combination.md). Dopo M70: M71 studio di fattibilità plasticità flessionale Frame2D.

> **23/10/2026 — M69-FIX1: CORREZIONE TEST XSS/JSON, GATE .NET ANCORA APERTO.** La prima esecuzione Windows di M69 ha individuato un’aspettativa scorretta in `MaliciousLabels_AreEscapedInsideStandaloneHtml`: un valore inserito in JSON HTML-safe viene codificato come `\u003C/script\u003E`, non `&lt;/script&gt;`. Il test verifica ora due soli blocchi `<script>`, assenza del payload letterale, parsing del dataset e recupero identico di titolo e identificatore del nodo. Nessun cambiamento a exporter, solver, API o JavaScript. **Rieseguire `scripts/run-m69-tests.ps1` e la QA browser prima di chiudere M69**. Vedere [`m69-fix1-json-escaping.md`](m69-fix1-json-escaping.md).

> **23/10/2026 — M68 VALIDATO dall’utente; M69 implementato, TEST .NET 8 DA ESEGUIRE.** M69 aggiunge viewer HTML offline per statica incrementale e dinamica Truss2D non lineari: selezione di incrementi accettati, geometrie deformate con amplificazione grafica, identificazione della plasticità accumulata, curve carico–spostamento, forza assiale, deformazione plastica e dissipazione globale. Due comandi `nonlinear-static-viewer` / `nonlinear-time-viewer`, test C#/xUnit, CLI E2E e script `run-m69-tests.ps1` con pulizia obbligatoria iniziale `bin`/`obj`. M64/M65 e API M68 invariati nel significato fisico. Leggere [`m69-nonlinear-viewer.md`](m69-nonlinear-viewer.md). La QA visiva reale resta da completare.

> **23/10/2026 — M67 confermato dai test dell’utente; M68 implementato, da compilare e validare.** M68 espone i solutori M64/M65 per Truss2D plastici attraverso una nuova API pubblica, JSON dedicato, sei comandi CLI e report Markdown/CSV. I benchmark nuovi sono soltanto C#/xUnit. Lo script `run-m68-tests.ps1` inizia con la pulizia `bin`/`obj`. Non altera i solver esistenti e non aggiunge viewer (M69). Leggere [`m68-public-nonlinear-api.md`](m68-public-nonlinear-api.md).

# Checkpoint M67 — 2026-10-23 (NUOVO)

**M66:** l’utente ha confermato `test passati` sul proprio .NET 8. **M67:** implementato e **NON ancora validato con build/test .NET 8**. Comprende `StructuralDynamicForceRecoveryService` per il post-processing lineare M59: reazioni globali `M*a+C*v+K*u-F`, azioni elastiche locali di estremità e inviluppi campionati; esportazioni `time-forces`, `time-forces-report`, `time-forces-csv`; aggregazione interna delle reazioni/forze assiali/stati plastici accettati M65. Nessun aggiornamento dei solver Newmark e non lineari. Vedere [`m67-dynamic-forces-and-reactions.md`](m67-dynamic-forces-and-reactions.md) e `scripts/run-m67-tests.ps1` (prima pulizia `bin`/`obj`, poi build, xUnit C# e regressione integrale). **Non** dichiarare disponibili diagrammi dinamici continui `N/V/M(x,t)` o ripartizioni univoche tra supporti ridondanti. M68 API pubblica per non linearità resta successivo alla convalida M67.

---

> **2026-10-23 — M66 implementato, test .NET 8 ancora da eseguire:** l’utente ha confermato **M65 superato**. M66 introduce esclusivamente una suite di validazione numerica C#/xUnit con riferimento algebrico indipendente per equilibri di due aste e per Newmark per rami, prove di invarianti, energia, storia plastica, raffinamento e meccanismi. `scripts/run-m66-tests.ps1` inizia con pulizia `bin`/`obj`, poi build, test M63–M66 e regressione totale. Nessun nuovo benchmark Python né modifica al solver di produzione/API/CLI. Gate M66 **NON ANCORA VALIDATO**; M68 pubblico resta bloccato. Nota: [`m66-nonlinear-validation-gate.md`](m66-nonlinear-validation-gate.md).

> **2026-10-23 — M65 implementato, in attesa dei test Windows .NET 8:** l'utente conferma M64 superato. È stato introdotto il prototipo **interno** `NonlinearTruss2DNewmark` per reticolari Truss2D elasto-plastiche: gradi di libertà fisici, matrice di massa consistente, vincoli inclinati/ridondanti, smorzamento fisso `C=alpha*M+beta*K0`, Newmark medio + Newton con tangente elastoplastica, commit globale degli stati solo dopo convergenza, reazioni comprendenti massa e viscosità, diagnostica energetica separata. I nuovi benchmark analitici, di convergenza, energia e rollback sono **solo C#/xUnit**; nessuno script Python è necessario. Il runner `scripts/run-m65-tests.ps1` esegue **per prima cosa** `clean-project-build.ps1`, poi build, test mirati e regressione completa. **M65 non è ancora validato**: attendere i test .NET 8 su Windows. README riscritto in italiano con badge Shields.io verificati, percorso M63–M75 e limiti espliciti. Prossimo M66, gate numerico indipendente. Vedere [`m65-nonlinear-truss-dynamics.md`](m65-nonlinear-truss-dynamics.md).

> **2026-10-23 — M64 implementato, in attesa dei test Windows:** l'utente conferma la suite M63 superata. È stato aggiunto un risolutore **interno** di reticolari Truss2D elasto-plastiche in statica incrementale (controllo di carico, assemblaggio delle tangenti e forze M63, Newton–Raphson, base compatibile con vincoli inclinati e ridondanti, commit globale solo dopo equilibrio, rollback su mancata convergenza, diagnostica meccanismi). Test xUnit con riferimenti analitici C#; **nessun nuovo script/benchmark Python da eseguire**. Script `scripts/run-m64-tests.ps1`: pulizia `bin`/`obj` come prima azione, build Release, test M64 e regressione totale. .NET non disponibile nell'ambiente di confezionamento: **M64 non ancora validato**; API pubbliche e solver lineare invariati. Nota: [`m64-incremental-elastoplastic-truss.md`](m64-incremental-elastoplastic-truss.md). M65 dinamica non lineare rimane pianificato.

> **2026-10-23 — M63 implementato, attende validazione Windows:** confermati dall'utente i test automatici M62 (`M62 automated checks PASSED`); il riferimento Python esterno opzionale è rimasto **SKIPPED** per mancanza di interprete utilizzabile. La prima tappa della roadmap futura, **M63 — singolo elemento Truss2D elasto-plastico**, è ora implementata come componente `internal` separato, con stato trial/commit, forze globali 4×1, tangente 4×4, energie e xUnit. Il test indipendente Python in ambiente di confezionamento ha prodotto PASS, ma compilazione e suite .NET 8 su Windows non sono state eseguite in questa fase. Eseguire `scripts/run-m63-tests.ps1` (prima azione: pulizia `bin`/`obj`). Riferimento: [`m63-elastoplastic-truss2d-element.md`](m63-elastoplastic-truss2d-element.md). **M64 e seguenti restano pianificati, non implementati.**


> **2026-10-23 — Stato aggiornato e nuova pianificazione (fonte corrente):** l’utente ha confermato `M62 automated checks PASSED` sul proprio Windows. Il benchmark Python esterno M62 è stato **SKIPPED**, non eseguito, a causa dell'alias Microsoft Store; i riferimenti numerici indipendenti incorporati nei test C# hanno superato la regressione. M62 resta un prototipo interno SDOF, non abilita la plasticità strutturale 2D. Il piano prospettico dettagliato **M63–M75** è in [`roadmap-forward-m63-m75.md`](roadmap-forward-m63-m75.md): M63 elemento Truss2D plastico isolato, M64 equilibrio statico incrementale, M65 dinamica 2D, M66 gate di benchmark, M67 post-processing, M68–M69 API/viewer, M70–M72 funzionalità opzionali, M73–M75 prestazioni/integrazione/consolidamento. **Nessun M63–M75 è ancora implementato**; stati storici riportati sotto sono documentazione d'archivio.

# StructuralSolver2D Roadmap

> **Stato corrente 23/10/2026:** l’utente ha confermato il superamento dei test M71 e M72-FIX2. **M73 — baseline prestazionale C# implementata, non ancora eseguita sul Windows dell’utente**. Comprende un progetto .NET 8 di misurazione, test xUnit e report CSV/JSON; nessun algoritmo FEM modificato. Prossimi M74 (adapter OpenCad2D) e M75 (consolidamento). Documento: [M73](m73-performance-baseline.md). Le note cronologiche sottostanti restano storiche.

StructuralSolver2D is developed as an independent .NET 8 structural analysis engine for two-dimensional linear structural models.

The project is intentionally developed in small, testable and documented milestones.
> **23/10/2026 — M72-FIX2 (solo warning test xUnit):** aggiornate sei asserzioni in quattro file dei test M65–M68 secondo gli analyzer xUnit2012, xUnit2031, xUnit2013. Nessun cambiamento al codice di produzione, ai benchmark numerici o agli script; resta aperta la verifica Windows dei test M71/M72. [Nota tecnica](m72-fix2-xunit-assertion-warnings.md).


The current goal is not to build a full professional structural design package, but to create a clean, validated and educational 2D structural analysis core.

**2026-10-23 M62 RESEARCH PROTOTYPE (latest):** User confirmed `run-m61-tests.ps1` passed on Windows. M62 adds a strictly internal one-degree-of-freedom elastoplastic spring oscillator: stateful isotropic-hardening return mapping, Newton–Newmark integration and atomic rollback on failed equilibrium, with explicit physical and discrete energy indicators. Independent closed-form branch reference in Python and xUnit benchmark/regression tests, no public API, JSON or CLI change. `scripts/run-m62-tests.ps1` preserves mandatory `bin`/`obj` cleanup first. No .NET 8 SDK in packaging environment: M62 is **pending Windows tests**. See `docs/structural/m62-sdof-elastoplastic-prototype.md`.

**2026-10-23 M61 FEASIBILITY + test preflight (historical; subsequently Windows-validated):** M60 .NET 8 tests PASSED per user report. M61 remains a **document-only feasibility milestone**: return mapping/tangent stiffness/Newton–Newmark state rollback and independent benchmark shortlist documented in `docs/structural/m61-nonlinear-feasibility.md`; isolated uniaxial Python return-map probe verified (not a production feature). All 12 PowerShell milestone test runners now invoke `scripts/clean-project-build.ps1` immediately after `$ErrorActionPreference`, deleting only the local `bin`/`obj` directories of discovered MSBuild projects. `run-m61-tests.ps1` adds cleanup safety tests and complete .NET regression. User subsequently confirmed the M61 Windows script passed; see `docs/structural/build-cleanup.md`.

**2026-10-23 M60 implementation (historical; Windows user tests subsequently passed):** User confirmed `run-m591-tests.ps1` PASSED on Windows for M59.1 audit FIX1. M60 exploratory non-normative seismic calculation is implemented with uniform prescribed GlobalX/GlobalY base acceleration, FEM-consistent effective inertial action `-M*r*a_g`, modal participation and effective mass, piecewise-linear period-based elastic pseudo-acceleration spectra and displacement/rotation SRSS envelopes. New opt-in API, CLI, Markdown and CSV, a loadless JSON example, focused numerical and regression tests, and `scripts/run-m60-tests.ps1`. The .NET build and tests for M60 have **not** been executed in the packaging environment; M60 remains pending user verification. Spectra are user supplied; no NTC/Eurocode, CQC, signed envelope, support differential motion, member forces or safety check. See `docs/structural/m60-seismic-exploratory.md`. Prior browser checks remain manual.

**2026-10-23 M59.1 audit FIX1 (historical; subsequently Windows-validated):** M59 automatic .NET 8 verification passed per user report; M59.1 is implemented but **not yet validated in .NET**. Independent SciPy/FEM comparison matches the prior M59 N8 displacements and finds medium/fine differences of roughly 0.98% Uy, 1.09% Vy and 13.24% Ay (so acceleration accuracy remains open). Expanded warnings for silent zero responses, sample peaks and an all-mode resolution target impossible under the 2000-step cap; no M59 integrator change. See `reports/m591-full-audit-2026-10-23.md`. Run `scripts/run-m591-tests.ps1` in a clean Windows directory before accepting M59.1. M60 remains planned.

**2026-10-23 M59 implementation update (historical, superseded in validation status by the note above):** The user reports that M58 `run-m58-tests.ps1` passed all automated checks, including the M57/FIX2 predecessors, CLI harmonic exports and full regressions. The viewer still requires visual browser inspection. M59 has been implemented with Newmark average-acceleration time integration, explicit load histories/initial conditions, energy diagnostics, API/CLI/report/CSV/standalone viewer, and numerical/CLI tests. **M59 has NOT been built or tested with .NET 8 in the packaging environment.** Run `scripts/run-m59-tests.ps1` in a freshly extracted folder before accepting. See `docs/structural/m59-time-history.md`.

**2026-10-23 M58 implementation update (historical):** M51–M56 (including M56-FIX1) test suites passed per user confirmation. The M56 failure was due to executing older files in an uncleared Windows project folder; after extracting the fixed release into a clean folder, all tests passed. M57 public modal features are implemented; M57-FIX1 corrected report normalization and M57-FIX2 corrects the static regression fixture enumeration. M58 now implements an opt-in linear harmonic-response API, damping models, CLI, CSV/Markdown and HTML plots; **M57-FIX2 has not yet been rerun and neither M57 nor M58 is accepted**. The clean Windows .NET 8 suite and browser acceptance remain pending. See [Carpinteri-based verification and structural dynamics workstream](#engineering-roadmap-extension--carpinteri-based-verification-and-structural-dynamics-2026-10-22) (Milestones 51–61; **M51–M56 user-validated; M57–M58 implemented awaiting tests; M59+ planned**). Milestones 49–50 retain their existing separate viewer/integration path.

---

## Development principle

Every important feature must be supported by:

- a clear model representation;
- deterministic tests;
- validation benchmarks;
- documentation;
- CLI usage where appropriate;
- reporting support where appropriate.

The guiding rule is:

> no significant new feature should be added without dedicated validation benchmarks.

This is especially important because the project deals with structural analysis results, where silent numerical or sign-convention errors can be misleading.

---

## Current status

The project currently includes:

- independent .NET 8 solution;
- structural model core;
- nodes, members, materials, sections and supports;
- load cases;
- manual load combinations;
- nodal forces and moments;
- uniform distributed loads;
- linear distributed loads;
- point loads on members;
- Frame2D analysis;
- Truss2D analysis;
- mixed Frame2D + Truss2D analysis;
- moment releases at Frame2D member ends;
- internal-force sampling;
- deformed-shape sampling;
- result summaries and extrema;
- characteristic internal-force points;
- CLI commands;
- JSON input examples;
- Markdown reports with educational guidance;
- CSV export for spreadsheet validation and external post-processing;
- benchmark catalog;
- automated benchmark runner;
- diagnostic analysis errors;
- README and AI handoff documentation;
- preliminary SLE deflection checks;
- parametric section helpers;
- initial elastic material library;
- public API facade;
- first technical release baseline;
- UI-independent viewer-ready result data;
- optional cyclic deformed-shape animation frames;
- support glyphs, scaled support reactions and static result annotations in SVG/HTML exports;
- first standalone interactive HTML viewer with pan, zoom and layer toggles.

---

## Completed milestones

### Milestone 1 — Independent .NET 8 solution

Created the initial independent .NET 8 solution.

The solver is not coupled to OpenCad2D, Avalonia, WPF or any graphical user interface.

---

### Milestone 2 — Structural core model

Added the first pure structural data model:

- `StructuralModel`;
- `StructuralNode`;
- `StructuralMember`;
- `StructuralMaterial`;
- `StructuralSection`;
- `StructuralSupport`.

The model is independent from the solver and from any UI.

---

### Milestone 3 — Loads and load cases

Added:

- `StructuralLoadCase`;
- `StructuralLoad`;
- nodal forces;
- nodal moments;
- member loads;
- load validation.

---

### Milestone 4 — Minimal Frame2D solver

Added the first linear elastic Frame2D solver.

Supported:

- 2D frame elements;
- global stiffness assembly;
- boundary conditions;
- nodal displacements;
- support reactions;
- local member end forces.

---

### Milestone 5 — Extended Frame2D validation

Added additional analytical tests for:

- simply supported beams;
- cantilevers;
- axial behavior;
- unloaded models;
- symmetric portal frames;
- unstable models.

---

### Milestone 6 — Internal-force sampling

Added sampling of:

- axial force `N(x)`;
- shear force `V(x)`;
- bending moment `M(x)`.

This enables future diagrams, reporting and graphical visualization.

---

### Milestone 7 — Result summaries and extrema

Added result summaries and maximum/minimum extraction for internal-force diagrams.

This makes the results easier to consume from CLI, reports and future UI layers.

---

### Milestone 8 — Minimal CLI examples

Added a minimal command line interface with predefined examples.

Example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- example simple-supported-beam
```

---

### Milestone 9 — JSON input examples

Added JSON model loading and the `analyze` command.

Example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\simple-supported-beam.json
```

---

### Milestone 10 — Markdown reports

Added a Markdown report generator.

Example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\simple-supported-beam.json reports\simple-supported-beam.md
```

---

### Milestone 11 — README and AI handoff

Added project-level documentation and `ai-handoff.md`.

The AI handoff file helps future LLM sessions understand:

- project architecture;
- current status;
- conventions;
- solver limits;
- next recommended steps.

---

### Milestone 12 — Reporting and CLI test coverage

Added tests for:

- Markdown report generation;
- JSON input reading;
- CLI-related input behavior;
- report options.

---

### Milestone 13 — Point loads on members

Implemented point loads applied along Frame2D members.

Supported:

- normalized position along the member;
- global directions;
- local directions;
- equivalent nodal loads;
- internal-force sampling with shear jumps.

---

### Milestone 14 — Linear distributed loads

Implemented linearly varying distributed loads.

This supports:

- triangular loads;
- trapezoidal loads;
- generic linear member loads.

---

### Milestone 15 — Manual load combinations

Added manual load combinations.

Example:

```json
{
  "id": "ULS1",
  "name": "ULS 1",
  "terms": [
    { "loadCaseId": "G1", "factor": 1.35 },
    { "loadCaseId": "Q1", "factor": 1.50 }
  ]
}
```

The solver does not generate normative combinations automatically.

Only user-defined combinations are supported.

---

### Milestone 16 — Truss2D solver

Added a Truss2D analyzer for axial-only members.

Supported:

- 2D truss members;
- nodal forces;
- support reactions;
- nodal displacements;
- axial member force.

---

### Milestone 17 — Benchmark catalog

Added a structured benchmark catalog.

Initial benchmark groups:

- beams;
- trusses;
- frames;
- expected results.

The benchmark catalog is intended to become the main validation base of the project.

---

### Milestone 18 — Automated benchmark runner

Added an automated benchmark runner.

The runner reads benchmark models and expected results, runs the solver and compares computed values against the expected values.

This turns benchmark files into regression tests.

---

### Milestone 19 — Frame2D displacement sampler

Added displacement and deformed-shape sampling along Frame2D members.

Sampled values include:

- local axial displacement;
- local transverse displacement;
- local rotation;
- global X displacement;
- global Y displacement.

Important note:

> the displacement sampler currently performs FEM interpolation of nodal displacements. It is useful for drawing the deformed shape, but it is not always equal to the exact closed-form internal displacement field for distributed loads.

---

### Milestone 20 — Deformed-shape samples in Markdown reports

Added deformed-shape samples to the Markdown report.

This prepares the project for:

- better educational reports;
- graphical output;
- future UI integration.

---

### Milestone 21 — Analysis diagnostics

Improved diagnostic errors for unsupported or invalid analysis cases.

Examples:

- wrong analyzer for member type;
- unsupported loads in Truss2D;
- unstable model;
- singular stiffness matrix;
- invalid model issues.

---

### Milestone 22 — Frame2D moment releases

Added moment releases at Frame2D member ends.

Supported:

- start moment release;
- end moment release;
- both-end moment release.

This allows modelling hinged member ends and internal hinges.

---

### Milestone 23 — Mixed Frame2D + Truss2D analyzer

Added `PlaneStructureAnalyzer`.

It supports mixed 2D structural models containing both:

- `Frame2D` members;
- `Truss2D` members.

The global model uses three degrees of freedom per node:

- `Ux`;
- `Uy`;
- `Rz`.

Truss members contribute only to translational degrees of freedom.

---



### Milestone 24 — Local/global load conventions and inclined member validation

Added tests and benchmark files for inclined members and local/global load conventions.

Covered topics:

- inclined Frame2D cantilever with global nodal load;
- inclined Frame2D cantilever with `LocalY` uniform load;
- inclined Frame2D cantilever with `GlobalY` uniform load;
- reversed member orientation `A -> B` and `B -> A`;
- axial load along an inclined member;
- mixed Frame2D + Truss2D model with inclined brace.

This milestone reinforces the convention that global loads stay global, while local loads rotate with the member axes.

### Milestone 25 — Global equilibrium checker

Added an automatic global equilibrium checker.

The checker verifies, for a completed analysis result:

```text
ΣFx ≈ 0
ΣFy ≈ 0
ΣMz ≈ 0
```

Implemented class:

```text
GlobalEquilibriumChecker
```

The checker computes:

- total applied horizontal force;
- total applied vertical force;
- total applied moment about a reference point;
- total support horizontal reaction;
- total support vertical reaction;
- total support moment reaction;
- force and moment residuals.

Supported load sources:

- nodal forces;
- nodal moments;
- point loads on members;
- uniform distributed loads;
- linearly varying distributed loads;
- manual load combinations.

The benchmark runner now executes this global equilibrium check for every catalog benchmark.

This milestone is important because it can detect:

- missing loads;
- wrong load signs;
- wrong reaction signs;
- local/global transformation errors;
- load combination errors;
- member load conversion errors.

---

# Upcoming milestones

## Milestone 26 — Mesh refinement and convergence benchmarks

### Goal

Introduce convergence benchmarks.

The same problem should be modeled with increasing discretization density:

```text
1 element
2 elements
4 elements
8 elements
```

### Candidate cases

- simply supported beam with uniform distributed load;
- cantilever with uniform distributed load;
- beam with triangular load;
- beam with point load not coincident with a node.

### Checks

The benchmark should evaluate convergence of:

- deflection;
- maximum bending moment;
- shear force;
- deformed shape;
- internal sampled values.

### Educational purpose

This milestone should document a key FEM concept:

> a finite element model is a discretized approximation of a continuous structure.

---

## Milestone 27 — Professional and institutional benchmark catalog

### Goal

Create a benchmark section inspired by recognized verification sources.

Possible sources:

- NAFEMS;
- SOFiSTiK verification examples;
- Autodesk Robot / Nastran verification manuals;
- Dlubal verification examples;
- OpenSees examples;
- university lecture examples.

### Proposed structure

```text
benchmarks/
  professional/
    README.md
    nafems-inspired/
    opensees-inspired/
    software-verification/
```

### Rules

For every professional/institutional benchmark, document:

- source;
- description;
- adaptation to StructuralSolver2D;
- assumptions;
- limitations;
- expected results;
- tolerances;
- what the benchmark validates.

Do not copy long copyrighted text from external manuals.

Use only minimal data, independent explanations and proper references.

---

## Milestone 28 — Improved benchmark runner

### Goal

Make the benchmark runner more expressive and maintainable.

### Planned improvements

Support expected checks for:

- reactions;
- nodal displacements;
- nodal rotations;
- support moments;
- internal-force values at specific positions;
- global maximum/minimum values;
- axial forces in truss members;
- equilibrium residuals;
- symmetry conditions;
- mesh convergence criteria.

### Why this matters

The benchmark runner should become the backbone of the validation strategy.

As the solver grows, benchmarks should be easier to add without duplicating test code.

---

## Milestone 29 — Examples and benchmarks reorganization

### Goal

Separate user examples from validation benchmarks.

### Proposed structure

```text
examples/
  beams/
  frames/
  trusses/
  mixed/
  combinations/

benchmarks/
  beams/
  frames/
  trusses/
  mixed/
  convergence/
  professional/
  expected/
```

### Difference

Examples are for users.

Benchmarks are for validation.

Examples should be easy to understand.

Benchmarks should be precise, documented and regression-tested.

---

## Milestone 30 — Initial theory documentation

### Goal

Add educational theory documentation.

### Proposed files

```text
docs/theory/
  matrix-method.md
  frame2d-element.md
  truss2d-element.md
  equivalent-nodal-loads.md
  local-global-coordinates.md
  sign-conventions.md
  displacement-interpolation.md
  validation-strategy.md
```

### Why this matters

StructuralSolver2D can be both:

- a lightweight 2D structural analysis engine;
- an educational codebase for learning matrix-based structural analysis.

The theory documentation should explain how and why the solver works.

---

## Milestone 31 — Improved internal-force diagrams and characteristic points

### Goal

Improve the post-processing of internal-force diagrams.

### Completed features

The post-processing pipeline detects characteristic points such as:

- maximum bending moment;
- minimum bending moment;
- maximum shear;
- zero shear points;
- zero moment points;
- concentrated load positions;
- shear discontinuities;
- diagram segment limits.

### Why this matters

This is necessary for:

- better reports;
- future graphical output;
- preliminary checks;
- future integration with OpenCad2D.

### Status

Completed. The implementation is based on sampled diagrams. It is suitable for reporting and educational inspection; exact analytical locations may still require dedicated closed-form post-processing for specific load configurations.

---

## Milestone 32 — Preliminary SLE deflection checks

### Goal

Add preliminary serviceability checks for deflection.

### Implemented scope

Milestone 32 adds a deliberately limited serviceability helper in the analysis project:

```text
StructuralSolver2D.Analysis.Serviceability.PreliminaryDeflectionChecker
```

It checks sampled member displacement diagrams against a simple limit:

```text
maximum sampled deflection <= L / limit
```

Common limits remain:

```text
L/200
L/250
L/300
L/400
```

The implementation reports:

- member id;
- checked direction;
- reference length;
- allowable deflection;
- maximum sampled absolute deflection;
- signed critical deflection;
- critical sample position;
- pass/fail status;
- utilization ratio.

### Important limitation

This must not be presented as a complete normative check.

The correct wording is:

```text
preliminary serviceability check
```

or:

```text
preliminary deflection check
```

not:

```text
full code-compliant design verification
```

The check is based on the existing FEM displacement/deformed-shape samples. For exact benchmark comparison at a specific critical point, that point should still be modeled as an explicit node.

### Status

Completed.

---

## Milestone 33 — Parametric sections

### Goal

Generate section properties from simple geometric inputs while preserving the existing explicit `StructuralSection` API.

### Implemented scope

Milestone 33 adds:

```text
StructuralSolver2D.Core.Model.Sections.StructuralSectionFactory
```

Implemented helpers:

- rectangular section;
- simple timber rectangular section;
- circular solid section;
- circular hollow section.

Example:

```csharp
var section = StructuralSectionFactory.Rectangular(
    "RECT_100x200",
    width: 0.10,
    height: 0.20);
```

The generated section computes:

- area in m²;
- second moment of area in m⁴;
- optional height and width metadata where meaningful.

### Status

Completed.

---

## Milestone 34 — Initial material library

### Goal

Add predefined elastic materials.

Possible materials:

- steel S235;
- steel S275;
- steel S355;
- timber C24;
- glulam GL24h;
- generic concrete.

### Limitation

The material library should initially support only elastic analysis data.

It should not imply full normative design verification.

---

## Milestone 35 — Advanced educational Markdown reports

Completed.

Milestone 35 makes Markdown reports more useful for study, debugging and validation without changing solver behavior.

Added:

- an optional `How to read this report` educational section;
- an executive summary with model size and governing absolute values;
- optional preliminary deflection-check reporting when `DeflectionCheckResult` values are supplied;
- report options to hide educational explanations, model statistics or deflection checks;
- tests for the new report sections and options.

The report remains a textual Markdown output. It is still intentionally separate from HTML/PDF rendering and from any graphical UI.

---

## Milestone 36 — CSV export

Completed.

Milestone 36 adds spreadsheet-friendly CSV export without changing solver behavior.

Added:

- `CsvStructuralResultExporter`;
- CSV export for nodal displacements;
- CSV export for support reactions;
- CSV export for local member end forces;
- CSV export for sampled internal-force diagrams;
- CSV export for sampled displacement/deformed-shape diagrams;
- CSV export for compact governing result summaries;
- CLI command `export-csv`;
- documentation in `docs/structural/csv-export.md`;
- tests for CSV headers, values, invariant numeric formatting and escaping.

Example command:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\beams\simple-supported-beam.json reports\csv\simple-supported-beam
```

CSV export is intended for spreadsheet validation and external post-processing. It is not a complete model exchange format.

---

## Milestone 37 — Public API stabilization

### Goal

Introduce a stable high-level public API before a first technical release.

### Completed

Milestone 37 adds `StructuralSolver2DService` in `StructuralSolver2D.Analysis.PublicApi`.

The facade keeps low-level components available, but gives external applications one preferred entry point for the standard workflow:

- analyze one load case;
- analyze one manual load combination;
- sample internal-force diagrams;
- optionally sample displacement diagrams;
- optionally perform preliminary deflection checks;
- compute compact governing result summaries.

New public API files:

```text
src/StructuralSolver2D.Analysis/PublicApi/StructuralSolver2DService.cs
src/StructuralSolver2D.Analysis/PublicApi/StructuralAnalysisRequest.cs
src/StructuralSolver2D.Analysis/PublicApi/StructuralAnalysisOptions.cs
src/StructuralSolver2D.Analysis/PublicApi/StructuralAnalysisOutput.cs
src/StructuralSolver2D.Analysis/PublicApi/StructuralAnalysisTargetKind.cs
```

The API is intentionally conservative: it wraps the current first-order linear elastic plane-structure analyzer and post-processing pipeline. It does not freeze future internals, does not introduce normative design checks and does not replace specialized low-level solver classes for advanced usage.

---

## Milestone 38 — First technical release

### Goal

Prepare the first GitHub technical release.

Version:

```text
v0.1.0
```

Suggested title:

```text
StructuralSolver2D v0.1.0 - First technical release
```

Release contents:

- README;
- `VERSION`;
- `CHANGELOG.md`;
- `ai-handoff.md`;
- documentation;
- CLI;
- JSON examples;
- Markdown report generation;
- CSV export;
- public API facade;
- benchmark catalog;
- automated tests;
- release notes;
- license.

### Status

Completed.

---

## Milestone 39 — Viewer-ready result data model

### Goal

Create a renderer-independent data model for future graphical result viewers.

### Status

Completed.

Implemented under:

```text
src/StructuralSolver2D.Reporting/Visualization
```

The visualization layer prepares:

- undeformed nodes and members;
- scaled deformed nodal coordinates;
- nodal `Ux`, `Uy` and `Rz`;
- deformed member polylines;
- normal-force, shear-force and bending-moment diagram polylines;
- drawing bounds;
- optional cyclic animation frames.

This milestone intentionally does not add a GUI dependency. It creates the stable data boundary for SVG, PNG, HTML, Avalonia, WPF or OpenCad2D clients.

---

## Upcoming milestones after Milestone 39

The post-M39 roadmap keeps both work streams:

- structural/model/reporting features planned before the viewer discussion;
- graphical viewer features enabled by the Milestone 39 visualization data model.

The unified sequence is:

```text
40 - Parametric model generators
41 - Validation example files in JSON
42 - Static graphical result export: SVG and HTML
43 - XLSX report export
44 - PDF technical report
45 - Section catalog persistence
46 - Advanced static structural annotations and oriented supports
47 - First simple interactive viewer prototype
48 - Viewer controls for result scale, diagram scale and animation playback
49 - Labels/tooltips for nodal displacements, rotations and diagram values
50 - OpenCad2D integration boundary study
```


### Milestone 43 — XLSX report export

Added a lightweight `.xlsx` workbook exporter based on direct OpenXML package generation.

The workbook contains sheets for summary values, nodal displacements, support reactions, member end forces, internal-force samples and displacement samples.

PNG export remains intentionally deferred; the graphical export path is vector-first through SVG/HTML.

### Milestone 44 — PDF technical report

Added a lightweight PDF technical report exporter.

The report contains model overview, assumptions, executive summary, nodal displacements, support reactions, member end forces and sampled result tables.

The first implementation writes a minimal PDF directly, without external PDF-generation dependencies.

### Milestone 45 — Section catalog persistence

Added reusable section catalogs with JSON persistence.

The catalog stores normal `StructuralSection` records, validates ids and section properties, and can apply reusable sections to a `StructuralModel`.

The next milestone is:

```text
46 - First simple interactive viewer prototype
```

Detailed planning is kept in:

```text
docs/structural/development-plan.md
```

---

# Short-term priority

Completed short-term milestones:

```text
31 - Improved diagrams and characteristic points
32 - Preliminary SLE deflection checks
33 - Parametric sections
34 - Initial material library
35 - Advanced educational reports
36 - CSV export
37 - Public API stabilization
38 - First technical release
39 - Viewer-ready result data model and animation frames
```

The roadmap has been realigned after Milestone 39 to preserve both planned work streams:

1. engineering/product features already planned before the viewer discussion;
2. graphical viewer features made possible by the new viewer-ready data model.

The next recommended step is:

```text
40 - Parametric model generators
```

See also:

```text
docs/structural/development-plan.md
```

---

## Milestone 26 completion update

Milestone 26 adds the first mesh-refinement and convergence benchmark layer.

Added files:

```text
benchmarks/convergence/
  C01-simple-supported-udl-1-elements.json
  C01-simple-supported-udl-2-elements.json
  C01-simple-supported-udl-4-elements.json
  C01-simple-supported-udl-8-elements.json
  C02-point-load-single-element.json
  C02-point-load-explicit-node.json

docs/structural/mesh-refinement.md

tests/StructuralSolver2D.Analysis.Tests/Benchmarks/MeshRefinementConvergenceTests.cs
```

The milestone validates and documents the distinction between:

- nodal finite element results;
- internal displacement interpolation;
- closed-form beam theory values;
- convergence behavior under mesh refinement.

This milestone reinforces the project rule that validation must evolve together with solver capabilities.

---

## Milestone 28 completion update

Milestone 28 improves the automated benchmark runner without changing the solver.

Added/refactored test-side files:

```text
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkCatalog.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkRepository.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkAnalysisRunner.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkResultAssertions.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkCatalogTests.cs
```

The benchmark runner now has a clearer structure:

- catalog loading and schema validation;
- repository path resolution;
- analyzer selection;
- result assertions;
- catalog orchestration.

The catalog validation checks:

- duplicate benchmark ids;
- missing names;
- missing model paths;
- missing model files;
- missing analysis ids;
- invalid tolerances.

The supported automatic result checks currently include:

- support reactions;
- nodal displacements and rotations;
- maximum absolute shear;
- maximum absolute bending moment;
- Truss2D member axial forces;
- named stability and symmetry checks;
- global equilibrium residuals.

This milestone makes the benchmark suite easier to extend before adding more validation cases and future expected-result types.

---

## Milestone 29 completion update

Milestone 29 reorganizes user-facing examples and documents the difference between examples and benchmarks.

Added preferred example folders:

```text
examples/
  README.md
  beams/
  trusses/
  mixed/
  combinations/
```

The new rule is:

```text
examples/   are for learning and CLI usage
benchmarks/ are for validation and regression testing
```

The CLI does not require code changes for this organization because it can analyze any JSON input path.

Existing flat example files may remain temporarily for compatibility, but new examples should use the categorized layout.

Documentation added:

```text
docs/structural/examples-and-benchmarks.md
examples/README.md
```

This milestone prepares the project for a larger benchmark and example catalog without mixing educational examples with validation cases.

---

## Milestone 30 completion update

Milestone 30 adds the initial theory documentation layer.

Added files:

```text
docs/theory/README.md
docs/theory/matrix-method.md
docs/theory/frame2d-element.md
docs/theory/truss2d-element.md
docs/theory/equivalent-nodal-loads.md
docs/theory/local-global-coordinates.md
docs/theory/sign-conventions.md
docs/theory/displacement-interpolation.md
docs/theory/validation-strategy.md
```

This milestone reinforces the educational purpose of StructuralSolver2D.

The new theory documentation explains:

- the displacement/matrix stiffness method;
- the current Frame2D element assumptions;
- the current Truss2D element assumptions;
- equivalent nodal loads;
- local/global coordinate transformations;
- sign conventions;
- displacement interpolation and deformed-shape sampling;
- benchmark and validation strategy.

Milestone 37 introduced a stable public facade that bundles analysis, post-processing and optional preliminary deflection checks for external applications.

---

## Milestone 38 completion update

Milestone 38 prepares the first technical release baseline.

Added release-oriented files:

```text
Directory.Build.props
VERSION
CHANGELOG.md
docs/structural/release-checklist.md
docs/structural/release-notes-v0.1.0.md
```

The release version is:

```text
v0.1.0
```

This milestone does not add new solver behavior. It consolidates the public-facing project state after Milestones 1-37 and documents how to validate, tag and publish the first technical release.

---

## Validation hardening and viewer preparation

Completed after the first technical release preparation:

- validation examples for rigid-joint frames, trusses, Nielsen-type schemes and Gerber beams;
- UI-independent viewer data model for deformed shapes, nodal displacement/rotation display and N/V/M diagram polylines;
- optional cyclic animation-frame data for the deformed shape.

### Milestone 39 — Viewer-ready result data model

Milestone 39 introduces a renderer-independent visualization layer under:

```text
src/StructuralSolver2D.Reporting/Visualization
```

The layer prepares:

- undeformed nodes and members;
- scaled deformed nodal positions;
- deformed member polylines;
- nodal `Ux`, `Uy` and `Rz` values for labels or tooltips;
- normal-force, shear-force and bending-moment diagram polylines;
- drawing bounds;
- optional cyclic deformed-shape animation frames;
- support glyphs, scaled support reactions and static result annotations in SVG/HTML exports;
- first standalone interactive HTML viewer with pan, zoom and layer toggles.

This is intentionally not a GUI. It is the stable data boundary for future SVG/PNG export, Avalonia/WPF viewers, web canvases or OpenCad2D integration.

---

## Milestone 40 completion update

Milestone 40 adds parametric model generators under:

```text
src/StructuralSolver2D.Core/Model/Generators
```

The first generator set covers:

- simply supported beams;
- Gerber beams with asymmetric loads;
- rigid-joint portal frames;
- Pratt-like bridge trusses;
- isostatic triangular trusses;
- Nielsen/parabolic trusses;
- inverted parabolic trusses;
- hyperstatic double-diagonal trusses.

The generators return normal `StructuralModel` instances and do not depend on analysis, reporting, CLI or UI layers.

---

## Milestone 41 completion update

Milestone 41 adds user-facing validation JSON files under:

```text
examples/validation/
```

The initial catalog contains:

- `rigid-joint-portal-frame.json`;
- `small-bridge-truss.json`;
- `isostatic-triangular-truss-beam.json`;
- `nielsen-parabolic-truss.json`;
- `inverted-parabolic-truss.json`;
- `double-diagonal-hyperstatic-truss.json`;
- `gerber-beam-asymmetric-loads.json`.

Automated tests load every JSON file, validate the structural model and analyze it through the public API.

---

## Unified upcoming milestones after Milestone 41

```text
42 - Static graphical result export: SVG and HTML
43 - XLSX report export
44 - PDF technical report
45 - Section catalog persistence
46 - First simple interactive viewer prototype
47 - Viewer controls for result scale, diagram scale and animation playback
48 - Labels/tooltips for nodal displacements, rotations and diagram values
49 - OpenCad2D integration boundary study
```

The previous viewer milestones are therefore not discarded. They are moved after the generator/export/reporting work so the project remains validated and useful before the interactive UI grows.


### Milestone 46 — Advanced static structural annotations

Added:

- support glyphs in exported SVG/HTML previews;
- scaled reaction arrows and reaction moment glyphs;
- member-length dimensions on the undeformed model;
- maximum displacement callout on the deformed shape;
- maximum-value labels on `N`, `V` and `M` diagrams;
- visualization data structures for supports, reactions and annotations.

These additions are primarily in the reporting/visualization layer. A later refinement also made oriented translational support restraints mechanically active in the analysis layer.


### Support orientation refinement

Added mechanically active support orientation for translational restraints. `StructuralSupport.OrientationDegrees` is now used by the analysis layer to enforce restrained local support directions through homogeneous constraint equations. SVG/HTML exports continue to use the same angle for support glyph rendering.


### Milestone 47 — First simple interactive viewer prototype

Added a standalone HTML viewer export based on `StructuralVisualizationModel`.

Implemented:

- CLI command `export-viewer`;
- mouse wheel zoom;
- pointer drag pan;
- reset view;
- layer toggles for undeformed model, deformed shape, diagrams, supports, reactions and dimensions;
- summary values displayed beside the scene.

The viewer is generated as a static HTML file and does not introduce WPF, Avalonia or browser framework dependencies.


### Milestone 48 — Viewer controls for scales and animation playback

Added standalone HTML viewer controls for:

- corrected deformed-shape scale based on base points plus displacement vectors;
- separate `N`, `V` and `M` visibility toggles;
- play/pause deformation animation;
- animation amplitude and speed.

The controls are visualization-only and do not modify solver results.


### Viewer/export refinement — Applied load visualization

Added graphical representation of applied loads in SVG/HTML and the interactive viewer:

- nodal forces;
- nodal moments;
- member point loads;
- uniform distributed member loads;
- linearly varying distributed member loads.

The loads are rendered in a dedicated `#loads` layer so they can be toggled independently in the viewer.
---

# Engineering roadmap extension — Carpinteri-based verification and structural dynamics (2026-10-22)

**Status: PLANNED — documentation only.** This section adds a new engineering workstream; it does **not** claim that modal, harmonic, transient, or seismic capabilities are implemented. The existing first-order, linear-elastic **static** solver remains the only supported computational engine until each respective milestone passes its tests.

## Why this workstream exists

The reference is Alberto Carpinteri, *Dinamica delle strutture*, Società Editrice Esculapio, ISBN 978-88-9385-390-3 (2023 edition). Its original lecture material and preface predate the current Italian seismic code; use the book for mechanics and numerical verification, **not** as an authoritative source of current regulatory requirements.

Useful source sections (printed page numbers, not PDF viewer page numbers):

| Book reference | Relevance to this project |
| --- | --- |
| Ch. 1, §§1.2–1.6 (pp. 3–23) | Free/forced SDOF motion, damping, harmonic, periodic, impulse and general forcing |
| Ch. 1, §§1.9–1.11 (pp. 30–37) | MDOF eigenproblems, Rayleigh quotient and Stodola–Vianello estimates |
| Ch. 2, §§2.2–2.4, 2.7 (pp. 40–64) | Analytical bending-beam eigenfrequencies and Rayleigh–Ritz benchmarks |
| Ch. 3, §§3.3–3.8 (pp. 68–85) | Energy principles, virtual work, boundary conditions, discretized dynamic equations |
| Ch. 4, §§4.4–4.6, 4.9–4.11 (pp. 110–141) | Stiffness assembly, planar trusses/frames, frame dynamics and examples |
| Ch. 5, §§5.1–5.6 (pp. 143–166) | Modal damping, response spectra, mode combination, shear-type example |
| Ch. 6, §§6.1–6.10 (pp. 169–206) | Nonlinear dynamic background; future research only |

**Source separation:** principles and analytical examples listed above come from the book. The proposed software architecture, proposed APIs, numerical methods, testing gates, milestones and work priorities below are project engineering decisions, **not** a claim that the book describes this repository or prescribes these implementations. Do not bundle the copyrighted PDF into the project ZIP; the bibliographic reference and independently prepared notes are sufficient.

## Relationship to earlier milestones and scope

- Milestones **1–48** retain their prior meanings/history; the new workstream does not renumber them.
- Milestone **49** (inspection labels/tooltips) and **50** (OpenCad2D integration boundary study) remain independently trackable on the existing viewer/integration path. Some viewer labels exist already, but their presence alone does not mark the complete M49 acceptance scope as closed.
- The new workstream starts at **Milestone 51**. M51 can be prioritized immediately without waiting for M49–50. M57 benefits from the standalone viewer but does not require OpenCad2D integration.
- **No implied certification:** static checks remain preliminary; dynamic milestones will be educational/experimental until independently verified. Seismic analysis must not be presented as an NTC/Eurocode-compliant design check simply because a spectrum has been evaluated.
- Maintain `.NET 8`, `Core` → `Analysis` → `Reporting`/`Cli`, plus the existing UI-independent rendering boundary. Do not bring UI framework classes into `Core` or `Analysis`.

## Delivery sequence and gates

| Wave | Milestone(s) | Intended outcome | Release gate |
| --- | --- | --- | --- |
| A — static reliability | **51** | Independent energetic/matrix verification of existing solver | All current static regression tests remain valid; new cross-checks pass |
| B — dynamic foundations | **52–54** | Mass definitions, element mass matrices, dynamically valid constraint handling | Units and assembly validated; constrained matrices physically meaningful |
| C — modal minimum viable feature | **55–57** | Eigenmodes, reference benchmarks and nonphysical-amplitude-aware modal viewer | Analytical + convergence comparisons; API/CLI/report/viewer regression coverage |
| D — forced dynamic response | **58–59** | Harmonic and time-history response for linear elastic structures | SDOF analytic comparisons; time-step and energy checks |
| E — earthquake/research | **60–61** | Separate non-normative seismic capability and nonlinear feasibility assessment | Modal participation validation; explicit out-of-scope boundaries |

**Cross-cutting rule:** do not treat a milestone as complete because an example looks plausible. Document the reference answer, assumptions, tolerances (absolute *and* relative, with meaningful zero handling), independently checked units, known limitations and failing-case diagnostics. Each newly shipped feature needs deterministic automated tests and at least one independently derived or otherwise credible benchmark.

## Milestone 51 — Static FEM invariants and energy-based cross-checks

**Priority:** P0. **Status:** completed on user-reported passing tests (no test log supplied). **Depends on:** existing static analyzers and regression catalog only.

**Goal:** improve confidence in the static results without changing the supported structural mechanics.

**Work items**

1. Inventory the paths through `Frame2DElementMatrices`, `Truss2DElementMatrices`, `PlaneStructureAnalyzer`, `SupportConstraintSystem` and global load assembly; document precisely where element stiffness, released-end condensation and *equivalent nodal member loads* are applied.
2. For each element, verify numerical symmetry `K_e ≈ K_e^T`, valid finite entries and the local/global transformation property `u_g^T K_g u_g ≈ u_l^T K_l u_l` for arbitrarily inclined elements. Check translational and rotational rigid-body motions of a **free** element against its expected nullspace, not against a fully restrained model.
3. Add the energy balance for an admissible zero-displacement-boundary-condition static solution: `U = 0.5 u^T K u`, `W = u^T F_equiv`, and `|W - 2U| ≈ 0`. Use the **assembled equivalent nodal loads** (including member distributed/point-load contributions); do not compare against an incomplete nodal-load-only vector. For future prescribed nonzero displacements, derive the additional work/reaction terms before applying this formula.
4. Verify the free-equation residual `r = K u - F_equiv` projected onto admissible free directions, and check that constrained reactions account for the residual in the restrained directions. Distinguish assembly residual, support residual and whole-structure equilibrium.
5. Extend diagnostics for near-mechanisms, unconstrained rigid-body modes, poorly conditioned stiffness and invalid/nonfinite input, with tests on inclined supports and redundant constraint directions. Avoid a blanket rejection of legitimately redundant constraints already supported by the current solver.
6. Add specific cross-checks for member end releases, mixed frames/trusses, reverse element orientation, zero load, combined load cases and equivalent nodal member actions.

**Suggested locations:** `tests/StructuralSolver2D.Analysis.Tests/Verification/`, `docs/theory/`, `benchmarks/verification/`; factor reusable verification utilities without altering public results or sign conventions.

**Acceptance criteria**

- All old benchmarks and viewer/report tests remain unchanged and green.
- Numerical tolerances are scale-aware and have documented near-zero behavior; no exact floating-point equality.
- Analytic beam/truss/frame cases satisfy equilibrium **and** energy checks; controlled faulty matrices/loads cause diagnostic failures.
- The new checks remain optional or non-breaking for existing public consumers until independently validated.

**Exit artifact:** verified static baseline, test catalog, written explanations of energetic invariants.

### M51 implementation checkpoint — 2026-10-22

**Status: IN PROGRESS (first code/test slice, tests not executed in this environment).**
Added internal `StaticSystemVerifier`, actual mixed-assembly audit entrypoints (load case and combination), extraction of the same homogeneous constraint rows, element invariant tests, beam/truss/frame energy tests, and `docs/theory/static-energy-verification.md`.
No public API, static output or dynamics semantics changed. The .NET 8 SDK was unavailable in the authoring environment; **do not mark M51 complete** pending `dotnet test StructuralSolver2D.sln`, multi-scale stress tests, independent analyzer path cross-checks, and near-mechanism diagnostics in the acceptance criteria above.

### M51 diagnostic checkpoint — 2026-10-22 (second implementation slice)

**Status: IN PROGRESS (not accepted: build/tests still pending).** Added non-public pre-solve `ConstrainedStiffnessInspector` with an independent constrained-subspace projection (including oriented restraints), diagonal-equilibrated symmetric spectrum for <=60 free DOFs, descriptive outcome types and a candidate critical DOF. The new internal `PlaneStructureAnalyzer.InspectStaticStability` uses the actual model assembly without attempting to solve a singular system. Added focused analytic and real-model regressions. No production solver thresholds or public static outputs changed.

**Gate remains:** compile/test on .NET 8, verify entire benchmark/report/viewer suite, investigate false-positive sensitivity across varying geometry and material/section scales, and retain comparisons between Frame2D/Truss2D and the mixed analyzer. This numerical heuristic is not a safety/stability design verification and does **not** complete M51.

### M51 acceptance update — 2026-10-22

**User reports that the complete M51 tests passed** using the prior full ZIP.
On that basis the project proceeds to M52; detailed command logs and the precise
.NET environment were not supplied in this conversation. The diagnostic remains
advisory and does not certify structural design safety.

## Milestone 52 — Dynamic data contract and coherent mass units

**Priority:** P0. **Status:** complete on user-reported .NET acceptance. **Depends on:** M51 review.

**Goal:** represent mass, initial conditions and future dynamic excitations without polluting static model semantics.

**Work items**

1. Document the existing fixed internal units (`m`, `kN`, `kNm`, `kN/m²`, `m²`, `m⁴`), and extend them explicitly with **seconds**, **Hz**, `rad/s`, acceleration `m/s²`, translational mass `kN·s²/m`, and consistent generalized rotational inertia units. Never pass `kg` or `N` directly as kN-based mass values without conversion.
2. Specify the conversion of existing `StructuralMaterial.UnitWeight` (`γ`, `kN/m³`) into mass density `ρ=γ/g` (`kN·s²/m⁴`), with `g` in `m/s²`, and line mass `μ=γ A/g` (`kN·s²/m²`). For nodal mass, use `m=weight/g` (`kN·s²/m`). **Unit weight is not mass density.** Keep gravitational self-weight loads separate from inertial mass: providing `UnitWeight` must not silently add a static gravity load.
3. Specify optional user-defined nodal translational masses and physically justified nodal rotational inertias as distinct properties; define duplicate/additive mass policies so section-derived mass is not accidentally counted twice.
4. Choose a versionable JSON/API extension with backward compatibility: existing files lacking mass information must still solve statically; a modal run with missing required mass must return an actionable validation error rather than assume an arbitrary default.
5. Define model-level dynamic requests and response types separate from `StructuralAnalysisRequest` static semantics; reserve temporal load histories and initial displacement/velocity data for later milestones.
6. Validate positivity, finiteness, material/section availability, scale and compatibility with frame/truss element types; record conversion rules in `docs/structural/units.md` when implemented.

**Acceptance criteria:** unit tests for every conversion, rejected missing/invalid/negative mass, static JSON/API compatibility and serialization round trips; **no dynamics silently enabled** in existing CLI commands.

**Exit artifact:** approved dynamic data contract plus unit and validation tests.

### M52 implementation checkpoint — 2026-10-22

**Status: COMPLETE — user confirmed the M52 tests passed; detailed logs not supplied.**

- Added Core-only `StructuralDynamicsDefinition`, `StructuralNodalMass`,
  `DynamicMassUnits`, `StructuralDynamicsValidator` with explicit schema v1 and
  contextual error codes. All member mass is derived from validated `UnitWeight`;
  nodal translational mass is isotropic and explicitly *additive*. An optional
  rotational inertia is permitted only at frame-connected nodes. Version and
  duplicate checks prevent unintended reinterpretation/double counting.
- CLI JSON parser now accepts a strict, optional `dynamics` section while
  preserving existing static file models/commands and output behavior.
  Unknown dynamic properties reject explicitly. No dynamic CLI command exists.
- Introduced reserved public request/output **type definitions** for future modal
  analysis, with no implementation of eigenvalue analysis or modal visualization.
  No modification to `StructuralAnalysisRequest` or static analyzers.
- Added independent conversion tests, negative/overflow/error tests, dynamic
  validation and JSON roundtrip/legacy compatibility tests, and a static numerical
  regression comparing models with and without dynamic metadata. Example:
  `examples/dynamics/m52-cantilever-with-additional-mass.json`.
- Documentation: `docs/structural/units.md` and
  `docs/theory/dynamic-2d-elements.md`; test command: `scripts/run-m52-tests.ps1`.

**Acceptance gate:** run `scripts/run-m52-tests.ps1` on Windows/.NET 8 and
investigate all failures before marking M52 done. A modal solver and mass matrices
remain strictly out of scope until M53+. After M52 acceptance the next milestone is
**M53 element mass matrices and global assembly**.

## Milestone 53 — Element mass matrices and global mass assembly

**Priority:** P0. **Status:** implemented, awaiting .NET 8 acceptance. **Depends on:** M52.

**Goal:** assemble physically valid global inertia `M` alongside the existing global stiffness `K`.

**Work items**

1. Implement tested consistent mass matrices for two-node `Truss2D` and Euler–Bernoulli `Frame2D` elements, respecting the current three-DOF mixed global layout and the physical translational/rotational roles of each degree of freedom. Keep a clearly labeled lumped-mass variant for controlled educational comparison only.
2. Apply the same local/global basis convention to mass as to stiffness (`M_g = T^T M_l T` with the **actual** transformation convention used in code). Test reversed member orientation and arbitrary angles.
3. Accumulate element, nodal and approved additional masses without double counting. Preserve symmetry and nonnegative kinetic energy for admissible velocity vectors.
4. Treat moment releases rigorously. A **stiffness-only static release condensation must not simply be copied onto the dynamic mass matrix**: investigate explicit internal DOFs or a mathematically justified reduction, with tests before supporting released-end dynamics. Until then, reject unsupported released configurations with a diagnostic; no silent approximation.
5. Detect unused/massless rotation DOFs in truss-only or mixed models. Do not regularize zero diagonal masses by injecting fictitious mass; record the correct treatment for M54.

**Suggested locations:** `src/StructuralSolver2D.Analysis/Dynamics/Elements/`, `Dynamics/Assembly/` and dedicated analytical tests; keep static assembly behavior stable.

**Acceptance criteria:** isolated-element checks for total translational mass, symmetry, coordinate invariance, kinetic-energy nonnegativity, dimensional consistency and refinement behavior. Assemblies with physically missing inertia are rejected or explicitly handled by M54, not numerically hidden.

**Exit artifact:** tested, solver-independent `(K,M)` assembly path for supported 2D elements.

### M53 implementation checkpoint — 2026-10-22

**Status: CODE + DOCUMENTATION COMPLETE; .NET ACCEPTANCE PENDING.**

- Core M52 accepted based on user confirmation of passing tests.
- Implemented consistent 4×4 Truss2D and 6×6 Euler–Bernoulli Frame2D mass
  matrices, plus a clearly designated educational lumped-mass variant.
- Internal `DynamicMatrixAssembler` now builds independent full **unconstrained**
  `(K,M)` matrices using the mixed solver's Ux/Uy/Rz DOF layout, material
  `UnitWeight`, element transformations and additive nodal mass metadata.
- Correctly records massless rotations without injecting fictitious inertia;
  rejects moment releases with `M53_RELEASE_UNSUPPORTED` instead of misusing
  static condensation. No modal solver or DOF reduction is implemented.
- Added quadrature, physical invariants, energy, orientation, mixed-model,
  subdivision, release rejection and validation regression tests.
- Documentation: `docs/theory/dynamic-2d-elements.md`; test command:
  `scripts/run-m53-tests.ps1`. No changes to public static API/CLI.

**Acceptance gate:** run `scripts/run-m53-tests.ps1` with .NET 8, investigate
all failures and only then start **M54 — admissible dynamic boundary conditions**.


## Milestone 54 — Dynamically admissible boundary conditions and DOFs

**Priority:** P0. **Status:** implemented, awaiting .NET 8 user validation. **Depends on:** M53 (user validated).

**Goal:** obtain reduced matrices for free physical motion without corrupting the generalized eigenproblem.

**Work items**

1. Reuse the mechanical meaning of `StructuralSupport.OrientationDegrees`, including inclined local restraints. Build the independent constraint subspace from equations `C u = 0`; handle redundant support directions correctly.
2. Construct an admissible-coordinate basis `Z` with `C Z = 0`; solve using `K_r = Z^T K Z`, `M_r = Z^T M Z`. A stiffness system augmented with Lagrange multipliers from the current **static** solver is **not itself** a valid ordinary positive-definite modal mass/stiffness pair.
3. Classify constrained DOFs, physically massless rotations, free rigid-body modes, mechanisms, and singular/ill-conditioned mass or stiffness matrices. Distinguish a genuinely free-free structural model (expected rigid-body zero modes) from an unintended unstable mechanism.
4. If massless algebraic DOFs require condensation, specify and validate the governing dynamic reduction; never remove arbitrary zero-mass DOFs without checking their stiffness coupling and physical effect.
5. Document whether free-free modes are initially supported or intentionally deferred; explicitly encode unsupported cases with stable error messages.

**Acceptance criteria:** inclined support tests; duplicate/redundant restraint tests; unconstrained/free-free and mechanism diagnostics; small models where reduced stiffness and mass are independently computed. Existing static support/reaction behaviors are unchanged.

**Exit artifact:** symmetric, admissible and well-diagnosed reduced generalized-eigenproblem inputs.

### M54 implementation checkpoint — 2026-10-22

- User confirmed the complete M53 regression suite passed; M53 is accepted on
  that report (no test logs attached). Previous M51/M52 acceptance unchanged.
- Added the **internal** `DynamicBoundaryConditionReducer` and
  `SymmetricMatrixRankInspector` in `Analysis/Dynamics/Reduction/`.
- Builds the orthonormal admissible basis `Z` from **production support direction
  semantics**, including inclination and redundant supports, plus only truly
  uncoupled/massless truss-only rotations. Projects `Kr = Zᵀ K Z`, `Mr = Zᵀ M Z`.
- Detects singular/indefinite/near-singular matrices and classifies free-free,
  unrestrained rigid-body motion and additional mechanisms. Explicitly rejects
  stiffness-coupled massless DOFs; no fake masses, implicit static condensation,
  end-release shortcuts or modal frequencies.
- Added M54 test cases and `scripts/run-m54-tests.ps1`; source documentation
  `docs/theory/modal-analysis.md`. No production static solver/API/CLI changes.
- **Acceptance gate:** build the complete solution and run focused plus full
  `.NET 8` regressions on Windows. M54 **validated by user report** (tests passed; test logs not attached).
- **Next after M54 acceptance:** M55 validated generalized eigenproblem solver;
  M56 analytical and convergence benchmarks before user-facing modal results.

## Milestone 55 — First generalized modal eigenvalue solver

**Priority:** P0. **Status:** internal implementation validated per user .NET test confirmation on 2026-10-23; scientific/public acceptance still requires M56. **Depends on:** M54 (user validated).

**Goal:** solve `K_r φ = ω² M_r φ` for a requested number of lowest physically meaningful modes.

**Work items**

1. Select a numerically appropriate **symmetric generalized eigenproblem** implementation for the present small/medium dense models. Evaluate conditioning, library/license constraints and testability before adopting any algorithm. Do **not** substitute the current Gaussian linear-system solver for an eigensolver.
2. Calculate eigenvalues `λ=ω²`, pulsations `ω` (`rad/s`), frequencies `f=ω/(2π)` (Hz), periods `T=1/f` (s), and global modal displacement/rotation vectors. Report nonpositive eigenvalues according to the validated rigid-body/mechanism policy.
3. Normalize eigenvectors by modal mass (`φ_i^T M φ_j≈δ_ij`) and check modal stiffness orthogonality (`φ_i^T K φ_j≈ω_i²δ_ij`). Fix an arbitrary sign convention for deterministic reports and tests, noting that modal sign and absolute amplitude are not physical observables.
4. For each returned mode, calculate `||Kφ-ω² Mφ||` with a scale-aware residual. Guard near-degenerate modes and ordering without assuming individual vectors are unique inside a repeated-eigenvalue subspace.
5. Add the Rayleigh quotient `R(q)=(q^T K q)/(q^T M q)` as an independent validation/educational estimate. Optionally compare a first-mode Stodola–Vianello approximation; neither is a replacement for an adequately validated eigensolver.
6. Introduce a modal analysis API without altering existing static analysis result records; add CLI access only after diagnostics and M56 reference cases pass.

**Acceptance criteria:** validated eigenpairs, mass/stiffness orthogonality, residuals and dimension checks; deterministic frequency ordering; clear errors for invalid spectra or unavailable mass; stable numerical tolerance across stiff/flexible test scales.

**Exit artifact:** experimental modal core ready for the M56 validation gate; not a seismic code check.

### M55 implementation checkpoint — 2026-10-23

- User confirmed **M54 build/tests passed** (logs not attached). M51–M54 are
  accepted for development; historical milestones and viewer M49–M50 remain.
- Added `Analysis/Dynamics/Modal/SymmetricGeneralizedModalSolver.cs`:
  diagonally scale the SPD mass pair; guarded Cholesky;
  solve the scaled symmetric problem by cyclic orthogonal Jacobi; lift eigenvectors
  through `D L⁻ᵀ` and the exact M54 basis `Z`. No external numerical package.
- Returns internal `ExperimentalModalSpectrum`: sorted positive eigenvalues,
  natural pulsations/frequencies/periods, unit-mass-normalized reduced/global
  shapes, Rayleigh estimate, scale-aware residuals and modal orthogonality error.
  Repeated eigenvalues are supported without claiming unique individual shapes.
- Explicit errors for M54-not-ready pairs, unsupported mass, invalid matrices,
  nonpositive or unresolved tiny eigenvalues and non-convergence; no artificial
  mass or stiffness. No changes to public API behavior, CLI, static calculations
  or reports. The existing modal request/output records remain a **future contract**.
- Added M55 focused/integration tests, expanded `docs/theory/modal-analysis.md`
  and `scripts/run-m55-tests.ps1` (full Release build + focused dynamics + full
  regression). **This run has not occurred in the packaging environment**.
- **Gate:** after user confirms .NET 8 acceptance, M56 must still verify beam,
  cantilever, axial bar, mesh convergence and invariances against independent
  analytical references **before any public modal service, reports or viewer**.

## Milestone 56 — Analytical modal benchmarks and convergence gate

**Priority:** P0. **Status:** benchmark implementation complete; awaiting user .NET 8 execution. **Depends on:** M55 prototype (user validated).

**Goal:** demonstrate independent numerical credibility before advertising modal capability.

**Minimum reference set**

1. **Shear-type two-story system:** use the explicit matrices `K=[[2k,-k],[-k,k]]`, `M=diag(m,m)` with analytic eigenvalues `ω₁²=(3−√5)k/(2m)` and `ω₂²=(3+√5)k/(2m)`. Compare eigenvalues, modal ratios, orthogonality and reconstructed equilibrium. This is a deliberately simple **independently specified** benchmark inspired by the book's MDOF and shear-type treatment, not an assertion that these precise equal-story matrices are printed there.
2. **Uniform simply supported Euler–Bernoulli beam:** `ω_n=(n²π²/L²)√(EI/μ)`; compare several low modes for 1/2/4/8/16 elements as suitable, measuring error and observing convergence (without asserting unconditional strict monotonicity of every numerical result).
3. **Uniform cantilever beam:** `ω_n=β_n²/L²√(EI/μ)` with dimensionless roots approximately `β₁=1.875104`, `β₂=4.694091`, `β₃=7.854757`; check frequencies and shapes.
4. **Simple axial bar / truss mode:** analytical longitudinal reference under compatible axial assumptions; separate this from transverse truss mechanisms.
5. **Coordinate invariance:** translate/rotate the same model, reverse element endpoints, alter unit-consistent `k` and mass scalings (`f∝√k`, `f∝1/√m`), rotate support restraints and compare results.
6. **Failure cases:** absent mass, insufficient restraint, mixed zero-mass DOFs, bad section properties, released-end unsupported dynamics, nonfinite input.

**Benchmark structure (proposed):** `benchmarks/modal/`, `benchmarks/modal/expected/`, `tests/StructuralSolver2D.Analysis.Tests/Modal/`. Store independent formula/source, model, expected values, permitted tolerances, element count and what the test actually proves.

**Acceptance criteria:** documented reference derivations; convergence tables; residual and orthogonality assertions; no regression of static analysis; coherent CLI/API errors for unsupported cases. Only after this gate may modal analysis be described as implemented and experimentally validated within its documented scope.

### M56 implementation checkpoint — 2026-10-23

- User confirmed **M55 complete .NET tests passed**; M51–M55 accepted by user report (test logs not attached). The scientific benchmark gate M56 is still separate.
- Added independent analytical continuum references and reproducible formula/fixture generator (`scripts/m56-reference-data.py`), with **14 geometric fixtures + shear matrix case** in `benchmarks/modal/` and a separate optional SciPy finite-element cross-check (`scripts/m56-independent-fem.py`). The SciPy numbers are explicitly **not** measured C# results.
- Added `ModalBenchmarkM56Tests.cs` exercising real M53–M55 analysis on simply supported Euler–Bernoulli beams, cantilevers, restrained axial trusses, and a two-story shear matrix. Meshes 1/2/4/8/16 (axial 2/4/8/16), analytic low-mode frequency bounds, MAC shape comparisons, convergence, mass/stiffness scales, rigid rotation and translation, reverse elements and unsupported-model diagnostics.
- **Scientific finding:** the third *global* frame eigenmode can be **axial**, and on 4-element beams higher bending modes can **alias** the lower bending shape at nodal Uy points. The first Windows M56 run passed 27/28 filtered tests but selected a 1013.397 Hz high mode rather than the expected ~329.015 Hz third bending mode (continuum 323.111 Hz). This was a mode-shape *test selection error*, not an observed M55 eigenvalue residual failure.
- **M56-FIX1 (2026-10-23):** the pinned-beam test now compares the full Hermite-interpolated transverse shape at three interior points/element (includes Rz) and rejects predominantly axial modes. Added a regression that first reproduces the ambiguous nodal MAC and then proves interpolation distinguishes the candidate shapes. Expected continuum frequencies, mesh tolerances, solver and static API are unchanged. Await Windows rebuild and all test gates before marking M56 accepted.
- New `scripts/run-m56-tests.ps1` executes Release build, M56-specific tests, all previous Dynamics tests, full regression and optional formula fixture integrity. Static public API, CLI, viewer and solver algorithms unchanged.
- **Gate pending:** run .NET 8 tests on Windows. Only after they pass may the supported elastic-linear modal core be described as **experimentally benchmark validated (internal)**. User-facing modal API/CLI/viewer belongs to M57, not M56. No professional safety or seismic certification.

## Milestone 57 — Modal API, reports and interactive visualizations

**Priority:** P1. **Status:** implementation complete, .NET 8 acceptance and browser review pending. **Depends on:** M56 (user-reported accepted); existing M39/M47/M48 viewer remains unaffected.

**Goal:** let users inspect modal results without confusing mode shapes with actual motion amplitudes.

**Work items**

1. Extend the public facade or introduce a separate `ModalAnalysisService` with a clear request specifying model, mode count and mass options; return frequencies, periods, normalized shapes, quality indicators and diagnostics.
2. Add explicit CLI modal command (name to be finalized), a compact Markdown/CSV modal table, and optional PDF/XLSX additions only after stable result serialization.
3. Add viewer mode selector, deformed modal shape, relative nodal contributions, eigenfrequency/period labels and play/pause/speed controls. Modal animation is a **normalized illustrative oscillation**; its graphical amplitude is not the predicted real displacement under load.
4. Separate static load/deformation animations from modal shapes both in UI terminology and in data types; modal playback frequency may be accelerated for visibility and must not be mislabeled as elapsed physical time.
5. Respect arbitrary modal sign, shared normalization conventions and degenerate-mode ambiguity in snapshots, tooltips and regression tests.

**Acceptance criteria:** exports reproduce computed frequencies with correct units; snapshot/browser controls are deterministic; model has no modal data unless analyzed; warnings and normalization information are visible; old static exports are unchanged.

### M57 implementation checkpoint — 2026-10-23

- User confirmed that `scripts/run-m56-tests.ps1` passed after deleting the old directory and extracting **M56-FIX1** afresh. M56 is accepted on user-reported tests; the earlier 27/28 failure related to stale files on Windows, not a remaining solver defect.
- New public `StructuralModalAnalysisService.Analyze(StructuralModalAnalysisRequest)` and facade `StructuralSolver2DService.AnalyzeModes(...)` use the existing consistent-mass M53 assembly, admissible M54 projection, and M55–M56 eigensolver. Return mass-normalized nodal `[Ux,Uy,Rz]` mode vectors, frequencies, periods, angular frequencies, residuals, Rayleigh values and orthogonality metrics; report DOF caps, redundant supports and unused rotations. M53/M54 refusal policies remain unchanged (released ends, free-free, mechanisms, massless stiffness-coupled DOFs, excessive DOF count).
- New explicit CLI commands `modal`, `modal-report`, `modal-csv` and `modal-viewer`, with optional positive mode count. Only these modal commands permit JSON without `loadCases`; all old static commands retain their existing validation. No modal execution occurs for existing static requests.
- New standalone `InteractiveHtmlModalViewerExporter`: selector, interpolated Euler–Bernoulli shapes (Hermite with nodal rotation), member/node geometry, relative nodal translation contributions, frequency/period/residual labels, illustrative amplitude and visual playback speed controls. Deliberately **separate** from static HTML viewer and load response animation. No seismic or forced-response claims.
- New `ModalReportExporter`: Markdown summary, invariant-culture CSV frequency and nodal shape tables (with escaped IDs). PDF/XLSX modal extensions deferred until output contract/browser acceptance.
- Added public API, reporting/viewer tests, a modal-only JSON example, and `scripts/run-m57-tests.ps1`: checks M56-FIX1 marker, performs a **clean** Release build, targeted benchmark + M57 tests, full regression and CLI smoke exports in a temporary directory. Browser display / accessibility review still recommended. No .NET SDK in this packaging environment.

**M57 acceptance gate (still open):** Run `.\scripts\run-m57-tests.ps1` from the newly extracted FULL ZIP folder, open the generated modal viewer in a browser, verify mode switching/animation/pause and ensure static viewer/exports remain unchanged. Until then, M57 is **not user validated**. M58 has been implemented at the user's request, but remains entirely provisional until both acceptance gates pass.

### M57-FIX1 — Markdown modal normalization reporting regression (2026-10-23)

- **Windows feedback:** the focused reporting suite completed 2/3 tests; `ModalTables_AreInvariantAndContainProperUnits` failed because the Markdown exporter omitted the leading `Mass:` from the documented `StructuralModalAnalysisOutput.Normalization` string, even though the API already provided that metadata. This is a reporting contract mismatch, **not an identified eigensolver error**.
- **Correction:** `ModalReportExporter.ExportMarkdown` now emits `output.Normalization` in the section **Modal normalization**, followed by the existing explanation that mode amplitudes and signs are not physical response displacements. No static or modal FEM core, modes or units have been changed.
- **Regression protections:** the reporting tests assert the exact API normalization string and verify that a supplied normalization value flows through; the `run-m57-tests.ps1` gate detects older reporting sources and verifies the generated CLI Markdown actually includes `Mass: phi^T M phi = 1`.
- **Validation status:** correction verified by source inspection and packaging/integrity checks only; **.NET 8 test rerun on Windows is required**. M57 remains pending acceptance and M58 is not started. Extract this new FULL ZIP to a clean directory before running the script.

### M57-FIX2 — Distinguish modal-only examples in full CLI regression (2026-10-23)

- **Windows evidence:** the focused M57 reporting tests progressed, but the full CLI regression failed at `CliCommandRegressionTests.DistributedExamples_ShouldAnalyzeAndExportViewer(relative: "dynamics\\m57-modal-cantilever.json")`: `analyze` correctly refused an input with no `loadCases`. The new `examples/dynamics/m57-modal-cantilever.json` intentionally includes `dynamics` and no static load case.
- **Root cause:** the legacy `Examples()` enumerator included every JSON under `examples/` except section catalogs, then unconditionally executed the **static** commands `analyze` and `export-viewer`. This is a test-fixture selection error, not evidence that the static reader should accept modal-only inputs. Preserve the static contract; do not fabricate static load cases in the modal example.
- **Correction:** classify fixtures by actual JSON content. Only models with an explicit `dynamics` object and absent/empty `loadCases` are treated as modal-only; all other examples remain in the existing static test. Added partition coverage to ensure no fixture is lost.
- **Positive/negative CLI regression:** the modal-only set must succeed for `modal`, `modal-report`, `modal-csv` and `modal-viewer`, while `analyze` and `export-viewer` must still reject it. The acceptance script checks for the M57-FIX2 source marker and runs these regressions *before* the full suite.
- **Scope:** production CLI, model parser, static/modal calculations, public API, visualization, fixtures, units and benchmarks are unchanged. This is a test/scripting/documentation correction, not a new structural capability.
- **Acceptance:** M57-FIX2 remains pending. The later M58 FULL archive includes M57-FIX2 and its still-open acceptance checks; extract the newest ZIP into a clean directory and run `scripts/run-m58-tests.ps1`. No .NET SDK 8 is available in the packaging environment.

## Milestone 58 — Linear harmonic response and damping models

**Priority:** P2. **Status:** .NET 8 automatic tests user-confirmed passed on 2026-10-23; browser visual review pending. **Depends on:** M56/M57 previously implemented and tested.

**Goal:** predict steady-state linear response to defined sinusoidal loading, progressing from SDOF to compatible MDOF systems.

**Work items**

1. Start with exact/verified SDOF reference `m x¨+c x˙+k x=F₀ sin(Ωt)`; validate magnification `1/√((1−r²)²+(2ζr)²)`, where `r=Ω/ω_n`, and the phase relationship.
2. Specify modal damping ratios first; consider proportional/Rayleigh **damping** `C=αM+βK` only as a separately documented approximation. Do not confuse Rayleigh damping with the Rayleigh **quotient** used in M55.
3. Extend to MDOF harmonic excitation by explicitly solving the dynamic stiffness / modal frequency response. Include load direction, frequencies, damping assumptions and units.
4. Plot response amplitude and phase against excitation frequency, identify resonance risk without asserting a design safety verdict.

**Acceptance criteria:** analytical SDOF comparisons across low frequency, resonance and high frequency; phase and damping sensitivity tests; dimensional checks; clear failure messages for missing/invalid damping or excitation data.

**M58 implementation checkpoint — automated tests passed (2026-10-23):**

- `StructuralHarmonicAnalysisService` and facade `StructuralSolver2DService.AnalyzeHarmonic(...)` calculate full-spectrum MDOF frequency response from M53–M56 mass-normalized modes, without changing static or free-modal calculations. `HarmonicFrequencyResponseSolver` supplies exact SDOF complex transfer and undamped resonance diagnostics.
- New explicit JSON `harmonic` block specifies synchronized nodal sinusoidal loads `Fx,Fy [kN], Mz [kN*m]`, excitation frequencies [Hz] and **one** damping model: `uniformModalRatio` or **both** `rayleighAlphaInverseSeconds` and `rayleighBetaSeconds` (`C=alpha*M+beta*K`). No default damping and no truncated modes.
- Physical nodal phasors `Ux,Uy [m]`, `Rz [rad]` expose real/imaginary, amplitude and lag phase. CSV/Markdown and standalone HTML amplitude/phase plots via `harmonic`, `harmonic-report`, `harmonic-csv`, `harmonic-viewer`. The chart connects supplied frequency points; it does not guarantee finding resonance peaks.
- Added SDOF analytic checks (including phase), MDOF zero-frequency cantilever/static limit and damping/first resonance checks; unsupported models and bad input are rejected. End-to-end CLI/report viewer tests included.
- **Acceptance update (2026-10-23):** user reports `scripts/run-m58-tests.ps1` passed, including the M57-FIX2 predecessor suite and full regressions; manual browser inspection remains open. SDK was unavailable in the package-building environment.
- See `docs/structural/m58-harmonic-response.md` for formulas, assumptions, input, output, units, errors and scope.


## Milestone 59 — Linear transient / time-history analysis

**Priority:** P2. **Status:** M59 automated .NET 8 tests user-confirmed passed on 2026-10-23; browser review and time-resolution study tracked in M59.1. **Depends on:** user-validated M58 foundations and M56 modal core.

**Goal:** calculate `M u¨(t)+C u˙(t)+K u(t)=F(t)` with explicit initial conditions.

**Work items**

1. Introduce deterministic piecewise/sample time histories with documented interpolation, time units, start/end conditions, load directions and initial displacement/velocity.
2. Evaluate and implement a validated integrator (candidate: average-acceleration Newmark, `β=1/4`, `γ=1/2` for linear systems), with diagnostics for time step, system conditioning and unsupported inputs.
3. Validate analytic SDOF free oscillation, damped free decay, step force and impulse approximations. Distinguish analytic impulses from finite-width numerical pulses.
4. Check convergence on smaller `Δt`, compare energy preservation for undamped unforced stable linear systems, and verify appropriate energy dissipation with damping.
5. Extend results/viewer to time-series displacement, velocity, acceleration, force and relevant member-result envelopes, without storing an unbounded animation for large analyses.

**Acceptance criteria:** time-step convergence evidence, conservative/dissipative energy tests, stable initial acceleration computation, fully documented output sampling and backward-compatible static/modal behavior.

**M59 implementation checkpoint (2026-10-23):**

- `StructuralTimeHistoryAnalysisService` uses the full M55 eigenspace of the M54 reduced K/M pair, with average-acceleration Newmark β=1/4, γ=1/2 on all modal coordinates. This is a physical time integration, not an illustrative animation or a modal cut-off.
- Public request/output includes additive piecewise-linear nodal load patterns, initial nodal displacements and velocities, mandatory damping model, 1..2000 time steps, time-labelled displacement/velocity/acceleration and external-force samples, absolute nodal envelopes, modal frequencies and energy terms.
- `time`, `time-report`, `time-csv`, `time-viewer` CLI, a modal-only JSON fixture, invariant CSV and Markdown exports and standalone time-series viewer. Viewer may downsample for playback; numerical export does not.
- **Explicit limits:** no reaction force histories or member end-force/stress envelopes yet; no seismic base motion, impulse Dirac delta, nonlinearity, nonlinear damping, releases, free-free, mass singularity, nonhomogeneous supports or time-varying distributed member loads.
- SDOF analytic/free/damped/step checks, forced FEM nodal reconstruction, initial constraint check, load superposition, JSON CLI/report tests included. The packaging environment did not run .NET tests; user confirms all M59 automated Windows .NET tests passed. Browser review and dt-halving acceptance remain open (M59.1).
- `scripts/run-m59-tests.ps1` performs clean .NET Release build, predecessor regressions, focused M59 tests, entire solution and CLI end-to-end export checks.


## Milestone 59.1 — Controllo automatico di convergenza temporale (completamento M59)

**Priorità:** P1 di validazione numerica. **Stato:** implementato, da validare con .NET 8. **Dipende da:** M59, confermato dall'utente tramite `run-m59-tests.ps1` (test automatici superati; ispezione visiva browser ancora aperta).

**Obiettivo:** distinguere la stabilità numerica dalla convergenza della risposta nel tempo e rendere espliciti i limiti di risoluzione delle frequenze più elevate.

**Consegne implementate:**

1. Servizio pubblico `StructuralTimeConvergenceAnalysisService` e facade `AnalyzeTimeHistoryConvergence`, per tre analisi M59 *indipendenti* sul medesimo tempo finale (dt, dt/2, dt/4); limite di 500 passi base per non superare 2.000 passi alla griglia più fine.
2. Confronti ai tempi comuni per tutte le nove componenti nodali, normalizzate separatamente per componente; esclusione dei segnali nulli dal giudizio e misura dell'ordine osservato solo se significativa.
3. Confronto di ampiezza e tempo del picco campionato del nodo/componente selezionato (default CLI: ultimo nodo, `Uy`); differenze dei picchi non confondibili con errore a istanti coincidenti.
4. Elenco dei modi propri e dell'indicatore `dt*f` su tutte le griglie; avviso esplicito per `dt*f>0.1` sulla griglia fine. Soglia conservativa ed euristica, non certificazione di accuratezza.
5. `time-convergence`, `time-convergence-report`, `time-convergence-csv`, benchmark su vibrazione libera, modello a risposta nulla, errori/limiti, carichi con nodi non allineati, compatibilità CLI e test PowerShell `scripts/run-m591-tests.ps1`.

**Criteri di accettazione:** build Release pulita, test M59.1 focalizzati e regressione completa .NET superati, report CLI reali generati, casi limite leggibili e documentazione delle soglie in `docs/structural/m591-time-convergence.md`. L'esito di convergenza non è una garanzia di sicurezza strutturale. Conservare M59 originale invariato.

**Da verificare:** .NET 8 sul PC Windows dell'utente e revisione quantitativa della convergenza per le eccitazioni effettivamente importanti. Solo allora chiudere M59.1. Nessuna modifica normativa o M60 anticipata.

## Milestone 60 — Exploratory seismic and response-spectrum analysis

**Priority:** P3. **Status:** implemented 2026-10-23; **pending Windows .NET 8 acceptance**. **Depends on:** validated M53–M59.1 FEM dynamics and time response.

**Implemented**

1. Uniform ground translation in GlobalX or GlobalY, with signed inertia equivalent `-M*r*a_g(t)`, matrix `M` before boundary projection and all consistent-mass terms (including rotational coupling).
2. Reuse the M59 linear Newmark complete modal basis for relative U/V/A and output of absolute global translational acceleration `a_abs = a_rel + r*a_g`.
3. Participation factors `Gamma_i`, effective modal masses, cumulative percentage referred to the **unconstrained** ground mass `rᵀ M r`, with explicit warnings on truncation and low participation; the admissible total need not reach 100% of global mass.
4. User-supplied elastic **pseudo**-acceleration spectrum Sa(T) in m/s². Linear interpolation in period without extrapolation; `Sd_i=Sa(T_i)/ω_i²`; nodal displacement/rotation envelopes by SRSS, with warnings for closely spaced participating modes. No CQC and no unsafe signed sum.
5. Separate API/CLI/report/CSV (`seismic-time*`, `seismic-spectrum*`) and `examples/dynamics/m60-seismic-cantilever.json`. All outputs clearly indicate **NON-NORMATIVE**.
6. Independent analytical one-DOF consistent-mass bar and two-mode bar-chain checks, bad-input tests and CLI end-to-end regressions. Script `scripts/run-m60-tests.ps1` performs clean build, focused tests, complete regression and exported-file checks.

**Acceptance criteria:** successful clean .NET 8 build, dedicated M60 tests, full M1–M59.1 regression, output smoke verification, coherent mass/frequency and ground acceleration signs, no changes to legacy commands. M60 is not accepted until the user runs the script and confirms. Accuracy of seismic time history depends on the time step and accelerogram sampling, which require scenario-specific review.

**Explicit exclusions:** authoritative earthquake records or hazard determination, normative response spectra, CQC, directional/modal combination under codes, ground rotations, independent support motions, SSI, nonlinear damage/plasticity, force/reaction histories and structural safety assessments. A separate future project would be required for any standards-based design verification.

## Milestone 61 — Nonlinear dynamics: isolated feasibility study only

**Priority:** P4 / research. **Status:** feasibility study documented (2026-10-23), no nonlinear solver feature delivered; automatic PowerShell/.NET validation pending. **Not** part of the minimum viable modal or dynamic deliverables.

Read Ch. 1 §§1.7–1.8 and Ch. 6 as theoretical background for elasto-plastic oscillators, hysteresis, phase portraits and incremental nonlinear FEM. Produce a design study of possible nonlinear material/element state, incremental equilibrium, iterations, tangent stiffness, path dependence, convergence and validation. **Do not** merge nonlinear states into current `Frame2D`/`Truss2D` static results or imply plastic capacity, collapse or seismic safety capability without separate implementation and testing.

**Exit artifact:** technical feasibility note and independent validation shortlist; no solver feature is promised.

**M61 study checkpoint (2026-10-23):** `docs/structural/m61-nonlinear-feasibility.md` supplies the isolated constitutive-state design, tangent/residual/Newton and rollback strategy, energy-accounting and dimensional safeguards, staged validation shortlist, and explicit exclusions. `scripts/m61-check-bilinear-return-map.py` is a *research-only* pure-Python one-dimensional constitutive spike, verified in the packaging environment; it is not an SDOF motion solver or a production nonlinear feature. Static, modal, transient, seismic code/API and model JSON are unchanged. M60 acceptance is user-confirmed from Windows tests. A separate engineering decision is required before any nonlinear production implementation.

**Build/test housekeeping added in M61:** `scripts/clean-project-build.ps1` locates MSBuild projects and deletes only their immediate `bin`/`obj` directories; all **12** `scripts/run-m*-tests.ps1` runners invoke it as their first executable action after setting `$ErrorActionPreference`. The previously duplicated `dotnet clean` calls were removed. `scripts/run-m61-tests.ps1` tests the cleanup policy and safe behavior on temporary projects, then builds/tests the full solution. See `docs/structural/build-cleanup.md`. .NET and PowerShell execution remain pending user verification; this is not a claim of an executed .NET suite.


## Milestone 62 — SDOF nonlinear elastoplastic prototype (research, isolated)

**Priority:** P4. **Status:** experimental implementation complete; .NET 8 Windows acceptance pending. **Dependency:** M61 feasibility and mandatory cleanup, both user-tested.

**Deliverable:** a one-degree-of-freedom spring–mass–damper laboratory model; no member nonlinearity, no section capacities, no seismic nonlinear solver and no safety/design claim.

1. Implement a stateful bilinear isotropic-hardening spring with no implicit state mutations: elastic trial, return mapping, consistent tangent, stored hardening energy and plastic dissipation.
2. Integrate the nonlinear physical SDOF equation with average-acceleration Newmark and consistent-tangent Newton. Do **not** reuse the M55 mass-normalized linear eigenmodes or modify the M59 integrator.
3. Commit plastic state only on Newton convergence; on non-convergence or numeric failure preserve the entire previous state, and produce a specific error.
4. Expose Newton iteration counts, force residual and **separate** discrete-work vs. constitutive-energy balance results. Do not treat a small algorithmic residual as time-step accuracy proof.
5. Validate elastically against M59, plastically against an **independent per-branch closed-form Newmark solution**, cyclic unloading, perfect plasticity, irreversible non-negative dissipation, failed-step rollback and time-step sensitivity.
6. Keep everything under internal `Dynamics/Nonlinear`, with no changes to public solver/CLI/viewer/JSON; maintain central cleanup as the first test action.

**Acceptance:** run `scripts/run-m62-tests.ps1` on Windows .NET 8. The script runs build, targeted xUnit tests, full legacy regression and an optional standalone Python reference. Until Windows acceptance M62 is not marked completed. See `docs/structural/m62-sdof-elastoplastic-prototype.md`.

**Potential next research gate (M63):** only after acceptance, feasibility and test specification for a single *axial elastoplastic Truss2D element*. Do not infer that frame plastic hinges, collapse analysis or regulatory seismic checks are available.

**M62-FIX1 checkpoint (2026-10-23):** l'utente ha confermato l'esecuzione delle prime tre fasi del runner M62, ma lo step opzionale Python 4/4 ha restituito un'eccezione PowerShell generica senza traceback Python. Il riferimento indipendente viene eseguito correttamente nell'ambiente di confezionamento (Python 3.13.5, residuo ~1,585e-12 kN). Corretto `run-m62-tests.ps1` per provare realmente Python >=3.7, usare `py -3` se necessario, segnalare la verifica opzionale non eseguibile come `SKIPPED`, mantenere bloccante un reale fallimento del benchmark e mostrarne l'output. La pulizia `bin`/`obj` rimane la prima azione. Vedere `m62-python-benchmark-runner-fix.md`. **M62-FIX1 necessita ancora validazione PowerShell Windows**, prima di iniziare M63.

## Proposed test and engineering checklist (applies to every milestone)

- [ ] Scope and exclusions are written before implementation.
- [ ] Input values and units are explicitly defined; errors are actionable.
- [ ] Each formula has a traceable derivation or independent cited reference.
- [ ] Matrix symmetry, physical admissibility, conditioning and residual checks are appropriate for the solved problem.
- [ ] One simple analytical test, one integration/regression test, one failure-mode test and, where relevant, a convergence study exist.
- [ ] Static APIs/CLI/model JSON/output conventions and the existing viewer continue to work.
- [ ] Numerical tolerances explicitly handle zero, relative scaling and near-degenerate cases.
- [ ] Documentation and benchmark expected values change in the **same** milestone as code.
- [ ] Automated tests/build pass in the supported .NET 8 environment; browser checks pass for viewer changes.
- [ ] Release notes identify capabilities as `planned`, `experimental` or `validated within scope` — never silently `certified`.

## Practical first implementation order

The **historical next task after M54 .NET acceptance** was **M55**, the generalized eigenvalue solver; it has now been implemented and user-validated. The current remaining gate is M59.1, followed by M60, not a repeat of M55. Preserve unit conversions and strict errors established by M52; do not guess mass or silently reuse static end-release condensation. Publish the first user-visible modal frequency only after **M53 → M54 → M55 → M56** are all verified.

**Suggested future artifacts, created only when those milestones are implemented:**

```text
docs/theory/static-energy-verification.md       # M51
docs/theory/dynamic-2d-elements.md              # M52–M53
docs/theory/modal-analysis.md                   # M54–M56
docs/structural/modal-analysis.md               # M57
benchmarks/verification/                        # M51
benchmarks/modal/                               # M56
src/StructuralSolver2D.Analysis/Dynamics/       # M53 accepted; M54 implemented, .NET gate pending
tests/StructuralSolver2D.Analysis.Tests/Modal/  # M55–M56
```

**End of added roadmap track (2026-10-22).** Future progress updates should mark individual milestone status here with evidence (tests/benchmarks), without rewriting completed historical milestones.
