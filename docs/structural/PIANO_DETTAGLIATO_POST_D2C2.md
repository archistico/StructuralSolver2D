# StructuralSolver2D — Stato reale del progetto e piano di completamento

**Data:** 25 ottobre 2026. **Baseline esaminata:** `StructuralSolver2D_P0D2C2_INTERAZIONE_M_V_NTC2018_2026-10-25_FULL.zip`. **Natura di questo documento:** pianificazione, nessun nuovo codice o test .NET. L'autrice ha comunicato «test passati» anche per **P0-D2C2**, oltre ai gate precedenti; la conferma è registrata come **PASS dichiarato dall'utente, senza log numerico allegato**. L'ultima regressione aritmetica indipendente documentata in quel pacchetto ha **4.203/4.203 controlli Python storici PASS in 15 script**, inclusi 946 della D2C2. Questo è uno stato ereditato, non il linguaggio dei futuri test; non confondere tali controlli con il numero di test xUnit.

## 1. Obiettivo che non cambia

Realizzare un programma **open source, modulare, chiaro e verificabile** per calcolare **travi, reticolari e telai piani 2D** in **acciaio e legno**, utile a studenti, docenti e professionisti per **studio e valutazioni preliminari**. Motore, norme, controlli di qualità, report e interfaccia sono moduli distinti. Il FEM non deve cambiare formula in base alla norma scelta. L'interfaccia grafica **C#/Avalonia arriva per ultima**.

**Decisioni approvate dall'autrice:** (1) NTC 2018, EC3 ed EC5 restano **norme e moduli distinti**, con versioni e coefficienti espliciti; (2) **da questo checkpoint in poi tutto il nuovo codice, test, benchmark eseguibile e automazione di progetto è in C#/.NET**. Non introdurre, eseguire o ampliare script Python. I risultati Python ottenuti in passato rimangono dati di regressione e memoria storica; ove necessario, i nuovi verificatori C# useranno calcoli analitici o **valori attesi indipendentemente fissati**, senza invocare il medesimo algoritmo di produzione come oracolo.

Fuori dal prodotto: strutture 3D, calcestruzzo armato, muratura, BIM, piastre/pareti, X-LAM e progettazione esecutiva di collegamenti. Analisi sismica normativa, incendio, fatica e contatto non sono funzioni da promettere implicitamente. Le verifiche semiprobabilistiche NTC/Eurocodici sono l'asse principale; un eventuale metodo a tensioni ammissibili sarebbe una modalità autonoma e successiva.

## 2. Cosa esiste oggi, modulo per modulo

