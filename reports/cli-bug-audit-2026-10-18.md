# Ricerca bug in Cli — 2026-10-18

Revisione del dispatcher, lettore JSON, analisi, output console e comandi di report/export. Base esaminata: commit `d344b25` (`fix reporting`), con le precedenti correzioni di Core e Analysis.

## Esito delle correzioni

**Tutti gli otto casi sono stati corretti**, con **169 nuovi casi di test** nella suite CLI: 33 sul reader e 136 che eseguono il processo CLI. I test di processo invocano l'assembly compilato tramite `dotnet`, usando argomenti separati, directory temporanee indipendenti e timeout.

- I sei export a file verificano i percorsi assoluti normalizzati prima del caricamento/scrittura; su Windows il confronto ignora le differenze di maiuscole. CSV verifica tutti i sei nomi di destinazione prima di scrivere il primo file. La sostituzione di report distinti dall'input resta consentita.
- I numeri obbligatori mancanti/null vengono rifiutati, mantenendo distinti gli zeri espliciti; gli elementi null nelle raccolte riportano percorso e indice, anche nei termini delle combinazioni.
- Analisi console, report ed export usano tutti `StructuralSolver2DService`, con gli stessi campionamenti. I casi e le combinazioni che escludono il momento sul reticolo passano; quelli che lo applicano senza rigidezza/vincoli rotazionali adeguati vengono rifiutati coerentemente.
- Rimossi A3 e C3, i due nodi orfani dell'esempio della casa con balconi. Il modello usa già A2 e C2 come estremi inferiori delle falde del tetto.
- Il reader conserva la provenienza del selettore predefinito: un `loadCombinationId` inesistente produce un errore di selezione, anche se esiste un caso con quell'id. Un argomento finale esplicito può sostituire il default. Per compatibilità `loadCaseId` può ancora contenere un id di combinazione.
- L'arità viene verificata per tutti i comandi e alias, prima di leggere input o scrivere output; gli argomenti eccedenti producono errore.
- Il riepilogo console stampa moduli non negativi; le tabelle di dettaglio mantengono i segni, e le posizioni degli estremi restano associate ai risultati originali.

La prima batteria di 147 nuovi test, eseguita prima delle correzioni, ha riprodotto **103 fallimenti** e 44 successi (154 casi totali includendo i sette esistenti). Sono stati poi aggiunti 22 controlli positivi sulla sintassi e sugli alias, portando i nuovi casi a 169.

Verifica finale:

| Progetto | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 192 | 0 |
| Analysis.Tests | 245 | 0 |
| Reporting.Tests | 119 | 0 |
| Cli.Tests | 176 | 0 |
| Totale .NET | 732 | 0 |

```powershell
dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal
```

I test persistenti sono in `tests/StructuralSolver2D.Cli.Tests/CliCommandRegressionTests.cs`, `CliSummaryRegressionTests.cs`, `Input/JsonInputRegressionTests.cs`, con il supporto di `CliTestWorkspace.cs`. Copiano gli esempi nell'output di build e non dipendono da `out/CliBugAudit/`. Tutti i 21 esempi distribuiti eseguibili passano analisi e viewer; passano inoltre SLS_HOUSE e ULS_HOUSE. Le reazioni sono verificate anche nel contenuto Markdown, CSV, PDF, XLSX e SVG/HTML.

Le sezioni seguenti conservano riproduzioni, riferimenti al codice e risultati raccolti **prima delle correzioni**. Gli interventi suggeriti sono stati completati come descritto sopra.

## Ricerca iniziale

**Esito: otto problemi riprodotti, sei P2 e due P3.** Sette riguardano il codice CLI; uno riguarda un esempio JSON distribuito e documentato per la CLI. P2 indica perdita di input, risultati o selezione dell'analisi inattesi, oppure un flusso di utilizzo bloccato. P3 indica un problema di diagnostica o presentazione.

Il codice applicativo, i test esistenti e gli esempi del repository non sono stati modificati durante questa ricerca. Le mutazioni, comprese le prove di sovrascrittura, sono state eseguite esclusivamente su copie sotto `out/CliBugAudit/`.

## 1. P2 — Report ed export possono sovrascrivere il modello sorgente

**Codice:** `src/StructuralSolver2D.Cli/Program.cs:186`, `:253`, `:295`, `:332`, `:374`, `:418`, `:469`.

I comandi leggono il modello e scrivono la destinazione senza controllare se il percorso di output coincide con l'input. Un errore nel percorso distrugge il JSON e restituisce comunque successo.

**Riproduzione:** copiare `examples/beams/cantilever-point-load.json` in `out/CliBugAudit/source.json`, quindi:

```powershell
dotnet src/StructuralSolver2D.Cli/bin/Debug/net8.0/StructuralSolver2D.Cli.dll report out/CliBugAudit/source.json out/CliBugAudit/source.json
```

