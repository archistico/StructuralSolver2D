# StructuralSolver2D — Stato corrente verificabile

**Aggiornamento: 25 ottobre 2026 — P0-F0, riordino della sola documentazione.** Il più recente **checkpoint con modifiche al codice** è **P0-D2C2** (`StructuralSolver2D_P0D2C2_INTERAZIONE_M_V_NTC2018_2026-10-25_FULL.zip`). Il checkpoint documentale F0 contiene lo stesso codice. Per la direzione del progetto leggere [visione 2D](vision-and-roadmap-2d.md); per le prossime attività [roadmap](roadmap.md) e [piano dettagliato](PIANO_DETTAGLIATO_POST_D2C2.md); per le decisioni vincolanti [regole di sviluppo](ENGINEERING_AND_VALIDATION_RULES.md).

## Ultima conferma e test

| Cosa | Stato esatto | Limite dell'affermazione |
|---|---|---|
| Gate .NET e regressioni fino a **P0-D2C2** | **PASS dichiarato dall'autrice** («test passati») | Conferma testuale, senza log/numero di test; non è una certificazione di sicurezza strutturale |
| Nuovi test .NET nel checkpoint **P0-F0** | **Nessuno** | Solo documentazione; la sorgente/test xUnit coincidono con P0-D2C2 |
| Regressione aritmetica indipendente R2–D2C2 | **4.203/4.203 PASS in 15 script** nel checkpoint D2C2 | Numeri storici derivati da strumenti Python; **non** sono esecuzioni C# né nuovi test F0 |
| Manifest SHA256 F0 | **Verificato durante il confezionamento** | Controlla l'integrità dei file; non è un'esecuzione dei test .NET |
| Esempio G8B, confronto automatico completo con riferimento indipendente | **Ancora da realizzare** | I test generali confermati non dimostrano l'equivalenza numerica di tutte le 16 simulazioni |

## Capacità effettive e lacune prioritarie

| Modulo | Implementato e collaudato nel perimetro dichiarato | Da completare |
|---|---|---|
| **FEM Core/Analysis** | Frame2D Euler–Bernoulli, Truss2D, modelli misti, vincoli/release/molle/cedimenti, carichi, peso proprio esplicito, reazioni e N/V/M; benchmark e casi limite | Equivalenza end-to-end C#↔golden indipendenti G8B; confini fisici su supporti/controventi; performance/solver opzionali solo se motivati |
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

**F1.1 — 16 scenari G8B**, confronto automatico del FEM di produzione con riferimenti numerici indipendenti e congelati tramite **test in C#/.NET**: spostamenti, reazioni e N/V/M a stazioni concordate, convenzioni e tolleranze prima dell'esecuzione, input alterato intenzionalmente per verificare che il confronto fallisca. **Non aggiungere formule normative o Python.** Successivi: **F2 evidenze fisiche → F3 completezza combinazioni → F4 massimi di utilizzo → F5 copertura e report → F6/F7 norme → F8 prestazioni → F9 API/release → F10 GUI**.

## Collocazione documentale

- [`README.md`](../../README.md): missione, capacità essenziali, requisiti, comandi utente e link. **Non** cronologia dettagliata o roadmap estesa.
- [`CURRENT_STATE.md`](CURRENT_STATE.md): stato autorevole e ultimo gate.
- [`roadmap.md`](roadmap.md): priorità operative, dipendenze e criteri di conclusione.
- [`PIANO_DETTAGLIATO_POST_D2C2.md`](PIANO_DETTAGLIATO_POST_D2C2.md): ragioni, elenco completo F0–F10 e rischi.
- [`ENGINEERING_AND_VALIDATION_RULES.md`](ENGINEERING_AND_VALIDATION_RULES.md): policy C# e separazione delle norme.
- [`HANDOFF_NUOVA_CHAT.md`](../../HANDOFF_NUOVA_CHAT.md): breve testo di ripresa con puntatori ai documenti autorevoli.
- [`CHANGELOG.md`](../../CHANGELOG.md): storia cronologica, fatti dei singoli checkpoint.
- [`docs/archive/`](../archive/): vecchie roadmap e handoff conservati **per storia**, non per l'ultimo stato.
