## 24/10/2026 — M86 RC1: source preview e gate di rilascio (test Windows da eseguire)

- M85-C confermato su Windows: build 0 errori/avvisi, 8 nuovi test, regressione completa 1.334 test superati.
- Nuovo runner `scripts/run-m86-release-gate.ps1` con compilazione `-warnaserror`, suite completa, smoke CLI di Frame2D/Truss2D/combinazioni e output Markdown/CSV/XLSX/PDF.
- Manifest SHA-256 verificabile e documenti di perimetro, limiti, esiti ed accettazione. Nessuna modifica ai sorgenti `src/` o ai solutori.
- Nessuna release stabile proclamata: il nuovo runner è da testare su Windows; versione assembly 0.1.0 invariata.

## 24/10/2026 — M85-C: verifica finale interna del Core statico (test Windows pendenti)

- Log M85-B FIX1 conferma 0 avvisi, 0 errori, tutti i 1.326 test di regressione, 5 casi integrati e 8 benchmark M83 a 20 ripetizioni riusciti.
- Nuovi 8 casi xUnit M85-C: reticolare triangolare con formula di lavoro virtuale e aree diverse, trave continua a due campate (reazioni e rotazioni analitiche), un esempio numerico esterno pubblicato (4 m, 3 kN/m), scaling di E, permutazione dell'ordine d'ingresso e traslazione rigida delle coordinate.
- Nuovo runner `scripts/run-m85c-tests.ps1`, matrice formule e checklist di accettazione M86. Nessun file `src/` modificato.
- Questo checkpoint **non** costituisce certificazione normativa, validazione intersoftware esterna o release M86; il gate Windows M85-C deve ancora essere eseguito.

## 24/10/2026 — M85-B-FIX1: runner benchmark e warning xUnit (benchmark Windows da ripetere)

- Risultati Windows confermati dal log: build Release riuscita, 5 test end-to-end M85-B superati, 18 controlli mirati e **1.326/1.326** test dell'intera regressione superati.
- Corretto `run-m85b-tests.ps1 -WithBenchmark`: `--repetitions 25` era fuori dal range **1..20** documentato e imposto dal runner M73/M83; usa ora `--repetitions 20` senza modificare lo strumento di profilazione.
- Corretto unicamente il warning `xUnit2012` nel test M82 (`Assert.Contains` invece di `Assert.True(...Any(...))`), con semantica invariata.
- **Nessuna modifica a `src/`**, ai solver, ai carichi, ai risultati o alle API. Il benchmark opzionale va rieseguito su Windows per chiudere il relativo gate.

## 24/10/2026 — M85-B: validazione integrata API/CLI/export (gate Windows pendente)

- L'utente conferma M85-A superato su Windows .NET 8.
- Valori reali M83-A ricevuti e archiviati, senza modifiche, in `benchmarks/m83/measurements/2026-10-24-windows-x64/`: 8 scenari, 3 ripetizioni, runtime .NET 8.0.29, Windows 10 x64.
- M83-B sparse rinviato: a 64 aste e 192 gdl liberi il primo ordine ha mediana 1,644 ms e 1,59 MB allocati; tempi irregolari richiedono più ripetizioni e modelli grandi.
- Nuovo xUnit end-to-end `IntegratedResultsM85BTests`: soluzione chiusa mensola, trave biappoggiata con G1/ULS1/SLS1, combinazione a coefficiente negativo, concordanza di tutti i sei CSV CLI con la API, valori in Markdown e celle reali XLSX, smoke test PDF.
- Runner cumulativo `scripts/run-m85b-tests.ps1` con benchmark diagnostico opt-in `-WithBenchmark` (25 ripetizioni, risultati in cartella separata), documento `docs/M85B_VALIDAZIONE_INTEGRATA.md`.
- Nessuna modifica ai `src/` di produzione o agli algoritmi FEM; compilazione e regressione M85-B da confermare su Windows .NET 8.

## 24/10/2026 — M85-A: validazione statica indipendente (gate Windows pendente)

- L'utente conferma M83-A superato su Windows. I valori CSV di scalabilità non sono stati allegati e non autorizzano ancora la sostituzione del solver denso.
- Quattro nuove famiglie di test C#/xUnit: mensola inclinata N/V/M con soluzione chiusa e reazioni; 2 barre truss in serie con A diverse; telaio iperstatico con reciprocità Maxwell–Betti e sovrapposizione dei risultati per combinazioni con fattore negativo; covarianza per rotazione di 90° in 4 scenari.
- Runner completo `scripts/run-m85-tests.ps1`: riparazione sorgente obsoleto M82, pulizia, build Release, gate mirato M85/M82/M83/energia, regressione Core/Analysis/Reporting/CLI. Il gate .NET 8 non è eseguibile nell'ambiente di pacchettizzazione.
- Nuovo documento con matrice di verifica, formule indipendenti, unità, tolleranze, limiti e lavoro M85-B/M86. Codice `src/`, algoritmi FEM, API e CLI invariati.
- [M85-A — Matrice di validazione](docs/structural/m85a-independent-validation.md).

## 24/10/2026 — M83-A: benchmark di scalabilità Frame2D (test Windows pendenti)

- Test M82 superati sul Windows dell'utente dopo riparazione del sorgente CS0246.
- Nuova suite di profilo C# `M83ScalabilitySuite`: tempi e allocazioni end-to-end per Frame2D statico e M82 linearizzato, con modelli a più elementi e controlli numerici indipendenti.
- Tool prestazioni M73 esteso con flag opt-in `--m83`, senza alterare il comportamento storico M73; output CSV/JSON separati.
- Test xUnit M83 e runner `scripts/run-m83-tests.ps1`: build pulita, gate mirati M73/M82/M83, regressione quattro assembly, esportazione benchmark.
- Nessun solutore sparse/banded aggiunto, nessun algoritmo FEM modificato; decisione M83-B subordinata ai dati Windows.
- [Specifiche e gate](docs/structural/m83a-frame2d-scalability-decision.md).

## 2026-10-24 — M82: Global linearized Frame2D P–Δ (Windows gate pending)

- M81 confirmed PASS by user. Added opt-in `Frame2DGlobalSecondOrderM82` and service facade. Assembles global elastic and frozen first-order axial geometric stiffness for unreleased Frame2D members, a single nodal-action case, rigid axis-aligned supports; unsupported configurations rejected with codes.
- Returns tangent displacements/reactions, first-order state, reference axial force list, bounded positive-definiteness critical multiplier. No nonlinear equilibrium iteration, mode eigenvectors, second-order N/V/M diagrams, code checks or UI.
- Adds C#/xUnit single/two-element column, Euler convergence, instability, portal, tension, zero-prestress, scope and reproducibility tests; `scripts/run-m82-tests.ps1` begins with bin/obj cleanup and runs focused + full regression. Documentation and roadmap synchronized.
- M82 has NOT been compiled/tested in the packaging environment; Windows .NET 8 gate pending.

