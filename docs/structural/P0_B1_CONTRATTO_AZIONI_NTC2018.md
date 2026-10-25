# P0-B1 — Contratto tecnico per azioni e combinazioni NTC 2018

**25 ottobre 2026 — SPECIFICA E GOLDEN ARITMETICI; NON IMPLEMENTAZIONE DEL GENERATORE.**

Documento applicativo derivato dal P0-A. Il motore e i modelli G8B restano invariati. È deliberatamente distinto fra **coefficienti normativi controllati sulla fonte primaria**, **scelta ingegneristica delle situazioni di carico**, **combinazione di casi FEM** e **verifica di una struttura**. Non sono sinonimi.

## Fonti e campo di applicazione

- [D.M. 17 gennaio 2018, NTC 2018, testo integrale in Gazzetta Ufficiale](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf): § 2.5.1 azioni; § 2.5.2 caratteristiche; **Tab. 2.5.I** (pagine editoriali 39–40); **§ 2.5.3, equazioni [2.5.1]–[2.5.4]** (pagina 40); **§ 2.6.1 e Tab. 2.6.I** (pagina 41).
- [Circolare 21 gennaio 2019, n. 7, pubblicata l'11 febbraio 2019](https://www.gazzettaufficiale.it/eli/gu/2019/02/11/35/so/5/sg/pdf): commento applicativo, non sostituisce il D.M.
- Il profilo **iniziale** è `NTC2018:2018-01-17:BUILDING:STR:A1:STATIC_NON_SEISMIC:v1`: elementi strutturali **senza azioni di tipo geotecnico** in **edifici civili/industriali correnti**. La tabella A1 *non* autorizza da sola verifiche EQU, GEO, plinti, muri, ponti, elementi con altre regole del capitolo 4, sisma o incendio. Gestire separatamente la situazione di progetto e l'applicabilità delle norme speciali.
- La generazione di un inviluppo richiede ipotesi motivate su presenza, alternatività e disposizione di ogni azione. Questo profilo non ricava autonomamente neve, vento, traffico, peso proprio o coefficienti aerodinamici; assume **casi caratteristici FEM già definiti e verificati**. Non genera carichi eccezionali/sismici né combinazioni per fatica.

## Tabelle di riferimento, versione bloccata

Per le situazioni **SLU STR A1** pertinenti alla specifica, dalla Tab. 2.6.I:

| Natura | γ favorevole | γ sfavorevole | Ulteriori condizioni |
|---|---:|---:|---|
| Permanente strutturale `G1` | 1,00 | 1,30 | Non dedurre favorevolezza dal segno globale Y |
| Permanente non strutturale `G2` | 0,80 | 1,50 | Distinguere omissione fisicamente giustificata; deroga G2 trattabile come G1 solo con evidenza esplicita |
| Variabile `Q` | **0,00 = azione omessa** | 1,50 | Omissione se favorevole all'effetto verificato; non serializzare il termine zero nel modello FEM corrente |

Tab. 2.5.I per **edifici civili e industriali correnti**:

| Categoria variabile | ψ₀ | ψ₁ | ψ₂ |
|---|---:|---:|---:|
| A, residenziale | 0,7 | 0,5 | 0,3 |
| B, uffici | 0,7 | 0,5 | 0,3 |
| C, affollamento | 0,7 | 0,7 | 0,6 |
| D, commerciale | 0,7 | 0,7 | 0,6 |
| E, archivi/magazzini/industria | 1,0 | 0,9 | 0,8 |
| F, parcheggi/traffico auto ≤ 30 kN | 0,7 | 0,7 | 0,6 |
| G, parcheggi/traffico auto > 30 kN | 0,7 | 0,5 | 0,3 |
| H, coperture per sola manutenzione | 0,0 | 0,0 | 0,0 |
| Vento | 0,6 | 0,2 | 0,0 |
| Neve, quota ≤ 1.000 m | 0,5 | 0,2 | 0,0 |
| Neve, quota > 1.000 m | 0,7 | 0,5 | 0,2 |
| Variazioni termiche | 0,6 | 0,5 | 0,0 |

Le categorie **I** e **K** sono da valutare *caso per caso*: **nessuna riga automatica ψ**, diagnosi `NeedsEngineeringInput`/`Unsupported`. La categoria H ha ψ=0 soltanto da **accompagnatrice** o nel valore frequente/quasi-permanente; **come guida rara/SLU la variabile usa il suo valore caratteristico**, se pertinente. **Non** confondere la classificazione delle destinazioni d'uso con il tipo di membro.

## Famiglie consentite e fattori effettivi

Ogni `LoadCaseId` riporta un valore **caratteristico** e tutti i suoi carichi sono firmati nelle coordinate del modello. Fattore effettivo = `γ × ψ`, senza invertire il segno originale. `ψ` vale 1 per azione guida SLU/rara, `ψ1` per guida frequente, `ψ2` per tutte le variabili quasi-permanenti. I permanenti in SLE hanno fattore 1, salvo un'omissione **esplicitamente motivata di G2**.

| Famiglia | Permanenti | Variabile guida Q₁ | Variabili concomitanti Qⱼ |
|---|---|---|---|
| `ULS_STR_A1` [2.5.1] | γG1/γG2 in base all'effetto | 1,50 se sfavorevole; altrimenti omessa | 1,50·ψ₀ⱼ se sfavorevole; altrimenti omessa |
| `SLS_RARE` [2.5.2] | 1,00 | 1,00 se sfavorevole; altrimenti omessa | ψ₀ⱼ se sfavorevole; altrimenti omessa |
| `SLS_FREQUENT` [2.5.3] | 1,00 | ψ₁₁ se sfavorevole; altrimenti omessa | ψ₂ⱼ se sfavorevole; altrimenti omessa |
| `SLS_QUASI_PERMANENT` [2.5.4] | 1,00 | nessuna guida privilegiata | ψ₂ⱼ per ogni azione sfavorevole; altrimenti omessa |

**Nota sostanziale:** per una struttura o una singola asta i fattori *favorevoli* possono cambiare a seconda dell'effetto (sollevamento vincolo, momento positivo o negativo, compressione, taglio, spostamento, ecc.). Né il segno positivo del `value` JSON né il verso del carico determinano a priori tale giudizio per **tutti** gli effetti. Nella versione iniziale, il generatore produce i **candidati** e conserva la scelta motivata del contesto di verifica; un audit di completezza impedisce di dichiarare esaustivo un insieme di combinazioni privo delle alternative applicabili. Non scegliere automaticamente il coefficiente più gravoso per **una sola** grandezza globale quando il report comprende molti effetti.

Per `Q` favorevole e per `ψ=0` il manifesto normativo include un record **omesso** con motivazione (`Favorable`, `PsiZero`, `MutuallyExclusive`, `NotPresentByEvidence`) ma **non** emette `StructuralLoadCombinationTerm(Factor=0)`: il validatore attuale in `Core/Validation/StructuralModelValidator.cs` lo respinge. Un coefficiente negativo **manuale** già accettato dal Core rimane supportato e non deve essere reinterpretato come `γ` normativo. La migrazione a termini-zero sarà un ticket specifico, non una modifica opportunistica al contratto esistente.

## Contratto dati proposto (per P0-B2; non API già disponibile)

Separazione: `StructuralSolver2D.Core` continua a descrivere **solo casi, carichi e combinazioni manuali**; un modulo di dominio `StructuralSolver2D.Combinations` potrà dipendere dal Core e fornire generatori, profili, audit ed esportazione, **mai il contrario**. `Analysis`, i golden G7 e i quattro input G8B **non cambiano**. In particolare non cambiare il significato dei vecchi `ULS_*`/`SLS_*`: sono identificatori manuali.

| Oggetto *proposto* | Campi minimi | Invariante/errore |
|---|---|---|
| `ActionDefinition` | `ActionId`, `LoadCaseId`, `Kind` G1/G2/Q, `VariableCategory` se Q, `Basis=Characteristic`, `EvidenceId`, `DurationClass` per legno quando necessaria, `ScenarioGroupId` | Identità distinta da nome leggibile; un caso FEM non può ricoprire azioni con **fattori differenti** senza prima essere suddiviso |
| `ActionAlternativeGroup` | Id, casi mutuamente esclusivi, condizioni di coesistenza, motivazione | `wind+` e `wind-` non sono sommabili, ma entrambi possono governare; stessi gruppi non assumono automaticamente simultaneità |
| `CombinationRequest` | Profilo/edizione, situazione STR o SLE, famiglia, insieme azioni, osservabile/contesto fisico, politiche omissione, lista scelte guida | Rifiutare profili misti, tipo ignoto, categoria I/K senza parametri e mancanza di fonte |
| `CombinationManifest` | Id generato, famiglia, guida eventuale, termini emessi, `OmittedAction[]` con ragione, γ/ψ usati e origine, fingerprint dei **termini FEM effettivi** | Uno zero omesso non può risultare indistinguibile da un caso assente; checksum calcolato sull'input vero, non su un commento |
| `CombinationAudit` | set combinazioni richiesto/ottenuto, alternative, gruppi, per-fattore provenance, `Complete/Incomplete/Invalid/Unsupported` | Mutazione del fattore di una combinazione FEM → mismatch, mai successo silenzioso |

Scelte di progettazione obbligatorie prima del codice: (1) come rappresentare *l'effetto da massimizzare* e stabilire γ favorevoli/sfavorevoli; (2) limite combinatorio e riduzione solo con prova di dominanza per tutte le quantità considerate; (3) due carichi di caratteristiche diverse sotto lo **stesso** `LoadCaseId` devono essere scissi; (4) azioni distribuite su parti diverse o con pattern diversi vanno definite come scenari distinti; (5) serializzatore JSON e CLI legacy devono mantenere la stessa semantica.

## Politica di validazione e copertura

1. **Validare l'inventario prima del calcolo:** `LoadCaseId` realmente presente nel `StructuralModel`; fattori unitari caratteristici; evidenza per natura/categoria/altitudine neve/G2; azioni eterogenee non miscelate nel caso.
2. **Enumerare l'insieme ammissibile** prima di applicare eventuali deduplicazioni. Generare guida per ciascuna variabile che può governare in SLU/rara/frequente e alternativa di segno/posizione compatibile. Per quasi permanente non esiste guida unica. Dichiarare gli assenti e le esclusioni.
3. **Calcolare per effetto:** stazioni e componenti ammissibili, incluse quelle dove favorevolezza cambia. Un inviluppo di soli fattori scalari non garantisce automaticamente che tutte le verifiche di membratura siano incluse (P0-C).
4. **Registrare il risultato:** versione della tabella, `loadCaseId`, categoria, guida/accompagnatrice, γ, ψ, fattore finale, omissione, scenario di compatibilità, combinazione effettiva serializzata, fingerprint.
5. **Separare gli esiti:** `ArithmeticVerified` non è `CombinationSetComplete`; questo non è `MemberChecked`; questo non è `BuildingCompliant`.

## Golden indipendenti P0-B1

Il dataset in `benchmarks/independent/p0b/P0B1_NTC2018_ACTION_GOLDEN.json` contiene coefficienti e **14 casi calcolati a mano**; `verify_p0b1_golden.py` ricalcola l'aritmetica con `Decimal`, controlla i fattori attesi e verifica le esclusioni. Sono esempi **di un singolo effetto scalare lineare**, non benchmark di sollecitazioni FEM e non certificano la completezza di un modello reale.

Provare obbligatoriamente, oltre ai casi positivi, fattore γ alterato, ψ alterato, guida sbagliata, aggiunta di termini zero, omissione non spiegata, `wind+` e `wind-` presenti insieme, doppio `LoadCaseId`, categoria I/K, neve senza fascia altimetrica ed effetto senza contesto per giudicare favorevolezza. Una prova deve fallire con diagnosi precisa; i test G7 invariati sono il controllo di non regressione.

### Gate e piano di rilascio

| Milestone | Prodotto | Stato 25/10/2026 |
|---|---|---|
| **P0-B1** | Fonte normativa, contratto, tabelle e golden aritmetici indipendenti | **Documentale + golden verificabili in Python**. Nessuna feature C# |
| **P0-B2** | `ActionDefinition` e validatore isolati, serializer/round-trip, test xUnit negativi | **Da implementare** |
| **P0-B3** | Combinazioni STR A1 e SLE + manifesto e algoritmo di scenari | **Da implementare** |
| **P0-B4** | Audit completezza e match con il vero modello FEM, CLI opt-in, test integrati + suite G7 Windows | **Da implementare; solo allora gate P0-B** |

**Limiti dichiarati:** nessuna generazione automatica attuale; nessuna verifica di edificio/collegamento/fondazione; nessun algoritmo 3D, geometrico non lineare o tension-only. I test .NET non sono stati eseguiti in questa milestone (SDK non presente nell'ambiente di preparazione). Avalonia resta in P3.
