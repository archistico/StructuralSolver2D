# Modal-analysis validation — M56 checkpoint (2026-10-23)

M55 eigensolver passed the user's .NET acceptance suite. M56 adds a **separate
analytical benchmark and convergence gate**, currently awaiting revalidation on Windows after the M56-FIX1 aliasing correction.
See `benchmarks/modal/README.md` for continuum reference equations, 14 beam/bar
geometry fixtures, two-story shear matrix, frequency tables, tests and accepted
model limitations. The modal computation remains **internal** until M57.

**Mode-family / spatial-aliasing warning (M56-FIX1):** In a Frame2D model,
longitudinal and flexural modes interleave. Worse, on a coarse beam mesh the
third and a higher bending mode can have identical *nodal Uy* up to scale and
sign. The first Windows M56 run reported 27/28 tests passing, with N=4 third
bending target 323.111 Hz incorrectly matched to a 1013.397 Hz higher mode.
The M56-FIX1 test now computes a MAC on interior Hermite interpolated deflections
using both Uy and Rz, and filters predominantly axial modes by their sampled
axial/transverse contributions. This disambiguates the 329.015 Hz third bending
mode (1.827% from continuum theory) without loosening any acceptance tolerance.
The method is a benchmark mode-shape discriminator, not a public modal API or
continuous mass-weighted MAC. The new regression test explicitly recreates the
nodal alias and verifies that interior sampling separates the modes.

**Evidence policy:** reproduced analytical formula/fixture checks and separate
SciPy reference FEM estimates have passed here, **but the C# full .NET 8 tests
have not been run in this packaging environment**. Only user execution of
`scripts/run-m56-tests.ps1` can complete the gate.

---

# Modal-analysis preparation — M54 boundary conditions and admissible DOFs

**Historical M54 checkpoint (2026-10-22):** This section documents the original
M54 boundary-condition implementation. M51–M55 are now user-test-validated and
M56 independent benchmark testing is the current gate. The internal M55 module
calculates modal eigenpairs; **no public modal API, CLI or seismic analysis is enabled**.

## Mathematical formulation and physical support geometry

M53 assembles unconstrained global stiffness `K` and consistent mass `M` in DOF
order `[Ux, Uy, Rz]` per node. M54 uses the **same homogeneous support directions**
as the production static solver, including `OrientationDegrees`:

- `RestrainedUx` adds `cos(θ) Ux + sin(θ) Uy = 0`;
- `RestrainedUy` adds `-sin(θ) Ux + cos(θ) Uy = 0`;
- `RestrainedRz` adds `Rz = 0`.

Repeated directions are reduced to an independent set. The number of redundant
support directions is reported independently (the static support builder already
removes duplicates). The reducer also removes exactly zero-stiffness **and**
zero-mass uncoupled rotations of truss-only nodes. It does **not** discard
unrestrained translations or rotational DOFs with nonzero stiffness coupling.

A double-reorthogonalized orthonormal basis `Z` is constructed for the nullspace
of the complete homogeneous constraint matrix `C`, such that `C Z ≈ 0` and
`Zᵀ Z ≈ I`. The matrices used for future modal analysis are:

```
K_reduced = Zᵀ K Z
M_reduced = Zᵀ M Z
u_global  = Z q
```

For any admissible `q`, the kinetic and strain quadratic forms are retained:
`uᵀ M u = qᵀ M_reduced q` and `uᵀ K u = qᵀ K_reduced q`. The production static
solver's augmented Lagrange-multiplier system is NOT used for the eigenproblem.
The global support reactions and static analysis API remain unchanged.

## Diagnostic/eligibility policy

`DynamicBoundaryConditionReducer.AssembleAndReduce` is an **internal**, opt-in
entry point. It returns `DynamicReductionResult` and a stable diagnostic code;
only `Status == Ready` indicates an admissible **candidate** for the subsequent
M55 eigensolver, **not** verified modal or structural safety results.

