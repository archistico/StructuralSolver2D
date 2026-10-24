# Ultimo checkpoint 2026-10-24 — M90-A

Base M89-B con test confermati dall'utente. Aggiunto classificatore EC3 Table 5.2 per IPE/HEA/HEB, solo compressione pura / flessione forte pura. Validazione contro esempi Bernuzzi cap. 4 ed OPPO. Da compilare/testare su Windows con `scripts/run-m90a-tests.ps1`. Roadmap dettagliata `docs/structural/M90A_CLASSIFICAZIONE_SEZIONI_EC3.md`. PDF di Bernuzzi esterno al repository e non redistribuito. Nessuna modifica al motore FEM, nessuna dichiarazione di conformità strutturale.

## M88-B2 (24 ottobre 2026): catalogo OPPO IPE/HEA/HEB/INP

La baseline M88-B è confermata dall'utente come test superati. M88-B2 aggiunge `CommercialRolledSteelCatalogM88B2` e 40 profili tabellati indicativi dalle 4 pagine OPPO selezionate dall'utente. Il progetto e i dati preesistenti sono immutati; il nuovo gate Windows attende conferma. Non sono introdotte formule normative, e le designazioni UNI delle pagine OPPO sono riferimenti storici. Prossimo M88-C materiali e resistenze caratteristiche, poi verifiche. Leggere `docs/structural/m88b2-oppo-rolled-steel-catalog.md`.

# Handoff operativo 24/10/2026 — M85-A (TEST WINDOWS PENDENTI)

**Ultimo gate confermato:** M83-A completo superato dall'utente. Il CSV numerico del benchmark M83 non è stato allegato; non prendere ancora decisioni sul solver sparso/banded M83-B. La precedente intestazione M83-A «in attesa» sotto è storia superata.

**M85-A nuovo:** `tests/StructuralSolver2D.Analysis.Tests/Validation/M85/IndependentStaticInvariantsM85Tests.cs` include quattro famiglie: mensola inclinata con formula Euler–Bernoulli e risultanti; due Truss2D di rigidezza diversa con formule a barre; portale iperstatico con Maxwell–Betti e combinazione firmata; portale ruotato con risultati locali invarianti. `scripts/run-m85-tests.ps1` esegue una build pulita, gate numerici e tutti i test Core, Analysis, Reporting e CLI. `docs/structural/m85a-independent-validation.md` dettaglia casi e tolleranze. **Nessuna modifica ai file di produzione `src/`.**

**Stato M85-A:** implementato, non compilato in questo ambiente (SDK .NET 8 assente). Su Windows eseguire `scripts/run-m85-tests.ps1`, riportare eventuali errori esatti. Se passa, avanzare a M85-B riferimenti esterni e controllo end-to-end API/JSON/CLI/report, quindi M86 release Core FEM elastico. Non anticipare normative, GUI, plasticità globale M84. In ogni avanzamento ricordare stato, risultato, prossimi lavori, rischi e semplificazioni. Consegnare ZIP integrale.

---

# Handoff operativo 24/10/2026 — M83-A (TEST WINDOWS PENDENTI)

**Ultimo gate confermato dall’utente:** M82 supera i test Windows dopo FIX CS0246. Non confondere il vecchio handoff M82 «pendenti» qui sotto con lo stato attuale. Strategia vincolante: FEM 2D acciaio/legno → verifiche normative SLU/SLE → GUI C#/Avalonia, solo alla fine.

**Nuovo M83-A:** reali benchmark di scalabilità Frame2D su primo ordine e rigidezza geometrica linearizzata M82; codici C# esclusivamente in `tools/StructuralSolver2D.PerformanceM73/M83ScalabilitySuite.cs` e flag `--m83` nel Program; `tests/StructuralSolver2D.Analysis.Tests/Performance/M83/Frame2DScalabilityM83Tests.cs`; `scripts/run-m83-tests.ps1`; documento `docs/structural/m83a-frame2d-scalability-decision.md`. **Nessun file solver FEM `src/` modificato.** M83-B sparso condizionato ai dati del report di scalabilità. M84 plasticità globale è opzionale, M85/M86 priorità per chiudere il FEM elastico. NON anticipare normative o GUI.

**Gate Windows per l'utente:** `scripts/run-m83-tests.ps1`; esegue riparazione file obsoleto M82 (se esiste), pulizia bin/obj, Release build, gate xUnit M73+M82+M83, regressione Core/Analysis/Reporting/Cli separati, quindi export `artifacts/benchmarks/m83/m83-scalability.csv` e `.json`. L'ambiente di confezionamento non ha .NET 8: NON dichiarare che M83-A sia compilato/testato prima del log Windows. Consegnare sempre ZIP completo.

**Promemoria fisso di comunicazione:** per ogni avanzamento riassumere stato e ultimi test effettivamente confermati, cosa è cambiato, cosa fare poi, criticità/difficoltà, cosa si può semplificare/rinviare.

---

# Handoff operativo 24/10/2026 — M82 (TEST WINDOWS PENDENTI)

L'utente ha confermato PASS della suite M81 Windows. M75–M81 validati per dichiarazione dell'utente. M82 implementa **solo** un calcolo globale lineare con rigidezza geometrica basata su N di primo ordine congelati: `Frame2DGlobalSecondOrderM82` e facade `StructuralSolver2DService.AnalyzeFrameSecondOrder`. Accetta singolo caso con sole forze/momenti nodali, aste Frame2D senza svincoli, supporti rigidi paralleli agli assi; altrimenti rifiuta esplicitamente. Restituisce spostamenti e reazioni linearizzati, primo ordine, N assiali di riferimento, moltiplicatore della perdita di definitezza positiva entro intervallo finito, NON sollecitazioni N/V/M del secondo ordine o modo critico. Non cambia le API e i solutori lineari.

Consegna completa `StructuralSolver2D_M82_SECONDO_ORDINE_GLOBALE_2026-10-24_FULL.zip`; test .NET da eseguire con `scripts/run-m82-tests.ps1` (prima pulizia bin/obj). Test xUnit soltanto C#. Nel packaging nessun dotnet installato: non affermare che M82 è testato; riportare log Windows se fallisce e correggere prima di M83. Riferimento autoritativo [nota M82](docs/structural/m82-global-linearized-second-order.md). Roadmap M83 condizionale, niente GUI/normative.

---

# Checkpoint prioritario M81 — 24/10/2026

- **Ultimo gate numerico completo Windows confermato: M80-FIX3**. L'utente conferma esplicitamente «tutti i test passati». Pertanto i milestone implementati M75–M80 risultano superati nei rispettivi test cumulativi; precedente stato «M73 ultimo valido» nei documenti sottostanti è **archivio storico superato**.
- **M81 implementato, NON testato in .NET 8 Windows**. Base esatta: archivio integrale `StructuralSolver2D_M80_FIX3_PAREGGIO_TAGLIO_2026-10-24_FULL.zip`.
- Nuovo codice SOLTANTO `src/StructuralSolver2D.Analysis/Frame2D/SecondOrder/Frame2DInitialStressM81.cs`: `Kg` lineare con forze assiali di riferimento esplicitamente date (positivo trazione, negativo compressione), `Ke+Kg`, trasformazione assi, minirisolutore cantilever a 2 DOF che rifiuta perdite di definitezza.
- **NO** solver globale, nessuna modifica a `Frame2DAnalyzer`, `PlaneStructureAnalyzer`, `StructuralSolver2DService`, JSON, CLI, Frame2D non lineare M72 o precedenti. M82 introdurrà la soluzione globale/eigenbuckling previo benchmark.
- Test solo C#/xUnit `tests/StructuralSolver2D.Analysis.Tests/Frame2D/M81/Frame2DInitialStressM81Tests.cs` con Gauss–Legendre indipendente, legge trazione/compressione, cantilever Pcr discretizzato, pinnato Pcr discreto, controlli input e rotazioni.
- Runner `scripts/run-m81-tests.ps1` inizia con `clean-project-build.ps1`, poi console stabile, build Release, gate M81 e regressione separata di Core, Analysis, Reporting e Cli. Log persistente; nessun runner Python.
- Documentazione `docs/structural/m81-frame2d-initial-stress.md`; roadmap vincolante `docs/structural/vision-and-roadmap-2d.md`: FEM → norme → GUI C#/Avalonia.
- **ATTENZIONE:** senza dotnet nell'ambiente di confezionamento compilazione/test M81 NON eseguiti; solo utente può confermare gate Windows. Distribuire sempre ZIP completo, non sostituire con patch.

---

# Checkpoint prioritario M80-FIX3 — 24/10/2026

