# P0-B4 — Audit delle combinazioni di un piano dichiarato e fingerprint degli input FEM

**25 ottobre 2026 — gate Windows dichiarato superato dall'utente nel checkpoint successivo («test passati», senza output numerico allegato).**

## Risultato e limite fondamentale

P0-B4 verifica un **insieme finito esplicitamente dichiarato per un singolo effetto scalare** (es. momento positivo in una stazione di una trave), non ricostruisce automaticamente tutte le situazioni di progetto fisicamente possibili. Se il progettista dimentica un'azione, un orientamento del vento, una disposizione di carico, una situazione di progetto o un effetto critico, il software **non può scoprirli solo controllando la coerenza di un elenco dichiarato**.

`MatchedDeclaredPlan` significa soltanto: *il piano dichiarato è coerente con l'inventario noto e tutte le combinazioni che il piano richiede coincidono con quelle realmente inserite nel modello FEM congelato*. Non significa progetto conforme, asta verificata, inviluppo completo, sicurezza dell'edificio o completezza normativa globale. Le proprietà `GlobalNormativeCompletenessEstablished` e `MemberAdequacyEstablished` valgono **sempre `false`**, anche senza errori.

## API e isolamento moduli

Solo file in `src/StructuralSolver2D.Combinations/Audit/` (dipendenza da `Core` e da `Combinations/Actions/Generation`):

| Tipo | Compito |
|---|---|
| `Ntc2018DeclaredCombinationPlan` | Identifica piano/effetto, famiglie richieste, azioni incluse/escluse, eccezioni di azione mai-guida con evidenza, compatibilità fisica attestata esternamente e scenari espliciti. |
| `Ntc2018DeclaredPlanValidator.Validate` | Rifiuta lacune interne al piano: azioni dell'inventario non classificate, famiglie non coperte, guide Q mancanti, alternative incompatibili, omessa azione senza motivazione, guide/favorevolezza contraddittorie. |
| `Ntc2018DeclaredCombinationAuditor.Capture` | **Prima di inserire i nuovi casi FEM**, esegue il validatore del piano e `StructuralModelValidator`; genera i candidati B3, congela fattori attesi e SHA-256 del modello di ingresso. Non modifica il modello. |
| `Ntc2018DeclaredCombinationAuditor.Audit` | **Dopo aver inserito i casi FEM**, legge direttamente `currentModel.LoadCombinations` (non una lista indipendente), verifica copertura e fattori, confronta i fingerprint e segnala in modo tipizzato carenze e incoerenze. |
| `CoreCombinationInputFingerprint.Compute` | SHA-256 su piano, inventario, termini della combinazione e reali input `Core`: nodi, geometria, aste, release, vincoli/molle, materiali e peso unitario, sezioni, casi e carichi caratteristici. |

L'hash è basato su campi canonizzati (numeri `round-trip`, ordinamento deterministico e separazione per lunghezza) e copre anche **carichi non selezionati** nel candidato; una variazione di peso proprio, geometria o definizione di un carico può cambiare la validità della baseline. Le altre `LoadCombinations` non sono nell'hash individuale: la loro presenza è auditata invece separatamente per ID attesi, mancanti o imprevisti, così inserire uno dei candidati dopo `Capture` non invalida falsamente tutti gli altri.

**Implicazione operativa:** l'hash è un controllo di *identità dell'input*, non un golden elastico o una prova di correttezza del solutore. `Capture` produce un oggetto baseline in memoria, non un documento firmato o resistente a manipolazioni; eventuale serializzazione/versionamento/autenticazione futura è un'attività distinta.

## Sequenza corretta

1. Compilare l'inventario B2 con **tutti** i `LoadCases` del modello e relative evidenze di valori caratteristici omogenei.
2. Per un effetto definito (asta/stazione/lato/tipo, verificato nel successivo P0-C), dichiarare le quattro famiglie pertinenti al profilo B1, il sottoinsieme di azioni in scope, le esclusioni motivabili, le eventuali variabili mai-guida e un'evidenza **esterna** sulla compatibilità degli scenari.
3. Elencare le varianti effettivamente richieste (guida Q, gruppi alternativi, disposizioni spaziali, presenza/assenza fisica e effetto favorevole/sfavorevole); chiamare `Capture(inventory, model, plan)` **prima** di aggiungere i candidati a `model.LoadCombinations`.
4. Inserire nel **medesimo** `StructuralModel` tutti i casi congelati; lanciare `Audit(inventory, model, plan, baseline)` prima del FEM. Un controllo passato significa *solamente insieme dichiarato riprodotto*.
5. Eseguire FEM, controllare l'esito del solutore e poi, in milestone P0-C/D/E, sezioni critiche, interazioni concomitanti, ritegni esterni, meccanismi non supportati e verifiche EC3/EC5/NTC pertinenti.

