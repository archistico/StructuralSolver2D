# P0-D1 — Prima regola nativa NTC 2018 per acciaio, trazione pura

**25 ottobre 2026. Codice C# e test xUnit predisposti; gate Windows NON ancora eseguito.**

## Normativa letta e decisione implementativa

**Fonte primaria:** D.M. 17 gennaio 2018, [NTC 2018, G.U. S.O. 8/2018](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf):

- **§4.2.4.1.1, Tabella 4.2.VII:** per membrature ordinarie qui considerate
  `γM0=1,05`, `γM1=1,05`, `γM2=1,25`. Il modulo D1 non riguarda i ponti.
- **§4.2.4.1.2.1, [4.2.5]–[4.2.7]:** per sezioni indebolite da fori di
  collegamento, `Npl,Rd = A·fyk/γM0`, `Nu,Rd = 0,9·Anet·ftk/γM2`,
  `Nt,Rd = min(Npl,Rd; Nu,Rd)` e `NEd/Nt,Rd ≤ 1`.
- **§4.2.4.1.2.1, [4.2.8]:** quando il progetto prevede la gerarchia delle
  resistenze (es. per azioni sismiche), `Npl,Rd ≤ Nu,Rd`. È un obbligo
  **distinto** dal confronto `NEd ≤ Nt,Rd`.

Il PDF della Circolare 7/2019 resta fonte interpretativa subordinata; qui
le tre formule e i coefficienti vengono letti direttamente dal D.M. La
somiglianza algebrica con EC3 **non** consente di chiamare «NTC» M89–M91.

## Funzioni aggiunte

File `src/StructuralSolver2D.Verification.Steel/Ntc2018SteelTensionRulesD1.cs`:

1. `Ntc2018SteelTensionProfileD1` identifica esclusivamente
   `DM 17 gennaio 2018 / NTC2018 / IT-NTC2018-NATIONAL-CODE` e blocca
   i tre fattori al valore e alla fonte della Tabella 4.2.VII. Nessun
   riutilizzo dei coefficienti EC3 o dei γM in libreria Timber.
2. `Ntc2018SteelTensionEvidenceD1` richiede **membro, sezione, qualità,
   analisi, combinazione e documentazione** esterna. Lo stato dei fori
   è tipizzato: `Unknown`, `NoRelevantHoles`, `RelevantConnectionHoles`.
   Per i fori rilevanti sono obbligatori `Anet` in mm², `ftk` in MPa
   e le rispettive giustificazioni. Non vengono dedotti dai risultati FEM.
3. `Ntc2018SteelPureTensionResistanceRuleD1` calcola il limite
   appropriato, usa la minore resistenza con i fori e ne traccia
   esplicitamente il ramo (`GROSS_YIELD`/`NET_RUPTURE`).
4. `Ntc2018SteelTensionCapacityHierarchyRuleD1` controlla separatamente
   [4.2.8] se il progetto ne documenta l'applicabilità. Restituisce
   `NotApplicable` quando è documentata una non applicabilità e
   `MissingData` quando l'applicabilità non è nota, oppure non vi è
   capacità netta sufficiente a calcolare il controllo.

Entrambe le regole implementano `IDesignCheckRule` e passano dal normale
`DesignCheckEngine`. Hanno identificatori diversi dagli EC3 e dichiarano
`EstablishesCompleteMemberCompliance=false`.

### Gate d'ingresso (fail-closed)

| Dato o fenomeno | Condizione di applicabilità | In difetto |
|---|---|---|
| Norma | Identità NTC2018 e profilo ordinario esatto con γM0/γM1/γM2 documentati | `Unsupported` per norma errata; `MissingData` per coefficienti manomessi |
| Domanda strutturale | Solo NTC SLU, `N>0`, `V=M=0` **esatti** alla stessa sezione | `NotApplicable` |
| Provenienza FEM | Identificativo membro, target combinazione, lato/stazione, analisi e combinazione coincidenti con le prove | `MissingData`/`InvalidInput` |
| Geometria/qualità | Profilo commerciale OPPO identificato e giustificato; S235 ecc. da M88-C con banda di spessore, prova prodotto esterna | `MissingData` |
| Fisica non osservabile nel FEM 2D | Trazione concentrica, nessuna torsione/azione fuori piano **documentate** | `MissingData` |
| Fori | Assenza confermata **oppure** presenza dichiarata con Anet validata, `0<Anet<A`, `ftk>fyk` e fonti | `MissingData` |
| Gerarchia | Se richiesta, prova applicabilità e confronto separato tra Npl,Rd/Nu,Rd | `Failed` oppure `MissingData` |