Risultato verificato: exit code `0`; il file sorgente ora contiene Markdown. Lo stesso comportamento è stato riprodotto con `export-xlsx`, `export-pdf`, `export-svg`, `export-html` e `export-viewer`.

Anche CSV può collidere con l'input: un modello JSON salvato come `nodal-displacements.csv` nella cartella di destinazione viene letto correttamente, poi sostituito dalla prima tabella CSV. Il reader non impone un'estensione al file di input.

**Intervento suggerito:** verificare i percorsi assoluti normalizzati prima di qualsiasi scrittura; per CSV confrontare tutti i nomi generati. Gestire coerentemente maiuscole/minuscole su Windows. La normale sostituzione di un precedente report distinto dall'input può restare consentita.

**Test da aggiungere:** tutti i sette comandi di scrittura; percorsi identici, alias relativi/assoluti e differenze di maiuscole su Windows. Verificare exit code non nullo e contenuto del sorgente identico byte per byte; per CSV evitare anche scritture parziali prima del controllo di collisione.

## 2. P2 — Numeri JSON obbligatori mancanti vengono sostituiti con zero

**Codice:** `src/StructuralSolver2D.Cli/Input/StructuralModelJsonReader.cs:47`, `:48`, `:116`, `:201`, `:203`, `:297`.

Le proprietà numeriche dei DTO sono `double` non nullable. Il reader non distingue proprietà assente e zero esplicito. La documentazione dichiara obbligatori `nodes[].x`, `nodes[].y` e `loads[].value`; quando zero è strutturalmente ammesso, il modello incompleto supera anche la validazione Core.

**Riproduzioni:**

1. Partire dalla mensola di 4 m, con nodo A `(0,0)` e B `(4,3)`. Eliminare `y` da B: `analyze` termina con exit code `0`, trattando B come `(4,0)`. Lo spostamento verticale cambia da `-0.076225 m` a `-0.060952 m`, senza segnalare il dato mancante.
2. Sulla mensola orizzontale usare un `LinearDistributedLoad` su M1 con `value = -6` ed `endValue = -12`. Eliminare `value`: exit code `0`, carico iniziale assunto uguale a zero. La reazione verticale passa da `36 kN` a `24 kN`, il momento da `80 kNm` a `64 kNm`.

La seconda prova non va confusa con un carico triangolare esplicitamente definito con `value: 0`: quest'ultimo è valido e deve restare accettato. Per altri numeri obbligatori, il default zero può essere già rifiutato dalla validazione meccanica, ma senza identificare correttamente l'assenza del campo.

**Intervento suggerito:** verificare la presenza dei campi numerici obbligatori durante il parsing, mantenendo i default solo per le proprietà documentate come opzionali.

**Test da aggiungere:** omissione separata di x/y e dei valori di carico; zero esplicito come controllo positivo; messaggio con percorso del campo; nessun report/export prodotto da un input incompleto.

## 3. P2 — Solver diversi rendono incoerenti i comandi sullo stesso caso di carico

**Codice:** `src/StructuralSolver2D.Cli/Program.cs:439`, `:484`, `:496`, `:498`; `src/StructuralSolver2D.Analysis/Truss2D/Truss2DAnalyzer.cs:142`.

Gli export SVG/HTML/viewer usano `StructuralSolver2DService`, mentre analisi console e report/export tabellari scelgono direttamente un analyzer. La validazione del `Truss2DAnalyzer` esamina anche carichi non appartenenti al caso selezionato.

**Riproduzione:** copiare `examples/trusses/simple-truss.json`. Lasciare LC1 selezionato e invariato, aggiungendo:

```json
{"id":"OTHER","name":"Unused moment case"}
```

ai casi di carico, e questo carico alla lista `loads`:

```json
{"id":"MZ","loadCaseId":"OTHER","type":"NodalMoment","targetType":"Node","targetId":"A","direction":"MomentZ","value":10}
```

| Comandi sul caso LC1 | Exit code | Esito |
|---|---:|---|
| analyze, report, export-csv, export-xlsx, export-pdf | 2 | `Unsupported loads: MZ (NodalMoment)` |
| export-svg, export-html, export-viewer | 0 | Export completato |

Il caso OTHER non partecipa a LC1. Non è necessario tentare l'analisi del momento sul reticolo per riprodurre l'incoerenza. Il reticolo originale passa l'analisi.

**Intervento suggerito:** condividere il workflow pubblico di analisi fra tutti i comandi e applicare una politica coerente ai carichi non selezionati. L'eventuale errore per un carico incompatibile effettivamente selezionato deve restare esplicito.

