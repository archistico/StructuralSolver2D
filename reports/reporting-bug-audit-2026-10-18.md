# Ricerca bug in Reporting — 2026-10-18

Revisione di report Markdown/PDF, export CSV/XLSX, modello di visualizzazione, SVG e viewer HTML. Lo stato esaminato comprende le correzioni di Core e Analysis, presenti nel commit `93ac2e5` al termine della ricerca.

## Esito delle correzioni

**Tutti gli otto problemi descritti sotto sono stati corretti**, con 80 nuovi casi di test .NET e 15 casi eseguiti nel browser sul viewer esportato.

- Le etichette di sezione usano posizione e distanza richieste, interpolando linearmente spostamenti e rotazioni tra i campioni adiacenti. Non si tratta di una ricostruzione analitica della soluzione continua.
- Reset conserva una copia dei valori iniziali; zoom e pan usano la trasformazione inversa da schermo a SVG.
- Punto, linea e testi del richiamo del massimo seguono scala e animazione; i valori fisici riportati restano invariati.
- Il PDF controlla lo spazio verticale per ogni riga prodotta dal wrapping.
- Il Markdown riporta orientazione dei vincoli e riferimento locale delle traslazioni vincolate.
- Le opzioni di visualizzazione e del viewer richiedono valori finiti entro gli intervalli ammessi.
- L'XLSX serializza i numeri con formato invariant `R`, preservando anche valori molto piccoli.

Prima delle correzioni, i nuovi test .NET producevano 50 fallimenti e 30 successi; quelli nel browser 11 fallimenti e 4 successi. Dopo le correzioni:

| Suite | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 192 | 0 |
| Analysis.Tests | 245 | 0 |
| Reporting.Tests | 119 | 0 |
| Cli.Tests | 7 | 0 |
| Totale .NET | 563 | 0 |
| Viewer nel browser | 15 | 0 |

Comandi di verifica:

```powershell
dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal
node scripts/run-reporting-browser-tests.mjs
```

I test persistenti sono in `tests/StructuralSolver2D.Reporting.Tests/ReportingRegressionTests.cs`, `tests/StructuralSolver2D.Reporting.Tests/Visualization/ReportingOptionRegressionTests.cs` e `tests/StructuralSolver2D.Reporting.BrowserTests/`. Il comando browser richiede Chrome/Chromium/Edge installato e scrive `out/reporting-browser-tests/results.json`; usa un documento locale e un profilo separato. Non richiede pacchetti npm. La suite browser resta separata dalla soluzione .NET.

Le sezioni seguenti conservano riproduzioni, riferimenti al codice e risultati raccolti **prima delle correzioni**; gli interventi e i test suggeriti sono stati completati come descritto sopra.

## Ricerca iniziale

**Esito: otto problemi riprodotti, cinque P2 e tre P3.** P2 indica un risultato visualizzato o un comportamento funzionale errato; P3 indica un difetto circoscritto a input particolari o di minore impatto. Nessuna modifica al codice applicativo o ai test esistenti durante questa ricerca.

## 1. P2 — Etichette L/4 e 3L/4 associate a sezioni diverse

**Codice:** `src/StructuralSolver2D.Reporting/Visualization/StructuralVisualizationModelBuilder.cs:764`, `:766`, `:776`.

Il builder sceglie il campione più vicino alla sezione richiesta, ma conserva l'etichetta della sezione originaria. Con una discretizzazione che non comprende esattamente i quarti, il nome della sezione, la coordinata e il valore riportati non coincidono.

**Riproduzione:** mensola Frame2D orizzontale di 5 m, incastro in A, forza nodale verticale `-10 kN` in B; `E = 210000000`, `A = 0.003`, `I = 0.00002`. Usare `StructuralAnalysisOptions.DisplacementSampleCount = 3`, quindi costruire il modello di visualizzazione.

| Etichetta prodotta | Posizione effettiva | Distanza effettiva | Uy riportato |
|---|---:|---:|---:|
| L/4 | 0 | 0 m | 0 m |
| L/2 | 0.5 | 2.5 m | -0.031001984126984135 m |
| 3L/4 | 0.5 | 2.5 m | -0.031001984126984135 m |

