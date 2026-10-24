# StructuralSolver2D

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Linguaggio-C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Modello](https://img.shields.io/badge/Strutture-2D-0078D6?style=for-the-badge)](docs/structural/model.md)
[![Test](https://img.shields.io/badge/Test-M85--C%20superato-159957?style=for-the-badge)](docs/structural/roadmap.md)
[![Sviluppo](https://img.shields.io/badge/M86--RC1-Gate%20Windows%20aperto-FFB000?style=for-the-badge)](docs/M85B_VALIDAZIONE_INTEGRATA.md)
[![Versione](https://img.shields.io/badge/Versione-0.1.0-6C757D?style=for-the-badge)](VERSION)
[![Materiali](https://img.shields.io/badge/Materiali-Acciaio%20%7C%20Legno-7F52FF?style=for-the-badge)](docs/structural/vision-and-roadmap-2d.md)
[![Licenza](https://img.shields.io/badge/Licenza-GPLv3-008080?style=for-the-badge)](LICENSE)

> **Obiettivo aggiornato — 23 ottobre 2026.** Software **open source per analisi FEM di strutture piane ad aste in acciaio e legno**, per studenti, docenti e professionisti (uso didattico e preliminare). Sviluppo nell'ordine **motore FEM verificato → verifiche NTC/Eurocodici SLU/SLE → editor leggero C#/Avalonia**. Esclusi 3D, c.a., BIM, muratura, pareti, piastre e X-LAM. OpenCad2D non è più prerequisito. **M85-C ha superato i test Windows**: build 0 errori/avvisi, 8 nuovi test e 1.334 di regressione (log 24/10/2026). **M86 RC1 è una release preview dei soli sorgenti, in attesa del proprio gate Windows**. I numeri M83-A forniti dall'utente sono ora disponibili nel repository. Solver sparso M83-B e plasticità M84 restano opzionali/differibili. M82 è un secondo ordine *globalmente linearizzato* a perimetro limitato, non un solver iterativo generale. **Nessuna verifica normativa ancora implementata.** La [roadmap di prodotto](docs/structural/vision-and-roadmap-2d.md) prevale sulla numerazione storica dei milestone.

**StructuralSolver2D è un motore di calcolo strutturale bidimensionale, open source e scritto in C# su .NET 8.** Nasce per analizzare strutture costituite da travi e aste reticolari, ma soprattutto per costruire, passo dopo passo, un risolutore di cui sia possibile capire e verificare ogni risultato.

Il progetto vuole colmare la distanza fra un esempio didattico di analisi matriciale e un'applicazione numerica realmente utilizzabile. Per noi non basta ottenere uno spostamento o un diagramma del momento: occorre conoscere le ipotesi che lo hanno prodotto, controllare l'equilibrio, confrontare soluzioni analitiche e riconoscere quando un modello supera i limiti del metodo adottato. Per questo ogni estensione viene accompagnata da benchmark e test automatici.

Il software funziona **autonomamente** attraverso file JSON, comandi CLI o API .NET. In futuro avrà un **editor desktop leggero in C#/Avalonia** come modulo separato; un eventuale adapter OpenCad2D resterà facoltativo.

## M86 RC1 — Release tecnica preview (24/10/2026)

**Baseline certificata dal log dell’utente:** M85-C, build Release 0 warning/0 error, 8 casi mirati e 1.334 regressioni superate. **Novità M86 RC1:** documento del perimetro e dei limiti, manifest SHA-256 e runner `scripts/run-m86-release-gate.ps1` per la build, i test e i smoke test CLI/CSV/XLSX/PDF. I sorgenti `src/` sono identici a M85-C; versione assembly 0.1.0 invariata. **M86 RC1 non è stata ancora testata su Windows.** [Istruzioni e perimetro](docs/structural/m86-release-preview.md) · [Registro di accettazione](docs/structural/m86-acceptance-results.md).

## M85-C — Riferimenti statici, robustezza e accettazione Core (24/10/2026)

**Baseline:** M85-B FIX1 confermato su Windows: build Release **0 avvisi e 0 errori**, 5 casi integrati e 1.326 test di regressione passati, 8 benchmark M83 completati a 20 ripetizioni. **Nuova suite M85-C da eseguire su Windows.** Aggiunti otto casi xUnit: reticolare triangolare analitica con EA disomogeneo e lavoro virtuale, trave continua a due campate con riferimento al teorema dei tre momenti e un esempio numerico pubblicato esternamente, scaling della rigidezza, indipendenza dall'ordine di inserimento e invarianza per traslazione delle coordinate. Non è stato modificato alcun file `src/`. Runner: `scripts/run-m85c-tests.ps1`. [Matrice tecnica](docs/structural/m85c-final-static-validation.md) · [Criteri di release M86](docs/structural/m86-release-acceptance-checklist.md).

## M85-B — Coerenza API/CLI/report (24/10/2026)

**M85-A e precedenti: test Windows superati secondo l’utente. M85-B: build, 5 test end-to-end e 1.326 test di regressione superati; benchmark opzionale corretto a 20 ripetizioni, da rieseguire.** Confronta soluzioni analitiche statiche (mensola e trave biappoggiata), API C#, CLI, tutti i CSV, Markdown e celle numeriche XLSX; verifica la generazione PDF come file valido. Non modifica il motore FEM. Avvio: `scripts/run-m85b-tests.ps1`. Campionamento prestazioni opzionale: `-WithBenchmark`. [Matrice integrata e limiti](docs/M85B_VALIDAZIONE_INTEGRATA.md).

### Storico M85-A

## M85-A — Verifiche indipendenti (24/10/2026)

**M85-A: test Windows superati, confermati dall’utente.** Un controllo di validazione statico usa formule chiuse per una mensola inclinata e due barre assiali, reciprocità di Maxwell–Betti, sovrapposizione con coefficienti negativi e rotazione del telaio di 90°. Nessun `src/` modificato. Eseguire `scripts/run-m85-tests.ps1`. [Matrice e limiti](docs/structural/m85a-independent-validation.md). I dati M83 originali sono stati ricevuti e inclusi nella versione M85-B; la decisione sul solver sparso resta rinviata.

### Storico M83

## M83-A — Benchmark di scalabilità Frame2D (24/10/2026)

**M82 e M83-A test Windows superati, confermati dall’utente il 24/10/2026.** Prima di introdurre un risolutore sparso, misuriamo su telai reali il solver elastico Frame2D e il solver linearizzato M82, con benchmark analitici, reazioni di equilibrio, CSV/JSON, test C#/xUnit e regressione. Il codice dei solver FEM esistenti non cambia. Per il gate: `scripts/run-m83-tests.ps1`. [Nota M83-A](docs/structural/m83a-frame2d-scalability-decision.md).

### Storico del secondo ordine

## M82 — Secondo ordine globale linearizzato (24/10/2026)

**M75–M82 con test Windows superati (conferma dell’utente; M82 dopo FIX CS0246).** Da M81 deriva un nuovo calcolo opt-in globale Frame2D con `Kt = Ke + Kg(N¹)` assemblata da sforzi assiali di primo ordine, reazioni, spostamenti e ricerca per bisezione della perdita di definitezza positiva. Non è un solver geometrico non lineare iterativo, né una verifica normativa. Accetta soltanto telai senza release, vincoli rigidi assiali, azioni nodali singolo caso; carichi di asta, peso proprio, molle e cedimenti sono esclusi **solo da M82**, non dal normale solver. [Nota M82](docs/structural/m82-global-linearized-second-order.md) · `scripts/run-m82-tests.ps1`.

### Storico delle tappe precedenti

## M81 — Elemento Frame2D con rigidezza geometrica (24/10/2026)

**M75–M80 validati**: dopo M80-FIX3, l'utente ha confermato il superamento della regressione completa Windows .NET 8. **M81 implementato, test Windows ancora pendenti**: la rigidezza geometrica consistente per un Frame2D prismatico usa la forza assiale di riferimento con segno (trazione positiva, compressione negativa) e fornisce una tangente linearizzata materiale + iniziale. Il componente è isolato e introduce soltanto un dimostratore di mensola precompressa; **non è ancora disponibile un solver globale P–Δ** e non costituisce una verifica normativa. Test esclusivamente C#/xUnit con riferimento indipendente da quadratura Hermite, biforcazione di mensola e colonna; `scripts/run-m81-tests.ps1` pulisce `bin/obj` per prima cosa, compila in Release e avvia la regressione dei quattro progetti. [Nota tecnica M81](docs/structural/m81-frame2d-initial-stress.md).

## Archivio di avanzamento (stato al momento dei singoli milestone)

## M80-FIX3 — Parità del taglio minimo nel benchmark M78 (24/10/2026)

Il log Windows di M80-FIX2 conferma una **build Release senza avvisi né errori**, 17/17 benchmark geometrici M80 e 25/25 regressioni di cataloghi e unità. Il gate mirato M78/M79 ha superato **13 test su 14**, con un unico fallimento in `InteriorPointLoad_OneSidedShearAndExactMomentPeakArePreserved`: il test attendeva `x=2 m` per `Vmin=-3 kN`, ma il medesimo minimo vale lungo l'intero tratto `[2,8] m` e il finder restituisce deterministici `x=8 m / Continuous` (la fine asta è candidata prima del limite al carico). Il test è stato corretto e rafforzato con verifiche esplicite del salto `+9 → -3 kN` e della continuità del momento in `x=2 m`. Nessun sorgente FEM/API modificato. **M75–M80 restano da validare con il runner completo**. [Nota FIX3](docs/structural/m80-fix3-minimum-shear-tie.md).

## M80-FIX2 — Correzione di due test M78/M79 (Windows: 539/541 Analysis superati)

Il controllo per progetto introdotto da FIX1 ha individuato **due fallimenti su 541 test `Analysis.Tests`**. M78 presentava una parità fisica: con una sola forza concentrata il taglio massimo è costante su un intero tratto e, per convenzione deterministica, l'estremo restituito è quello iniziale `Continuous`, non necessariamente `BeforePointLoad`. Il test è stato adeguato alla regola effettiva e affiancato a un caso **con salto e massimo univoco su ciascun lato**. M79 costruiva la combinazione invertita con un fattore nullo `Q=0`, già proibito dal validatore del modello; il termine è stato omesso lasciando `G=-1`. Le formule, i solver, il validatore e le API pubbliche **non sono stati cambiati**. Il runner controlla M78/M79 prima della regressione completa e utilizza il logger testuale compatto, con log su file. **M75–M80 restano in attesa del gate completo Windows**; i 539/541 casi non equivalgono a validazione globale. [Nota diagnostica](docs/structural/m80-fix2-m78-m79-tests.md).

## M80-FIX1 — Esito dei test Windows e diagnostica dei runner

La regressione completa comprende **quattro assembly di test indipendenti**: Core, Analysis, Reporting e CLI. Un riepilogo «231 test superati» in `Cli.Tests` non certifica gli altri tre: se il codice di uscita complessivo è diverso da zero, la regressione **non è ancora confermata**. Per rendere individuabile l’eventuale errore, `scripts/run-m80-tests.ps1` esegue i quattro progetti in sequenza e mostra `PASSED`/`FAILED` per ciascuno e il percorso del log comune. Non sono state cambiate le analisi FEM o le prove C#. **M75–M80 attendono ancora la validazione cumulativa su Windows.**

## M80 — Geometria sezionale per acciaio e legno (test pendenti)

Abbiamo ampliato le proprietà delle sezioni senza complicare il motore FEM: a area e inerzia di flessione nel piano si affiancano un secondo momento d’inerzia fuori piano, due moduli elastici resistenti e due raggi d’inerzia. Sono grandezze geometriche necessarie alle future verifiche di sezioni e stabilità, **non** capacità resistenti normative. Sono disponibili rettangolo (anche con denominazione legno), cerchio pieno e cavo, tubolare rettangolare idealizzato e doppio T simmetrico idealizzato. Tutte le dimensioni dei calcoli diretti sono in metri.

Un nuovo catalogo JSON opzionale accetta dimensioni in **millimetri** e conserva stato/provenienza dei dati. I profili di esempio sono chiaramente dimostrativi, non IPE/HE commerciali; il catalogo storico `StructuralSection` e il solver restano invariati. [Nota tecnica M80](docs/structural/m80-section-geometric-properties.md) · [esempio geometrie](examples/sections/m80-parametric-example.json).

**Ultimo gate Windows confermato: M73.** M75–M80 sono implementati ma non ancora verificati sul tuo Windows. Il runner cumulativo sarà `scripts/run-m80-tests.ps1`. M81 affronterà il secondo ordine geometrico nel piano, senza anticipare verifiche NTC/Eurocodici né la GUI Avalonia.

## M79 — Inviluppi globali N/V/M e combinazioni governanti (test pendenti)

M79 riutilizza gli estremi **analitici** M78 di ogni risultato statico e li confronta tra **casi e combinazioni manuali esplicitamente selezionati**. Per ciascuna asta Frame2D restituisce minimo, massimo e massimo assoluto di N/V/M con ascissa, segno, lato del salto, identificatore e tipo dello scenario governante. Sono disponibili API .NET e esportazione CSV/Markdown opt-in, senza modifiche ai solver FEM e senza generazione automatica SLU/SLE. Non sono curve di inviluppo punto per punto né verifiche normative. [Nota tecnica M79](docs/structural/m79-exact-static-force-envelopes.md).

**M73 è l'ultimo gate Windows confermato; M75–M79 restano da compilare e verificare** con `scripts/run-m79-tests.ps1` (pulizia iniziale, test C# mirati e regressione completa). M80 affronterà le proprietà geometriche delle sezioni.

## M78 — Estremi analitici delle sollecitazioni (test pendenti)

Il precedente visualizzatore dei diagrammi N/V/M usa valori campionati, adatti al disegno ma non necessariamente sufficienti a trovare il massimo momento. **M78 introduce un secondo percorso di post-processing, basato su formule per tratti e sul calcolo delle ascisse critiche**, indipendente dal numero di campioni grafici. Tiene separati i limiti prima e dopo i carichi concentrati e riporta anche il caso o la combinazione cui appartiene ciascun estremo. L'accesso è disponibile via API C# opt-in; il normale riepilogo campionato resta invariato. Il confronto fra diversi scenari è ora disponibile separatamente nel servizio M79. [Nota tecnica M78](docs/structural/m78-exact-frame-force-extrema.md).

**M73 è l'ultimo milestone validato su Windows. M75–M79 attendono un gate cumulativo di compilazione e regressione** tramite `scripts/run-m79-tests.ps1`.

## M77 — Appoggi elastici e cedimenti imposti (test pendenti)

M77 estende la **statica lineare** alle molle nodali traslazionali e rotazionali, comprese le direzioni di appoggio inclinate. Le molle si assegnano ai supporti come rigidezze `StiffnessUx` e `StiffnessUy` (kN/m) e `StiffnessRz` (kNm/rad). Le reazioni elastiche sono recuperate esplicitamente; una molla non equivale a un vincolo rigido.

È inoltre possibile definire un **cedimento imposto per caso di carico** tramite `SupportSettlement`, destinato a una direzione effettivamente bloccata (`LocalX`, `LocalY` o `MomentZ`). I coefficienti delle combinazioni valgono anche per i cedimenti. Il solver distingue lo spostamento prescritto da una forza applicata, verifica la compatibilità di vincoli ridondanti e preserva i dati nel JSON standard. Il nuovo [documento M77](docs/structural/m77-elastic-supports-and-settlements.md) illustra equazioni, unità, limiti e benchmark analitici C#.

Il supporto è limitato alle analisi **statiche elastiche di primo ordine**; gli altri solutori non sono stati estesi e richiederanno un gate distinto. Non esistono ancora verifiche NTC/Eurocodici. **M75, M76 e M77 attendono il test di compilazione e regressione su Windows.**

## M76 — Peso proprio esplicito nelle analisi statiche (test pendenti)

Abbiamo implementato il **peso proprio degli elementi Frame2D e Truss2D**, anche quando convivono nello stesso modello. Indicando una singola azione `SelfWeight` nel caso di carico e il peso specifico dei materiali, il programma calcola il peso di ogni asta dalla sua **area × lunghezza × peso specifico**, applicandolo globalmente verso il basso. Per le travi viene mantenuta la natura distribuita del carico, per le reticolari si usano le equivalenti forze nodali: in questo modo restano coerenti reazioni, diagrammi, combinazioni e visualizzazioni.

Il calcolo non aggiunge peso proprio implicitamente: è una scelta esplicita dell'utente. Una sola azione per caso evita duplicazioni accidentali; fattori di combinazione restano liberi. Sono disponibili benchmark indipendenti **solo C#/xUnit**, un nuovo esempio JSON e uno script PowerShell con pulizia iniziale `bin/obj`. Il [documento M76](docs/structural/m76-automatic-self-weight.md) descrive convenzioni, limiti e confronti analitici.

**Stato:** M75 e M76 sono entrambi in attesa dei test Windows. M73 resta l'ultimo milestone numerico confermato dall'utente. **Il supporto M76 è limitato alla statica lineare**; peso proprio in transitorio e non linearità non sono stati introdotti.

## M75 — Primo avanzamento dopo la nuova roadmap

Abbiamo avviato il completamento del motore FEM con il **contratto di ingresso e salvataggio dei modelli**. M75 aggiunge la serializzazione JSON canonica del modello 2D, un preflight con diagnostica leggibile e un test che confronta il modello prima e dopo il salvataggio. Gli esempi e la CLI precedenti restano compatibili: questi servizi sono *opt-in*, quindi nessun solver lineare, dinamico o non lineare viene modificato.

Il modello standard ora è pronto per essere conservato e riaperto da futuri strumenti, ma **non conserva blocchi di configurazione specifici di analisi non rappresentati dal contratto standard**. Il [documento M75](docs/structural/m75-json-model-contract.md) spiega API, limiti, unità e convenzioni dei segni.

**Stato M75: implementato, in attesa di test .NET 8 sul tuo Windows.** M73 rimane l'ultimo gate numerico validato. Il percorso è ora proseguito con M76 (peso proprio) e M77 (appoggi e cedimenti), senza anticipare verifiche normative o interfaccia Avalonia.

## Cosa possiamo fare oggi

L'analisi **statica lineare** permette di modellare aste `Truss2D`, travi `Frame2D` e strutture miste, con materiali, sezioni, carichi nodali e distribuiti, vincoli anche inclinati e combinazioni di carico definite dall'utente. Calcoliamo spostamenti, reazioni e forze agli estremi degli elementi; per le travi sono disponibili i diagrammi di sforzo normale, taglio e momento. I risultati possono essere esportati in Markdown, CSV, XLSX, PDF e in rappresentazioni grafiche SVG/HTML, compreso un viewer interattivo.

Abbiamo poi esteso lo stesso modello alla **dinamica lineare**. L'analisi modale ricava frequenze naturali e forme di vibrazione, la risposta armonica studia ampiezze e sfasamenti a diverse frequenze e l'analisi transitoria segue il moto nel tempo usando Newmark. Sono disponibili anche verifiche del bilancio energetico e della convergenza temporale. L'analisi sismica esplorativa consente di applicare accelerazioni uniformi del terreno e spettri di risposta forniti dall'utente, ma non esegue verifiche normative.

La ricerca più recente riguarda il **comportamento elasto-plastico**. Abbiamo prima verificato un oscillatore non lineare a un grado di libertà (M62), poi una singola asta reticolare plastica (M63) e infine l'equilibrio incrementale di reticolari composte da più aste (M64). **M65 aggiunge in via sperimentale la dinamica non lineare Truss2D**, con massa consistente, Newmark–Newton e aggiornamento controllato degli stati plastici. Questa parte rimane interna al progetto: **i test C# di M65 e M66 sono stati superati**, ma non costituiscono una validazione d’impiego professionale. M66 ha esteso e validato i benchmark indipendenti. M67, già validato nei test C#, aggiunge in forma opzionale il recupero delle reazioni vincolari nel tempo e delle azioni elastiche di estremità degli elementi, sempre distinguendole dai diagrammi sezionali dinamici completi.

M68 introduce l’**accesso pubblico opt-in** a queste analisi non lineari: nuove richieste e risposte .NET, file JSON isolati, CLI e CSV/Markdown per storia plastica, reazioni e forza assiale. **M69**, superato nei test Windows dopo il FIX1, aggiunge un viewer HTML autonomo per esplorare deformazioni plastiche, forze assiali e storie di carico, mantenendo espliciti gli arresti non convergenti. Il collaudo grafico manuale degli HTML reali rimane distinto dai test automatici.

**M70 estende l’analisi sismica spettrale** già esistente: oltre alla combinazione SRSS, ora possiamo richiedere la **CQC** quando i modi di vibrare hanno frequenze ravvicinate. Il modello usa uno smorzamento modale uniforme inserito esplicitamente dall’utente e conserva i segni dei contributi modali prima della combinazione. Non modifica la risposta sismica temporale né calcola una verifica antisismica secondo normativa. La nuova funzionalità è [documentata nel dettaglio](docs/structural/m70-cqc-modal-combination.md) e **validata dai test .NET 8 confermati dall’utente**.

**Correzione M70-FIX1 validata:** il benchmark CQC a due campate ora vincola la traslazione trasversale del nodo intermedio, evitando un meccanismo nella fixture. Gli script PowerShell utilizzano `--tl:off` per disabilitare il logger interattivo di MSBuild. Da M71 i runner caricano anche un gestore comune dell’output .NET, che filtra le sequenze di ridisegno e salva un log testuale. [Dettagli della correzione](docs/structural/m70-fix1-cqc-benchmark-console.md).

**M72-FIX2 — pulizia dei warning xUnit:** corrette sei asserzioni nei test dei milestone M65–M68, utilizzando gli overload `Assert.Contains` e `Assert.Single` consigliati dagli analyzer. Nessuna modifica al motore di calcolo o ai test numerici sostanziali. **La compilazione e la regressione M71/M72 sono state superate su Windows**, secondo la conferma dell’utente. [Nota tecnica](docs/structural/m72-fix2-xunit-assertion-warnings.md).

**Nota storica M72-FIX1 (ora risolta):** la prima esecuzione M72 si è interrotta prima di compilare per un conflitto fra `-v:minimal` e la funzione PowerShell `dotnet`. I runner M71/M72 sono stati corretti e il wrapper ora tenta la decodifica UTF-8 temporanea per i caratteri accentati. **Entrambi hanno successivamente superato i test M72-FIX2**. [Dettagli tecnici](docs/structural/m72-fix1-powershell-arguments.md).

**M71 e M72 — flessione non lineare sperimentale.** M71 ha definito il modello teorico delle cerniere rotazionali concentrate, con benchmark C# successivamente superati. M72 aggiunge un **primo elemento Frame2D interno**, costituito da una trave elastica con una o due cerniere plastiche di estremità: equilibrio locale, tangente condensata, scarico e dissipazione, con nuovi benchmark C#. È un prototipo **validato nei test .NET 8 confermati dall’utente**, separato dal solver Frame2D lineare: non è disponibile nelle API pubbliche o nella CLI e non consente ancora l'analisi globale incrementale di telai elasto-plastici. [Dettagli e limiti M72](docs/structural/m72-first-concentrated-plastic-frame-element.md).

**M73 introduce una baseline prestazionale riproducibile in C#**: misura la crescita dei costi su matrici dense, reticolari statiche e dinamiche, ed elementi Frame2D a cerniere plastiche. I dati sono esportati in CSV/JSON con tempi, allocazioni, gradi di libertà e verifiche numeriche. Non abbiamo ancora ottimizzato o sostituito il backend denso: [limiti e metodo dei benchmark M73](docs/structural/m73-performance-baseline.md). **M73 ha superato la suite di test Windows confermata dall’utente.**

Per ogni tipologia di analisi, il software distingue esplicitamente ciò che è implementato da ciò che è stato verificato, e ciò che rimane sperimentale.

## Dove vogliamo arrivare

L'obiettivo non è accumulare funzioni senza controllo, ma far crescere una piattaforma modulare e affidabile per lo studio delle strutture 2D. Il percorso attuale è definito nella [roadmap di prodotto M74–M107](docs/structural/vision-and-roadmap-2d.md). La [roadmap M63–M75](docs/structural/roadmap-forward-m63-m75.md) rimane come archivio storico, non come piano futuro operativo.

| Tappa | Direzione di sviluppo | Stato al 23 ottobre 2026 |
|---|---|---|
| **M51–M66** | Fondamenti, dinamica lineare, non linearità Truss2D e benchmark | Test C# confermati |
| **M67** | Reazioni transitorie e azioni elastiche di estremità; aggregazione plastica interna | Test C# confermati |
| **M68** | API, JSON, CLI e report non lineari Truss2D | Test C# confermati |
| **M69** | Viewer HTML per analisi non lineari | Test C# M69-FIX1 superati; QA grafica manuale separata |
| **M70** | Combinazione CQC, diagnostica e reporting spettrale | Test .NET superati (FIX1) |
| **M71** | Studio delle cerniere plastiche flessionali | Test C# superati |
| **M72** | Elemento Frame2D con cerniere agli estremi (interno) | Test C# superati (FIX2); assemblaggio globale differito |
| **M73** | Profiling numerico C#, baseline CSV/JSON e accuratezza | Test C# superati |
| **M74–M107** | Consolidamento calcolo → verifiche normative → applicazione Avalonia | Nuova roadmap |

Il **gate M66** di validazione indipendente è stato superato nei test confermati dall’utente. M67, M68 e M69-FIX1 hanno superato i test C#; la CQC M70-FIX1 ha superato i test Windows; M71 e M72 hanno superato i test C# sul Windows dell’utente; M73 introduce un benchmark quantitativo validato nella suite sul PC dell’utente. Il collaudo visivo dei viewer rimane un’attività separata. Vogliamo inoltre completare gli inviluppi delle forze interne e delle reazioni nel tempo, rendere i risultati più facili da esplorare nei viewer e collegare il motore a un ambiente CAD senza legarne la matematica alla sua interfaccia.

**Non abbiamo ancora** una soluzione completa per telai elasto-plastici, effetti geometrici del secondo ordine, instabilità, rottura degli elementi, strutture 3D o verifiche di progetto secondo NTC/Eurocodici. Sono esclusioni tecniche precise, non funzionalità implicitamente presenti.

## Come provarlo

Serve il **.NET SDK 8**. È un progetto .NET e non richiede Python per compilare o eseguire i test C# attuali. Il codice è multipiattaforma laddove supportato da .NET; gli script di automazione forniti sono in PowerShell.

Dalla cartella principale del repository:

```powershell
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln -c Release
dotnet test StructuralSolver2D.sln -c Release
```

Per analizzare una trave semplicemente appoggiata in uno degli esempi inclusi:

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- analyze examples/beams/simple-supported-beam.json
```

Per generare un **viewer HTML** consultabile nel browser:

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- export-viewer examples/beams/simple-supported-beam.json reports/beam.html
```

Per esplorare le vibrazioni naturali di una mensola:

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- modal examples/dynamics/m57-modal-cantilever.json
```

Per recuperare reazioni dinamiche e azioni elastiche agli estremi delle aste su una storia di carico:

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- time-forces-report examples/dynamics/m59-time-cantilever.json reports/forze-m67.md
```

Le reazioni includono massa e smorzamento; le azioni di estremità sono la sola parte elastica nodale e non equivalgono a diagrammi completi delle sollecitazioni interne durante il moto.

E per vedere l'analisi temporale di una mensola caricata da un impulso di durata finita:

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- time-viewer examples/dynamics/m59-time-cantilever.json reports/time.html
```

Per esplorare la storia di un'asta elasto-plastica nei nuovi viewer offline:

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- nonlinear-static-viewer docs/structural/fixtures/m68-nonlinear-static.json reports/nonlinear-static.html
dotnet run --project src/StructuralSolver2D.Cli -- nonlinear-time-viewer docs/structural/fixtures/m68-nonlinear-time.json reports/nonlinear-time.html
```

Le deformate sono amplificate soltanto nella rappresentazione: gli spostamenti, le forze e la plasticità provengono dal solver. Le analisi interrotte vengono contrassegnate come parziali.

La cartella [`examples`](examples/README.md) contiene modelli già pronti, compresi casi per la validazione numerica. Non tutti gli esempi dinamici includono casi di carico statici: i relativi comandi sono volutamente separati.

### Test e pulizia delle build

Per ogni milestone è presente uno script `scripts/run-mXX-tests.ps1`. **Ogni script di test esegue prima** [`scripts/clean-project-build.ps1`](scripts/clean-project-build.ps1), che rimuove esclusivamente le cartelle `bin` e `obj` appartenenti ai progetti .NET, quindi ricompila e avvia i test mirati e l'intera regressione. In questo modo si evita di testare per errore DLL compilate da una versione precedente.

Per il milestone M69 (già testato):

```powershell
.\scripts\run-m69-tests.ps1
```

È possibile verificare quali cartelle verrebbero eliminate, senza modificarle:

```powershell
.\scripts\clean-project-build.ps1 -WhatIf
```

I benchmark introdotti a partire da M64 usano **C# e xUnit**, senza interpreti o verifiche Python opzionali da installare. Per ottenere la baseline delle prestazioni M73 e i relativi CSV/JSON:

```powershell
.\scripts\run-m73-tests.ps1
```

Questa esecuzione comprende la pulizia iniziale, la compilazione integrale, i test numerici e la misurazione in Release. I tempi non sono soglie di promozione: vanno confrontati sulla stessa macchina.

## Unità, significato e responsabilità dei risultati

Il sistema usa unità coerenti **kN–m–s**: lunghezze in metri, forze in kN, momenti in kN·m, moduli elastici in kN/m² e masse coerenti con la dinamica. Ad esempio, se l'asse globale Y è rivolto verso l'alto, un carico verticale verso il basso è generalmente rappresentato da un valore `GlobalY` **negativo**. Nella dinamica, massa e peso non devono essere confusi.

Le deformate modali nei viewer sono normalizzate o amplificate per rappresentazione; non rappresentano spostamenti fisici sotto un particolare carico. Al contrario, le ampiezze della risposta armonica e gli spostamenti temporali sono risultati nelle unità fisiche previste dai rispettivi modelli.

Il software è destinato a **ricerca, apprendimento e prototipazione**. Nessun risultato costituisce di per sé un'attestazione di sicurezza strutturale. Per applicazioni professionali o decisioni che coinvolgono la sicurezza delle persone sono necessarie verifiche indipendenti, giudizio ingegneristico e controlli secondo la normativa applicabile.

## Architettura e documentazione

La soluzione separa i contratti dei modelli (`Core`), gli algoritmi numerici (`Analysis`), le esportazioni (`Reporting`) e i comandi (`Cli`). Questa indipendenza permette di sviluppare nuovi metodi senza compromettere il comportamento dell'analisi statica già verificata.

Per approfondire, sono disponibili il [modello strutturale](docs/structural/model.md), la [guida al JSON](docs/structural/json-input.md), l'[API pubblica](docs/structural/public-api.md), il [Validation Manual](docs/structural/validation-manual.md), la [teoria del metodo matriciale](docs/theory/matrix-method.md) e la [roadmap](docs/structural/roadmap.md). Le decisioni sui limiti di M65 sono in [M65 — dinamica non lineare](docs/structural/m65-nonlinear-truss-dynamics.md).

## Licenza

Il progetto è distribuito sotto [GNU General Public License v3.0](LICENSE). Codice e documentazione sono disponibili per essere studiati e migliorati nel rispetto della licenza.

Lo sviluppo più recente è **M73 — misurazione riproducibile delle prestazioni e della stabilità numerica**, implementato in C# e validato nei test Windows. M71 e M72 sono stati validati sul PC dell’utente, ma la plasticità Frame2D rimane un prototipo di elemento, non un solver globale di telai. Dopo M73, la nuova sequenza è descritta nella [visione e roadmap 2D](docs/structural/vision-and-roadmap-2d.md): prima calcolo, poi verifiche, infine editor C#/Avalonia; l’assemblaggio non lineare globale Frame2D resta una capacità distinta, da pianificare e verificare. Vedi lo [studio M71](docs/structural/m71-frame2d-plasticity-feasibility.md) e la [nota tecnica M72](docs/structural/m72-first-concentrated-plastic-frame-element.md).

Gli script di test ora inviano l'output di `dotnet` attraverso una console testuale progressiva e conservano un log temporaneo, evitando le barre che riscrivono le righe. Restano da verificare sul Windows dell'utente: [dettagli sulla console](docs/structural/m71-console-streaming.md). Per la verifica più recente: `.\scripts\run-m72-tests.ps1` (comprende i test M71). Come sempre, **la prima operazione elimina `bin` e `obj`** dei progetti.
