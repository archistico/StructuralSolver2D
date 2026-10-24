> **24/10/2026 — Nuovo stato operativo:** M80-FIX3 ha superato la suite Windows; M75–M80 sono convalidati nei test automatici. M81 introduce un elemento isolato con matrice geometrica/beam-column sotto precarico costante e test indipendenti C#, senza toccare il solver globale. Test M81 sul Windows ancora pendenti. La stabilità globale P–Δ/eigenbuckling resta M82. Vedere [`m81-frame2d-initial-stress.md`](m81-frame2d-initial-stress.md).

> **Checkpoint operativo M80 (23/10/2026):** implementate proprietà geometriche sezionali e catalogo parametrico opzionale in mm. Test Windows M75–M80 pendenti; M73 ultimo gate confermato. Il percorso corrente, prima calcolo, poi normativa, infine Avalonia, è [Visione e roadmap 2D](vision-and-roadmap-2d.md). Segue M81 secondo ordine nel piano. [Nota M80](m80-section-geometric-properties.md).

> **Checkpoint M79 — 23/10/2026:** inviluppi globali esatti Frame2D N/V/M su risultati di casi e combinazioni, con governanti e report; test .NET pendenti, M73 ultimo validato. Fase A continua con M80 proprietà di sezione. [Dettagli](m79-exact-static-force-envelopes.md).

> **Checkpoint M77 (23/10/2026):** appoggi elastici e cedimenti per statica lineare implementati, ma M75–M77 ancora senza test Windows .NET 8. Per lo sviluppo corrente prevale la [roadmap di prodotto](vision-and-roadmap-2d.md) e la [nota M77](m77-elastic-supports-and-settlements.md). M73 è l’ultimo milestone confermato nei test.

> **Aggiornamento di priorità — 23/10/2026.** Per l'obiettivo FEM 2D acciaio/legno, seguire la [roadmap canonica M74–M107](vision-and-roadmap-2d.md). I piani più vecchi su OpenCad2D sono archivio storico. M73 testato; M75 contratto JSON/preflight implementato, test Windows ancora da eseguire, dettagli [qui](m75-json-model-contract.md). L'ordine vincolante rimane calcolo → normative → UI Avalonia.

> **DOCUMENTO STORICO (23/10/2026).** Dopo la validazione M73, la visione di prodotto è cambiata: FEM 2D open source per acciaio e legno; **prima completamento motore FEM, poi verifiche NTC/Eurocodici SLU/SLE, e solo infine editor C#/Avalonia**. La vecchia integrazione OpenCad2D M74 non è più un prerequisito. Per prossimi milestone, copertura e limiti vedere **[Visione e roadmap 2D](vision-and-roadmap-2d.md)**. Il contenuto seguente è conservato a fini di tracciabilità, non come pianificazione vincolante.

> **Aggiornamento 23/10/2026:** validazione M71/M72-FIX2 confermata dall’utente. M73 benchmark e profilazione numerica C# implementati e in attesa dei test .NET sul suo PC. Vedere [m73-performance-baseline.md](m73-performance-baseline.md). Lo sviluppo del solver globale Frame2D elasto-plastico resta separato dalla pianificazione M74–M75.

> **23/10/2026 — M72 PROTOTIPO INTERNO, IN ATTESA DEI TEST WINDOWS.** M70-FIX1 validato dall’utente. M71-AUDIT-FIX1 non ancora testato su Windows; M72 prosegue come *branch sperimentale condizionale*, senza promuovere M71 a validato. Implementati legge momento–rotazione a incrudimento isotropo, cerniere agli estremi in serie con trave elastica Euler–Bernoulli, Newton locale, tangente condensata, forze locali/globali, energia e trial/commit. Benchmark C# indipendenti e `run-m72-tests.ps1` (pulizia `bin/obj`, build, M71+M72, regressione). Nessuna modifica al solver elastico, alle API/JSON/CLI o all’assemblaggio globale. [Nota tecnica M72](m72-first-concentrated-plastic-frame-element.md).

