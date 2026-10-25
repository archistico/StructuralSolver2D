# StructuralSolver2D — Stato corrente verificabile

**Aggiornamento: 25 ottobre 2026 — P0-F1.2-A, benchmark analitici C# per orientamento, carichi locali e release.** L'ultimo **gate .NET confermato dall'autrice** è **P0-F1E** («test passati», senza log allegato), con un avviso non bloccante `xUnit2031` corretto in questo checkpoint. **F1.2-A aggiunge 12 casi xUnit ancora da compilare e provare; i sorgenti FEM di produzione non cambiano.** Per la direzione del progetto leggere [visione 2D](vision-and-roadmap-2d.md); per le prossime attività [roadmap](roadmap.md), [rapporti F1A](F1_1A_G8B_VALIDAZIONE_CSHARP_16_SCENARI.md), [F1B](F1_1B_G8B_SOLLECITAZIONI_EQUILIBRIO_CSHARP.md), [F1C](F1_1C_G8B_RIFERIMENTO_VARIAZIONALE_CSHARP.md), [F1D](F1_1D_G8B_ESPORTAZIONE_GOLDEN_NVM_CSHARP.md), [F1E](F1_1E_G8B_VALIDAZIONE_VIEWER_REPORT_CSHARP.md), [F1.2-A](F1_2A_ORIENTAMENTO_CARICHI_RELEASE_ANALITICI_CSHARP.md) e [piano dettagliato](PIANO_DETTAGLIATO_POST_D2C2.md); per le decisioni vincolanti [regole di sviluppo](ENGINEERING_AND_VALIDATION_RULES.md).

## Ultima conferma e test

| Cosa | Stato esatto | Limite dell'affermazione |
|---|---|---|
| Gate .NET e regressioni fino a **P0-F1E** | **PASS dichiarato dall'autrice** («test passati») | Comprende F1A–F1D-FIX1 e i 37 nuovi casi F1E sul suo PC; conferma testuale senza log completo; non certifica una struttura |
| Programma C# standalone **P0-F1D-FIX1** | **PASS riferito, JSON NON RICEVUTO** | Hash incompleto del primo avvio corretto; non abbiamo il file `proposed` né il suo SHA256 e perciò nessun golden statico completo approvato |
| Suite di post-processing **P0-F1E** | **PASS riferito dall'autrice; warning xUnit2031 corretto in F1.2-A** | 16 scenari SVG, 16 Markdown, frecce dei carichi, viewer HTML e controprova del segno; la correzione riguarda solo un'asserzione xUnit |
| Benchmark analitici **P0-F1.2-A** | **12 NUOVI TEST C# — .NET PENDING** | Sei mensole ruotate, quattro travi inclinate a due release, combinazione con fattore negativo e controprova di carico invertito; SDK non presente qui |
| Regressione aritmetica indipendente R2–D2C2 | **4.203/4.203 PASS in 15 script** nel checkpoint D2C2 | Numeri storici derivati da strumenti Python; **non** sono esecuzioni C# né nuovi test F0 |
| Manifest SHA256 F1.2-A | **Rigenerato e verificato in fase di confezionamento** | Verifica byte per byte dei file distribuiti, distinta dai test FEM .NET |
| G8B, confronto dei 16 scenari con riferimento indipendente congelato | **F1A — PASS comunicato dall'autrice** | U/reazioni/equilibrio e N/V/M colonne inferiori, dati indipendenti SHA256 bloccati |
| G8B, nuovi controlli di aste, sbalzi, nodi e diagonali | **F1B — PASS comunicato dall'autrice** | Valori assoluti indipendenti per i balconi; per altre aste equilibrio/identità fisiche |
| G8B, risolutore variazionale di controllo e N/V/M su tutte le aste | **F1C — PASS comunicato dall'autrice** | Quadratura + Cholesky indipendenti ancorati al golden storico. Forze N/V/M ricalcolate in test; il golden statico per tutte le aste resta da esportare |
| G8B, export statico 336 membri-scenario e 1.680 stazioni | **F1D-FIX1 — PASS riferito, golden non ancora congelato** | Tool C# con verifica input e golden storico; manca consegna del JSON e hash definitivo |

## Capacità effettive e lacune prioritarie

