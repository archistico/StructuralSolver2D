# M75 — Contratto del modello 2D, round-trip JSON e preflight

**Data:** 23/10/2026 · **Stato:** implementato, **test Windows .NET 8 non ancora eseguiti** · **Base:** M74-PLAN, codice M73 validato dall'utente.

## Perché questo milestone

L'editor Avalonia futuro deve poter salvare un modello e riaprirlo senza cambiare elementi, riferimenti, segni, vincoli o dati dinamici. Il lettore JSON già presente era un punto d'ingresso unidirezionale; mancavano un writer canonico e un preflight indipendente dal calcolo. Non si introducono nuove formulazioni FEM, nuovi carichi o norme tecniche: l'autopeso rimane **M76**.

## Nuove API C# (nel modulo CLI.Input)

```csharp
StructuralModelJsonFile input = StructuralModelJsonReader.Read(path);
StructuralModelJsonWriter.Write("saved.json", input);
string json = StructuralModelJsonWriter.Serialize(input);

// Controllo preliminare opt-in, senza assemblare matrici:
var diagnostics = new StructuralModelJsonContractValidator().Validate(input);
if (!diagnostics.IsValid)
    foreach (var issue in diagnostics.Errors)
        Console.WriteLine($"{issue.Code}: {issue.Message}");

// Oppure: deserializzazione + controllo con eccezione strutturata
StructuralModelJsonFile valid = StructuralModelJsonReader.ReadValidated("saved.json");
```

`Read(...)` **rimane compatibile** e conserva il suo comportamento storico. `ReadValidated(...)` è un nuovo metodo *opt-in* che usa il validatore Core e verifica che il loadCaseId / loadCombinationId selezionato esista; in presenza di metadati `dynamics` usa anche `StructuralDynamicsValidator`. Le eccezioni `StructuralModelJsonValidationException` espongono `Validation.Issues`, con severità, codice stabile, testo e ID entità quando disponibile. Warning non bloccanti non rendono invalido il modello.

L'esportazione copre i **dati del modello standard**: title, description, selettore del caso, nodi, materiali, sezioni, aste e release, vincoli con orientamento, load cases, carichi (inclusi endValue/position), combinazioni con fattori firmati, dinamica v1 con masse nodali. I nomi delle proprietà sono `camelCase` e gli enum sono serializzati come stringhe; le proprietà nullable non impostate sono omesse. Ordine di aste e carichi conservato. Non modifica le entità originali.

**Limitazione deliberata:** l'attuale reader del modello non memorizza gli eventuali blocchi radice specifici di analisi (`seismic`, `timeHistory`, `nonlinear` ecc.). Pertanto `StructuralModelJsonWriter` non deve essere usato come convertitore lossless di *documenti con estensioni*: è lossless rispetto al **modello standard e alla sua dinamica v1**, non rispetto a proprietà radice sconosciute. Le estensioni hanno contratti autonomi da versionare in futuro. Lo stesso vale per l'eventuale commento JSON d'origine: non viene conservato durante la riscrittura.

## Conversioni esplicite per i cataloghi futuri

`StructuralSolver2D.Core.Units.StructuralUnitConversions` converte mm, cm, N, mm², mm⁴, cm⁴, MPa e GPa nelle unità interne; per trasformare la densità di massa [kg/m³] in **peso specifico [kN/m³]** richiede esplicitamente l'accelerazione di gravità. Nessuna conversione avviene automaticamente quando si legge il JSON: i numeri nel file sono sempre unità interne. I test dimensionali C# fanno parte del gate M75.

## Segni, unità, terminologia

| Campo | Convenzione |
|---|---|
| Coordinate e lunghezze | m; X verso destra, Y verso alto |
| Forza nodale | kN, positiva nel verso della direzione specificata |
| Momento nodale | kN·m intorno a Z (antiorario positivo) |
| Distribuito | kN/m, positivo nel verso dell'asse/direzione scelta |
| Modulo elastico | kN/m² |
| Area e inerzia nel piano | m², m⁴ |
| Peso specifico `unitWeight` | kN/m³, **non massa volumica** |
| Massa additiva | kN·s²/m; inerzia rotazionale kN·s²·m |
| Fattore combinazione | Adimensionale; negativo ammesso, zero attualmente rifiutato dal validatore storico e rinviato al M79 |

Il caricamento `GlobalY` con `value: -18.0` punta verso il basso (gravità); con `+18.0` verso l'alto. **L'autopeso non viene ancora generato** da `unitWeight`: la voce `SelfWeight` rimane intenzionalmente rifiutata nel validatore sino a M76.

`StructuralSupport.OrientationDegrees` definisce il sistema di vincolo locale ruotato, non soltanto un'inclinazione grafica. `releaseStartMoment` / `releaseEndMoment` sono *svincoli perfetti*, non molle rotazionali elastiche. La verifica normativa SLU/SLE non esiste ancora.

## Test e gate

I test `StructuralModelJsonContractM75Tests` e `StructuralUnitConversionsM75Tests` coprono: round-trip di tutte le entità, metadata con caratteri accentati, valori negativi/variabili, default case vs combination, optional dynamics, creazione cartelle, riferimenti selezionati inesistenti, valori non finiti, lunghezza nulla, schema dinamico non supportato e modalità modale senza caso di carico. Il runner richiama anche la regressione storica del reader/CLI e tutta la soluzione.

```powershell
.\scripts\run-m75-tests.ps1
```

Prima operazione: `clean-project-build.ps1` (solo cartelle `bin` e `obj` legate a `.csproj`), poi output testuale stabile e log, compilazione Release, test mirati e suite completa. Non vengono generati benchmark Python.

**Criterio per chiudere M75:** esecuzione positiva del runner su Windows .NET 8 dell'utente. I test non certificano il software per progetto esecutivo, normative o casi non coperti. Il prossimo M76 riguarda autocargo/peso proprio e nuovi carichi con verifiche di equilibrio e non duplicazione.