**Test da aggiungere:** matrice di tutti gli otto comandi sul medesimo input; casi e combinazioni che escludono/includono OTHER; confronto di id dell'analisi e risultati, oltre al solo exit code.

## 4. P2 — L'esempio documentato della casa con balconi è instabile

**Dati:** `examples/mixed/two-storey-house-balconies.json:85`, `:97`. Utilizzo documentato in `examples/README.md:59` e nell'elenco esempi del README principale.

I nodi A3 e C3 duplicano le coordinate di A2 e C2 ma non sono referenziati da alcuna asta. Le falde del tetto collegano A2–B3 e B3–C2, lasciando A3 e C3 isolati e privi di vincoli.

**Riproduzione:**

```powershell
dotnet src/StructuralSolver2D.Cli/bin/Debug/net8.0/StructuralSolver2D.Cli.dll analyze examples/mixed/two-storey-house-balconies.json
```

Risultato: exit code `2`, matrice vincolata singolare al pivot `39`. Anche `export-viewer` fallisce.

**Controprova:** eliminando soltanto A3 e C3 in una copia diagnostica, entrambi i comandi terminano con exit code `0`. È un errore nel dataset distribuito, distinto da un difetto del dispatcher. Il rifiuto dei gradi di libertà traslazionali senza rigidezza è coerente con le recenti correzioni di Analysis e non va disattivato per far passare l'esempio.

**Intervento suggerito:** rimuovere i nodi orfani o completare la connettività secondo il modello voluto. Per questa geometria, il tetto usa già A2 e C2.

**Test da aggiungere:** analisi e viewer dell'esempio documentato; controllo dei nodi non connessi nella raccolta di esempi eseguibili.

## 5. P2 — loadCombinationId può selezionare silenziosamente un caso semplice

**Codice:** `src/StructuralSolver2D.Cli/Input/StructuralModelJsonReader.cs:134`; `src/StructuralSolver2D.Cli/Program.cs:442`, `:493`.

Il reader conserva il selettore predefinito in una stringa senza mantenere se provenga da `loadCaseId` o `loadCombinationId`. I comandi ne deducono nuovamente il tipo cercando l'id tra le combinazioni; in assenza di una corrispondenza ripiegano sul caso semplice.

**Riproduzione:** nella mensola originale, sostituire il selettore radice `"loadCaseId":"LC1"` con `"loadCombinationId":"LC1"`, senza definire alcuna combinazione LC1. Eseguire senza override finale.

Risultato: exit code `0`, analisi di LC1 e reazione verticale `12 kN`. Anche il viewer viene esportato. Non viene segnalato che la combinazione richiesta non esiste. Un errore di configurazione della combinazione viene quindi mascherato dall'esistenza di un caso omonimo.

È stato verificato anche il comportamento inverso (`loadCaseId: ULS1` risolto come combinazione); l'eventuale compatibilità con questo uso storico va decisa esplicitamente. Il difetto certo qui è il fallback da un `loadCombinationId` inesistente a un caso semplice.

**Intervento suggerito:** conservare o verificare il tipo del selettore predefinito. L'argomento esplicito finale, documentato come caso o combinazione, può continuare a risolvere entrambi i tipi.

**Test da aggiungere:** combinazione predefinita esistente, inesistente e con id di un caso semplice; verifica su tutti i comandi; normale precedenza di loadCombinationId e selezione esplicita finale.

## 6. P2 — Argomenti in eccesso ignorati con exit code di successo

**Codice:** `src/StructuralSolver2D.Cli/Program.cs:105`, `:125`, `:133`, `:147`, `:158` e controlli analoghi negli export.

I comandi controllano solo un numero minimo di argomenti. Parametri successivi a quelli riconosciuti vengono ignorati. Un comando scritto male può quindi produrre un risultato e un file validi per una selezione diversa da quella che l'utente stava cercando di esprimere.

**Riproduzione:**

```powershell
dotnet src/StructuralSolver2D.Cli/bin/Debug/net8.0/StructuralSolver2D.Cli.dll analyze examples/combinations/load-combination.json G1 ULS1
```

Risultato: exit code `0`, id analisi G1, reazioni `25 kN`, momento massimo `31.25 kNm`; ULS1 viene ignorato. Se selezionato correttamente, ULS1 dà invece reazioni `48.75 kN` e momento massimo `60.9375 kNm`.

Anche `report` ed `export-viewer` con G1 ULS1 scrivono il risultato di G1 senza errore. Una prova con un argomento finale `UNEXPECTED` ha restituito successo per tutti i sette comandi di scrittura e per `example`. La CLI non documenta una sintassi per selezionare più analisi in una sola invocazione.

**Intervento suggerito:** controllare l'arità completa prima del caricamento del modello e di qualsiasi scrittura, indicando gli argomenti inattesi.