Le sezioni richieste sono invece 1.25 m, 2.5 m e 3.75 m. Il numero di campioni è un'opzione pubblica valida; non è necessario fornire dati non validi per riprodurre il difetto.

**Intervento suggerito:** interpolare i campioni alla posizione richiesta secondo un criterio esplicito, oppure riportare correttamente posizione ed etichetta del campione scelto, segnalando l'approssimazione. Non presentare il valore dell'estremo come valore a L/4.

**Test da aggiungere:** campionamenti a 2, 3, 6, 11 e 21 punti; verifica congiunta di etichetta, posizione, distanza, valori e coordinate geometriche.

## 2. P2 — Reset view non ripristina la vista iniziale

**Codice:** `src/StructuralSolver2D.Reporting/Visualization/InteractiveHtmlStructuralViewerExporter.cs:159`, `:228`.

`original` conserva un riferimento a `svg.viewBox.baseVal`, che è un oggetto DOM aggiornato quando cambia il viewBox. Il pulsante Reset rilegge quindi lo stato corrente, anziché una copia delle coordinate iniziali.

**Riproduzione nel viewer esportato, eseguito in Chrome headless:**

```text
viewBox iniziale: 0 0 1400 900
dopo Zoom +:     105 67.5 1190 765
dopo Reset view: 105 67.5 1190 765
```

**Intervento suggerito:** copiare immediatamente i quattro valori iniziali in un oggetto indipendente dal DOM.

**Test da aggiungere:** eseguire davvero lo script nel browser, applicare zoom/pan e verificare l'esatto ripristino del rettangolo iniziale. La sola presenza del pulsante nel markup non verifica il comportamento.

## 3. P2 — Richiamo del massimo spostamento fermo mentre la deformata cambia

**Codice:** `src/StructuralSolver2D.Reporting/Visualization/SvgStructuralResultExporter.cs:489`, `:501`, `:502`; `InteractiveHtmlStructuralViewerExporter.cs:188`, `:196`.

Le polilinee deformate e le etichette nodali/di sezione ricevono dati per l'aggiornamento della scala. Il cerchio, la linea e i testi del richiamo `umax` sono invece esportati con coordinate statiche e non vengono aggiornati da `applyDeformation`.

**Riproduzione:** mensola del caso 1, visualizzazione con `DeformationScale = 10`. Aprire il viewer e spostare Deformed scale da 100% a 0%.

- La polilinea torna sull'asse, con ordinata SVG finale `573.714`.
- Il cerchio del massimo resta a `cy = 763.057`, uguale al valore precedente.
- Il richiamo indica quindi una posizione che non appartiene più alla deformata visualizzata. Lo stesso problema riguarda l'animazione.

I valori fisici dello spostamento possono legittimamente restare invariati quando cambia la sola amplificazione grafica; il difetto riguarda la posizione del richiamo e del suo ancoraggio.

**Intervento suggerito:** fornire al richiamo completo dati di base/spostamento e aggiornarne geometria e ancoraggi con lo stesso fattore usato per la deformata.

**Test da aggiungere:** scala 0%, 100%, 200% e fattori di animazione negativi; il punto indicato deve coincidere con il corrispondente punto della deformata.

## 4. P2 — PDF: testo a capo oltre il bordo inferiore della pagina

**Codice:** `src/StructuralSolver2D.Reporting/Pdf/PdfTechnicalReportExporter.cs:219`, `:221`, `:231`, `:234`.

La disponibilità di spazio viene controllata una volta per `ReportLine`, prima del ciclo che la suddivide in più righe visive. Una singola voce molto lunga continua a essere scritta sulla stessa pagina anche dopo aver esaurito lo spazio. Il cambio pagina avviene soltanto alla voce successiva.

**Riproduzione:** esportare il risultato della mensola con `PdfTechnicalReportOptions.SourceLabel = new string('X', 15000)`.