| Code | Meaning / deliberate policy |
| --- | --- |
| `M54_READY` | Finite symmetric reduced pair with diagnostic SPD mass/stiffness |
| `M54_FULLY_CONSTRAINED` | No free physical coordinate remains |
| `M54_DENSE_LIMIT` | More than 120 free DOFs; dense experimental reduction deferred |
| `M54_INVALID_MATRIX`, `M54_INVALID_REDUCED_MATRIX` | Nonfinite, asymmetric or invalid matrix |
| `M54_INDEFINITE_MASS`, `M54_INDEFINITE_STIFFNESS` | Matrix violates expected PSD condition |
| `M54_SINGULAR_MASS` | Zero-inertia admissible motion; eigenanalysis blocked |
| `M54_MASSLESS_STIFFNESS_COUPLING` | Example: lumped frame rotation has stiffness but zero mass |
| `M54_MASS_NEAR_SINGULAR` | Suspicious normalized mass pivots |
| `M54_FREE_FREE_DEFERRED` | Expected unconstrained rigid-body modes; explicit M55+ policy required |
| `M54_RIGID_BODY_MOTION` | At least one rigid-body motion is still unconstrained |
| `M54_INTERNAL_MECHANISM` | More stiffness-null directions than admissible global rigid motions |
| `M54_STIFFNESS_NEAR_SINGULAR` | Suspicious normalized stiffness pivots |

For a free-free connected frame, three global planar rigid-body directions
remain. M54 identifies them but intentionally does not produce or filter
zero-frequency eigenmodes. Structural mechanisms and disconnected components
require further modeling judgment. Massless, stiffness-coupled algebraic DOFs
need a *validated dynamic static-condensation rule* (for example, a Schur
complement when the massless block is invertible and inertia is uncoupled), or
must remain unsupported. M54 chooses **explicit rejection** rather than an
untested modification of K and M. Educational lumped beam masses are thus not
modal-ready; consistent mass is the default.

M53 still rejects moment-released frames with `M53_RELEASE_UNSUPPORTED`; their
consistent dynamic representation needs internal DOFs or another validated
formulation. The reducer never guesses a release mass matrix.

## Rank and conditioning limitations

The small-dense diagnostic uses diagonal scaling and diagonal-pivoted positive
semidefinite factorization to estimate matrix rank and nullity. Ratios of
normalized pivots are **numerical warnings, not true condition numbers** and
not structural safety checks. The tolerance can conservatively classify very
weak but physically stable directions as mechanisms, especially near degeneracy.
The basis uses modified Gram–Schmidt with two passes; it does not replace
well-conditioned QR/SVD algorithms for large/highly constrained systems.
Positive definiteness must be checked again with the method chosen for the M55
symmetric generalized eigenproblem. Duplicated restraints are handled, but
independence of nearly coincident support directions is tolerance-dependent.

No time integration, response spectrum, safety classification, forces from
modal amplitudes, or seismic code checks are provided.

## Test acceptance before M55

Run `scripts/run-m54-tests.ps1` using .NET 8. Tests include actual cantilever
and truss assemblies; independent inclined-support quadratic energies; duplicate
and redundant constraints; physical free-free rigid modes; unrestrained rigid
motion versus internal mechanism; all-restrained structures; lumped-beam
massless rotations; unconnected translations; indefinite/invalid matrices; and
mixed-angle supports. Run the entire project regression suite, not only M54 tests.

Once accepted, M55 can adopt a tested symmetric generalized eigensolver and
use `Basis` to lift eigenvectors back to the original DOF space. Do not report
any modal frequency until the M55/M56 benchmark gate is complete.

Source context: A. Carpinteri, *Dinamica delle strutture*, chapters 1–4, and
the project's M51–M53 formulations. The M54 numerical reduction and acceptance
policies are project engineering decisions, not a reproduction of book formulas.

---

## M55 — Experimental generalized symmetric eigenproblem (internal only)

**2026-10-23 status:** M54 .NET build and tests reported passing by the user.
The M55 implementation is written but still requires .NET 8 compilation and
full regression; the **separate M56 independent modal-reference and mesh
convergence acceptance gate remains outstanding**. Nothing in M55 enables a
public modal API, CLI command, reports, seismic design checks or real-time
animation. The existing M52 public request/output *contracts* remain data-only.

### Algorithm / units

The solver accepts **only** `DynamicReductionResult` with status `Ready`; any
M54 rejection (free-free, internal mechanism, indefinite stiffness, singular
mass, massless stiffness-coupled DOF, unresolved near-singularity, etc.) is
retained as a refusal, not turned into a nominal natural frequency. The pair
comes from M53 consistent element masses and M54 admissible projection.

1. `K_r`, `M_r` must be finite, symmetric and dimensionally consistent. Define
   the diagonal scaling `D_ii = 1/sqrt((M_r)_ii)` and form
   `K_bar = D K_r D`, `M_bar = D M_r D`. A separate positive scalar `s`
   (maximum absolute entry of `K_bar`) makes `K_bar/s` well scaled.