> **Stato corrente 23/10/2026 — M71 AUDIT-FIX1:** M51–M70-FIX1 hanno superato i test .NET confermati dall'utente. M71 è uno studio Frame2D con riferimenti C# e audit statico completato; il test Windows della suite e del nuovo wrapper PowerShell è ancora aperto. Nessun elemento Frame2D plastico di produzione è stato rilasciato. Il piano storico sottostante va letto con riferimento a questo stato; dettagli in [`m71-audit-pretest.md`](m71-audit-pretest.md).

## Stato operativo 23/10/2026 — M71 (ultimo aggiornamento)

- M70-FIX1: test C# confermati superati dall’utente.
- M71: documento di scelta architetturale fra cerniere concentrate e plasticità distribuita; soluzione proposta con cerniere rotazionali di estremità in serie a Frame2D elastico. Nessun elemento plastico di produzione.
- C# xUnit: riferimenti analitici sul collegamento trave–molla e sul return mapping di cerniera; test e regressione da eseguire su .NET 8 Windows.
- Runner: dopo la pulizia obbligatoria, tutti gli script caricano `stable-dotnet-console.ps1` per streaming lineare e log temporanei.
- M72: subordinato alla verifica dei benchmark e all'approvazione delle ipotesi del documento M71; non confondere cerniere plastiche e release elastiche.

> **Aggiornamento 23/10/2026:** M67 test confermati. M68 API/JSON/CLI/report non lineari implementato, in attesa di test .NET; M69 viewer + QA browser reale segue il gate M68. Leggere `m68-public-nonlinear-api.md`.

## Checkpoint M67 (2026-10-23)

M66 validato dall’utente. M67 post-processing lineare per reazioni e azioni elastiche nodali di estremità (NON diagrammi sezionali dinamici completi), aggregazione interna M65 e CLI/report CSV/Markdown. xUnit C#, runner pulizia `bin/obj` → Release build → focused tests → full regression. Test Windows M67 ancora da eseguire. Dopo la convalida: M68 API opt-in per reticolari non lineari, seguito da M69 viewer/QA.

## Checkpoint M66 — 23/10/2026 (PIÙ RECENTE)

M65 ha superato i test confermati dall'utente. Preparato il gate di validazione **M66** con riferimenti algebrici indipendenti soltanto C#, suite xUnit, e `scripts/run-m66-tests.ps1` che elimina per prima cosa `bin`/`obj`, compila e ripete l'intera regressione. Nessuna modifica ai solver di produzione o agli output pubblici. In attesa della validazione .NET Windows: M67 dopo M66, M68 pubblico bloccato dal gate. Vedere [`m66-nonlinear-validation-gate.md`](m66-nonlinear-validation-gate.md).

> **Checkpoint M65 (23/10/2026):** M64 validato dall'utente; M65 prototipo di dinamica non lineare per sole reticolari implementato, in attesa di test .NET8. Benchmark C# soltanto, pulizia `bin/obj` prima di ogni runner. README aggiornato con badge e direzione di progetto. Gate successivo M66; consultare [roadmap M63–M75](roadmap-forward-m63-m75.md) e [nota M65](m65-nonlinear-truss-dynamics.md).

> **M64 (2026-10-23):** con M63 validato dall'utente, avviato e implementato il risolutore **interno** per reticolari Truss2D elasto-plastiche con carico incrementale e Newton, vincoli direzionali, rollback e benchmark analitici xUnit C#. Build e suite .NET Windows restano da eseguire. **Da M64 non produrre nuovi benchmark Python da eseguire:** utilizzare esclusivamente C# per il calcolo dei riferimenti. PowerShell resta l'orchestratore di pulizia `bin`/`obj`, build e test. Prossima tappa M65 dopo superamento del gate M64.

