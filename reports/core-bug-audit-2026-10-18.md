# Ricerca bug in StructuralSolver2D.Core — 2026-10-18

## Esito delle correzioni

Corretti tutti i sei rilievi sotto elencati e il caso secondario dei riferimenti nodo nulli.
I dettagli e i valori numerici nelle sezioni successive descrivono il comportamento prima delle correzioni.

- Le reticolari paraboliche condividono gli estremi dei correnti e non contengono aste duplicate. Gli ID terminali sono `B0` e `B{panels}`; gli eventuali riferimenti precedenti a `T0`/`T{panels}` vanno aggiornati.
- La Gerber mantiene due cerniere interne e una campata sospesa; i vincoli esterni sono un incastro sinistro e un vincolo destro su `Uy`/`Rz`, con `Ux` libero.
- Le collisioni fra ID di casi e combinazioni vengono rifiutate con `LOAD_COMBINATION_ID_CONFLICT`, anche se differiscono soltanto per maiuscole/minuscole.
- Il catalogo ammette inerzia zero. Il validatore la permette per sezioni assiali, richiede inerzia positiva per Frame2D e rifiuta inerzia negativa o non finita per tutte le sezioni.
- Tutti gli ingressi dimensionali dei generatori rifiutano valori non positivi o non finiti.
- Il validatore segnala `MEMBER_INVALID_TYPE` per enum inesistenti e restituisce issue per riferimenti nodo nulli.

Aggiunti **127 casi di test**: topologia dei generatori, pannellature pari e dispari, equilibrio nodale, reazioni e momenti analitici della Gerber, freccia analitica mediante lavori virtuali, diagrammi dei casi/combinazioni, catalogo JSON, dimensioni non valide, enum numerici da JSON e riferimenti nulli.

Prima delle correzioni, la prima esecuzione dei nuovi test Core/Analysis dava 71 fallimenti e 54 successi, confermando la sensibilità ai difetti. Dopo le correzioni, il comando `dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal` termina senza fallimenti né avvisi di compilazione:

| Progetto | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 192 | 0 |
| Analysis.Tests | 140 | 0 |
| Reporting.Tests | 39 | 0 |
| Cli.Tests | 7 | 0 |
| Totale | 378 | 0 |

Test persistenti aggiunti:

- `tests/StructuralSolver2D.Core.Tests/Validation/CoreModelRegressionTests.cs`
- `tests/StructuralSolver2D.Core.Tests/Model/Generators/ParametricGeneratorRegressionTests.cs`
- `tests/StructuralSolver2D.Analysis.Tests/Frame2D/CoreGeneratorAnalysisRegressionTests.cs`
- due casi nel file esistente `tests/StructuralSolver2D.Cli.Tests/Input/StructuralModelJsonReaderTests.cs`

Aggiornati anche changelog e documentazione di generatori, catalogo e input JSON. Non sono state modificate le implementazioni degli analyzer o il solver numerico: le correzioni riguardano i dati generati e la loro validazione.

## Rilievi iniziali

Revisione di modello, validazione, generatori, factory, libreria materiali e catalogo sezioni. Le dipendenze in Analysis sono state consultate ed eseguite per verificare gli effetti dei dati prodotti da Core.

Risultato: sei rilievi riprodotti. P1 indica risultati errati o modelli instabili con possibili risultati apparentemente riusciti; P2 indica una funzionalità inutilizzabile o un'incompatibilità; P3 indica una lacuna di controllo intercettata successivamente.

## 1. P1 — Estremi scollegati nei generatori parabolici

**Codice:** `src/StructuralSolver2D.Core/Model/Generators/ParametricTrussGenerator.cs`, righe 141, 167–168 e 222–225.

`NielsenParabolicTruss` e `InvertedParabolicTruss` creano due nodi distinti e coincidenti a ciascuna estremità: `B0`/`T0` e `B{panels}`/`T{panels}`. La parabola ha quota zero agli estremi, ma i nodi non vengono unificati; le verticali terminali vengono escluse e i vincoli sono applicati soltanto ai nodi B. La coincidenza geometrica non impone continuità degli spostamenti. Il modello conserva modi di meccanismo.