2. Verify `M_bar` is **strictly SPD** by a guarded lower-triangular Cholesky
   factorization, `M_bar = L Lᵀ`, with no invented diagonal regularizer.
3. Compute the symmetric ordinary eigenproblem
   `B y = eta y`, `B = L^-1 (K_bar/s) L^-T` using explicit triangular
   solves (not an inverse of the original `M`). Find the orthonormal `y`
   basis using cyclic **symmetric Jacobi rotations**, limited to 80 sweeps;
   reject non-convergence.
4. The recovered reduced shape is `phi = D L^-T y`, with
   `lambda = s eta = omega²` and `omega = sqrt(lambda)` in rad/s;
   `f = omega/(2*pi)` in Hz; `T = 1/f` in seconds.
5. Normalize each reduced mode to `phiᵀ M_r phi = 1` and map it back to the
   original model DOFs with `phi_global = Z phi`. The displayed/returned sign
   is fixed by making the largest magnitude global component positive; this is
   for *repeatability*, not a physical sign or amplitude measurement.
6. Check each eigenpair's scale-aware relative residual
   `||K_r phi - lambda M_r phi|| / (||K_r phi|| + lambda ||M_r phi||)`;
   check `lambda ≈ R(phi)` where `R(q)=qᵀK_r q / (qᵀM_r q)`;
   check mass orthogonality and stiffness orthogonality for all modes returned.

The implementation is **pure C#** with no new NuGet dependencies or license
changes. Dense cyclic Jacobi is appropriate as a transparent, testable baseline
for the current `<=120` reduced-DOF experimental limit, not a replacement for a
well-optimized large sparse or high-accuracy eigensolver. Cholesky and symmetric
Jacobi are deliberately distinct from the static Gaussian equations solver.
The mass normalization uses the project's existing kN–m–s internal convention:
the numeric global vectors therefore have *mass-normalized* units, and are **not**
operating displacements.

### Numerical acceptance and limits

- Only the first `min(requestedModeCount, availableDofs)` strictly positive,
  finite eigenpairs are returned. Nonpositive/tiny eigenvalues are rejected:
  no rigid-body modes are filtered silently, and a near-zero eigenvalue is
  not misreported as a positive physical frequency.
- Symmetric Jacobi convergence target: relative Frobenius off-diagonal norm
  `1e-13`; maximum 80 sweeps. The eigenvalue and modal residual/orthogonality
  tolerances are explicit in the source. Close/repeated eigenvalues have a
  **well-defined invariant eigenspace** but **non-unique individual vectors**;
  no test or result should infer uniqueness of those shapes.
- The relative residual and Rayleigh consistency limit is `5e-7` for the
  experimental dense solver. This is **not** an accuracy guarantee for modal
  frequencies or a substitute for analytic benchmarks and mesh refinement.
- The matrix diagonals and Cholesky pivots are inspected again even for
  M54-ready input. Extremely extreme mixed magnitudes can overflow or lose
  significant digits; such cases are rejected rather than 'corrected' with
  fake masses, modified stiffness or invented eigenvalues.
- M55 deliberately does **not** implement released-frame dynamics, lumped
  beam-mass condensation, free-free modes, transient excitation, damping,
  nonlinear behavior or normative seismic verification.

### Internal use and tests

The internal entry points are:

```csharp
var reduced = DynamicBoundaryConditionReducer.AssembleAndReduce(model, dynamics);
var spectrum = SymmetricGeneralizedModalSolver.Solve(reduced, requestedModeCount: 3);
// Internal testing/validation only, not available via CLI or StructuralSolver2DService.
```

`SymmetricGeneralizedModalSolver.AssembleAndSolve(model, dynamics, 3)` is a
shorter internal equivalent. `ExperimentalModalSpectrum` contains computed
modes, each with `lambda`, angular frequency, Hz, period, global and reduced
mass-normalized vectors, Rayleigh quotient and relative residual, plus maximum
pairwise mass/stiffness orthogonality error.

M55 focused tests include analytical two-DOF shear building, off-diagonal
consistent masses, repeated eigenvalues, stiffness/mass scaling, non-coordinate
aligned projection, Rayleigh bounds, dense 24-DOF example, and actual frame
assembly with inclined supports. These are **unit/integration smoke and
analytical checks**. M56 expands them to independent beam/cantilever/truss
solutions, explicit mesh-convergence data, model invariances and separate
failure-path benchmarks. Run `scripts/run-m55-tests.ps1` before considering the
M55 implementation accepted; retain M56 as a separate mandatory quality gate.
