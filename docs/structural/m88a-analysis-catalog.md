# M88-A — Catalogo di sezioni e materiali (24 ottobre 2026)

## Decisione di progetto

Non duplicare la geometria delle sezioni M80. `StructuralSolver2D.Catalogs` è una libreria aggiuntiva che riusa `SectionProfileCatalogM80`, `SectionGeometryFactoryM80` e `StructuralSection` / `StructuralMaterial` già validati dal FEM.

Lo scopo **non** è una banca dati autorizzata per EC3, EC5 o NTC: M88-A produce **solo input elastici** per il motore 2D. In particolare il campo `Material.Name = Steel S235` è un'etichetta di preset per E e peso di volume; non contiene `fy`, `fu`, parametri del legno o fattori parziali e non costituisce verifica normativa.

## Dotazione concreta

- 15 sezioni geometriche esemplificative: 4 I doppiamente simmetrici **idealizzati**, 3 RHS senza raccordi, 2 CHS circolari ideali, 4 rettangolari massicce di legno, 2 rettangolari esemplificative di legno lamellare.
- 5 preset elastici di materiale, derivati dai valori già previsti nella libreria Core M80: S235, S275, S355, C24, GL24H.
- JSON con `schemaVersion`, nome, famiglia Steel/Timber, dimensioni **mm**, modulo E **kN/m²**, peso di volume **kN/m³**, e `dataStatus`.
- Identificativi univoci case-insensitive; geometrie validate dal resolver M80; reiezione di dati mancanti, incoerenti, non finiti, duplicati e materiali assegnati alla famiglia sbagliata.
- `SelectForElasticAnalysis` restituisce gli oggetti originali `StructuralSection` e `StructuralMaterial` utilizzabili in FEM. Non aggiunge capacità normative.

### Avvertenza sui profili commerciali

Le dimensioni della serie I presenti nell'esempio evocano alcune dimensioni nominali IPE, ma **i loro identificativi includono `IDEAL`** perché il modello a spigoli vivi M80 non comprende i raccordi di laminazione. Area, inerzia e modulo resistente **non coincidono necessariamente** con le proprietà dei profili commerciali IPE. Non esportare come «IPE certificato» o utilizzare automaticamente per resistenze EC3/NTC.

Riferimenti per la successiva verifica dei dati commerciali: catalogo ArcelorMittal (serie IPE, dimensioni EN 10365), https://sections.arcelormittal.com/products_and_solutions/EN ; tabella EN 10365:2026: https://standards.theiteh.com/catalog/standards/cen/ba63a1e4-3c1c-406a-b6c6-852b137fda78/en-10365-2026 . Questi riferimenti indicano **famiglie/dimensioni nominali** e non attestano le proprietà derivate dal modello ideale.

## Uso C#

```csharp
using StructuralSolver2D.Catalogs;

var catalog = AnalysisCatalogM88A.Load("catalogs/m88/analysis-starter.json");
var selected = catalog.SelectForElasticAnalysis("WOOD-80X160", "C24");
// selected.Section : StructuralSection (A, Iz in m²/m⁴)
// selected.Material: StructuralMaterial (E in kN/m²)
// selected.IncludesNormativeStrengthOrDesignFactors == false
```

Il contenuto può essere serializzato con `ToJson()` e ricostruito con `FromJson()` per i futuri strumenti GUI/CLI.

## Gate di test

Da PowerShell, nella cartella principale:

```powershell
.\scripts\verify-m88a-manifest.ps1
.\scripts\run-m88a-tests.ps1
```

Comprende compilazione Release con warning-as-error, nuovi test xUnit M88, 34 test Verification storici, 1.334 test di regressione su Core/Analysis/Reporting/CLI. Il numero e l'esito reale dei nuovi test dovranno essere confermati su Windows .NET 8.

## Cosa rimane per M88-B e M89

- Catalogare i profili **reali** IPE/HEA/HEB/RHS/CHS e proprietà certificate con fonti, edizioni, unità e tolleranze. Distinguere nettamente proprietà geometriche per FEM 2D e proprietà indispensabili a verifiche fuori piano.
- Reperire proprietà caratteristiche e campi di validità dei gradi/classi acciaio e legno da fonti normative utilizzabili legalmente; non confondere valori caratteristici con valori di progetto.
- Risolvere legame esplicito fra membro FEM, geometria catalogata e materiale, senza attribuzione automatica di resistenze o coefficienti parziali.
- Solo dopo, M89–M91 acciaio e M92–M94 legno, con clausole, edizione e annesso nazionale dichiarati.

### Rischi e semplificazioni

**Rischio maggiore:** uno stesso nome commerciale o classe di materiale potrebbe essere erroneamente trattato come materiale certificato. Mitigazione: suffisso IDEAL, `Illustrative`, e interfaccia che crea **solo** input elastici. Non introdurre tensioni ammissibili, `fy`, `fm,k`, `gammaM`, `kmod` in M88-A.

**Semplificazione:** non inventare formule geometriche nuove; mantenere catalogo iniziale piccolo, espandere solo quando dati verificati e relativi test di regressione sono disponibili.