## M81 — 24/10/2026 — Rigidezza geometrica Frame2D (test .NET pendenti)

- L'utente conferma: M80-FIX3 e l'intera regressione Windows sono PASSATI. M75–M80 diventano validati nel perimetro di test; l'ultimo gate è M80.
- Aggiunto il componente **interno** `Frame2DInitialStressM81`: rigidezza geometrica consistente Hermite 6×6 di asta prismatico Euler–Bernoulli, `Kt=Ke+Kg`, conversione locale/globale, trazione N positiva, compressione negativa.
- Dimostratore isolato di cantilever precompresso con rigidezza flessionale tangente 2×2, diagnostica `M81_LINEARIZED_INSTABILITY` vicino al carico critico.
- Aggiunti benchmark C#/xUnit con integrazione Gauss indipendente, chiusure Euler discrete, analisi energetica e rotazione, oltre a test errori.
- `scripts/run-m81-tests.ps1`: pulizia `bin/obj` in apertura, build Release, gate M81, regressione separata Core/Analysis/Reporting/CLI. Nessun Python e nessun cambiamento alle API/CLI/JSON o ai solver 2D esistenti.
- Il secondo ordine **globale** e l'eigenbuckling sono ancora **M82**. M81 non è una verifica NTC/Eurocodici. **Gate .NET 8 M81 non eseguito in confezionamento; test Windows ancora pendenti.**

## M80-FIX3 — 24/10/2026 — Pareggio del minimo di taglio in M78 (test .NET pendenti)

- Log Windows M80-FIX2: build Release OK (0 warning / 0 error), gate M80 geometria 17/17, regressione cataloghi 25/25, gate M78/M79 13/14 (1 fallito). La regressione completa non è stata eseguita dopo il gate fallito.
- Corretto `InteriorPointLoad_OneSidedShearAndExactMomentPeakArePreserved`: senza carico distribuito `Vmax=+9 kN` per `x∈[0,2)` e `Vmin=−3 kN` per `x∈(2,8]`; il finder sceglie, per ordine dei candidati, `x=0 / Continuous` e `x=8 / Continuous` come rappresentanti delle due parità.
- Rafforzato il test verificando direttamente i due valori del salto `+9/−3 kN` a `x=2 m` e la continuità del momento `18 kNm`. Il benchmark M78 con estremi univoci sui due lati resta invariato.
- Nessuna modifica a `src/`, alle API, al wrapper PowerShell o ai metodi FEM. `run-m80-tests.ps1` resta lo stesso; `scripts/clean-project-build.ps1` è la prima azione. **Gate completo Windows ancora aperto.**

## M80-FIX2 — 24/10/2026 — Due regressioni M78/M79 e runner compatto (test .NET pendenti)

- Test M78: rettificata l'aspettativa della parità del taglio massimo su `[0,2)`; aggiunto benchmark C# con carico distribuito crescente e salto che rende i due estremi univoci a `x=2 m` (BeforePointLoad/AfterPointLoad).
- Test M79: rimossa dalla combinazione `REV` la voce esplicita `Q × 0`, non ammessa dal validatore storico; resta `G × −1`, senza alterare le sollecitazioni attese.
- `run-m80-tests.ps1`: nuovo gate mirato M78/M79 prima della regressione per progetto e output meno prolisso (`console;verbosity=minimal`) con log persistente.
- **Zero modifiche a `src/`**; stato: `Analysis.Tests` precedente 539/541 riusciti e 2 falliti, nuovo gate non ancora eseguito su Windows. Nessun cambiamento normativo.

## 2026-10-24 — M80-FIX1: diagnosi della regressione PowerShell (test Windows pendenti)

- `run-m80-tests.ps1` ora esegue Core, Analysis, Reporting e CLI uno alla volta nella regressione completa; stampa chiaramente per ciascuno `PASSED` o `FAILED` e il relativo exit code.
- Evita di scambiare il riepilogo positivo del solo `Cli.Tests` per il completamento con successo dell’intera soluzione. Conserva pulizia iniziale `bin/obj`, build Release, test M80 mirati e log comune.
- Nessuna modifica ai sorgenti FEM, alle asserzioni C#, alle formule o al wrapper dotnet. Risultato dei test di M80-FIX1 da verificare su Windows.

## 2026-10-23 — M80: proprietà geometriche sezionali (test Windows pendenti)

- `SectionGeometryFactoryM80` calcola proprietà indipendenti per rettangolo, legno rettangolare, cerchio pieno/cavo, RHS idealizzato e doppio T simmetrico idealizzato: A, I nel piano/fuori piano, W elastici e raggi d’inerzia; input in metri.
- `SectionProfileCatalogM80`: catalogo dimensionale opt-in in millimetri con stato/provenienza espliciti, JSON separato e conversione a `StructuralSectionCatalog` standard, mantenendo retrocompatibilità.
- Nuovo esempio di sole geometrie dimostrative, test C#/xUnit (formule chiuse, scomposizione indipendente, pareti sottili, errori, round-trip), runner cumulativo `run-m80-tests.ps1` con pulizia iniziale `bin`/`obj`.
- Nessun algoritmo FEM o verifica normativa introdotto. **M73 è l’ultimo milestone validato dall’utente; M75–M80 attendono test .NET 8.**

## 2026-10-23 — M79 (implementato, test Windows pendenti)

- Inviluppi globali di estremi analitici N/V/M Frame2D tra casi/combinazioni statiche, identificazione scenario governante e ascissa; API opt-in e report CSV/Markdown.
- Test C#/xUnit e runner di regressione con pulizia iniziale bin/obj. Niente modifiche al solver, nessuna normativa e nessuna GUI. M73 ultimo gate testato, M75–M79 pendenti.

## M78 — 23 ottobre 2026 — estremi analitici di N/V/M (test Windows pendenti)

- Profilo di forze sezionali Frame2D condiviso tra campionamento e nuovo finder M78, nessuna modifica al solver FEM.
- Minimo, massimo e massimo assoluto per N/V/M con posizione, caso/combinazione e lato del salto dei carichi concentrati.
- Radici analitiche su tratti lisci: stazionarietà N/V e V=0 per M; supporta carichi uniformi, triangolari, concentrati, peso proprio e combinazioni negative.
- Nuovi test C#/xUnit, accesso pubblico opt-in e runner `run-m78-tests.ps1` (pulizia prima della compilazione).
- Nessuna verifica normativa. M73 ultimo gate confermato; M75, M76, M77, M78 ancora da validare sul Windows dell'utente.
- [Nota tecnica](docs/structural/m78-exact-frame-force-extrema.md).

## M77 — Appoggi elastici e cedimenti vincolari (23 ottobre 2026; test Windows pendenti)

