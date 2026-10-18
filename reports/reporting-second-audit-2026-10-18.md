# Seconda revisione Reporting: bug ed ergonomia — 2026-10-18

Base esaminata: commit `265b9ee` (`fix cli`), comprendente le correzioni della prima revisione Reporting. Esaminati viewer interattivo, preview HTML, SVG, Markdown, PDF e XLSX, includendo quantità delle informazioni, leggibilità e uso su schermi stretti.

**Esito della verifica finale: corretti i nove bug e affrontate le quattro criticità ergonomiche.** Aggiunti 34 casi di regressione Reporting e un caso CLI; suite completa .NET: **767 superati, 0 falliti**. Verifiche browser: **180 superate, 0 fallite**, su mensola, casa e ponte a tre dimensioni dello schermo. Verificate anche selezione con mouse e tocco tramite eventi reali del browser e schermate finali.

## Correzioni applicate

| Casi | Soluzione e copertura |
|---|---|
| B1 | Vincoli, reazioni e quote in gruppi SVG indipendenti. Test delle 16 combinazioni di flag e dei controlli nel browser. |
| B2 | Un solo ciclo di animazione; Play idempotente, Pause cancella il frame pendente. Verificata anche la ripartenza. |
| B3 | Wrapping PDF secondo font e dimensione effettivi; forze d'estremità su righe Start/End, identificativi lunghi separati. Test dei margini orizzontali, titoli e valori conservati; restano attivi i test di paginazione. |
| B4 | WinAnsi/Windows-1252 con escape PDF corretti: accenti italiani, punteggiatura e simboli supportati conservati. Test del testo decodificato. Altri alfabeti producono un errore esplicito; non viene più sostituito il testo con `?`. |
| B5 | Identificativi e testi Markdown proteggono pipe, backtick, HTML, backslash e ritorni a capo. Test delle colonne e dei casi particolari. |
| B6 | Magnitudini non negative nei riepiloghi Markdown/PDF/XLSX; segni mantenuti nelle tabelle dettagliate. Test con carico positivo e negativo. |
| B7 | Statistiche limitate alla sezione dimensioni; verifiche attivate/disattivate coerentemente anche nel riepilogo. Test delle quattro combinazioni. |
| B8 | Copia locale delle opzioni SVG in viewer e preview. Test dello stato e dell'export SVG prima/dopo il riuso. |
| B9 | Slider senza normalizzazione a step fissi e serializzazione numerica fedele. Verificati 102%, 117% e velocità 1.1 nel browser. |
| E1, E2 | Vista iniziale Modello con vincoli e frecce dei carichi; modalità Spostamenti/N/V/M, vista completa opzionale, strumenti secondari richiudibili e pannello dei dettagli su selezione. Layout adattivo e tratti leggibili su schermi stretti; tastiera, mouse e tocco. Le aree di selezione non vengono coperte da carichi/vincoli. |
| E3 | Profili Summary/Full. CLI `report` e alias `markdown` sintetici, `report-full` completo. Summary conserva tutti i nove estremi con posizione/entità, unità, caso/combinazione e fattori, tabelle con valori a segno e verifiche richieste. Il default API rimane Full. Test del confronto fra tutti gli estremi nei due profili e dei comandi CLI. |
| E4 | Feedback dell'amplificazione corrente, anche durante l'animazione; Fit sulla geometria attualmente visibile. Test della deformata a 200% e dei punti contenuti nella vista. |

### Misure dopo le correzioni

| Misura | Prima | Dopo |
|---|---:|---:|
| Ponte 1366 × 900: testi SVG iniziali visibili | 205 | 0; valori disponibili su selezione e nei livelli opzionali |
| Ponte: coppie di bounding box testuali sovrapposte nella vista iniziale | 350 | 0 |
| Controlli principali inizialmente esposti | 21 | 5 |
| Casa 390 × 844: larghezza scorrevole documento viewer | 730 px | 375 px, entro la viewport di 390 px |
| Casa: altezza barra viewer | 407 px | 112 px |
| Casa: inizio canvas | 630 px | 200 px |
| Casa: canvas nella prima schermata | 214 px | 589 px |
| Preview statica 390 px: larghezza documento | 569 px | 390 px |