| Area | Funzionalità già presenti | Maturità e limiti |
|---|---|---|
| **Core** | Modelli di nodi, aste, materiali, sezioni, carichi, vincoli, combinazioni e JSON | Base utilizzata e testata; la coerenza fisica fra modello idealizzato e opera reale rimane responsabilità esplicita |
| **FEM statico** | `Frame2D` Euler–Bernoulli, `Truss2D`, telai e modelli misti; spostamenti, reazioni, N/V/M | Solutore elastico lineare **di primo ordine**, bidimensionale; benchmark ed edge case confermati. Niente rigidezza a taglio Timoshenko nella formulazione Frame2D di base |
| **Vincoli e carichi** | Rilasci, appoggi anche inclinati, molle, cedimenti, forze/momenti nodali, carichi puntuali, uniformi, lineari, peso proprio esplicito | Appoggi e truss trattati **bilateralmente**. Tension-only, distacco appoggio, torsione e ritegni spaziali non sono provati dal FEM piano |
| **Risultati e postprocessore** | Diagrammi, estremi analitici delle singole sollecitazioni, inviluppi e lati dei salti, campionamento, deformazioni, report | Un massimo di N o M non coincide necessariamente con il massimo di una **funzione di utilizzo**. Il grafico campionato non certifica il massimo esatto |
| **Analisi avanzate** | Modale/dinamica e sviluppi sperimentali non lineari/secondo ordine | Funzioni delimitate: non sono automaticamente una verifica sismica, plastica o di secondo ordine generale |
| **Azioni NTC P0-B1–B4** | Tipizzazione azioni; validazione; candidati SLU STR A1/SLE con coefficienti; audit di insiemi dichiarati e fingerprint | **Non è un generatore generale ed esaustivo** di tutte le azioni normative/varianti, siti, situazioni progettuali; schema sismico/GEO/EQU/incendio escluso dalle attuali promesse |
| **Bridge e massimi P0-C1–C2C1** | Snapshot N/V/M FEM concomitanti, combinazione/stazione/lato; diagnostici affini/polinomiali; audit risultati locali; pilota EC3 su N di trazione costante | Non è dimostrata la ricerca esaustiva del massimo `η(x)` per tutte le regole e sezioni lungo ogni asta |
| **Cataloghi M88** | Profili OPPO IPE/HEA/HEB/INP, materiali caratteristici per acciaio/legno e alcune sezioni standard | Dati tabellari **nominali e indicativi**, non attestazioni del prodotto reale; INP non supportati da molte regole avanzate |
| **Acciaio EC3 M89–M91** | Regole parziali di resistenza sezionale, classi e alcune verifiche di instabilità con dati esterni; audit M91-D | Edizione EC3 esatta e distinta da NTC; classi, azioni, ritegni e collegamenti non coperti integralmente |
| **Acciaio NTC D1, D0–D0M8, D2A–D2C2** | Trazione pura lorda/netta, classificazioni I/H pure e miste in casi delimitati, compressione pura, flessione forte pura, taglio puro, **interazione Mz–Vy con N=0** | **Molte regole sono soltanto sezionali**; mancano almeno interazioni resistenti N–M e N–M–V generali, instabilità di membratura NTC, classe 4 efficace, SLE acciaio |
| **Legno EC5 M92** | Resistenze pure di sezioni rettangolari e coefficienti EC5 | Non equivalente alla copertura delle interazioni, stabilità e SLE del percorso NTC legno |
| **Legno NTC G1–G7** | Coefficienti γM/kmod/kcr, pure e N–M, V concomitante, alcuni casi di stabilità, frecce istantanee/finali, auditor dichiarato, benchmark | Risultati circoscritti a ipotesi, stazioni, sezioni e ritegni dichiarati; non coprono tutte le geometrie, giunzioni e tipologie di carico |
| **CLI, reporting e viewer** | Analisi ed esportazioni (es. Markdown/HTML, diagrammi e viewer), esempi G8B | **Non è ancora un flusso normativo automatico completo**, e non è la GUI editor desktop finale |
| **Qualità** | 7 progetti test, benchmark, golden indipendenti, manifest, manuali, molti gate Windows confermati | Nessuna dichiarazione che tutte le funzionalità possibili siano verificate. Per G8B manca ancora un **confronto automatico .NET contro risultati indipendenti congelati**, per tutte le grandezze concordate |

### Milestone principali e conferma dei gate

1. **Motore e maturazione storica Mxx**, benchmark B13/B14 e casi limite; **G1–G7 legno NTC**: gate dichiarati PASS dall'autrice nei rispettivi ambiti.
2. **P0-A**: audit di struttura, rischi e matrice di copertura. Solo documentazione; alcune righe sono **storiche** e precedono gli sviluppi NTC acciaio D1–D2C2.
3. **P0-B1–B4**: azioni tipizzate, candidati e audit del piano dichiarato; gate B2/B3/B4 confermati PASS.
4. **P0-C1, C2A, C2B, C2C0, C2C1**: concomitanza, ricerca diagnostica degli estremi e audit puntuale; gate successivamente confermati dall'autrice, **senza trasformare i diagnostici in conformità globale**.
5. **P0-R0/R1/R2**: confronti normativi e golden di sola verifica aritmetica; questi audit non sono certificazioni del software.
6. **P0-D1 e D0–D0M8**: NTC acciaio per trazione e classificazione delle sezioni. Gate progressivamente confermati dall'autrice.
7. **P0-D2A/D2B/D2C1/D2C2**: resistenze sezionali a compressione, flessione, taglio e interazione M–V. **D2C2 ora confermato PASS dall'autrice**. `4.203/4.203` riguarda esclusivamente la regressione Python dei benchmark indipendenti, non il conteggio xUnit.

