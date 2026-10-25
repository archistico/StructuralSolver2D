# StructuralSolver2D — Roadmap operativa

**25 ottobre 2026 — ultimo gate .NET confermato dall'autrice P0-F1E; F1.2-A, 12 benchmark analitici C# pronti, build e test PENDING.** Corretto in F1.2-A il warning `xUnit2031` emerso nei test F1E. Manca ancora il JSON `proposed` F1D per il congelamento del riferimento statico. I dettagli di ambito e stato sono in [CURRENT_STATE](CURRENT_STATE.md), le decisioni vincolanti in [ENGINEERING_AND_VALIDATION_RULES](ENGINEERING_AND_VALIDATION_RULES.md) e il ragionamento completo nel [piano dettagliato F0–F10](PIANO_DETTAGLIATO_POST_D2C2.md).

## Vincoli di esecuzione

1. **Nuovo codice, test, runner, golden generator e script del progetto esclusivamente C#/.NET.** Nessun nuovo Python/PowerShell/Bash: i file esistenti in altri linguaggi sono storici o compatibilità della baseline, non il modello per i prossimi incrementi.
2. **NTC 2018, EC3 ed EC5 distinti**, con identità di codice, edizione, eventuale annesso nazionale, parametri e stato. Vietata l'equivalenza implicita. Per la seconda generazione degli Eurocodici creare **nuove versioni/profili**, dopo verifica delle edizioni e degli annessi applicabili.
3. Sequenza vincolante: **affidabilità numerica → validità fisica e completezza delle azioni → massimi e copertura → formule normative → API e rilascio → GUI C#/Avalonia**.
4. Un `Passed` **locale** non rappresenta l'esito di un'intera asta o struttura. Dati esterni, fenomeni e obblighi mancanti devono rimanere visibili.
5. Per i riferimenti indipendenti usare **dati congelati e calcoli esterni all'algoritmo di produzione**, con confronto e test negativi in C#; gli esiti storici Python possono essere consultati **senza eseguirli**.

## Milestone e ordine di implementazione

| Fase | Obiettivo | Consegna principale | Condizione di chiusura (*gate*) | Stato |
|---|---|---|---|---|
| **F0** | Allineare documentazione e decisioni | Stato corrente, roadmap, policy C#, handoff, storico archiviato | Nessuna modifica di codice; riferimenti univoci; manifest ZIP integro | **Completata (solo documentazione)** |
| **F1** | Validazione numerica FEM integrata | **F1.1 G8B: 4 varianti × 4 combinazioni = 16 scenari**, poi casi di orientamento/release/scale | Confronto C# di spostamenti, reazioni e N/V/M con attesi indipendenti congelati; controprove falliscono quando si altera l'input | **In corso — F1.1-A/B/C/D-FIX1/E PASS riferiti; F1.2-A .NET PENDING; golden F1D da ricevere** |
| **F2** | Applicabilità e dati fisici esterni | Contratti tipizzati per ancoraggi, ritegni, controventi, collegamenti e limiti 2D | Mancanza di evidenze/validità produce `MissingData` o `Incomplete`, non `Passed` | Da fare |
| **F3** | Completezza delle combinazioni nel dominio dichiarato | Generazione/audit SLU STR e SLE **non sismici**, scenari e azioni alternative | Nessuna combinazione dovuta persa; segni/favorevolezza/fattori zero e fingerprint verificati con controprove | Da fare |
| **F4** | Massimi di utilizzo lungo le aste | Funzione `η(x)` e stazioni governanti per regola/segmento provabile | Massimo verificato dove matematicamente possibile; altrove `ScreeningIncomplete` | Da fare |
| **F5** | Piano obblighi e report di copertura | Esiti distinti per regola, membro, scenari, dati esterni, norma | Obbligo omesso → `Incomplete`; `Failed` non mascherato; report cita ciò che resta fuori | Da fare |
| **F6** | Acciaio NTC ed EC3, separati | Interazioni N–M/N–M–V, stabilità d'asta/LTB nei sottodomini dimostrabili | Fonti primarie, dati fisici, benchmark indipendenti e rifiuti fuori dominio per **ciascuna** norma/versione | Da fare |
| **F7** | SLE e legno NTC/EC5 separati | Frecce/SLE pertinenti e regole legno mancanti scelte per priorità | Provenienza delle combinazioni, servizio, durata/umidità e ipotesi esterne testate | Da fare |
| **F8** | Robustezza, prestazioni, eventuali estensioni | Limiti solver denso; valutazione sparse/Timoshenko solo se motivata | Benchmark di scala e convergenza; nessuna estensione opt-in presentata come generale | Da fare |
| **F9** | API/CLI/report stabili e beta | Contratti versionati, esempi, Validation Manual, release; opzionale screening profili | Regressioni .NET e documentazione per ambito dichiarato; nessuna promessa di certificazione | Da fare |
| **F10** | **GUI desktop C#/Avalonia**, soltanto alla fine | Editor 2D, cataloghi, azioni, risultati, diagrammi, verifiche e report | UI consuma API convalidate, senza replicare il FEM o le formule normative | **Bloccata da F9** |

