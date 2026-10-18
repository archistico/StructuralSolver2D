# Seconda ricerca bug in Core — 2026-10-18

Base esaminata: `26b466f` (`report`). Revisione di modello, validatore, generatori, factory delle sezioni, catalogo JSON e libreria materiali. Consultati CLI, Analysis e Reporting per verificare gli effetti dei dati prodotti da Core.

**Esito delle correzioni: risolti tutti i sette rilievi, con 88 nuovi casi Core e quattro CLI. Suite completa: 866 test superati, zero fallimenti.** I rilievi iniziali erano due P2 e cinque P3; i dettagli nelle sezioni successive descrivono la base prima delle correzioni. La questione della variante Pratt resta un'osservazione separata dal perimetro dei sette bug.

## Correzioni e verifica finale

| Caso | Correzione | Copertura aggiunta |
|---|---|---|
| 1 | DTO di lettura con numeri nullable; area e inerzia mancanti/nulli sono rifiutati con percorso del campo. Zero esplicito e formato serializzato restano supportati. | Inerzia assente, refuso, null, zero e valore positivo con round-trip e nomi di proprietà senza distinzione di maiuscole. |
| 2 | Controlli di altezza/larghezza opzionali allineati al catalogo, con `SECTION_INVALID_HEIGHT` e `SECTION_INVALID_WIDTH`. | Entrambe le proprietà con null, positivo, zero, negativo, NaN e infiniti; rifiuto anche dal processo CLI. |
| 3 | Guardie nei metodi Add e nel costruttore dei termini; il validatore segnala null con collezione/indice e continua su una copia locale filtrata. | Nove contenitori, inserimento in testa/coda, ulteriori errori rilevati e liste originali non mutate; otto metodi Add e costruttore della combinazione. |
| 4 | Controllo `SupportType`, senza alterare i flag dei vincoli. | Due enum invalidi anche da JSON/CLI, tutti i sei valori definiti e preservazione dei vincoli espliciti. |
| 5 | Norma riscalata e issue `MEMBER_INVALID_LENGTH` per lunghezze non finite. | Norma 3–4–5 a tre scale, tolleranza molto piccola e differenza di coordinate non rappresentabile. |
| 6 | Prodotti di mantisse normalizzate/esponenti; formule cave fattorizzate; proprietà calcolate positive e finite o eccezione esplicita. | Rettangolo, legno, cerchio pieno e cavo con intermedi fuori intervallo; scale miste, risultati subnormali, parete di un ULP, area/inerzia non rappresentabili. |
| 7 | Interpolazione normalizzata, ordinata parabolica con fattore limitato a [0,1], controllo di nodi distinti e accumuli Gerber. | Cinque generatori con coordinate grandi e finite, somma fuori intervallo/campata persa per arrotondamento e nodi collassati. |

Test persistenti aggiunti:

- [SecondCoreValidationRegressionTests.cs](../tests/StructuralSolver2D.Core.Tests/Validation/SecondCoreValidationRegressionTests.cs).
- [SecondCoreCatalogInputTests.cs](../tests/StructuralSolver2D.Core.Tests/Model/Sections/SecondCoreCatalogInputTests.cs).
- [SecondCoreNumericRegressionTests.cs](../tests/StructuralSolver2D.Core.Tests/Model/SecondCoreNumericRegressionTests.cs).
- [SecondCoreGeneratorRegressionTests.cs](../tests/StructuralSolver2D.Core.Tests/Model/Generators/SecondCoreGeneratorRegressionTests.cs).
- [SecondCoreInputRegressionTests.cs](../tests/StructuralSolver2D.Cli.Tests/SecondCoreInputRegressionTests.cs).

Comando finale:

```powershell
dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal
```

| Suite finale | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 280 | 0 |
| Analysis.Tests | 245 | 0 |
| Reporting.Tests | 160 | 0 |
| Cli.Tests | 181 | 0 |
| Totale | 866 | 0 |

