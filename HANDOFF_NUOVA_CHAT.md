# StructuralSolver2D — HANDOFF NUOVA CHAT

**Checkpoint operativo: P0-C2A ESTREMI DIAGNOSTICI AFFINI, TEST WINDOWS IN ATTESA (25 ottobre 2026)**  
**Origine:** G8B FIX2 + audit P0-A; **gate Windows confermati dall'utente:** G7, P0-B2, P0-B3, P0-B4 e P0-C1 FIX1 nei rispettivi perimetri.  
**P0-A completato documentalmente:** audit, matrice e backlog testabile nei tre documenti P0_A in docs/structural.  
**P0-B1 completato come specifica e riferimento aritmetico:** docs/structural/P0_B1_CONTRATTO_AZIONI_NTC2018.md, benchmarks/independent/p0b; 14 casi positivi e 10 mutazioni negative verificati con Python.  
**P0-B2 GATE PASSED (conferma dell'utente: «test passati», senza log numerico allegato)** per i test richiesti dopo il precedente checkpoint. Modulo di azioni tipizzate e JSON confermato nel perimetro B2; non un generatore completo.  
**P0-B3 GATE PASS confermato dall'utente («tutti i test passati», senza log numerico):** coefficienti γ/ψ, quattro famiglie STR A1/SLE, scenari espliciti e confronto dei golden B1. Vedere docs/structural/P0_B3_GENERATORE_COMBINAZIONI_CANDIDATE.md.  
**P0-B4 GATE PASS confermato dall'utente («test passati», senza contatori):** validazione del **piano finito dichiarato per effetto**, fingerprint dei reali input FEM e termini nel Core; non dimostra esaustività globale. Vedere docs/structural/P0_B4_AUDIT_INSIEME_DICHIARATO.md.  
**P0-C1 FIX1 GATE PASS confermato dall'utente («test passati»), senza nuovo log numerico allegato:** nel primo log l'unico test C1 fallito selezionava x=0 invece del salto x=2; FIX1 ha corretto **solo il test**, senza toccare produzione. Leggere docs/structural/P0_C1_AUDIT_SOLLECITAZIONI_CONCOMITANTI.md.  
**P0-C2A ora implementato, test .NET NUOVI IN ATTESA:** nuovo `FemAffineUtilizationExtremaC2A` in `Verification.Integration`, massimo analitico di **sole metriche matematiche dichiarate** nei tratti con N/V/M affini del Frame2D con carichi puntuali, nessun distribuito/selfweight sull'asta. Per carichi fuori scope torna `ScreeningIncomplete`. Zero formule o pass normativi attribuiti. Vedere docs/structural/P0_C2A_MASSIMI_UTILIZZO_DIAGNOSTICO_AFFINE.md.  
**Prossimo: gate Windows P0-C2A** (nuovi test + regressione G7); poi C2B/C2C/C3/C4 e P0-E/D. Avalonia soltanto P3.

> **Istruzione essenziale per la nuova chat:** leggere handoff, rapporti P0_A, contratti P0-B, manuale C1 e **P0_C2A_MASSIMI_UTILIZZO_DIAGNOSTICO_AFFINE.md**, poi roadmap, README e changelog. **C1 FIX1 superato su conferma utente**. Eseguire i nuovi test C2A su Windows e regressione G7; soltanto dopo iniziare C2B. Conservare G7/G8B; nessuna GUI.

## 1. Obiettivo, ambito e vincoli inderogabili

StructuralSolver2D è un progetto open source in **C# / .NET 8**, destinato a studenti, docenti e professionisti per didattica, ricerca e valutazioni preliminari di **strutture piane**: travi, reticolari e telai **in acciaio e legno**.

Ordine di sviluppo vincolante:

1. **Motore FEM 2D corretto, testato e documentato**: analisi, carichi, vincoli, reazioni, N/V/M, postprocessing, benchmark e casi patologici.
2. **Verifiche normative separabili dal calcolo**, con provenienza delle azioni, categorie, combinazioni SLU/SLE e copertura realmente dimostrabile; NTC 2018/Circolare 2019 e versioni dichiarate di EC3/EC5.
3. **GUI leggera C#/Avalonia soltanto alla fine**: editor di nodi, aste, vincoli, carichi, combinazioni e consultazione risultati; non introdurre UI mentre mancano affidabilità e copertura.

**Fuori ambito per decisione di prodotto:** FEM 3D, cemento armato, muratura, piastre/pareti, X-LAM, BIM, progettazione esecutiva generale dei nodi/collegamenti. Non interpretare un vecchio file didattico in calcestruzzo come autorizzazione a includere il c.a.

Un esito positivo di **una singola regola** o di **una singola sezione** non significa «asta conforme» e, a maggior ragione, non significa «edificio verificato».

## 2. Baseline: cosa prendere in carico e cosa è veramente passato

- **Archivio FULL consigliato:** StructuralSolver2D_P0C2A_ESTREMI_AFFINI_2026-10-25_FULL.zip. Include P0-A/B1/B2/B3/B4/C1 FIX1, codice/test C2A e manifest SHA; nessuna modifica a motore FEM, G8B, benchmark storici, Core/Analysis/Verification/CLI/Reporting di produzione.
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
| P0-A | **Completato documentalmente** | Audit codice/test e matrice di copertura; nessun nuovo gate .NET |
| P0-B1 | **Specifica e golden aritmetici completati** | 14/14 casi positivi e 10/10 controlli negativi Python; generatore .NET assente |
| P0-B2 | **GATE PASS confermato dall'utente** | Test C# del contratto azioni/JSON e regressioni richieste dichiarati passati; nessun log analitico allegato |
| P0-B3 | **GATE PASS confermato dall'utente** | 14 golden B1 integrati in xUnit, tabelle γ/ψ, scenari e omissioni; senza log numerico allegato |
| P0-B4 | **GATE PASS confermato dall'utente** | Insieme finito di combinazioni dichiarate per effetto, verifica termini presenti nel Core/fingerprint; non esaustività normativa |
| P0-C1 FIX1 | **GATE PASS confermato dall'utente** | Unico test precedente errato sul lato del carico a x=2 corretto senza toccare l'algoritmo; nuova conferma «test passati», senza log allegato |
| P0-C2A | **Codice/test predisposti, gate .NET IN ATTESA** | Massimi di **indicatori diagnostici** sotto ipotesi affini; distribuiti/selfweight marcati `ScreeningIncomplete`, **NON** conformità normativa o asta verificata |

## 3. Architettura: directory da conoscere

| Percorso | Ruolo e cautela |
|---|---|
| `src/StructuralSolver2D.Core/` | Modelli di geometria, nodi, aste, vincoli, materiali, sezioni, carichi e contratti |
| `src/StructuralSolver2D.Combinations/` | P0-B2 azioni/validator/JSON, P0-B3 candidati NTC/manifesto, P0-B4 audit **del piano dichiarato** e fingerprint Core; sola dipendenza verso Core; nessun audit normativo globale |
| `src/StructuralSolver2D.Analysis/` | Risolutore FEM 2D, analisi e postprocessing; non confondere esperimenti avanzati con validazione generale |
| `src/StructuralSolver2D.Catalogs/` | Cataloghi materiali/profili e geometrie; provenienza e approssimazioni da verificare |
| `src/StructuralSolver2D.Verification/` | Motore comune, contratti, esiti (`Passed`, `Failed`, `Unsupported`, ecc.) |
| `src/StructuralSolver2D.Verification.Steel/` | Regole **parziali** EC3 M89–M91, stabilità e audit M91-D |
| `src/StructuralSolver2D.Verification.Timber/` | Regole EC5 M92 e NTC2018 G1–G6 |
| `src/StructuralSolver2D.Verification.Integration/` | Bridge FEM M87-B/C, audit C1 N/V/M concomitanti e C2A massimi di due indicatori matematici sui soli tratti affini; nessuna regola normativa nuova |
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

# Gate NUOVO P0-C2A (non eseguito nell'ambiente privo di SDK .NET)
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~FemAffineUtilizationExtremaC2ATests' --logger 'console;verbosity=normal'

# Dimostrazione della casetta G8B: 16 report e 2 viewer, NON un gate normativo
.\scripts\run-g8b-house-demo.ps1

# Elenco dei soli quattro modelli corretti
Get-ChildItem .\examples\mixed\g8b-house -Filter *.json | Select-Object Name
```

L'esecuzione di uno script di test, il numero di test passati e l'eventuale confronto fra risultanze .NET e valori Python vanno sempre **registrati nel changelog/handoff** senza inferenze. `RELEASE_SHA256SUMS.txt` è verificato per i file presenti nel manifest; una scansione separata deve escludere file estranei dalla distribuzione.

## 8. P0 — stato corrente: P0-B2/B3/B4/C1 passati, P0-C2A da collaudare

**Aggiornamento autorevole 25/10/2026, P0-C2A:** l'utente ha dichiarato **«test passati»** per FULL P0-C1 FIX1: registrato **GATE C1 PASS** senza dati numerici di questa seconda esecuzione. Implementato in `Verification.Integration` `FemAffineUtilizationExtremaC2A`: modello Frame2D primo ordine con soli carichi concentrati/nodali/settlement sul membro, riconoscimento salti e valori N/V/M ai due lati; massimo analitico dei **soli** indici diagnostici `|N|/Ns+|V|/Vs+|M|/Ms` e `|(N/Ns)(M/Ms)|`. Per carichi uniformi, variabili linearmente o SelfWeight attivi restituisce `ScreeningIncomplete`, nessun massimo. Il supporto a un massimo **normativo** completo richiede metodi per ogni regola in P0-C2C e copertura C3. Nuovi xUnit C2A con golden manuali e caso matematico interno; SDK .NET assente localmente, gate Windows richiesto. Guida docs/structural/P0_C2A_MASSIMI_UTILIZZO_DIAGNOSTICO_AFFINE.md. Dopo: **C2B distribuiti**, **C2C metriche normative**, C3/C4/E/D. Test storico C1 527/528 pre-FIX1 resta documentato ma superato dalla conferma attuale.

**Aggiornamento autorevole 25/10/2026, P0-C1 FIX1:** letto log Windows `dotnet-20261025-014719-25108-2fab4f16.log`: compilazione Release **0 warning, 0 errori**. Suite FEM G7 **3/3**, integrazione G7 **3/3**, Verification **527/528**; unico fallimento `AContinuousSectionAtAConcentratedMemberLoad_CannotBeValidated` (atteso `Invalid`, ottenuto `MatchedDeclaredStations`). Il test selezionava per errore `plan.Stations[0]` e `requests[0]` (x=0 già `Continuous`), quindi la mutazione era un no-op; il carico concentrato era a x=2 nella stazione **indice 1**. Corretto il test, aggiunta asserzione esatta `DistanceMetres=2` e controllo positivo della stazione continua valida a x=0. **Il codice di produzione resta identico al P0-C1 originale**; suite FIX1 da ripetere interamente. Non promuovere C1 a PASS sulla base di 527/528. La porzione P0-C1 precedente sotto è storica.

**Aggiornamento autorevole 25/10/2026, P0-C1:** l'utente ha confermato **«test passati»** per il precedente FULL P0-B4, che passa a **GATE PASS** (senza log/contatori allegati). In P0-C1 implementato un audit conservativo `FemConcomitantStationAuditorC1`: piano di stazioni/target/regole per membro e edizione, controllo combinazioni e lati, riesecuzione analisi per ogni target e confronto delle **tre** sollecitazioni firmate (N/V/M) a stessa sezione; rifiuto di copie erronee da altri inviluppi. I nuovi xUnit verificano anche che la completezza dell'asta e un massimo di utilizzo tra stazioni **NON** siano mai dedotti dal pass C1. **Test .NET nuovi da eseguire su Windows**, SDK assente qui. Dettaglio: docs/structural/P0_C1_AUDIT_SOLLECITAZIONI_CONCOMITANTI.md. Dopo gate C1: **C2 massimo di utilizzo continuo**, **C3 obblighi membro/regole**, C4 reporting, quindi E/D. Lo stato precedente di B4 riportato sotto è storico.

**Aggiornamento autorevole 25/10/2026, P0-B4:** l'utente ha confermato **«tutti i test passati»** per il precedente checkpoint P0-B3; registrato **B3 GATE PASS**, senza output numerico allegato. Implementato l'auditor B4 separato, che richiede un piano *finito dichiarato* per un solo effetto, verifica guide/alternative/classificazione e confronta i fattori delle `StructuralModel.LoadCombinations` reali con la baseline e i SHA-256 degli input Core (incluse geometria, carichi, sezioni, materiali, vincoli, peso unitario, release). Nuovi xUnit positivi, negativi e mutazioni. **Non compilato .NET qui**; gate Windows B4 e regressione G7 da eseguire. La validazione del piano non scopre automaticamente i casi fisici/normativi omessi: non dichiarare «insieme globale completo», «membro Passed» o «edificio verificato». Dettaglio: docs/structural/P0_B4_AUDIT_INSIEME_DICHIARATO.md. **Dopo il gate B4: P0-C stazioni critiche/concomitanza N/V/M, P0-E limiti esterni, P0-D ampliamento normativo.**

**Aggiornamento autorevole 25/10/2026:** P0-A è chiuso documentalmente. P0-B1 ha fissato da fonte primaria NTC 2018 le tabelle γ STR A1, ψ e le equazioni SLU/SLE entro ambito dichiarato. Il controllo Python ha superato **14/14 scenari aritmetici** e rilevato **10/10 alterazioni volontarie**: non è un test del generatore C# né del FEM. Il validatore attuale respinge fattori zero: il nuovo contratto richiede **omissione tracciata nel manifesto** senza mutare le combinazioni manuali. La favorevolezza dipende dall'effetto; le alternative vento ± e le guide richiedono audit. P0-B2 implementa modelli e validator tipizzati senza toccare G7. Il riferimento Python G8B ha riprodotto 16/16 scenari nei campi controllati; manca ancora il confronto .NET ↔ Python e la conferma CLI dei report.

**Implementazione successiva P0-B2, stesso giorno:** aggiunto StructuralSolver2D.Combinations con ActionInventory, categorie NTC e provenienza, gruppi alternativi, ActionInventoryValidator e ActionInventoryJsonSerializer; la soluzione e Core.Tests hanno riferimenti al modulo. Test xUnit nuovi predisposti; SDK dotnet assente qui, quindi **compilazione e gate Windows NON confermati**. Core e sue combinazioni manuali sono invariati; G7 resta ultimo gate precedente confermato. Prima azione: eseguire su Windows il comando dotnet test B2, poi scripts/run-g7-tests.ps1 (istruzioni in P0_B2_CONTRATTO_AZIONI_E_VALIDAZIONE.md).

**Aggiornamento autorevole successivo (25/10/2026):** l'utente ha dichiarato **«test passati»** in risposta a P0-B2; registrato come gate B2 confermato senza contatori del log. P0-B3 aggiunge Ntc2018BuildingActionCoefficients e Ntc2018BuildingCombinationGenerator: quattro famiglie non sismiche per edifici STR A1/SLE, scelta favorevolezza per effetto esplicita, omissione ψ0/ψ1/ψ2=0 e alternative tracciate. Il generatore restituisce **solo un candidato** e un manifesto con FullCombinationSetAudited=false; P0-B4 deve ancora dimostrare completezza e fingerprint FEM. Nuovi xUnit confrontano 14/14 golden indipendenti B1 senza cambiarli, ma **B3 NON ha ancora gate .NET**. P0-C/E/D e GUI P3 invariati.

**Storia:** il piano iniziale chiedeva P0-A dopo G7, ormai completato. Il contratto P0-B1 e i golden numerici sono le specifiche di ingresso obbligatorie per P0-B2/B3/B4. Dettaglio dei gap: docs/structural/P0_A_PRIORITA_E_CRITERI_DI_TEST.md.

- **P0-A — Audit tecnico completo:** inventario dimostrabile del FEM, carichi, stabilità, cataloghi, combinazioni, verifiche acciaio/legno e reporting; collegare ogni capacità a implementazione e test; classificare supportato/validato, parziale, mancante, esterno, non applicabile. Identificare gap P0-B…P0-E.
- **P0-B — Combinazioni normative SLU/SLE:** implementazione solo dopo specifica, fonti e test indipendenti, gestione gruppi di azioni, ψ/γ, alternanza azione guida, favorevoli/sfavorevoli, risultati tracciabili.
- **P0-C — Stazioni critiche e inviluppi:** copertura dell'intera asta, concomitanza N/V/M, discontinuità, estremi e sezioni interne, evitare falsi superamenti.
- **P0-D — Copertura normativa acciaio/legno:** priorità dai gap realmente riscontrati, selezione dell'edizione, resistenze, interazioni, instabilità, SLE, classi e netti; fallback `Unsupported` quando una prova è fuori campo.
- **P0-E — Limiti fisici e modello:** ritegni, nodi e collegamenti, diaframmi, stabilità spaziale, contatto/sollevamento, aste tension-only, secondo ordine, eventuali controventi compressi.

Poi **P1** Timoshenko/ulteriori benchmark e accuratezza; **P2** consolidamento API/report; **P3** GUI Avalonia solo alla fine. Nessun passo autorizza certificare la casetta.

### Accettazione P0-A (criterio storico già soddisfatto documentalmente)

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

## 11. Messaggio di ripresa corrente (P0-C2A)

**Usare ora questa richiesta di ripresa:** Continuare dal FULL P0-C2A ESTREMI AFFINI. Leggere HANDOFF, P0_C2A_MASSIMI_UTILIZZO_DIAGNOSTICO_AFFINE, C1, contratti P0-B e rapporti P0-A, roadmap/changelog. **P0-B2/B3/B4/C1 FIX1 passati per conferma utente. Testare ora C2A su Windows** con `FemAffineUtilizationExtremaC2ATests` e `scripts/run-g7-tests.ps1`. C2A tratta solo due metriche matematiche per tratti affini, non verifica normativa. Dopo gate C2A: C2B distribuiti, C2C funzioni normative, C3/C4/E/D, GUI Avalonia soltanto P3. Il testo seguente è storico.

> Continuiamo StructuralSolver2D dal pacchetto FULL P0-HANDOFF che allego. Leggi prima `HANDOFF_NUOVA_CHAT.md`, poi `docs/structural/P0_A_PIANO_OPERATIVO.md`, `docs/structural/roadmap.md` e `CHANGELOG.md`. La suite G7 è stata confermata `G7 GATE PASSED`; la demo G8B non va scambiata per una verifica normativa. **Inizia P0-A: audit tecnico rigoroso del FEM e della copertura EC3/EC5/NTC**, confrontando codice, test, benchmark e limiti del modello, e produci una matrice delle lacune con priorità e azioni concrete. Mantieni codice e gate collaudati, aggiorna handoff/changelog/roadmap, rimanda le modifiche normative a dopo l'audit e la GUI Avalonia all'ultima fase. Non chiedermi di ripetere decisioni già contenute nel pacchetto.
