# M52 validation handoff — 2026-10-22

## Scope

The current slice introduces optional, versioned mass **data contracts**, unit conversions,
separate future modal request/output types, model validation and JSON support.
It does not assemble mass matrices, solve eigenvalues, introduce damping, integrate
in time or perform seismic analysis.

M51: user reported that the previous test package passed. The original test log was
not supplied, so the status records a **user-confirmed** acceptance, not an
independent rerun in this authoring environment.

## Tests added

- `DynamicMassUnitsTests`: kg, kg*m², kN weight, volumetric/line/member mass,
  dimensional numerical benchmarks, rejection of nonfinite/negative/zero and
  overflow/underflow.
- `StructuralDynamicsValidatorTests`: missing member specific weight, invalid
  nodal mass or inertia, unsupported version, duplicate entries, disconnected and
  unknown nodes, truss-only rotational inertia, structural prerequisites and JSON
  roundtrip of dynamic metadata.
- `ModalContractTests`: separate request, mode count and reserved result units.
- `DynamicsJsonContractTests`: optional/legacy JSON, strict mass parsing,
  unsupported fields/versions and comparison of **actual static solver outputs**
  for otherwise-identical inputs with and without dynamics metadata.

## Local checks performed

- Example JSON parsed with Python's JSON parser; schema fields and numerical
  reference conversion values reviewed independently.
- Source and documentation consistency reviewed manually.
- **Not run:** .NET 8 compilation, xUnit tests, browser/viewer regressions.
  The `.NET` SDK is not installed in this environment.

## Next acceptance gate

On Windows, from the extracted project, run:

```powershell
.\scripts\run-m52-tests.ps1
```

It builds the complete solution, runs focused Core, Analysis and CLI M52 tests,
then runs `dotnet test StructuralSolver2D.sln`. Record the command output if
failures occur. After that gate is green, update the M52 status to completed
and proceed to M53 without changing static output behavior.
