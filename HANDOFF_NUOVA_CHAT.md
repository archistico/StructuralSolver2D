# StructuralSolver2D — HANDOFF NUOVA CHAT

**Checkpoint operativo: P0-HANDOFF (25 ottobre 2026)**  
**Origine:** progetto FULL G8B FIX2; **ultima suite confermata dall'utente:** `G7 GATE PASSED`.  
**Prossima attività concordata:** **P0-A — audit tecnico completo del FEM, delle verifiche normative e dei limiti fisici**, prima di introdurre nuove funzioni.  
**Natura di questa consegna:** *solo documentazione*. Non implica un nuovo gate software e non costituisce attestazione di conformità delle strutture.

> **Istruzione essenziale per la nuova chat:** leggere tutto questo documento, poi `docs/structural/P0_A_PIANO_OPERATIVO.md`, la roadmap, il README e il changelog. Esaminare **il codice effettivo** prima di affermare che una capacità esiste. Non ricominciare da G1, non rifare G8B e non partire dalla GUI. Il lavoro successivo è P0-A.

## 1. Obiettivo, ambito e vincoli inderogabili

StructuralSolver2D è un progetto open source in **C# / .NET 8**, destinato a studenti, docenti e professionisti per didattica, ricerca e valutazioni preliminari di **strutture piane**: travi, reticolari e telai **in acciaio e legno**.

Ordine di sviluppo vincolante:

1. **Motore FEM 2D corretto, testato e documentato**: analisi, carichi, vincoli, reazioni, N/V/M, postprocessing, benchmark e casi patologici.
2. **Verifiche normative separabili dal calcolo**, con provenienza delle azioni, categorie, combinazioni SLU/SLE e copertura realmente dimostrabile; NTC 2018/Circolare 2019 e versioni dichiarate di EC3/EC5.
3. **GUI leggera C#/Avalonia soltanto alla fine**: editor di nodi, aste, vincoli, carichi, combinazioni e consultazione risultati; non introdurre UI mentre mancano affidabilità e copertura.

**Fuori ambito per decisione di prodotto:** FEM 3D, cemento armato, muratura, piastre/pareti, X-LAM, BIM, progettazione esecutiva generale dei nodi/collegamenti. Non interpretare un vecchio file didattico in calcestruzzo come autorizzazione a includere il c.a.

Un esito positivo di **una singola regola** o di **una singola sezione** non significa «asta conforme» e, a maggior ragione, non significa «edificio verificato».

## 2. Baseline: cosa prendere in carico e cosa è veramente passato

- **Archivio FULL consigliato per proseguire dopo questo handoff:** `StructuralSolver2D_P0_HANDOFF_2026-10-25_FULL.zip`, che è G8B FIX2 con *soltanto* handoff, roadmap, changelog e piano P0-A aggiornati; nessun sorgente, test, modello FEM, script o riferimento numerico modificato.
- **Archivio tecnico precedente da cui deriva:** `StructuralSolver2D_G8B_FIX2_REPAIR_ESEMPI_RESIDUI_2026-10-25_FULL.zip`.
- **Ultima baseline algoritmica collaudata:** G7. L'utente ha ribadito **«G7 GATE PASSED» anche dopo la sequenza G8B FIX2**; questa è la conferma della suite G7, non una certificazione del confronto numerico G8B e non un nuovo gate nominato G8B.
- **G8B:** modelli e script di confronto predisposti. I **16 report della casetta, i 2 viewer e il confronto valore per valore con il riferimento Python** non hanno ancora una conferma esplicita dell'utente. Non dichiararli validati senza output.
- **VERSION:** `0.1.0`; nessuna modifica di versione semantica per documentazione o demo.
- **Ambiente dell'assistente che ha assemblato gli ZIP:** privo di SDK `dotnet`. I test .NET sono eseguiti **su Windows dall'utente**. Non attribuire agli script test risultati mai eseguiti qui.

### Gate confermati nel rispettivo perimetro