| Modulo | Implementato e collaudato nel perimetro dichiarato | Da completare |
|---|---|---|
| **FEM Core/Analysis** | Frame2D Euler–Bernoulli, Truss2D, modelli misti, vincoli/release/molle/cedimenti, carichi, peso proprio esplicito, reazioni e N/V/M; benchmark e casi limite; F1E PASS riferito | Ricevere e congelare il JSON statico N/V/M F1D; provare F1.2-A, quindi orientamenti/release residui e robustezza; confini fisici su supporti/controventi; performance/solver opzionali solo se motivati |
| **Azioni e combinazioni P0-B1–B4** | Inventario azioni tipizzato, candidati ULS/SLE nel dominio supportato, audit su insieme *dichiarato* e fingerprint | Esaustività del dominio applicabile, alternative/azioni guida, condizioni di favore e scenari non supportati |
| **FEM→verifiche P0-C1–C2C1** | Snapshot concomitanti N/V/M, ascissa e lato esatti, diagnostici e audit locali, primo caso reale EC3 a N di trazione costante | Massimi continui di utilizzo `η(x)` per regola, inventario completo degli obblighi di membratura e report aggregato |
| **Acciaio EC3 M89–M91** | Regole pure/sezionali, classificazione e alcuni controlli di instabilità con dati esterni | Copertura parziale di interazioni e instabilità; classi/prodotti/assi fuori campo; **non** equivalenti a NTC |
| **Acciaio NTC D1, D0–D0M8, D2A–D2C2** | Trazione lorda/netta e gerarchia delimitata, classi I/H, compressione, flessione, taglio, interazione M–V per N=0 in casi precisi | Resistenza N–M/N–M–V generale, instabilità di membro e d'anima, classe 4 efficace, SLE acciaio, fori/giunzioni negli altri rami |
| **Legno EC5 M92-A/B** | Resistenze pure di rettangoli, selezione coefficienti | Interazioni, stabilità/SLE e altri casi EC5 non ancora coperti in modo equivalente alle NTC |
| **Legno NTC G1–G7** | Coefficienti, pure e interazioni N–M/taglio, alcuni controlli di stabilità/SLE, audit dichiarato e benchmark | Copertura di geometrie, collegamenti, condizioni fisiche, scenari e massimi lungo membratura |
| **Cataloghi M88** | Profili acciaio OPPO IPE/HEA/HEB/INP e resistenze caratteristiche acciaio/legno | Dati **nominali indicativi**, non certificati di prodotto; diverse regole avanzate non supportano INP |
| **CLI e reporting** | Analisi JSON, diagrammi, esportazioni e viewer HTML | Percorso normativo integrato, report obblighi e documentazione senza ambiguità di `Passed` locale |
| **GUI desktop** | Non avviata | C#/Avalonia **esclusivamente P3**, dopo API, verifica e report consolidati |

**Avvertenza obbligatoria:** `DesignCheckReport.OverallStatus.Passed` significa soltanto che sono passate le **regole richieste**, non che sono state richieste tutte le verifiche applicabili. Le attività **F2–F7** della roadmap devono impedire qualunque falsa dichiarazione di conformità di membratura o opera.

## Decisioni di progetto confermate

1. **Solo strutture piane 2D in acciaio e legno**; esclusi 3D, c.a., muratura, piastre/pareti, X-LAM e BIM.
2. **Motore indipendente dalla normativa** e GUI solo dopo analisi/validazione/verifiche.
3. **NTC 2018, EC3 ed EC5 rigorosamente distinti** per modulo, edizione, coefficienti, annesso e report. Un PASS EC3 non implica PASS NTC.
4. **Nuovo codice, nuovi test e nuovi script/strumenti eseguibili soltanto in C#/.NET**. **Non creare o eseguire script Python** nei checkpoint futuri. Gli script Python preesistenti restano storici, congelati, senza nuove dipendenze.
5. Per i nuovi benchmark indipendenti: **C# test harness + valori attesi derivati indipendentemente e congelati**, senza chiamare il solutore produttivo come proprio oracolo.
6. Cataloghi OPPO indicativi; usare le fonti e il libro di Bernuzzi come riferimenti didattici, ma controllare clausole normative su documenti normativi pertinenti.
7. Ogni gate deve distinguere **codice realizzato**, **test .NET confermato dall'autrice**, **test non eseguito** e **limiti normativi/fisici**.

## Prossimo checkpoint tecnico

**F1E è PASS dichiarato dall'autrice.** Nel FULL F1.2-A è stato corretto il warning `xUnit2031` e sono pronti 12 benchmark nuovi confrontati con formule chiuse di trave ed equilibrio. **Eseguire build/test F1.2-A su Windows; consegnare il JSON `proposed` prodotto da F1D con SHA256** per revisione e congelamento numerico immutabile. **Il gate F1.1 resta aperto** finché il golden statico completo non sarà approvato e vincolato a nuovi test. Poi completare i sottogruppi restanti di **F1.2** orientamenti/carichi/release e **F1.3** robustezza. Nessuna nuova formula normativa o Python. Successivi: **F2 evidenze fisiche → F3 completezza combinazioni → F4 massimi di utilizzo → F5 copertura e report → F6/F7 norme → F8 prestazioni → F9 API/release → F10 GUI**.

## Collocazione documentale

- [`README.md`](../../README.md): missione, capacità essenziali, requisiti, comandi utente e link. **Non** cronologia dettagliata o roadmap estesa.
- [`CURRENT_STATE.md`](CURRENT_STATE.md): stato autorevole e ultimo gate.
- [`roadmap.md`](roadmap.md): priorità operative, dipendenze e criteri di conclusione.
- [`PIANO_DETTAGLIATO_POST_D2C2.md`](PIANO_DETTAGLIATO_POST_D2C2.md): ragioni, elenco completo F0–F10 e rischi.
- [`ENGINEERING_AND_VALIDATION_RULES.md`](ENGINEERING_AND_VALIDATION_RULES.md): policy C# e separazione delle norme.
- [`HANDOFF_NUOVA_CHAT.md`](../../HANDOFF_NUOVA_CHAT.md): breve testo di ripresa con puntatori ai documenti autorevoli.
- [`CHANGELOG.md`](../../CHANGELOG.md): storia cronologica, fatti dei singoli checkpoint.
- [`docs/archive/`](../archive/): vecchie roadmap e handoff conservati **per storia**, non per l'ultimo stato.
