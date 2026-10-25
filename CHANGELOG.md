# Changelog — StructuralSolver2D

Registro **sintetico e cronologico**. Ogni stato «gate superato» è basato sulle conferme dei test Windows dell'utente, non su una certificazione della conformità normativa dell'opera. I dettagli delle milestone si trovano in `docs/structural/` e nell'[archivio](docs/archive/README.md).

## 25 ottobre 2026 — P0-C2A: estremi analitici di metriche diagnostiche affini (gate Windows in attesa)

- Conferma dell'utente **«test passati»** per C1 FIX1: segnato **P0-C1 GATE PASS** senza log numerico della nuova esecuzione; i 527/528 del precedente FIX0 restano storico.
- Nuovo componente separato `FemAffineUtilizationExtremaC2A` in Verification.Integration: sui **soli** Frame2D con tratti a sollecitazioni affini, massimizza due indicatori matematici (`|N|/Ns+|V|/Vs+|M|/Ms` e `|(N/Ns)(M/Ms)|`), conservando i due lati di carichi puntuali. Sum-abs convessa → estremi; prodotto quadratico → estremi + stazionari analitici.
- Carichi uniformi, linearmente distribuiti e peso proprio del modello attivi → **`ScreeningIncomplete`**, nessun massimo. Indicatori/scale **NON sono formule/resistenze EC3/EC5/NTC**: nessun risultato di idoneità di membro, struttura o norma.
- Test xUnit nuovi con golden manuale P=12 kN a L/2, N=-9 kN, `ηmax=3` su entrambe le facce del punto; combinazioni con fattori negativi, più salti, matematica prodotto con massimo interno, rigetti distribuiti/selfweight/truss/invalidi. SDK .NET assente qui, **gate Windows C2A richiesto**; Core/Analysis, esempi G8B, vecchi test/golden e README invariati.

## 25 ottobre 2026 — P0-C1 FIX1: corregge test sul lato del carico puntuale (gate da ripetere)

- Analizzato log Windows: compilazione Release **0 avvisi/0 errori**; 3/3 Analysis G7, 3/3 integrazione G7, **527/528** Verification. Unico errore: test nuovo `AContinuousSectionAtAConcentratedMemberLoad_CannotBeValidated` attendeva `Invalid` ma otteneva `MatchedDeclaredStations`.
- Causa: il test modificava la stazione **indice 0, x=0, già Continuous**, anziché la **indice 1, x=2 m, BeforePointLoad**. La simulazione M87-FEM rifiuta già correttamente un punto di salto richiesto come `Continuous`: nessun difetto riscontrato nel solver o nell'auditor.
- Corretto il test per selezionare il salto e aggiunte asserzioni di geometria/lato e una regressione positiva per la sezione continua a x=0. **Soltanto file test + documentazione/manifest modificati**. Nessun golden, FEM, Core/Analysis, C1 produzione, G8B o README alterato.
- Gate Windows **P0-C1 FIX1 in attesa**; non dedurre `Passed` da una compilazione riuscita o dalla suite parziale.

## 25 ottobre 2026 — P0-C1: audit terne FEM concomitanti (gate Windows in attesa)

- L'utente ha confermato **«test passati»** per P0-B4: registrato **P0-B4 GATE PASS** senza log numerico allegato; nessuna promozione dell'esito a completezza normativa globale.
- In `Verification.Integration` aggiunto `FemConcomitantStationAuditorC1`: piano finito di stazioni/casi/combinazioni/lati/regole/edizione e controllo delle richieste M87-B/C contro il **FEM ricalcolato** una volta per target. Conferma N/V/M firmati **della stessa sorgente** e rifiuta mescolanza degli inviluppi.
- Aggiunti xUnit con combinazioni multiple, fattori negativi, salti V, mancanze, duplicati, manomissioni N/V/M e mutazioni di provenienza; controesempio `η(x)=|x(1−x)|` con massimo interno. **Non prova massimo utilizzo continuo né idoneità dell'asta**.
- Documento `P0_C1_AUDIT_SOLLECITAZIONI_CONCOMITANTI.md`; README invariato, nessun cambio ad Analysis/Core, G8B, benchmark G7 o coefficienti normativi. Nuovi test .NET **in attesa di Windows** per assenza SDK nell'ambiente di assemblaggio.