| Fase | Stato | Portata effettiva |
|---|---|---|
| M86 e precedenti | Gate storici confermati | Motore FEM 2D e numerosi moduli collaterali, entro i test esistenti |
| M89–M91 | Gate storici confermati | Verifiche **parziali** dell'acciaio EC3, cataloghi/classi/instabilità, auditor M91-D |
| M92-A/B | Gate confermati | Verifiche pure e coefficienti del legno EC5 in ambito delimitato |
| G1 FIX1 | **PASS** | Selettore coefficienti nativo legno NTC2018 e separazione da EC5 |
| G2 | **PASS** | Resistenze pure di sezioni rettangolari in legno NTC2018 |
| G3 FIX1 | **PASS** | Interazioni N–M e taglio concomitante alla medesima stazione |
| G4 | **PASS** | Verifiche parziali di instabilità con ipotesi e dati esterni |
| G5 | **PASS** | SLE legno: frecce istantanee/finali, kdef e coefficienti ψ nei casi supportati |
| G6 | **PASS** | Auditor di copertura **dichiarata** legno NTC; distingue mancante/non supportato/non applicabile |
| G7 | **PASS** | Benchmark B13/B14 e test integrazione G1→G2/G3→G6 + regressioni |
| G7-DOC1 | Solo documentazione | README, roadmap, changelog e handoff razionalizzati |
| G8A/G8B FIX2 | Esempi e manutenzione | Casetta e dati indipendenti; suite G7 confermata, **confronto dedicato G8B ancora da completare** |
| P0-A | **Non iniziato** | Prossimo lavoro |

## 3. Architettura: directory da conoscere

| Percorso | Ruolo e cautela |
|---|---|
| `src/StructuralSolver2D.Core/` | Modelli di geometria, nodi, aste, vincoli, materiali, sezioni, carichi e contratti |
| `src/StructuralSolver2D.Analysis/` | Risolutore FEM 2D, analisi e postprocessing; non confondere esperimenti avanzati con validazione generale |
| `src/StructuralSolver2D.Catalogs/` | Cataloghi materiali/profili e geometrie; provenienza e approssimazioni da verificare |
| `src/StructuralSolver2D.Verification/` | Motore comune, contratti, esiti (`Passed`, `Failed`, `Unsupported`, ecc.) |
| `src/StructuralSolver2D.Verification.Steel/` | Regole **parziali** EC3 M89–M91, stabilità e audit M91-D |
| `src/StructuralSolver2D.Verification.Timber/` | Regole EC5 M92 e NTC2018 G1–G6 |
| `src/StructuralSolver2D.Verification.Integration/` | Collegamento tra FEM e verifiche, da ispezionare in P0-A |
| `src/StructuralSolver2D.Reporting/` | Report, grafici e formati di esportazione |
| `src/StructuralSolver2D.Cli/` | CLI `analyze`, `report`, `export-viewer` e altri comandi |
| `tests/` | xUnit e test di regressione separati per modulo |
| `benchmarks/` | Riferimenti indipendenti, golden e casi storici; non confondere con `examples/` |
| `examples/` | Modelli JSON analizzabili: i test CLI li percorrono **ricorsivamente** |
| `scripts/` | Script Windows per build, test, riparazioni mirate e report |
| `docs/structural/` | Documentazione tecnica e roadmap; `docs/archive/` è storico, non stato corrente |

Il motore di base usa **Euler–Bernoulli** per `Frame2D` e `Truss2D` per aste assiali. La rigidezza lineare 2D **non** prova instabilità fuori piano, risposta tension-only, contatto monolatero o verifiche dei collegamenti. M82 ha un ambito di secondo ordine **linearizzato**, non è una soluzione non lineare generalizzata. Le unità di input del FEM sono coerenti con **kN–m–s**; `GlobalY` positivo punta in alto, dunque i carichi gravitazionali hanno segno negativo.

## 4. Prescrizioni tecniche da non perdere

- Fonti normative dichiarate: **NTC 2018**, **Circolare n. 7/2019**, EC3 **EN 1993-1-1:2005+A1:2014**, EC5 **EN 1995-1-1:2004+A1:2008+A2:2014**. Non sostituire automaticamente versioni più recenti; distinguere norma, commento tecnico, approssimazione didattica e input esterno.
- Manuale **Ballio–Mazzolani–Bernuzzi–Landolfo, _Strutture di acciaio — Teoria e progetto_**, seconda edizione (2020): tre letture L1/L2/L3 hanno prodotto esempi e ipotesi di test, riflessi in `benchmarks/G6_COVERAGE_REFERENCE_CROSSWALK.json` e nel manuale G7. Fonte **secondaria**, mai superiore a NTC/Eurocodici. La sua equazione 9.43 contiene un **possibile refuso** sul fattore di imperfezione: verificare sulle norme.
- Combinazioni manuali nel software ≠ **generatore normativo completo**. Gestire eventuali nuove combinazioni con categorie, ψ, γ, azioni favorevoli/sfavorevoli, casi guida/accompagnatrici e segni; G1 non li genera completamente.
- Una singola sezione FEM non copre automaticamente la **membratura intera**. Controllare stazioni critiche, limiti/discontinuità e lati, inviluppi, condizioni di carico e prove di completezza.
- Per interazioni N–M–V usare **effetti concomitanti** dello stesso caso/combinazione e punto; non comporre massimi indipendenti da inviluppi diversi.
- **Non applicabile** richiede giustificazione fisica; **non implementato** rimane gap, non «Passed». Una verifica fallita rimane tale anche se ci sono altre verifiche mancanti.
- Da dichiarare esplicitamente: ritegni fuori piano, instabilità flesso-torsionale e torsionale, collegamenti/fori/netto, diaframmi, azioni 3D, fondazioni, contatto, controventi tension-only, possibili deformazioni a taglio trascurate, effetti non lineari, sisma e completezza di combinazioni quando non supportati dal modello.
- Cataloghi e sezioni nominali, così come i carichi ipotizzati, non costituiscono certificati di prodotto o progetto esecutivo.

