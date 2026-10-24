# M72-FIX1 — Correzione del wrapper PowerShell e della build

## Errore riscontrato

Il primo avvio su Windows di `scripts/run-m72-tests.ps1` ha eseguito la pulizia e attivato il wrapper, ma MSBuild si è fermato con `MSB1008: È possibile specificare un solo progetto`. Nella riga di comando MSBuild è presente il valore isolato `minimal` accanto a `StructuralSolver2D.sln`.

## Diagnosi

`stable-dotnet-console.ps1` definisce una **funzione PowerShell** chiamata `dotnet` per registrare e ripulire lo stdout di .NET. Il comando del runner `dotnet build --tl:off StructuralSolver2D.sln -c Release -v:minimal` non viene analizzato come un'applicazione nativa: la sintassi abbreviata `-v:minimal` si sovrappone a `-Verbose` (parametro comune PowerShell). Di conseguenza viene passato a MSBuild un argomento in più. Non è un problema della soluzione FEM o del nuovo elemento Frame2D: la build non era ancora iniziata.

## Soluzione

- Eliminato `-v:minimal` dai runner **M71** e **M72**. La compilazione usa la verbosità predefinita; rimane `--tl:off` per disabilitare gli aggiornamenti dinamici.
- Il wrapper tenta di usare UTF-8 durante il comando .NET per leggere correttamente i messaggi localizzati, ripristinando la codifica originale in `finally`. Nei terminali che non consentono di modificare `Console.OutputEncoding`, prosegue senza interrompere i test.
- Mantenuti pulizia `bin`/`obj` come prima azione, log testuale in TEMP, codici d'uscita, nessuno script Python.

**Regola per nuovi runner:** non usare mai l'opzione breve `-v:...` per i comandi instradati attraverso una funzione PowerShell chiamata `dotnet`; usare la verbosità predefinita o `--verbosity minimal` (argomenti distinti).

## Stato di validazione

Verificati staticamente i due runner, l'integrità ZIP e l'assenza di `-v:minimal` negli script. L'ambiente di confezionamento non dispone di .NET 8 e Windows PowerShell 5.1: nessuna compilazione o test xUnit è stata eseguita qui. **M71 e M72 restano da validare**.

Sul Windows dell'utente, estrarre l'archivio completo e avviare:

```powershell
.\scripts\run-m72-tests.ps1
```

Per una diagnosi rapida, eseguire anche `dotnet build --tl:off StructuralSolver2D.sln -c Release` **da PowerShell senza caricare il wrapper**; in questo modo si distingue un problema del progetto da un problema dell'automazione.
