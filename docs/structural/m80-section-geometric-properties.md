# M80 — Proprietà geometriche delle sezioni e catalogo parametrico

**Data:** 23 ottobre 2026. **Stato:** implementato, **test Windows .NET 8 pendenti**. Ultimo gate confermato dall'utente: M73. M75–M79 sono anch'essi in attesa di validazione cumulativa.

## Obiettivo

Preparare il futuro catalogo di profili di acciaio e legno senza modificare i calcoli FEM già presenti e senza confondere una geometria idealizzata con un prodotto certificato. M80 fornisce le proprietà geometriche per forme semplici e un contratto distinto per le *definizioni dimensionali* di catalogo. Non implementa verifiche NTC/Eurocodici, instabilità, proprietà efficaci, resistenza dei materiali o una GUI.

## API

Nuovi servizi nel progetto `StructuralSolver2D.Core`, namespace `StructuralSolver2D.Core.Model.Sections`:

- `SectionGeometryFactoryM80` — restituisce `SectionGeometricPropertiesM80` da dimensioni **in metri**.
- `SectionGeometricPropertiesM80` — sezione FEM standard più `OutOfPlaneInertia`, `InPlaneElasticSectionModulus`, `OutOfPlaneElasticSectionModulus`, `InPlaneRadiusOfGyration`, `OutOfPlaneRadiusOfGyration`.
- `SectionProfileDefinitionM80` e `SectionProfileCatalogM80` — catalogo opzionale con dimensioni dichiarate **in millimetri**, provenienza e formato JSON separato; `ToSolverCatalog()` costruisce il `StructuralSectionCatalog` v1 già supportato.

### Coordinate e unità

La trave Frame2D ha asse locale x longitudinale, asse locale y nel piano e rotazione intorno a z. La `StructuralSection.MomentOfInertia` già esistente rappresenta la flessione **nel piano** (I della sezione rispetto all'asse z, in m⁴). M80 aggiunge l'altro momento di inerzia geometrico, utile solo come dato di catalogo per future verifiche di instabilità fuori piano: **non crea gradi di libertà 3D**. Per forme simmetriche e con altezza `h` lungo y e larghezza `b` lungo z:

- `Area` [m²];
- `InPlaneInertia` [m⁴] e `OutOfPlaneInertia` [m⁴];
- `InPlaneElasticSectionModulus = 2·I_inPlane / h` [m³];
- `OutOfPlaneElasticSectionModulus = 2·I_outOfPlane / b` [m³];
- `InPlaneRadiusOfGyration = sqrt(I_inPlane / A)` [m] e analogamente fuori piano.

I cataloghi M80 dichiarano `widthMm`, `heightMm` e gli spessori in millimetri e li convertono *esplicitamente* tramite `StructuralUnitConversions`. Il formato JSON del solver e il catalogo storico `StructuralSectionCatalog` non cambiano unità né schema.

### Geometrie supportate

| Forma | Parametri | Formula caratteristica |
|---|---|---|
| Rettangolo | b, h | A=bh; I_in=b h³/12; I_out=h b³/12 |
| Rettangolo di legno | b, h | Identiche proprietà geometriche del rettangolo; la classe resistente **non** viene assegnata |
| Cerchio pieno | d | A=πd²/4; I_in=I_out=πd⁴/64 |
| Tubolare circolare | D, d | A=π(D²−d²)/4; I=π(D⁴−d⁴)/64; riusa le formule fattorizzate esistenti |
| Tubolare rettangolare ideale | b, h, spessore t costante | Somma di quattro rettangoli senza raggi agli spigoli, con momenti ottenuti mediante trasporto |
| Doppio T simmetrico ideale | b, h, spessore anima, spessore ali | Somma di due ali e un'anima, senza raggi di raccordo e senza tolleranze |

Per RHS e doppio T il codice somma direttamente le parti anziché sottrarre due inerzie quasi uguali: ciò riduce il rischio di cancellazione numerica per spessori piccoli. Le geometrie impossibili, proprietà non finite o derivate nulle vengono respinte.

**Importante:** le geometrie dei tubolari rettangolari e del doppio T sono *idealizzazioni a spigoli vivi*. **Non** sostituiscono le proprietà tabellate di IPE, HEA/HEB, RHS, UPN o altri profili laminati/formati, le cui sezioni reali hanno raccordi, tolleranze, spessori e regole produttive differenti. Le proprietà plastiche, J torsionale, Iw d'ingobbamento, aree di taglio, classe di sezione e capacità di progetto **non sono presenti**; non si assegnano valori arbitrari.

## Catalogo e provenienza

`examples/sections/m80-parametric-example.json` è una **dimostrazione** e non un catalogo commerciale. Schema distinto, versione 1, array `profiles`, stringa `shape` e dimensioni in mm. `dataStatus` può essere:

- `Illustrative`: forma geometrica di esempio; nessuna provenienza industriale implicita.
- `SourceDocumented`: l'utente fornisce obbligatoriamente `sourceDocument` e `sourceEdition`; **non implica revisione o certificazione** dei numeri. È un'indicazione di tracciabilità, non una verifica automatica del documento citato.

Nessun profilo commerciale verificato viene consegnato con M80. Il catalogo reale e la verifica dei dati rimangono M89, nella fase normativa. Si evita così di importare impropriamente misure di un produttore o generare dati IPE/HE a partire da semplici rettangoli ideali.

```csharp
using StructuralSolver2D.Core.Model.Sections;

var geometry = SectionGeometryFactoryM80.TimberRectangular("WOOD_80x240", 0.080, 0.240);
double inertia2D = geometry.InPlaneInertia;
double radiusOutOfPlane = geometry.OutOfPlaneRadiusOfGyration;

// Nessuna modifica al contratto esistente del solver.
var sectionForModel = geometry.ToSolverSection();

// Il nuovo formato M80 è opzionale e indipendente dal catalogo originale.
var profiles = SectionProfileCatalogM80.Load("examples/sections/m80-parametric-example.json");
var solverCatalog = profiles.ToSolverCatalog();
```

## Benchmark e test

`tests/StructuralSolver2D.Core.Tests/Model/Sections/SectionGeometryM80Tests.cs` contiene prove C#/xUnit: rettangolo con soluzione chiusa; RHS contro differenza di due rettangoli e parete molto sottile; doppio T contro decomposizione di rettangoli; sezione circolare; rotazione b/h; dimensioni impossibili; catalogo mm→m, round-trip JSON, conversione al vecchio catalogo, provenienza e duplicati. Nessuno script numerico Python.

Runner: `scripts/run-m80-tests.ps1`. Come primo comando elimina `bin`/`obj` tramite il pulitore centralizzato, poi abilita l'output testuale stabile, compila, testa il nuovo codice, esegue la regressione dei cataloghi precedenti e infine l'intera soluzione (incluso M75–M79). Gli esiti di .NET **non sono stati eseguiti in questo ambiente**.

## Decisioni e prossima tappa

- M80 crea proprietà geometriche, non caratteristiche resistenti certificate.
- Lo schema di `StructuralSection` e il reader JSON del solver restano invariati; le nuove API sono opt-in.
- Le proprietà di sezione fuori piano sono dati per verifiche future, non un FEM tridimensionale.
- **M81:** analisi geometrica del secondo ordine **nel piano**, P–Δ/rigidezza geometrica, benchmark e diagnostica di instabilità, in un modulo isolato inizialmente.