[Log completo delle verifiche](../out/CoreSecondAudit/all-fix-tests.log). I casi numerici estremi verificano il contratto di Core; non estendono implicitamente il dominio di applicabilità numerica degli analyzer. I nuovi rifiuti sono controlli intenzionali su dati malformati o proprietà/geometrie non rappresentabili.

## Ricerca iniziale

La fase di sola ricerca non modificava sorgenti applicativi o test persistenti. Le riproduzioni iniziali sono in `out/CoreSecondAudit/`, ignorato da Git. I sei problemi già corretti nella prima ricerca non sono ricontati; la suite esistente continua a passare. Le sezioni sottostanti conservano gli esiti prima degli interventi.

## 1. P2 — Nel catalogo JSON un'inerzia omessa diventa zero

**Codice:** [StructuralSectionCatalogJsonSerializer.cs](../src/StructuralSolver2D.Core/Model/Sections/StructuralSectionCatalogJsonSerializer.cs), righe 50–66; [StructuralSection.cs](../src/StructuralSolver2D.Core/Model/StructuralSection.cs), righe 13–17; [StructuralSectionCatalog.cs](../src/StructuralSolver2D.Core/Model/Sections/StructuralSectionCatalog.cs), righe 166–171.

La deserializzazione costruisce direttamente `StructuralSection`, senza distinguere un numero mancante da uno zero dichiarato. Dopo l'ammissione dell'inerzia zero per sezioni assiali, il catalogo accetta anche documenti privi della proprietà `momentOfInertia`.

Riproduzione:

```json
{"schemaVersion":1,"sections":[{"id":"SEC1","name":"Section","area":0.01}]}
```

- `Deserialize` termina con successo e restituisce `MomentOfInertia = 0`.
- Con il refuso `"momentOfInteria": 0.00002` si ottiene lo stesso zero, perdendo il valore fornito.
- Serializzando nuovamente, il documento contiene esplicitamente `"momentOfInertia": 0`: l'omissione non è più riconoscibile.
- Il controllo con `"momentOfInertia": 0` intenzionale viene accettato correttamente e deve continuare a esserlo.

È una regressione del controllo d'ingresso emersa con `63e5811`: prima, `ValidatePositiveFinite` sull'inerzia rifiutava anche lo zero prodotto dall'omissione. La correzione che permette sezioni assiali è corretta; manca il controllo della presenza del campo JSON. Le sezioni Frame2D a inerzia zero sono comunque rifiutate dal validatore del modello: non è stato riprodotto un calcolo flessionale riuscito con rigidezza nulla.

**Intervento proposto:** distinguere presenza e valore, mediante DTO con numero nullable o proprietà JSON obbligatoria. Rifiutare l'inerzia assente conservando lo zero esplicito.

**Test proposti:** proprietà mancante, zero dichiarato, valore positivo, null, nome errato e round-trip della sezione assiale.

## 2. P2 — Dimensioni di sezione invalide superano la validazione del modello

**Codice:** [StructuralModelValidator.cs](../src/StructuralSolver2D.Core/Validation/StructuralModelValidator.cs), righe 142–165, rispetto a [StructuralSectionCatalog.cs](../src/StructuralSolver2D.Core/Model/Sections/StructuralSectionCatalog.cs), righe 174–181.

Il validatore controlla nome, area e inerzia, ma ignora `Height` e `Width`. Il catalogo richiede invece che, quando presenti, entrambe siano positive e finite.

Riproduzione: modello della trave appoggiata, sezione con area/inerzia valide, sostituire una dimensione con `-0.2`, `0`, `NaN` o `PositiveInfinity`. Verificate entrambe le proprietà, otto combinazioni.

- Validazione: `IsValid = true`, nessuna issue.
- Analisi: termina; la somma delle reazioni verticali rimane `50 kN`.
- `StructuralSectionCatalog.FromSections(model.Sections)`: `ArgumentOutOfRangeException`.
- Riprodotto anche dal JSON CLI con `height = -0.2` e `width = 0`: il comando `analyze` termina con codice 0.