## 5. Caso dimostrativo principale — casetta a due piani G8A/G8B

La casetta è un **esempio didattico di prova del software**, non un edificio dichiarato dimensionato.

**Geometria:** 4 file di pilastri a `x=0,5,7,12 m` (campate 5 + 2 corridoio + 5 m); livelli `y=0,3,6 m`; balconi simmetrici a quota `+3 m` con sbalzo `1,20 m`; colmo a `(6,8)`; fascia di influenza/tributaria `4,0 m`. Telaio in **acciaio**, falde in **legno GL24h**. Appoggi d'esempio idealizzati come incastri.

**G8A:** `examples/mixed/two-storey-steel-house-timber-roof.json`; guida `docs/structural/examples/two-storey-steel-house-timber-roof.md`. È un primo esempio con carichi permanenti, d'uso, neve, vento, sollevamento e combinazioni **manuali**.

**G8B, quattro alternative** in `examples/mixed/g8b-house/`:

| File / alternativa | Colonne | Travi 5 m | Corridoio + balconi | Legno copertura | X |
|---|---|---|---|---|---|
| `g8b-b_riferimento-senza-controventi.json` | HEB 220 | IPE 330 | IPE 220 | GL24h 160×360 | No |
| `g8b-a_leggero-controventato.json` | HEB 200 | IPE 300 | IPE 200 | GL24h 140×320 | Sì |
| `g8b-b_riferimento-controventato.json` | HEB 220 | IPE 330 | IPE 220 | GL24h 160×360 | Sì |
| `g8b-c_robusto-controventato.json` | HEB 240 | IPE 360 | IPE 240 | GL24h 180×400 | Sì |

- X nella **campata 0–5 m** su due livelli; aste `Truss2D` **bilaterali** (elasticamente attive anche a compressione). L'incrocio della X non è un nodo. Non rappresentano cavi/tiranti tension-only né la reale stabilità e resistenza degli attacchi.
- Pesi propri dichiarati tramite **`G_SELF` / `SelfWeight`**, distinto dai carichi di pacchetto, evitando doppio conteggio.
- Carichi didattici di piano: permanenti 5,5 kN/m² → 22 kN/m; residenziale variabile 2,0 → 8 kN/m; corridoio 3,0 → 12 kN/m; balconi permanenti 6,0 → 24 kN/m, variabili 4,0 → 16 kN/m; tetto/neve ipotetici 1,5 kN/m² → ca. 5,70 kN/m per falda lungo la lunghezza inclinata; vento orizzontale ±44 kN **caratteristici** e sollevamento didattico in copertura.
- Combinazioni illustrative `ULS_GRAVITY`, `ULS_WIND_POS`, `ULS_WIND_NEG`, `SLS_RARE`; le loro etichette non implicano automaticamente conformità NTC. Nei casi ULS_WIND il vento orizzontale è fattorizzato nei modelli di esempio.
- Guida: `docs/structural/examples/G8B_VERIFICA_CASSETTA_E_CONFRONTO.md`.
- Calcolo **separato** Python/NumPy in `benchmarks/independent/g8b-house/independent_frame2d_reference.py`, valori in `g8b-risultati-elastici-indipendenti.json`, controlli in `g8b-controlli-analitici.json`, confronto sezioni CSV. Esempio `ULS_WIND_POS`: `maxAbsUx` indipendente B senza X ≈7,1365 mm, B con X ≈1,5966 mm: **riduzione teorica ~77,6% nell'idealizzazione lineare**, non conferma dell'esecuzione CLI né verifica di progetto.
- Runner Windows `scripts/run-g8b-house-demo.ps1`: 4 modelli × 4 combinazioni → **16 report e 2 viewer** da confrontare con riferimento indipendente; finché il risultato non è mostrato, l'equivalenza numerica non è verificata.

## 6. Incidente G8B CLI e correzioni già incluse: NON regredire

