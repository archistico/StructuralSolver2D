# Ricerca bug in Analysis — 2026-10-18

## Esito delle correzioni

**Corretti tutti e quattro i difetti principali.** Le sezioni dei rilievi sotto riportano il comportamento osservato prima delle correzioni.

- Il solver equilibra righe e colonne, applica una soglia relativa sui pivot e controlla residui e finitezza della soluzione. I meccanismi ruotati vengono rifiutati anche con carichi nulli. Le traslazioni libere non vengono più bloccate artificialmente come DOF inattivi.
- Il sampler recupera le rotazioni rilasciate mediante `Kcc uc = fc - Kcr ur`, per ciascuna asta e con i carichi equivalenti originali del caso/combinazione. Il nodo condiviso mantiene la sua rotazione globale.
- Le reazioni del nodo sono decomposte sulle direzioni vincolate dei singoli supporti: i due carrelli del rilievo 3 restituiscono `(-5, 5)` e `(5, 5) kN`.
- Le equazioni di supporto ridondanti sono ridotte a una base indipendente. Le reazioni individuali in presenza di ridondanza usano la soluzione a norma minima dei moltiplicatori locali: la somma è la reazione nodale; la ripartizione è convenzionale e non rappresenta una rigidezza fisica dei singoli appoggi.

**Test aggiunti: 105 casi**, in tre file persistenti:

- `tests/StructuralSolver2D.Analysis.Tests/Constraints/AnalysisConstraintRegressionTests.cs`: labilità a più angoli con/senza carico nei tre analyzer; carrelli ortogonali e non ortogonali; ridondanze e ordine dei supporti; risposte analitiche con rigidezze scalate.
- `tests/StructuralSolver2D.Analysis.Tests/Frame2D/ReleasedDisplacementRegressionTests.cs`: rilasci singoli/doppi, carichi uniformi/lineari/concentrati, combinazioni con fattori positivi e negativi, aste ruotate, nodo condiviso e verifica di freccia tramite facade.
- `tests/StructuralSolver2D.Analysis.Tests/Solvers/DenseLinearSystemSolverRegressionTests.cs`: sistemi analitici con scale da `1e-100` a `1e100`, scale diverse delle incognite, rango insufficiente con carichi nulli e input non finiti.

La prima esecuzione degli 85 test iniziali, prima delle correzioni, dava **70 fallimenti e 15 successi**. Dopo le correzioni sono stati aggiunti altri 20 casi per gli aspetti numerici e geometrici emersi nella revisione finale.

Verifica finale: `dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal`, senza fallimenti né avvisi di compilazione:

| Progetto | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 192 | 0 |
| Analysis.Tests | 245 | 0 |
| Reporting.Tests | 39 | 0 |
| Cli.Tests | 7 | 0 |
| Totale | 483 | 0 |

Aggiornati changelog, documentazione di analisi/interpolazione e contratto delle reazioni. Le API pubbliche conservano le firme esistenti.

## Ricerca iniziale

Revisione della versione `63e5811` (comprende le correzioni precedenti di Core). Esaminati solver, assemblaggio Frame2D/Truss2D/misto, vincoli orientati, recupero delle forze, interpolazione degli spostamenti, controlli di freccia, equilibrio e facade pubblica.

**Esito: quattro difetti riprodotti, tre P1 e uno P2.** Nessuna modifica ai sorgenti o ai test applicativi durante questa ricerca.

P1: risultato numerico errato restituito senza errore bloccante. P2: regressione che impedisce l'analisi di una struttura stabile.

## 1. P1 — Strutture labili accettate come risolte

**Riferimenti:** `src/StructuralSolver2D.Analysis/Solvers/DenseLinearSystemSolver.cs:11`, `:52`, `:98`; `src/StructuralSolver2D.Analysis/Constraints/SupportConstraintSystem.cs:74`.

Il solver accetta un pivot sulla base di una soglia assoluta `1e-12`, senza rapportarla alla scala della matrice. Su matrici singolari, gli errori di arrotondamento dell'eliminazione possono lasciare pivot superiori a tale soglia. La soluzione viene restituita senza una verifica conclusiva del residuo o del rango. Gli analyzer e la facade non bloccano i risultati fuori equilibrio.

**Riproduzione:** un'asta Frame2D di 5 m, `E = 210000000`, `A = 0.003`, `I = 0.00002`, con cerniera nel solo nodo iniziale e forza globale verticale `-10 kN` al nodo finale. L'asta può ruotare rigidamente attorno alla cerniera: il momento esterno non è equilibrabile.

| Inclinazione asta | Comportamento osservato |
|---|---|
| 0° | Eccezione per matrice singolare |
| 15° | Analisi completata; massimo spostamento traslazionale assoluto `5.983361563797021e12 m` |
| 30° | Analisi completata; massimo spostamento traslazionale assoluto `6.801464295788544e12 m` |
| 45° | Analisi completata; massimo spostamento traslazionale assoluto `1.1984156115512424e12 m` |