**Regola fondamentale:** un risultato `Passed` della singola verifica *non significa* che l'asta, il collegamento o l'edificio siano conformi. È vero anche se tutte le regole richieste in un report locale hanno superato il controllo: l'elenco delle regole potrebbe essere incompleto.

## 3. I rischi che dobbiamo chiudere PRIMA di aggiungere molte altre formule

| Priorità | Problema concreto | Conseguenza se ignorato | Come capiremo che è risolto |
|---|---|---|---|
| **Critica** | Insieme di combinazioni e azioni non dimostrato esaustivo | Il caso gravoso può mancare del tutto | Audit del dominio dichiarato e delle varianti: omissione intenzionale provocata nel test → **Incomplete**, mai PASS |
| **Critica** | Massimo utilizzo lungo le aste non sempre determinato | Passano le stazioni controllate ma fallisce un punto interno | Controesempi analitici con massimo interno; limite matematico dimostrato o `ScreeningIncomplete` |
| **Critica** | Assenza di registro obblighi di membratura completo | Si confonde il superamento della sezione con quello dell'asta | Mancare una sola verifica applicabile rende `MemberCoverage.Incomplete` |
| **Critica** | Ritegni laterali, appoggi monolateri, tension-only e collegamenti idealizzati | Il modello può risultare più rigido/stabile del reale | Evidenza esterna vincolata a membro/scenario; in sua assenza stato bloccante e warning fisico esplicito |
| **Alta** | Instabilità acciaio NTC di membratura e interazione N–M–V non implementate nel loro insieme | Verifica sezionale incompleta rispetto al comportamento dell'asta | Regole nuove separate, con clausole e fonti controllate; nessun PASS su N–M usando una regola di N puro |
| **Alta** | G8B non confrontato formalmente con valori attesi indipendenti per tutti i campi | Una regressione reale d'integrazione potrebbe essere inosservata | 16 scenari con verificatore scritto in C#, spostamenti/reazioni/sollecitazioni, criteri numerici fissati prima dell'esecuzione |
| **Alta** | Dati OPPO e proprietà Wpl/Aw/Av nominali non certificativi | Una proprietà tabellare può non descrivere l'elemento reale | Provenienza tipizzata, verifiche di coerenza e indicazione esplicita «dato nominale» |
| **Media** | Solutore denso e funzioni avanzate sperimentali | Prestazioni/scalabilità o campo di applicazione sopravvalutati | Benchmark dimensione, condizionamento, residui; funzioni opt-in con limiti dichiarati |
| **Media** | Documentazione cronologica con status storici contraddittori | È difficile capire quale milestone sia davvero corrente | Un solo `CURRENT_STATE`, roadmap aggiornata e manuali storici etichettati, senza cancellare le prove |

## 4. Roadmap tecnica, nell'ordine raccomandato

Le sigle che seguono sono **proposte di checkpoint**, non implementazioni o risultati già acquisiti. L'ordine tiene il progetto rigorosamente **FEM e affidabilità → verifiche normative → progettazione assistita/report → GUI**. Ogni checkpoint porta una sola responsabilità definita, test d'accettazione positivi **e negativi**, riferimento indipendente quando numerico e nessuna promozione implicita di `Passed` locale a conformità globale.

### Fase F0 — Congelamento della baseline e stato autorevole (solo documentazione)

- **F0.1:** registrare D2C2 .NET PASS sulla conferma dell'autrice, distinguendola dai 4.203 golden Python **storici**.
- **F0.2:** documento unico dello stato attuale con matrice **Implementato / Confermato con test / Coperto parzialmente / Da fare / Escluso**. Correggere l'apparente conflitto fra README ancora fermo al «G7» e handoff D2C2: nessun codice FEM toccato.
- **F0.3:** fissare la versione delle norme e delle tabelle, le convenzioni di segno e le unità per ciascun modulo.
- **Gate:** chi prende il progetto deve poter rispondere senza scorrere 100 checkpoint storici: qual è l'ultimo ZIP, quale gate è stato confermato, cosa significa `Passed`, quali funzioni sono effettivamente disponibili.

### Fase F1 — Validazione numerica d'integrazione del motore 2D

