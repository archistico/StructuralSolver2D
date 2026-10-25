# StructuralSolver2D — Roadmap operativa dopo P0-C2A

**Aggiornata il 25/10/2026 (checkpoint P0-C2A, gate Windows da eseguire).** Fonte corrente di priorità: questo file; i vecchi checkpoint M51–M85 sono conservati in archivio e **non** rappresentano lo stato presente. Per la missione di prodotto leggere [visione 2D](vision-and-roadmap-2d.md).

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

**Aggiornamento autorevole P0-C2A, 25/10/2026:** l'utente ha confermato **P0-C1 FIX1 PASS** («test passati», senza log numerico aggiornato), dopo il singolo test errato identificato nel primo log. In C2A implementate due **metriche diagnostiche** con ricerca analitica del massimo su tratti a forze affini: somma normalizzata di valori assoluti e prodotto assiale×momento; risoluzione FEM reale, discontinuità, fattori negativi e casi `ScreeningIncomplete` per distributed/selfweight. **Non si tratta delle formule EC3/EC5/NTC né di un giudizio di idoneità.** Nuovi xUnit C2A da eseguire su Windows insieme alla regressione G7. Prossimo C2B (polinomi di distribuiti), C2C (funzioni normative individuali), C3/C4 (obblighi/report), E/D; GUI P3. FULL: `StructuralSolver2D_P0C2A_ESTREMI_AFFINI_2026-10-25_FULL.zip`, dettagli in [P0_C2A_MASSIMI_UTILIZZO_DIAGNOSTICO_AFFINE.md](P0_C2A_MASSIMI_UTILIZZO_DIAGNOSTICO_AFFINE.md). Gli aggiornamenti precedenti sotto sono storici.

**Aggiornamento autorevole P0-C1 FIX1, 25/10/2026:** primo log Windows: build **0 errori/0 warning**, 3/3 G7 Analysis, 3/3 G7 integrazione, **527/528 Verification**. Errore solo nel test C1 che sceglieva **x=0, Continuous (valido)** anziché **x=2, BeforePointLoad (salto)**; correzione nel test all'indice 1 e nuova regressione positiva. **Codice FEM e C1 di produzione invariato**. Ripetere gate C1 su Windows; soltanto dopo iniziare P0-C2. FULL corrente: `StructuralSolver2D_P0C1_FIX1_TEST_STAZIONE_2026-10-25_FULL.zip`. Le note P0-C1/B precedenti sotto sono storiche.

**Aggiornamento autorevole P0-C1, 25/10/2026:** P0-B4 è **GATE PASS per conferma dell'utente**, oltre ai precedenti B2/B3 e G7. C1 è implementato/test xUnit predisposti in `Verification.Integration`: rianalisi FEM per target e controllo delle terne N/V/M **concomitanti** a ciascuna stazione dichiarata con lato e profilo; **non** dimostra picchi della funzione di utilizzo nell'intervallo o copertura di tutte le verifiche. Nuovi test da eseguire su Windows (`FemConcomitantStationAuditC1Tests` e regressione G7), non compilati qui in assenza SDK. Prossimi: **P0-C2 massimo di utilizzo**, **C3 obblighi membri/regole**, **C4 report**, P0-E fisica, P0-D regole; GUI soltanto P3. FULL corrente: `StructuralSolver2D_P0C1_CONCOMITANZA_FEM_2026-10-25_FULL.zip`; dettagli in [P0_C1_AUDIT_SOLLECITAZIONI_CONCOMITANTI.md](P0_C1_AUDIT_SOLLECITAZIONI_CONCOMITANTI.md). Le sezioni P0-B1…B4 sottostanti sono aggiornamenti storici.

**Aggiornamento autorevole P0-B4, 25/10/2026:** P0-B2 e P0-B3 sono **GATE PASS per conferma dell'utente** (senza contatori di log). B4 implementa validator di **piano finito dichiarato per effetto**, generazione baseline B3 e controllo SHA-256 dei veri input e termini delle combinazioni **presenti nel modello FEM**. Nuovi xUnit B4, **non compilati localmente per assenza SDK**: servono `Ntc2018DeclaredCombinationAuditB4Tests` e regressione `run-g7-tests.ps1` su Windows. Un esito `MatchedDeclaredPlan` **non** attesta che tutte le azioni fisiche, i casi di carico, gli effetti o le condizioni normative esistenti siano stati scoperti. Dopo il gate B4 si passa a **P0-C** e alla delimitazione fisica **P0-E**, poi **P0-D**; GUI soltanto P3. FULL corrente: `StructuralSolver2D_P0B4_AUDIT_PIANO_DICHIARATO_2026-10-25_FULL.zip`; istruzioni in [P0_B4_AUDIT_INSIEME_DICHIARATO.md](P0_B4_AUDIT_INSIEME_DICHIARATO.md). I precedenti aggiornamenti P0-B1/B2/B3 sotto sono storico.

