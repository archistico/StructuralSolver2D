# Pulizia centralizzata degli artefatti di compilazione (M61)

Per evitare che DLL e test di versioni precedenti vengano eseguiti per errore, tutti gli script PowerShell `scripts/run-m*-tests.ps1` iniziano ora con:

```powershell
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
```

Queste due istruzioni sono **prima di ogni controllo SDK, build e test**. Una pulizia non riuscita interrompe lo script; non si passa a una vecchia build. La regola vale anche per i nuovi runner futuri.

## Esecuzione manuale

Dalla cartella principale, su Windows PowerShell 5.1 o PowerShell 7:

```powershell
# Anteprima senza eliminare nulla
.\scripts\clean-project-build.ps1 -WhatIf

# Elimina effettivamente gli artefatti di build
.\scripts\clean-project-build.ps1

# Oppure lancia direttamente i test: la pulizia avviene automaticamente all'inizio
.\scripts\run-m61-tests.ps1
```

## Ambito e sicurezza

- Richiede che la directory di destinazione contenga una soluzione `.sln` o `.slnx` e almeno un `.csproj`, `.vbproj` o `.fsproj`.
- Trova i progetti nell'albero e cancella **soltanto le directory immediate** `bin` e `obj` accanto a ciascun file di progetto (comprese tutte le configurazioni Debug/Release e i TFMs).
- Non cancella cartelle chiamate `bin`/`obj` in documenti o directory prive di progetto; non cancella `TestResults`, sorgenti, JSON, esportazioni, pacchetti NuGet globali o file della soluzione.
- Blocca la rimozione di percorsi `bin`/`obj` simbolici/junction direttamente rilevati e segnala errori di file bloccati, invece di proseguire silenziosamente.
- È idempotente; `-WhatIf` mostra le azioni senza effettuare cancellazioni.
- **Non effettua** `dotnet clean` aggiuntivo: la rimozione preventiva delle cartelle rende ridondante quel passaggio e la `dotnet build` ripristina gli artefatti necessari.
- Questa è una pulizia della compilazione, **non** una pulizia di tutti i file temporanei o dei pacchetti: la decisione di non toccare altri dati è voluta.

### Verifiche previste

`run-m61-tests.ps1` controlla staticamente l'ordine del preflight in ogni runner; crea due progetti fittizi in una directory temporanea, verifica `-WhatIf`, cancellazione, idempotenza e preservazione di una directory `docs/bin` senza file di progetto. Esegue quindi la build e la regressione .NET dell'intera soluzione. Nessuno dei due controlli usa risorse del solver oltre alla struttura dei progetti.

**Nota:** eseguire gli script quando IDE, processi e test che tengono aperte DLL/PDB sono chiusi. Se Windows impedisce la rimozione di file ancora in uso, lo script si fermerà indicando il percorso interessato.