- Supporti elastici nodali in assi orientati, molle lineari Ux/Uy/Rz con rigidezza e reazioni esplicite.
- Nuova azione `SupportSettlement` per caso di carico (assi locali) e RHS non omogeneo per il sistema di vincoli; combinazioni fattorizzate, verifiche di compatibilità e degli input.
- Contratto JSON reader/writer, preflight e rifiuto esplicito degli appoggi M77 nella validazione dinamica.
- xUnit C# su soluzioni algebriche, appoggio inclinato, molla rotazionale, cedimenti e regressione del round-trip JSON; `scripts/run-m77-tests.ps1` con pulizia bin/obj e regressione completa.
- M75/M76/M77 ancora da compilare e validare su Windows. Ultimo milestone confermato: M73.

## 2026-10-23 — M76: peso proprio automatico statico (test .NET 8 pendenti)

- `StructuralLoad.SelfWeightLoad` con scala dimensionless positiva, applicazione globale verso −Y e dati `UnitWeight` in kN/m³; validazione specifica del carico, densità/peso specifico obbligatorio e prevenzione di più azioni nello stesso caso.
- Espansione idempotente e senza mutazioni del modello `SelfWeightStaticLoadExpander`: carico distribuito Frame2D, due carichi nodali equivalenti Truss2D; supporto al calcolo misto, combinazioni e confronti con carichi manuali.
- Equilibrio, diagrammi Frame2D, campionamento spostamenti con svincoli e viewer includono il peso proprio; nessuna modifica a massa dinamica o carichi non lineari.
- Test C#/xUnit indipendenti su trave, truss verticale, telaio misto, combinazioni, svincoli e JSON; runner M76 con pulizia iniziale, CLI reale e regressione completa.
- M75 e M76 sono **in attesa di esecuzione Windows .NET 8**; non sono state introdotte verifiche normative. Vedere `docs/structural/m76-automatic-self-weight.md`.

## 2026-10-23 — M75: standard JSON model contract (Windows gate pending)

- M73 validato dall'utente, M74 riallineamento strategico acquisito.
- Implementato `StructuralModelJsonWriter`: round-trip del modello 2D standard, incluso metadata dinamico v1, nomi enum, release, selezione load case/combinazione e valori di carico.
- Introdotti `StructuralModelJsonContractValidator`, `StructuralModelJsonReader.ReadValidated` e `StructuralModelJsonValidationException` con diagnostica strutturata e codici stabili, opt-in senza modificare il comportamento di `Read`.
- Conversioni unitarie esplicite e test dimensionali in `Core.Units.StructuralUnitConversions` senza alterare la semantica del reader. Nuovi test C#/xUnit M75 e `scripts/run-m75-tests.ps1` con pulizia iniziale, build e regressione. Nessun Python; nessun solver FEM modificato.
- Resta da eseguire il gate Windows .NET 8. Il writer non conserva estensioni radice sconosciute (come configurazioni sismiche/non lineari specifiche), limitazione documentata. Peso proprio rinviato a M76.

## 2026-10-23 — M74-PLAN: riallineamento strategico (solo documentazione)

- M73 confermato dall’utente come testato con successo in C#/.NET.
- Nuova visione 2D acciaio/legno e priorità: calcolo FEM → verifica normative SLU/SLE → editor C#/Avalonia.
- Roadmap dettagliata `docs/structural/vision-and-roadmap-2d.md`, README, `roadmap.md` e handoff riallineati.
- Integrazione OpenCad2D e pianificazione M74–M75 precedenti dichiarate storiche, non obiettivi immediati.
- Nessuna modifica a sorgenti `src/`, test, strumenti, JSON o script; i test già superati restano riferiti al medesimo codice M73.

## 2026-10-23 — M73 (implementato; verifiche Windows in attesa)

- Aggiornato stato M71/M72-FIX2 a test C# passati come confermato dall’utente.
- Aggiunto benchmark **C#/.NET** `StructuralSolver2D.PerformanceM73` (Dense-SPD, Truss2D statica, Newmark, elemento Frame2D cerniere) con controlli analitici e report CSV/JSON locali.
- Nuovi test xUnit del contratto di misura e runner `run-m73-tests.ps1` (prima pulizia `bin/obj`, poi full Release build, test mirati, regressione e baseline); nessuna soglia su millisecondi.
- Documentazione prestazioni/limiti e roadmap aggiornate. Solver FEM di produzione invariati; solo friend-assembly esplicita per benchmark interno.
- **.NET 8 non disponibile qui:** M73 non ancora validato dalla compilazione su Windows.

## 2026-10-23 — M72-FIX2: avvisi xUnit2012/2031/2013 (test .NET pendenti)

- Eliminati i sei avvisi segnalati da xUnit nei test M65, M66, M67 e M68 con le asserzioni native `Assert.Contains`, `Assert.Single(predicate)` e `Assert.Single(collection)`.
- Soltanto quattro file di test C# modificati; invariati solver, API, CLI, esempi e script PowerShell, comprese le correzioni M72-FIX1.
- Nessun analyzer disattivato e nessun nuovo script Python. Verifica Windows: `scripts/run-m72-tests.ps1` (clean + build + gate M71/M72 + regressione completa).
- M71/M72 non dichiarati validati prima della conferma della suite .NET.

## 2026-10-23 — M72-FIX1: PowerShell argument binding e leggibilità console (test Windows pendenti)

- Risolto `MSB1008` prima della compilazione .NET 8: quando `dotnet` è intercettato dalla funzione di `stable-dotnet-console.ps1`, `-v:minimal` può essere associato al parametro comune PowerShell `-Verbose` e generare un argomento `minimal` spurio. Rimossa l'opzione abbreviata dalle build M71 e M72; la verbosità di default è sufficiente.
- Il wrapper imposta temporaneamente la decodifica UTF-8 per correggere la possibile visualizzazione corrotta dei caratteri accentati (`├¿`), poi ripristina la precedente impostazione. Rimangono invariati il log testuale, le variabili d'ambiente isolate e l'uscita di errore.
- Nessuna modifica ai sorgenti C#, ai test numerici, alla CLI di produzione, ai JSON e al modello Frame2D; M71 e M72 ancora da validare su Windows.
- Verifica: `scripts/run-m72-tests.ps1` (pulizia `bin`/`obj` prima di qualsiasi build/test).

## 2026-10-23 — M72, primo elemento Frame2D non lineare (prototipo interno, test pendenti)

- M71-AUDIT-FIX1 non ancora convalidato dall’utente: M72 rimane una prosecuzione **condizionale e sperimentale**.
- Aggiunti `BilinearRotationalHinge` ed `ElastoplasticFrame2DElement` in `Frame2D/Nonlinear`: una/due cerniere plastiche di estremità in serie con trave elastica, Newton e condensazione tangente consistenti, forze locali/globali, energie e commit esplicito.
- Test C#/xUnit per rigidezza elastica, soluzione chiusa momento–rotazione, lavoro/dissipazione, tangente, orientamento, due cerniere, carico ciclico, plasticità perfetta, stale commit e rollback locale.
- Runner `scripts/run-m72-tests.ps1`: pulizia di bin/obj prima di ogni altra operazione, console testuale M71, build, gate M71+M72 e regressione completa. Nessun nuovo Python.
- README, roadmap, piano di sviluppo e handoff aggiornati. **Non implementati** assemblaggio globale statico non lineare Frame2D, dinamica plastica, API/JSON/CLI; invariati tutti i solver lineari.

