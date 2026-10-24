# M70-FIX1 — Fixture CQC stabile e console PowerShell leggibile

**Stato:** correzione implementata; compilazione e regressione .NET 8 sul Windows dell'utente ancora da eseguire.

## 1. Causa del test fallito

`SeismicCqcM70Tests.TwoSpanModel_CqcAgreesWithIndependentMassWeightedModeContributions` utilizzava una catena orizzontale di due aste `Truss2D` (A–B, B–C), con A incastrato e C appoggiato verticalmente. La traslazione verticale `Uy` del nodo **B** rimaneva libera, pur avendo la reticolare collineare rigidezza verticale nulla: il nodo B possedeva un **meccanismo interno**. Il riduttore M54 era quindi corretto a emettere `M54_INTERNAL_MECHANISM`.

**FIX1:** il modello del benchmark vincola anche `Uy` di B con `StructuralSupport.SimpleSupport("SB", "B")`, senza impedire `Ux` di B o `Ux` di C. I gradi di libertà assiali attivi sono così due, adatti alla verifica CQC. Questo replica la costruzione fisicamente ammissibile già impiegata dal benchmark M60 `Spectrum_TwoAxialModes_SRSSMatchesIndependentReference_AndTruncatesConservatively`.

Un nuovo test `TwoSpanWithoutIntermediateTransverseRestraint_IsCorrectlyRejectedAsMechanism` elimina volutamente il vincolo B.Uy, verificando che il sistema **continui a rifiutare** il meccanismo. Non si modifica l'algoritmo CQC, né si indeboliscono la riduzione M54 o il solver M60.

## 2. Output MSBuild / PowerShell non progressivo

La sovrapposizione delle righe (ripristini di NuGet che si mescolano con stack trace e prompt PowerShell) è compatibile con il **Terminal Logger** MSBuild, che può ridisegnare lo schermo usando sequenze di controllo. Per una console testuale progressiva, tutti i **21 script `run-m*-tests.ps1`** invocano ora `dotnet build` e `dotnet test` con `--tl:off`. L'opzione è supportata dall'SDK .NET 8 e seleziona il logger di console tradizionale, senza riutilizzare dinamicamente le righe dello schermo. I comandi `dotnet run --no-build` rimangono invariati. Non cambia l'esecuzione logica dei test.

La pulizia `scripts/clean-project-build.ps1` resta la **prima operazione** di ogni script di test. Il miglioramento riguarda anche i runner precedenti, così non si deve ricordare un parametro diverso in ogni milestone successivo.

Se anche con `--tl:off` la console terminale resta difficilmente leggibile, usare una nuova finestra di Windows Terminal / PowerShell e produrre un log testuale con `Start-Transcript`:

```powershell
Start-Transcript -Path .\m70-tests-console.log -Force
try {
    .\scripts\run-m70-tests.ps1
}
finally {
    Stop-Transcript
}
```

Conservare nel log l'**errore del test xUnit originale**, non soltanto l'eccezione PowerShell conclusiva. Per il test fallito si può anche usare direttamente, dalla radice del progetto, dopo build pulita:

```powershell
dotnet test .\tests\StructuralSolver2D.Analysis.Tests\StructuralSolver2D.Analysis.Tests.csproj -c Release --no-build --tl:off --filter 'FullyQualifiedName~SeismicCqcM70Tests'
```

## 3. Validazione

Eseguire in una **cartella estratta nuova**, dalla radice:

```powershell
.\scripts\run-m70-tests.ps1
```

Ordine: pulizia `bin`/`obj`; build Release; test numerici M70, reporting e CLI; regressione dell'intera soluzione; export reali CQC e SRSS.

### Verifiche già effettuate sul pacchetto

- Archivio sorgente M70 originale preservato, con cambiamenti limitati al benchmark M70, ai runner PowerShell e ai documenti.
- Confronto con fixture M60: appoggio verticale intermedio presente nella soluzione fisicamente stabile.
- Controllo statico: tutte le invocazioni `dotnet build`/`dotnet test` nei 21 runner usano `--tl:off` e la pulizia rimane al primo posto.
- Nessun cambiamento ai sorgenti di produzione, alle formule CQC, all'API o ai viewer.

**Non verificato in questo ambiente:** compilazione C# e suite .NET 8 (SDK non disponibile). Il test xUnit non è ancora dichiarato superato.
