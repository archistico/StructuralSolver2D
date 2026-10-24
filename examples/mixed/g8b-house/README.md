# G8B — Confronto telaio di casetta acciaio / copertura legno

Questa cartella contiene **soltanto quattro modelli JSON strutturali completi**: i test CLI li enumerano automaticamente e si aspettano `nodes` e `members`. I file JSON di controllo e i dati numerici generati sono conservati **separatamente** in `benchmarks/independent/g8b-house/`, non qui.

**G8B FIX2:** se FIX1/FIX2 e' stato estratto sopra G8B, due vecchi JSON di benchmark possono restare nel filesystem. `scripts/run-g7-tests.ps1` esegue automaticamente `repair-g8b-legacy-examples.ps1`, che li riconosce e li salva in `.g8b-legacy-backup/` prima del test. Per migrare da soli: `./scripts/repair-g8b-legacy-examples.ps1` in PowerShell.

**G8B FIX1:** non aggiungere file JSON di report a `examples/`: il test `CliCommandRegressionTests.DistributedExamples_ShouldAnalyzeAndExportViewer` li interpreterebbe come modelli.

| Modello | Controventi | Pilastri | Travi da 5 m | Travi corridoio/balconi | Travetti di copertura |
|---|---|---|---|---|---|
| `g8b-b_riferimento-senza-controventi.json` | No | HEB 220 | IPE 330 | IPE 220 | GL24h 160×360 mm |
| `g8b-b_riferimento-controventato.json` | Sì, X nel campo 0–5 m | HEB 220 | IPE 330 | IPE 220 | GL24h 160×360 mm |
| `g8b-a_leggero-controventato.json` | Sì | HEB 200 | IPE 300 | IPE 200 | GL24h 140×320 mm |
| `g8b-c_robusto-controventato.json` | Sì | HEB 240 | IPE 360 | IPE 240 | GL24h 180×400 mm |

## Geometria

Pilastri agli x = 0, 5, 7, 12 m. Piani y = 3 e 6 m, base y = 0 m. Balconi all'esterno della quota +3 m con sbalzo da 1,2 m. Colmo del tetto x = 6 m, y = 8 m. Fascia tributaria di 4 m. Schema di edificio **2D**, non modello completo 3D.

La disposizione a X (4 diagonali, due a ogni piano) occupa la campata da **5 m** sulla sinistra. Nella realtà richiede una parete o una linea controventante compatibile con aperture e locali. Il corridoio da 2 m non viene sbarrato.

## Nuovo peso proprio esplicito

G8A riportava `unitWeight` nei materiali ma non richiedeva espressamente il peso proprio. Ogni G8B contiene ora un carico globale:

```json
{"id":"SW_ALL","loadCaseId":"G_SELF","type":"SelfWeight","targetType":"Model","targetId":"","direction":"GlobalY","value":1.0}
```

Questo carico genera la gravità di **tutti** gli elementi 1D in base all'area e al peso specifico del materiale; viene incluso in tutte le combinazioni. Le pressioni `G_FLOOR`, `G_BALC` e `G_ROOF` sono considerate come permanenti **aggiuntivi** dei relativi pacchetti e non includono di nuovo il peso degli elementi 1D del telaio.

## Controventi: significato fisico

Le X hanno aste `Truss2D` elastiche di area nominale **1500 mm²**, con sforzo normale sia positivo che negativo. **Non sono un modello di tiranti a sola trazione**; una diagonale compressa può instabilizzarsi e il modello FEM piano non lo verifica automaticamente. L'incrocio delle diagonali non è un nodo. La controventatura non è un progetto esecutivo di collegamento/ancoraggio.

## Risultati di controllo indipendenti sugli input

Da forze e lunghezze del modello, prima di lanciare il FEM:

| Caso | Risultante di tutte le azioni del caso |
|---|---:|
| `G_FLOOR` | Fy = −528 kN |
| `Q_ROOM` | Fy = −160 kN |
| `Q_CORR` | Fy = −48 kN |
| `G_BALC` | Fy = −57,6 kN |
| `Q_BALC` | Fy = −38,4 kN |
| `G_ROOF`, `SNOW` ciascuno | Fy ≈ −72,10 kN |
| `WIND_POS` | Fx = +44 kN |
| `WIND_NEG` | Fx = −44 kN |
| `WIND_UP` | Fy ≈ +31,62 kN |

`G_SELF` cambia in funzione della sezione: vedere CSV e JSON in `benchmarks/independent/g8b-house/`. Queste risultanti permettono di controllare che **la somma delle reazioni del solver** equilibri gli input di ciascun caso. Non determinano da sole la distribuzione delle reazioni, i momenti o gli spostamenti.

## Prova comparativa Windows

Dalla radice del progetto:

```powershell
.\scripts\verify-g7-manifest.ps1
.\scripts\run-g8b-house-demo.ps1
```

Lo script compila la solution Release, genera **16 report** e **2 viewer HTML** nella cartella `reports/g8b-house/`. A parità di sezioni e carichi si possono confrontare drift, reazioni e sollecitazioni dovuti ai soli controventi; a parità di controventi si confrontano le tre serie di sezioni.

## Dimensionamento: limiti e informazioni mancanti

I carichi superficiali scelti per l'esercizio (uso abitativo, corridoi, balconi, neve/vento) sono **esempi illustrativi**. Le combinazioni `ULS_GRAVITY`, `ULS_WIND_POS/NEG` e `SLS_RARE` sono combinazioni **manuali incomplete**: non sono il set normativo esaustivo NTC 2018 e non sono valide per un progetto reale senza una revisione completa. I carichi di vento/neve dipendono dal luogo e dalle caratteristiche dell'edificio; la ripartizione dei carichi nel 2D è assunta, non ricavata da un solaio 3D.

Le sezioni **non sono state verificate** nei rispettivi SLU/SLE; i dati di profilo IPE, HEB e GL24h sono nominali o indicativi e vanno confrontati con cataloghi e specifiche di fornitura. Non sono controllati collegamenti, instabilità laterale, tamponamenti, impalcati, fondazioni, capacità effettiva dei ritegni o azioni sismiche. Le verifiche normative nel progetto sono modulari e parziali: il CLI `report` di questi esempi non produce una certificazione di dimensionamento.


## Dati di controllo e rigenerazione

`benchmarks/independent/g8b-house/g8b-controlli-analitici.json` e `g8b-confronto-sezioni-e-carichi.csv` documentano carichi e geometrie. `g8b-risultati-elastici-indipendenti.json` contiene i risultati numerici. Lo script `benchmarks/independent/g8b-house/independent_frame2d_reference.py` rigenera quest’ultimo **nella directory dei benchmark**, senza sporcare la raccolta dei modelli sotto `examples/`.