- **F1.1 (G8B):** eseguire le 4 varianti × 4 combinazioni (**16 scenari**) e verificare risultati FEM **tramite un test harness in C#/.NET** contro valori attesi indipendenti e **congelati come dati**. Le grandezze da confrontare comprendono nodi, reazioni, N/V/M a estremi e stazioni determinate, unità, orientamenti e tolleranze esplicite; aggiungere la verifica dei due viewer già predisposti. È disponibile un riferimento numerico storico generato in Python: **non eseguirlo, non estenderlo, non riscriverlo**; prima di promuovere i nuovi campi N/V/M, ottenere golden da derivazioni analitiche, tabelle verificate o altro algoritmo di riferimento indipendente, realizzato in C#. Non usare il solutore di produzione come oracolo di se stesso.
- **F1.2:** ricontrollare segni e trasformazioni (aste ruotate), carichi distribuiti, salti, cedimenti, meccanismi, release e continuità delle risultanti con test indipendenti.
- **F1.3:** definire una soglia documentata di robustezza numerica (residuo, rapporti di scala, comportamento vicino alla singolarità), non una percentuale astratta di test superati.
- **Gate:** nessun mismatch inspiegato G8B; controprove con un segno o un release alterato devono fallire; FEM base resta di primo ordine senza pretese spaziali. **Non aggiungere Timoshenko o altro solver in F1**.

### Fase F2 — P0-E0/E1: validità fisica del modello e premesse esterne

- **F2.1:** schema versionato per vincolo bilatero/monolatero, diagonale bilatera/tirante, ritegni fuori piano, lunghezze d'instabilità per asse, schemi di controvento, attacchi e basi, proprietà/certificati di materiale.
- **F2.2:** separare `KnownAndDocumented`, `Unknown`, `PhysicallyNotApplicable` con motivazione e `NotImplemented`. Ogni evidenza deve riferirsi a **specifico membro/scenario/versione del modello**; una semplice stringa non diventa prova materiale autentica.
- **F2.3:** avvisi obbligatori su appoggi a reazione negativa senza ancoraggio dimostrato, diagonali compresse in modelli assunti tension-only e verifiche spaziali non derivabili dal FEM piano.
- **Gate:** togliere la prova del ritegno fa cambiare l'esito in incompleto; un supporto monolatero non passa con reazione di trazione; modello bilatero utile ai calcoli ma mai spacciato per non lineare con contatto.

### Fase F3 — P0-B5: completezza dimostrabile delle azioni e combinazioni in un dominio limitato

- **F3.1:** fissare esattamente il primo insieme supportato: **SLU STR e SLE non sismici per strutture piane ordinarie**, con azioni permanenti e variabili documentate. Separare da questi i casi GEO/EQU, sismici, incendio, accidentali non coperti.
- **F3.2:** enumerare alternative, gruppi incompatibili, azione guida/accompagnatrici, segni/direzioni e favorevolezza con criterio esplicito; audit anche dei fattori zero/omissioni e fingerprint **effettivamente usato dal FEM**.
- **F3.3:** produrre la lista delle combinazioni *attese* con stato di copertura e riferimento alla singola norma/edizione, senza dichiarare che il generatore conosce automaticamente tutti i carichi reali dell'edificio.
- **Gate:** prove indipendenti su G+Q+neve+vento±, omissione di combinazione gravosa, duplicati, fattore scorretto e incompatibilità. Un elenco dichiarato completo ma alterato deve fallire l'audit; l'origine dei carichi è sempre esposta.

### Fase F4 — P0-C2C2: massimo della funzione di verifica lungo l'asta

- **F4.1:** stabilire per ogni **regola** se la domanda `η(x)` è dimostrabilmente continua/polinomiale/monotona a tratti, se contiene cambi di ramo o se dispone solo di una griglia diagnostica.
- **F4.2:** implementare un massimo verificato **per sottofamiglia dimostrabile**: ad esempio trazione pura N costante già C2C1, poi casi regolari di M, V e M–V D2C2 per tratti con espressioni note. Conservarne la combinazione, il membro e il lato del salto.
- **F4.3:** quando massimo continuo non dimostrabile, restituire un **valore osservato** e `ScreeningIncomplete`; non scegliere il massimo degli inviluppi indipendenti di N, V e M.
- **Gate:** caso artificiale con massimo di utilizzo fra i campioni, cambio di segno e salto puntuale; la ricerca a griglia non può produrre `MemberPassed`.

