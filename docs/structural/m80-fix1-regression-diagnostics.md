# M80-FIX1 — Diagnostica della regressione completa

## Perché 231 test superati e un errore finale non sono contraddittori

`run-m80-tests.ps1` eseguiva `dotnet test StructuralSolver2D.sln`. La soluzione contiene **quattro assembly di test**: Core, Analysis, Reporting e Cli. L’output riportato dall’utente il 24 ottobre 2026 dice che tutti i **231 test di `StructuralSolver2D.Cli.Tests`** sono passati, ma che il comando generale ha restituito un codice di uscita diverso da zero. Da questo output non si può stabilire se un altro assembly abbia fallito o se vi sia un problema di propagazione del codice di uscita.

## Soluzione diagnostica

La fase 4 ora avvia separatamente i quattro `.csproj`, conservando il `--no-build`, l’assenza del Terminal Logger, il wrapper che salva il log e la pulizia iniziale. Ogni uscita è catturata subito dopo `dotnet test`: il progetto viene marcato `PASSED` solo per exit code `0`, altrimenti `FAILED (exit n)`. L’esecuzione prosegue sugli altri progetti e al termine elenca quelli falliti. Il gate complessivo passa solo se **tutti e quattro** restituiscono exit code 0.

## Uso

Dalla radice del progetto:

```powershell
.\scripts\run-m80-tests.ps1
```

Per cercare i fallimenti nel log stampato dal runner:

```powershell
Select-String -Path '<percorso-log-stampato>' -Pattern '\[FAIL\]', 'Non superato', 'Test Run Failed', 'error ' -Context 2,5
```

Se un singolo progetto mostra `L'esecuzione dei test è riuscita` ma risulta `FAILED (exit 1)`, allegare log completo e versione PowerShell/SDK per indagare il passaggio del codice d’uscita.

**Nessun benchmark FEM è stato cambiato.** Stato M75–M80: attesa test Windows.
