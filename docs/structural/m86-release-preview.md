> **Aggiornamento del 24/10/2026:** il gate di M86 RC1 è stato eseguito e superato su Windows: compilazione 0 errori/avvisi, 1.334/1.334 test, prove CLI e report Markdown/CSV/XLSX/PDF. L'annotazione storica più sotto "gate ancora aperto" descrive il momento in cui fu preparato RC1.

# M86 RC1 — Prima release tecnica preview del motore FEM elastico 2D

**Data preparazione:** 24 ottobre 2026. **Versione di assembly invariata:** `0.1.0` (vedere `VERSION` e `Directory.Build.props`). **Identificatore dell'archivio:** M86-RC1; non è un rilascio stabile certificato né un tag Git creato automaticamente.

## Gate pregresso confermato

Il log Windows M85-C `dotnet-20261024-144236-29064-c1bb0a61.log` attesta:

- build .NET 8 Release, **zero errori, zero warning**;
- M85-C **8/8** test indipendenti;
- regressione completa: Core **325**, Analysis **581**, Reporting **192**, CLI **236**, totale **1.334/1.334**;
- test mirati aggiuntivi **28 Analysis e 5 CLI**, sovrapposti in parte alla regressione e non sommati al totale.

Questo è il risultato della versione **M85-C** eseguita dall'utente, non ancora della nuova procedura M86 RC1.

## Composizione e perimetro

La release preview distribuisce **sorgenti**, esempi JSON, documentazione, benchmark, test e runner PowerShell. Non contiene eseguibili Windows già pubblicati, installatori o pacchetti NuGet autonomi. Richiede **SDK .NET 8** e, per lo script, PowerShell 5.1 o superiore. La CLI compila localmente.

**Supportato dal gate interno:** analisi statica lineare elastica di aste Frame2D e Truss2D piane, modelli misti nei casi previsti, azioni nodali e distribuite, vincoli e releases supportati dal solver, carichi e combinazioni manuali, reazioni, spostamenti e forze di estremità, post-processing e report. Nomenclatura e convenzioni dettagliate sono documentate in `docs/structural/json-input.md` e nelle note dei singoli milestone.

**Unità di base:** coordinate [m], forze [kN], momenti [kNm], carichi distribuiti [kN/m], E [kN/m²], area [m²], I [m⁴], rotazioni [rad]. Assi globali X a destra, Y verso l'alto; **un carico verticale verso il basso è negativo in GlobalY**. Modulo elastico in unità coerenti, nessuna conversione silenziosa da MPa.

**Limitazioni fondamentali:**

- non contiene verifiche SLU/SLE secondo NTC/Eurocodici, coefficienti normativi, verifiche di instabilità fuori piano o qualificazione di elementi per opere reali;
- M82 è **secondo ordine globale linearizzato opt-in**: non è un'analisi geometrica non lineare iterativa e rifiuta varie tipologie di carico, svincoli e vincoli (vedere `docs/structural/m82-global-linearized-second-order.md`);
- i moduli dinamici, sismici e non lineari presenti nel repository non sono inclusi nel **contratto di accettazione M86 del nucleo statico elastico**;
- assenza di confronto intersoftware indipendente su una suite ampia; confronto numerico esterno M85-C limitato a un esempio pubblicato; nessuna revisione esaustiva dei contenuti numerici PDF;
- le prestazioni osservate M83 coprono fino a 64 aste Frame2D statiche e 16 aste M82: niente garanzie su grandi sistemi o vicino alle instabilità numeriche;
- non sono previste GUI desktop Avalonia, funzioni FEM 3D, acciaio/legno normativo, BIM, cemento armato, muratura, piastre e X-LAM in questa release.

## Prerequisiti e procedura di collaudo su Windows

1. Estrarre lo ZIP in **una nuova cartella vuota**, senza sovrapporlo a un vecchio progetto. Aprire PowerShell nella cartella `StructuralSolver2D`.
2. (Facoltativo ma raccomandato, **prima di modificare qualsiasi file**) `./scripts/verify-m86-manifest.ps1`, che verifica tutti gli SHA-256 dei sorgenti inclusi.
3. Avviare `./scripts/run-m86-release-gate.ps1`. Il runner ripara un eventuale file M82 obsoleto, elimina `bin/obj`, esegue la build Release con warning come errori, la regressione completa dei quattro progetti e smoke test reali da JSON, combinazione ULS1 e report in Markdown, CSV, XLSX e PDF.
4. Verificare i risultati `artifacts/m86-rc1-smoke/`, il log .NET riportato dallo script e l'assenza di errori.

Lo script opera nel progetto estratto e cancella esclusivamente `bin`/`obj` delle directory di progetto mediante `clean-project-build.ps1`; non modifica i file sorgente. L'opzione `-SkipClean` evita la pulizia ma **non** sostituisce il gate pre-release pulito.

**Test CLI manuale aggiuntivo:**

```powershell
dotnet run --project src/StructuralSolver2D.Cli -c Release -- analyze examples/beams/cantilever-point-load.json
dotnet run --project src/StructuralSolver2D.Cli -c Release -- analyze examples/combinations/load-combination.json ULS1
```

**Atteso per la mensola:** L=4 m, P=12 kN verso il basso, reazione verticale +12 kN, momento vincolare +48 kNm in equilibrio esterno, spostamento verticale in B da formula `-P L^3/(3 E I)` = circa `-0.06095238 m` usando E=210e6 kN/m² e I=2e-5 m⁴. Per il significato dei momenti locali e dei segni nei report vedere la documentazione; le formule analitiche sono già coperte dalla suite xUnit.

## Tracciabilità e integrità

- `RELEASE_SHA256SUMS.txt`: hash SHA-256 per ciascun file sorgente incluso, escluso il manifest stesso; confrontare tramite `scripts/verify-m86-manifest.ps1`.
- Lo ZIP esterno contiene solo file di progetto, senza `bin/obj`, log o artifact generati. Un hash dell'archivio esterno può essere pubblicato separatamente, dopo la conferma del gate Windows.
- Licenza dichiarata: GPL-3.0-or-later (`LICENSE`); le dipendenze NuGet di test e gli eventuali asset di terze parti devono essere censiti e verificati prima di distribuire un pacchetto binario per il pubblico. La presenza di pacchetti NuGet come dipendenze di test non costituisce una licenza del progetto.
- Non è stato creato un tag Git né un SBOM ufficiale: saranno parte del rilascio pubblico successivo al gate.

## Criterio di uscita M86

**RC1 pronta per il collaudo** se il pacchetto è integro e le modifiche ai sorgenti `src/` rispetto a M85-C sono zero. **RC1 accettata** solo dopo esecuzione riuscita su Windows di `run-m86-release-gate.ps1`, report verificati e log archiviato. Il passaggio successivo è la baseline del modulo `Verification` per NTC/Eurocodici; l'editor Avalonia verrà realizzato alla fine.
