# P0-A — Piano operativo di audit tecnico FEM e verifiche normative

**Piano originario P0-HANDOFF del 25/10/2026. Stato aggiornato:** P0-A completato documentalmente il 25/10/2026. Risultati nei tre file P0_A_AUDIT_TECNICO.md, P0_A_MATRICE_COPERTURA.md e P0_A_PRIORITA_E_CRITERI_DI_TEST.md nella stessa directory. P0-B1 completato come specifica e golden in P0_B1_CONTRATTO_AZIONI_NTC2018.md; prossima P0-B2. Nessun nuovo gate .NET.
**Baseline:** G7 GATE PASSED, archivio corrente P0-HANDOFF derivato da G8B FIX2.  
**Non si richiede implementazione immediata di nuove formule.** Prima serve un quadro verificabile di capacità, limiti e lacune.

## Scopo e criteri di affidabilità

Identificare il **perimetro effettivo** del motore StructuralSolver2D e di tutte le verifiche acciaio/legno; accertare se i dati prodotti dal FEM sono sufficienti a chiamare le regole normative nei casi pertinenti. Risultato: una matrice auditabile, utile a pianificare P0-B…P0-E senza inventare comportamenti 3D o nuove norme.

Gli esempi G8B e i dati numerici del libro Ballio–Mazzolani servono come **stimoli e scenari di applicabilità**. Non vanno dichiarati “verificati” soltanto perché i report vengono generati. Una verifica puntuale passa solo se input, metodo, geometria, norma, comb., stazione e ipotesi sono dimostrabili.

## Procedura in sei passi

### A1 — Inventario implementazione reale

Esaminare **`src/`, `tests/`, `benchmarks/`, `examples/`, `scripts/`**. Per ogni funzione annotare file/classe/metodo, configurazioni ammesse, precondizioni e test che ne esercitano il flusso. Controllare in particolare `Core`, `Analysis`, `Verification`, `Verification.Integration`, `Verification.Steel`, `Verification.Timber`, `Reporting` e `Cli`.

Per evitare false attribuzioni, distinguere fra: classe/contratto esistente; funzione effettivamente invocabile; test eseguito; benchmark numerico indipendente; validazione fisica e normativa. L'esistenza di una classe non vale come prova che il percorso CLI → FEM → verifica sia completo.

### A2 — Copertura del FEM e applicabilità fisica

Verificare e documentare:

- modello `Frame2D` Euler–Bernoulli e `Truss2D` (proprietà geometriche, orientamento, assi locali/globali, trasformazioni, segni);
- vincoli/rigidezze, release, meccanismi, matrici singolari, reazioni positive/negative e stabilità numerica;
- carichi nodali, puntuali/distribuiti/triangolari, `SelfWeight` e combinazioni manuali; pesi propri e convenzioni di unità;
- soluzioni statiche, campionamento dei diagrammi interni, limiti di discontinuità, effetti di elementi quasi orizzontali/verticali;
- convergenza e confronto di benchmark, casi molto piccoli/grandi, rotazioni, reazioni ed equilibrio;
- moduli modali/dinamici e secondo ordine *solo negli ambiti supportati*, non generalizzare risultati storici;
- esplicita assenza o presenza documentata di taglio deformativo Timoshenko, instabilità fuori piano, tension-only, sollevamento/contatto, collegamenti e diaframmi.

### A3 — Copertura normativa e interfaccia FEM→regole

Per **ciascun** insieme di verifiche distinguere il materiale e l'edizione: EC3 acciaio, EC5 legno, NTC2018 legno (G1–G6) e NTC2018 acciaio dove eventualmente supportato. Verificare regole pure, combinate, instabilità e SLE **senza extrapolare** da controlli locali a intere aste.

Eseguire un audit dei contratti di input: provenienza dei carichi, combinazione e tipo SLU/SLE, categoria di azione, temperatura/classe di servizio, materiali e resistenze, sezioni efficaci/nette, assi di verifica, ritegni fuori piano, lunghezze libere, coeff. ψ/γ e dati forniti dal tecnico. Controllare conservazione dell'identità della stazione, del lato e delle sollecitazioni **concomitanti** e modalità `Unsupported` quando mancano dati o supporto.

### A4 — Carichi, combinazioni e casi limite

Verificare chi seleziona le azioni guida, chi produce fattori ψ/γ, chi distingue favorevole/sfavorevole e quando il sistema ricade su combinazioni manuali. Cercare omissioni: vento di segno opposto, neve su falde asimmetriche, azioni nulle, fattori negativi, effetti combinati e azioni assenti/non pertinenti.

Non dichiarare complete le combinazioni NTC perché esiste `loadCombinations` nel JSON o il selettore coefficienti G1. Occorre separare validatore delle combinazioni da loro generatore automatico e definire il perimetro normativo esplicito.

### A5 — Confronto casi applicativi e benchmark

Usare almeno:

- **B13/B14** (reazioni e momenti dello sbalzo, incluso appoggio bilatero con reazione negativa);
- test G1→G2/G3→G6 (esiti parziali e audit, rifiuto di stazioni incongruenti);
- **G8B casetta** (4 modelli, 4 combinazioni, X bilaterale, SelfWeight, carichi, 16 report attesi); confrontare nei **medesimi nodi e carichi** con `benchmarks/independent/g8b-house/g8b-risultati-elastici-indipendenti.json`. Non usare solo residui di equilibrio come prova di accuratezza;
- casi di edge, meccanismi, quasi verticali/orizzontali, releases e combinazioni nulle/negative già in `benchmarks/edge-cases/`;
- esempi analitici/documentali dell'indice `benchmarks/G6_COVERAGE_REFERENCE_CROSSWALK.json`, promuovendo solo quelli con dati fisicamente completi.

### A6 — Rapporto di gap, ordinamento e decisione

Costruire un report leggibile e una matrice strutturata. Ogni riga deve descrivere **fenomeno + materiale + normativa/edizione + ambito FEM + test/evidenza + stato + conseguenza dell'assenza + priorità + correzione proposta + test di accettazione**. Evidenziare i rischi che potrebbero generare un falso `Passed` più di quelli che producono `Unsupported` esplicito.

## Stati della matrice (mai collassare categorie diverse)

| Stato | Definizione |
|---|---|
| `IMPLEMENTED_VALIDATED_SCOPE` | Implementato e coperto da prove specifiche in un ambito circoscritto |
| `IMPLEMENTED_PARTIAL` | Una parte è disponibile, ma stazioni, azioni, geometria o fenomeni non sono coperti completamente |
| `UNSUPPORTED` | Non implementato o senza dati/algoritmo sufficienti; non può produrre un `Passed` |
| `EXTERNAL_EVIDENCE_REQUIRED` | Dipende da ritegni, collegamenti, quote, certificati, contatti ecc. che il FEM non conosce |
| `PHYSICALLY_NOT_APPLICABLE` | Non pertinente **solo con giustificazione verificabile** nel contesto dichiarato |
| `NOT_YET_ASSESSED` | Non ancora ispezionato; non va confuso con `Passed` né con `Unsupported` |

Classificazione **non automatica**: lo stato deve essere motivato da riferimenti verificati. Le annotazioni `Passed` delle singole regole non sono stati di completezza globale.

## Output attesi da P0-A

1. `docs/structural/P0_A_AUDIT_TECNICO.md`: esito analitico, architettura ispezionata, riscontri puntuali e limiti.
2. `docs/structural/P0_A_MATRICE_COPERTURA.md` oppure JSON/CSV equivalente per consultazione/filtraggio, con *fonti testuali dei dati* e rischio.
3. `docs/structural/P0_A_PRIORITA_E_CRITERI_DI_TEST.md`: ticket concreti per P0-B/C/D/E, dipendenze e test indipendenti richiesti.
4. Aggiornamento **`HANDOFF_NUOVA_CHAT.md`** e **`CHANGELOG.md`**, e roadmap se cambiano le priorità.
5. Se P0-A introduce test automatici per verificare lacune, fornire nuovo script/gate e richiedere conferma Windows; non cambiare produzione senza una decisione motivata.

## Priorità iniziali per la lettura del codice (ipotesi da verificare)

1. **Falso esito di conformità**: stazione, lato, inviluppi non concomitanti, combinazioni incomplete, condizioni di stabilità ignote.
2. **Combinazioni e azioni**: categorie, ψ/γ, favorevoli/sfavorevoli, vento ±, neve, permanenti/variabili, pesi propri.
3. **Instabilità e collegamenti**: fuori piano, scorrimenti, giunti, appoggi monolateri, controventi in compressione/tensione.
4. **Interfaccia normative**: EC3/NTC acciaio e EC5/NTC legno, edizioni, stato del catalogo, assi e proprietà sezionali.
5. **Qualità dei dati e report**: unità, id, riutilizzo improprio di profili/fingerprint, dichiarazione delle assunzioni e report incompleti.

Questa è una **sequenza di ispezione**, non un giudizio già dimostrato di difettosità.

## Criteri di completamento P0-A

P0-A è pronto per essere considerato **concluso documentalmente** quando:

- le capacità invocate dalla roadmap e dal README sono state confrontate con codice e test, senza lasciare aree classificate erroneamente come `Passed`;
- ogni gap significativo ha evidenza/localizzazione, impatto, dipendenze e test di accettazione;
- tutti i casi in cui un risultato parziale potrebbe apparire come conformità globale sono chiaramente identificati;
- i task P0-B…P0-E hanno confini tecnici non ambigui e ordine motivato;
- `HANDOFF_NUOVA_CHAT.md`, `CHANGELOG.md`, roadmap e manifest risultano allineati;
- se non sono stati modificati sorgenti o test, **non** è richiesto dichiarare un nuovo gate .NET (solo integrità documentale); se modificati, occorre collaudo Windows.

**Il risultato di P0-A non è un certificato di dimensionamento della casetta e non autorizza ancora la GUI.**