## 25 ottobre 2026 — P0-B4: audit di piano dichiarato, fingerprint FEM (gate Windows in attesa)

- L'utente ha confermato **«tutti i test passati»** per P0-B3: segnato **P0-B3 GATE PASS**, senza conteggi del log allegato. G7 e B2 restano superati nel proprio perimetro; G8B non diventa un certificato di verifica.
- Aggiunto in Combinations/Audit un piano finito per **un effetto dichiarato**, validator per famiglie, azioni classificate, guide Q, vento alternativo, assenze/favorevolezze motivate; `Capture` congela fattori attesi e SHA-256 di inventario, piano, geometria, sezioni, materiali, vincoli, carichi e termini FEM.
- `Audit` legge **direttamente** `StructuralModel.LoadCombinations`, controlla termini reali, duplicati, assenti o imprevisti e fingerprint cambiati; stati `Invalid`, `Incomplete`, `Unsupported`, `MatchedDeclaredPlan`, **mai completezza normativa o idoneità della membratura**.
- Aggiunti xUnit B4 positivi, scenari vento ± e mutazioni negative; manuale `P0_B4_AUDIT_INSIEME_DICHIARATO.md`. Python golden P0-B1 riconfermato **14/14 + 10/10**. **SDK .NET assente in preparazione: i test B4 sono nuovi e in attesa di gate Windows.** Solver Core/Analysis, G8B, README e benchmark immutati; manifest FULL SHA256 rigenerato.

## 25 ottobre 2026 — P0-B3: generatore NTC di combinazioni candidate (gate Windows in attesa)

- Ricevuta conferma dell'utente «test passati» relativa al checkpoint P0-B2: registrato **P0-B2 GATE PASSED** per le prove richieste, senza conteggi/trace del log. Conservata la distinzione dal completamento normativo e dal confronto G8B.
- Implementati Ntc2018BuildingActionCoefficients (γ STR A1 e tutte le 12 righe determinate ψ NTC 2018) e Ntc2018BuildingCombinationGenerator (ULS STR A1, SLS rara/frequente/quasi permanente) in Combinations, senza dipendenze inverse dal FEM.
- Input obbligatorio di scenario/effetto e scelta favorevolezza documentata; alternative incompatibili rifiutate, termini zero omessi nel manifesto, omissioni fisiche/non selezionate distinte. Ogni risultato resta un **candidato, FullCombinationSetAudited=false**.
- Aggiunti xUnit B3 che leggono **senza modifiche** i 14 golden P0-B1 copiati nell'output test e altri casi negativi; documentato P0_B3_GENERATORE_COMBINAZIONI_CANDIDATE. README e modelli FEM invariati. **B3 non compilato/testato qui**: SDK .NET assente, gate Windows richiesto prima di P0-B4.

## 25 ottobre 2026 — P0-B2: modulo C# azioni tipizzate (gate Windows in attesa al momento della consegna)

- Aggiunto StructuralSolver2D.Combinations (.NET 8, sola dipendenza verso Core): ActionDefinition G1/G2/Q, categorie ψ NTC 2018, base caratteristica, evidenze, alternative vento±, durata legno opzionale e deroga G2 motivata.
- Aggiunti ActionInventoryValidator (stati Valid/Incomplete/Unsupported/Invalid, codici B2_*) e ActionInventoryJsonSerializer con JSON autonomo, enum stringa e rifiuto campi sconosciuti. Nessuna generazione SLU/SLE né modifica alla semantica della CLI o dei fattori manuali.
- Integrato il progetto nella soluzione e referenziato da Core.Tests; nuovi xUnit per caso positivo, round-trip, omissioni, duplicati, categoria I/K, neve senza quota, gruppi e regressione fattori zero/negativi. Documento tecnico P0_B2_CONTRATTO_AZIONI_E_VALIDAZIONE.md.
- SDK .NET non disponibile nell'ambiente di preparazione: **nessuna compilazione o esecuzione xUnit dichiarata**. P0-B1 Python confermato; G7 ancora ultimo gate Windows storico confermato. Handoff/roadmap/manifest aggiornati, README immutato. Prima il gate Windows B2/G7, poi P0-B3.

