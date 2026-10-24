# StructuralSolver2D

**Motore FEM strutturale 2D open source in C# / .NET 8** per lo studio di travi, reticolari e telai piani in acciaio e legno. Il progetto è destinato alla didattica, alla ricerca e a valutazioni preliminari **con limiti espliciti**. La versione corrente non costituisce uno strumento certificato per attestare la sicurezza di un’opera.

> **Stato al 24 ottobre 2026:** milestone **G7 GATE PASSED**, confermata dall’utente su Windows. Le prove verificano le funzionalità coperte dalla suite, **non** la completezza delle combinazioni normative, delle verifiche di membratura o della modellazione fisica. Nessuna GUI desktop è stata avviata.

## Cosa fa il progetto

- **FEM 2D:** modelli di travi, telai (`Frame2D`) e reticolari (`Truss2D`), carichi e combinazioni manuali, spostamenti, reazioni e sollecitazioni `N`, `V`, `M`.
- **Analisi e risultati:** funzioni elastiche statiche, dinamiche e metodi avanzati in ambiti delimitati; post-processing, diagrammi, report e viewer HTML.
- **Acciaio:** cataloghi e verifiche **parziali** EC3 per le famiglie effettivamente implementate (M89–M91); audit della copertura M91-D.
- **Legno:** verifiche parziali EC5 M92-A/B e regole **native NTC 2018** G1–G5; audit NTC legno G6; benchmark integrati G7.
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

Gli [esempi](examples/README.md) servono all'apprendimento; i [benchmark](benchmarks/README.md) contengono casi di riferimento per le regressioni. È incluso anche un esempio di **casetta a due piani acciaio-legno** in `examples/mixed/two-storey-steel-house-timber-roof.json`, sviluppato in **quattro varianti G8B** con/senza controventi e differenti sezioni (`examples/mixed/g8b-house/`). La guida G8B è in `docs/structural/examples/G8B_VERIFICA_CASSETTA_E_CONFRONTO.md`; lo script Windows `scripts/run-g8b-house-demo.ps1` genera report e viewer. Il confronto G8B non è ancora stato collaudato sul PC. Non confondere esempi illustrativi e verifiche normative complete.

## Collaudo della versione corrente

Eseguire da **un'estrazione pulita** dello ZIP completo:

```powershell
.\scripts\verify-g7-manifest.ps1
.\scripts\run-g7-tests.ps1
```

Il primo script confronta i file con `RELEASE_SHA256SUMS.txt`; il secondo migra fuori da `examples/` eventuali due vecchi JSON G8B di **dati di riferimento** lasciati da estrazioni sovrapposte (backup in `.g8b-legacy-backup/`), pulisce le build di progetto, compila in Release e avvia benchmark e regressioni. Il **gate G7 è stato dichiarato superato dall'utente**; una nuova modifica richiederà un nuovo collaudo. Lo script mantiene il nome G7 perché valida la baseline software, anche dopo il riordino documentale.

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
| [Roadmap aggiornata](docs/structural/roadmap.md) | Priorità, limitazioni e fasi successive a G7 |
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
