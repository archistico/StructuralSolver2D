# StructuralSolver2D M89-A — note di rilascio (24/10/2026)

**Baseline confermata dall’utente:** M88-C test superati. **M89-A ancora da compilare e testare su Windows.**

## Differenze rispetto a M88-C
- Nuovo progetto: `src/StructuralSolver2D.Verification.Steel` con la regola **EC3 classe 3, flessione elastica pura**, EN 1993-1-1:2005+A1:2014, 6.2.5(2) Eq (6.14).
- Nuovi test: `tests/StructuralSolver2D.Verification.Tests/M89/ElasticBendingRuleM89ATests.cs`.
- Nuovi script di collaudo e manifest SHA-256.
- **Tutti i sorgenti C# precedenti sono identici a M88-C.** Nessuna modifica a FEM, cataloghi, reporting o CLI.

## Precauzioni
Il risultato `Passed` riguarda **solo** la singola resistenza di sezione con dati richiesti e condizioni esplicite. Il catalogo OPPO è di riferimento, non certificato. Un `DesignCheckReport.OverallStatus = Passed` significa che *tutte le regole richieste* sono passate, non che sia stata completata ogni verifica necessaria al progetto. Non usare M89-A per dichiarare la conformità EC3, NTC o di singoli componenti.

## Gate
Da Windows PowerShell nella directory `StructuralSolver2D`:

```powershell
.\scripts\verify-m89a-manifest.ps1
.\scripts\run-m89a-tests.ps1
```

## Successive attività
M89-B: resistenza assiale con condizioni di area netta e/o taglio validati. M90: sezione di classe 1-4 e interazioni; M91: instabilità globale con ritegni fuori piano verificabili. M92-M94: legno. M95: SLE. M96-M99: integrazione, benchmark esterni e release. GUI C#/Avalonia per ultima.