Risultato: PDF di tre pagine, con **102 righe di testo a coordinate Y negative**, fino a `Y = -1318`, fuori dal MediaBox A4 (`0 0 595 842`). Parte del contenuto è quindi invisibile nel documento pur essendo presente nello stream PDF.

È un caso limite di metadati lunghi accettati dall'API, distinto dalla troncatura intenzionale delle tabelle: non viene applicato `MaxRowsPerSection` alla stringa SourceLabel e non compare un avviso di troncatura per quel testo.

**Intervento suggerito:** eseguire il controllo di spazio prima di ogni riga visiva prodotta dal wrapping, includendo correttamente altezza di titoli e intestazioni.

**Test da aggiungere:** sorgente, titolo e identificatori lunghi, anche in prossimità del fondo pagina; verificare che tutte le righe restino nell'area stampabile e che il contenuto sia distribuito su un numero adeguato di pagine.

## 5. P2 — Report Markdown privo dell'orientazione meccanica dei vincoli

**Codice:** `src/StructuralSolver2D.Reporting/Markdown/MarkdownStructuralReportGenerator.cs:290`, `:294`, `:299`.

La tabella dei vincoli riporta soltanto Type e flag Ux/Uy/Rz. Non riporta `OrientationDegrees` né chiarisce nella tabella che le traslazioni vincolate sono locali. Da quando i supporti orientati sono meccanicamente attivi, queste informazioni sono necessarie per ricostruire il modello analizzato.

**Riproduzione:** aggiungere alla mensola un supporto `SimpleSupport("ROTATED", "B", orientationDegrees: 45)`, analizzare il modello aggiornato e generare il Markdown.

La tabella presenta il supporto così:

```text
ROTATED | B | SimpleSupport | Ux=no | Uy=yes | Rz=no
```

La stessa riga descriverebbe anche un carrello a 0°, che rappresenta un vincolo diverso. Il calcolo del modello a 45° restituisce invece per ROTATED circa `Fx = -9.992006 kN`, `Fy = 9.992006 kN`: la reazione inclinata è corretta, ma il report omette il dato di ingresso che la giustifica.

**Intervento suggerito:** aggiungere l'orientazione in gradi e specificare il riferimento locale di Ux/Uy. Allineare le spiegazioni educative del report alle convenzioni correnti di Analysis.

**Test da aggiungere:** report con carrelli a 0°, 45° e 90°, verificando presenza di angolo e convenzione locale, non soltanto dei flag.

## 6. P3 — Opzioni NaN/infinite accettate, con SVG non valido

**Codice:** `src/StructuralSolver2D.Reporting/Visualization/StructuralVisualizationModelBuilder.cs:894`, `:896`, `:922`; `Visualization/SvgExportOptions.cs:56`.

I controlli verificano il segno o le dimensioni relative, ma non la finitezza. Con NaN tutti i confronti di ordinamento risultano falsi; alcuni valori infiniti superano ugualmente i controlli.

**Riproduzioni verificate:**

- `VisualizationOptions.DeformationScale = double.NaN`: il builder restituisce il modello e l'export SVG contiene coordinate `NaN`.
- `SvgExportOptions.Width = double.NaN`: export completato con viewBox contenente `NaN`.
- `SvgExportOptions.Width = double.PositiveInfinity`: export completato con viewBox contenente `Infinity`.

**Intervento suggerito:** richiedere valori finiti per scale, padding, raggi e dimensioni, prima di calcolare geometria e markup. Applicare controlli analoghi alle opzioni iniziali dei controlli HTML.

**Test da aggiungere:** NaN e infinito per tutte le opzioni numeriche, più controlli positivi sui valori limite ammessi.

## 7. P3 — Zoom con rotella non conserva il punto sotto il cursore

**Codice:** `src/StructuralSolver2D.Reporting/Visualization/InteractiveHtmlStructuralViewerExporter.cs`, funzione JavaScript `zoom` da riga 166.

Il codice ricava le coordinate SVG mediante una proporzione rispetto all'intero rettangolo CSS. Questo non tiene conto delle bande introdotte da `preserveAspectRatio` quando il rapporto larghezza/altezza del contenitore differisce da quello del viewBox.

