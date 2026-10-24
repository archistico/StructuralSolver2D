# M88-B2 — Catalogo OPPO IPE, HEA, HEB, INP (24/10/2026)

## Risultato consegnato

Questa integrazione di M88-B contiene **40 profili tabellati indicativi**, 10 per ciascuna famiglia **IPE, HEA, HEB, INP**. Il motore FEM e il precedente catalogo M88-B IPE sono invariati.

Dati: `catalogs/m88/steel-oppo-ipe-hea-heb-inp.json`. API: `CommercialRolledSteelCatalogM88B2` del progetto `StructuralSolver2D.Catalogs`. Test: `OppoRolledSteelCatalogM88B2Tests.cs`.

## Fonti specifiche utilizzate (indicate dall'utente)

- IPE, ali parallele, tabella **UNI 5398-78**: https://www.oppo.it/tabelle/profilati_ipe.htm
- HEA, ali larghe alleggerite, **UNI 5397-78**: https://www.oppo.it/tabelle/profilati_hea.htm
- HEB, ali larghe normali, **UNI 5397-78**: https://www.oppo.it/tabelle/profilati_heb.htm
- INP, ali inclinate, **UNI 5679-73**: https://www.oppo.it/tabelle/profilati_inp.htm

La data di consultazione registrata è 2026-10-24. I riferimenti UNI sono riportati dalle pagine di origine e **non equivalgono a dichiarazione di conformità a una norma di prodotto vigente**. Le stesse pagine OPPO avvertono che i dati sono indicativi e possono non includere aggiornamenti successivi. Le tabelle potrebbero non coincidere con schede certificate di produttori specifici.

## Mappatura e unità

Colonne OPPO `h`, `b`, `a`, `e`, `r`, `Peso`, `Sezione`, `Jx`, `Jy`, `Wx`, `Wy`, `ix`, `iy`.

- `h/b/a/e/r` millimetri. `a` = spessore anima, `e` = spessore ala; attenzione all'ordine delle colonne nelle pagine HEA/HEB (`b` prima di `h`).
- `Peso` kg/m, `Sezione` cm², `Jx/Jy` cm⁴, `Wx/Wy` cm³, `ix/iy` cm.
- Direzione **forte** `Jx/Wx` OPPO ↔ `InPlaneInertia` / `InPlaneElasticSectionModulus`; direzione **debole** `Jy/Wy` ↔ `OutOfPlaneInertia` / `OutOfPlaneElasticSectionModulus`.
- Conversioni: cm²→m² = `1e-4`; cm³→m³ = `1e-6`; cm⁴→m⁴ = `1e-8`; mm→m = `1e-3`; cm→m = `1e-2`.
- I valori **tabellati**, comprensivi degli effetti geometrici dei raccordi dichiarati dalla tabella, sono conservati; non si applica la formula idealizzata M80 a spigoli vivi.
- Le INP hanno ali inclinate e restano una famiglia separata; la geometria piena delle ali non viene ricostruita dal solo insieme di proprietà tabellate.

## API minima

```csharp
var catalog = CommercialRolledSteelCatalogM88B2.Load(
    "catalogs/m88/steel-oppo-ipe-hea-heb-inp.json");
var section = catalog.Find("HEB-200");
var femSection = section.ToSolverSection(); // A e I forte in SI
var elastic = AnalysisCatalogM88A.Load("catalogs/m88/analysis-starter.json");
var input = catalog.SelectForElasticAnalysis("INP-200", elastic, "S355");
```

La selezione di acciaio prescritto non implica alcun valore di snervamento verificato, né EC3/NTC. Non avviene mai una rotazione implicita dell'asse forte/debole. Per una trave che fletta sull'asse debole serve un'impostazione di orientazione **esplicita** da aggiungere, non uno scambio tacito. La massa tabellata non modifica di nascosto il peso specifico del preset elastico.

## Condizioni di arresto e controlli

Il caricamento rifiuta: profili duplicati o famiglia errata, URL non corrispondente alla famiglia, riferimento storico assente/errato, dati senza stato indicativo, valori nulli o non finiti, assi invertiti, incongruenze tra J/W/dimensioni, tra raggi e A/J, tra massa/area, dimensioni fisicamente impossibili, schema JSON non supportato. La tolleranza riguarda soltanto i normali arrotondamenti delle tabelle.

**Limiti**: area/taglio efficace, proprietà torsionali, costante d'ingobbamento, moduli plastici, classi di sezione, instabilità laterale, proprietà caratteristiche/resistenti, sezioni di serie non catalogate, validazione presso produttore. Nessuna formula di verifica normativa è inclusa in M88-B2.

## Gate Windows

```powershell
.\scripts\verify-m88b2-manifest.ps1
.\scripts\run-m88b2-tests.ps1
```

Compilazione Release con warning come errori; test Catalogs M88-A/B/B2, Verification M87, intera regressione FEM, Reporting e CLI. **Fino al test su Windows, il gate M88-B2 è da considerare non ancora confermato.**

## Sviluppo successivo

M88-C: proprietà caratteristiche dei materiali acciaio/legno con provenienza, edizioni e valori di progetto separati. Poi verifiche di resistenza semplici con fonti normative controllate, e infine stabilità e interazioni con informazioni fuori piano esplicite. GUI Avalonia solo a valle delle verifiche.
