# StructuralSolver2D — Regole vincolanti per sviluppo e validazione

Decisioni confermate dall'autrice il **25 ottobre 2026**, checkpoint documentale **P0-F0**. Questo documento governa i nuovi incrementi, senza cambiare retroattivamente il significato del codice già collaudato.

## Linguaggio e strumenti di progetto

- **Tutto il nuovo codice produttivo è C#/.NET.** Core, FEM, combinazioni, cataloghi, verifica, CLI, reporting e futura GUI rimangono progetti con responsabilità separate.
- **Tutti i nuovi test automatici sono C#** (xUnit, `StructuralSolver2D.*.Tests` o appositi progetti test .NET) e **ogni nuovo programma di servizio, benchmark eseguibile, runner, generatore e script di automazione va realizzato in C#/.NET**. Preferire un piccolo tool `dotnet run --project ...` per l'esecuzione locale/CI.
- **Non introdurre, ampliare o eseguire nuovi script Python.** I benchmark Python contenuti nei pacchetti storici sono preservati esclusivamente per provenienza, risultati già registrati e riproducibilità archivistica. **Non usarli come dipendenza di build, test, runtime o futuro gate**.
- Gli script PowerShell già esistenti restano compatibilità storica e possono essere progressivamente sostituiti da runner **C#** quando si tocca il flusso; **non aggiungere nuovi script PowerShell, Bash o Python come componenti del progetto**. Le operazioni di confezionamento del rilascio sono attività infrastrutturali e non costituiscono nuovi programmi applicativi.
- La GUI ultima fase **P3** utilizzerà **C#/Avalonia**, senza formule normative duplicate e senza dipendere da Python.

## Indipendenza della validazione, pur usando sempre C#

La richiesta di usare C# non elimina il requisito di **controllo indipendente**. Un test che chiama il FEM di produzione una seconda volta e confronta le due risposte **non** è un golden indipendente.

Metodo per nuovi benchmark:

1. Redigere l'esempio e l'atteso **prima** della modifica all'algoritmo. Fonti ammesse: formule manuali documentate, soluzioni chiuse, letteratura tecnica con condizioni complete, risultati indipendenti già congelati e verificati, o una piccola implementazione analitica in **C# che non riusi né importi il motore testato**.
2. Conservare i numeri attesi in dati strutturati versionati (per esempio JSON) e indicare unità, segni, geometria, condizioni, precisione e fonte.
3. Scrivere il validatore/confrontatore **in C#**, preferibilmente xUnit, con tolleranze assolute/relative stabilite prima della prima esecuzione. Non scegliere le tolleranze per cancellare differenze inattese.
4. Inserire sempre **test negativi**: perturbare un segno, una release, una combinazione o una proprietà del materiale deve far fallire il controllo.
5. Eseguire build e test .NET del progetto su Windows o in CI; distinguere un **gate realmente eseguito** da un calcolo manuale o da un risultato storico.

**Prima applicazione: F1.1 G8B.** I risultati numerici storici generati in passato da un riferimento Python possono essere letti **come dati statici di provenienza**, senza avviare Python. Per i valori mancanti N/V/M occorrono nuovi attesi indipendenti derivati a mano o attraverso metodo C# separato; confronto automatico di tutti i 16 scenari solo in C#/.NET. Non adattare l'oracolo ai risultati del FEM.

## Norme e versioni: mai mescolare NTC/EC3/EC5

| Famiglia | Modulo/identità | Politica |
|---|---|---|
| **NTC 2018** | D.M. 17 gennaio 2018, disposizioni italiane, tabelle e coefficienti nazionali, Circolare applicativa 7/2019 come riferimento interpretativo | Regole native **NTC**, non alias delle classi e capacità EC3/EC5 |
| **EC3** | EN 1993-1-1, edizione/annesso espliciti già definiti nel codice | Verifiche acciaio separate dalle NTC, anche quando un risultato numerico coincide |
| **EC5** | EN 1995-1-1, edizione/annesso espliciti già definiti nel codice | Verifiche legno EC5 separate dai moduli NTC G1–G7 |
| **Futuri Eurocodici di seconda generazione** | Nuovi profili con nuova edizione e annesso nazionale | **Nuove regole/versioni**, senza mutare silenziosamente fattori o formule dei profili precedenti |