## 25 ottobre 2026 — P0-B1: specifica NTC 2018 e golden indipendenti (non gate .NET)

- Consultata la fonte primaria D.M. 17/01/2018: §2.5.3, Tab. 2.5.I, §2.6.1 Tab. 2.6.I. Specificato il profilo delimitato SLU STR A1 per edifici correnti non geotecnici e SLE rara/frequente/quasi permanente; EQU/GEO/sismici/eccezionali esclusi.
- Aggiunto docs/structural/P0_B1_CONTRATTO_AZIONI_NTC2018.md: identità e categoria azione, guide/accompagnatrici, vento e alternative, favorevolezza rispetto all'effetto, gamma/psi e omissioni tracciate senza termini zero nel FEM legacy.
- Aggiunti benchmarks/independent/p0b: **14/14 golden manuali e 10/10 controlli negativi superati** dal verificatore Python Decimal, senza dipendenze esterne; nessun confronto con generatore C# ancora inesistente.
- Aggiornati handoff, roadmap, messaggio di ripresa e manifest SHA256; README invariato. Nessuna modifica a sorgenti/test .NET, quattro modelli G8B o benchmark esistenti; **G7 resta l'ultimo gate Windows confermato**. Segue P0-B2, poi B3/B4.

## 25 ottobre 2026 — P0-A: audit tecnico documentale completato

- Esaminati sorgenti e test reali del FEM, carichi/combinazioni, bridge, EC3/EC5/NTC, cataloghi, reporting e G8B. Creati i tre documenti P0_A_AUDIT_TECNICO, P0_A_MATRICE_COPERTURA e P0_A_PRIORITA_E_CRITERI_DI_TEST in docs/structural, con stato, rischio, prove e ticket P0-B/C/E/D.
- Gap prioritari: generatore SLU/SLE assente; fattore zero rifiutato come termine FEM ma conservabile nel manifesto G1; massimo di utilizzo non garantito dalle stazioni candidate; verifiche parziali; ritegni e collegamenti fisici esterni.
- Ricalcolati nel riferimento Python/NumPy G8B **16/16 scenari** senza discrepanze nei campi di spostamenti, reazioni e risultanti rispetto al dataset. Nessun confronto C#↔Python o nuovo gate .NET (SDK assente).
- Aggiornati handoff, roadmap, piano e manifest. README invariato; nessuna modifica a codice, test, benchmark, modelli, cataloghi, script o VERSION. Prossimo P0-B; Avalonia soltanto P3.

## 25 ottobre 2026 — P0-HANDOFF: preparazione nuova chat e avvio P0-A (solo documentazione)

- Confermato nuovamente dall'utente **`G7 GATE PASSED`** dopo la sequenza G8B FIX2; la conferma riguarda la suite G7, **non** l'esecuzione documentata dei 16 report e del confronto numerico G8B.
- Riscritto `HANDOFF_NUOVA_CHAT.md` con versione, architettura, gate, G8A/G8B, incidenti FIX1/FIX2, regole normative, limitazioni e istruzioni di ripresa.
- Aggiunto `docs/structural/P0_A_PIANO_OPERATIVO.md`: audit del codice e della copertura di FEM/EC3/EC5/NTC, matrice dei gap e criteri per P0-B…E.
- Aggiunto `docs/NUOVA_CHAT_MESSAGGIO.txt` da incollare nella conversazione successiva; aggiornata la roadmap con **P0-A come prossima attività**, senza aggiunte al motore.
- Nessuna modifica a sorgenti, test, benchmark, esempi, cataloghi, script o `VERSION`; rigenerato il manifest SHA-256. **Non è un nuovo gate .NET.**

## 25 ottobre 2026 — G8B FIX2: migrazione sicura dei JSON obsoleti (gate Windows da ripetere)