**Riproduzione:** `InvertedParabolicTruss(10, 2, 4)`, aggiungere una forza nodale `GlobalY = -10` su `B2` e chiamare `Truss2DAnalyzer.Analyze(model, "LC1")`.

- Validazione: `IsValid = true`, nessuna issue.
- Risultato osservato: analisi completata, `max(abs(Uy)) = 53_687_091_200 m`.
- Con sei pannelli, entrambi i generatori lanciano un'eccezione per matrice singolare.
- Prova di controllo: unificando gli estremi nel modello Nielsen a sei pannelli, lo stesso carico dà `max(abs(Uy)) ≈ 9.96218e-5 m`.

**Intervento proposto:** usare un unico nodo strutturale per ciascun estremo comune e controllare le aste duplicate dopo l'unificazione. Aggiungere test di analisi con carichi per entrambi i generatori, inclusi pannelli pari e dispari. Il fatto che il solver accetti alcune matrici singolari dipende anche dal controllo numerico dei pivot in Analysis; resta distinto dal difetto di topologia in Core.

## 2. P1 — Collisione tra ID di caso e combinazione: diagrammi errati

**Codice:** `src/StructuralSolver2D.Core/Validation/StructuralModelValidator.cs`, righe 48–50 e metodo `ValidateLoadCombinations` da riga 338.

Il validatore controlla separatamente i duplicati nei casi e nelle combinazioni, consentendo che le due categorie condividano un ID, anche con differenze di maiuscole. Il post-processing ricostruisce i fattori cercando prima una combinazione con l'ID del risultato (`Frame2DInternalForceSampler.BuildLoadCaseFactors`, riga 189). Un risultato del caso singolo viene quindi interpretato come combinazione.

**Riproduzione:** trave semplicemente appoggiata di 5 m, caso `LC1`, carico uniforme `-10 kN/m`; aggiungere una combinazione `lc1 = 2 × LC1`. Analizzare il caso singolo `LC1` e campionare `M1`.

- Validazione: `IsValid = true`.
- Somma delle reazioni verticali: `50 kN`, coerente con il caso singolo.
- Momento campionato a metà trave: `0 kNm`, invece di `31.25 kNm`.
- Momento campionato all'appoggio destro: `-125 kNm`, invece di circa zero.

**Intervento proposto:** impedire la collisione tra ID dei casi e delle combinazioni con confronto `OrdinalIgnoreCase`, oppure portare il tipo di analisi nei risultati e utilizzarlo in tutti i consumatori. Aggiungere un test che attraversi validazione, analisi e post-processing.

## 3. P2 — Il generatore Gerber produce un meccanismo

**Codice:** `src/StructuralSolver2D.Core/Model/Generators/ParametricBeamGenerator.cs`, righe 74–79.

Tre aste consecutive, con due cerniere interne, sono sostenute soltanto da una cerniera a sinistra e un carrello a destra. Mancano i vincoli necessari a stabilizzare le campate esterne e quella sospesa.

**Riproduzione:** `GerberBeamWithAsymmetricLoads(4, 3, 5)` seguito da `new Frame2DAnalyzer().Analyze(model, "LC1")`.

- Validazione: `IsValid = true`.
- Analisi: `StructuralAnalysisException`, matrice singolare al pivot 4.
- Il test esistente verifica conteggi, rilasci e validazione, senza eseguire l'analisi. Gli esempi di analisi costruiscono invece una Gerber diversa, con più vincoli, senza utilizzare questo generatore.

**Intervento proposto:** definire uno schema Gerber stabile con vincoli e geometria coerenti con la campata sospesa. Verificare reazioni, equilibrio e momenti alle cerniere mediante un test sul modello generato.

## 4. P2 — Le sezioni valide per reticolari non possono entrare nel catalogo

**Codice:** `src/StructuralSolver2D.Core/Model/Sections/StructuralSectionCatalog.cs`, riga 167; `src/StructuralSolver2D.Core/Validation/StructuralModelValidator.cs`, righe 156–163.

