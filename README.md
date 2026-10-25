# StructuralSolver2D

**Motore FEM strutturale 2D open source in C# / .NET 8** per lo studio di travi, reticolari e telai piani in acciaio e legno. Il progetto è destinato alla didattica, alla ricerca e a valutazioni preliminari **con limiti espliciti**. La versione corrente non costituisce uno strumento certificato per attestare la sicurezza di un’opera.

> **Stato al 25 ottobre 2026:** ultimo gate .NET **P0-F1E PASS** riferito dall'autrice; i nuovi test analitici **P0-F1.2-A attendono collaudo**. Il golden statico G8B completo è ancora da ricevere e approvare. Dettagli in [stato corrente](docs/structural/CURRENT_STATE.md). Nessuna GUI desktop è stata avviata.

## Cosa fa il progetto

- **FEM 2D:** modelli di travi, telai (`Frame2D`) e reticolari (`Truss2D`), carichi e combinazioni manuali, spostamenti, reazioni e sollecitazioni `N`, `V`, `M`.
- **Analisi e risultati:** funzioni elastiche statiche, dinamiche e metodi avanzati in ambiti delimitati; post-processing, diagrammi, report e viewer HTML.
- **Acciaio:** cataloghi e verifiche **parziali, separate** EC3 (M89–M91) e NTC 2018 (D1, D0–D0M8, D2A–D2C2).
- **Legno:** verifiche **parziali e distinte** EC5 M92-A/B e NTC 2018 G1–G7.
- **Validazione:** test xUnit, benchmark di equilibrio, proprietà numeriche, casi limite e confronti con riferimenti indipendenti.

**Non fa automaticamente:** certificazione NTC/Eurocodici dell’intera asta o struttura, generazione e verifica *completa* di tutte le combinazioni di progetto, accertamento dei collegamenti e dei ritegni reali, stabilità spaziale a partire dal solo modello piano. Alcune funzioni numeriche sono volutamente sperimentali o circoscritte; non vanno interpretate come analisi non lineare generale.

## Avvio rapido

Requisito: **SDK .NET 8**. Dalla directory del progetto:

```powershell
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln -c Release
dotnet test StructuralSolver2D.sln -c Release
```

Per analizzare un modello JSON incluso:

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- analyze examples/beams/simple-supported-beam.json
```

Per esportare un viewer HTML:

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- export-viewer examples/beams/simple-supported-beam.json reports/beam.html
```

Gli [esempi](examples/README.md) servono all'apprendimento; i [benchmark](benchmarks/README.md) contengono casi di riferimento per le regressioni. È incluso anche un esempio di **casetta a due piani acciaio-legno** in `examples/mixed/two-storey-steel-house-timber-roof.json`, sviluppato in **quattro varianti G8B** con/senza controventi e differenti sezioni (`examples/mixed/g8b-house/`). La guida G8B è in `docs/structural/examples/G8B_VERIFICA_CASSETTA_E_CONFRONTO.md`; lo script Windows `scripts/run-g8b-house-demo.ps1` genera report e viewer. I test F1A–F1E sono confermati dall'autrice; il golden definitivo e i nuovi F1.2-A restano aperti. Non confondere esempi illustrativi e verifiche normative complete.

## Collaudo della versione corrente

Eseguire da **un'estrazione pulita** dello ZIP completo:

```powershell
.\scripts\verify-g7-manifest.ps1
.\scripts\run-g7-tests.ps1
```

Il primo script confronta i file con `RELEASE_SHA256SUMS.txt`; il secondo gestisce eventuali vecchi JSON G8B lasciati da estrazioni sovrapposte, pulisce le build, compila in Release e avvia regressioni. Sono **script Windows già presenti** nella baseline: ogni **nuovo** runner o script di progetto sarà in **C#/.NET**. Il collaudo fino a F1E è confermato dall'autrice; i nuovi test F1.2-A attendono esecuzione. Il nome G7 identifica lo storico runner.

## Architettura

| Modulo | Responsabilità |
|---|---|
| `src/StructuralSolver2D.Core` | Modelli, geometrie, carichi e contratti dati |
| `src/StructuralSolver2D.Analysis` | Solutori FEM e risultati strutturali |
| `src/StructuralSolver2D.Catalogs` | Materiali e sezioni con provenienza |
| `src/StructuralSolver2D.Verification*` | Motore di verifica, regole EC3/EC5/NTC e audit |
| `src/StructuralSolver2D.Reporting` | Report ed esportazioni |
| `src/StructuralSolver2D.Cli` | Comandi e interfaccia testuale |
| `tests/`, `benchmarks/` | Test automatizzati e riferimenti indipendenti |

Le unità del modello FEM devono essere coerenti nel sistema **kN–m–s**. Per esempio, con `GlobalY` positivo verso l'alto, una forza verso il basso ha valore negativo. I cataloghi di riferimento non sono automaticamente attestazioni di prodotto.

## Documentazione senza duplicazioni

| Documento | A cosa serve |
|---|---|
| [CHANGELOG.md](CHANGELOG.md) | Cronologia sintetica dei progressi, con esito dei gate |
| [Stato corrente](docs/structural/CURRENT_STATE.md) | Milestone, test confermati e lacune ancora aperte |
| [Roadmap aggiornata](docs/structural/roadmap.md) | Priorità, dipendenze e criteri dei prossimi gate |
| [Regole di sviluppo](docs/structural/ENGINEERING_AND_VALIDATION_RULES.md) | C#/.NET per nuovi script/test e NTC/EC3/EC5 distinti |
| [Validazione G8B F1C e generatore F1D](docs/structural/F1_1D_G8B_ESPORTAZIONE_GOLDEN_NVM_CSHARP.md) | Procedura C# per esportare il golden N/V/M proposto, con riferimento al [metodo indipendente F1C](docs/structural/F1_1C_G8B_RIFERIMENTO_VARIAZIONALE_CSHARP.md) |
| [Handoff nuova chat](HANDOFF_NUOVA_CHAT.md) | Stato operativo da aggiornare in **ogni nuova consegna** |
| [Manuale di validazione](docs/G7_MANUALE_VALIDAZIONE_INDIPENDENTE.md) | Fonti, benchmark G7 e prove effettivamente eseguite |
| [Audit legno](docs/structural/G6_IMPLEMENTATION_GUIDE.md) | Come leggere gli esiti di copertura NTC |
| [Checklist G7](docs/structural/G7_IMPLEMENTATION_CHECKLIST.md) | Criteri di completamento del gate G7 |
| [Documentazione del modello](docs/structural/model.md) e [input JSON](docs/structural/json-input.md) | Contratti d'ingresso e modellazione |
| [Archivio storico](docs/archive/README.md) | Vecchie note di rilascio, roadmap e handoff, **non autorevoli per lo stato attuale** |

Le singole milestone tecniche rimangono descritte in `docs/structural/` e `docs/`. Il README **non** viene usato come registro di tutte le release.

## Ambito e responsabilità

**Inclusi:** strutture piane di aste in acciaio e legno. **Esclusi per scelta di prodotto:** FEM 3D, cemento armato, muratura, piastre/pareti, X-LAM, BIM e progettazione esecutiva di collegamenti. I fenomeni non verificati devono risultare **incompleti o esplicitamente esclusi con giustificazione**, mai implicitamente superati.

Lo sviluppo segue l'ordine **motore validato → regole normative validate → visualizzazione/editor desktop Avalonia alla fine**. La licenza è [GPL-3.0](LICENSE).