Le etichette globali non sono necessarie per leggere i risultati: il pannello mostra coordinate, spostamenti e rotazioni del nodo, oppure tipo, nodi estremi, lunghezza, massimi N/V/M e valori di sezione dell'asta. `All layers` e i singoli interruttori conservano l'accesso alla scena completa.

| Modello | Righe precedente CLI `report` | Nuovo Summary | Nuovo Full con campioni di spostamento |
|---|---:|---:|---:|
| Mensola | 237 | 97 | 239 |
| Casa | 1796 | 130 | 1798 |
| Ponte | 3113 | 154 | 3115 |
| Tre piani | 1790 | 129 | 1792 |

Le due righe aggiuntive del profilo completo identificano esplicitamente il contesto di carico. Il profilo sintetico mantiene tutte le tabelle dei risultati nodali e d'estremità; riduce i campioni e le ripetizioni, non i risultati dell'analisi.

Verifiche finali ripetibili:

```powershell
dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal
node scripts/run-reporting-browser-tests.mjs
```

| Suite finale | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 192 | 0 |
| Analysis.Tests | 245 | 0 |
| Reporting.Tests | 153 | 0 |
| Cli.Tests | 177 | 0 |
| Totale .NET | 767 | 0 |
| Viewer: controlli browser su tre modelli/tre finestre | 180 | 0 |

Schermate e output finali locali:

- [Ponte: vista iniziale desktop](../out/ReportingSecondAudit/bridge-after-1366.png).
- [Casa: vista iniziale 390 px](../out/ReportingSecondAudit/house-after-390.png).
- [Ponte: momento e pannello dell'asta](../out/ReportingSecondAudit/bridge-m-selected-after-1366.png).
- [Viewer interattivo del ponte](../out/ReportingSecondAudit/bridge.html).
- [Report sintetico del ponte](../out/ReportingSecondAudit/bridge-summary.md).
- [Misure browser finali](../out/ReportingSecondAudit/browser-results-after.json) e [risultati dei test persistenti](../out/reporting-browser-tests/results.json).

Il PDF supporta il repertorio Windows-1252; per altri alfabeti occorrono Markdown/XLSX oppure una futura implementazione con font Unicode incorporati. La preview statica conserva la scena completa e può avere etichette sovrapposte: per l'ispezione su schermi piccoli è disponibile il viewer. I controlli PDF verificano testo, coordinate, avanzamenti dei font e paginazione; non includono una rasterizzazione. Le verifiche ergonomiche non sostituiscono uno studio con utenti.

Le sezioni successive conservano i rilievi sulla base iniziale e le prove prima delle correzioni. Le riproduzioni e le immagini sono sotto `out/ReportingSecondAudit/`, ignorato da Git. Gli otto problemi della prima ricerca non sono ricontati: i relativi test continuano a passare. I nove bug originari sono cinque P2 e quattro P3; le quattro criticità ergonomiche sono valutazioni di usabilità supportate dalle misure.

## Priorità di intervento

1. Rendere leggibile la vista iniziale: un risultato attivo alla volta, annotazioni essenziali e dettagli su selezione/focus; raggruppare i controlli secondari e correggere il layout stretto.
2. Correggere indipendenza dei livelli e ciclo di animazione; rendere esplicita la scala di deformazione effettiva.
3. Correggere perdita di testo/colonne nei PDF e nelle tabelle Markdown.
4. Offrire un report sintetico con dettaglio completo opzionale, mantenendo coerenti opzioni e riepiloghi.

## E1 — Vista iniziale sovraccarica di diagrammi ed etichette

**Priorità ergonomica: alta.** Codice: `Visualization/InteractiveHtmlStructuralViewerExporter.cs:110` e controlli successivi; `Visualization/SvgStructuralResultExporter.cs:170`, `:580`.

La configurazione iniziale mostra contemporaneamente modello, deformata, carichi e loro etichette, N, V, M, vincoli, reazioni, quote e annotazioni degli estremi. La barra espone 21 controlli senza una gerarchia fra uso principale e avanzato. Le etichette di spostamento nodali e di sezione sono già nascoste: il sovraccarico avviene anche senza abilitarle.

Nel viewer del ponte da 41 aste, a 1366 × 900, sono presenti **205 testi SVG visibili**. Le bounding box producono **350 coppie di intersezioni**; nella casa da 20 aste sono 129 testi e 181 coppie. Si vedono sovrapposizioni fra quote, estremi dei diagrammi, nomi dei nodi e carichi. Lo screenshot conferma che più valori non sono leggibili separatamente.

Le intersezioni sono un indicatore geometrico, non una misura automatica di 350 difetti distinti: comprendono testi duplicati e possono essere sensibili a font e viewport. Il problema di leggibilità è comunque visibile direttamente nella scena.

**Proposta:** modalità principali `Modello`, `Spostamenti`, `N`, `V`, `M`, con una sola quantità risultante attiva per volta. Carichi, quote e valori secondari accessibili da un gruppo dettagli. Mettere i dati dell'asta/nodo selezionato in un pannello leggibile, disponibile anche tramite tastiera e tocco. Conservare un'opzione per la vista completa.

**Verifica proposta:** scena semplice, casa e ponte; screenshot desktop/tablet; numero di etichette inizialmente visibili e assenza di annotazioni duplicate sugli stessi punti; possibilità di raggiungere tutti i dati senza attivare ogni livello insieme.

## E2 — Layout stretto: overflow e area di lavoro quasi fuori dalla prima schermata

**Priorità ergonomica: alta; difetto responsive riprodotto.** Codice: `Visualization/InteractiveHtmlStructuralViewerExporter.cs:70`, `:79`, `:86`; `Visualization/HtmlStructuralResultPreviewExporter.cs:38`.

Il riepilogo del viewer mantiene sei colonne con minimo 110 px; quello della preview quattro colonne con minimo 120 px. Non sono presenti adattamenti per viewport strette. La barra si distribuisce su molte righe, mentre l'altezza del canvas continua a dipendere da `78vh`, senza considerare l'area occupata da titolo, descrizione e controlli.

Misure sul file della casa:

| Misura | Viewer 390 × 844 | Preview statica 390 × 844 |
|---|---:|---:|
| Larghezza scorrevole del documento | 730 px | 569 px |
| Altezza barra controlli | 407 px | — |
| Inizio canvas dall'alto | 630 px | 325 px |
| Altezza totale canvas | 658 px | 205 px |
| Porzione canvas nella prima schermata | 214 px | 205 px |
| Altezza mediana dei testi SVG renderizzati | 4 px | 4 px |

Nel viewer la porzione inizialmente visibile è bianca; la struttura si trova più in basso nella scena. La pagina richiede anche scorrimento orizzontale. A 768 px la barra è già alta 231 px e i testi del ponte hanno altezza mediana di 7 px.

Il canvas inoltre non è focalizzabile e non implementa comandi di pan da tastiera. I pulsanti di zoom sono accessibili come controlli nativi, ma non sostituiscono il trascinamento per esplorare le parti fuori vista.

**Proposta:** riepilogo a colonne adattive, descrizione e strumenti secondari richiudibili, area di lavoro dimensionata sullo spazio disponibile. Rendere utilizzabile il pan anche da tastiera e prevedere una vista adattata al contenuto. Non ridurre semplicemente tutti i testi per farli entrare.

**Verifica proposta:** 390, 768 e 1366 px; assenza di overflow orizzontale del documento, struttura raggiungibile nella prima schermata, navigazione dei controlli e della scena senza mouse.

## E3 — Markdown molto lungo e privo di un profilo sintetico

**Priorità ergonomica: media.** Codice: `Markdown/MarkdownReportOptions.cs:31`, `:41`, `:51`; `Markdown/MarkdownStructuralReportGenerator.cs:231`, `:465`, `:525` e sezioni dei risultati.

Il report ripete estremi nel riepilogo, nei risultati, nei campioni e nei punti caratteristici. I limiti sono per singola asta, senza una scelta di profilo o un limite globale. Per una quantità costante o nulla, minimo, massimo e massimo per modulo possono aggiungere righe allo stesso punto senza informazioni nuove.

| Modello | Aste | Righe Markdown di default | Righe di tabella di default | Righe con campioni di spostamento |
|---|---:|---:|---:|---:|
| Mensola | 1 | 209 | 104 | 237 |
| Casa | 20 | 1348 | 1053 | 1796 |
| Ponte | 41 | 2889 | 2384 | 3113 |
| Tre piani | 21 | 1370 | 1065 | 1790 |

Le righe di tabella includono intestazioni e separatori. Il profilo con campioni di spostamento riflette l'impostazione attuale del comando CLI `report`; non è il default di `MarkdownReportOptions` per quel singolo flag.

La completezza è utile, ma l'utente non dispone di un profilo compatto che mantenga subito visibili risultati principali, caso/combinazione, unità e limiti, spostando i dettagli in sezioni successive o file separati.

**Proposta:** profilo sintetico e profilo completo; un riepilogo coerente, tabelle degli estremi utili e campioni su richiesta. Evitare duplicati sui medesimi punti e descrivere i valori omessi, mantenendo CSV/XLSX per il dettaglio numerico integrale. Non eliminare dati dal modello o dagli export completi.

**Verifica proposta:** confronto di contenuti indispensabili fra i profili; assenza di perdita di id/posizione dei risultati critici; report compatto che non cresca di 21 campioni per ogni asta.

## E4 — La scala effettiva non è spiegata nel feedback della vista

**Priorità ergonomica: media.** Codice: `Visualization/InteractiveHtmlStructuralViewerExporter.cs:106`, `:138`, `:245`; `Visualization/SvgStructuralResultExporter.cs:34`.

Il significato documentato dello slider è percentuale rispetto alla scala con cui è stato costruito il modello di visualizzazione. Il riepilogo e la didascalia SVG però mostrano semplicemente `Deform. scale: 100` e `Deformation scale: 100`, senza distinguere scala base e amplificazione effettiva.

Su una scena costruita con scala 100:

| Slider | Scala grafica effettiva | Didascalia e riepilogo |
|---|---:|---|
| 0% | 0 | 100 |
| 200% | 200 | 100 |

La polilinea cambia realmente: la punta passa da Y SVG 195.174 a 1424.022, mentre entrambi i testi restano invariati. A 200% esce anche dal viewBox originale alto 900. Lo zoom manuale consente di recuperarla; Reset ripristina soltanto la vista iniziale.

Non è un cambiamento dei valori fisici dell'analisi. Il problema è la mancanza di un'indicazione chiara dell'amplificazione attualmente visualizzata.

**Proposta:** mostrare `Amplificazione ×200`, mantenendo la scala base fra i dettagli; aggiungere `Adatta vista` al contenuto corrente e distinguere animazione grafica da analisi dinamica. Il richiamo del massimo può continuare a riportare i valori fisici invariati.

**Verifica proposta:** scala 0%, 100%, 200%, impostazione iniziale non predefinita e animazione; coerenza fra feedback e geometria, con possibilità di recuperare l'intera struttura senza tentativi ripetuti di zoom.

## B1. P2 — Nascondere Undeformed elimina livelli che risultano ancora attivi

**Codice:** `Visualization/SvgStructuralResultExporter.cs:119`, `:128`, `:136`, `:155`; `Visualization/InteractiveHtmlStructuralViewerExporter.cs:110`, `:118`, `:260`.

Vincoli, reazioni e quote sono figli di `#undeformed-model`. Nascondere il gruppo nasconde anche questi elementi, mentre le rispettive checkbox restano selezionate.

**Riproduzione:** viewer della mensola. Prima: un simbolo di vincolo, due primitive di reazione e una quota visibili. Disattivando `Undeformed`: tutti a zero, con `Supports`, `Reactions` e `Dimensions` ancora selezionati. Riattivare una sola checkbox figlia non ripristina il contenuto finché il gruppo padre è nascosto.

Anche nell'API SVG, `IncludeUndeformedModel = false` insieme a `IncludeSupportSymbols = true` e `IncludeReactions = true` non produce vincoli/reazioni.

**Intervento suggerito:** gruppi indipendenti oppure dipendenze esplicite e stato dei controlli coerente. Per separare la lettura della deformata da vincoli e reazioni è preferibile poter mantenere questi ultimi visibili.

**Test proposti:** tutte le combinazioni delle quattro visibilità nel browser e controllo strutturale dei gruppi SVG; includere la visibilità effettiva degli antenati.

## B2. P2 — Play ripetuto crea più cicli di animazione

**Codice:** `Visualization/InteractiveHtmlStructuralViewerExporter.cs:219`, `:225`, `:243`.

Ogni clic su Play chiama `requestAnimationFrame`, anche quando l'animazione è già avviata. Ogni callback si rischedula autonomamente e usa il medesimo stato globale. Non vengono conservati né annullati gli id dei frame.

**Riproduzione nel documento esportato**, sostituendo solo il pianificatore dei frame con una coda deterministica:

```text
Dopo un clic:                  1 callback
Dopo due clic:                 2 callback
Dopo aver eseguito un frame:   2 callback rischedulate
Dopo Pause/Play senza drenare: 3 callback
```

Su modelli grandi questo moltiplica aggiornamenti delle polilinee, parsing dei punti e aggiornamenti delle etichette senza aggiungere informazioni.

**Intervento suggerito:** Play idempotente, un solo frame pianificato, cancellazione del frame pendente in pausa e stato dei pulsanti esplicito.

**Test proposti:** doppio clic, dieci clic, Pause/Play rapido, pausa con frame pendente; una sola catena attiva e nessun aggiornamento residuo dopo la pausa.

## B3. P2 — Il PDF perde cifre sul bordo destro

**Codice:** `Pdf/PdfTechnicalReportExporter.cs:22`, `:121`, `:221`, `:270`.

Il wrapping usa un numero fisso di caratteri, indipendente dalla larghezza del font e dall'area disponibile. La correzione precedente riguarda il controllo verticale di ogni riga e non risolve questo problema orizzontale.

**Riproduzione:** trave orizzontale di 4 m, entrambi gli estremi incastrati, carico uniforme -10 kN/m; id asta `BEAM_GROUND_FLOOR_01`. La riga delle azioni agli estremi contiene 110 caratteri, meno del limite 112, quindi non viene suddivisa:

```text
BEAM_GROUND_FLOOR_01       0.000000      20.000000      13.333333       0.000000      20.000000     -13.333333
```

Il PDF usa Courier 8.5 pt per questa riga. Con avanzamento 0.6 em, la fine del testo è a `48 + 110 × 8.5 × 0.6 = 609 pt`, oltre il MediaBox largo **595 pt** e il margine utile destro a **547 pt**. Le cifre finali di M2 finiscono quindi fuori pagina. Questa verifica è geometrica sul font e sullo stream PDF; non deriva da uno screenshot raster del PDF.

**Intervento suggerito:** dimensionare testo e colonne in punti, usando le metriche del font; gestire id lunghi senza perdere l'associazione fra colonna e dato. Verificare anche titoli e intestazioni, che hanno font diversi.

**Test proposti:** limiti X/Y con font e dimensioni reali, identificatori ordinari/lunghi, valori grandi/negativi, preservazione delle ultime cifre e delle intestazioni.

## B4. P2 — Il PDF sostituisce accenti e identificatori con punti interrogativi

**Codice:** `Pdf/PdfTechnicalReportExporter.cs:377`, `:381`.

`EscapePdfText` conserva solo ASCII stampabile; tutti gli altri caratteri diventano `?`. Il writer usa poi ASCII. Non riguarda solo simboli decorativi: vengono modificati anche gli identificatori dell'analisi e i nomi dei file.

**Riproduzione verificata:**

| Input | Stream PDF |
|---|---|
| `Relazione – capacità e qualità` | `Relazione ? capacit? e qualit?` |
| `modello_è.json` | `modello_?.json` |
| Analysis id `Été` | `Analysis id: ?t?` |

Gli input sono accettati dal modello e dall'analisi. Identificatori diversi che differiscono per questi caratteri possono diventare indistinguibili nel report.

**Intervento suggerito:** codifica e font compatibili almeno con i caratteri italiani e i simboli usati dal report; evitare conversioni silenziose per gli altri caratteri, definendo un comportamento esplicito.

**Test proposti:** accenti italiani, nomi/id con accenti, apostrofi e trattini tipografici; estrazione/decodifica del testo con la codifica dichiarata nel PDF.

## B5. P2 — Identificatori e testi multilinea rompono le tabelle Markdown

**Codice:** `Markdown/MarkdownStructuralReportGenerator.cs:231`, righe analoghe nelle altre tabelle, e `:649`.

Gli id sono interpolati in code span senza proteggere i delimitatori di tabella. Il metodo `Text` protegge le pipe di alcuni campi testuali ma non i ritorni a capo. Il Core non vieta questi identificatori o etichette.

**Riproduzione:** nodo id `A|base`, etichetta `Appoggio\nsinistro`. Il report contiene:

```text
| `A|base` | 0.000000 | 0.000000 | Appoggio
sinistro |
```

La pipe nell'id introduce una separazione di cella e il ritorno a capo interrompe la riga. Lo stesso id ricompare nelle tabelle di aste, vincoli, reazioni e riepiloghi, estendendo il difetto a più sezioni. Anche i backtick negli id richiedono una gestione coerente del code span.

**Intervento suggerito:** una serializzazione di cella condivisa per id e testo, che gestisca delimitatori e contenuti multilinea secondo il formato Markdown scelto.

**Test proposti:** pipe, backtick, CR/LF e combinazioni nei campi ammessi; numero di celle e conservazione del testo nel Markdown renderizzato.

## B6. P3 — Massimi assoluti ancora negativi nei report

**Codice:** `Markdown/MarkdownStructuralReportGenerator.cs:165`, `:421`; `Pdf/PdfTechnicalReportExporter.cs:171`; `Xlsx/XlsxStructuralResultExporter.cs:70` e righe successive.

La CLI ora stampa il modulo, ma i tre formati Reporting continuano a mostrare il valore con segno in celle dichiarate `Max |...|` o sotto `Governing absolute values`.

**Riproduzione:** mensola di 4 m, carico nodale -12 kN. Markdown/PDF riportano `Max |Uy| = -0.060952` e `Max |M| = -48`. Nell'XML del foglio Summary XLSX sono memorizzati circa `-0.06095238095238096` per Max |Uy|, `-0.02285714285714286` per Max |Rz| e `-48.00000000000001` per Max |M|.

I valori con segno sono corretti per il risultato critico; è la dicitura di valore assoluto a essere incoerente. Non occorre modificare i risultati di Analysis.

**Intervento suggerito:** condividere la convenzione fra CLI e report, mostrando modulo e, se utile, valore con segno in campi distinti.

**Test proposti:** positivi/negativi nei tre formati, dettaglio con segni invariati e posizione dell'estremo preservata.

## B7. P3 — Le opzioni Markdown non isolano i contenuti indicati

**Codice:** `Markdown/MarkdownStructuralReportGenerator.cs:142`, `:170`, `:595`; descrizioni in `Markdown/MarkdownReportOptions.cs:31`, `:61`.

Con un risultato che include verifiche preliminari, `IncludeDeflectionChecks = false` omette il corpo ma lascia nel riepilogo conteggi pass/fail, utilizzo e membro governante. Il report contiene quindi sia risultati di verifiche sia la frase che le verifiche sono omesse dalle opzioni.

Inoltre `IncludeModelStatistics = false`, descritto come controllo delle statistiche del modello, elimina l'intero Executive summary, compresi valori governanti e riepilogo delle verifiche. Tentare di ridurre informazioni secondarie rimuove così anche il punto di ingresso ai risultati principali.

**Intervento suggerito:** applicare ogni opzione a tutte e sole le sezioni pertinenti; separare statistiche, riepilogo risultati e riepilogo verifiche. Il profilo compatto proposto in E3 deve poter mantenere gli estremi anche senza statistiche.

**Test proposti:** combinazioni dei flag con verifiche presenti/assenti, indipendenza delle sezioni e assenza di messaggi contraddittori.

## B8. P3 — L'export HTML modifica le opzioni SVG del chiamante

**Codice:** `Visualization/InteractiveHtmlStructuralViewerExporter.cs:20`, `:21`, `:22`; `Visualization/HtmlStructuralResultPreviewExporter.cs:19`.

Il viewer modifica direttamente titolo e flag di etichettatura nell'istanza `SvgOptions` ricevuta. La preview modifica il titolo. Se il chiamante condivide questa istanza con l'export SVG statico, il risultato successivo cambia in base all'ordine di chiamata.

**Riproduzione:** istanza SVG con titolo `Original SVG` ed entrambe le opzioni delle etichette false. Esportare SVG, poi viewer con titolo `Viewer title` usando la stessa istanza, poi SVG. Dopo il viewer, titolo `Viewer title`, entrambe le opzioni true e SVG statico diverso dal primo.

Il viewer può incorporare le etichette necessarie ai suoi controlli, ma non deve per questo alterare le impostazioni riutilizzate dal chiamante.

**Intervento suggerito:** derivare opzioni locali senza mutare l'istanza ricevuta.

**Test proposti:** stato dell'oggetto prima/dopo, riuso con viewer/preview/SVG in ordini diversi, flag e titolo dell'export statico preservati.

## B9. P3 — Valori iniziali ammessi dall'API vengono arrotondati dal browser

**Codice:** `Visualization/InteractiveViewerExportOptions.cs:65`; slider in `Visualization/InteractiveHtmlStructuralViewerExporter.cs:106` e righe successive.

La validazione controlla intervallo e finitezza, mentre gli slider impongono step 5 per percentuali e 0.25 per velocità. Il browser normalizza gli input a un valore allineato allo step.

**Riproduzione:** export completato con opzioni iniziali valide per l'API:

| Opzione | Richiesta | Valore effettivo nel DOM |
|---|---:|---:|
| Deformazione percentuale | 102 | 100 |
| Testo percentuale | 117 | 115 |
| Velocità animazione | 1.1 | 1 |

Lo script legge poi il valore DOM e aggiorna geometria/feedback da quello. Il problema è circoscritto ai valori non allineati agli step; i default restano corretti.

**Intervento suggerito:** accettare e rappresentare la precisione prevista dall'API oppure validare/documentare gli step ed eventuale normalizzazione prima dell'export.

**Test proposti:** valori allineati/non allineati, estremi dell'intervallo e confronto fra impostazione accettata, DOM, output e fattore applicato.

## Verifiche e artefatti prima delle correzioni

Suite completa eseguita sulla base iniziale:

```powershell
dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal
node scripts/run-reporting-browser-tests.mjs
```

| Suite | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 192 | 0 |
| Analysis.Tests | 245 | 0 |
| Reporting.Tests | 119 | 0 |
| Cli.Tests | 176 | 0 |
| Totale .NET | 732 | 0 |
| Viewer: test browser esistenti | 15 | 0 |

I casi rilevati erano lacune della copertura esistente, non fallimenti dei test già presenti. La tabella descrive la fase di sola ricerca; copertura aggiunta e risultati finali sono riportati all'inizio del documento.

Riproduzioni diagnostiche locali:

```powershell
dotnet run --project out/ReportingSecondAudit/ReportingSecondAudit.csproj
python out/ReportingSecondAudit/analyze_outputs.py
node out/ReportingSecondAudit/browser-audit.mjs
```

Il progetto .NET legge quattro esempi reali e produce viewer, preview, Markdown/PDF e un XLSX, oltre ai casi particolari descritti sopra. `stats.json`, `output-results.json` e `options-mutation.json` contengono le misure degli export. Lo script browser usa Chrome headless, DevTools Protocol e un profilo locale separato; scrive `browser-results.json` e screenshot. Non usa siti esterni né pacchetti npm. L'avvio del browser ha richiesto esecuzione fuori dalla sandbox, autorizzata dopo l'interruzione della sessione nella sandbox.

Schermate locali ispezionate:

- [Ponte, desktop 1366 px](../out/ReportingSecondAudit/bridge-1366.png).
- [Casa, viewport 390 px](../out/ReportingSecondAudit/house-390.png).
- [Preview statica, viewport 390 px](../out/ReportingSecondAudit/house-static-390.png).

Le misure dipendono da viewport/font/versione del browser. La prova di animazione usa lo script reale e una coda deterministica per `requestAnimationFrame`, senza misurare direttamente il frame rate. L'analisi del PDF controlla testo, coordinate e avanzamento Courier, non una sua renderizzazione raster. Non è stata misurata la comprensione degli utenti con uno studio di usabilità.

Questa revisione non dimostra l'assenza di altri difetti. L'estensione del massimo nodale a massimi interni campionati e la natura interpolata della deformata restano scelte da trattare esplicitamente, senza confonderle con gli errori di presentazione qui riprodotti.