## Aggiornamento M63 — 2026-10-23

Nel piano futuro M63–M75, **M63 è stato implementato come elemento Truss2D plastico isolato `internal`** con fixture xUnit e riferimento indipendente, **non ancora validato mediante test Windows**. Dopo il gate `scripts/run-m63-tests.ps1` il prossimo lavoro sarà **M64: assemblaggio globale e Newton statico incrementale**. L'analisi statica lineare e la CLI restano invariate. Riferimento: `m63-elastoplastic-truss2d-element.md`.


## Piano successivo a M62 — 2026-10-23

Per M63–M75 utilizzare il piano operativo aggiornato `docs/structural/roadmap-forward-m63-m75.md` e i gate di `docs/structural/roadmap.md`. Il lavoro originale M49–M50 rimane tracciato separatamente. Questa modifica è solo documentale.

> **Dynamic workstream status, updated 2026-10-23:** M58 user-confirmed automatic tests passed, including M57; M59 transient full-spectrum Newmark implemented and awaiting Windows .NET tests. Detailed M59 rules in `m59-time-history.md` and acceptance in `roadmap.md`. Manual viewer inspections remain.

> **Historical status before M58 acceptance, 2026-10-23:** M51–M56 (including M56-FIX1)
> validated by user Windows test reports after clean extraction. M57 public modal
> API/CLI/reports/standalone viewer implemented, awaiting Windows .NET 8 and browser
> acceptance; see `roadmap.md` and `modal-analysis.md`. M58 harmonic steady-state response implemented experimentally at user request, with M57-FIX2 and M58 acceptance still pending; M59+ planned.

# StructuralSolver2D Development Plan After Milestone 39

This document keeps the post-Milestone-39 roadmap aligned.

Milestone 39 introduced a renderer-independent visualization data model. During planning, two future-roadmap threads temporarily overlapped:

1. engineering/product features already planned before the viewer discussion;
2. graphical viewer features derived from the new visualization model.

The unified roadmap below keeps both threads and renumbers them consistently.

---

## Completed baseline

The current completed baseline is:

```text
31 - Improved internal-force diagrams and characteristic points
32 - Preliminary SLE deflection checks
33 - Parametric sections
34 - Initial material library
35 - Advanced educational Markdown reports
36 - CSV export
37 - Public API stabilization
38 - First technical release
39 - Viewer-ready result data model and animation frames
40 - Parametric model generators
41 - Validation example files in JSON
```

Milestone 39 is deliberately not a GUI. It creates the stable data boundary that future SVG, PNG, HTML, Avalonia, WPF or OpenCad2D viewers can consume.

---

## Completed Milestones 40-41 and next milestones

### Milestone 40 — Parametric model generators

Status: completed. The Core project now contains parametric generators for beams, frames and trusses.

The generator layer returns normal `StructuralModel` instances and remains independent from analysis, reporting, CLI and UI projects.

---

### Milestone 41 — Validation example files in JSON

Status: completed. The first validation JSON files live under `examples/validation/` and are covered by automated analysis smoke tests.

The initial files cover rigid-joint frames, small bridge trusses, isostatic trusses, Nielsen/parabolic schemes, inverted parabolic schemes, double-diagonal hyperstatic trusses and Gerber beams with asymmetric loads.

---

### Milestone 42 — Static graphical result export: SVG and HTML

Status: completed. The exporter produces vector-first static previews through SVG and HTML. PNG export is intentionally deferred.

Scope:

- undeformed model;
- scaled deformed shape;
- N/V/M diagrams;
- configurable deformation scale;
- configurable diagram scales;
- simple HTML wrapper around SVG.

This milestone is still not an interactive viewer. It is usable from CLI and tests.

---

### Milestone 43 — XLSX report export

Status: implemented.

The first workbook export creates spreadsheet reports richer than CSV without changing solver behavior.

Scope:

- dependency-free XLSX package writer;
- summary sheet;
- nodal displacement sheet;
- support reaction sheet;
- member end-force sheet;
- internal-force sample sheet;
- displacement sample sheet;
- CLI command `export-xlsx`.

### Milestone 44 — PDF technical report

Goal: generate a professional technical report suitable for review and attachment to a calculation note.

Expected contents:

- cover/header metadata;
- assumptions and units;
- model input tables;
- analysis summary;
- reactions/displacements/member forces;
- characteristic points;
- preliminary serviceability checks;
- static figures from Milestone 42 where available.

The wording must remain clear that the current solver performs first-order linear elastic analysis and does not provide complete code-compliant design verification.

---

### Milestone 45 — Section catalog persistence

Status: completed.

Implemented:

- reusable `StructuralSectionCatalog`;
- JSON catalog format with schema version;
- load/save helpers;
- validation of section values and ids;
- duplicate id detection;
- apply-to-model workflow;
- tests and documentation.

Future extensions remain possible for steel/timber profile libraries and GUI editing.

---

### Milestone 46 — Advanced static structural annotations and support visualization

Status: completed.

Implemented:

- support symbols for simple support, hinge and fixed support in SVG/HTML exports;
- scaled support reaction arrows and reaction moment glyphs with value labels;
- member-length dimensions on the undeformed model;
- maximum displacement callout on the deformed shape;
- maximum value labels on `N`, `V` and `M` diagrams;
- visualization-layer data objects for supports, reactions and annotations.

This milestone intentionally stays inside the reporting/visualization layer so that a future viewer can reuse the same prepared data.

---

### Milestone 47 — First simple interactive viewer prototype

Status: completed.

Implemented as a standalone HTML export generated from `StructuralVisualizationModel`.

Features:

- mouse wheel zoom;
- pointer drag pan;
- reset view;
- layer toggles for undeformed model, deformed shape, diagrams, supports, reactions and dimensions;
- summary values for the current visualization model.

The viewer remains a reporting/export feature and does not introduce solver or GUI-framework coupling.

---

### Milestone 48 — Viewer controls for scales and animation playback

Goal: add controls to the viewer.

Controls:

- corrected deformation scale based on displacement vectors;
- show/hide undeformed model;
- show/hide deformed shape;
- separate `N`, `V` and `M` visibility toggles;
- play/pause animation;
- animation amplitude;
- animation speed.

---

### Milestone 49 — Labels and tooltips for nodal and diagram values

Goal: make the viewer useful for inspection, not only visualization.

Expected labels/tooltips:

- node id;
- member id;
- nodal `Ux`, `Uy`, `Rz`;
- support reactions;
- diagram value at sampled points;
- max/min diagram values;
- characteristic points where available.

---

### Milestone 50 — OpenCad2D integration boundary study

Goal: define the integration boundary without coupling the solver to OpenCad2D.

The correct dependency direction remains:

```text
OpenCad2D or another UI
    -> adapter/client layer
        -> StructuralSolver2D.Core
        -> StructuralSolver2D.Analysis
        -> StructuralSolver2D.Reporting
```

The solver projects must not reference OpenCad2D, Avalonia UI controls, WPF UI classes or CAD persistence types.

---

## Strategic note

The viewer path is important, but the project should not skip generators, JSON validation examples and professional exports. Those features make the solver easier to validate and easier to compare with existing structural tools before investing heavily in interaction design.


### Milestone 44 — PDF technical report

Status: completed. The Reporting project now includes a lightweight dependency-free PDF exporter for technical computation reports.

Scope implemented:

- model overview;
- assumptions and limitations;
- executive summary;
- nodal displacements;
- support reactions;
- member end forces;
- sampled internal-force rows;
- sampled displacement rows;
- CLI command `export-pdf`.

The next milestone is Milestone 48 — Viewer controls for scales and animation playback.


### Support orientation refinement