**Aggiornamento P0-A, 25/10/2026, prevalente sulle note storiche sotto:** audit documentale completato, senza nuovo gate .NET. Si vedano P0_A_AUDIT_TECNICO.md, P0_A_MATRICE_COPERTURA.md, P0_A_PRIORITA_E_CRITERI_DI_TEST.md. Ultimo gate confermato: G7. Ricalcolo Python G8B 16/16 senza discrepanze nei campi verificati, mentre il confronto C#↔Python è ancora mancante. **Prossimo P0-B**, poi P0-C; i contratti fisici P0-E vanno predisposti durante B/C; estensioni P0-D dopo le precondizioni; GUI P3. Le righe storiche successive che definiscono P0-A come prossimo lavoro sono superate.

**Aggiornamento P0-B1, 25/10/2026:** specifica NTC 2018 STR A1/SLE, tabelle γ/ψ da Gazzetta Ufficiale, **14 casi aritmetici + 10 controlli negativi superati con Python**. Vedere [contratto P0-B1](P0_B1_CONTRATTO_AZIONI_NTC2018.md) e benchmarks/independent/p0b. **Prossimo P0-B2**: modelli di azione e validazione tipizzati, compatibilità con manuali e test xUnit; **B3** generazione SLU/SLE delimitata; **B4** audit completezza/FEM e gate Windows. Nessun nuovo gate .NET, nessuna verifica strutturale automatica completa.

La casetta G8B e le quattro alternative sono esempi di prova **non dimensionati e non normativamente conformi per definizione**: riferimento `docs/structural/examples/G8B_VERIFICA_CASSETTA_E_CONFRONTO.md`. L'utente ha confermato la suite G7, **non** l'esecuzione documentata del confronto indipendente dei 16 report G8B. Il più recente archivio completo di ripresa è `StructuralSolver2D_P0_HANDOFF_2026-10-25_FULL.zip` (solo aggiornamento documentale rispetto a G8B FIX2).

**Aggiornamento P0-B2, 25/10/2026:** implementato il modulo separato Combinations, inventario G1/G2/Q e validator con stati conservativi, round-trip JSON e xUnit in Core.Tests. **Non compilato né collaudato in .NET** per assenza SDK; il testo storico precedente sui checkpoint è superato. Primo passo: gate Windows B2 più intera suite G7; poi P0-B3 (generatore), B4 (audit completezza). FULL corrente: StructuralSolver2D_P0B2_AZIONI_VALIDATE_2026-10-25_FULL.zip.

**Aggiornamento autorevole P0-B3, 25/10/2026:** l'utente ha confermato **«test passati»** dopo P0-B2. Il generatore NTC 2018 ora produce soltanto candidati ULS STR A1 e SLE per **uno scenario/effetto esplicito** e i relativi manifesti; legge γ/ψ versione fissa e omette i fattori nulli nel solo report di omissione. **XUnit B3 da eseguire su Windows** confrontando 14 golden P0-B1 immutati, poi regressione G7. Non ancora audit di completezza (B4), capacità 3D o conformità dell'edificio. FULL corrente: StructuralSolver2D_P0B3_COMBINAZIONI_CANDIDATE_2026-10-25_FULL.zip.

## Priorità dopo G7 (non implementate dal solo riordino)

### P0 — Chiusura dei rischi di affidabilità e perimetro

1. **P0-A, COMPLETATO documentalmente:** matrice unica codice/test per FEM e regole, stati distinti, rischi e prove di accettazione; non nuovo gate .NET.
2. **P0-B1, COMPLETATO come specifica:** equazioni, tabelle NTC2018 fissate, contratto delle azioni, 14 golden e 10 controprove Python indipendenti dal C#; nessun algoritmo FEM o generatore modificato.
3. **P0-B2, GATE PASS CONFERMATO DALL'UTENTE:** tipi di azioni, categoria, alternative, provenienza, validator e round-trip JSON. Nessun log numerico allegato.
4. **P0-B3, GATE PASS CONFERMATO DALL'UTENTE:** coefficienti e generatore candidati ULS STR A1/SLE con favorevolezza manuale documentata, 14 golden B1 referenziati da xUnit, omissioni tracciate.
5. **P0-B4, GATE PASS CONFERMATO DALL'UTENTE:** audit di **insieme finito dichiarato** e fingerprint input FEM; non prova autonoma di esaustività globale. Sisma, GEO/EQU e incendio fuori profilo.
6. **P0-C1, GATE PASS CONFERMATO DALL'UTENTE:** ricalcolo e confronto della tripla N/V/M sulle stazioni dichiarate. **P0-C2A, CODICE/TEST PRONTI, GATE WINDOWS IN ATTESA:** due indici matematici e prova delimitata a tratti affini; `ScreeningIncomplete` per distribuiti/selfweight. **P0-C2B** prossimo: diagrammi di grado superiore; **C2C**: singole formule normative; **C3**: completezza obblighi e preminenza `Failed`; **C4**: report di stazioni/esattezza/campioni.
7. **P0-E, successivo/in parallelo come contratto fisico:** appoggi, collegamenti, ritegni e instabilità fuori piano restano *dati o verifiche esterni*, non inferenze automatiche dal FEM piano.
8. **P0-D, successivo dopo B/C/E: acciaio e copertura normativa:** riesame dei gap EC3/NTC, sezioni nette, fori, interazioni fuori campo e stabilità, con casi indipendenti ricavati dal Ballio–Mazzolani (non fonte normativa sostitutiva).

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