- Ultimo log Windows caricato `dotnet-20261024-113452-19084-371a6c90.log`: **compilazione Release OK, 0 avvisi e 0 errori; Core benchmark M80 17/17; Core cataloghi/units 25/25; gate M78/M79 13/14**. Un fallimento: `Frame2DExactExtremaM78Tests.InteriorPointLoad_OneSidedShearAndExactMomentPeakArePreserved`, atteso ascissa minimo taglio `x=2`, reale `x=8`. Nessuna regressione completa post-gate nel log.
- Spiegazione: senza distribuito, taglio `+9` in `[0,2)` e `−3` in `(2,8]` (L=8 m, P=−12 kN a x=2); entrambi gli estremi sono plateau e l'algoritmo `Frame2DExactExtremaFinderM78` inserisce prima gli estremi dell'asta, poi i due lati del punto di salto. `MinBy/MaxBy` privilegiano la prima occorrenza. Atteso deterministico: Vmax `x=0 / Continuous`, Vmin `x=8 / Continuous`.
- M80-FIX3 cambia **solo il test** e la documentazione. Il test ora controlla anche `V(2⁻)=+9`, `V(2⁺)=−3 kN`, salto `−12 kN` e `M(2⁻)=M(2⁺)=18 kNm` via `Frame2DInternalForceSampler.PrepareMember` (visibile ai test C#). Il test univoco con +1 kN/m e carico −12 rimane immutato.
- Preservare tutti i sorgenti di produzione M80-FIX2 e il runner `scripts/run-m80-tests.ps1`. Eseguire da ZIP integrale M80-FIX3 sul Windows utente; pulizia prima, build, gate C#, regressione Core/Analysis/Reporting/CLI. Non segnare M75–M80 come validati finché l'intero runner non passa.
- Strategia prodotto: FEM 2D acciaio/legno; prima calcolo, poi normative, infine Avalonia. Niente benchmark Python utente.
- Riferimento: `docs/structural/m80-fix3-minimum-shear-tie.md`.

---

# Checkpoint prioritario M80-FIX2 — 24/10/2026

**Ultima evidenza Windows:** il runner M80-FIX1 ha mostrato `Analysis.Tests`: **541 totali, 539 superati e 2 falliti**. `Cli.Tests` in precedenza mostrava 231/231 superati. Non attribuire questi conteggi all'intera soluzione; i progetti `Core/Reporting` non sono ancora confermati dall'ultimo report. **M73 rimane l'ultimo milestone numerico integralmente validato**.

I due fallimenti Analysis erano test M78/M79, non prove di malfunzionamento dei solutori:
- `Frame2DExactExtremaM78Tests.InteriorPointLoad_OneSidedShearAndExactMomentPeakArePreserved`: `Vmax=9 kN` su tutto `[0,2)`; per tie deterministico M78 sceglie `x=0 / Continuous` invece della sezione `x=2 / BeforePointLoad`. Sistemata aspettativa e aggiunto test `PointLoadWithDistributedGradient_UniqueShearExtremaRetainBothOneSidedLimits`: `+1 kN/m` verso l'alto e `-12 kN` in `x=2 m` su `L=8 m` → `Vmax=+7 kN` Before, `Vmin=-5 kN` After, a `x=2 m`.
- `Frame2DStaticForceEnvelopeM79Tests.Combinations_SelectSignedMinMaxAndAbsoluteWithGoverningIdentity`: termine `Q × 0` nella combinazione `REV`, vietato dal validatore comune `LOAD_COMBINATION_INVALID_FACTOR`. Eliminato termine nullo, lasciato `G × -1`.

Nessun `src/` o algoritmo FEM cambiato. `scripts/run-m80-tests.ps1` ora aggiunge gate di test mirato M78/M79 e riduce la verbosità della regressione generale, mantenendo pulizia iniziale, wrapper/log, Release e quattro progetti testati separatamente. **Rieseguire `scripts/run-m80-tests.ps1` su Windows prima di promuovere M75–M80.** Nota `docs/structural/m80-fix2-m78-m79-tests.md`. Numerici solo C#/.NET; nessun Python da eseguire. Nuova roadmap 2D calc → norme → Avalonia invariata.

---

# CHECKPOINT CORRENTE — M80, 23/10/2026

- **Ultimo gate .NET 8 confermato dall’utente: M73.** M74 è solo riallineamento; M75, M76, M77, M78, M79 e **M80** sono implementati e NON ancora testati sul suo Windows. Non promuoverli prima del riscontro esplicito.
- M80 aggiunge solo `StructuralSolver2D.Core/Model/Sections/{SectionGeometricPropertiesM80,SectionGeometryFactoryM80,SectionProfileCatalogM80}.cs`, con proprietà sezionali geometriche in-plane/out-of-plane per forme simmetriche ideali. Solver, Analysis, CLI e schema `StructuralSection`/catalogo JSON v1 preesistenti invariati.
- Catalogo M80 separato opzionale, dimensioni in **millimetri**, `dataStatus` Illustrative o SourceDocumented con riferimento+edizione obbligatori nel secondo caso. Nessuna verifica commerciale automatica. Esempio `examples/sections/m80-parametric-example.json` NON rappresenta IPE, HE o profili conformi.
- Test solo C#/xUnit `SectionGeometryM80Tests`, runner `scripts/run-m80-tests.ps1`: prima pulizia `bin/obj`, poi console testuale, Release build, test sezioni, regressione cataloghi legacy, regressione intera incl. M75–M79. Dotnet non disponibile nell'ambiente di preparazione.
- Riferimenti: `docs/structural/m80-section-geometric-properties.md`, `docs/structural/vision-and-roadmap-2d.md` (roadmap vincolante), `README.md` e `docs/structural/roadmap.md`. **Prossimo M81**: secondo ordine P–Δ/rigidezza geometrica e benchmark; non introdurre normativa, GUI Avalonia, 3D o finti profili commerciali.
- Ordine di prodotto invariato: **calcolo FEM → NTC/Eurocodici SLU/SLE → GUI leggera C#/Avalonia**. Solo bench/test C#, nessuno script numerico Python; ZIP completo a ogni consegna.

---

## M79 — 23 ottobre 2026 — Inviluppi N/V/M Frame2D (test Windows pendenti)

- Base: M78 completo (560 file); tutti i file preservati. M73 ultimo gate numerico testato dall’utente, M75–M79 ancora pendenti.
- Servizio pubblico `AnalyzeStaticForceEnvelope(model, scenarios)`: scenari `LoadCase`/`LoadCombination` espliciti, nessuna generazione normativa. Ogni scenario risolto una volta con `PlaneStructureAnalyzer` e analizzato da M78.
- Valori globali per asta Frame2D: minimo, massimo e massimo assoluto firmati N/V/M, combinazione responsabile, ascissa, lato dei salti. Non curve di inviluppo x per x; nessuna combinazione concomitante N/M/V per verifiche normative.
- Export C# CSV/Markdown `StaticForceEnvelopeReportM79`, test C# xUnit con confronti indipendenti, fattori negativi, parità, salto e sicurezza CSV; nessun Python.
- Runner `scripts/run-m79-tests.ps1`: pulizia bin/obj PRIMA, wrapper console, build, test M78/M79, reporting e regressione intera. I test .NET 8 non sono stati eseguiti nell’ambiente di confezionamento.
- Prossimo M80: proprietà di sezioni / cataloghi; conservare priorità motore FEM → normativa → Avalonia.
- [Nota tecnica](docs/structural/m79-exact-static-force-envelopes.md).

# CURRENT HANDOFF — M78, 23/10/2026

**ULTIMO CODICE VALIDATO DALL'UTENTE:** M73. M74 solo riallineamento strategico. M75, M76, M77 e M78 implementati, **non ancora compilati/testati su Windows**. La suite cumulativa da eseguire è `scripts/run-m78-tests.ps1` (prima pulizia bin/obj). Non promuovere alcuno di questi quattro milestone prima di feedback esplicito. L'ultimo ZIP completo è il checkpoint M78 (contiene l'intero M77). **Tutti i benchmark C#/.NET; mai script Python richiesti all'utente.**

M78 aggiunge `Frame2DExactExtremaFinderM78`, `ExactMemberInternalForceExtremaM78` e `StructuralSolver2DService.FindExactStaticMemberForceExtrema()`. Rileva veri estremi nelle funzioni polinomiali N/V/M, includendo il salto prima/dopo un carico concentrato, senza dipendere dal passo del diagramma grafico. Il servizio è opt-in; il riepilogo precedente resta campionato. Test in `tests/StructuralSolver2D.Analysis.Tests/Frame2D/Frame2DExactExtremaM78Tests.cs`. Dettagli in `docs/structural/m78-exact-frame-force-extrema.md`.

**PERIMETRO:** software FEM 2D open source acciaio/legno per didattica e primo dimensionamento; niente 3D, c.a., BIM, pareti, muratura o XLAM. Sequenza: terminare motore di calcolo → verifiche normative NTC/Eurocodici → GUI C#/Avalonia per ultima. M79 successivo: inviluppi multi combinazione; NON introdurre normative durante M79. [Roadmap](docs/structural/vision-and-roadmap-2d.md).

---

# Handoff prioritario M77 — 23/10/2026 (sostituisce le indicazioni di prossimo step nei checkpoint storici)

- Stato: **M73 test confermati**; **M74 documentazione**; **M75–M77 implementati ma test Windows .NET 8 NON eseguiti**. Non promuovere a validati fino alla prova utente.
- M77 supporti: proprietà opzionali `StiffnessUx`/`StiffnessUy` kN/m, `StiffnessRz` kNm/rad negli assi ruotati del supporto. Rigidezza `k ddᵀ` aggiunta ai tre assemblaggi statici, reazione elastica `-k(dᵀu)d`; vincoli rigidi sulla stessa direzione di una molla respinti dal validatore.
- Carico `StructuralLoadType.SupportSettlement` su `StructuralLoadTargetType.Support` e `Direction=LocalX|LocalY|MomentZ`, unità m/rad, deve corrispondere a restrizione rigida. Fattorizzato per caso/combinazione nel RHS del sistema aumentato `Cu=g`; ridondanze incompatibili respinte. `GlobalEquilibriumChecker` non lo considera forza applicata.
- Reader/writer JSON M75 aggiornati; fixture `docs/structural/fixtures/m77-elastic-support-settlement.json`. Validazione dinamica lo rifiuta (non supportato). **Non applicare M77 a analisi dinamiche, non lineari o normative.**
- Test predisposti solo in C#: `ElasticSupportSettlementM77Tests`, `ElasticSupportJsonM77Tests`. Script `scripts/run-m77-tests.ps1`: pulizia bin/obj per prima, stable console, Release, test mirati, CLI ed intera regressione incluse M75/M76. Non c'è esecuzione dotnet in questo ambiente.
- Riferimento: `docs/structural/m77-elastic-supports-and-settlements.md`, `docs/structural/vision-and-roadmap-2d.md`. Prossimo M78 solo dopo il gate Windows M75–M77. Solutore e GUI Avalonia restano fasi distinte.

# Handoff prioritario M76 — 23/10/2026

- Ultimo gate validato dall'utente: **M73**. M74 soltanto roadmap. **M75 e M76 implementati, NON testati su Windows .NET 8.** Non promuoverli a validati senza output del runner.
- M76: `StructuralLoad.SelfWeightLoad` e `Core.Model.Loads.SelfWeightStaticLoadExpander`, `StructuralModelValidator` con peso specifico obbligatorio; espansione statica Frame2D in `UniformDistributedLoad` e Truss2D in forze nodali consistenti. `Frame2DAnalyzer`, `Truss2DAnalyzer`, `PlaneStructureAnalyzer` eseguono l'espansione nel proprio solver; checker globale, campionatori Frame2D e builder di visualizzazione la usano per rappresentare risultati e carichi. Non muta il modello originario.
- Il peso specifico `UnitWeight` è in kN/m³, scala `SelfWeight.Value>0` dimensionless, direzione fisica `-GlobalY`, non generato automaticamente. Una volta per caso; combinazioni consentite. **Solo analisi statica lineare**, non dinamica o non lineare.
- Nuovi test C# `SelfWeightM76Tests`, `SelfWeightJsonM76Tests`, JSON CLI `fixtures/m76-self-weight-beam.json`. Il test storico che attendeva `LOAD_SELF_WEIGHT_NOT_SUPPORTED` ora si attende `LOAD_SELF_WEIGHT_MISSING_UNIT_WEIGHT`.
- Runner `scripts/run-m76-tests.ps1`: prima pulizia `bin/obj`, poi wrapper log testo, build e regressioni. Nessun Python, nessun altro codice solutore modificato. Documento: `docs/structural/m76-automatic-self-weight.md`.
- **Prossimo:** eseguire i test M75/M76; risolvere ogni errore segnalato prima di M77. Obiettivo software: FEM 2D open source acciaio/legno, prima calcolo, poi verifica NTC/EC SLU/SLE, infine GUI Avalonia.

---

# Handoff prioritario M75 — 23/10/2026

- M73: test Windows C#/.NET superati dall'utente. M74: riallineamento strategico/documentale. **M75: codice implementato, test Windows ANCORA DA ESEGUIRE**.
- Nuovo writer canonico in `src/StructuralSolver2D.Cli/Input/StructuralModelJsonWriter.cs`, preflight in `StructuralModelJsonContractValidator.cs`, eccezione strutturata e `ReadValidated` opt-in. Conversioni esplicite unità in `Core/Units/StructuralUnitConversions.cs` con test C#. Contratto: **solo modello standard e `dynamics` v1**; estensioni custom fuori perimetro e non preservate.
- Test `tests/StructuralSolver2D.Cli.Tests/Input/StructuralModelJsonContractM75Tests.cs`; runner `scripts/run-m75-tests.ps1` prima pulisce bin/obj, poi log, build, test mirati e regressione completa. Non sono stati toccati i solver FEM. Nessun benchmark Python.
- Documentazione autorevole: `docs/structural/vision-and-roadmap-2d.md`, `docs/structural/m75-json-model-contract.md`. Prossimo sviluppo **M76 carichi/peso proprio**, soltanto dopo la verifica M75.
- Sequenza di progetto vincolante: calcolo FEM completo → NTC/Eurocodici acciaio+legno SLU/SLE → GUI C#/Avalonia alla fine; OpenCad2D opzionale. Non aggiungere BIM, 3D, c.a., muratura, pareti o X-LAM.

---

# Checkpoint strategico M74 — 23/10/2026 (prevale sugli handoff storici)

- **Stato test confermato dall’utente:** M51–M73 (ultima esecuzione M73 .NET 8 riuscita). M74 modifica solo documentazione/roadmap, non gli algoritmi.
- **Nuova missione:** FEM 2D open source per strutture ad aste in acciaio e legno, destinato a studenti, docenti, professionisti per studio e valutazioni preliminari. Esclusi 3D, c.a., muratura, piastre, pareti, X-LAM e BIM.
- **Sequenza obbligatoria:** calcolo → norme/SLU/SLE → editor C#/Avalonia alla fine. OpenCad2D è opzionale, non più il milestone M74.
- **Fonte autorevole per il futuro:** `docs/structural/vision-and-roadmap-2d.md`; il vecchio `roadmap-forward-m63-m75.md` è archivio storico, non la nuova pianificazione.
- **Lacune prioritarie:** auto-peso, carichi, vincoli elastici, post-processing, inviluppi, secondo ordine/stabilità, cataloghi verificati, NTC/EC3/EC5, verifiche e report.
- **Non confondere:** risultati FEM verificati con verifiche normative, né test automatici con certificazione per l'uso professionale. Fuori piano servono dati aggiuntivi.
- **Regole operative:** tutti i benchmark numerici C#/.NET; ogni runner PS comincia con pulizia `bin/obj`; mantenere inalterato il solver validato, aggiornare README, roadmap, handoff e consegnare ZIP integrale per modifiche future.

---

# Checkpoint M73 — baseline di prestazioni, 23/10/2026

- **Gate utente:** M71 e M72-FIX2 superati (compresi gli avvisi xUnit corretti). **M73 implementato ma non validato con .NET 8 su Windows**.
- Sorgenti FEM M63–M72 non modificati. Nuovo progetto console `tools/StructuralSolver2D.PerformanceM73` (.NET 8) incluso in soluzione, con accesso `InternalsVisibleTo` solo alla assembly di benchmark; non modifica le API pubbliche.
- Quattro scenari: Dense-SPD (riferimento `x=1`), Truss-static (formula `F·L/EA`), Truss-Newmark (residuo ed elasticità), Frame-hinge-trials M72 (simmetria tangente e no-commit). Output mediana tempo/allocazioni, dimensioni, iterazioni, accuratezza in CSV/JSON; **non impone soglie di tempo**.
- Nuovi test C#/xUnit `Performance/M73/PerformanceBaselineM73Tests.cs`. Runner `scripts/run-m73-tests.ps1`: pulizia `bin/obj` prima operazione; poi Release build, test mirati, regressione completa e benchmark in `artifacts/benchmarks/m73/`. La console resta testuale con log.
- Documento di riferimento: `docs/structural/m73-performance-baseline.md`. **Non introdotti sparse solver, accelerazioni hardware o solutore globale per telai plastici**.
- Ripresa: eseguire `scripts/run-m73-tests.ps1`, correggere eventuali problemi e raccogliere baseline sull'hardware dell'utente. Poi M74 adapter OpenCad2D, M75 consolidamento; assemblaggio Frame2D non lineare globale pianificato separatamente.

---

> **2026-10-23 — M72-FIX2 warning cleanup:** su base M72-FIX1, corrette sei asserzioni xUnit in quattro test M65–M68 senza toccare i solver o i runner. Nota `docs/structural/m72-fix2-xunit-assertion-warnings.md`; eseguire `scripts/run-m72-tests.ps1` sul Windows dell’utente. M71 e M72 rimangono non validati finché la suite non sarà superata.

> **23/10/2026 — M72-FIX1 CONSOLE:** la prima esecuzione M72 su Windows si è arrestata prima della build con `MSB1008` a causa dell'opzione PowerShell `-v:minimal` applicata alla funzione wrapper `dotnet` (interpretata come `-Verbose` / argomento `minimal` spurio). L'opzione abbreviata è stata eliminata dai runner M71 e M72; normalizzata anche la decodifica UTF-8 temporanea con ripristino. **I test M71/M72 NON sono ancora passati: ripetere `scripts/run-m72-tests.ps1` dal pacchetto M72-FIX1.** Il codice C# rimane invariato.

# Checkpoint M72 sperimentale — 2026-10-23 (più recente)

- **Stato di validazione:** utente conferma M70-FIX1 superato; M71-AUDIT-FIX1 NON ancora testato su Windows. M72 è sviluppato condizionatamente, NON validato in .NET 8 qui.
- Base archivio M71-AUDIT-FIX1 completa; nessun progetto/file precedente eliminato. Leggere `docs/structural/m72-first-concentrated-plastic-frame-element.md` e il precedente studio `m71-frame2d-plasticity-feasibility.md`.
- Nuovi componenti solo `internal`: `Frame2D/Nonlinear/BilinearRotationalHinge.cs` (ritorno plastico isotropo puro) e `ElastoplasticFrame2DElement.cs` (0,1,2 cerniere opzionali, iterazione sulle rotazioni interne, Schur complement 6×6, coordinate locali/globali, energie, commit/rollback). Non toccano Frame2DAnalyzer o il codice pubblico, non esiste ancora un assemblatore globale incrementale di telai.
- Test `Frame2D/M72/ElastoplasticFrame2DM72Tests.cs`; runner `scripts/run-m72-tests.ps1` pulisce sempre per primo, compila, prova M71+M72 e regressione completa. Solo C# per benchmark numerici; PowerShell per automazione e console progressiva.
- **Prima di qualunque promozione:** eseguire `run-m72-tests.ps1` su Windows .NET8, ispezionare il log e correggere fallimenti; poi discutere il gate successivo di assemblaggio globale non lineare Frame2D.

# Checkpoint M71 AUDIT-FIX1 — 2026-10-23 (più recente)

- Audit pre-test M71 completato su ZIP M71: 9 progetti, 22 runner, 82 JSON validi, JS e link README verificati; nessun file M70-FIX1 rimosso. Conferma Windows **M70-FIX1 superato**; M71 e nuovo wrapper **in attesa di test**.
- Difetto reale risolto: `stable-dotnet-console.ps1` non deve lasciare `CI`, `TERM`, `NO_COLOR`, `DOTNET_NOLOGO` impostate dopo il runner; ora applica/restaura l'ambiente in `function dotnet` con `finally`.
- `run-m71-tests.ps1` verifica exit code, ripristino dell'ambiente e creazione log prima di build/test. Non cambiano i solver FEM o i test numerici C#; nessuna nuova dipendenza Python.
- Rapporto `docs/structural/m71-audit-pretest.md`. Eseguire `scripts/run-m71-tests.ps1` in cartella nuova con .NET 8 e ispezionare l'output reale del terminale. Non avviare M72 finché M71 non supera il gate.

---

# Handoff operativo M71 — 2026-10-23

- **Validato dall'utente:** M70-FIX1 (.NET 8). **M71 implementato come studio, non ancora testato**: nessun Frame2D plastico di produzione. Nuovo documento `docs/structural/m71-frame2d-plasticity-feasibility.md`; prima proposta: cerniere rotazionali concentrate in serie con trave elastica, N–M e geometric nonlinearities esclusi nel pilota. M72 è un gate decisionale condizionale.
- Test C#/xUnit `Frame2D/M71/PlasticHingeFeasibilityM71Tests.cs` con riferimenti elastici/algebrici; non confondere con verifica di un solver M72. Nessun Python nuovo.
- **Console:** ogni `run-m*-tests.ps1` chiama PRIMA `clean-project-build.ps1`, poi dot-source `scripts/stable-dotnet-console.ps1`. Questo wrapper intercetta `dotnet`, sanifica ANSI e carriage return e trasmette il testo progressivamente con log UTF-8 in `TEMP/StructuralSolver2D-test-logs`; preserva `$LASTEXITCODE`. Verificarlo su PowerShell Windows prima di dichiararlo consolidato. Dettagli: `docs/structural/m71-console-streaming.md`.
- Eseguire da nuova cartella `scripts/run-m71-tests.ps1`: pulizia, compilazione Release, test riferimento M71, regressione completa. .NET 8 non è disponibile nell'ambiente di confezionamento.

---

# Handoff M70-FIX1 — 2026-10-23 (CHECKPOINT CORRENTE)

- L'utente ha eseguito M70 su Windows: fallisce **solo** il test `SeismicCqcM70Tests.TwoSpanModel_CqcAgreesWithIndependentMassWeightedModeContributions` con `M54_INTERNAL_MECHANISM` e segnala console PowerShell con righe sovrapposte.
- Causa: helper `TwoSpanAxial` ometteva il vincolo verticale del nodo intermedio B di due aste `Truss2D` orizzontali; il problema era una fixture meccanicamente instabile, non il metodo CQC. Ora B.Uy è vincolato, mantenendo liberi B.Ux e C.Ux. Test negativo aggiunto per garantire che l'instabilità reale venga ancora rilevata. Nessun codice di produzione modificato.
- Tutti i 21 script `run-m*-tests.ps1` hanno `dotnet build/test --tl:off`, senza cambiare la priorità della pulizia `bin/obj`. Il parametro impedisce i ridisegni dinamici MSBuild; per log opzionali è documentato `Start-Transcript`.
- Test numerici **solo C#**; non creare nuovi script Python da lanciare. Usare lo ZIP FULL M70-FIX1 estratto in cartella pulita ed eseguire `scripts/run-m70-tests.ps1`; M70 **non ancora validato** fino ai test Windows.
- Leggere `docs/structural/m70-fix1-cqc-benchmark-console.md` e roadmap. Dopo validazione continuare M71.

---

# Handoff M70 — 2026-10-23 (CHECKPOINT CORRENTE)

- M51–M69-FIX1: test automatizzati C# .NET superati secondo conferma dell’utente. QA visiva manuale viewer distinta e non dichiarata completata.
- M70: **implementato ma NON ANCORA validato Windows**. CQC opt-in nei metodi spettrali M60 con `SeismicModalCombination.Cqc` e damping uniforme esplicito; legacy SRSS invariato. `EqualDampingCqc` usa contributi firmati, rho simmetrico e test C# su coppie vicine/separate, SDOF, integrazione API/CLI e report/CSV.
- Usare `examples/dynamics/m70-seismic-cqc-cantilever.json` e `scripts/run-m70-tests.ps1`; **pulizia `bin/obj` prima di build/test**. Nessun test Python richiesto.
- Documentazione: `docs/structural/m70-cqc-modal-combination.md`, `docs/structural/roadmap.md`, README.
- Limiti: spettri pseudo-accelerazione forniti dall’utente, smorzamento uguale per ogni modo; niente normative, CQC con smorzamenti differenziati, combinazione direzioni, forze normative, certificazione. M71 è lo studio di plasticità Frame2D, non codice automatico.

---

# Handoff M69-FIX1 — 2026-10-23 (RIFERIMENTO CORRENTE)

- L’utente ha eseguito `run-m69-tests.ps1` su Windows: la fase `StructuralSolver2D.Reporting.Tests` è fallita in `InteractiveNonlinearViewerM69Tests.MaliciousLabels_AreEscapedInsideStandaloneHtml` perché il test pretendeva `&lt;/script&gt;`, mentre `System.Text.Json` produce correttamente `\u003C/script\u003E` nel dataset incorporato.
- **M69-FIX1 corregge soltanto il test**, che ora controlla JSON parsabile, round-trip di titolo e node ID, protezione contro l'iniezione di script e `textContent`. Nessuna modifica al motore, all'exporter HTML e al JS.
- Test .NET 8 **NON eseguibili nell'ambiente di preparazione**; M69 resta in attesa del nuovo test Windows e QA manuale degli HTML. Usare ZIP FULL M69-FIX1 in cartella pulita.
- Script `scripts/run-m69-tests.ps1` rimuove `bin` e `obj` prima della build; benchmark solo C#/xUnit, senza Python. Vedere `docs/structural/m69-fix1-json-escaping.md`. Dopo gate M69 avviare M70.

---

# Handoff M69 — 2026-10-23 (RIFERIMENTO CORRENTE)

- L’utente ha confermato `test passati` per **M68**. I test numerici M51–M68 risultano superati secondo le conferme Windows.
- **M69 è implementato, NON ANCORA VALIDATO con compilazione/test .NET 8**. L’esportatore `Reporting/Nonlinear/InteractiveHtmlNonlinearTruss2DViewerExporter.cs` usa i risultati pubblici M68 senza rieseguire o alterare i solver. Risorse CSS/JS embedded da `Reporting/Nonlinear/Assets`.
- Due comandi CLI opt-in `nonlinear-static-viewer <json> <html>` e `nonlinear-time-viewer <json> <html>`; file HTML standalone con slider passi, animazione, curva carico–spostamento, forze, p plastica e dissipazione cumulata. Analisi parziali esplicite, code 2 e solo stati convergenti.
- Test nuovi esclusivamente **C#/xUnit**, PowerShell solo per orchestration. `scripts/run-m69-tests.ps1`: per prima cosa invoca `clean-project-build.ps1`, poi build Release, test reporting/CLI, regressione completa, E2E export.
- Check JS `node --check` e interazione Chromium con **dataset sintetico** passati nel workspace di preparazione; non equivalgono al collaudo dell’HTML prodotto su Windows via .NET. Aprire manualmente entrambi gli HTML in Edge/Chrome e confrontare il valore degli stati con CSV M68.
- Leggere `docs/structural/m69-nonlinear-viewer.md` e roadmap. Dopo il gate M69 passare a M70 CQC/spettri, oppure rivalutare una priorità di viewer CAD M74. Le limitazioni FEM rimangono quelle M68 (nessuna plasticità Frame2D, instabilità, verifica normativa).

---

# Handoff M68 — 2026-10-23

- M51–M67: test .NET 8 confermati dall'utente. M68 implementato; **NON ANCORA TESTATO** su Windows.
- Base: ZIP M67 con 489 file, mantenuti. Public API `AnalyzeNonlinearTrussStatic/Time` usa internamente M64/M65. Input dedicato e opt-in, mai riusare il JSON lineare.
- CLI: `nonlinear-static`, `nonlinear-static-report`, `nonlinear-static-csv`, `nonlinear-time`, `nonlinear-time-report`, `nonlinear-time-csv`; fixture `docs/structural/fixtures/m68-*.json`. Report e CSV espongono esplicitamente i run parziali; codice CLI 2 se non convergente.
- Eseguire `scripts/run-m68-tests.ps1`: pulizia bin/obj come prima azione, build Release, test xUnit, regressione totale, export end-to-end. Nessun Python.
- Nota: `docs/structural/m68-public-nonlinear-api.md` e `docs/structural/roadmap.md`. M69 viewer interattivo successivo solo dopo gate M68.

---

## Checkpoint M67 — 2026-10-23 (ATTUALE)

**M66 confermato superato dall’utente.** M67 include servizio pubblico opt-in `AnalyzeTimeHistoryForces` per risultati lineari transitori: ricostruzione reazioni come `M*a+C*v+K*u-F`; forze **elastiche locali nodali** di estremità per Truss2D/Frame2D; massimo assoluto e istante campionato; residuo sulle coordinate ammissibili. Modal damping: estensione completa mass-orthogonal esplicita, effetti ai vincoli dipendenti dalla convenzione; Rayleigh full-space esatto. Anche aggregatore **interno** per forze assiali, reazioni e plasticità M65 su soli passi convergenti; niente API non lineare pubblica finché M68. CLI `time-forces`, `time-forces-report`, `time-forces-csv`; esportazioni CSV e Markdown; prove xUnit C#, incluse CLI E2E. Nessun nuovo benchmark Python. `scripts/run-m67-tests.ps1` comincia con `clean-project-build.ps1`, poi Release build, test selettivi e full regression. **M67 non ancora testato su Windows; non dichiararlo validato.** Limitazione cruciale: `K_local*u_local` non fornisce diagrammi `N(x,t),V(x,t),M(x,t)` completi sotto inerzia/smorzamento distribuiti. Dettagli: `docs/structural/m67-dynamic-forces-and-reactions.md`.

## Checkpoint M66 — 2026-10-23 (ATTUALE)

L'utente ha confermato **M65 test C# superati**. Nuovo M66 **solo validazione**, nessuna modifica a produzione: `tests/StructuralSolver2D.Analysis.Tests/Truss2D/NonlinearM66AlgebraicReference.cs` implementa riferimento scalarizzato **per rami algebrici senza Newton** e soluzione monotona due aste indipendente dall'assemblaggio; `NonlinearValidationM66Tests.cs` confronta forze, stati, energia, convergenza, invarianti e rollback. Documentazione `docs/structural/m66-nonlinear-validation-gate.md`. Script `scripts/run-m66-tests.ps1` pulisce `bin` e `obj` **prima di ogni altro intervento**, compila in Release, esegue test M63–M66 e regressione completa. Solo C#/xUnit, PowerShell per orchestrazione, nessun nuovo Python. **M66 è IN ATTESA di test .NET Windows; non rendere pubblico M68 finché non supera il gate.** README aggiornato con badge M65 validato e M66 in verifica; nessuna API/CLI/JSON nuova. Prossimo M67 post-processing dopo gate. La validazione browser pregressa rimane distinta.

---

## M65 checkpoint — 2026-10-23 (PIÙ RECENTE)

L'utente conferma che `run-m64-tests.ps1` ha superato i test C#. Implementato `Truss2D/Nonlinear/NonlinearTruss2DNewmark.cs` come prototipo interno di dinamica elasto-plastica (massa consistente, vincoli, Newton–Newmark fisico, commit/rollback, energia), con `NonlinearTruss2DNewmarkM65Tests.cs` e `scripts/run-m65-tests.ps1` (prima operazione: pulizia `bin`/`obj`, poi compilazione, test specifici e regressione). Solo benchmark C#/xUnit, **nessun nuovo Python**. README riscritto in italiano e corredato di badge GPLv3/.NET8/stato. M65 **è da testare** su Windows .NET8, dopo il quale M66 sarà il gate di validazione numerica; API/CLI/JSON pubblici invariati. Leggere `docs/structural/m65-nonlinear-truss-dynamics.md`.

---

## M64 checkpoint — 2026-10-23 (NEWEST)

L’utente conferma `run-m63-tests.ps1` superato. Implementato M64 **research-only**: `Truss2D/Nonlinear/IncrementalElastoplasticTruss2DSolver.cs`, analisi statica incrementale di reticolari 2D con forze interne + tangente M63, carico controllato, vincoli inclinati/ridondanti, Newton in sottospazio libero, singolarità e rollback dell'intero passo. Nuovo test xUnit `IncrementalElastoplasticTruss2DM64Tests.cs` con formule analitiche interamente C#. Nessun benchmark Python da eseguire per M64; PowerShell solo orchestrazione. `scripts/run-m64-tests.ps1` esegue per prima cosa `clean-project-build.ps1` (bin/obj), poi build, test specifici e regressione. API, CLI, JSON, Frame2D e analizzatori lineari non cambiati. M64 **da testare su Windows .NET 8** prima di M65. Roadmap: `docs/structural/roadmap.md` e `m64-incremental-elastoplastic-truss.md`.

---

## Ultimo checkpoint operativo — M63 Truss2D elasto-plastico ISOLATO (2026-10-23)

- **Baseline:** utente conferma `M62 automated checks PASSED`; Python opzionale SKIPPED per alias Microsoft Store, non trattarlo come benchmark Windows eseguito.
- **M63 implementato, in attesa di build/test .NET 8 Windows.** Nuovi file `Analysis/Truss2D/Nonlinear/BilinearIsotropicAxialMaterial.cs` e `ElastoplasticTruss2DElement.cs`, con stato committed/trial e commit esplicito, moduli `E,Y,H` in kN/m², `A` in m², `L0` in m, forza N in kN e tangente in kN/m. Nessun assemblatore globale, nessuna API/JSON/CLI pubblica, nessuna modifica ai sorgenti lineari.
- Test `tests/StructuralSolver2D.Analysis.Tests/Truss2D/ElastoplasticTruss2DM63Tests.cs`; riferimento analitico Python **opzionale** `scripts/m63-independent-axial-reference.py` PASS nel container. Lo script di validazione `scripts/run-m63-tests.ps1` prima ripulisce tutti i `bin/obj` mediante `scripts/clean-project-build.ps1`, poi build Release, test M63 e regressione completa.
- Leggere `docs/structural/m63-elastoplastic-truss2d-element.md`; `docs/structural/roadmap-forward-m63-m75.md` distingue stato M63 da roadmap M64–M75. **Non dichiarare M63 completato fino ai test utente.**
- **Prossimo dopo il gate M63:** M64, assemblaggio e soluzione Newton incrementale di reticolari plastiche con rollback atomico. Revisione manuale dei viewer precedenti ancora aperta.

## Aggiornamento 23 ottobre 2026 — piano successivo a M62

Il riferimento operativo per la pianificazione M63–M75 è `docs/structural/roadmap-forward-m63-m75.md`, aggiunto **senza modifiche ai sorgenti**. M62: test automatizzati C# superati su Windows secondo l'utente; benchmark Python opzionale esterno SKIPPED. Il prossimo step è M63, ancora pianificato e non implementato. La verifica browser manuale pregressa rimane aperta.

# AI Handoff - StructuralSolver2D

This file is intended to help an LLM quickly understand the StructuralSolver2D project and contribute consistently.

Read this file before proposing code changes.

---

## Latest checkpoint — M62-FIX1 Python optional reference diagnostics (2026-10-23)

- User reports `run-m62-tests.ps1` reached step 4/4 then threw `M62 independent Python benchmark failed.`; full .NET build, focused M62 xUnit tests, and full C# regression therefore completed. Python traceback was not supplied; root cause on Windows is NOT conclusively known.
- `scripts/m62-reference-piecewise-sdof.py` runs successfully on Python 3.13.5 in packaging environment, max force residual ~1.585e-12 kN. No production C# or reference formulas changed.
- FIX1 revises `scripts/run-m62-tests.ps1` to probe usable Python >=3.7 with `dataclasses`, with Windows `py -3` fallback; missing/unusable Python skips **only the optional independent runtime check** with a visible warning, while genuine reference errors remain failures and show Python output/exit code. First action remains shared `bin`/`obj` cleanup.
- Documentation `docs/structural/m62-python-benchmark-runner-fix.md`. PowerShell behavior still needs validation on user's Windows. Start from this full M62-FIX1 archive before continuing M63.

## Latest checkpoint — M62 research-only elastoplastic SDOF (2026-10-23)

- **Accepted predecessor:** user confirmed `scripts/run-m61-tests.ps1` PASSED. Central mandatory `scripts/clean-project-build.ps1` cleanup of local MSBuild `bin` and `obj` remains the first test action, including NEW `scripts/run-m62-tests.ps1`.
- **M62 pending Windows .NET 8 acceptance:** strictly internal `Dynamics/Nonlinear/BilinearIsotropicSpring.cs` and `NonlinearSdofNewmark.cs`, with xUnit tests under `Dynamics/M62`, plus separate `scripts/m62-reference-piecewise-sdof.py`. No public API, CLI, JSON schema, FEM elements, modal or linear Newmark source altered.
- Numerical scope: virgin elastic initial state, bilinear isotropic hardening, exact return mapping, Newmark average acceleration + Newton, atomic state commit on success/rollback on failure, physical energy vs discrete work residuals distinct. No adaptive substeps, nonlinear member analysis, plastic hinge, damage, collapse or seismic code compliance.
- **Full reference:** `docs/structural/m62-sdof-elastoplastic-prototype.md`; run `scripts/run-m62-tests.ps1` from extracted root for build, M62 suite and all regressions. Python analytical branch reference optional at test runtime. Do not claim compiled/passed .NET 8 without user results.
- Proposed next research gate M63 (only after M62 validation): investigate single axial elastoplastic 2D truss with proper history, coordinate transforms and benchmarks; no direct nonlinearity in Frame2D.

## One-sentence summary

StructuralSolver2D is an independent .NET 8 / C# planar structural analysis engine for simple 2D structures made of one-dimensional members, currently focused on first-order linear elastic 2D frame analysis.

---

## M61 feasibility and mandatory build cleanup — current handoff (2026-10-23)

- **User reports M60 .NET tests PASSED**. Current baseline is the M60 full ZIP. Historical pre-M60 acceptance flags below are superseded by this note.
- M61 is **feasibility only**, not a nonlinear frame solver: read `docs/structural/m61-nonlinear-feasibility.md`. Constitutive state and rollback, Newton tangent/effective stiffness, energy and independent benchmarks are specified; future development requires a separate decision. The independent standard-library `scripts/m61-check-bilinear-return-map.py` has passed scalar verification but is **not** linked to the C# solver.
- Every `scripts/run-m*-tests.ps1` (M51 through M61, including M591) MUST begin with `$ErrorActionPreference = 'Stop'` and call `& (Join-Path $PSScriptRoot 'clean-project-build.ps1')` before checking the SDK, building or testing. Future scripts must preserve this invariant. `clean-project-build.ps1 -WhatIf` previews project-local `bin`/`obj` deletions. Do not delete other directories; see `docs/structural/build-cleanup.md`.
- `scripts/run-m61-tests.ps1` runs fixture safety tests and the full .NET regression. **PowerShell and .NET execution not available in the packaging environment**; M61 acceptance remains to be confirmed by the user. No production C# code or existing model schema modified.

## M60 seismic exploratory (2026-10-23) — current handoff

- **User status:** M59.1 audit FIX1 test suite PASSED on Windows. Do not rerun or modify M59.1 casually; it is the accepted baseline. M60 is **implemented but untested with .NET 8** here.
- New `StructuralSeismicAnalysisService` plus facade `AnalyzeSeismicTime` / `AnalyzeSeismicSpectrum`: uniform global X/Y translational acceleration, nodal equivalent inertia from **consistent assembled M** (`-M r a_g`), relative M59 response, separately reported absolute translation accelerations, modal Gamma / effective masses, pseudo-spectrum T->Sa, Sa/omega² and unsigned SRSS envelopes.
- `M60` input/CLI/reporting is **non-normative**, explicitly excludes NTC/Eurocode, CQC, independent support displacements, differential earthquake motion, absolute energy accounting, member forces and safety certification. Report warns if modes are near or mass participation low.
- New files: `src/StructuralSolver2D.Analysis/Dynamics/Seismic/SeismicParticipation.cs`, `PublicApi/StructuralSeismicAnalysis*`, `src/StructuralSolver2D.Reporting/Seismic/SeismicReportExporter.cs`, M60 tests, `examples/dynamics/m60-seismic-cantilever.json`, `docs/structural/m60-seismic-exploratory.md`, `reports/m60-independent-check.py`.
- Run `scripts/run-m60-tests.ps1` in a **clean** Windows extraction to validate; includes previous-regression tests. No .NET SDK in packaging environment. The M59.1 release is superseded only as an archive baseline, not as accepted mathematics.

## M59.1 audit FIX1 (2026-10-23) — status and new diagnostics

- Previous M59 user .NET automated tests passed. M59.1 baseline has **not** yet had Windows .NET acceptance. Current audit ZIP includes a targeted quality-gate correction: an unexcited/zero-response case no longer reports positive tolerance/monotonic flags; warnings `M591_STEP_BUDGET`, `M591_FOCUS_ZERO`, `M591_PEAK_VARIATION`, `M591_PEAK_TIME_SHIFT` make limitations and peak sampling explicit.
- Independent NumPy/SciPy 2D beam model reproduced original M59 N8 Uy peak within ~7e-15 m. The 0.6 s example's observed N8 medium/fine differences are about 0.98% Uy, 1.09% Vy and **13.24% Ay**, so do NOT call transient accelerations converged. All-mode fmax≈19.7 kHz would demand >118k steps for illustrative dt*f≤0.1, above M59's 2000-step cap.
- Source: `reports/m591-full-audit-2026-10-23.md`. .NET SDK unavailable here; execute `scripts/run-m591-tests.ps1` cleanly on Windows before accepting M59.1. No production changes to M59 integrator, matrices or static core. M60 not started.

## Latest development — M59.1 time-grid convergence (2026-10-23)

- **Baseline:** M59 full ZIP, user reports all M59 automated .NET tests passed, including CLI end-to-end. Time-domain browser review and accuracy-at-refined-dt remain open.
- **Implemented, pending Windows tests:** `StructuralTimeConvergenceAnalysisService`, facade `AnalyzeTimeHistoryConvergence`. Three grids dt/dt/2/dt/4 at same duration, global per-component common-time comparisons, zero-signal diagnostic, selected peak times/amplitudes and per-mode dt*f warning. Base count ≤500 to preserve M59's 2,000-step limit. No new integrator, mass formulation, change to M59 or static code.
- CLI `time-convergence`, `time-convergence-report`, `time-convergence-csv`, report exporter, tests and script `scripts/run-m591-tests.ps1`. Use `examples/dynamics/m59-time-cantilever.json` directly (unchanged JSON contract). Documentation `docs/structural/m591-time-convergence.md`, roadmap M59.1 section.
- Relative grid comparisons are **not exact error bounds**. Numerical indicator thresholds (1% U/2% V/5% A, dt*f=0.1) are non-normative; inspect high modes, forcing discontinuities and peak timing. Do not mark M59.1 complete until user Windows run. M60 still planned.

## Latest development — M59 transient time-history (2026-10-23)

- **Baseline:** full M58 (`StructuralSolver2D_M58_HARMONIC_DAMPING_2026-10-23_FULL.zip`), user reports M58 automatic tests passed. M57-FIX2 predecessor tests were included in M58 suite. Browser visual checks still open.
- **New implementation pending Windows .NET 8 tests:** `StructuralTimeHistoryAnalysisService` and facade `AnalyzeTimeHistory` integrate the full mass-normalized modal basis using Newmark β=1/4 γ=1/2, with nodal piecewise-linear load histories, optional compatible physical initial states, explicit M58 damping, physical nodal U/V/A, sampled force and node envelopes, energy work/dissipation diagnostic.
- CLI `time`, `time-report`, `time-csv` (two files), `time-viewer`. Fixture `examples/dynamics/m59-time-cantilever.json` is modal-only, never to be fed to legacy static commands. Viewer downsampled (full CSV data retained).
- Read `docs/structural/m59-time-history.md`, then `docs/structural/roadmap.md`. Run `scripts/run-m59-tests.ps1` **from a clean folder**. No .NET SDK available here: do not label M59 tested/accepted. Manual browser inspection, dt-halving convergence and node envelope verification remain. Member force envelopes/reaction histories intentionally not implemented (not a safety-rated solver).
- M60 seismic analysis is still planned, not started; do not add seismic code assumptions based on older book provisions.

## Previous development — M58 harmonic steady-state frequency response (2026-10-23)

- **Current complete baseline:** M58 FULL ZIP built on `StructuralSolver2D_M57_FIX2_MODAL_FIXTURE_REGRESSION_2026-10-23_FULL.zip`. User cannot currently test; **M57-FIX2 Windows rerun remains pending** and **M58 is implemented but unvalidated**. Do not conflate source/ZIP integrity checks with successful .NET 8 compilation or test execution.
- Added opt-in `StructuralHarmonicAnalysisService`, `StructuralSolver2DService.AnalyzeHarmonic`, complex phasors and sinusoidal in-phase nodal force inputs, uniform modal or proportional Rayleigh damping with explicit coefficients; complete M55 modal basis, no truncation and no static path changes. Exact internal SDOF transfer detects undamped resonance.
- CLI: `harmonic`, `harmonic-report`, `harmonic-csv`, `harmonic-viewer`, using an additional top-level `harmonic` JSON block. New fixture `examples/dynamics/m58-harmonic-cantilever.json` must stay modal-only and compatible with M57-FIX2 static/modal fixture partitioning.
- Output is physical steady-state displacement/rotation magnitude and phase (not normalized eigenmode playback). HTML shows amplitude and phase curves connecting user-provided samples only, not dynamic time histories or an automatic resonance search. M59 will address transient response separately.
- Tests: SDOF frequency magnification and lag phase, damping validation, cantilever zero-frequency analytical limit, first-mode resonant peak, constrained loads, invalid frequency/models, CLI/CSV/Markdown/HTML. Start `scripts/run-m58-tests.ps1` **from a clean extracted archive**; it checks M57-FIX2 and all new tests before full regression.
- Read `docs/structural/m58-harmonic-response.md`, then `docs/structural/roadmap.md`. Do not claim M57/M58 completion until Windows tests and browser review pass; next planned work is M59 only after this gate.

## M57-FIX2: CLI example regression categorization — 2026-10-23

- **Latest Windows user report:** entire CLI suite passed 190/191 tests; the sole failure was `DistributedExamples_ShouldAnalyzeAndExportViewer` on `dynamics/m57-modal-cantilever.json`. Legacy `analyze` exited with `The JSON model must define at least one load case.` That behavior is **correct** for static commands; the fixture is intentionally modal-only.
- **Fix location:** `tests/StructuralSolver2D.Cli.Tests/CliCommandRegressionTests.cs`. `Examples()` now omits only JSON with `dynamics` and no static `loadCases`, and new `ModalOnlyExamples()` ensures the modal CLI commands can process them. There is an explicit partition test and a negative static-command regression. `scripts/run-m57-tests.ps1` adds an M57-FIX2 source gate and runs example-oriented tests before the full suite.
- **Important:** Do not change `StructuralModelJsonReader` to allow modal-only JSON in static commands; do not add a fictitious `loadCases` to the modal example; do not hide the fixture entirely. No production code changed in FIX2.
- **State:** user validated M51–M56; M57-FIX1 corrected Markdown output, and M57-FIX2 repairs erroneous CLI test enumeration. Both fixes still require full .NET 8 Windows test acceptance. Modal viewer manual browser inspection remains pending. Do not begin M58 until M57 accepted.
- **Baseline release artifact:** `StructuralSolver2D_M57_FIX2_MODAL_FIXTURE_REGRESSION_2026-10-23_FULL.zip` (complete ZIP). Work from this, not older M57/FIX1 builds.

## M57-FIX1 reporting defect and correction — 2026-10-23

- User executed M57 focused reporting tests on Windows: `ModalTables_AreInvariantAndContainProperUnits` failed (one of 3) because the Markdown exporter wrote `phi^T M phi = 1` without the API contract's `Mass:` prefix; the failure originated at `tests/StructuralSolver2D.Reporting.Tests/Modal/ModalReportingM57Tests.cs:40`.
- Corrected **only the reporting contract**: `ModalReportExporter.ExportMarkdown` now writes `output.Normalization`; it does not alter the modal solver, numeric results or static behavior. Added explicit regression test for the output property, an M57-FIX1 source guard, and a post-CLI Markdown-content check to the Windows test runner.
- **Current baseline:** this M57-FIX1 FULL archive, superseding `StructuralSolver2D_M57_API_REPORT_VIEWER_2026-10-23_FULL.zip`. Extract fresh to avoid retaining older source/binaries, then run `scripts/run-m57-tests.ps1`. **Tests are still pending** for this correction; no .NET 8 SDK in package environment. M58 is not started; browser review remains necessary after passing tests.

## Active workstream status — M57 (2026-10-23)

- **Baseline**: M56-FIX1 full archive (`StructuralSolver2D_M56_FIX1_ALIASING_2026-10-23_FULL.zip`), with user-confirmed passing .NET 8 tests after removing the older project directory. M51–M56 are user validated; no TRX logs were supplied.
- **Current release**: M57 public linear-elastic modal outputs. New `StructuralModalAnalysisService` and `StructuralSolver2DService.AnalyzeModes` call M53–M55 with M56 benchmarked constraints; no static solver or previous CLI command semantics changed.
- `StructuralModalAnalysisOutput` provides mass-normalized nodal components, periods/frequencies/residuals, Rayleigh, max modal orthogonality errors and warnings. Modal sign/amplitude arbitrary; repeated eigenspaces non-unique. All previous M53/M54 rejections remain explicit (end releases, free-free, mechanisms, massless coupled rotations, oversized dense systems).
- Four opt-in CLI commands: `modal`, `modal-report`, `modal-csv`, `modal-viewer`. The latter is **a distinct HTML modal viewer** (not an invasive change to the static viewer). The viewer samples the Euler–Bernoulli transverse deformation inside the element with Hermite shape functions, so it does not rely solely on nodal positions as in the original M56 alias failure.
- Modal-only JSON example: `examples/dynamics/m57-modal-cantilever.json`. For these modal commands only, JSON may omit static `loadCases`, but must include explicit `dynamics` metadata. Normal static reader behavior remains as before.
- **Validation pending**: `.\scripts\run-m57-tests.ps1` (checks M56-FIX1 source, runs clean .NET build, M56 benchmark, M57 tests, full regression and all modal CLI exports). The packaging environment has no .NET SDK. Static JS syntax and JSON fixture have been independently checked with Node/Python. Manual browser behavior still requires user review.
- **Next gate**: accept M57 on Windows .NET tests and viewer inspection before M58 harmonic/damping work. Do NOT claim real modal vibration amplitudes, seismic safety, or broader release/singular-mass support.


---

## Current strategic decision

The project is intentionally independent from OpenCad2D.

OpenCad2D may become a future graphical front-end or integration target, but the solver must not depend on OpenCad2D, Avalonia, CAD entities, canvas rendering, UI commands or CAD persistence.

Do not introduce OpenCad2D references into the solver projects.

Correct dependency direction for future integration:

```text
OpenCad2D or another UI
    -> adapter/client layer
        -> StructuralSolver2D.Core
        -> StructuralSolver2D.Analysis
        -> StructuralSolver2D.Reporting
```

Never invert this dependency.

---

## Project identity

The project has two purposes:

1. lightweight 2D structural analysis engine;
2. educational codebase for studying matrix structural analysis / FEM-style 2D frame analysis.

The code should therefore remain clear, explicit and easy to test. Avoid clever abstractions that obscure the structural mechanics.

---


## Latest milestone

Milestone 41 adds user-facing validation JSON files under `examples/validation/`. These files are loaded, validated and analyzed by automated tests and should be used as reusable inputs for future SVG/HTML/PNG, XLSX, PDF and viewer milestones.

Milestone 39 added a UI-independent viewer-ready result data model.

The reporting project now contains `StructuralSolver2D.Reporting.Visualization`, which prepares:

- undeformed nodes and member axes;
- scaled deformed nodal coordinates;
- nodal `Ux`, `Uy` and `Rz` values for labels/tooltips;
- deformed member polylines;
- normal-force, shear-force and bending-moment diagram polylines;
- drawing bounds;
- optional cyclic animation frames for deformed-shape playback.

This is not a GUI and must remain independent from Avalonia, WPF, HTML, SVG and OpenCad2D.

The post-M41 roadmap has been realigned. The next recommended milestone is:

```text
Milestone 46 — First simple interactive viewer prototype
```

The viewer path is still planned, but it now follows the static graphical export/report/export work instead of replacing it. See `docs/structural/development-plan.md`.

Important convention:

```text
examples/      user-facing files for learning and CLI usage
benchmarks/    validation and regression cases with expected results
docs/theory/   educational notes explaining solver assumptions and mechanics
```

Preferred example layout:

```text
examples/
  beams/
  trusses/
  mixed/
  combinations/
  validation/
```

The CLI can analyze any JSON file path, so no solver changes are required for this organization.

Existing flat `examples/*.json` files may remain temporarily for compatibility, but new examples should use the categorized layout.

## Current repository layout

```text
src/
  StructuralSolver2D.Core/
  StructuralSolver2D.Analysis/
  StructuralSolver2D.Cli/
  StructuralSolver2D.Reporting/

tests/
  StructuralSolver2D.Core.Tests/
  StructuralSolver2D.Analysis.Tests/

examples/
  README.md
  beams/
  trusses/
  mixed/
  combinations/
  validation/

benchmarks/
  beams/
  frames/
  trusses/
  mixed/
  convergence/
  expected/

docs/theory/
  matrix-method.md
  frame2d-element.md
  truss2d-element.md
  equivalent-nodal-loads.md
  local-global-coordinates.md
  sign-conventions.md
  displacement-interpolation.md
  validation-strategy.md

docs/structural/
  vision.md
  scope.md
  architecture.md
  model.md
  units.md
  analysis.md
  validation.md
  reporting.md
  csv-export.md
  public-api.md
  release-checklist.md
  release-notes-v0.1.0.md
  roadmap.md
```

The solution file is:

```text
StructuralSolver2D.sln
```

The project targets:

```text
net8.0
```

---

## Project responsibilities

### `StructuralSolver2D.Core`

Contains only structural data model and validation.

Main objects:

- `StructuralModel`
- `StructuralNode`
- `StructuralMember`
- `StructuralMaterial`
- `StructuralMaterialLibrary`
- `StructuralSection`
- `StructuralSupport`
- `StructuralLoadCase`
- `StructuralLoad`
- `StructuralModelValidator`
- validation result/issue/severity classes
- enums for member, support, load type, target type and direction

Rules:

- no solver logic;
- no matrix logic;
- no CLI logic;
- no report generation;
- no UI/CAD concepts;
- no OpenCad2D references.

### `StructuralSolver2D.Analysis`

Contains the Frame2D solver and post-processing.

Main objects:

- `Frame2DAnalyzer`
- `Frame2DElementMatrices`
- `DenseLinearSystemSolver`
- `StructuralAnalysisException`
- `StructuralAnalysisResult`
- `NodalDisplacementResult`
- `SupportReactionResult`
- `MemberEndForceResult`
- `Frame2DInternalForceSampler`
- `MemberInternalForceSample`
- `MemberInternalForceDiagram`
- `Frame2DResultSummarizer`
- `StructuralAnalysisSummary`
- extrema result classes

Rules:

- depends on `Core`;
- validates the model before analysis;
- supports only `MemberType.Frame2D` at the current stage;
- should throw controlled `StructuralAnalysisException` for unsupported or invalid analysis cases;
- should not know anything about CLI, JSON, reports or UI.

### `StructuralSolver2D.Cli`

Contains command-line usage and JSON input for examples.

Current commands:

```text
help
example <name>
analyze <input.json> [loadCaseId]
report <input.json> <output.md> [loadCaseId]
export-csv <input.json> <output-directory> [loadCaseId]
export-xlsx <input.json> <output.xlsx> [loadCaseId]
```

Rules:

- CLI can reference Core, Analysis and Reporting;
- CLI should remain thin;
- model-building examples can live here for now;
- JSON schema intentionally mirrors the current Core model to remain educational.

### `StructuralSolver2D.Reporting`

Contains reporting logic.

Current implementation:

- `MarkdownStructuralReportGenerator`
- `MarkdownReportOptions`
- `CsvStructuralResultExporter`
- `XlsxStructuralResultExporter`
- `PdfTechnicalReportExporter`
- `SvgStructuralResultExporter`
- `HtmlStructuralResultPreviewExporter`

Milestone 35 report options:

- `IncludeEducationalExplanations`
- `IncludeModelStatistics`
- `IncludeDeflectionChecks`

Rules:

- reporting can reference Core and Analysis results;
- reporting should not run the solver itself;
- reporting should receive a model, analysis result, diagrams and summary.

---

## Internal units

The solver uses fixed coherent internal units.

| Quantity | Unit |
|---|---:|
| Length | m |
| Force | kN |
| Moment | kNm |
| Elastic modulus | kN/m² |
| Area | m² |
| Second moment of area | m⁴ |
| Uniform distributed load | kN/m |

Important examples:

```text
Steel E = 210000 N/mm² = 210000000 kN/m²
Timber E = 11000 N/mm² = 11000000 kN/m²
```

Do not introduce mixed internal units.

UI/CLI/report layers may format values differently in the future, but the solver should receive internal units.

---

## Structural model assumptions

Current analysis type:

```text
First-order linear elastic static analysis of plane frame structures.
```

Current element:

```text
Frame2D
```

Degrees of freedom per node:

```text
Ux - horizontal displacement
Uy - vertical displacement
Rz - in-plane rotation
```

Degrees of freedom per member:

```text
Ux1, Uy1, Rz1, Ux2, Uy2, Rz2
```

Supported structural schemes at this stage include:

- simply supported beam;
- continuous beam, where modelled with frame members and supports;
- cantilever;
- simple portal/frame;
- axially loaded bar represented as a frame member;
- simple educational frame examples.

---

## Current load support

The Core model can represent:

- `NodalForce`
- `NodalMoment`
- `UniformDistributedLoad`
- `PointLoadOnMember, LinearDistributedLoad`
- `SelfWeight` enum support may exist depending on current code state

The current Frame2D analyzer supports:

- nodal force;
- nodal moment;
- uniform distributed load on members.

The current Frame2D analyzer does not yet support:

- self-weight generation.

Point loads on members are supported through equivalent nodal loads. Keep benchmark tests for centered and eccentric point loads when changing this area. A useful reference case is a simply supported beam with a centered point load:

```text
RA = P / 2
RB = P / 2
Mmax = P L / 4
```

---

## Current result pipeline

The intended result pipeline is:

```text
StructuralModel
    -> StructuralModelValidator
    -> Frame2DAnalyzer
    -> StructuralAnalysisResult
    -> Frame2DInternalForceSampler
    -> MemberInternalForceDiagram
    -> Frame2DResultSummarizer
    -> StructuralAnalysisSummary
    -> CLI output / Markdown report
```

Do not bypass validation in analysis code.

---

## Current CLI commands

Build/test:

```powershell
dotnet build StructuralSolver2D.sln
dotnet test StructuralSolver2D.sln
```

Help:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- help
```

Built-in example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- example simple-supported-beam
```

Analyze JSON:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\simple-supported-beam.json
```

Analyze JSON with explicit load case:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\simple-supported-beam.json LC1
```

Generate Markdown report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\beams\simple-supported-beam.json reports\simple-supported-beam.md
```

Export CSV files:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\beams\simple-supported-beam.json reports\csv\simple-supported-beam
```

---

## Validation philosophy

Validation is not optional in this project.

Before extending user-facing functionality, add tests with known analytical results.

Existing benchmark categories include:

- simply supported beam with uniform distributed load;
- simply supported beam with centered point load;
- cantilever with point load at free end;
- cantilever with uniform distributed load;
- axially loaded bar;
- no-load model;
- load-case filtering;
- symmetric portal/frame behavior;
- unstable/labile model;
- internal force sampling;
- extrema summarization.

When adding a feature, include:

1. a model-level validation test if the feature affects model validity;
2. an analysis test if the feature affects solver behavior;
3. a benchmark test with hand-computable expected results whenever possible.

---

## Current limitations

Do not claim that the project currently supports:

- real professional structural design;
- NTC or Eurocode design checks;
- steel or timber member verification;
- buckling checks;
- seismic analysis;
- dynamic/modal analysis;
- geometric nonlinearity;
- material nonlinearity;
- connection design;
- fire or fatigue design;
- 3D analysis;
- CAD integration;
- automatic conversion from CAD geometry.

The current project is an experimental and educational structural analysis engine.

---

## Coding style preferences

Follow the style already used in the project:

- C# / .NET 8;
- nullable reference types enabled;
- clear namespaces matching folders;
- small focused classes;
- explicit names over abbreviated names;
- XML documentation for public API where appropriate;
- deterministic tests;
- no hidden global state;
- no UI dependencies in Core or Analysis;
- prefer clear exceptions over silent failures.

When adding a new milestone, prefer incremental changes:

```text
small feature
+ tests
+ documentation/readme update if user-facing
```

---

## Important sign conventions

The current documentation and tests use the convention that, for a horizontal left-to-right beam:

```text
positive bending moment = sagging moment
```

A simply supported beam with a downward uniform load should produce positive bending moment in span.

A downward-loaded cantilever should produce negative bending moment at the fixed end under the current convention.

Before changing sign conventions, update tests, reports and documentation together. Do not change signs casually.

---

## Known next-step candidates

The next recommended milestone is:

```text
Milestone 46 — First simple interactive viewer prototype
```

The unified post-M39 roadmap is:

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

The static export, XLSX/PDF and section-catalog milestones are now in place. The viewer path should grow on top of reusable JSON validation files, graphical exports and professional reports.

See:

```text
docs/structural/development-plan.md
```


Milestone 44 — PDF technical report

Status: completed. Reporting now includes `PdfTechnicalReportExporter` and `PdfTechnicalReportOptions` under `StructuralSolver2D.Reporting/Pdf`. The CLI command is `export-pdf <file.json> <output.pdf> [loadCaseId|combinationId]`. The implementation writes a minimal PDF directly and intentionally avoids external PDF-generation dependencies.

Milestone 45 — Section catalog persistence

Status: completed. Core now includes `StructuralSectionCatalog` and `StructuralSectionCatalogJsonSerializer` under `StructuralSolver2D.Core/Model/Sections`. Catalogs store normal `StructuralSection` records, validate values and ids, support JSON load/save and can apply sections to a `StructuralModel`.

Next milestone: 47 — First simple interactive viewer prototype.


## Recent milestone note

Milestone 46 adds support glyphs, scaled support reactions, member-length dimensions, maximum displacement callouts and `N`/`V`/`M` maximum-value labels to the SVG/HTML static preview pipeline. These are implemented in `StructuralSolver2D.Reporting.Visualization` through additional visualization primitives and do not introduce any GUI dependency.


## Support orientation refinement

Support orientation refinement now makes rotated translational restraints mechanically active. `StructuralSupport.OrientationDegrees` is interpreted by the analysis layer as the local support-axis rotation. Restrained local `Ux` and/or local `Uy` are enforced through homogeneous constraint equations, so rotated simple supports / rollers influence the solved displacements and reactions. The SVG/HTML rendering still uses the same orientation for support glyphs. Future work can add named helpers for explicit biella/pendolo and pattino/manicotto support types.


## Recent milestone note

Milestone 47 adds a standalone interactive HTML viewer export through `InteractiveHtmlStructuralViewerExporter` and the CLI `export-viewer` command. It supports pan, zoom, reset view and layer toggles while consuming only `StructuralVisualizationModel`. It does not add GUI-framework dependencies or model-editing behavior.


## Recent visualization note

M47 extra adds deformation value labels. `StructuralVisualizationModel` now carries nodal displacement labels in addition to the maximum displacement callout. SVG/HTML exports can show real displacement values in millimetres near the deformed shape, and the interactive viewer exposes a `Displacement labels` toggle.


## Recent visualization note

The viewer/export visualization layer includes member displacement station labels at `L/4`, `L/2` and `3L/4`. They are prepared as `VisualizationMemberDisplacementLabel` records and rendered in SVG/HTML as optional member station labels. Values are derived from `MemberDisplacementDiagram` samples when available, otherwise interpolated from nodal displacements.


## JSON input documentation

The detailed input JSON guide is `docs/structural/json-input.md`. It documents the root schema, nodes, materials, sections, members, supports, oriented supports, load cases, loads, manual load combinations and inclined force decomposition. The example `examples/loads/inclined-nodal-force.json` shows a 10 kN force at -30 degrees represented as two nodal force components.


## Recent milestone note — M48

Milestone 48 adds standalone viewer controls to `InteractiveHtmlStructuralViewerExporter`: corrected deformed-shape scale, separate `N`/`V`/`M` visibility toggles, and deformation animation playback with amplitude/speed controls. The deformation scale is no longer an SVG group transform; it recomputes deformed polyline coordinates from embedded base points and displacement vectors.


## Recent viewer note

The visualization pipeline now includes applied load graphics. `StructuralVisualizationModel` carries concentrated load arrows, load moment glyphs and distributed-load glyphs. SVG/HTML exports render them in a dedicated `#loads` layer, and the interactive viewer exposes a Loads toggle.

---

## Engineering roadmap update — 2026-10-22 (planning only)

The canonical roadmap now includes a detailed Carpinteri-informed verification/dynamics workstream, **Milestones 51–61**, in `docs/structural/roadmap.md`. This is a **plan**, not completed functionality. The existing solver remains first-order linear-elastic **static**, and dynamic/modal/seismic analysis is **not implemented**. Existing viewer/integration Milestones 49–50 remain independently planned/trackable and are not renumbered.

**Recommended next engineering task:** Milestone 51 (static energy/matrix invariants and independent regression checks) before adding any modal features. Then: M52 mass data/units, M53 mass matrices, M54 dynamic constraints/zero-mass DOFs, M55 modal eigensolver, M56 analytical validation gate, M57 modal viewer and exports. Later optional M58–M60 forced/transient/seismic, M61 nonlinear feasibility.

Do not reuse static Lagrange-multiplier-augmented matrices directly as ordinary modal pairs, do not reuse static end-release condensation for mass without proof, and do not create fictitious inertia for inactive DOFs. `StructuralMaterial.UnitWeight` is weight per volume (kN/m³), not mass density. The book's historical seismic standards are **not** current design-code requirements. No code behavior, public API, tests or example files were changed by this documentation-only update.


## M51 implementation checkpoint — 2026-10-22

Started M51 static verification. Internal modules:

- `src/StructuralSolver2D.Analysis/Verification/StaticSystemVerifier.cs` — symmetry, `W=2U`, projected free residual, homogeneous constraints, finite data;
- `SupportConstraintSystem.GetHomogeneousConstraintRows` — reproduces support/inactive DOF equations for audit;
- `PlaneStructureAnalyzer.AnalyzeWithStaticVerification` and `AnalyzeCombinationWithStaticVerification` — internal opt-in verification on the **real** stiffness/load assembly, including released member equivalents;
- `tests/StructuralSolver2D.Analysis.Tests/Verification/` — analytical and regression checks;
- `docs/theory/static-energy-verification.md`, `benchmarks/verification/README.md` — derivation, tolerances, limitations, reference data.

**M51 IN PROGRESS**, not complete: .NET SDK not present in this execution environment, so tests have not run; run `dotnet test StructuralSolver2D.sln` and complete remaining gates before any claim of completion. Existing public analyzer behavior remains unchanged. Do not start dynamic mass work yet.

Run `scripts/run-m51-tests.ps1` on a Windows installation with .NET 8 SDK to compile and run targeted + full tests.

## M51 additional diagnostic checkpoint — 2026-10-22

Added internal `Verification/ConstrainedStiffnessInspector.cs`, `PlaneStructureAnalyzer.InspectStaticStability(model, loadCaseId)` and `Verification/ConstrainedStiffnessInspectorTests.cs`. It performs an optional pre-solve projected-stiffness inspection (including inclined restraints, duplicate constraints, unused rotations) with a **normalized** eigenvalue ratio and a candidate critical DOF. It can examine nominally singular models without changing `Analyze(...)`. It is NOT a modal/dynamic implementation, certified stability test or physical condition-number estimator; it intentionally handles no more than 60 free DOFs.

M51 remains **IN PROGRESS**: the .NET SDK is not installed in the authoring environment, so all new and previous M51 .NET tests must be run on a machine with .NET 8. Run `scripts/run-m51-tests.ps1`, then review benchmark/report/browser results before planning M52.

Detailed validation handoff: `docs/structural/m51-verification-status.md`.

### M88-C — checkpoint 2026-10-24

Aggiunto catalogo proprietà caratteristiche materiali in `StructuralSolver2D.Catalogs` con JSON, validazione e test. Eseguire `scripts/verify-m88c-manifest.ps1`, `scripts/run-m88c-tests.ps1`. Non considerare M88-C validato fino a esecuzione di .NET 8 su Windows. Prossimo: M89 resistenze sezioni acciaio con norma/edizione/annesso espliciti. Motore FEM non modificato.