Ogni `DesignCheckRequest` deve portare codice, edizione, eventuale annesso, coefficienti espliciti, materiale, stato limite, origine FEM, membro, combinazione, stazione e lato. Un cambio da EC3 a NTC, o fra edizioni, richiede **selezione esplicita e rivalidazione**. Nessuna equivalenza è dedotta dall'uguaglianza di una formula.

Fonti primarie di riferimento:

- [D.M. 17 gennaio 2018, NTC 2018 — Gazzetta Ufficiale](https://www.gazzettaufficiale.it/eli/id/2018/02/20/18A00716/sg).
- [Circolare applicativa 21 gennaio 2019 n. 7 — Gazzetta Ufficiale](https://www.gazzettaufficiale.it/eli/id/2019/02/11/19A00855/sg).
- [JRC / Commissione europea — Eurocodici seconda generazione](https://eurocodes.jrc.ec.europa.eu/second-generation-eurocodes), con transizione nazionale da seguire e annessi da acquisire; non assumere equivalenza fra edizioni.

## Stati e responsabilità dei risultati

- `Passed` di una **regola locale** non è membro approvato. `DesignOverallStatus.Passed` si riferisce soltanto alle regole richieste.
- `Failed` osservato deve essere mantenuto anche in presenza di dati mancanti su altre regole.
- `MissingData`, `Unsupported` e `NotApplicable` sono semanticamente distinti; non si elimina un obbligo perché manca il suo algoritmo.
- Esclusioni fisiche richiedono fatti/attestazioni esterni riferiti al caso; **un attestato descrittivo non diventa una certificazione materiale**.
- Spazi 3D, ritegni, connessioni, tension-only, appoggi monolateri, dettagli di fornitura, sismica e altre obbligazioni fuori modello vanno dichiarati o esclusi con motivo, mai inferiti dal solo FEM piano.
- Cataloghi OPPO: dati utili per studio e pre-dimensionamento **non certificati**. Classi 4, fori e capacità efficaci vanno trattati da regole specifiche.

## Ordine e gate per ogni checkpoint

1. **Prima:** validazione indipendente e chiusura dei rischi FEM/combination/stazione/copertura secondo [roadmap](roadmap.md). **Poi:** nuove verifiche normative giustificate. **Ultima:** GUI.
2. Motivazione e ambito del cambiamento, formula/algoritmo, file interessati; input e output con unità, assi, convenzioni e provenienza.
3. Fonte indipendente **non** ottenuta richiamando l'algoritmo modificato, casi di frontiera e casi negativi.
4. C# build/test dell'intera soluzione. In assenza di SDK, registrare **PENDING**; su conferma dell'autrice, registrare **PASS dichiarato senza log** senza inventare conteggi.
5. Handoff, [stato corrente](CURRENT_STATE.md), roadmap quando cambia la priorità, changelog e manifest aggiornati; uno ZIP FULL immutabile per checkpoint, così la versione storica può essere ricostruita.
6. README destinato soltanto a **cosa fa il software, requisiti, comandi d'uso e link essenziali**; niente diario delle milestone o dettagli di roadmap nel README.

## Limitazioni permanenti di prodotto

Solo **FEM 2D** per travi/reticolari/telai di acciaio e legno. Esclusi c.a., muratura, X-LAM, pareti, piastre, BIM, 3D, progettazione esecutiva dei collegamenti e certificazione automatica dell'opera. Metodo semiprobabilistico principale; eventuale tensione ammissibile come modulo successivo indipendente, solo dopo decisione e validazione dedicata.
