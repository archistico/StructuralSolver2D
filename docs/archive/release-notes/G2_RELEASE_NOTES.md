# StructuralSolver2D G2 — rilascio da collaudare

**Baseline G1 FIX1, che l'utente ha confermato supera il gate Windows.** G2 implementa quattro verifiche di sezione PURA del legno NTC 2018, aggiunge test xUnit e nessuna modifica al motore FEM o alle regole EC5 precedenti.

File principali:
- `src/StructuralSolver2D.Verification.Timber/Ntc2018TimberRectangularSectionRulesG2.cs`
- `tests/StructuralSolver2D.Verification.Tests/M92/Ntc2018TimberRectangularSectionG2Tests.cs`
- `docs/structural/G2_NTC2018_RESISTENZE_PURE_SEZIONI_LEGNO.md`
- `scripts/run-g2-tests.ps1`
- `scripts/verify-g2-manifest.ps1`

Il profilo G1 fornisce coefficienti ma **non autorizza** l'uso delle regole EC5. Queste nuove regole sono native NTC e continuano a NON certificare interazioni, instabilità o conformità complessiva. Compilazione .NET 8 e test: **in attesa del log Windows dell'utente**.
