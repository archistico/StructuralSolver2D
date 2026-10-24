> **CHECKPOINT CORRENTE — 24/10/2026, M86 RC1:** M85-C ha superato su Windows 8/8 test, 1.334/1.334 regressioni, zero warning e zero error. M86 RC1 è una source preview che aggiunge manifest, documenti di accettazione, smoke CLI e gate Release; il gate Windows RC1 è ancora aperto. Nessuna modifica al solver. M83-B e M84 differiti; normative e GUI ancora non implementate. [Nota M86](m86-release-preview.md). Tutti i checkpoint sottostanti sono storici.

> **CHECKPOINT CORRENTE — 24/10/2026, M85-C:** M85-B FIX1 confermato su Windows: build 0 errori/avvisi, 5 test integrati, regressione 1.326/1.326, benchmark M83 8/8 a 20 ripetizioni. M85-C aggiunge 8 verifiche statiche indipendenti/invarianti con runner `scripts/run-m85c-tests.ps1`; **test Windows ancora aperti**. Zero modifiche a `src/`. Il solver sparso e la plasticità globale sono rinviati; M82 resta globalmente linearizzato e limitato. Prossimo gate: M86 release preview Core elastico, con checklist e limitazioni dichiarate. [Dettagli M85-C](m85c-final-static-validation.md) · [Checklist M86](m86-release-acceptance-checklist.md). I checkpoint precedenti qui sotto sono storici e non rappresentano lo stato attuale.

> **Checkpoint CORRENTE 24/10/2026 — M85-A:** l'utente ha confermato i test M83-A superati su Windows. Non abbiamo il CSV dei tempi e allocazioni: M83-B (solver sparso) resta **condizionato alle misure**. M84 è opzionale/differibile. M85-A aggiunge nuove verifiche FEM statiche indipendenti (formule chiuse, reciproca Maxwell–Betti, superposizione, rotazione); **gate Windows non ancora eseguito**. Il codice numerico esistente non è modificato. [Matrice M85-A](m85a-independent-validation.md). I checkpoint datati sottostanti descrivono lo stato storico dei singoli milestone.

> **Checkpoint aggiornato 24/10/2026:** test M82 Windows confermati dall’utente dopo il FIX CS0246. M83-A aggiunge benchmark di scalabilità su telai Frame2D reali, con nuovo gate Windows aperto; il solver sparso M83-B rimane **condizionato ai dati prestazionali**. Il motore FEM lineare/non lineare non viene modificato. [Nota M83-A](m83a-frame2d-scalability-decision.md).

> **Stato operativo 24/10/2026:** M75–M81 **testati con successo dall’utente**; M82 **implementato come prima API globale di rigidezza geometrica linearizzata, test Windows da eseguire**. M82 affronta solo parte dell'ambizione pianificata per secondo ordine/eigenbuckling: campo assiale congelato, singolo caso nodale, telaio senza svincoli, supporti rigidi non inclinati, moltiplicatore critico senza forma modale. Le parti restanti richiedono ulteriori milestone e benchmark prima di chiudere la capacità generale. [Nota tecnica M82](m82-global-linearized-second-order.md). M83 (solver sparso) dipende dai dati prestazionali M73; non introdurre una sostituzione per principio.

> **Checkpoint operativo M81 (24/10/2026):** il gate cumulativo **M80-FIX3 è PASSATO** sul Windows dell’utente; quindi M75–M80 sono validati. **M81** è implementato internamente con rigidezza geometrica lineare di Frame2D e benchmark C#, **ma attende i test Windows**; [nota tecnica](m81-frame2d-initial-stress.md). Il secondo ordine/eigenbuckling globale è M82; nessuna verifica normativa o GUI anticipata.

# StructuralSolver2D — Visione di prodotto e roadmap operativa 2D

**Data:** 23 ottobre 2026  
**Checkpoint di riferimento:** M73 test superati dall'utente; M75 implementato e in attesa di test  
**Stato del documento:** nuova roadmap strategica; sostituisce la *direzione futura* M74–M75 (integrazione OpenCad2D) della pianificazione storica, ma conserva come archivio i milestone M51–M73 e i relativi risultati.


**Aggiornamento M75 (23/10/2026):** prima implementazione dopo M74. Nuovo `StructuralModelJsonWriter` per il contratto standard JSON, `StructuralModelJsonContractValidator` e `ReadValidated` opt-in; test round-trip C#/xUnit e script `run-m75-tests.ps1`. Nessun nuovo algoritmo FEM, nessuna verifica normativa e nessuna GUI. **Test Windows ancora pendenti**. [Dettagli tecnici](m75-json-model-contract.md).

