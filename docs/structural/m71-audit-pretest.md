# M71 — Audit pre-test completo (AUDIT-FIX1)

**Data:** 23 ottobre 2026  
**Base:** archivio integrale `StructuralSolver2D_M71_FRAME2D_STUDIO_CONSOLE_2026-10-23_FULL.zip`.  
**Stato:** audit statico concluso; suite .NET 8 e verifica reale del terminale Windows **non eseguite in questo ambiente**.

## 1. Risultati verificabili qui

| Area | Risultato | Metodo |
|---|---|---|
| Integrità ZIP | OK | Verifica completa dei membri compressi |
| Compatibilità archivio con M70-FIX1 | OK | Confronto SHA-256 di tutti i 516 file precedenti; nessuna eliminazione |
| Progetti .NET | 9 | Nove `.csproj` XML validi e file `.sln` presenti |
| Runner PowerShell | 22 | Tutti invocano `clean-project-build.ps1` prima del wrapper e dei comandi .NET |
| Esecuzione Python obbligatoria nei runner | Assente | Analisi di tutti i `run-m*-tests.ps1`; script Python storici restano nel repository ma non vengono invocati |
| Dati JSON | 82 validi | Parsing strutturale completo |
| Script JavaScript distribuiti | Controllo sintattico superato | `node --check` sui `.js/.mjs` distribuiti |
| Link locali README | Nessun riferimento mancante | Verifica dei target relativi Markdown |
| Cartelle di build nello ZIP | Nessuna | Nessun `bin/obj` confezionato |
| Riferimenti C# M71 | Revisione statica coerente | H01–H05: matrice di rigidezza, molla in serie, incrudimento isotropo, scarico, differenze finite |
| Compilazione C# ed esecuzione xUnit | **Non verificati qui** | SDK .NET 8 assente |
| Funzionamento PowerShell 5.1 / terminale Windows | **Non verificato qui** | `pwsh`/Windows PowerShell non disponibili |

## 2. Difetto reale trovato e corretto

Il gestore di output M71 impostava globalmente `CI=true`, `NO_COLOR=1`, `TERM=dumb` e `DOTNET_NOLOGO=1` durante il *dot-sourcing*. Le variabili di ambiente di processo persistono anche dopo la conclusione dello script figlio e potevano cambiare il comportamento delle successive applicazioni nello stesso terminale.

**Correzione:** le quattro variabili sono ora impostate esclusivamente dentro `function dotnet`, subito prima dell'avvio del processo nativo, e sempre ripristinate al valore precedente nel blocco `finally`. Restano invariati il log progressivo, la rimozione delle sequenze di controllo e l'inoltro dell'exit code. Il runner M71 contiene un *preflight* aggiuntivo che verifica il successo di `dotnet --version`, il codice non nullo di un comando intenzionalmente inesistente, l'assenza di modifiche residue all'ambiente e la creazione del log.

La verifica reale di queste modifiche è **ancora necessaria su Windows**. Non considerare un controllo del sorgente equivalente al superamento del test PowerShell.

## 3. Limitazioni rilevanti e gate M71

- M71 è uno **studio di fattibilità**: non è presente alcun elemento `Frame2D` elasto-plastico di produzione. Le formule H01–H05 verificano le ipotesi matematiche scelte, non la soluzione di un telaio plastico.
- Le cerniere proposte sono concentrate e finite, in serie con trave elastica. La rigidezza iniziale dell'elemento si riduce rispetto a quella di una trave con connessione rigida; la scelta di `k_theta` richiede una valutazione di sensibilità prima di M72.
- Non sono compresi interazione sforzo normale–momento, effetto P–Delta, grandi rotazioni, collasso o verifiche normative.
- I test C# precedenti risultano superati secondo le conferme dell'utente sino a M70-FIX1. L'audit qui non li ha rieseguiti.
- Il progetto conserva cinque script Python storici non invocati dai runner; nessuna nuova dipendenza Python è stata introdotta e nessun test numerico richiede Python.
- I viewer preesistenti non sono stati nuovamente collaudati visivamente in questo audit.

## 4. Test di accettazione consigliato

Estrarre lo ZIP completo `M71-AUDIT-FIX1` in una **cartella nuova**, quindi eseguire dalla radice:

```powershell
.\scripts\run-m71-tests.ps1
```

Il runner pulisce `bin` e `obj`, verifica il wrapper console, compila in Release, esegue i benchmark M71 e la regressione completa. Per confermare la risoluzione del problema della console, verificare che i messaggi avanzino senza ridisegno e che il file indicato in `StructuralSolver2D-test-logs` sia leggibile. Se un comando fallisce, l'exit code deve continuare a causare il fallimento del runner.

**Decisione:** nessuna criticità identificata nei riferimenti matematici M71 con la revisione statica; è stato corretto un effetto collaterale certo del wrapper console. **M71 non è dichiarato validato** finché il runner e il collaudo Windows non saranno superati.