**Test da aggiungere:** numero minimo/massimo valido e uno o più argomenti eccedenti per ciascun comando e alias; nessun file creato o modificato quando la sintassi è errata.

## 7. P3 — Elementi null nei JSON producono una NullReferenceException senza contesto

**Codice:** `src/StructuralSolver2D.Cli/Input/StructuralModelJsonReader.cs:43`, cicli analoghi, e `:129`.

Il reader gestisce liste assenti/null come vuote, ma non verifica gli elementi al loro interno. `System.Text.Json` può deserializzare `null` in una lista di oggetti anche se il tipo C# è annotato non nullable.

**Riproduzioni verificate:** `[null]` nelle liste nodes, materials, sections, members, supports, loadCases, loads, loadCombinations e terms di una combinazione.

Tutte terminano con exit code `3` e:

```text
Error:
  Object reference not set to an instance of an object.
```

Il processo intercetta l'eccezione, ma la diagnostica non identifica né la lista né l'elemento invalido. Diversamente da altri campi obbligatori, l'utente non riceve un'indicazione correggibile del JSON.

**Intervento suggerito:** rifiutare gli elementi null con un errore di input che riporti percorso e indice; non ignorarli silenziosamente.

**Test da aggiungere:** tutte le nove raccolte, anche con elemento null in mezzo ad altri validi; messaggio con percorso/indice e assenza di output generati.

## 8. P3 — Il riepilogo stampa valori negativi come massimi in valore assoluto

**Codice:** `src/StructuralSolver2D.Cli/Output/CliOutputWriter.cs:150` fino alle stampe dei nove estremi.

Il summarizer conserva il valore con segno dell'estremo selezionato per modulo. Il writer lo inserisce direttamente in espressioni `Max |...|`, senza esplicitare che si tratta del valore con segno al punto critico.

**Riproduzione:** `example cantilever-point-load` o analisi dell'equivalente JSON:

```text
Maximum absolute results:
  Max |Uy| = -0.060952 m       at node B
  Max |Rz| = -0.022857 rad     at node B
  Max |M|  = -48.000000 kNm    at member M1, x = 0.000000 m
```

Anche `axial-bar` stampa `Max |Fx| = -100.000000 kN`. La posizione dell'estremo è corretta; l'incoerenza è nella rappresentazione del modulo come numero negativo.

**Intervento suggerito:** stampare il modulo nelle espressioni `Max |...|`, eventualmente aggiungendo a parte il valore con segno, oppure descrivere esplicitamente un estremo con segno selezionato per modulo. Non occorre cambiare la convenzione dei risultati di Analysis.

**Test da aggiungere:** estremi positivi e negativi per spostamenti, reazioni e azioni interne; conservazione dell'entità e della distanza associate.

## Verifiche eseguite e limiti

Suite completa eseguita:

```powershell
dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal
```

| Progetto | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 192 | 0 |
| Analysis.Tests | 245 | 0 |
| Reporting.Tests | 119 | 0 |
| Cli.Tests | 7 | 0 |
| Totale .NET | 563 | 0 |

I sette casi esistenti di Cli.Tests riguardano il reader JSON; non esercitano il processo CLI, il dispatcher e la scrittura dei file.

Sono state eseguite inoltre **140 invocazioni del processo CLI** attraverso tre script diagnostici locali, senza modifiche al codice applicativo:

- `out/CliBugAudit/audit.py`: 46 invocazioni per riproduzioni, controlli di base e gestione degli errori; risultati in `results.json`.
- `out/CliBugAudit/controls.py`: 86 invocazioni attese di successo: 21 esempi JSON eseguibili attraverso analyze/viewer, quattro selezioni di caso/combinazione per tutti gli otto comandi, quattro esempi integrati e otto alias. **84 successi e due fallimenti**, entrambi sul file della casa con balconi descritto al punto 4; risultati in `controls.json`.
- `out/CliBugAudit/focus.py`: otto controprove sulla casa senza nodi orfani, argomenti eccedenti, selettore di tipo errato e confronto G1/ULS1; risultati in `focus.json`.

Gli script e gli output diagnostici sono sotto `out/`, ignorato da Git. Si eseguono con `python out/CliBugAudit/audit.py`, `python out/CliBugAudit/controls.py` e `python out/CliBugAudit/focus.py`, dopo la build della CLI. Non sono ancora test di regressione persistenti che attendono il comportamento corretto.

Non sono stati classificati come bug il rifiuto del peso proprio non supportato, l'uso dei soli massimi nodali nel riepilogo degli spostamenti o l'approssimazione documentata della deformata campionata. Il controllo di processo sugli esempi verifica esito ed esportazione, non l'esattezza analitica di ogni risultato.

Questa revisione non dimostra l'assenza di ulteriori difetti. Le correzioni applicative e i test di regressione indicati restano da implementare.