### Fase F5 — P0-C3/C4: inventario delle verifiche e report del membro

- **F5.1:** piano degli obblighi per tipo di asta e materiale: sezione, instabilità, carichi locali, SLE, necessità di collegamenti/ritegni, combinazioni e domini non supportati. Importare *risultati* da G6/M91-D senza trattare le checklist dell'utente come un generatore normativo infallibile.
- **F5.2:** definire stati aggregati autonomi: `Failed`, `Incomplete`, `ChecksPassedWithinDeclaredScope` e, **solo dopo prove di completezza del dominio**, eventuale esito più forte accuratamente denominato. Le omissioni prevalgono sui singoli PASS; i FAIL reali non devono essere nascosti da gap.
- **F5.3:** report leggibile con **chi/cosa/quando/con quali unità**, versione norma, provenienza, grafici marcati campionati o esatti, elenchi delle sezioni/combinazioni e liste puntuali dei requisiti **non verificati**.
- **Gate:** togliere una verifica di instabilità da una trave che supera M sezionale deve produrre `Incomplete`; report PDF/Markdown/HTML concordanti; nessuna intestazione «struttura verificata» generata da un subset di regole.

### Fase F6 — P0-D2D/D2E e D4: completamento mirato dell'acciaio NTC/EC3

- **F6.1:** regole di **N–M forte** sezionale per sezioni I/H compatibili, con classe e modello resistente appropriati e dati fisici; non riutilizzare la classificazione D0 come se fosse già una capacità resistente.
- **F6.2:** **N–M–V** nelle condizioni di classe e taglio per cui la norma fornisce un percorso verificabile; distinguere azione combinata da tre domande massime non concomitanti.
- **F6.3:** **instabilità d'asta** a compressione e pressoflessione e **instabilità flesso-torsionale** delle travi, con dati E espliciti. L'instabilità a taglio dell'anima e le classi 4 vanno trattate **solo** con proprietà efficaci/metodi e fonti autonomi, come checkpoint successivi; nessun obbligo di supportarle tutte prima del primo uso didattico limitato.
- **F6.4:** mantenere **EC3 e NTC** in identità, versioni e coefficienti distinti. L'EC3 di seconda generazione richiederà una nuova edizione del modulo e relativo annesso, non la sostituzione silenziosa delle regole correnti.
- **Gate:** prove di pass/fail e confine delle formule da norma primaria, carico combinato e instabilità contrastante con PASS sezionale, classe 4/fori/rifiuti, valore indipendente e regressione completa. Una nuova regola non deve mai espandere in segreto le classi e i prodotti supportati.

### Fase F7 — P0-D3 e consolidamento del legno

- **F7.1:** SLE acciaio (deformazioni, combinazioni e condizioni applicabili; eventuali vibrazioni solo se il modello e il criterio sono supportati), con massimo verificato e distinzioni fra deformazione FEM e limite normativo/di progetto.
- **F7.2:** integrare meglio legno NTC G1–G5 nelle stazioni governanti e nelle combinazioni effettive; condizioni di durata del carico/neve, umidità, ritegni e scorrimento dei giunti dichiarati e controllati nel loro ambito.
- **F7.3:** decidere i **sottoinsiemi EC5** davvero prioritari (interazione N–M, stabilità, SLE) senza ribattezzare regole NTC come Eurocodice.
- **Gate:** benchmark indipendenti per freccia e combinazioni; casi di mancata prova su umidità/durata/ritegni e influenza del collegamento devono risultare incompleti. Le esclusioni restano esplicite.

### Fase F8 — P1: qualità del motore e modello, senza espandersi nel 3D