Il problema è la validazione incoerente dei dati e il fallimento del passaggio al catalogo. Gli analyzer attuali usano area e inerzia, quindi questo caso non dimostra una modifica delle loro reazioni dovuta alle dimensioni opzionali.

**Intervento proposto:** applicare anche nel validatore i controlli positivo/finito delle dimensioni opzionali, con issue specifiche. `null` continua a significare dimensione non specificata.

**Test proposti:** per entrambe le proprietà, null, positivo, zero, negativo, NaN e infiniti; confronto fra modello, catalogo e ingresso JSON.

## 3. P3 — Un elemento nullo nelle collezioni interrompe il validatore

**Codice:** [StructuralModel.cs](../src/StructuralSolver2D.Core/Model/StructuralModel.cs), collezioni e metodi `Add*`; [StructuralModelValidator.cs](../src/StructuralSolver2D.Core/Validation/StructuralModelValidator.cs), righe 43–50 e 366–379; [StructuralLoadCombination.cs](../src/StructuralSolver2D.Core/Model/StructuralLoadCombination.cs), riga 24.

Riproduzioni indipendenti su un modello altrimenti valido:

```csharp
model.Nodes[0] = null!;
new StructuralModelValidator().Validate(model);
```

e, con una combinazione esistente:

```csharp
model.LoadCombinations[0].Terms[0] = null!;
new StructuralModelValidator().Validate(model);
```

Entrambi producono `NullReferenceException`, senza un risultato di validazione. Verificati nove contenitori: Nodes, Members, Materials, Sections, Supports, LoadCases, Loads, LoadCombinations e Terms; tutti interrompono il validatore.

Si tratta di robustezza dell'API .NET con dati malformati: i tipi sono dichiarati non nullable e il reader CLI controlla già gli elementi JSON nulli. Il rilievo non implica che un JSON nullo aggiri il reader. È distinto dai riferimenti nodo nulli delle aste, già gestiti nella prima correzione.

**Intervento proposto:** rifiutare null negli ingressi `Add*`/costruzione dei termini e controllare le liste pubblicamente modificabili prima delle proiezioni LINQ; restituire issue con collezione e indice.

**Test proposti:** nove contenitori separati, null in testa/in coda e insieme a entità con altri errori, verificando che la diagnosi non venga interrotta.

## 4. P3 — SupportType non definiti vengono accettati

**Codice:** [StructuralModelValidator.cs](../src/StructuralSolver2D.Core/Validation/StructuralModelValidator.cs), righe 218–240.

Il validatore verifica gli enum di aste e carichi, ma non `StructuralSupport.Type`.

Riproduzione: sulla trave valida, sostituire il tipo del primo vincolo con `(SupportType)999` o `(SupportType)(-1)`.

- `IsValid = true`, nessuna issue.
- L'analisi termina, con somma delle reazioni verticali `50 kN`.
- Il valore inesistente rimane nel modello. Reporting usa il ripiego grafico basato sui gradi di libertà; i vincoli meccanici rimangono quelli dei flag espliciti.

È una lacuna di validazione dei metadati, non un errore riprodotto nelle condizioni al contorno. Non si propone di dedurre o modificare i flag meccanici dal nome del tipo.

**Intervento proposto:** `Enum.IsDefined(support.Type)` con issue `SUPPORT_INVALID_TYPE`.

**Test proposti:** tutti i valori definiti, due valori numerici fuori enum e relativo ingresso JSON.

## 5. P3 — GetLength perde lunghezze finite per overflow e underflow

**Codice:** [StructuralMember.cs](../src/StructuralSolver2D.Core/Model/StructuralMember.cs), righe 35–40; [StructuralModelValidator.cs](../src/StructuralSolver2D.Core/Validation/StructuralModelValidator.cs), righe 209–213.

