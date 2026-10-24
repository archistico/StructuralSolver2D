# M88-B — IPE commerciali: proprietà nominali tabellate

**Stato:** candidato tecnico da compilare/testare su Windows. **Non** è una validazione normativa.

## Perché questo catalogo è separato da M88-A

`SectionGeometryFactoryM80` calcola aree e inerzie di sezioni a spigoli vivi. Un IPE laminato presenta raccordi che modificano le proprietà; perciò **non** calcoliamo A, Iy, Iz, Wely e Welz con M80. `CommercialSteelCatalogM88B` conserva esattamente i valori **pubblicati in tabella**, memorizza dimensioni nominali e converte solo le unità richieste dal FEM.

## Fonti e responsabilità

- **Serie e dimensioni nominali:** EN 10365:2026, entrata in vigore il 1° luglio 2026 (scheda UNI): https://store.uni.com/en-10365-2026 . Le dimensioni pubbliche sono state confrontate anche con la tabella consultabile in https://www.iteh.us/catalog/standards/cen/ba63a1e4-3c1c-406a-b6c6-852b137fda78/en-10365-2026 .
- **Proprietà tabellate principali:** https://crosssections.app/en/ipe (valori visibili il 24 ottobre 2026: A, Iy, Iz, Wely, Welz, massa, h, b, tw, tf).
- **Controllo indipendente di plausibilità dei valori:** https://www.sectionlib.com/sections/ipe (tabella di confronto con arrotondamenti non identici, riferita ad ArcelorMittal Sales Programme V2026-1). Non è stata effettuata una certificazione di lotto o la verifica presso un organismo autorizzato.

La dicitura `PublishedThirdParty` significa **valori consultati da una tabella pubblica**; non significa "conformità certificata", né proprietà verificate da un produttore specifico, né capacità resistente di progetto. Per valutazioni esecutive confrontare proprietà con le tabelle ufficiali applicabili, l'edizione effettiva della norma e la documentazione di fornitura.

## Dotazione M88-B

8 profili IPE: **80, 100, 120, 140, 160, 180, 200, 240**. Unità del JSON: dimensioni **mm**, area **cm²**, inerzie **cm⁴**, moduli elastici **cm³**, massa lineare **kg/m**. Unitá nel `StructuralSection` FEM: `Area` **m²**, `MomentOfInertia` **m⁴**, `Height`, `Width` **m**.

Convenzione assi: per questo catalogo il FEM flette sull'**asse forte y-y** del profilo IPE (Iy). `OutOfPlaneInertia = Iz` è solo un dato complementare: non abilita un'analisi 3D né una verifica di instabilità laterale. **Nessuna rotazione di profilo implicita.** Per l'asse debole servirà un'API di orientamento esplicito.

### Esempio C#

```csharp
using StructuralSolver2D.Catalogs;

var profiles = CommercialSteelCatalogM88B.Load("catalogs/m88/steel-ipe-published-2026.json");
var materialCatalog = AnalysisCatalogM88A.Load("catalogs/m88/analysis-starter.json");
var input = profiles.SelectForElasticAnalysis("IPE-200", materialCatalog, "S355");
// input.Section.Area = 0.002848 m²; I_y = 1.943e-5 m⁴
// input.Material.ElasticModulus = 210000000 kN/m²
// Solo analisi elastica; nessuna proprietà fy, gammaM, resistenza, ecc.
```

### Limiti intenzionali

- Non si calcolano classi di sezione, Aeff, instabilità, interazioni, Wpl, It, Iw, Av, resistenze EC3, NTC, né proprietà di legno.
- M88-A (15 geometrie **illustrative**) e M88-B (8 IPE **tabellati**) rimangono cataloghi distinti.
- Le tabelle pubbliche hanno precisioni diverse; non è corretto importare una proprietà da una fonte e forzare la concordanza sostituendola con un calcolo a spigoli vivi.
- La massa nominale tabellata non è la massa misurata di un elemento reale. Nel solver il peso proprio rimane determinato dal prodotto tra area tabellata e peso volumico del materiale prescelto.
- I materiali S235/S275/S355 di M88-A forniscono soltanto E e peso specifico; la classe non definisce ancora valori di resistenza.
- `SourceCheckedOn` rappresenta la data di ricognizione; non è un certificato di revisione della tabella.

## Test di accettazione

Dalla radice `StructuralSolver2D`:

```powershell
.\scripts\verify-m88b-manifest.ps1
.\scripts\run-m88b-tests.ps1
```

Nuovi test: confronto con valori tabellati congelati, conversioni, interfaccia FEM, differenza dal modello ideale, dati mancanti, incongruenze tra assi, provenienza, serializzazione, compatibilità con il materiale e regressione totale.
