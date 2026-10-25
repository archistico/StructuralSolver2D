# F1.1-D — Esportazione verificata del riferimento N/V/M G8B in C#

**25 ottobre 2026 — P0-F1D/FIX1.** L'autrice ha successivamente confermato **P0-F1D-FIX1 PASS** («test passati»), senza log allegato. Il **generatore standalone C#/.NET 8** era stato avviato su Windows; il primo export si era fermato su un hash atteso incompleto, poi corretto nel FIX1. **Il JSON proposto e il suo SHA256 non sono stati consegnati**, quindi non si può ancora congelare il golden statico. Le nuove verifiche di presentazione sono in [F1E](F1_1E_G8B_VALIDAZIONE_VIEWER_REPORT_CSHARP.md), ancora PENDING .NET.

**FIX1, dopo il primo avvio Windows:** il comando F1D è partito ma ha correttamente rifiutato un hash atteso di 56 caratteri, trascritto per errore per `g8b-b_riferimento-senza-controventi.json`. Il valore nel nuovo `Program.cs` è stato allineato al file distribuito e al test F1C (`da10a8e7a3c65a048505506fbd295d890f49075631a4d490779f8379335f1c36`). Aggiunta una verifica preventiva di lunghezza/formato degli hash attesi. **Il FIX1 è stato confermato PASS dall'autrice**: i comandi sottostanti possono essere usati per produrre/verificare il file da trasmettere.

## Perché serve questa fase

Nel checkpoint [F1C](F1_1C_G8B_RIFERIMENTO_VARIAZIONALE_CSHARP.md) abbiamo realizzato un secondo calcolo FEM autonomo con quadratura dell'energia e soluzione Cholesky. Prima ha riprodotto i vecchi spostamenti/reazioni indipendenti, poi ha confrontato le sollecitazioni di tutte le aste con quelle del motore produttivo. Il PASS comunicato rende sensato **salvare un riferimento numerico statico separato** per evitare che future modifiche al codice del secondo risolutore cambino silenziosamente gli attesi.

Il programma `tools/StructuralSolver2D.G8BReferenceF1D/` usa **lo stesso sorgente C# indipendente che F1C ha validato**, incluso nel progetto con un collegamento `Compile` alla classe di test `G8BVariationalReferenceF1C.cs`. Nessuna duplicazione di formule e nessuna chiamata al solutore di produzione. Il nuovo progetto è incluso nella soluzione `.sln`, quindi `dotnet build` lo compila con gli altri moduli.

## Comportamento del generatore

1. Controlla l'**SHA256 dei quattro JSON G8B** e il file di confronto indipendente storico, identici ai valori già bloccati in F1C.
2. Per ciascuno dei **16 scenari**, risolve autonomamente il modello con quadratura + Cholesky.
3. Prima di esportare qualsiasi risultato, confronta spostamenti scelti, massimi e reazioni con il **golden precedente**. Se anche un controllo fallisce, termina **senza creare il file**.
4. Esporta in ordine deterministico **tutti i DOF nodali**, reazioni, sei azioni di estremità e `N/V/M` alle stazioni `0`, `1/4 L`, `1/2 L`, `3/4 L`, `L` di ogni asta. Sono attesi **336 coppie membro-scenario** e **1.680 triple N/V/M di sezione**.
5. Inserisce nel JSON la provenienza dell'algoritmo, le unità `m`, `kN`, `kNm`, gli hash degli input e l'identità del golden storico, con stato **`proposed-unreviewed`**.
6. Stampa l'SHA256 del nuovo JSON. La modalità `--verify` ricalcola il riferimento e verifica la corrispondenza esatta con il file esportato.

Il tool usa `FileMode.CreateNew`: **non sovrascrive mai un file esistente**. Se il file è già presente, leggere/verificare quello oppure scegliere un nuovo nome; non cancellare l'unico risultato precedente per rigenerarlo.

## Sequenza Windows, da directory estratta pulita

Prima compilare e collaudare l'intera soluzione:

```powershell
.\scripts\verify-g7-manifest.ps1
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln -c Release
dotnet test StructuralSolver2D.sln -c Release
```

Poi **generare** il file proposto:

```powershell
dotnet run --project tools/StructuralSolver2D.G8BReferenceF1D -c Release -- --output benchmarks/independent/g8b-house/g8b-member-nvm-f1d.proposed.json
```

E **verificarlo separatamente**:

```powershell
dotnet run --project tools/StructuralSolver2D.G8BReferenceF1D -c Release -- --verify benchmarks/independent/g8b-house/g8b-member-nvm-f1d.proposed.json
```

Quando terminano senza errori, inviare il **file `g8b-member-nvm-f1d.proposed.json` e/o il suo SHA256**, preferibilmente entrambi. Serve per revisionarlo e integrare un **golden definitivo immutabile** nel checkpoint successivo, senza inventare valori né produrli usando il FEM da validare.

## Cosa è verificato e cosa manca

| Aspetto | Stato al confezionamento |
|---|---|
| F1A, F1B, F1C | **PASS dichiarato dall'autrice** per la suite .NET sui checkpoint precedenti |
| `src/` FEM e verifiche normative | **Invariati** |
| Nuovo progetto C# generatore incluso in `.sln` | **PASS dichiarato dall'autrice per FIX1**, senza log allegato |
| SHA256 manifest F1D | **PASS del controllo dei file** in confezionamento; non è un test FEM |
| File statico N/V/M completo | **Non ancora prodotto**, perché qui manca l'SDK .NET |
| Golden approvato e hash bloccato in una nuova suite | **Prossima fase**, dopo revisione dei dati reali esportati |
| Viewer/report confrontati con il riferimento indipendente F1C | **F1E aggiunto, .NET PENDING**; l'ulteriore controllo contro il golden statico resta da completare |

La validazione resta confinata al **modello elastico piano idealizzato** di G8B, con X-brace bilatere e appoggi incastrati. Non dimostra collegamenti, ritegni reali, stabilità fuori piano o conformità normativa delle aste.

## Proseguimento della roadmap

1. **Chiudere F1.1-D:** ricevere e rivedere il JSON proposto, congelarne SHA256 e verificare i report/viewer nei casi significativi, inclusi carichi e segni N/V/M.
2. **F1.2 / F1.3:** estendere le prove a casi limite e robustezza numerica.
3. **F2–F5:** validità fisica, completezza combinazioni, massimi lungo membratura e audit di copertura.
4. **F6 / F7:** verifiche normative distinte per NTC 2018, EC3 ed EC5.
5. **F8 / F9:** prestazioni, API, beta; **F10 GUI Avalonia C# soltanto alla fine**.

Tutto il nuovo codice e lo strumento eseguibile sono **C#/.NET**, senza nuove dipendenze Python, Bash o PowerShell. Gli script storici della baseline non sono stati cambiati.
