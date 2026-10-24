# M68 — API, JSON e reporting opt-in delle reticolari elasto-plastiche 2D

**Data:** 23 ottobre 2026. **Stato:** implementato; build/test C# Windows .NET 8 ancora da eseguire. M67 validato dall'utente.

## Finalità e confini

M68 espone tramite API pubblica gli algoritmi **già implementati e testati** in M64 (statica incrementale con Newton e commit atomico) e M65 (dinamica Newmark–Newton). Non introduce nuovi modelli costitutivi o formulazioni FEM e non modifica i comandi o gli esempi esistenti. L'API usa contratti tipizzati nel namespace `StructuralSolver2D.Analysis.PublicApi`, con un input dedicato `NonlinearTruss2DModel` che è distinto da `StructuralModel` per evitare conversioni implicite tra modelli lineari e plastici.

Due metodi pubblici di `StructuralSolver2DService`:

- `AnalyzeNonlinearTrussStatic(StructuralNonlinearStaticRequest request)` → `StructuralNonlinearStaticOutput`;
- `AnalyzeNonlinearTrussTime(StructuralNonlinearTimeRequest request)` → `StructuralNonlinearTimeOutput`.

Le singole aste definiscono `Area` [m²], `ElasticModulus`, `YieldStress`, `HardeningModulus` [kN/m²]. Solo per la dinamica è obbligatoria `MassPerLength` in kN·s²/m². Vincoli omogenei sono definiti dal normale `(NormalX, NormalY)` e hanno spostamento prescritto nullo. Carichi Fx/Fy sono in kN; `LoadFactors` costituisce la storia di moltiplicatori del medesimo schema nodale. I risultati sono campionati nei passi accettati con ordinamento di nodi/aste uguale a quello dell'input.

## JSON distinto e modalità CLI

Esempi completi:

- `docs/structural/fixtures/m68-nonlinear-static.json`
- `docs/structural/fixtures/m68-nonlinear-time.json`

Il documento usa `title` e un blocco `nonlinearTruss2D`, con `mode: static` oppure `time`, `model` (nodes/members/supports/loads), `loadFactors` e, per il solo modo temporale, `timeStep`, smorzamento opzionale e `initialVelocity` opzionale. I campi sconosciuti vengono rifiutati durante la deserializzazione; il comando deve concordare con `mode`. La CLI non invoca il reader dei modelli statici M01–M67 per questi file.

```powershell
# Statica non lineare
 dotnet run --project src/StructuralSolver2D.Cli -- nonlinear-static docs/structural/fixtures/m68-nonlinear-static.json
 dotnet run --project src/StructuralSolver2D.Cli -- nonlinear-static-report docs/structural/fixtures/m68-nonlinear-static.json reports/nonlinear-static.md
 dotnet run --project src/StructuralSolver2D.Cli -- nonlinear-static-csv docs/structural/fixtures/m68-nonlinear-static.json reports/nonlinear-static-csv

# Dinamica non lineare
 dotnet run --project src/StructuralSolver2D.Cli -- nonlinear-time docs/structural/fixtures/m68-nonlinear-time.json
 dotnet run --project src/StructuralSolver2D.Cli -- nonlinear-time-report docs/structural/fixtures/m68-nonlinear-time.json reports/nonlinear-time.md
 dotnet run --project src/StructuralSolver2D.Cli -- nonlinear-time-csv docs/structural/fixtures/m68-nonlinear-time.json reports/nonlinear-time-csv
```

I CSV sono `nonlinear-nodes.csv` e `nonlinear-members.csv`; la directory contiene anche `nonlinear-status.md` per la visibilità obbligatoria di `Completed` e `Failure`. In caso di non convergenza il servizio conserva **solo i passi convergenti**: i report etichettano chiaramente l'esecuzione come parziale e la CLI esce con codice **2**, anche quando il report parziale viene esportato. Eccezioni di input vengono riportate come errore (codice 1). Nessun rollback manuale di stati o risultati esterni è necessario, poiché ogni esecuzione crea un nuovo solver interno.

## Criteri di accettazione

1. Compilazione .NET 8 Release e regressione completa del codice precedente senza regressioni.
2. Static API: caso con tratto elastico e plastico, correttezza di N, spostamenti, dissipazione e stato; mancata convergenza senza commit del passo fallito.
3. Dynamic API: unità e time-grid, reazioni fisiche, masse positive dichiarate, stato temporale e diagnosi dei casi mancanti.
4. CLI end-to-end Markdown/CSV per entrambe le modalità, round-trip JSON di fixture, schema estraneo non accettato, mode guard, protezione collisione input/output.
5. I report non devono attribuire un significato di verifica di sicurezza strutturale ai risultati. Stati e limitazioni devono essere espliciti.
6. Script `scripts/run-m68-tests.ps1` esegue **come primissima operazione** la cancellazione controllata delle cartelle `bin` e `obj`, poi build, test dedicati, regressione e CLI reali.

## Limiti e cosa rimane a M69

Questa è **ricerca numerica sperimentale** per sole Truss2D a piccole deformazioni, legge bilineare con incrudimento isotropo, geometria iniziale fissa, carichi nodali controllati, vincoli omogenei e dimensioni limitate come da M64/M65. Non gestisce grandi rotazioni, instabilità geometrica, plasticità flessionale, rottura o verifiche normative. I valori energetici dei risultati temporali distinguono energia recuperabile, incrudimento, dissipazione e residuo del bilancio numerico: **un residuo minimo non è garanzia di convergenza temporale**.

M69 introdurrà il viewer interattivo per reticolari non lineari, selezione di passi accettati, colorazione dello stato plastico, grafici N–u e iterazioni, insieme a test visivi in browser reale. M68 non deve essere marcato validato finché l'utente non conferma l'esecuzione della suite .NET 8.