Added mechanically oriented translational support constraints for `StructuralSupport.OrientationDegrees`.

The analysis layer now enforces restrained local support directions through homogeneous constraint equations. This makes rotated simple supports / rollers structurally active, not only graphical. Axis-aligned support behavior remains unchanged.

---

## Reference to subsequent engineering roadmap (2026-10-22)

This post-M39 planning document retains the historical viewer/export sequence (through M50). The later **proposed** static-verification and structural-dynamics workstream, M51–M61, is documented in `docs/structural/roadmap.md` under **Engineering roadmap extension — Carpinteri-based verification and structural dynamics**.

**Status:** planned only. M49–M50 remain separately trackable; the recommended next engineering activity is M51 static numerical verification. This cross-reference does not mark unimplemented features as completed.


### M51 development checkpoint (2026-10-22)

The initial optional code and tests for static FEM energy/invariant verification are in place. See `docs/theory/static-energy-verification.md`. **M51 is in progress, not accepted:** pending .NET 8 tests, additional multi-scale cases and near-mechanism diagnostics. M52 remains planned.

### M51 numerical diagnostics follow-up — 2026-10-22

The optional pre-solve constrained-stiffness inspector and associated regression tests have been added. It checks projected stiffness and a diagonal-scaled eigenvalue ratio, with explicit statuses for possible mechanisms, poor numerical conditioning, unsupported size and invalid matrices. Existing public solver behavior and dynamics scope are unchanged. **Not validated** until the .NET 8 regression gate is run; M52 is still planned. See `docs/theory/static-energy-verification.md` §7.

---

## Independent engineering verification and dynamics extension (2026-10-22)

The main engineering milestones 51–61, derived from a review of Carpinteri's
*Dinamica delle strutture*, are documented in the authoritative
[`roadmap.md`](roadmap.md). They do not replace the existing viewer M49–M50 path.
M51 has been accepted on the user's report of passing tests. M52 dynamic mass and
unit contracts are coded but require .NET 8 acceptance; `scripts/run-m52-tests.ps1`
is the next verification gate. M53 builds the actual element mass matrices and
assembly. No modal or seismic calculation is currently available.


### 2026-10-22 — engineering dynamics M53 checkpoint

The Carpinteri engineering track is detailed in `docs/structural/roadmap.md`.
M51/M52 are user-validated. M53 consistent truss/frame mass matrices and an
internal unconstrained global K/M assembly are implemented, **pending .NET 8
acceptance** (`scripts/run-m53-tests.ps1`). M54 is the next step after testing:
constraint projection, massless DOFs and rigid-body modes. This does not replace
or complete the independent interactive-viewer milestones M49–M50.

### 2026-10-23 — engineering dynamics M55 checkpoint

M54 dynamic constraint/reduction tests were confirmed passing by the user. The
M55 internal generalized symmetric eigensolver is implemented with guarded
Cholesky plus Jacobi, explicit mode/error diagnostics, analytical two-story
shear example and assembled-frame integration tests. **Do not expose it through
public API/CLI/reporting yet**: run `scripts/run-m55-tests.ps1` first, then
complete the independent M56 benchmark/convergence gate. See
`docs/theory/modal-analysis.md` for the derivation and limitations.


## Stato aggiuntivo — M61 e pulizia automatica (2026-10-23)

M60 ha superato i test .NET Windows comunicati dall'utente. M61 produce la sola nota di fattibilità non lineare (`m61-nonlinear-feasibility.md`), con un piccolo modello costitutivo uniaxiale isolato per convalidare ipotesi matematiche; nessuna API non lineare è stata aggiunta. Per la manutenzione, tutti i runner PowerShell richiamano ora `clean-project-build.ps1` per eliminare `bin` e `obj` dei progetti prima della build. Verificare la compatibilità su Windows con `scripts/run-m61-tests.ps1`. Il contenuto normativo e il calcolo strutturale restano fuori da M61.