- **F8.1:** test di scala, condizionamento e limiti dell'attuale solutore **denso**; introdurre uno sparse solo quando i benchmark dimostrano che serve.
- **F8.2:** valutare **elemento Timoshenko** soltanto per travi corte/tozze, come opzione separata dal Frame2D storico, con confronto analitico e convergenza.
- **F8.3:** valutare necessità di second'ordine più completo/instabilità piana/tension-only **solo** come estensione isolata e verificabile; evitare che le API sperimentali attuali diventino la modalità predefinita.
- **F8.4:** garantire stabilità dell'API con casi riproducibili, prestazioni e memoria misurate.
- **Gate:** benchmark e regressioni del FEM storico invariati; nuovi solutori opzionali non accettano carichi/limiti non supportati.

### Fase F9 — P2: API stabili, esempi, dimensionamento preliminare assistito e release

- **F9.1:** congelare contratti JSON/CLI/report con versione, migrazione e errori leggibili; `VERSION`, manuale utente, manuale sviluppatore e Validation Manual aggiornati.
- **F9.2:** esempi autocontenuti: trave isostatica, continua, portale acciaio, reticolare, copertura in legno, telaio misto G8B; ognuno con tabella delle verifiche disponibili e di quelle fuori campo.
- **F9.3 (facoltativo per prima release):** modulo di **progettazione preliminare** che prova profili candidati acciaio/legno già presenti nel catalogo e restituisce **candidati/screening**, mai un «profilo sicuro» se mancano obblighi.
- **F9.4:** pacchetto open source stabile, licenza, test automatici ad ogni modifica, dataset golden fissi, change log e limiti di applicabilità ben visibili.
- **Gate:** un utente può passare da JSON a risultati + esiti documentati con un comando; errori di dominio sono chiari; report leggibili su dati incompleti. Il modulo di scelta sezione non salta l'audit di copertura.

### Fase F10 — P3: interfaccia desktop Avalonia (ultimo passo)

- **F10.1:** architettura MVVM semplice; modello grafico 2D con nodi e aste, snap e selezione, proprietà modificabili tramite editor affidabile.
- **F10.2:** vincoli (con assunzioni fisiche), sezioni/cataloghi, materiali, casi e combinazioni, validazione del modello, salvataggio/apertura JSON.
- **F10.3:** esecuzione FEM e selezione della norma/versione; diagrammi N/V/M, spostamenti e reazioni, stazioni critiche, deformata e scelta delle scale; frecce e segni dei carichi testati indipendentemente.
- **F10.4:** pannello verifiche con *Passed/Failed/Incomplete/NotApplicable/Unsupported*, distinta evidenza di input esterni e generazione report.
- **Gate:** la GUI richiama gli stessi contratti/test del motore CLI senza formule proprie; round-trip UI→JSON→FEM→report identico, test di usabilità e prestazioni per modelli didattici.

## 5. Dipendenze: cosa non conviene anticipare

| Si vuole fare… | Devono prima essere soddisfatti… | Motivo |
|---|---|---|
| Dichiarare verificata **l'asta** | F2 evidenze + F3 combinazioni + F4 massimi + F5 obblighi + regole F6/F7 applicabili | Il solo `Passed` sezionale non è un giudizio completo |
| Calcolare instabilità attendibile | F2 ritegni, lunghezze, collegamenti, sezioni/materiali e norma identificati | Il FEM piano non conosce la stabilità fuori piano reale |
| Selezionare automaticamente il profilo più leggero | F5 copertura e F6/F7 resistenze pertinenti | La sezione con «massa minima» potrebbe fallire una verifica omessa |
| Fare release numerica credibile | F1 verificatore C# contro golden indipendenti G8B e regressioni future | Dati calcolati dallo stesso algoritmo di produzione non costituiscono riferimento indipendente |
| Costruire GUI definitiva | API e rappresentazioni stabili F9 | Evita di riscrivere editor e report a ogni cambiamento del contratto |
| Supportare Eurocodici seconda generazione | Edizione e annessi nazionali identificati, formule autonome e golden nuovi | Non cambiare retroattivamente significato di un risultato EC3/EC5 esistente |

## 6. Due obiettivi di rilascio distinti

**Prima beta didattica affidabile**, una volta chiusi F1–F5 e scelto un sottoinsieme F6/F7: modelli piani realmente validati, azioni documentate e combinazioni nel dominio dichiarato, verifiche puntuali e audit di ciò che manca; anche se resta `Incomplete` per molti edifici, lo strumento è onesto e utile per imparare, confrontare soluzioni e valutare preliminarmente.

