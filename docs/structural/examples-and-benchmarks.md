# Examples and Benchmarks

StructuralSolver2D keeps examples and benchmarks separate.

This distinction is important because the two folders have different goals.

## Examples

Examples are user-facing input files intended to show how the solver can be used from the CLI.

They should be:

- readable;
- small;
- easy to modify;
- useful for learning the JSON model format;
- suitable for generating reports.

Recommended structure:

```text
examples/
  beams/
  trusses/
  mixed/
  combinations/
```

Example commands:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json
dotnet run --project src\StructuralSolver2D.Cli -- report examples\beams\simple-supported-beam.json reports\simple-supported-beam.md
```

Examples do not need to include all validation metadata. They are meant for users.

## Benchmarks

Benchmarks are validation and regression cases.

They should be:

- precise;
- documented;
- associated with expected results;
- automatically tested where possible;
- traceable to formulas, hand checks, internal regression cases or external references.

Recommended structure:

```text
benchmarks/
  beams/
  frames/
  trusses/
  mixed/
  convergence/
  professional/
  expected/
```

Benchmark files are consumed by the automated benchmark runner through:

```text
benchmarks/expected/expected-results.json
```

## Rule

Use `examples/` to explain how to use the solver.

Use `benchmarks/` to prove that the solver is correct and remains correct over time.

## Adding a new example

When adding a new user-facing example:

1. place it in the appropriate `examples/` subfolder;
2. keep it small and readable;
3. add a short description in `examples/README.md` if it introduces a new concept;
4. make sure it runs with the CLI.

## Adding a new benchmark

When adding a new validation benchmark:

1. place the model file in the appropriate `benchmarks/` subfolder;
2. add expected values to `benchmarks/expected/expected-results.json`;
3. document the case in `benchmarks/expected/expected-results.md` where appropriate;
4. ensure `dotnet test` runs the benchmark automatically;
5. include tolerances and explain what the benchmark validates.

## Legacy flat examples

Earlier milestones used flat example paths such as:

```text
examples/simple-supported-beam.json
```

The categorized folders are now the preferred structure, for example:

```text
examples/beams/simple-supported-beam.json
```

Existing flat files may remain temporarily for compatibility, but new examples should use the categorized layout.
