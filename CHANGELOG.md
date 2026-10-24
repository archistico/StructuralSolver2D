# Changelog — StructuralSolver2D

Registro **sintetico e cronologico**. Ogni stato «gate superato» è basato sulle conferme dei test Windows dell'utente, non su una certificazione della conformità normativa dell'opera. I dettagli delle milestone si trovano in `docs/structural/` e nell'[archivio](docs/archive/README.md).

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