In G8B originale due file dati (`g8b-controlli-analitici.json`, `g8b-risultati-elastici-indipendenti.json`) erano stati erroneamente collocati sotto `examples/mixed/g8b-house/`. La suite CLI tratta ricorsivamente ogni `examples/**/*.json` come **modello** e lancia `DistributedExamples_ShouldAnalyzeAndExportViewer`, che richiede `nodes` e `members`. Due esecuzioni Windows hanno mostrato `NullReferenceException` alla riga 295 del test, pur con build Release **0 warning/0 errori** e altri progetti verdi (CLI 241/243).

- **FIX1:** dati JSON e CSV spostati in `benchmarks/independent/g8b-house/`, generatore Python aggiornato per non ricrearli sotto `examples/`. Modelli e sorgenti C# non modificati.
- **FIX2:** estrarre FIX1 sopra la vecchia directory *non rimuove* i file obsoleti. `scripts/repair-g8b-legacy-examples.ps1` è richiamato **all'inizio** di `scripts/run-g7-tests.ps1`, prima di clean/build, e sposta in `.g8b-legacy-backup/` soltanto quei due vecchi JSON riconosciuti come reference data (`models` senza `nodes/members`). Non elimina né modifica modelli validi; è idempotente.
- **Presidio permanente:** non depositare mai dati numerici/reference JSON in `examples/` se privi di struttura FEM. Tenere distinti `benchmarks/` ed `examples/`; prevedere nella QA eventuali file residui da estrazioni sovrapposte.

L'utente ha successivamente dichiarato `G7 GATE PASSED`. Non interpretare questo come esecuzione di tutti i 16 casi CLI della demo, né ignorare l'episodio di regressione nell'audit P0-A.

## 7. Script e comandi verificabili su Windows

Da estrazione nuova del pacchetto, nella cartella principale `StructuralSolver2D`:

```powershell
# Integrità dell'archivio estratto (manifest SHA256)
.\scripts\verify-g7-manifest.ps1

# Suite precedente: fix mirati dei file obsoleti, build Release warning-as-error,
# benchmark G7 + regressioni Core, Analysis, Verification, Catalogs, Reporting, CLI
.\scripts\run-g7-tests.ps1

# Dimostrazione della casetta G8B: 16 report e 2 viewer, NON un gate normativo
.\scripts\run-g8b-house-demo.ps1

# Elenco dei soli quattro modelli corretti
Get-ChildItem .\examples\mixed\g8b-house -Filter *.json | Select-Object Name
```

L'esecuzione di uno script di test, il numero di test passati e l'eventuale confronto fra risultanze .NET e valori Python vanno sempre **registrati nel changelog/handoff** senza inferenze. `RELEASE_SHA256SUMS.txt` è verificato per i file presenti nel manifest; una scansione separata deve escludere file estranei dalla distribuzione.

## 8. P0 — piano approvato; dove iniziare senza ulteriori chiarimenti

L'utente ha scelto di passare a **P0** dopo G7. **Non** introdurre subito altro codice di calcolo: iniziare da **P0-A**, audit tecnico e matrice di copertura. Dettaglio operativo e criteri di completamento: **[`docs/structural/P0_A_PIANO_OPERATIVO.md`](docs/structural/P0_A_PIANO_OPERATIVO.md)**.

- **P0-A — Audit tecnico completo:** inventario dimostrabile del FEM, carichi, stabilità, cataloghi, combinazioni, verifiche acciaio/legno e reporting; collegare ogni capacità a implementazione e test; classificare supportato/validato, parziale, mancante, esterno, non applicabile. Identificare gap P0-B…P0-E.
- **P0-B — Combinazioni normative SLU/SLE:** implementazione solo dopo specifica, fonti e test indipendenti, gestione gruppi di azioni, ψ/γ, alternanza azione guida, favorevoli/sfavorevoli, risultati tracciabili.
- **P0-C — Stazioni critiche e inviluppi:** copertura dell'intera asta, concomitanza N/V/M, discontinuità, estremi e sezioni interne, evitare falsi superamenti.
- **P0-D — Copertura normativa acciaio/legno:** priorità dai gap realmente riscontrati, selezione dell'edizione, resistenze, interazioni, instabilità, SLE, classi e netti; fallback `Unsupported` quando una prova è fuori campo.
- **P0-E — Limiti fisici e modello:** ritegni, nodi e collegamenti, diaframmi, stabilità spaziale, contatto/sollevamento, aste tension-only, secondo ordine, eventuali controventi compressi.

Poi **P1** Timoshenko/ulteriori benchmark e accuratezza; **P2** consolidamento API/report; **P3** GUI Avalonia solo alla fine. Nessun passo autorizza certificare la casetta.