## Stati e diagnosi principali

- `Invalid`: input FEM/plan contraddittori, combinazione duplicata/imprevista, fattore differente, hash mutato, modello Core invalido, alternative simultanee o favorevolezza incoerente. **Bloccare il report positivo.**
- `Incomplete`: azione non classificata, guida/scenario/famiglia mancante, assenza senza motivazione, combinazione/termine atteso non trovato. **Non attribuire un superamento.**
- `Unsupported`: inventario B2 rimanda a un profilo non supportato; si richiede valutazione esterna o nuovo modulo versionato.
- `MatchedDeclaredPlan`: nessuna lacuna **rispetto al piano dichiarato**. È impossibile elevarlo a conforme o a completo rispetto all'intera struttura mediante il solo B4.

In caso di compresenza, prevalenza **Invalid → Unsupported → Incomplete**. I messaggi B4_* sono controllabili da test; gli ID scenario e la distinzione tra azione e `LoadCase` rimangono espliciti. `StructuralModelValidator` non dimostra stabilità globale di qualunque configurazione, né verifica capacità resistenti o nodi.

## Test P0-B4

`tests/StructuralSolver2D.Core.Tests/Combinations/Ntc2018DeclaredCombinationAuditB4Tests.cs` esercita:

- piano positivo di 7 scenari su G1, categoria A e vento (SLU STR A1: 2 guide; SLE rara: 2; frequente: 2; quasi permanente: 1);
- guide e famiglie mancanti, assenza senza evidenza oppure motivata, azioni non classificate, compatibilità fisica non attestata, due alternative vento +/− con guide esplicite;
- caso FEM richiesto non inserito, fattore modificato, termine assente/imprevisto, combinazione duplicata o estranea;
- mutazioni di carico caratteristico, nodo, E, area, release, vincolo e peso unitario; modifica dell'evidenza del piano; `Capture` senza mutazioni FEM;
- mantenimento esplicito di `GlobalNormativeCompletenessEstablished = false` e `MemberAdequacyEstablished = false`.

Da PowerShell Windows, **sulla nuova directory estratta pulita**:

```powershell
dotnet test .\tests\StructuralSolver2D.Core.Tests\StructuralSolver2D.Core.Tests.csproj -c Release --filter 'FullyQualifiedName~Ntc2018DeclaredCombinationAuditB4Tests' --logger 'console;verbosity=normal'
.\scripts\run-g7-tests.ps1
```

L'ambiente di assemblaggio dell'archivio **non dispone del SDK .NET**: i nuovi xUnit sono preparati, **non ancora compilati o eseguiti qui**. Conservati i 14 golden P0-B1 e 10 negative mutations Python; G7/P0-B2/P0-B3 risultano passati secondo le conferme dell'utente ma **non** equivalgono all'esecuzione dei test B4.

## Limiti aperti dopo questo gate

**P0-C (prossimo):** definizione/stazioni/segni dell'effetto; selezione di picchi interni ed estremi lato destro/sinistro, sezioni geometriche, N/V/M concomitanti e inviluppi di intera membratura. Collegare esplicitamente il piano B4 a un insieme di punti critici controllabili.

**P0-E (in parallelo come contratto di limitazione):** vincoli e ritegni effettivi, collegamenti, condizioni di sollevamento, stabilità fuori piano, tiranti e controventi non lineari; segnalare `Unsupported` dove il 2D non può giustificare le condizioni fisiche.

**P0-D (successivamente):** ampliare in sicurezza regole EC3/EC5/NTC dove prove indipendenti e precondizioni B/C/E consentano un giudizio locale, non una certificazione globale. P1 accuratezza ed elementi opzionali; P2 API/report; **GUI Avalonia solo P3**.

**Fuori profilo B4:** azioni non incluse nel piano, ricerca automatica dell'inviluppo, analisi non lineari/spaziali, sisma, incendio, EQU/GEO e controlli di collegamenti o membrature. I risultati dei quattro modelli G8B restano dimostrativi e privi di certificazione normativa.
