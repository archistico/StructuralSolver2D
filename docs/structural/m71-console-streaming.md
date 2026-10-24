# M71 — Console PowerShell lineare e log persistenti per la suite .NET

## Il problema osservato

I runner precedenti usavano `--tl:off` per disabilitare il terminal logger di MSBuild, ma sul Windows dell'utente l'output risultava ancora sovrapposto: righe di ripristino, test xUnit ed eccezioni riscrivevano la stessa area del terminale. `--tl:off` non governa necessariamente ogni sequenza di controllo prodotta dal test runner o dagli strumenti dipendenti.

## Diagnosi addizionale della CLI

L’ispezione dei comandi C# `StructuralSolver2D.Cli` non evidenzia un motore di barre di avanzamento o chiamate a `Console.SetCursorPosition`: i comandi usano prevalentemente `Console.WriteLine/Write` per output e report. Il difetto descritto compare mentre `dotnet build/test` emette messaggi di ripristino, test xUnit e stack trace. La nuova normalizzazione interessa quindi **i runner .NET**, non il calcolo/renderer del programma CLI.

## Intervento

Dopo la pulizia obbligatoria, ogni `run-m*-tests.ps1` include `scripts/stable-dotnet-console.ps1` tramite *dot-sourcing*. Il file:

1. verifica l'esistenza di `dotnet` come **applicazione**, non come alias PowerShell;
2. disabilita progress UI di PowerShell e applica **solo durante ciascun comando dotnet** un ambiente CLI a colori/progressione ridotti (`CI=true`, `NO_COLOR=1`, `DOTNET_NOLOGO=1`, `TERM=dumb`), ripristinando sempre le variabili precedenti in `finally`;
3. intercetta `stdout` e `stderr` del processo nativo attraverso la pipeline PowerShell, impedendo le scritture dirette e non coordinate sulla console interattiva;
4. elimina sequenze ANSI/OSC e caratteri di controllo; normalizza i ritorni carrello in righe regolari;
5. trasmette in modo progressivo l'output leggibile sulla console **e** in un log UTF-8 in una sottocartella temporanea;
6. ripropaga `$LASTEXITCODE` al chiamante: un fallimento resta fallimento, non viene mascherato.

Questa soluzione si aggiunge a `--tl:off`, già previsto. Nessun test C# e nessun solutore numerico cambia. Non dipende da Python o da moduli PowerShell di terze parti.

## Dove trovare il log

Lo script stampa inizialmente il percorso completo di un file nella cartella temporanea `StructuralSolver2D-test-logs`. Il file contiene solo testo riga per riga ed è consultabile anche quando la console ha perso parte della cronologia. **Non inserire segreti** nell'output degli script: il log replica gli output del processo.

## Collaudo da fare su Windows

- Estrarre lo ZIP in una cartella pulita; eseguire `scripts/run-m71-tests.ps1`.
- Verificare che la prima fase sia sempre la rimozione di `bin`/`obj` e che compaia il percorso del log.
- Controllare che l'output dei `dotnet build` e `dotnet test` sia progressivo e non sovrascriva righe già stampate.
- Il runner M71 prova automaticamente un comando dotnet valido e uno deliberatamente non valido, verifica la propagazione dell'exit code e il ripristino delle variabili di ambiente; la prova non modifica i test xUnit.
- Se continuano artefatti grafici, allegare il log UTF-8 e indicare terminale e versione PowerShell: in tal caso servirà distinguere il renderer del terminale dai processi .NET.

**Limiti:** il helper è predisposto per PowerShell 5.1+ e serializza le righe nell'ordine di arrivo; `stdout` e `stderr` separati possono avere un ordine relativo leggermente diverso dal tempo fisico di emissione. Le prove effettive sono ancora necessarie sull'ambiente Windows dell'utente. Non trattare la sanitizzazione come sicurezza HTML o come sostituto dei test numerici.

## AUDIT-FIX1 (23/10/2026)

Corretto il leak delle variabili di ambiente di processo; aggiunto il preflight nel runner M71. Vedere `m71-audit-pretest.md`. La verifica effettiva su Windows rimane aperta.
