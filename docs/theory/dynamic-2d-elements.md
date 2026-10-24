# Dynamic extension — mass definitions and M53 design notes

**M52 status: data contract and unit validation only, not a dynamic solver.**

## Mass and static loads are different

The solver uses kN, m and s. A material's `UnitWeight` represents force/volume;
`rho=gamma/g0` represents inertial mass/volume. A member of length L and area A
therefore carries `m=gamma*A*L/g0`. Do not multiply nodal masses by `g0` inside
a mass matrix, and do not generate static self-weight from `UnitWeight` implicitly.

The mass definition is deliberately separate from `StructuralModel`, so existing
static requests and JSON files are not affected. User-supplied nodal masses add
equipment inertia to member distributed masses; both translational directions use
the same mass in v1. Distinct rotational inertia has units `kN*s²*m` and is never
inferred merely from `Area` or `MomentOfInertia`.

## Engineering constraints for M53–M54

For a two-node Euler–Bernoulli frame, consistent mass comes from the kinetic-energy
form integrated over member length using the relevant axial and transverse interpolation
functions. For a truss, only translational DOFs are physical. Rotations associated with
truss-only nodes may not have inertia or stiffness; an eigenvalue solver must reduce
these structurally unused DOFs, not regularize them with fake diagonal terms.

The mass-matrix transformation must use the same actual global/local convention as
current stiffness matrices. Reversed member ordering and inclined members need
independent benchmarks. End moment releases in the **static** stiffness computation
are condensed algebraically; simply applying the same static condensation to a
consistent mass matrix is not dynamically valid. Until a sound approach is proven,
dynamic requests involving released frame members must be rejected before assembly.

A single nodal mass is **not** mass per attached member; it must be assembled only once
per node. Automatic member masses must be assembled separately exactly once each.

## Subsequent stages

M53: consistent element matrices and global mass assembly; M54: dynamic constraints,
mechanisms and massless DOFs; M55–M56: eigenvalue solution and benchmark convergence;
M57: viewer. Modal shapes are arbitrarily normalized and must never be reported as
physical displacements without an independently specified amplitude.

Reference for the underlying principles: A. Carpinteri, *Dinamica delle strutture*,
chapters 1–4. This source is not adopted as a contemporary design-code reference.

## M53 implementation: solver-independent mass and stiffness assembly (2026-10-22)

**Status: implemented, awaiting .NET 8 acceptance.** M52 dynamic metadata and units
were user-validated before starting M53. The M53 assembly is **internal and opt-in**;
no static command, JSON format, public service method or eigenvalue solver is modified.

### Truss2D: consistent translational mass

With `mu = UnitWeight * A / g0` [kN*s²/m²], L [m], and the physical global DOFs
`[Ux1, Uy1, Ux2, Uy2]`, use linearly interpolated **in-plane translational**
velocity to obtain:

```text
M_truss = mu*L/6 * [ 2 0 1 0 ]
                   [ 0 2 0 1 ]
                   [ 1 0 2 0 ]
                   [ 0 1 0 2 ]
```

Unlike axial stiffness, transverse translational inertia is physical. A truss adds
**no nodal rotational inertia**. No orientation correction is needed for this
matrix in isotropic global translation coordinates; the truss stiffness still uses
the existing directional cosines.

### Frame2D: Euler–Bernoulli consistent mass

DOF order `[u1, v1, theta1, u2, v2, theta2]`. Linear axial interpolation and
cubic Hermite transverse interpolation produce:

```text
M_frame,local = mu*L/420 *
  [140    0       0      70    0        0   ]
  [  0  156     22L       0   54      -13L ]
  [  0  22L     4L²       0  13L      -3L² ]
  [ 70    0       0     140    0        0   ]
  [  0   54     13L       0  156      -22L ]
  [  0 -13L    -3L²       0 -22L       4L² ]
```

`u_local=T*u_global` is the existing stiffness convention, so `M_global=Tᵀ*M_local*T`.
The interpolated transverse velocity produces bending-rotation entries; these
are *not* independently specified cross-section rotary inertia of a Timoshenko
beam. The v1/v2/u1/u2 terms, unlike theta terms, have direct mass units.

### Lumped matrices — education only

The optional internal `EducationalLumped` path places `mu*L/2` on each
translational DOF at either end (zero entries for frame rotations). It preserves
rigid-translation total mass but has **singular frame rotational inertia** and
must not be passed unmodified to a generalized eigenvalue solver. No automatic
switch from consistent to lumped formulation occurs.

### Global assembly and limitations

`DynamicMatrixAssembler.Assemble(model, dynamics)` validates the M52 contract,
uses the same stiffness element builders and DOF ordering as the mixed-plane
static analyzer, and adds each member matrix **once**. Each extra nodal mass is
added **once at its node**, isotropically to Ux and Uy, with optional Rz inertia.
The result carries raw K and M, node index map, physical mass totals and a list
of **zero-diagonal-mass DOF indices** as *diagnostics*, not reduced eigen-DOFs.
It neither assembles load cases nor adds gravitational self-weight.

- Any member with an end-moment release is rejected with
  `M53_RELEASE_UNSUPPORTED`, before a matrix is returned. Static end-release
  condensation cannot be copied to the dynamic mass matrix; a later
  validated formulation with suitable internal DOFs is required.
- Massless truss-only rotations are **not** stabilized by fake diagonal values;
  their correct deletion/constraint projection is the M54 task.
- Support transformations, redundant constraints, rigid-body modes and massless
  algebraic elimination remain explicitly **M54**, not M53.
- No modal frequencies, periods, modes, seismic forces or engineering safety
  results can be obtained from M53 alone.

### Independent acceptance checks

`tests/StructuralSolver2D.Analysis.Tests/Dynamics/ElementMassMatrixTests.cs` uses
five-point Gauss quadrature to independently integrate the frame shape functions;
other tests check rigid translations, rigid rotation kinetic energy, symmetry,
random energy vectors and basis invariance. Assembly tests cover reversed
members, subdivision, mixed frame–truss models, additional nodal masses,
truss-only massless rotations, unsupported releases and absent unit weight.
Run `scripts/run-m53-tests.ps1` on .NET 8 Windows for focused and full regression.

Bibliographic context: A. Carpinteri, *Dinamica delle strutture*, chapters 2–4,
with implementation formulas and tests documented independently here.