La norma viene calcolata sommando i quadrati delle differenze, senza riscalamento. I quadrati possono uscire dall'intervallo `double` anche quando la lunghezza è rappresentabile.

| Estremi sull'asse X | Lunghezza attesa | `GetLength` | Validazione |
|---|---:|---:|---|
| 0, `1e155` | `1e155` | `Infinity` | Valido, nessuna issue |
| 0, `1e-170` | `1e-170` | `0` | `MEMBER_ZERO_LENGTH`, anche con tolleranza `1e-180` |

Nel primo caso il validatore confronta soltanto `length <= tolerance`, quindi la lunghezza infinita non viene segnalata. Il secondo usa intenzionalmente una tolleranza personalizzata ammessa dall'API, inferiore alla lunghezza attesa.

Sono scale estranee agli esempi strutturali ordinari. Il rilievo riguarda il contratto numerico delle API e non dimostra un errore alle dimensioni usuali.

**Intervento proposto:** norma con riscalamento per evitare quadrati fuori intervallo e controllo esplicito delle lunghezze non finite; diagnosticare separatamente geometrie non rappresentabili.

**Test proposti:** lunghezza 3–4–5, componenti grandi e piccole ancora rappresentabili, tolleranza personalizzata e differenza di coordinate effettivamente fuori intervallo.

## 6. P3 — Le factory delle sezioni restituiscono inerzia infinita con risultato rappresentabile

**Codice:** [StructuralSectionFactory.cs](../src/StructuralSolver2D.Core/Model/Sections/StructuralSectionFactory.cs), righe 31–32 e 81–82; formule analoghe alle righe 120–121.

Le dimensioni d'ingresso sono controllate, ma l'ordine di potenze, prodotti e divisioni può produrre overflow intermedio e non viene verificato il risultato restituito.

| Chiamata | Inerzia osservata | Inerzia finita attesa |
|---|---|---:|
| `Rectangular("S", 1, 1e103)` | `Infinity` | circa `8.333333333333333e307` |
| `CircularSolid("S", 1e77)` | `Infinity` | circa `4.908738521234051e306` |

Nel rettangolo è il cubo a uscire dall'intervallo prima della divisione per 12; nel cerchio il prodotto per π prima della divisione per 64. I valori attesi sono stati verificati con un ordine di operazioni che conserva gli intermedi finiti. Il validatore e il catalogo rifiutano poi l'inerzia infinita: il difetto non supera quei controlli successivi.

Anche questo rilievo interessa esclusivamente scale estreme. Non è stata riprodotta una discrepanza alle dimensioni delle sezioni degli esempi.

**Intervento proposto:** calcolo delle proprietà con intermedi controllati e verifica dei risultati, oppure limiti dimensionali espliciti con rifiuto chiaro delle dimensioni fuori dominio. Controllare anche underflow a zero e la formula della sezione cava.

**Test proposti:** benchmark dimensionali ordinari, risultati finiti con intermedi ingenui fuori intervallo, risultati effettivamente non rappresentabili e sezioni molto piccole.

## 7. P3 — I generatori producono coordinate infinite da dimensioni finite

**Codice:** [ParametricBeamGenerator.cs](../src/StructuralSolver2D.Core/Model/Generators/ParametricBeamGenerator.cs), riga 26; [ParametricTrussGenerator.cs](../src/StructuralSolver2D.Core/Model/Generators/ParametricTrussGenerator.cs), riga 138.

Riproduzioni, senza grandi conteggi di elementi:

- `SimplySupportedBeam(1e308, 2)`: l'ultimo nodo ha X infinito, perché `span * 2` trabocca prima della divisione. La coordinata attesa è `1e308`, rappresentabile.
- `NielsenParabolicTruss(10, 5e307, 2)`: T1 ha Y infinito, perché `4 * rise` trabocca prima dei fattori `t * (1-t)`. Alla mezzeria la quota attesa è esattamente `rise = 5e307`.

