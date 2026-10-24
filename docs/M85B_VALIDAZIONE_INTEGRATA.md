# M85-B — Validazione integrata dell'analisi statica 2D

**Data:** 24 ottobre 2026  
**Stato:** compilazione e tutti i test M85-B / regressione superati su Windows (log 24/10/2026); benchmark opzionale da ripetere dopo correzione del runner.

## Obiettivo e confini

Il motore statico elastico 2D è già stato sottoposto ai casi analitici indipendenti di M85-A.
M85-B verifica ora che **gli stessi risultati numerici** arrivino senza alterazioni da:

1. modello JSON letto dal CLI;
2. API `StructuralSolver2DService` (`AnalyzeLoadCase`, `AnalyzeLoadCombination`);
3. output console del comando `analyze`;
4. tutti e sei i CSV di `export-csv`;
5. tabelle dei report Markdown `report` e `report-full`;
6. celle numeriche OpenXML dell'export XLSX;
7. intestazione e produzione dell'export PDF (controllo di integrità del contenitore, **non** confronto numerico del testo PDF).

**Non** si modificano solver, assemblaggi, carichi, segni, rigidezze o risultati di produzione.
I test si trovano in `tests/StructuralSolver2D.Cli.Tests/Validation/M85/IntegratedResultsM85BTests.cs`.

## Riferimenti fisici indipendenti

Tutti i valori qui riportati derivano dall'equilibrio statico e dalla teoria lineare di Eulero-Bernoulli,
non dall'implementazione FEM. Unità: m, kN, kNm, kN/m².

| Caso | Schema e carico | Riferimento |
|---|---|---|
| V01 | Mensola `L=4`, `P=-12` verticale in estremità, `E=210 000 000`, `I=0,00002` | `Uy_B=P L³/(3EI)`, `Rz_B=P L²/(2EI)`, `Fy_A=+12`, `Mz_A=+48` |
| V02 | Trave appoggiata `L=5`, `G1=-10`, `Q1=-4`, combinazione `ULS1=1,35 G1+1,50 Q1` | `q=-19,5`, `Fy_A=Fy_B=48,75`, `|M_max|=60,9375` |
| V03 | Stessa trave, `SLS1=G1+Q1` | `q=-14`, `Fy_A=Fy_B=35`, `|M_max|=43,75` |
| V04 | Stessa trave, caso esplicito `G1` (override del default `ULS1`) | `Fy_A=Fy_B=25`, `|M_max|=31,25` |
| V05 | Stessa trave, nuova combinazione `NEG=1,2G1 - 0,5Q1` | `q=-10`, `Fy_A=Fy_B=25`, `|M_max|=31,25` |

In tutte le prove i sei CSV CLI vengono anche confrontati testualmente con l'export
calcolato dalla API pubblica (21 campioni per membro). I valori precisi di deflessione e reazione
vengono inoltre letti dalle celle XLSX, non cercati come semplici stringhe.

**Limite interpretativo:** concordanza fra API e CSV dimostra continuità della pipeline,
non correttezza fisica indipendente: quest'ultima è controllata dai valori chiusi sopra e dai test M85-A.
La generazione PDF qui è un semplice smoke test (`%PDF-` e dimensione del file); sono
ancora da verificare layout, contenuto numerico PDF e apertura in più visualizzatori.

## Esecuzione su Windows

Dalla radice del repository:

```powershell
.\scripts\run-m85b-tests.ps1
```

Lo script ripulisce i residui di build, compila Release, esegue i test M85-B,
riesegue i controlli M85-A/M82/M83 e poi l'intera regressione di Core, Analysis,
Reporting e CLI. Produce un log tramite `stable-dotnet-console.ps1`.

Per un nuovo campionamento diagnostico M83 a 20 ripetizioni (massimo accettato dal tool), usare:

```powershell
.\scripts\run-m85b-tests.ps1 -WithBenchmark
```

File prodotti (cartella nuova, senza sovrascrivere il M83 originale):

```
artifacts/benchmarks/m85b/m83-scalability.csv
artifacts/benchmarks/m85b/m83-scalability.json
```

La soglia temporale **non** blocca i test: tempi e allocazioni dipendono dalla macchina,
dall'inizializzazione del runtime e dal carico del sistema. Per confronti seri servono
più campioni e modelli FEM più grandi.

## Decisioni e difficoltà

- **M83-B / sparse:** rinviato; la misura M83-A massima del primo ordine è 64 aste,
  192 gdl liberi, 1,644 ms, 1,59 MB allocati. Questa scala non dimostra un limite reale.
  Inoltre il campione a 16 aste presenta 2,968 ms contro 0,681 ms a 32 aste: 3 ripetizioni
  non costituiscono una curva di scalabilità.
- **M82:** validato solo come secondo ordine *globalmente linearizzato*, su perimetro
  limitato; non è una P-Delta/P-delta non lineare iterativa generale.
- **M84 plasticità globale:** rinviabile a un ramo sperimentale distinto.
- **M85-C (da pianificare):** benchmark esterni riproducibili, casi limite, audit errori/diagnostica,
  accuratezza del PDF e coerenza di resa grafica.
- **M86:** prima release **Core FEM elastico 2D**, con confini di utilizzo dichiarati e
  nessuna validazione normativa implicita.
- **Fase successiva:** verifiche acciaio/legno secondo NTC/Eurocodici; GUI C#/Avalonia solo alla fine.

## Criteri di chiusura

Il gate di compilazione e test M85-B è superato sul Windows dell’utente: 5 test specifici, 18 controlli mirati, 1.326 test di regressione tutti superati. Nel primo tentativo con `-WithBenchmark` il runner passava 25 ripetizioni al tool che ne accetta al massimo 20: il blocco era esclusivamente nel benchmark opzionale. La versione corretta usa 20 ripetizioni; il suo esito va ancora confermato.
Il superamento non equivale alla certificazione del software per progetti esecutivi.

## Dati M83-A archiviati

I file forniti dall'utente il 24 ottobre 2026 sono conservati **senza modifiche** in
`benchmarks/m83/measurements/2026-10-24-windows-x64/` (CSV e JSON).
Contengono la piattaforma Windows 10 x64, runtime .NET 8.0.29, 32 processori logici,
3 ripetizioni. Sono misure esplorative, non valori di prestazione garantiti.
Il massimo errore numerico riportato a 64 aste nel primo ordine è circa `1,96e-10`
(secondo la metrica del benchmark, non un errore globale certificato).
Il moltiplicatore critico M82 passa da 6,480245 a 2 aste a 6,476929 a 16 aste:
la tendenza alla convergenza riguarda soltanto questo caso e questa discretizzazione.