## 1. Missione

Costruire un programma **open source, trasparente e semplice** per l'analisi agli elementi finiti di **strutture piane di aste** in **acciaio** e **legno**. Destinatari: studenti, docenti e professionisti che vogliono studiare o dimensionare preliminarmente travi continue, capriate, piccoli telai e strutture simili, comprendendo ipotesi, risultati e verifiche.

Il programma si compone di moduli indipendenti: (1) modello e motore FEM; (2) benchmark, test e validazione; (3) verifiche normative; (4) post-processing e report; (5) editor e visualizzatore desktop. L'editor sarà sviluppato **per ultimo** in C# e Avalonia, senza dipendenza obbligatoria da OpenCad2D. API e CLI resteranno utilizzabili senza interfaccia grafica.

**Fuori ambito:** strutture FEM 3D, cemento armato, muratura, pannelli e piastre, pareti, X-LAM, BIM, geotecnica complessa, modellazione solida, calcolo completo di collegamenti, progettazione esecutiva integrata. Un'asta è geometria 1D in un modello con due coordinate, non un elemento bidimensionale di piastra.

**Distinzione obbligatoria:** risultato FEM ≠ verifica normativa ≠ attestazione di sicurezza dell'opera. Le verifiche fuori piano richiedono informazioni sui ritegni e sulle lunghezze libere non deducibili dal solo modello 2D. Quando non sono disponibili, la verifica deve essere segnalata come *non eseguita / dati insufficienti*, mai come positiva per default.

## 2. Stato certo al checkpoint M73

| Ambito | Stato | Gap rispetto alla missione |
|---|---|---|
| Elementi lineari Frame2D e Truss2D, assemblaggio, vincoli, carichi, combinazioni | Implementati con test | Copertura professionale dei carichi, molle, eccentricità e risultati da consolidare |
| Diagrammi elastici, reazioni, report CSV/Markdown/PDF/HTML, viewer | Disponibili | Inviluppi e risultati per ogni sezione/caso con tracciabilità omogenea |
| Analisi modale, armonica, Newmark, spettrale SRSS/CQC | Implementate, testate nel perimetro | Analisi sismica normativa NON disponibile |
| Truss2D elasto-plastico incrementale/transitorio, API/CLI/viewer | Implementati, testati nel perimetro | Modelli limite e validazione professionale restano separati |
| Elemento Frame2D con cerniere plastiche concentrate | Prototipo interno isolato, testato | Manca assemblaggio globale incrementale di telai plastici |
| Benchmark numerici / profiler M73 | Implementati, test Windows superati | Benchmark esterni e collaudo visuale da accrescere |
| Cataloghi profili acciaio/legno | Solo preset materiali e sezioni semplici | Archivio normativamente qualificato assente |
| Peso proprio automatico | Non disponibile nel contratto attuale | Implementare e validare |
| Verifiche SLU, SLE, NTC / EC3 / EC5 | Non implementate | Area di sviluppo fondamentale |
| Editor grafico desktop | Non implementato | Sviluppare solo dopo core e normative |

Non usare percentuali come KPI di qualità: il completamento va misurato per **funzionalità verificata e perimetro dichiarato**. I test confermati dall'utente attestano la suite nei casi coperti, non la sicurezza di un'opera reale.

## 3. Principi architetturali

1. **Core:** geometria, proprietà delle aste, carichi, combinazioni e tipi di risultato indipendenti da UI e normative; unità coerenti kN–m–s e conversione esplicita all'interfaccia.
2. **Analysis:** motore FEM lineare/dinamico/non lineare; dipendenza solo da Core; nessun riferimento Avalonia.
3. **Verification:** nuovo modulo con regole acciaio/legno, requisiti d'ingresso, edizione normativa e versione degli annessi nazionali; consuma risultati Analysis, non ne sostituisce la matematica.
4. **Catalogs:** database di sezioni e materiali con fonte, edizione, unità, identificatore stabile, proprietà per analisi e proprietà per verifica separate. Mai attribuire proprietà 3D inventate a un profilo 2D.
5. **Reporting:** formule, coefficienti e clausole/edizioni di riferimento, combinazione governante, aste e sezioni, indicatori di sfruttamento, *warnings* visibili e risultati non verificabili.
6. **Desktop (fase finale):** Avalonia/C#, MVVM, canvas 2D, modello editabile, undo/redo e risultato selezionabile; editor UI solo consumer di servizi versionati.
7. **Validation:** test xUnit e benchmark C# con soluzioni indipendenti, bilanci, casi degeneri; QA visuale su browser/Desktop; regressione completa in ogni milestone. PowerShell pulisce `bin/obj` prima dei test, log lineari senza progress interattivo.