Gli ingressi superano i controlli positivo/finito, ma i modelli generati ricevono `NODE_INVALID_COORDINATE`. È distinto dall'accettazione di NaN/infinito in ingresso già corretta nella prima revisione: qui gli ingressi sono finiti e i nodi richiesti restano rappresentabili.

**Intervento proposto:** riordinare i prodotti mediante il parametro normalizzato e verificare le coordinate prodotte, compresi gli accumuli di campate della Gerber. Una geometria non rappresentabile deve produrre un errore chiaro.

**Test proposti:** pochi pannelli/divisioni con dimensioni finite grandi, confronto degli estremi con gli argomenti e controllo della finitezza di tutti i nodi. Non serve allocare modelli con conteggi estremi.

## Osservazione separata: variante Pratt da esplicitare

[ParametricTrussGenerator.cs](../src/StructuralSolver2D.Core/Model/Generators/ParametricTrussGenerator.cs), righe 29–33, orienta le diagonali verso l'alto avvicinandosi alla mezzeria. Su `PrattBridge(12, 2.5, 4)`, con carichi verticali `-10 kN` nei nodi inferiori B1/B2/B3, le quattro diagonali hanno N circa `-23.430749`, `-7.810250`, `-7.810250`, `-23.430749 kN`, quindi compressione. Invertendo le diagonali nel solo modello diagnostico, si ottengono i corrispondenti valori positivi.

La Pratt convenzionale per gravità ha diagonali tese; esistono anche varianti orientate per carichi di sollevamento. La fonte illustra entrambe: [Steel Construction Info — Pratt truss](https://steelconstruction.info/topics/design/trusses).

Il risultato meccanico è riprodotto, ma non basta per attribuire un bug univoco: il metodo si descrive come “Pratt-like” e non dichiara il verso di carico per cui è concepito. Per questo non è incluso nei sette rilievi. Occorre esplicitare la variante nell'API/documentazione e verificare la disposizione delle diagonali, invece dei soli conteggi di nodi e aste.

## Verifiche e copertura prima delle correzioni

Suite esistente eseguita sulla base invariata:

```powershell
dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal
```

| Suite | Superati | Falliti |
|---|---:|---:|
| Core.Tests | 192 | 0 |
| Analysis.Tests | 245 | 0 |
| Reporting.Tests | 160 | 0 |
| Cli.Tests | 177 | 0 |
| Totale | 774 | 0 |

Le riproduzioni sono lacune della copertura corrente; non sono fallimenti dei test persistenti già presenti.

```powershell
dotnet run --project out/CoreSecondAudit/CoreSecondAudit.csproj --verbosity quiet
python out/CoreSecondAudit/check-results.py
dotnet run --project src/StructuralSolver2D.Cli --no-build -- analyze out/CoreSecondAudit/invalid-dimensions.json
```

Il progetto diagnostico esegue 31 casi osservativi: otto dimensioni opzionali, un ingresso JSON CLI, tre varianti di catalogo, nove contenitori nulli, due enum di vincolo, due lunghezze, due factory, due generatori e due orientamenti delle diagonali. Lo script Python conferma i risultati osservati sulla base attuale; le sue asserzioni descrivono i difetti presenti e non vanno trasferite come aspettative corrette nei futuri test di regressione.

Artefatti locali:

- [Risultati delle riproduzioni](../out/CoreSecondAudit/results.json).
- [Log suite .NET](../out/CoreSecondAudit/baseline-tests.log).
- [Analisi CLI con dimensioni invalide](../out/CoreSecondAudit/cli-invalid-dimensions.log).

La verifica non dimostra l'assenza di altri difetti. Le priorità tengono conto dell'impatto effettivamente osservato: omissioni/dati incoerenti nei flussi ordinari prima, robustezza e dominio numerico estremo dopo. Nessun risultato numerico a dimensioni strutturali usuali è stato dichiarato errato sulla sola base di questi casi estremi.