Nel caso a 15°, il controllo indipendente `GlobalEquilibriumChecker` restituisce residui `Fx = -5.1875 kN`, `Fy = -9.75 kN`, `Mz = -48.29629131445341 kNm`. La rotazione geometrica cambia quindi il rilevamento della stessa labilità, anziché produrre sempre un errore controllato.

**Intervento suggerito:** gestione numerica della scala e del rango/pivot relativi, più una verifica dei residui sulle equazioni risolte e sui vincoli prima di accettare il risultato. Un controllo dei residui da solo non sostituisce il rilevamento dei meccanismi non caricati.

**Test da aggiungere:** stesso schema labile a più angoli, con rifiuto coerente; modelli stabili di controllo a più scale di rigidezza; verifica di residui e vincoli prima del successo dell'analisi.

## 2. P1 — Rilasci di momento: deformata nulla e verifica di freccia falsamente positiva

**Riferimenti:** `src/StructuralSolver2D.Analysis/Frame2D/Frame2DDisplacementSampler.cs:72`, `:75`, `:79`, `:88`; effetto a valle in `Serviceability/PreliminaryDeflectionChecker.cs:78`.

Il sampler usa sempre le rotazioni globali dei nodi come rotazioni di estremità dell'elemento. Dopo la condensazione dei rilasci queste non coincidono con le rotazioni fisiche dell'asta: i gradi di libertà rilasciati devono essere recuperati dalla relazione di condensazione. Quando sono inattivi nel sistema globale vengono fissati a zero; il sampler non li ricostruisce.

**Riproduzione:** trave di 5 m con le stesse proprietà del caso precedente, cerniera a sinistra, carrello verticale a destra e carico uniforme `-10 kN/m`. Chiamare `StructuralSolver2DService.AnalyzeLoadCase` con `DeflectionLimit(1000)`.

| Modello equivalente | Massimo `abs(Uy)` campionato | Limite | Esito |
|---|---:|---:|---|
| Estremità senza rilasci espliciti; rotazioni nodali libere | `0.015500992063492057 m` | `0.005 m` | Fail |
| Stessi vincoli, con entrambi i rilasci di momento sull'asta | `0 m` | `0.005 m` | **Pass errato** |

In entrambi i casi le reazioni sono corrette, `25 kN` per appoggio, e l'equilibrio globale risulta soddisfatto. L'errore riguarda la ricostruzione degli spostamenti: non viene intercettato dal controllo di equilibrio.

Riprodotto anche con forza concentrata di `-10 kN` al centro dell'asta: il massimo campionato passa da `0.004650297619047619 m` a zero aggiungendo i due rilasci, senza cambiare lo schema fisico.

**Distinzione rispetto a una limitazione nota:** il progetto documenta l'approssimazione dell'interpolazione cubica con carichi interni all'elemento. Per il carico uniforme, `qL^4/(96EI)` è il valore interpolato già atteso dai test, diverso dalla formula esatta `5qL^4/(384EI)`. Il difetto qui è la perdita delle rotazioni rilasciate e l'azzeramento della deformata, non tale differenza documentata.

**Intervento suggerito:** recuperare le rotazioni rilasciate per elemento usando matrice originale, spostamenti trattenuti e carichi equivalenti originali fattorizzati. Non assegnare semplicemente una rotazione al nodo condiviso: aste incidenti con rilasci possono ruotare indipendentemente.

**Test da aggiungere:** equivalenza fra i due modelli, rilasci singoli e doppi, asta rilasciata su nodo condiviso con un'altra asta, casi/combinazioni e propagazione corretta alla verifica di freccia.

## 3. P1 — Reazioni duplicate per vincoli indipendenti sullo stesso nodo

**Riferimenti:** `src/StructuralSolver2D.Analysis/Constraints/SupportConstraintSystem.cs:100`, `:108`, `:109`.

Il metodo di estrazione delle reazioni copia il residuo globale del nodo in ciascun supporto che ha una componente vincolata lungo l'asse globale corrispondente. Non separa le azioni appartenenti alle singole equazioni di vincolo. Con due carrelli inclinati indipendenti, entrambi ricevono l'intera reazione traslazionale del nodo.

**Riproduzione:** asta orizzontale di 5 m, proprietà come sopra, carico nodale verticale `-10 kN` al nodo finale. Al nodo iniziale applicare:

- un carrello verticale locale con orientazione `45°`;
- un carrello verticale locale con orientazione `-45°`;
- un vincolo separato sulla sola rotazione `Rz`.

I tre vincoli sono indipendenti ed equivalgono cinematicamente a un incastro. L'analisi degli spostamenti riesce e la freccia finale è coerente con la mensola, circa `-0.09920634920634926 m`.

