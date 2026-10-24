# M88-C — Catalogo caratteristiche dei materiali (preview)

## Scopo

Questo incremento aggiunge **valori caratteristici o minimi tabellari di riferimento**, NON verifiche strutturali. Non vengono introdotte resistenze di progetto, coefficienti `gamma_M`, `kmod`, verifiche SLU/SLE o approvazioni NTC/EC3/EC5. Il motore FEM resta invariato.

- `StructuralSolver2D.Catalogs/CharacteristicMaterialCatalogM88C.cs`: API con selezione esplicita della classe e dello spessore per acciaio, e proprietà caratteristiche distinte per legno massiccio/lamellare.
- `catalogs/m88/characteristic-strength-classes.json`: dataset versionato e tracciabile.
- `tests/.../CharacteristicMaterialCatalogM88CTests.cs`: benchmark di valori pubblicati, limiti delle fasce di spessore, JSON, unità, errori e incoerenze.

## Campo di validità

| Famiglia | Classi | Norma di prodotto fonte | Limitazioni |
|---|---|---|---|
| Acciaio laminato | S235, S275, S355 | EN 10025-2:2019 | Minimi tabellari di snervamento in MPa; solo spessori (0,80] mm, senza estrapolazioni; sottogrado/dichiarazione di prestazione da verificare |
| Legno massiccio di conifera | C16, C24, C30 | EN 338:2016 | Proprietà caratteristiche di classe; non sostituiscono classificazione del prodotto |
| Legno lamellare omogeneo | GL24h, GL28h | EN 14080:2013 | Non confondere `h` (omogeneo) con `c` (combinato) |

Fonti di riscontro pubblico:
- https://www.steelforlifebluebook.co.uk/explanatory-notes/ec3-ukna/introduction-resistance-tables
- https://risponde.promolegno.com/domanda/data/ho-visto-che-e-in-vigore-la-nuova-uni-en-3382016-ci-sono-cambiamenti-rispetto-alla-vecchia-versio/index.html
- https://cni-online.it/Attach/DV12776.pdf (CNR-DT 206 R1/2018, tabelle materiali)

Sono riscontri pubblici, non copie autorizzate di testo normativo integrale. Per verifiche professionali confrontare i numeri con gli originali normativi applicabili e il prodotto effettivo. In particolare, la transizione agli Eurocodici di seconda generazione è in corso nel 2026: la presenza di una classe di prodotto non identifica l'edizione della regola di progetto o l'Annesso Nazionale.

## API e unità

```csharp
var catalog = CharacteristicMaterialCatalogM88C.Load(
    "catalogs/m88/characteristic-strength-classes.json");

double fy16Mpa = catalog.GetSteelYieldMpa("S355", 16);  // 355 MPa
double fy20Mpa = catalog.GetSteelYieldMpa("S355", 20);  // 345 MPa
var c24 = catalog.FindTimber("C24");
double fmCharacteristicMpa = c24.BendingStrengthMpa;   // 24 MPa
double eMeanMpa = c24.ElasticModulusParallelMeanMpa; // 11000 MPa
```

I moduli elastici del FEM sono in **kN/m²**; `1 MPa = 1000 kN/m²`. Non convertire automaticamente i valori della resistenza in capacità di un'asta. La densità è in `kg/m³`, distinta dai pesi volumici `kN/m³` dei preset elastici M88-A.

## Regole di sicurezza dell'API

- Un grado non registrato o uno spessore `> 80 mm` genera errore; nessuna estrapolazione.
- Le fasce di spessore devono essere contigue, partendo da zero, con limite inferiore escluso e superiore incluso. La soglia `16 mm` appartiene alla prima fascia.
- Dati nulli, duplicati, negativi, `NaN`, non finiti, provenienza assente e famiglie incompatibili vengono rifiutati.
- `ReferenceNotCertified` non significa dichiarazione del produttore o documento di conformità.
- `IncludesDesignStrengthsOrPartialFactors` e `ConfersCodeCompliance` restano `false`.

## Passi successivi

M89: calcolo delle resistenze delle sezioni in acciaio, **soltanto** dopo selezione esplicita di norma di progetto, edizione, annesso, classificazione e coefficienti. M92+: verifiche EC5/NTC con classe di servizio, durata del carico, `kmod`, `gamma_M`, `kdef`, etc. La stabilità laterale delle aste resta fuori perimetro fino a quando i ritegni non saranno rappresentati.

## Collaudo

Dalla cartella principale:

```powershell
.\scripts\verify-m88c-manifest.ps1
.\scripts\run-m88c-tests.ps1
```

La compilazione e i test M88-C devono essere verificati su Windows con .NET 8; non dedurre l'esito dal successo dei precedenti M88-B2.
