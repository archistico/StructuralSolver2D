# StructuralSolver2D G7 — validazione selezionata, esempi FEM e audit integrato

**Data:** 24 ottobre 2026. **Baseline G6:** test superati su Windows (comunicato dall’utente). **G7:** pronto per i test, non collaudato in questo ambiente.

### Aggiunte

- `benchmarks/beams/b13-overhang.json`, `b14-overhang.json`: due modelli isostatici su sbalzo con carichi distribuiti a tratti.
- `benchmarks/expected/expected-results.json`: due nuove voci B13/B14; file preesistente esteso **senza cambiare le attese dei benchmark precedenti**.
- `benchmarks/G7_INDEPENDENT_GOLDEN.json`: origine analitica e risultati di riferimento derivati indipendentemente.
- `tests/StructuralSolver2D.Analysis.Tests/Benchmarks/G7OverhangIndependentEquilibriumTests.cs`: prove analitiche autonome, reazione negativa e differenze fra schemi di carico.
- `tests/StructuralSolver2D.Verification.Tests/M92/NtcTimberValidationPipelineG7Tests.cs`: test integrati con i **veri** selettori/regole G1-G3 e auditor G6, non con falsi report precompilati.
- `docs/G7_MANUALE_VALIDAZIONE_INDIPENDENTE.md`, `G7_IMPLEMENTATION_CHECKLIST.md`: manuale, tracciabilità delle fonti e ambiti mancanti.
- `scripts/run-g7-tests.ps1`, `scripts/verify-g7-manifest.ps1`.

### Fuori G7

Nessuna modifica al motore FEM, alle regole NTC G1–G5, all'auditor G6, alle verifiche acciaio EC3 M89–M91 o alla GUI. **Non significa conformità completa del membro/struttura.**

### Collaudo

```powershell
.\scripts\verify-g7-manifest.ps1
.\scripts\run-g7-tests.ps1
```