**Riproduzione nel browser:** SVG con viewBox di rapporto `1400/900`, contenitore misurato `701 × 375.953125` pixel CSS. Misurando il punto SVG sotto il cursore tramite `getScreenCTM().inverse()`, uno zoom con la rotella produce uno spostamento del punto di circa **5.9188 unità SVG**, anziché mantenerlo fisso. Sono state usate coordinate intere del cursore per escludere errori dovuti all'arrotondamento dell'evento.

**Intervento suggerito:** convertire le coordinate schermo in coordinate SVG con la matrice inversa; adottare la stessa convenzione per il trascinamento.

**Test da aggiungere:** viewport di rapporti diversi, zoom vicino ai bordi e trascinamento con letterboxing.

## 8. P3 — XLSX: valori non nulli sotto la soglia decimale esportati come zero

**Codice:** `src/StructuralSolver2D.Reporting/Xlsx/XlsxStructuralResultExporter.cs:328`.

La serializzazione numerica usa `0.###############`: è una precisione fissa di quindici cifre dopo il separatore, non quindici cifre significative. Valori piccoli vengono arrotondati a zero nel dato della cella, non soltanto nella sua visualizzazione.

**Riproduzione:** mensola del caso 1 con modulo elastico moltiplicato per `1e15`, usata come prova di scala numerica. Lo spostamento calcolato in B è `Uy = -9.920634920634916e-17 m`; nel foglio `Nodal displacements`, cella `C3`, il valore XML è `<v>-0</v>`.

Si tratta di un caso numerico estremo: non viene presentato come perdita significativa per il modello ordinario della mensola. Dimostra però che un dato non nullo accettato dall'export viene cancellato dalla serializzazione. Excel può rappresentare numeri di quest'ordine usando la notazione esponenziale.

**Intervento suggerito:** usare una serializzazione numerica generale con cifre significative/notazione esponenziale; gestire la formattazione leggibile tramite gli stili del foglio.

**Test da aggiungere:** riaprire il contenuto XML delle celle numeriche e confrontare valori positivi/negativi di piccola e grande scala, distinguendo dato memorizzato e formato visuale.

## Verifiche e artefatti

Comando eseguito sulla soluzione: `dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal`.

| Progetto | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 192 | 0 |
| Analysis.Tests | 245 | 0 |
| Reporting.Tests | 39 | 0 |
| Cli.Tests | 7 | 0 |
| Totale | 483 | 0 |

Le prove aggiuntive sono nel progetto diagnostico locale ignorato da Git `out/ReportingBugAudit/ReportingBugAudit.csproj`. Il programma genera `viewer.html`, `viewer-audit.html` e `long-source.pdf`; legge inoltre l'XML del workbook XLSX e stampa i valori rilevanti.

Esecuzione delle riproduzioni .NET: `dotnet run --project out/ReportingBugAudit/ReportingBugAudit.csproj --no-restore`.

Il comando `node out/ReportingBugAudit/browser-audit.mjs` avvia Chrome headless sul documento locale con un profilo separato sotto `out/ReportingBugAudit`, esegue i controlli DOM e stampa i risultati JSON. La prova nel browser ha richiesto esecuzione fuori dalla sandbox, autorizzata dall'utente dopo il fallimento dell'avvio nella sandbox. Nessun sito esterno è usato per le verifiche.

I test esistenti del viewer verificano soprattutto la presenza di markup e script; quelli PDF controllano intestazioni/sezioni, senza verificare le coordinate stampabili. Le riproduzioni diagnostiche non sostituiscono ancora test persistenti che attendono il comportamento corretto.

Non è stata classificata come bug la differenza documentata tra interpolazione della deformata e soluzione continua esatta. Il richiamo del massimo attuale viene costruito dai soli nodi: prima di estenderlo agli estremi interni delle aste occorre definire esplicitamente se debba rappresentare il massimo nodale o quello campionato.

La revisione non dimostra l'assenza di ulteriori difetti. Gli export e i risultati già presenti nel repository sono stati lasciati invariati.
