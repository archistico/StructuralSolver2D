# M51 — Stato della validazione e istruzioni di ripresa

**Pacchetto:** implementazione M51, seconda tranche (diagnostica dei meccanismi), 22 ottobre 2026.

**Stato:** `IN CORSO`, **non dichiarare completato**. Non è stato possibile eseguire `dotnet build` e `dotnet test` nell'ambiente di preparazione perché il .NET SDK 8 non è disponibile. Eventuali file `.trx` già presenti nel pacchetto provengono dai checkpoint precedenti e **non attestano** l'esito dei nuovi test.

## Modifiche della seconda tranche

- `src/StructuralSolver2D.Analysis/Verification/ConstrainedStiffnessInspector.cs`: ispezione interna del sottospazio degli spostamenti ammissibili, classificazioni esplicite, rapporto tra autovalori su matrice equilibrata, indicazione del DOF dominante; massimo 60 DOF liberi.
- `src/StructuralSolver2D.Analysis/PlaneStructure2D/PlaneStructureAnalyzer.cs`: nuovo accesso **interno** `InspectStaticStability` che usa l'assemblaggio esistente e non tenta di risolvere matrici singolari.
- `tests/StructuralSolver2D.Analysis.Tests/Verification/ConstrainedStiffnessInspectorTests.cs`: test per matrici con soluzione nota e modelli strutturali reali.
- `tests/StructuralSolver2D.Analysis.Tests/Verification/StaticEnergyVerificationTests.cs`: ulteriori confronti analitici di mensola con proprietà e lunghezze differenti.
- `benchmarks/verification/stability-inspector-reference.md`: autovalori e rapporti attesi ricavabili analiticamente.
- Roadmap, teoria FEM, sviluppo, handoff, changelog e script dei test aggiornati.

## Verifiche effettuate durante la preparazione

- Presenza/coerenza dei riferimenti interni verificata per ispezione statica.
- Controllo lessicale di parentesi e delimitatori C# effettuato sui sorgenti dell'area Analysis/Verification (non sostituisce il compilatore).
- Autovalori analitici 2×2 confrontati con un calcolo numerico indipendente, **senza eseguire il codice C#**.
- Lo ZIP deve essere controllato integralmente (CRC) e confrontato con l'archivio precedente per garantire che nessun file venga rimosso.

## Validazione da eseguire con .NET 8

In PowerShell, nella root estratta:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-m51-tests.ps1
```

Lo script deve eseguire build Release, suite M51 mirata (invarianti elementi, energia, diagnostica), poi **tutta** la suite `.sln`. In caso di fallimenti: registrare errore, scenario, stack trace e commit/file modificato; non ritoccare soglie per rendere verdi i test senza dimostrazione numerica. Confrontare anche output PDF/HTML/browser contro i baseline già inclusi, ove necessitano tool di test aggiuntivi.

## Gates per il prossimo step

1. Compilazione .NET 8 riuscita.
2. Nuovi e precedenti test verdi; classificazioni diagnostiche sensibili a scale/collinearità rivalutate sui casi reali.
3. Regressioni di `Frame2DAnalyzer`, `Truss2DAnalyzer`, `PlaneStructureAnalyzer`, CLI e Reporting confermate.
4. Solo dopo questi controlli si può chiudere M51 e iniziare la modellazione delle masse M52, senza confondere `UnitWeight` con densità di massa.
