# M53 — independent numerical check (reference computations)

**2026-10-22, not a .NET execution report.** The values below were calculated
independently in Python/NumPy from the frame interpolation functions and the
published Euler–Bernoulli matrix expression. They verify theoretical reference
values; they **do not** establish that the C# code builds or passes its xUnit suite.

- Frame line mass `mu = 1.7` kN·s²/m², length `L = 4.25` m.
- Maximum absolute coefficient difference: **8.882e-16**
  (analytic 6×6 matrix versus five-point Gauss–Legendre quadrature of axial and
  Hermite transverse interpolation functions).
- Minimum matrix eigenvalue for this example: **0.0558580351771**
  (positive).
- Rigid rotation velocity pattern `[0,0,1,0,L,1]`: quadratic form
  **43.5005208333**; analytical expected `mu*L³/3 = 43.5005208333`.
- Max reversal-orientation matrix discrepancy across four orientations:
  **0.000e+00** (permuted end-node ordering).

### Remaining acceptance gate

On the user's Windows machine, execute `scripts/run-m53-tests.ps1` to compile the
solution, run focused dynamics tests and then the complete .NET regressions.
M53 is not complete until those pass. The dynamic assembly contains no
boundary-condition projector or modal eigenvalue solver (M54+).