## 2026-10-23 — M71 AUDIT-FIX1: controllo pre-test e console

- Audit integrale di progetto, runner, formato JSON, documentazione, benchmark M71 e confezionamento.
- Corretto il ripristino delle variabili `CI`, `TERM`, `NO_COLOR` e `DOTNET_NOLOGO`: valgono ora soltanto durante i processi `dotnet`, senza contaminare la sessione PowerShell successiva.
- Aggiunto in `run-m71-tests.ps1` un preflight automatico su exit code del comando nativo, isolamento dell'ambiente e produzione del log testuale.
- Nessun cambiamento ai solver, alle API, al formato JSON, ai test xUnit o ai benchmark numerici in C#; compilazione e test Windows M71 ancora necessari.
- Rapporto: `docs/structural/m71-audit-pretest.md`.

## 2026-10-23 — M71 (studio Frame2D + output console)

- M70-FIX1 validato dall’utente con test .NET 8.
- Aggiunto studio dettagliato delle alternative di plasticità flessionale concentrata/distribuita, con scelta **proposta** per M72 e limiti fisici chiariti.
- Aggiunte prove analitiche C#/xUnit isolate per flessione Frame2D elastica, molla rotazionale in serie, plasticizzazione e tangente consistente (non costituiscono un nuovo solver di produzione).
- Tutti gli script `run-m*-tests.ps1` mantengono la pulizia `bin`/`obj` come prima operazione, poi caricano `scripts/stable-dotnet-console.ps1`: output `dotnet` progressivo senza riscrittura di righe e log UTF-8 in TEMP.
- Test del nuovo helper PowerShell e M71 ancora da validare sul Windows dell’utente; non aggiunti script Python.

## 2026-10-23 — M70-FIX1: benchmark CQC e output PowerShell statico (test Windows pendenti)

- Nel benchmark a due campate `Truss2D`, aggiunto l'appoggio verticale di B omesso per errore, che causava `M54_INTERNAL_MECHANISM`; verificato il riferimento equivalente M60.
- Aggiunto test negativo che conserva la diagnosi del meccanismo reale quando il vincolo intermedio viene rimosso.
- Disabilitato il terminal logger dinamico (`--tl:off`) su `dotnet build` e `dotnet test` in tutti i 21 runner PowerShell, per una console progressiva senza ridisegno.
- Conservati rigore della riduzione M54, algoritmi CQC/M60, API, CLI, viewer e suite precedenti. Rimane da eseguire il gate M70 Windows.

## 2026-10-23 — M70: combinazione modale CQC (in attesa di test .NET)

- M69-FIX1 validato dall’utente mediante script Windows.
- CQC opzionale con smorzamento modale uniforme esplicito, segno dei contributi mantenuto e formula di correlazione per modi vicini.
- Compatibilità SRSS invariata di default; nuove proprietà API/JSON opzionali e intestazioni CQC nei report/CSV.
- Suite C#/xUnit numerica, report e CLI; nuovo esempio M70 e script con pulizia `bin/obj` iniziale.
- Risultati non normativi; nessuna generazione automatica di spettri NTC/EC8, forze di progetto o certificazione.
- Test .NET 8 ancora da eseguire sul Windows dell’utente.

## M69-FIX1 — 2026-10-23 — Test di sicurezza HTML/JSON (in attesa di regressione Windows)

- Corretto soltanto il test xUnit `MaliciousLabels_AreEscapedInsideStandaloneHtml`: i valori non fidati sono serializzati in JSON con escaping `\u003C`, non come entità HTML.
- Verificato tramite test il contenuto del dataset, i due soli blocchi script previsti, la decodifica senza alterazione dei valori e l'uso di `textContent`.
- Nessuna modifica ai calcoli FEM, alle API, al viewer JavaScript o ai comandi CLI. Test .NET 8 completo e QA browser ancora da ripetere.

## M69 — 2026-10-23 (in attesa di test .NET)

- M68 validato dall’utente. Viewer autonomo offline per analisi statiche incrementali e temporali Truss2D elasto-plastiche; grafici, selettori e stati accettati.
- Due comandi CLI `nonlinear-static-viewer` / `nonlinear-time-viewer` e esportazione HTML con codice CLI 2 se analisi parziale.
- Nuova suite xUnit C# e script `run-m69-tests.ps1` con pulizia `bin`/`obj` come prima operazione. Benchmark numerici senza nuovi script Python.
- Controllati JavaScript e interazione Chromium su dataset sintetico; generazione effettiva .NET e QA visiva sul browser del destinatario ancora da completare. Nessun cambiamento ai solutori lineari/non lineari.

## M68 — 2026-10-23 — API pubbliche e report reticolari non lineari (in attesa di test)

- Esposte API opt-in per M64 e M65 tramite `StructuralSolver2DService`; DTO pubblici tipizzati e risultati di passi accettati, con stato di convergenza.
- Aggiunti sei comandi CLI dedicati, JSON isolato, due coppie di CSV nodi/aste, status Markdown e report dei passi.
- Test C# xUnit di API, report e CLI; script `run-m68-tests.ps1` con pulizia obbligatoria iniziale.
- Nessuna modifica ai solutori M64/M65, alle API lineari o ai viewer. M67 confermato dall'utente; M68 ancora da validare.

## 2026-10-23 — M67: reazioni dinamiche ed estremità elastiche (test .NET in attesa)

- Confermato dall’utente il superamento della suite M66.
- Nuovo servizio lineare separato per reazioni `M*a+C*v+K*u-F`, azioni nodali elastiche di estremità, inviluppi e residui nelle coordinate libere.
- Nuovi comandi CLI `time-forces`, `time-forces-report`, `time-forces-csv`; CSV invariant e avvisi espliciti sui limiti fisici.
- Aggregatore interno risultati plastici M65 basato solo su passi Newton accettati.
- Benchmark C#/xUnit per mensola, truss, inerzia, smorzamento, reazioni e rollback; `scripts/run-m67-tests.ps1` con pulizia automatica iniziale.
- Nessuna modifica a Newmark, static solver o schemi JSON preesistenti. Nessun diagramma continuo dinamico o certificazione antisismica.

## 2026-10-23 — M66: gate di verifica numerica non lineare 2D (test Windows in attesa)