**Prima release preliminare con verifiche di membratura per casi definiti**: richiede ulteriori F6/F7 e dimostrazione della stabilità/ritegni e delle stazioni governanti per le tipologie dichiarate. Non si attende la copertura universale di ogni Eurocodice, ma si vieta un risultato globale più ampio della prova disponibile. Non è un software certificato per progettazione esecutiva.

La **GUI P3** arriva dopo la maturazione del primo flusso stabile; in seguito si può ampliare gradualmente l'insieme dei casi strutturali riconosciuti.

## 7. Cosa faremo subito dopo questo passaggio di pianificazione

**Proposta operativa concreta, in tre consegne piccole e verificabili:**

1. **Prossimo checkpoint: F1.1 G8B con test harness C# e golden indipendenti congelati**. Prima test di equivalenza del motore per i 16 scenari, reazioni, spostamenti e forze interne. **Nessun nuovo Python** e nessuna nuova formula normativa.
2. **Checkpoint seguente: F2.1 contratto di evidenza fisica**. Prove esterne e warning su appoggi/controventi/ritegni; nessun solver non lineare nuovo.
3. **Terzo checkpoint: F3.1 inventario delle combinazioni dichiarate**. Mettere alla prova B3/B4 con casi mancanti e favorevolezza; decidere poi l'interfaccia con F4/C3.

In ciascuno: **uno ZIP completo**, manifest di integrità, nuova sezione di handoff, fonte o benchmark indipendente, suite Windows dell'autrice e test di non regressione. Se il gate .NET non è ancora eseguito, dirlo apertamente e non anticipare PASS.

## 8. Indicatori da registrare in ogni avanzamento

- **Stato reale:** ultimo ZIP, ultimo gate .NET confermato, esatto ambito dei test, nuovi test pending, benchmark indipendenti.
- **Copertura:** tipologie/azioni/classi/norme supportate e, separatamente, verifiche di membro/combinazioni/stazioni coperte.
- **Affidabilità:** benchmark analitici, confronto tramite C# con risultati indipendenti congelati, residui e condizioni di fallimento atteso.
- **Rischi aperti:** appoggi bilateri, tension-only, ritegni, instabilità, input di sito, dati catalogo non certificati, classi 4/fori ecc.
- **Roadmap:** checkpoint in corso, successivo, dipendenze e criterio preciso di superamento.

Le percentuali di completamento «del programma» non sono utili finché non è fissato il numero e il peso dei casi supportati. Preferire una matrice di capacità **dimostrabili** e un elenco dei **blocchi aperti**, senza confondere quantità di test con sicurezza dell'opera.

## 9. Norme: baseline e aggiornamenti futuri

- **NTC 2018:** D.M. 17 gennaio 2018, [Gazzetta Ufficiale](https://www.gazzettaufficiale.it/eli/id/2018/02/20/18A00716/sg); guida applicativa [Circolare 21 gennaio 2019 n. 7](https://www.gazzettaufficiale.it/eli/id/2019/02/11/19A00855/sg). Ogni regola deve registrare clausola/edizione e rispettivo campo di applicazione; prima dell'impiego reale va verificato l'assetto normativo applicabile al progetto.
- **Eurocodici seconda generazione:** secondo il [Joint Research Centre della Commissione europea](https://eurocodes.jrc.ec.europa.eu/second-generation-eurocodes), disponibilità dei testi entro marzo 2026, scadenza prevista per pubblicazione nazionale **30/09/2027** e per il ritiro delle norme nazionali confliggenti della prima generazione **30/03/2028**; nuovi annessi e transizioni vanno seguiti. **Decisione di architettura proposta:** congelare le attuali identità EC3/EC5 e preparare moduli/versioni nuovi, senza mescolare formule e coefficienti.

**Decisione consigliata:** niente nuova formula resistente nel prossimo checkpoint. Prima chiudere le prove di equivalenza FEM G8B e costruire la catena affidabile **azioni → FEM → stazioni governanti → applicabilità fisica → verifiche → copertura → report**. Così i nuovi moduli normativi, quando arriveranno, diventeranno utili davvero, senza essere scambiati per una verifica completa dell'opera.