## Prossima consegna tecnica: F1.1 G8B

1. Definire **prima dell'implementazione** matrice delle 16 esecuzioni, nodi/supporti/membri/stazioni attese, sistema di unità kN–m–s, segni e tolleranze relative/assolute.
2. Leggere i numeri già congelati nel JSON indipendente G8B **solo come dati statici**; verificare loro origine e ambito. Completare gli attesi N/V/M mancanti con formule o metodo di riferimento autonomo **in C#**, documentando la derivazione; non copiarli dal FEM da testare.
3. Scrivere un **test harness .NET/xUnit** che esegua tutti gli scenari mediante le API pubbliche e confronti ogni grandezza, esponendo differenze con ID, membro/stazione, unità e segno.
4. Inserire controprove: forza invertita, release modificata, sezione cambiata e combinazione omessa devono generare un mismatch. Verificare i viewer previsti senza confonderli con il confronto FEM.
5. Eseguire `dotnet build` e `dotnet test` sulla soluzione completa. Riportare i risultati reali: **PASS confermato**, **FAIL** o **PENDING** se non eseguiti, senza inventare conteggi.

**Gate F1.1:** 16/16 confronti senza scarti inspiegati e controprove correttamente rilevate; registrare i limiti del modello 2D ideale. **Non introdurre nuove formule normative, GUI o script Python in F1.1.**

**Avanzamento F1.1-A — PASS riferito dall'autrice:** test C# dei 16 scenari, spostamenti, reazioni, equilibrio e N/V/M **delle colonne inferiori**, con controprove e SHA256 degli attesi. Leggere [rapporto F1.1-A](F1_1A_G8B_VALIDAZIONE_CSHARP_16_SCENARI.md).

**Avanzamento F1.1-B — PASS riferito dall'autrice:** formule analitiche **indipendenti** `N/V/M` delle due mensole, equilibrio di tutti i Frame2D e dei nodi non vincolati, legge assiale degli X-brace Truss2D e controprova con carico modificato. **Questi controlli di equilibrio NON sono golden numerici assoluti indipendenti per tutte le aste.** Leggere [rapporto F1.1-B](F1_1B_G8B_SOLLECITAZIONI_EQUILIBRIO_CSHARP.md).

**Avanzamento F1.1-C — PASS riferito dall'autrice:** il riferimento C# variazionale (quadratura di energia/carichi + Cholesky) è **autonomo** rispetto alle matrici e al solver produttivi. Per ogni scenario riproduce prima gli spostamenti/reazioni del **golden storico con SHA256 bloccato**, poi confronta tutti i nodi e `N/V/M` a cinque stazioni di ogni asta. I quattro JSON G8B sono protetti da hash. **I valori per tutte le aste non sono ancora congelati come JSON autonomo**: sono calcolati nei test da dati congelati. Vedere [rapporto F1.1-C](F1_1C_G8B_RIFERIMENTO_VARIAZIONALE_CSHARP.md).