- M65 confermato dall'utente come superato nella suite automatica C#.
- Aggiunti **solo ai test** `NonlinearM66AlgebraicReference.cs` e `NonlinearValidationM66Tests.cs`: soluzione algebrica indipendente per rami elastoplastici e Newmark a 1 DOF, equilibrio monotono due aste, proprietà di orientamento/partizione, energia e dissipazione, singolarità, stati non fisici e rollback.
- Aggiunto runner `scripts/run-m66-tests.ps1`: pulizia `bin`/`obj` obbligatoria come prima operazione, build Release, test mirati e regressione completa.
- Aggiornati README, roadmap, handoff e documentazione tecnica. **Nessun cambiamento ai sorgenti di produzione, alle API pubbliche, alla CLI o al solver lineare.**
- .NET 8 non disponibile nell'ambiente di preparazione: M66 **non è ancora validato**.

## 2026-10-23 — M65 dinamica non lineare Truss2D + nuovo README (test Windows pending)

- M64 confermato dall'utente come testato. Prototipo interno M65 di integrazione non lineare in gradi di libertà fisici: massa consistente, Newmark–Newton, smorzamento iniziale proporzionale, supporti inclinati, reazioni, stati plastici atomici e bilanci energetici distinti.
- Test C#/xUnit con soluzione algebrica chiusa per asta elastica e plastica, prove di energia, smorzamento, rotazione, due aste, rollback e raffinamento `dt`. Nessun nuovo benchmark Python.
- `run-m65-tests.ps1`: pulizia obbligatoria `bin/obj` come prima operazione, Release, test M63–M65, regressione completa.
- README italiano completamente riscritto con badge Shields.io coerenti con .NET8, C#, GPLv3 e `VERSION=0.1.0`, presentazione del progetto, funzioni validate, limiti, traguardi M63–M75 ed esempi CLI.
- Nessuna modifica alle API pubbliche, ai formati JSON, al viewer o al solver lineare. La build .NET8 M65 resta da confermare.

## 2026-10-23 — M64: reticolari elasto-plastiche, statica incrementale (test Windows pending)

- Accettati i test C# M63 comunicati dall'utente.
- Nuovo risolutore **internal** per Truss2D con assemble tangente globale, Newton–Raphson, supporti anche inclinati/ridondanti, stati plastici commit solo dopo convergenza e rollback.
- Test xUnit con valori chiusi C#: trave assiale, due aste parallele, reticolare triangolare, reazioni, inversioni, zero-load mechanism, perfetta plasticità e mancata convergenza multi-elemento.
- Script `scripts/run-m64-tests.ps1`: pulizia `bin`/`obj` obbligatoria prima della compilazione, build, test M64 e regressione intera; **nessuna dipendenza Python**.
- API/CLI/JSON e codice di analisi lineare invariati. La nuova funzionalità non è ancora validata tramite .NET Windows e non consente verifiche normative/di collasso.

**2026-10-23 — M63 (implementazione in attesa dei test Windows):** primo elemento Truss2D elasto-plastico a geometria fissa, separato dal solver lineare. Return mapping a incrudimento isotropo (tensioni/deformazioni), stato committed+trial con commit esplicito, forze interne globali e rigidezza tangente 4×4, energie elastiche/plastiche, verifiche analitiche e confronto dimensionale con M62. Nuovi test xUnit, riferimento Python opzionale e runner `run-m63-tests.ps1` con pulizia `bin`/`obj` prima di build/test. Nessuna API/CLI/JSON pubblica, nessuna modifica alle matrici lineari. Test .NET 8 ancora da eseguire su Windows.


## 2026-10-23 — Pianificazione successiva a M62 (solo documentazione)

- Registrata l'accettazione dei test automatizzati C# M62 comunicata dall'utente; Python esterno opzionale non eseguito su Windows (`SKIPPED`, alias Microsoft Store).
- Nuovo `docs/structural/roadmap-forward-m63-m75.md`: milestones M63–M75 con attività, dipendenze, test, criteri di accettazione, priorità e confini d'uso.
- Documento principale, piano di sviluppo e handoff puntano alla nuova pianificazione. Nessuna modifica agli algoritmi, ai file `.cs`, ai test e agli script `.ps1`.

## 2026-10-23 — M62-FIX1 optional Python runner preflight/diagnostics (Windows retest pending)

- User Windows report: M62 runner stopped only at step `4/4` (Python reference) with a generic exception; C# build, M62 tests and full solution regression had passed to reach that stage. No Python traceback was supplied.
- Validated `scripts/m62-reference-piecewise-sdof.py` independently with Python 3.13.5 (exit code 0, maximum force residual ~1.585e-12 kN). No change to reference math or C# production/tests.
- `scripts/run-m62-tests.ps1`: preserve mandatory `bin`/`obj` cleanup first; require a working Python >=3.7 (including `dataclasses`) before attempting the *optional* reference, fall back from `python` to Windows launcher `py -3`, show `SKIPPED` explicitly if neither works. A real benchmark error remains fatal and prints the original diagnostic and exit code.
- See `docs/structural/m62-python-benchmark-runner-fix.md`. New PowerShell behavior awaits Windows acceptance; do not infer reason for the user's old Python failure without interpreter output.

## 2026-10-23 — M62 isolated nonlinear SDOF research prototype (Windows .NET acceptance pending)

- M61 user-confirmed .NET/PowerShell tests passed. M62 adds an **internal, opt-in-by-tests only** bilinear elastoplastic SDOF spring/mass oscillator, not a nonlinear frame/truss public feature.
- Constitutive return mapping with immutable committed plastic state, isotropic hardening, non-negative dissipation; tangent-Newton and average-acceleration Newmark with failure rollback.
- Separate discrete work and physical energy residuals; initial plastic states without prior history, malformed inputs and nonconvergent Newton are rejected explicitly.
- Added C# tests for elastic M59 equivalence, yield/unloading/cyclic reversal, finite-width pulse, analytic piecewise-reference values, convergence and rollback; pure-Python independently branched analytical Newmark reference.
- Added `scripts/run-m62-tests.ps1` with **mandatory project-local bin/obj cleanup first**, full build and regression. Existing public static/modal/harmonic/transient/seismic services untouched; no .NET 8 SDK in packaging environment.
- Roadmap, M62 technical note and AI handoff updated. **M62 tests still require user Windows execution.**

## 2026-10-23 — M60 exploratory seismic excitation and response spectra (pending .NET acceptance)

## 2026-10-23 — M61 nonlinear feasibility study and mandatory test cleanup (Windows acceptance pending)