- Il secondo log Windows ha mostrato gli stessi **due fallimenti CLI**: FIX1 era corretto, ma l'estrazione sopra la cartella G8B precedente non aveva eliminato i vecchi file in `examples/`.
- Aggiunto `scripts/repair-g8b-legacy-examples.ps1`, eseguito **prima della compilazione** da `scripts/run-g7-tests.ps1`: riconosce per struttura JSON soltanto i due file di riferimento obsoleti, li sposta in `.g8b-legacy-backup/` senza sovrascrivere backup e non tocca alcun modello valido. Lo script e' idempotente.
- Nessuna modifica a motore FEM, test C#, benchmark numerici, sezioni, carichi o combinazioni; `README.md`, guida G8B, handoff e manifest aggiornati. Il gate G8B resta **in attesa del nuovo test Windows**.

## 25 ottobre 2026 — G8B FIX1: file numerici separati dai modelli (gate Windows da eseguire)

- Corretto il difetto emerso dai **2 test CLI** del log Windows: due JSON di benchmark analitici in `examples/mixed/g8b-house/` venivano enumerati erroneamente come modelli con nodi/aste.
- Spostati i due JSON e il CSV in `benchmarks/independent/g8b-house/`; aggiornato il generatore Python perché scriva nella nuova destinazione e non ripresenti il problema.
- Aggiornata la documentazione della casetta e l'handoff. **Nessuna modifica** a sorgenti .NET, test C#, quattro modelli di input, coefficienti o benchmark precedentemente validati.
- Log originale: build Release 0 errori/0 avvisi, suite FEM e verifiche superate; CLI 241/243 con 2 fallimenti dovuti esclusivamente ai file non-modello. **FIX1 da ricollaudare su Windows**.

## 24 ottobre 2026 — G8B: quattro modelli casetta e confronto elastico (gate Windows da eseguire)

- Tre varianti controventate con sezioni A leggera, B riferimento e C robusta; quarta variante B priva di X, per isolare l'effetto della controventatura. Geometrie, carichi d'uso e neve/vento identici; peso proprio esplicito diverso con A e sezioni.
- Controventi X `Truss2D` **bilaterali** nel campo 0–5 m; non simulano tiranti a sola trazione, nodi/connessioni o instabilità a compressione.
- Dati CSV/JSON dei pesi propri e delle risultanti; seconda analisi elastica Euler–Bernoulli in Python/NumPy riproducibile, dichiarata **indipendente ed esplorativa**, non gate software.
- Runner `scripts/run-g8b-house-demo.ps1` per 16 report e 2 viewer; guida `docs/structural/examples/G8B_VERIFICA_CASSETTA_E_CONFRONTO.md`; `HANDOFF_NUOVA_CHAT.md` aggiornato. Nessuna modifica a `src/`, test G7, cataloghi o benchmark preesistenti.

## 24 ottobre 2026 — G8A: esempio casetta acciaio-legno (modello dimostrativo)

- Aggiunto l'esempio `examples/mixed/two-storey-steel-house-timber-roof.json`: telaio a 2 piani con colonne a interassi `5 + 2 + 5 m`, interpiano `3 m`, balconi a sbalzo `1,2 m` al primo piano e copertura inclinata in legno.
- Inclusi carichi permanenti, variabili, neve, vento laterale e sollevamento di copertura, con combinazioni manuali `ULS_GRAVITY`, `ULS_WIND_POS`, `ULS_WIND_NEG`, `SLS_RARE`.
- Aggiunta guida `docs/structural/examples/two-storey-steel-house-timber-roof.md` con ipotesi di carico, sezioni preliminari e workflow di dimensionamento.
- Aggiornato `examples/README.md` e il manifest SHA-256. **Nessuna modifica** al motore FEM o ai moduli normativi; collazione utile per mostrare il flusso di lavoro su una struttura realistica.

## 24 ottobre 2026 — G7-DOC1: riordino documentale (solo documentazione)

