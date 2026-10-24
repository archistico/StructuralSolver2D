# M51 — Independent reduced-stiffness reference cases

This companion to `tests/StructuralSolver2D.Analysis.Tests/Verification/ConstrainedStiffnessInspectorTests.cs` documents elementary *exact* spectra. They are diagnostic fixtures, **not** member strength or buckling checks.

For a homogeneous constraint matrix `C`, use a basis `Z` with `C Z = 0`, then `Kr = Zᵀ K Z` and `S = D⁻¹/² Kr D⁻¹/²` where `D = diag(Kr)`. The indicator is `min(eigenvalues(S)) / max(eigenvalues(S))`. `S` is a *numerical* normalization, not a physical stiffness spectrum or modal solution.

| Case | Matrix `K` | Constraints | Exact eigenvalues of `S` | Ratio | Expected classification |
| --- | --- | --- | --- | --- | --- |
| SPD | `[[2,-1],[-1,2]]` | none | `1/2`, `3/2` | `1/3` | Healthy |
| rigid translation | `[[2,-2],[-2,2]]` | none | `0`, `2` | `0` | PossibleMechanism |
| indefinite (positive diagonals) | `[[1,2],[2,1]]` | none | `-1`, `3` | `-1/3` | Indefinite |
| weak positive eigenvalue | `[[1,1-10⁻⁷],[1-10⁻⁷,1]]` | none | `10⁻⁷`, `2-10⁻⁷` | about `5·10⁻⁸` | PoorlyConditioned |
| numerically nearly singular | `[[1,1-10⁻¹³],[1-10⁻¹³,1]]` | none | `10⁻¹³`, `2-10⁻¹³` | about `5·10⁻¹⁴` | PossibleMechanism |
| inclined support | `[[4,1],[1,4]]` | `uₓ+uᵧ=0` | `1` (one free DOF after diagonal normalization) | `1` | Healthy |

Extra invariants:

- Uniformly multiplying `K` by any *positive* factor leaves `S` unchanged (test `10⁻¹⁰⁰`, `1`, `10¹⁰⁰`).
- Duplicating a constraint row does not remove further free DOFs or introduce an artificial singularity.
- All DOFs restrained means zero free DOFs; the correct status is `FullyConstrained`, **not** `Healthy` or `PossibleMechanism`.
- The optional inspection returns `TooLarge` when more than 60 unrestrained DOFs are present; it never rejects the production static solve.

Run `scripts/run-m51-tests.ps1` from PowerShell with .NET 8 SDK installed. The written reference spectra have been cross-checked independently using a numerical eigensolver during authoring, **but this is not evidence that the new C# tests compiled or passed**.