Il commit `04d3d37` ha reso valida una sezione con inerzia zero quando usata soltanto da elementi Truss2D. Il catalogo continua a richiedere sempre inerzia strettamente positiva.

**Riproduzione:** creare `IsostaticTriangularTruss(6, 2)`, sostituire la sua sezione con una copia avente `MomentOfInertia = 0`, validare e chiamare `StructuralSectionCatalog.FromSections(model.Sections)`.

- Il modello è valido.
- La creazione del catalogo lancia `ArgumentOutOfRangeException` su `MomentOfInertia`.
- La stessa restrizione impedisce di caricare dal formato JSON del catalogo una sezione di quel tipo.

È un'incompatibilità tra il comportamento introdotto nel validatore e quello del catalogo, la cui documentazione continua a richiedere inerzia positiva; occorre allineare esplicitamente il contratto.

**Intervento proposto:** consentire nel catalogo inerzia zero per sezioni utilizzabili nelle reticolari, mantenendo il controllo di inerzia positiva quando assegnate a Frame2D. Aggiungere un test di salvataggio/caricamento di una sezione per reticolare.

## 5. P3 — NaN e infinito superano i controlli dimensionali dei generatori

**Codice:** metodi `EnsurePositive` in `ParametricBeamGenerator.cs:109`, `ParametricFrameGenerator.cs:51` e `ParametricTrussGenerator.cs:232`.

Il controllo `value <= 0.0` non esclude `double.NaN` e `double.PositiveInfinity`.

**Riproduzioni:** `SimplySupportedBeam(double.NaN, 2)`, `PortalFrame(6, double.PositiveInfinity)` e `PrattBridge(10, double.NaN, 4)` restituiscono modelli invece di rifiutare gli argomenti. Verificati entrambi i valori per tutte e tre le famiglie di generatori.

Il validatore intercetta successivamente le coordinate non finite, quindi il problema non implica da solo un risultato numerico errato dal percorso ordinario di analisi. Tuttavia il generatore restituisce dati invalidi al chiamante.

**Intervento proposto:** aggiungere `!double.IsFinite(value)` a tutti e tre i controlli e testare NaN e infinito.

## 6. P3 — Il validatore accetta MemberType non definiti

**Codice:** `src/StructuralSolver2D.Core/Validation/StructuralModelValidator.cs`, metodo `ValidateMembers`, da riga 168.

I valori enum dei carichi vengono verificati con `Enum.IsDefined`; il tipo delle aste non riceve lo stesso controllo.

**Riproduzione:** creare `SimplySupportedBeam(5, 1)` e sostituire l'asta con `member with { Type = (MemberType)999 }`.

- Validazione: `IsValid = true`, nessuna issue.
- Gli analyzer rifiutano poi il tipo non supportato: il difetto riguarda la validazione, senza bypass del controllo successivo.

**Intervento proposto:** emettere un errore `MEMBER_INVALID_TYPE` per valori enum non definiti. Verificare anche l'ingresso JSON con enum numerico.

## Verifiche eseguite e limiti

Comando: `dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal`.

| Progetto | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 88 | 0 |
| Analysis.Tests | 119 | 0 |
| Reporting.Tests | 39 | 0 |
| Cli.Tests | 5 | 0 |
| Totale | 251 | 0 |

Per la ricerca iniziale è stato usato un progetto diagnostico locale ignorato da Git, `out/CoreBugAudit/CoreBugAudit.csproj`. Le riproduzioni sono state successivamente sostituite dai test di regressione persistenti elencati all'inizio del rapporto.

Ulteriore osservazione di robustezza, esclusa dai sei rilievi principali: passando esplicitamente `StartNodeId = null` tramite API .NET, il validatore lancia `ArgumentNullException` su `Dictionary.TryGetValue` invece di restituire una issue. Il parametro è dichiarato non nullable e il reader JSON della CLI lo controlla già.

I due generatori difettosi risalgono al commit `5577738` (M40); l'incompatibilità sulle sezioni emerge dopo `04d3d37`. La revisione non dimostra l'assenza di ulteriori difetti.