- User confirmed M60 full .NET 8 test suite passed. M61 delivers a **research-only** study of nonlinear constitutive history, tangent stiffness, Newton–Newmark iteration, energy accounting, failure/rollback and benchmark validation. No public nonlinear C# solver added.
- Added `docs/structural/m61-nonlinear-feasibility.md`, plus standalone **non-production** bilinear uniaxial return-map experiment `scripts/m61-check-bilinear-return-map.py` (independent numeric checks passed).
- Added shared `scripts/clean-project-build.ps1` with project-file discovery, safety guards and `-WhatIf`; updated every milestone PowerShell test runner M51–M60/M591 to call it first. Replaced redundant `dotnet clean` commands with the common preflight.
- Added `scripts/run-m61-tests.ps1` to verify preflight ordering, deletion/dry-run/idempotency and non-project preservation on scratch fixtures, then perform full solution build/tests. Added usage guide `docs/structural/build-cleanup.md`.
- **Verification:** no PowerShell or .NET SDK available in packaging environment. Windows execution remains pending, so this does not claim the newly edited test runners pass.


- M59.1 audit FIX1 passed on user Windows per report. M60 adds opt-in, **non-normative** uniform base-acceleration time history, signed equivalent nodal inertial loading based on full consistent FEM mass, separate relative and absolute acceleration results.
- Modal Gamma and effective masses measured against full before-restraint ground mass; user-supplied elastic pseudo-acceleration spectra interpolated by period, unsigned SRSS nodal displacement/rotation envelopes and mass/dense-mode limitations.
- New API, CLI `seismic-time*` and `seismic-spectrum*`, Markdown + CSV outputs, regression/analytical tests and a dedicated example. Existing static/modal/harmonic/Newmark/M59.1 algorithms unchanged.
- Validation: independent SciPy one-/two-DOF formulas and JSON checks; Windows .NET 8 M60 and full regression remain pending via `scripts/run-m60-tests.ps1`.

## 2026-10-23 — M59.1 audit FIX1 (awaiting Windows .NET tests)

- Verified 436-file baseline and 429/429 M59 predecessor preservation; six JavaScript syntax checks, 78 JSON parse checks and independent SciPy FEM/Newmark reference comparison.
- Added diagnostics for an impossible 2000-step all-mode resolution budget, inconclusive zero responses and misleading focus/peak sampling; expanded M59.1 xUnit regression tests.
- Recorded independently estimated medium/fine differences in tip Uy (0.98%), Vy (1.09%) and Ay (13.24%). Acceleration convergence is not established; diagnostic thresholds are illustrative only.
- No changes to original M59 integrator, matrices, M51–M58 algorithms, JSON schema or viewers. .NET 8 build and full suite remain pending.

## 2026-10-23 — M59.1 time-step convergence diagnostics (awaiting Windows acceptance)

- Added three-grid (dt, dt/2, dt/4) sensitivity study for all physical nodal U/V/A components and peak timing; conservative natural-frequency resolution diagnostics and warnings for inactive responses and knot alignment.
- Public opt-in `AnalyzeTimeHistoryConvergence`, dedicated reporting and CSV/Markdown CLI commands; no change to original Newmark dynamics, static solver, model schema or viewers.
- Added focused numerical, output and CLI tests plus clean build/regression PowerShell script. New M59.1 suite has **not** been executed with .NET 8 in the packaging environment. M59 automatic tests passed on user Windows system.

## 2026-10-23 — M59 linear transient integration and time-history exports (pending .NET verification)

- M58 automated Windows suite accepted by user, including M57 predecessor regressions; manual viewer browser inspection remains open.
- Added full-spectrum average-acceleration Newmark transient solver (physical nodal displacement/velocity/acceleration, initial conditions, piecewise-linear nodal force histories, explicit modal/Rayleigh damping and energy residual diagnostic), no static solver changes.
- Added optional M59 API, new `time*` CLI commands, two invariant CSV exports, Markdown report, standalone transient HTML viewer, JSON example and regression tests.
- Added clean Windows `scripts/run-m59-tests.ps1`; packaging environment lacks .NET SDK, so M59 compilation/tests remain pending. Member-force/reaction envelopes remain out of scope and are documented.

## 2026-10-23 — M58 experimental linear harmonic response and damping (awaiting tests)

- Complete M57-FIX2 baseline preserved; its Windows acceptance is still pending by user choice.
- New opt-in steady-state SDOF/MDOF frequency response (full-spectrum FEM modes; consistent mass, admissible reduced coordinates), physical nodal amplitudes and lag phase; sinusoidal nodal Fx/Fy/Mz only.
- Explicit uniform modal damping or Rayleigh proportional damping, with dimensional validation and no silent defaults. Resonance without damping is rejected when excited.
- New API, separate CLI harmonic commands, invariant CSV/Markdown exports, a standalone HTML amplitude/phase viewer, JSON example and analytic/CLI/report regression tests.
- Added `scripts/run-m58-tests.ps1` for clean .NET 8 build, M57-FIX2 acceptance, M58 tests, whole-solution regression and CLI smoke exports. JavaScript syntax can be checked with Node. No .NET SDK available in the packaging environment; **no .NET tests claimed passed**.

## 2026-10-23 — M57-FIX2 CLI static/modal example regression correction (awaiting Windows retest)

- Fixed `CliCommandRegressionTests.Examples()` to distinguish legacy static fixtures from new modal-only examples by `dynamics` + missing/empty `loadCases` JSON content. The previous test accidentally ran static `analyze` against `examples/dynamics/m57-modal-cantilever.json` and failed, correctly, with no load case.
- Added full partition regression and positive modal CLI coverage (`modal`, `modal-report`, `modal-csv`, `modal-viewer`) plus rejection tests for static `analyze`/`export-viewer`. Updated `run-m57-tests.ps1` to gate FIX2 and run targeted CLI examples before full regression.
- No production source, existing model/fixture or calculation modified. M57 acceptance remains pending full Windows tests and browser inspection; M58 not started.

## 2026-10-23 — M57-FIX1 Markdown normalization output correction (awaiting Windows retest)

- Fixed Markdown modal report to display the `StructuralModalAnalysisOutput.Normalization` contract, including `Mass: phi^T M phi = 1`; the previous exporter omitted `Mass:` and caused one M57 reporting xUnit failure (2/3 passed).
- Added regression assertions for the default and supplied normalization text and an end-to-end Markdown-content check in `run-m57-tests.ps1`, plus a source revision gate to avoid stale-folder test runs.
- No structural analysis, mass matrices, modal eigensolver, static analysis, public API, CSV or viewer behavior was modified. M57 still requires full Windows .NET 8 suite and browser acceptance.

## 2026-10-23 — M57 Public modal analysis, reports and viewer (awaiting .NET 8 acceptance)

- M56-FIX1 accepted on user's Windows after clean extraction; earlier stale source files caused a repeated failure.
- Public, opt-in modal analysis facade and spectrum quality metadata, preserving mass normalization and diagnostic refusal behavior.
- Modal CLI summary, Markdown/CSV exports and standalone animated HTML modal viewer, with actual natural frequencies distinguished from illustrative playback speed and amplitude.
- Viewer uses interior Hermite interpolation for Frame2D mode shapes; previous static viewer remains unchanged.
- M57 regression/CLI smoke script (`run-m57-tests.ps1`) cleans before compiling and checks corrected M56 code marker.
- New modal-only JSON example, tests and scope/limitations documentation. Full Windows .NET 8 testing and manual browser review pending.