- Riscritto `README.md` come presentazione e guida d'uso, eliminando avvisi scaduti, duplicazioni e note di rilascio accumulate.
- Consolidato lo stato dei gate fino a **G7 GATE PASSED**; introdotto [`HANDOFF_NUOVA_CHAT.md`](HANDOFF_NUOVA_CHAT.md) come riferimento operativo da mantenere aggiornato.
- Sostituiti i checkpoint storici in cima alla [roadmap](docs/structural/roadmap.md) con priorità reali post-G7; archiviate le versioni precedenti senza perderne la storia.
- Spostate le note di release nella documentazione storica e le guide operative G6/G7 in `docs/structural/`.
- **Nessuna modifica** a `src/`, `tests/`, `benchmarks/`, `catalogs/`, `scripts/`, `examples/` e `tools/`; nuove prove .NET non necessarie per questa operazione documentale, ma manifest aggiornato da verificare sul PC.

## 24 ottobre 2026 — G7 (gate Windows superato)

- Benchmark FEM **B13/B14**: trave con sbalzo, carichi a tratti e reazione negativa sul vincolo bilatero.
- Test integrati reali **G1 → G2/G3 → G6**: coefficienti e risultati normativi con audit di copertura conservativo.
- Manuale di validazione e valori numerici indipendenti: `docs/G7_MANUALE_VALIDAZIONE_INDIPENDENTE.md`, `benchmarks/G7_INDEPENDENT_GOLDEN.json`.
- Il gate superato **non prova** contatto unilaterale, generazione completa delle combinazioni o conformità dell'asta.

## 24 ottobre 2026 — G6 (gate Windows superato)

- Audit dei risultati legno NTC 2018 G2–G5 su piano esplicito di controlli SLU/SLE.
- Differenza obbligatoria tra verifiche **mancanti/non supportate** e **fisicamente non applicabili**; provenienza esatta e prevalenza dei fallimenti.
- Audit acciaio EC3 M91-D indipendente; nessuna falsa conformità complessiva.

## 24 ottobre 2026 — G5 (gate Windows superato)

- SLE legno: frecce istantanee, combinazione rara e deformazioni finali con `kdef`, `ψ0`, `ψ2` e provenienza delle azioni.
- Limiti di deformabilità esplicitamente adottati; verifiche parziali.

## 24 ottobre 2026 — G4 (gate Windows superato)

- Instabilità colonne in compressione pura (due assi) e svergolamento delle travi, con vincoli e tensioni critiche documentati esternamente.

## 24 ottobre 2026 — G3 FIX1 / G3 (gate Windows superato)

- Tensoflessione e pressoflessione NTC del legno, controllo separato del taglio concomitante.
- G3 FIX1 ha corretto una **asserzione xUnit sul testo della traccia numerica**, non le formule di produzione.

## 24 ottobre 2026 — G2 e G1 FIX1 (gate Windows superati)

- **G2:** quattro verifiche pure di sezione rettangolare in legno secondo NTC 2018.
- **G1:** selettore autonomo NTC 2018 (`γM`, `kmod`, `kcr` quando previsto), rigorosamente distinto da EC5.
- **G1 FIX1:** corretto un test che si aspettava `NotApplicable` invece del corretto `Unsupported` per normativa incompatibile.

## 24 ottobre 2026 — Milestone precedenti

- **M92-A/B:** verifiche pure EC5 e selezione coefficienti EC5 — gate Windows superati nel perimetro dichiarato.
- **M89–M91:** moduli parziali acciaio EC3, classificazione sezioni, instabilità e audit; regole con ambito limitato.
- **M86:** consolidamento e validazione del core FEM elastico; le milestone M51–M85 includono analisi statiche, dinamiche e prototipi avanzati delimitati.
- Riferimenti storici: [`CHANGELOG_PRE_G7.md`](docs/archive/CHANGELOG_PRE_G7.md) e [note archiviate](docs/archive/release-notes/).

## Regole per le prossime consegne

Ogni incremento aggiorna **questa cronologia (poche righe)**, [roadmap](docs/structural/roadmap.md) se le priorità cambiano e **sempre** [`HANDOFF_NUOVA_CHAT.md`](HANDOFF_NUOVA_CHAT.md). Lo storico dettagliato resta in un documento tecnico della milestone, non nel README.
