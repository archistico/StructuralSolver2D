# StructuralSolver2D — Roadmap operativa dopo G7

**Aggiornata il 25/10/2026 (checkpoint P0-HANDOFF).** Fonte corrente di priorità: questo file; i vecchi checkpoint M51–M85 sono conservati in [`docs/archive/roadmap_PRE_G7.md`](../archive/roadmap_PRE_G7.md) e **non** rappresentano lo stato presente. Per la missione di prodotto leggere [visione 2D](vision-and-roadmap-2d.md).

## Obiettivo e ordine di sviluppo

Un software **FEM strutturale 2D open source**, semplice e modulare, per travi, telai e reticolari in **acciaio e legno**; destinato a studenti, docenti e professionisti per studio e valutazioni preliminari. Resta inderogabile: **(1) motore numerico affidabile e validato; (2) regole normative e verifiche SLU/SLE tracciabili; (3) soltanto alla fine GUI leggera Avalonia/C#.** Esclusi 3D, c.a., muratura, BIM, piastre, pareti e X-LAM.

## Stato delle milestone

| Fase | Gate Windows | Cosa valida e cosa NON valida |
|---|---|---|
| M86 e precedenti, M89–M91 | Superati nei rispettivi perimetri, secondo le conferme storiche | FEM 2D e regole parziali EC3; non copertura normativa generale |
| M92-A/B | **Superato** | Verifiche pure e coefficienti EC5 prima generazione; non annessi nazionali e completezza asta |
| G1 FIX1 | **Superato** | Coefficienti NTC, isolamento da EC5; non combinazioni complete |
| G2 | **Superato** | Verifiche pure legno NTC, singola sezione |
| G3 FIX1 | **Superato** | Interazioni N–M locali e taglio nella stessa sezione; non instabilità |
| G4 | **Superato** | Stabilità legno in ipotesi documentate; non verifica fisica dei ritegni |
| G5 | **Superato** | Frecce SLE e deformazioni differite in perimetro dichiarato |
| G6 | **Superato** | Audit copertura dichiarata legno; non validazione della completezza autonoma del piano |
| G7 | **Superato** | Benchmark B13/B14 e pipeline reale selettore→regola→audit; non conformità globale |
| G7-DOC1 | Riordino documentale; non richiede nuovi test numerici | README/roadmap/changelog/handoff, manifest rigenerato |

## Decisione attuale e punto di ripresa

L'utente ha confermato **`G7 GATE PASSED`** anche dopo gli aggiornamenti dimostrativi G8B FIX2 e ha scelto di procedere con **P0**, iniziando obbligatoriamente da **P0-A (audit tecnico completo)**. L'audit **non è ancora stato eseguito**: la prossima chat deve partire dai sorgenti e dai test correnti, come descritto in [`P0_A_PIANO_OPERATIVO.md`](P0_A_PIANO_OPERATIVO.md), costruire la matrice di copertura e formulare un backlog testabile prima di modificare le formule.

La casetta G8B e le quattro alternative sono esempi di prova **non dimensionati e non normativamente conformi per definizione**: riferimento `docs/structural/examples/G8B_VERIFICA_CASSETTA_E_CONFRONTO.md`. L'utente ha confermato la suite G7, **non** l'esecuzione documentata del confronto indipendente dei 16 report G8B. Il più recente archivio completo di ripresa è `StructuralSolver2D_P0_HANDOFF_2026-10-25_FULL.zip` (solo aggiornamento documentale rispetto a G8B FIX2).

## Priorità dopo G7 (non implementate dal solo riordino)

### P0 — Chiusura dei rischi di affidabilità e perimetro

1. **P0-A, PROSSIMO: audit formale dei gap:** matrice unica tra fenomeni fisici rilevanti, moduli FEM, regole EC3/EC5/NTC implementate e scenari non supportati. Non confondere «not applicable» e «not implemented».
2. **P0-B, successivo: azioni e combinazioni:** specifica e test indipendenti per generazione/validazione delle combinazioni SLU/SLE NTC (incluse azioni favorevoli/sfavorevoli, azione principale/accompagnatrici, coefficienti nulli e negativi); G1 non è un generatore completo.
3. **P0-C, successivo: completezza di stazioni e membrature:** verificare criterio di selezione delle sezioni critiche e concomitanza N/V/M; non attribuire a un inviluppo isolato un'interazione normativa.
4. **P0-E, successivo: validazione delle condizioni esterne:** appoggi, collegamenti, ritegni e instabilità fuori piano restano *dati o verifiche esterni*, non inferenze automatiche dal FEM piano.
5. **P0-D, successivo: acciaio e copertura normativa:** riesame dei gap EC3/NTC, sezioni nette, fori, interazioni fuori campo e stabilità, con casi indipendenti ricavati dal Ballio–Mazzolani (non fonte normativa sostitutiva).

### P1 — Accuratezza del modello e benchmark aggiuntivi

- Confrontare **Euler–Bernoulli 2D** con formule di Timoshenko per travi corte/tozze; poi decidere se introdurre un elemento alternativo opt-in, isolato e rigorosamente collaudato.
- Casi di **reazione negativa e contatto monolatero**, **aste tension-only**, eccentricità dei nodi e controventi: non attribuire al solver lineare un comportamento non implementato.
- Rafforzare riferimenti su instabilità e rigidezze di vincolo; gestire gli effetti geometrici M82 come **linearizzazione delimitata**, non analisi generale del secondo ordine.
- Promuovere a benchmark automatici soltanto gli esempi del libro completamente determinati: vedere `benchmarks/G6_COVERAGE_REFERENCE_CROSSWALK.json` e `docs/G7_MANUALE_VALIDAZIONE_INDIPENDENTE.md`.

### P2 — Consolidamento documenti, API, report e qualità

- Rapporto che espone input, fonti, norma/edizione, combinazione, sezione/stazione/lato, esito, coperture e limitazioni.
- Coerenza e compatibilità tra JSON, API, CLI e report; test di non regressione per ogni nuova funzione.
- QA grafica separata per viewer esistenti; nessuna UI desktop anticipata.

### P3 — GUI **solo alla fine**

Editor e visualizzatore 2D in **C#/Avalonia** con nodi, aste, vincoli, carichi, combinazioni e risultati. Deve consumare il motore già validato; non è un CAD/BIM generale.

## Regola di promozione e stato

Un incremento si considera **testato** soltanto dopo build e suite su Windows con conferma dell'utente; documentare il perimetro di ogni gate. I golden numerici non si correggono per far superare un test senza una dimostrazione indipendente. Una verifica singola «Passed» non dimostra **conformità dell'intera asta**. A ogni consegna aggiornare [README](../../README.md) *solo se cambiano funzioni/uso*, [CHANGELOG](../../CHANGELOG.md) con una voce sintetica e **sempre** [HANDOFF_NUOVA_CHAT](../../HANDOFF_NUOVA_CHAT.md).