| Supporto | Reazione attesa `(Fx, Fy)` kN | Reazione restituita `(Fx, Fy)` kN |
|---|---|---|
| Carrello a 45° | `(-5, 5)` | `(0, 10)` |
| Carrello a -45° | `(5, 5)` | `(0, 10)` |

Il vincolo rotazionale restituisce correttamente circa `50 kNm`, ma la somma delle reazioni verticali è `20 kN` invece di `10 kN`. `GlobalEquilibriumChecker` conferma un residuo verticale di circa `10 kN`.

**Intervento suggerito:** ricostruire le reazioni di ogni supporto dai moltiplicatori delle sue equazioni di vincolo, conservandone l'associazione, invece di copiare il residuo nodale totale. Per vincoli dipendenti occorre anche gestire esplicitamente l'indeterminazione della ripartizione.

**Test da aggiungere:** due carrelli inclinati indipendenti su uno stesso nodo, con carichi X/Y e controllo delle singole direzioni di reazione, della somma e dell'equilibrio globale. Verificare gli analyzer che condividono questa implementazione.

## 4. P2 — Vincoli ridondanti fanno fallire una struttura stabile

**Riferimenti:** `src/StructuralSolver2D.Analysis/Constraints/SupportConstraintSystem.cs:131`, `:149`, `:158`, `:167`, sistema aumentato risolto a riga 74.

Le equazioni di vincolo vengono aggiunte senza eliminare quelle linearmente dipendenti. Un vincolo già imposto da un altro supporto rende singolare il sistema aumentato, anche se la struttura è stabile e il campo degli spostamenti è determinato.

**Riproduzione:** mensola orizzontale di 5 m, incastro `Sfixed` sul nodo iniziale, forza `-10 kN` al nodo finale. Aggiungere un carrello verticale `Sroller` sullo stesso nodo dell'incastro, con ID diverso.

- Il modello passa la validazione.
- La mensola senza carrello aggiuntivo viene risolta correttamente.
- Con il carrello aggiuntivo viene lanciata `StructuralAnalysisException`: matrice singolare al pivot 9, modello probabilmente instabile.
- Il carrello aggiuntivo non cambia i gradi di libertà vincolati. È la duplicazione dell'equazione su `Uy` a rendere singolare il sistema con i moltiplicatori; non è comparso un meccanismo strutturale.

**Regressione identificata nel codice:** il commit `461e8e1` ha introdotto il sistema aumentato per i vincoli orientati. Il codice precedente combinava i vincoli sullo stesso grado di libertà con `|=` nella maschera delle incognite vincolate; la stessa duplicazione non creava un'equazione aggiuntiva. Il confronto storico è stato eseguito sul sorgente, senza ricompilare la revisione precedente.

**Intervento suggerito:** ridurre i vincoli a una base indipendente, mantenendo il legame con i supporti per definire come aggregare o segnalare reazioni non univocamente ripartibili. Non segnalare una struttura labile quando il problema è la ridondanza delle equazioni di vincolo.

**Test da aggiungere:** incastro più carrello coincidente, vincoli ripetuti con angoli equivalenti e vincoli ortogonali indipendenti. Gli spostamenti e la reazione totale devono coincidere con il modello equivalente senza duplicati.

## Verifiche eseguite

`dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal`:

| Progetto | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 192 | 0 |
| Analysis.Tests | 140 | 0 |
| Reporting.Tests | 39 | 0 |
| Cli.Tests | 7 | 0 |
| Totale | 378 | 0 |

Per la ricerca iniziale è stato usato il progetto diagnostico locale, ignorato da Git, `out/AnalysisBugAudit/AnalysisBugAudit.csproj`. Le riproduzioni sono state successivamente trasformate nei test persistenti elencati all'inizio del rapporto.

Come controlli positivi, mensole stabili e travi interamente vincolate con lunghezze 5, 0.1, 0.01 e 0.001 m hanno dato i risultati attesi. La mancata ricerca di zeri ed estremi esatti tra i campioni non è classificata come bug: il relativo finder documenta espressamente l'interpolazione fra campioni.

Osservazione aggiuntiva, non inclusa nei quattro difetti principali: `Truss2DAnalyzer.ValidateTrussOnlyModel` controlla i tipi di carico di tutti i casi, anche quelli non selezionati. Una reticolare con caso `LC1` scarico e momento nodale nel solo caso `OTHER` viene rifiutata analizzando `LC1`, mentre `PlaneStructureAnalyzer` risolve `LC1`. È da chiarire se il contratto dell'analyzer specializzato debba rifiutare l'intero modello o soltanto i carichi del caso/combinazione richiesto.

La revisione non dimostra l'assenza di ulteriori difetti. I casi principali usano dati finiti e proprietà meccaniche ordinarie; non dipendono da overflow o input volutamente non validi.