**Avanzamento F1.1-D — FIX1 PASS riferito dall'autrice:** nuovo programma standalone **C#/.NET 8** nella soluzione. Riesegue l'oracolo storico con input SHA256-bloccati; produce solo tramite comando esplicito un JSON **proposto** con forze alle estremità, N/V/M a cinque stazioni, DOF e reazioni per **336 membri-scenario / 1.680 stazioni**. Non sovrascrive file ed è verificabile byte per byte. Il JSON **non è stato trasmesso in questa conversazione**: mancano revisione, hash fissato e test contro il golden statico. Vedere [F1.1-D](F1_1D_G8B_ESPORTAZIONE_GOLDEN_NVM_CSHARP.md).

**Avanzamento F1.1-E — PASS riferito dall'autrice:** 37 test C# nei **16 scenari** per geometria SVG delle polilinee N/V/M con normale locale firmata, tabelle Markdown confrontate con F1C, frecce di vento/gravità, payload HTML e controprova di segno del momento. Unico warning `xUnit2031` su `Assert.Single(Where(...))`, corretto nel successivo F1.2-A senza cambiare il significato dell'asserzione. FEM, esportatori, esempi e moduli normativi invariati. Vedere [F1.1-E](F1_1E_G8B_VALIDAZIONE_VIEWER_REPORT_CSHARP.md).

**Avanzamento F1.2-A — 12 nuovi test C# pronti, .NET PENDING:** sei mensole inclinate con carichi concentrati assiali/trasversali di segno opposto, quattro travi inclinate a due release con carico uniforme `LocalY` negativo e carrello globale verticale, una combinazione di carico con coefficiente negativo e una controprova d'inversione. Attesi da formule chiuse di Euler–Bernoulli e statica, non dal FEM di produzione; confronto di spostamenti, rotazioni, reazioni e N/V/M alle cinque sezioni. **Nessuna modifica a `src/`**. Il sottogruppo successivo F1.2-B dovrà esplorare release singole, orientamento dei supporti e altre azioni locali; F1.3 svilupperà prove di robustezza e scala. Vedere [F1.2-A](F1_2A_ORIENTAMENTO_CARICHI_RELEASE_ANALITICI_CSHARP.md).

**Per chiudere F1.1:** ricevere il JSON F1D proposto dal PC Windows, revisionarlo, **congelare hash e valori in test xUnit C#** ed eseguire il controllo incrociato con il golden statico. **F1E è già PASS**; parallelamente F1.2-A deve essere collaudato, poi seguono altri carichi/orientamenti/release F1.2 e **F1.3** robustezza. Senza il golden statico approvato non dichiarare il gate F1.1 completo.

## Prerequisiti decisivi

| Prima di dichiarare... | Servono almeno... |
|---|---|
| «Asta verificata» | F2 evidenze fisiche, F3 combinazioni, F4 massimi, F5 obblighi, F6/F7 regole applicabili |
| «Instabilità valutata» | Ritegni, lunghezze, modello resistente e dati esterni tipizzati (F2) |
| «Profilo suggerito per il progetto» | Copertura esplicita degli obblighi e risultati pertinenti (F5–F7) |
| «Beta didattica affidabile» | F1–F5 chiuse e un sottoinsieme F6/F7 validato con limiti dichiarati |
| «GUI conclusiva» | Contratti/report/documentazione stabili (F9) |

## Dove annotare i risultati

- [CURRENT_STATE](CURRENT_STATE.md): solo ciò che è **effettivamente disponibile** e l'ultimo gate confermato.
- [CHANGELOG](../../CHANGELOG.md): fatti per checkpoint; distingue conferme .NET da regressioni aritmetiche storiche.
- [Piano completo](PIANO_DETTAGLIATO_POST_D2C2.md): criteri e rischi tecnici approfonditi F0–F10.
- [Handoff](../../HANDOFF_NUOVA_CHAT.md): ripresa sintetica e puntatori, non diario tecnico.
- [README](../../README.md): funzioni, limiti, avvio rapido e link essenziali, **senza** roadmap dettagliata.