### Accettazione P0-A

Il deliverable iniziale dovrebbe essere **documentale**, con matrice di copertura *basata sul codice*, riferimenti esatti a file e test, gravità del rischio, esempio minimo dimostrativo, prerequisiti e milestone proposta. Non inventare capacità basandosi su titoli, README o checklist storiche. Non compilare una regola solo perché esiste una classe chiamata “verify”. La matrice deve indicare anche **come verificare il gap** e quali decisioni architetturali sono necessarie per P0-B…E.

## 9. Documentazione corrente e documenti che fanno fede

1. **`HANDOFF_NUOVA_CHAT.md`** — questo file: versione, gate, incidente noto, limiti e prossima attività.
2. **`docs/structural/P0_A_PIANO_OPERATIVO.md`** — checklist e output attesi del prossimo lavoro.
3. **`docs/structural/roadmap.md`** — priorità P0–P3, dal checkpoint attuale.
4. **`README.md`** — guida stabile d'uso, non diario quotidiano.
5. **`CHANGELOG.md`** — cronologia breve, gate confermati e cambi recenti.
6. **`docs/G7_MANUALE_VALIDAZIONE_INDIPENDENTE.md`**, **`docs/structural/G6_IMPLEMENTATION_GUIDE.md`** — validazione e copertura già esistenti.
7. **`docs/structural/examples/G8B_VERIFICA_CASSETTA_E_CONFRONTO.md`** — analisi dei modelli e delle ipotesi.
8. **`benchmarks/G6_COVERAGE_REFERENCE_CROSSWALK.json`**, **`benchmarks/G7_INDEPENDENT_GOLDEN.json`** — benchmark già censiti.
9. `docs/archive/` — **solo storico**: molte note sono superate e non devono essere prese per stato corrente.

Materiale secondario L1/L2/L3 relativo al libro e PDF normativi possono essere allegati separatamente nella nuova chat, se servono verifiche di formule/fonti; **non presumerli inclusi nello ZIP**.

**Igiene documentale:** nella root rimangono solo i tre Markdown `README.md`, `CHANGELOG.md`, `HANDOFF_NUOVA_CHAT.md`. Dettagli tecnici in `docs/structural/`, release note precedenti in `docs/archive/`. A ogni milestone aggiornare obbligatoriamente handoff e changelog, roadmap quando cambia l'ordine; README solo quando cambiano capacità o comandi.

## 10. Protocollo della nuova chat e delle nuove milestone

- Prima leggere `HANDOFF_NUOVA_CHAT.md` e il piano P0-A, **poi ispezionare il codice reale**; preservare vincoli e API esistenti, non dare per dimostrati i risultati sulla base dei nomi dei file.
- Per P0-A usare inventario e analisi puntuale dei sorgenti, test e dataset; **nessuna aggiunta di funzioni normative prima di descrivere le lacune** e stabilire i criteri di test.
- Se si modificano codice/test, produrre test positivi e negativi e gate Windows; **non modificare i golden numerici per farli passare**, ma solo con derivazione indipendente.
- Ogni archivio FULL deve includere sorgenti, test, cataloghi, esempi, documentazione, manifest SHA-256, script Windows e uno handoff **aggiornato**; controllare ZIP CRC, hash, directory e assenza di residui.
- La nuova chat deve distinguere chiaramente **risultati osservati** dai **risultati attesi/teorici** e gli **esiti delle regole** dalle attestazioni professionali.
- Prima di cambiare le assunzioni della casetta, preservare una baseline confrontabile dei quattro modelli G8B; le ipotesi di vento/neve e le sezioni non sono un progetto reale.

## 11. Frase pronta per avviare la nuova conversazione

> Continuiamo StructuralSolver2D dal pacchetto FULL P0-HANDOFF che allego. Leggi prima `HANDOFF_NUOVA_CHAT.md`, poi `docs/structural/P0_A_PIANO_OPERATIVO.md`, `docs/structural/roadmap.md` e `CHANGELOG.md`. La suite G7 è stata confermata `G7 GATE PASSED`; la demo G8B non va scambiata per una verifica normativa. **Inizia P0-A: audit tecnico rigoroso del FEM e della copertura EC3/EC5/NTC**, confrontando codice, test, benchmark e limiti del modello, e produci una matrice delle lacune con priorità e azioni concrete. Mantieni codice e gate collaudati, aggiorna handoff/changelog/roadmap, rimanda le modifiche normative a dopo l'audit e la GUI Avalonia all'ultima fase. Non chiedermi di ripetere decisioni già contenute nel pacchetto.