## 2026-10-23 — M56-FIX1: repair modal benchmark spatial-alias selection (pending .NET revalidation)

- User reported 27 passing and 1 failing focused M56 tests: the 4-element pinned
  beam's third flexural mode was erroneously matched to a higher bending mode at
  1013.397 Hz by a MAC based only on nodal transverse displacements.
- Replaced the pinned-beam selector with MAC evaluated from cubic Hermite
  interpolation inside each element using nodal Uy and Rz; discard mostly axial
  eigenvectors via sampled axial-versus-bending displacement norm.
- Added a dedicated regression reproducing the nodal alias and demonstrating
  that interior sampling distinguishes the correct ~329 Hz mode from the high mode.
  Retained analytical target 323.111 Hz and original 2% coarse-mesh tolerance.
- Did not change M55 eigensolver, static calculation core, public contracts, CLI
  or viewer. Full .NET 8 tests must be rerun before M56 can be accepted.

## 2026-10-23 — M56 independent modal analytical benchmarks (pending .NET acceptance)

- User confirmed M55 full .NET tests passed; M51–M55 now user-validated (logs not attached).
- Added continuous-theory reference comparisons for two-story shear, simply supported
  Euler–Bernoulli beams, cantilevers and longitudinal truss vibration.
- Added mesh convergence, transverse/axial MAC, near mode-family interleaving,
  coordinate transforms, section/density/length scaling and explicit failure tests.
- Added 14 JSON geometric fixtures, a two-story matrix fixture, reproducible pure-Python
  analytical references, optional independent SciPy FE cross-check and detailed
  `benchmarks/modal/README.md`; reference numbers are not C# execution results.
- Added `scripts/run-m56-tests.ps1`, roadmap, theory and handoff updates.
- No production static code, public API, CLI, viewer, or M55 eigensolver changes.
- Pending: .NET 8 build, focused M56 test suite, Dynamics regressions and full regression.

## 2026-10-23 — M55 internal generalized modal eigensolver (pending .NET acceptance)

- User confirmed M54 full tests passed; M51–M54 now user-validated (logs not attached).
- Added pure-C# internal symmetric generalized eigensolver (mass equilibration,
  Cholesky, cyclic symmetric Jacobi) for M54-ready positive definite matrices.
- Calculates natural frequencies, periods, mass-normalized global/reduced shapes,
  Rayleigh quotients, eigen-residuals and modal orthogonality diagnostics.
- Added dense matrix, repeated mode, shear-building analytical, assembled frame,
  inclined support, invalid mass/spectrum and scale-consistency regression tests.
- Added script `run-m55-tests.ps1`, theory note, roadmap and handoff updates.
- No changes to public static service/CLI, no modal outputs enabled yet.
- Pending: .NET 8 build/full regression and M56 independent benchmark gate.

## 2026-10-22 — M54 admissible dynamic DOFs (pending .NET acceptance)

- User confirmed M53 build/tests passed; M53 accepted on that report.
- Added opt-in, internal dynamic support nullspace projection (`Z`) with
  inclination/redundancy handling and removal of truly unused truss rotations.
- Added reduced symmetric `K/M`, mechanism/free-free/singular-mass diagnostic
  statuses and stable codes; no modal eigensolver or static solver changes.
- Added M54 regression tests, theory/roadmap/handoff and validation script.
- Pending: `.NET 8` compilation, focused and complete regression suite on Windows.

## 2026-10-22 — M53 consistent dynamic mass matrices (pending .NET acceptance)

- User confirmed M52 tests passed; M52 accepted on that report (full logs not supplied).
- Added internal 2D truss and Euler–Bernoulli frame consistent mass matrices, with
  educational-only diagonal lumping for comparison.
- Added opt-in unconstrained global K/M assembler with additive nodal inertia,
  truss-only massless-rotation diagnostics and explicit release rejection.
- Added independent Hermite quadrature/energy/orientation and mixed-assembly tests,
  extended theory, roadmap/handoff, and `scripts/run-m53-tests.ps1`.
- Production static solver, CLI input/output and public runtime API unchanged.
- Pending: run .NET 8 build/tests and full regressions before closing M53.

## 2026-10-22 — M52 versioned dynamic mass data contract (pending .NET acceptance)

- User confirmed M51 tests passed; M51 may proceed on that basis (logs not supplied).
- Added opt-in dynamic schema v1, separate modal request/output data types (no solver),
  unit conversions from weight and kilograms, additive nodal masses, and detailed
  dynamic-only validation of mass sources, referenced nodes, duplicate entries,
  missing unit weight, rotational inertia and numeric scale.
- Extended CLI JSON reader with an optional strictly validated `dynamics` section;
  all pre-existing static JSON and static calculations remain independent.
- Added an educational input example, Core/Analysis/CLI tests, `run-m52-tests.ps1`,
  and documentation on dimensional consistency, double-counting policy and M53 risks.
- **Pending:** .NET 8 build and full automated test suite in user's environment.
  No modal/dynamic calculations or seismic verification have been implemented.

## 2026-10-22 — M51 pre-solve mechanism/conditioning diagnostics (second implementation slice)

- Added internal constrained-stiffness projection and numerical spectrum inspector (small models only) to distinguish possible mechanisms, poor normalized conditioning, indefinite stiffness and invalid matrix data.
- Added a pre-solve internal model entrypoint that shares production assembly, and deterministic analytic and structural-model regression test cases for free-free systems, inclined/redundant supports and weak mechanisms.
- Updated M51 theory, roadmap, handoff and PowerShell test filters; no public API, JSON, CLI, production solve decisions or static results were intentionally altered.
- **M51 still in progress:** compilation and test execution require .NET 8 SDK, unavailable here. Stability warnings are heuristic and not structural safety checks.

## 2026-10-22 — M51 static FEM verification (initial implementation)

- Added opt-in internal energy/equilibrium/symmetry/constraint verification on the existing mixed-plane assembly.
- Added independent element transformation and rigid-body invariant regression tests, analytical cantilever/truss energy checks, released-member/distributed load, mixed frame, redundant support and combination scenarios.
- Added reference and theory notes; no public API, CLI, JSON or static calculation behavior changes.
- **Pending validation:** .NET 8 SDK unavailable in this environment; tests could not be run. M51 remains in progress.

# Changelog

All notable project-level release changes are documented in this file.

The format follows a simple milestone-based structure. StructuralSolver2D is still experimental and educational; version numbers indicate technical snapshots, not certified engineering releases.

---

## Unreleased

### Documentation

- Expanded `docs/structural/roadmap.md` with a Carpinteri-informed Milestones 51–61 plan for static numerical verification, mass modeling, modal analysis, dynamic response and exploratory seismic work. This is documentation only; no dynamics implementation or certification is claimed. Aligned `ai-handoff.md` and the post-M39 development plan cross-reference.