L'area OPPO non costituisce certificazione di prodotto o disegno di
officina. I valori di prova `Anet` e `ftk` sono **input espliciti
ipotetici** per i test, non una verifica geometrica del giunto. Non
esistono conversioni nascoste da un modello di collegamenti inesistente.

## Esempio riproducibile a mano e golden

Profilo IPE-200 indicativo OPPO: `A=28,48 cm²=2848 mm²`;
qualità S235, spessore laminato nell'intervallo con `fyk=235 MPa`.

`Npl,Rd = 2848 · 235 / (1,05 · 1000) = 637,4095238095 kN`.

| Scenario | Anet (mm²) | ftk (MPa) | Nu,Rd (kN) | Nt,Rd (kN) | Risultato con NEd=630 kN |
|---|---:|---:|---:|---:|---|
| Nessun foro rilevante **comprovato** | — | — | non richiesto nel sottocaso | 637,4095 | `Passed` **sezione pura** |
| Fori, legamento netto inferiore | 2400 | 360 | 622,08 | 622,08 | **`Failed`**, governa rottura netta |
| Fori, legamento netto maggiore | 2700 | 360 | 699,84 | 637,4095 | `Passed` **sezione pura**, governa snervamento |

Con `Anet=2400` la gerarchia [4.2.8] fallisce:
`637,4095 > 622,08`; con `Anet=2700` passa:
`637,4095 ≤ 699,84`. La gerarchia non dipende dal valore del carico
applicato `NEd`: può fallire anche quando `NEd=100 kN`.

**Controprove locali:** `python3 benchmarks/independent/p0d1/verify_p0d1_steel_tension_arithmetic.py`
controlla 16 relazioni aritmetiche esatte indipendenti dal C#.
Suite precedente P0-R2: 34 confronti aritmetici Python. **Non** sono
test di compilazione o esecuzione del nuovo modulo C#.

### Eseguire su Windows

Dal percorso root estratto del pacchetto:

```powershell
dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj --filter FullyQualifiedName~Ntc2018SteelTensionRulesD1Tests
dotnet test StructuralSolver2D.sln
```

Se il file `.sln` della tua estrazione si chiama diversamente, scegliere
la soluzione presente nella root, senza modificare i test. Inviare
gli errori/esiti esatti; il gate **resta PENDING** finché non viene
realmente confermato. **Nessun SDK .NET** disponibile nella sessione di
preparazione del codice.

## Limiti da non nascondere

- Questa è una verifica **di una sezione in pura trazione**, non una
  verifica completa di collegamento, tirante, membratura o edificio.
- Restano esterne e non verificabili automaticamente: foratura reale,
  filetti di bulloni/giunti, concentrazioni o riduzioni geometriche
  particolari, stabilità, difetti, fatica, sismica completa, azioni
  fuori piano e capacità degli altri componenti del percorso resistente.
- Non tratta compressione, flessione, taglio né le loro interazioni;
  non cerca i massimi normativi continui su un'asta o tutte le combinazioni.
- La proprietà `Passed` è riferita **esclusivamente al singolo RuleId**.
  La gerarchia [4.2.8] è una seconda regola da includere quando richiesta.
  P0-C3 deve ancora imporre l'inventario di tutti gli obblighi prima di
  dichiarare coperto un membro.
- Il caso C2C1 preesistente rimane **solo EC3 e solo gross yield**;
  non è stato modificato o promosso automaticamente a NTC.

**Roadmap:** gate Windows P0-C2B/C2C0/C2C1 e ora **P0-D1**; poi
P0-D0 classificazione tipizzata, P0-D2 interazioni e taglio,
P0-D3 SLE acciaio, P0-E stabilità spaziale e P0-C3/C4 obblighi/report.
**GUI C#/Avalonia esclusivamente P3.**