## 4. Strategia di prodotto

**Prima versione utile senza GUI:** un modello JSON (o costruito con API C#) per travi/telai/reticolari in regime elastico, con auto-peso, carichi, combinazioni, verifiche NTC/EC3/EC5 per casistiche dichiarate, rapporti sfruttamento e report verificabili. Gli elementi non verificabili devono essere marcati esplicitamente.

**Beta desktop:** stesso motore con un editor leggero e completo per modellare travi e telai, scegliere profili, visualizzare diagrammi e lanciare verifiche. Non fare un CAD general-purpose, non importare BIM, non programmare DXF/DWG come requisito iniziale.

**Funzioni avanzate isolate:** dinamica avanzata, plasticità reticolare, plasticità Frame2D, pushover, verifiche sismiche, connessioni e metodi a tensioni ammissibili legacy non devono ritardare il primo prodotto lineare SLU/SLE. Riaprirli solo dietro motivazione, benchmark e gate specifico.

## 5. Roadmap dettagliata — fase A: completamento motore FEM (prima delle normative)

Le durate sottostanti sono **settimane-equivalenti di sviluppo umano a tempo pieno**, inclusive di test e documentazione di quel milestone. Sono stime per pianificazione, non impegni di consegna; le attività possono sovrapporsi e le somme non corrispondono automaticamente al tempo di calendario.

| ID | Priorità | Lavoro principale | Gate misurabile per chiusura | Stima | Difficoltà |
|---|---|---|---|---:|---|
| **M74** | P0 | Riallineamento requisiti, inventario funzionale, matrice di copertura, nuova roadmap, decisione OpenCad2D non vincolante | Stato reale per ogni feature; requisiti e limiti approvati; nessun cambio algoritmico | 1–2 sett. | Bassa |
| **M75** | P0 | Consolidare contratti model/API/JSON, input validation, messaggi, unità, convenzioni dei segni, risultati per load case | Round-trip, retrocompatibilità, test di conversione e segnali d'errore | 2–4 sett. | Media |
| **M76** | P0 | Carichi di progetto mancanti: **peso proprio automatico**, carichi concentrati/distribuiti/variabili per asta, categorie, aggregazione, carichi termici solo se compatibili | Equilibrio globale e diagrammi con casi chiusi, carichi da densità×area; niente conteggio doppio | 3–6 sett. | Medio-alta |
| **M77** | P0 | Vincoli e collegamenti: molle traslazionali/rotazionali, cedimenti imposti, release parziali, eccentricità rigide limitate | Controlli di stabilità e rigidezza; confronto con riferimenti indipendenti | 3–6 sett. | Alta |
| **M78** | P0 | Post-processing unificato: N(x), V(x), M(x), massimi interni, posizione estremi, reazioni, inviluppi per casi e combinazioni, CSV/report | Inviluppi esatti o convergenti, sezioni note, cambi di segno, discontinuità rispettate | 3–5 sett. | Medio-alta |
| **M79** | P0 | Carichi e combinazioni: categorie esplicite, coefficienti anche negativi, combinazioni annidate con controlli, risultati governanti | Superposizione testata, cicli/riferimenti invalidi rifiutati, tracciabilità | 2–4 sett. | Media |
| **M80** | P0 | Libreria geometrica preparatoria: proprietà sezionali corrette (A, I, W, aree di taglio dove servono), acciaio/timber rettangolare; unità e provenienza | Proprietà geometriche testate; nessun valore normativo attribuito senza fonte | 2–4 sett. | Media |
| **M81** | P1 | Secondo ordine piano P–Δ, rigidezza geometrica, precompressione, convergenza e possibili imperfezioni | Confronti noti, biforcazioni e warning; analisi lineare resta invariata | 5–9 sett. | Molto alta |
| **M82** | P1 | Stabilità globale/eigenbuckling 2D con limitazioni e modi instabili | Autovalori e carichi critici di casi analitici, diagnosi meccanismi | 4–7 sett. | Alta |
| **M83** | P1 condizionata | Benchmark M73 guidano l'eventuale introduzione di solver sparso/banded e fattorizzazioni robuste; nessuna sostituzione senza beneficio osservato | Stesse soluzioni, scalabilità riproducibile, fallback affidabile | 4–8 sett. | Alta |
| **M84** | P2 opzionale | Assemblaggio globale Frame2D elasto-plastico da M72, Newton incrementale, rollback, redistribuzione, eventuale controllo spostamenti | Telaio benchmark indipendente, bilancio energia e stabilità incrementale | 6–12 sett. | Molto alta |
| **M85** | P0 | *Core validation gate*: manuale, suite esterna, fattori di scala, precisione, casi negativi, regressione e collaudo visuale | Matrice copertura firmata e limitazioni pubblicate; 0 test falliti | 3–5 sett. | Alta |
| **M86** | P0 | Release **Core FEM 2D v0.2** utilizzabile da API/CLI; JSON stabile e integrazioni future | Stessa risposta fra API/CLI/report, esempio end-to-end e tracciabilità | 2–3 sett. | Media |

**Percorso critico A:** M74 → M75 → M76/M77 → M78/M79 → M85 → M86. M80 può essere anticipato o sviluppato in parallelo. M81/M82 sono importanti per la stabilità dei telai, da far precedere alle corrispondenti verifiche normative; M83 è subordinato alla misurazione. M84 non deve bloccare una prima release FEM elastica.

## 6. Roadmap dettagliata — fase B: verifiche normative acciaio e legno

**Riferimenti da qualificare per edizione e applicabilità:** NTC 2018 (DM 17.01.2018, modifiche 2023), Circolare 7/2019; EN 1990/1991 (azioni e combinazioni), EN 1993 (acciaio), EN 1995 (legno), relativi Annessi Nazionali. Non assumere che «Eurocodice» identifichi una sola edizione immutabile: la seconda generazione è in transizione verso 2027–2028. Prima di ogni implementazione normativa, congelare *norma+edizione+paese+annesso+clausola* e farne esaminare l'interpretazione da un professionista qualificato.

| ID | Priorità | Lavoro principale | Gate misurabile | Stima | Difficoltà |
|---|---|---|---|---:|---|
| **M87** | P0 | Architettura `Verification`: `NormativeProfile`, verifica non eseguibile, coefficienti, versioning, rapporto domanda/capacità, tracciabilità | Nessun coefficienti magic; regole testabili senza FEM | 3–5 sett. | Alta |
| **M88** | P0 | Generatore combinazioni SLU/SLE NTC/EN 1990, categorie G/Q/neve/vento, coefficienti e fattori ψ, alternanza carichi | Matrice combinazioni con tabelle di riferimento e casi esclusi | 4–7 sett. | Alta |
| **M89** | P0 | Catalogo acciaio: IPE, HEA/HEB/HEM, UPN, angolari, RHS/SHS/CHS iniziali, sezioni parametriche; E, f_y, f_u e proprietà complete | Dataset versionato, fonti e verifica dimensionale campionata | 4–7 sett. | Medio-alta |
| **M90** | P0 | Acciaio — verifiche di sezione SLU: classificazione, N, V, M, combinazioni entro i limiti | Riferimenti analitici e indipendenti a mano per ogni formula | 5–8 sett. | Alta |
| **M91** | P0 | Acciaio — instabilità aste: compressione, lunghezze libere, curve di instabilità, instabilità flesso-torsionale, interazioni N–M e ritegni laterali | Verifiche distinguono in piano/fuori piano e segnalano dati mancanti | 8–14 sett. | Molto alta |
| **M92** | P0 | Catalogo legno: massiccio C-class, lamellare GL-class, E, G, densità, resistenze, classi di servizio e durata carico | Origine ed edizione di ogni dato; distinzione proprietà medie/caratteristiche | 3–6 sett. | Medio-alta |
| **M93** | P0 | Legno — SLU: trazione, compressione parallela/perpendicolare, flessione, taglio, combinazioni; fattori k_mod e γ_M | Confronti manuali e limite di applicabilità documentato | 5–9 sett. | Alta |
| **M94** | P0 | Legno — stabilità: pressoflessione, instabilità flessionale, instabilità laterale, variazione coefficienti e ritegni | Casi di instabilità fuori piano con parametri espliciti | 5–9 sett. | Molto alta |
| **M95** | P0 | SLE acciaio/legno: frecce istantanee e finali, fluage (k_def nel legno), limiti selezionabili, vibrazioni semplici solo se pertinenti | Confronti su trave, combinazioni SLE, limiti tracciati per progetto | 4–7 sett. | Alta |
| **M96** | P1 | Assistenti carichi da neve/vento per **modelli piani** con dati d'ingresso completi, senza pretendere ricostruzione 3D del fabbricato | Tutti i valori e coefficienti sono mostrati/modificabili | 5–9 sett. | Alta |
| **M97** | P0 | Relazioni di verifica e diagnostica: tutti i risultati per asta, combinazione dominante, clausole, coefficienti, esclusioni, warnings, audit trail | Nessun esito «OK» quando un controllo necessario è mancante | 4–7 sett. | Alta |
| **M98** | P0 | Validazione normativa indipendente: esempi pubblicati/autorizzati, calcoli manuali e revisione di strutturista per acciaio/legno | Catalogo benchmark versionato e matrice di conformità al perimetro dichiarato | 6–12 sett. | Molto alta |
| **M99** | P0 | Prima release **StructuralSolver2D Verify v0.3** via API/CLI, con copertura dichiarata e ambiti esclusi | Rapporti riproducibili, warning obbligatori, validazione esterna documentata | 3–5 sett. | Alta |

**Tensioni ammissibili:** eventuale modalità **storica/educativa indipendente** dopo M99, con parametri e normativa di riferimento propri; non sostituisce automaticamente le verifiche agli stati limite. **Sismica normativa e progettazione dissipativa:** ramo avanzato successivo, non requisito del primo prodotto; M60/M70 sono calcoli esplorativi, non verifiche NTC/EC8.

**Percorso critico B:** M87 + M88 + (M89→M90→M91) + (M92→M93→M94) + M95 + M97 → M98 → M99. Il legno richiede carichi di durata, classe di servizio e deformazioni differite; l'acciaio richiede caratteristiche sezionali e ritegni. Sono requisiti di dati, non solo formule.

## 7. Roadmap dettagliata — fase C: desktop Avalonia, SOLO dopo il gate B

| ID | Priorità | Lavoro principale | Gate misurabile | Stima | Difficoltà |
|---|---|---|---|---:|---|
| **M100** | P0 | Shell desktop Avalonia, MVVM, apertura/salvataggio JSON, servizi API isolati | Aprire/riprodurre un modello Core senza alterarlo | 3–5 sett. | Media |
| **M101** | P0 | Canvas 2D, viewport, zoom/pan, griglia, coordinate, snapping, nodi e aste | Input geometrico preciso e modello canonico corretto | 5–9 sett. | Alta |
| **M102** | P0 | Selezione, multi-selezione, proprietà, sposta/dividi/unisci/copìa, undo/redo, errori modellazione | Interazioni riproducibili e cronologia reversibile | 5–9 sett. | Alta |
| **M103** | P0 | Editor vincoli, molle, carichi, casi/combinazioni, profili e materiali da catalogo | Rapido input senza proprietà obbligatorie nascoste | 5–10 sett. | Alta |
| **M104** | P0 | Risultati: deformate, reazioni, N/V/M, inviluppi, scelta caso/combinazione, etichette e dettagli selezionati | Parità dati con API/CSV e QA visuale sul browser/app | 5–9 sett. | Alta |
| **M105** | P0 | Vista verifiche: sfruttamenti SLU/SLE per asta, non verificabile, formulazioni e report | Passaggio disegno→asta→verifica→dati→formula | 4–8 sett. | Alta |
| **M106** | P1 | UX: preset, wizard trave/capriata/telaio, guida iniziale, linguaggio chiaro, accessibilità, IT/EN | Test di usabilità con studenti, docenti e strutturisti | 4–7 sett. | Media |
| **M107** | P0 | Packaging desktop, import/export, sample project, e2e GUI, release candidate e beta pubblica | Installer, regressione, documento limiti, esempi e manuale | 5–9 sett. | Alta |

Per la prima versione desktop **no** CAD general purpose, no DXF/DWG obbligatorio, no disegno esecutivo, no BIM. Integrare OpenCad2D solo in futuro come *adapter opzionale*, se emergerà un caso concreto; non è più un prerequisito di M74.

## 8. Tempi realistici e assunzioni

I valori sono stime **di sviluppo umano** per una persona esperta C# con revisione tecnico-strutturale periodica; non sono tempi promessi dal modello o previsioni per ogni singolo messaggio. Comprendono iterazione e test ordinari ma non sostituiscono un audit professionale.

| Traguardo | Orizzonte indicativo a 35–40 h/settimana | Note |
|---|---|---|
| Core FEM con carichi/risultati/stabilità consolidati | 4–9 mesi | M81/M82 e motore sparso aumentano l'incertezza |
| Verifiche acciaio + legno di perimetro dichiarato | ulteriori 8–15 mesi | richiede collaborazione di strutturista e licenze/fonti normative |
| Desktop editor, visualizzatore e verifica | ulteriori 8–15 mesi | grande parte dell'impegno è UX e QA, non solo canvas |
| Validazione/release/bugfix | 3–6 mesi distribuiti, in parte sovrapposti | test esterni, documentazione, correzioni |
| **Beta completa ragionevole** | **24–42 mesi a tempo pieno** | range prudenziale, non impegno |

Con circa **15–20 ore/settimana**, stimare indicativamente **4–8 anni** per un prodotto analogo, a seconda di supporto specialistico, ampiezza delle verifiche e debito tecnico. Con meno ore il calendario aumenta, e le norme possono cambiare nel frattempo. Un **MVP didattico FEM 2D** senza normative complete e senza GUI è ottenibile molto prima, ma non va chiamato sostituto professionale.

I colli di bottiglia principali saranno: instabilità e secondo ordine, verifiche di stabilità dell'acciaio fuori piano, creep/durata dei carichi nel legno, gestione delle edizioni normative, tracciabilità e copertura benchmark, modello editabile del canvas Avalonia. Non spendere mesi su plasticità globale, sismica normativa, solver sparso o grafica CAD finché non è dimostrato che siano necessari per il primo prodotto.

## 9. Gate di qualità obbligatori

Ogni milestone deve consegnare: cambiamento isolato, API e input compatibili, benchmark analitico indipendente in C#, casi degeneri, bilanci e tolleranze giustificate, test xUnit + regressione, documenti di limiti, ZIP completo; script PS che inizia con `clean-project-build.ps1` e log leggibile. Gli output normativi aggiungono: fonte, versione, annesso nazionale, descrizione del dominio valido, combinazione dominante e warning dei controlli non eseguiti.

**Stati consentiti:** `pianificato` → `implementato (non validato)` → `test automatici superati` → `validato per il perimetro dichiarato` → `release`. Non identificare mai `test superati` con `software certificato`.

## 10. Priorità immediata dopo M73

**M74** deve introdurre il nuovo perimetro formale senza cambiare il codice FEM, ripulire i documenti di roadmap contraddittori, registrare i gap effettivi con test e aggiornare README/handoff. Poi **M75–M79**, concentrandosi su contratto dei dati, auto-peso, carichi, supporti e diagrammi: sono le funzioni che più velocemente avvicinano l'attuale motore a un'utilità concreta per travi e capriate.

Non procedere direttamente con l'editor Avalonia, né con verifiche normative senza prima rendere tracciabili le grandezze prodotte dal solver.

## 11. Riferimenti esaminati (fonti, non specifiche da copiare)

- [Sismicad 13.2 — scheda tecnica Concrete](https://www.concrete.it/download/docs/product/scheda_tecnica-sismicad.pdf): organizzazione disegno/modello/verifiche, cataloghi, carichi e analisi, perimetro molto più ampio del nostro.
- [BeamCAD+ — scheda tecnica Concrete](https://www.concrete.it/download/docs/product/Scheda_tecnica-BeamcadPlus.pdf): travi continue, appoggi, carichi, database acciaio/legno, deformabilità e instabilità.
- [NTC 2018 — Gazzetta Ufficiale](https://www.gazzettaufficiale.it/eli/id/2018/02/20/18A00716/sg), [DM 52/2023 — Ministero Infrastrutture](https://mit.gov.it/node/18453), [Circolare 7/2019 — Gazzetta Ufficiale](https://www.gazzettaufficiale.it/eli/id/2019/02/11/19A00855/sg).
- [JRC / Commissione europea — seconda generazione Eurocodici](https://eurocodes.jrc.ec.europa.eu/second-generation-eurocodes): pubblicazione nazionale prevista entro 30 settembre 2027 e ritiro delle norme EN nazionali in conflitto entro 30 marzo 2028; uso operativo e annessi da verificare per paese.
- [Avalonia — documentazione ufficiale](https://docs.avaloniaui.net/docs/get-started): framework UI desktop multipiattaforma C#/.NET, scelta rimandata all'ultima fase.