### Changed

- PDF reports use a navy/teal A4 layout with numbered sections, model statistics, highlighted results, and a vector geometry/deformation schematic. Tables use separate aligned columns, alternating backgrounds, repeated headers, and page totals. Added pagination, PDF structure, option and signed-deformation regressions; example PDFs have been regenerated.
- Marked PDF artifacts as binary so Git line-ending normalization cannot corrupt their object offsets or stream lengths.

### Fixed

- Second Core review: section catalog JSON rejects missing/null inertia while preserving explicit zero; model validation checks optional section dimensions and support enums. Null model entries and combination terms produce indexed validation issues, and fluent additions reject null immediately.
- Member lengths use a scaled norm and reject non-finite lengths during validation. Section factories avoid intermediate overflow/underflow, preserve representable subnormal results and thin walls, and reject unrepresentable properties. Generators normalize interpolation before multiplying dimensions and reject collapsed or overflowing coordinates.
- Added 88 Core regression cases and four CLI process cases for the second review; the complete .NET suite passes 866 cases.
- Static deformation playback now follows undeformed → deformed → undeformed with a non-negative loading/unloading factor, replacing the misleading displacement reversal. Prepared frames use the same cycle. Added uniform-load beam regressions across the whole browser cycle and four frame counts; browser coverage now runs 333 checks.
- Deformation animation preserves nonuniform sample origins, fits both cycle peaks and the moving maximum callout, changes speed without phase jumps, and stops when the deformed layer is hidden. Added analytical cantilever and per-sample browser comparisons; coverage now includes 273 browser checks and three additional Reporting cases.
- Second Reporting review: independent SVG context layers, one viewer animation loop, preserved caller options and exact fractional initial controls.
- Viewer starts with model, supports and load arrows; result modes show one quantity at a time, with optional full layers, collapsed secondary controls, selection details, keyboard navigation, current amplification and Fit view. Narrow layouts no longer overflow horizontally.
- CLI `report`/`markdown` produce a concise report; `report-full` retains model tables and sampled appendices. Critical values, locations, units, analysis context and signed result tables remain in the summary; the Markdown API keeps its full default.
- PDF wrapping uses font widths, member forces use separate start/end rows, and WinAnsi text preserves Italian accents. Unsupported scripts fail explicitly instead of replacing identifiers. Markdown escapes special table content, report flags control only their sections, and Markdown/PDF/XLSX absolute summaries use magnitudes.
- Added 34 Reporting and one CLI regression cases; browser coverage now runs 180 checks across three models and three viewport sizes.
- CLI report/export commands reject output paths that would overwrite their input model, including every generated CSV filename.
- JSON input distinguishes missing/null required numbers from explicit zero and identifies null collection elements by path and index.
- All CLI analysis/report/export commands share the public analysis workflow; unused load cases no longer trigger different analyzer behavior between commands.
- A default `loadCombinationId` must identify a combination, explicit target overrides remain supported, and commands reject extra arguments before reading or writing files.
- Removed disconnected duplicate roof nodes from the distributed house-with-balconies example, and console absolute summaries now show magnitudes while detailed results retain their signs.
- Added CLI reader and process regression coverage for source preservation, aliases, input errors, case/combination selection, reactions, extrema and distributed examples.
- Reporting station labels interpolate sampled displacements at the requested quarter-span positions instead of relabeling the nearest sample.
- Interactive viewer reset restores the initial view; zoom and pan account for SVG aspect-ratio margins, and the maximum displacement callout follows deformation scaling and animation.
- PDF pagination checks every wrapped text line, Markdown reports include local support restraints and orientation, and XLSX numeric cells preserve small values with round-trip serialization.
- Visualization and viewer options reject non-finite values and enforce the supported numeric ranges.
- Added 80 Reporting .NET regression cases and 15 browser cases for exported viewer behavior.
- Analysis equilibrates linear systems before relative pivot checks and verifies residuals and finite solutions, rejecting rotated and unloaded mechanisms consistently.
- Released frame end rotations are recovered per member before displacement sampling, including factored point and distributed loads; deflection checks no longer use an artificial zero deformation.
- Independent support directions at a shared node receive their own reactions. Redundant supports use independent equations and a documented minimum-norm reaction distribution.
- Added regression coverage across all analyzers, load combinations, rotated members/supports, redundant constraints and stiffness scales.
- Parabolic truss generators now share chord end nodes and avoid duplicate end-panel diagonals.
- The Gerber generator uses stable cantilever end restraints around its suspended span.
- Validation rejects conflicting load-case/combination identifiers and undefined member types, and reports null member-node references without throwing.
- Section catalogs support zero inertia for truss sections; model validation still requires positive inertia for frame use and rejects negative or non-finite inertia everywhere.
- All parametric generators reject non-finite dimensions.
- Added regression coverage for generator topology, analytical reactions and deflections, nodal equilibrium, load diagrams, section persistence and invalid inputs.

## 0.1.0 - First technical release

### Release type

First technical release / technical preview.

This release consolidates the solver after Milestones 1-37 and creates a stable baseline for future work.

### Added

- Independent .NET 8 solution for StructuralSolver2D.
- Core structural model with nodes, members, materials, sections, supports, load cases and manual load combinations.
- Frame2D linear elastic analysis.
- Truss2D linear elastic analysis.
- Mixed plane-structure analysis with Frame2D and Truss2D members.
- Nodal forces and moments.
- Uniform, point and linearly varying member loads.
- Local/global load direction handling for inclined members.
- Frame2D member end moment releases.
- Support reactions, nodal displacements and local member end forces.
- Internal-force sampling for `N(x)`, `V(x)` and `M(x)`.
- Deformed-shape/displacement sampling for Frame2D members.
- Characteristic internal-force points for reporting and future graphical output.
- Global equilibrium checker.
- Preliminary sampled deflection checks for serviceability-oriented workflows.
- Parametric section helpers.
- Initial elastic material library.
- JSON examples and benchmark catalog.
- Automated benchmark runner.
- Markdown reports with educational explanations, executive summary, model statistics and optional deflection-check tables.
- CSV export for spreadsheet validation and external post-processing.
- Public API facade through `StructuralSolver2DService`.
- Documentation for scope, architecture, model, units, analysis, validation, reporting, CSV export, public API and theory notes.

### Current scope

- First-order linear elastic static analysis.
- Two-dimensional structures made of one-dimensional members.
- Educational and experimental use.
- Benchmark-backed development.

### Not included

- Certified structural design checks.
- Eurocode, NTC or other normative verification.
- Second-order analysis.
- Material nonlinearity.
- Dynamic, modal or seismic analysis.
- 3D structures.
- Plates, shells, solids or mesh FEM.
- CAD integration.

### Recommended validation command

```powershell
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln
dotnet test StructuralSolver2D.sln
```
