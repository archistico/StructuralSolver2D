# Parametric model generators

Milestone 40 introduces small parametric generators for common 2D structural layouts.

The goal is not to replace a graphical editor. The goal is to create repeatable, validated and testable models that can be used by examples, regression tests, JSON export in later milestones and the future viewer.

## Scope

The generators currently create `StructuralModel` instances with:

- nodes;
- members;
- one generic material;
- one generic section;
- basic supports;
- an optional default load case.

Most generators do not add design loads automatically. This keeps geometry generation separate from engineering assumptions. The Gerber beam generator is the only first-step exception because it is explicitly meant to demonstrate asymmetric loading.

## Beam generators

```csharp
var beam = ParametricBeamGenerator.SimplySupportedBeam(
    span: 8.0,
    divisions: 4);
```

Available beam generator methods:

- `SimplySupportedBeam(...)`
- `GerberBeamWithAsymmetricLoads(...)`

The Gerber beam represents internal hinges by applying moment releases to the adjacent frame members.

## Frame generators

```csharp
var frame = ParametricFrameGenerator.PortalFrame(
    bayWidth: 6.0,
    height: 3.5);
```

Available frame generator methods:

- `PortalFrame(...)`

The portal frame uses rigid beam-column joints and fixed column bases.

## Truss generators

```csharp
var truss = ParametricTrussGenerator.NielsenParabolicTruss(
    span: 20.0,
    rise: 4.0,
    panels: 8);
```

Available truss generator methods:

- `PrattBridge(...)`
- `IsostaticTriangularTruss(...)`
- `NielsenParabolicTruss(...)`
- `InvertedParabolicTruss(...)`
- `DoubleDiagonalTruss(...)`

All generated truss members use `MemberType.Truss2D` by default.

## Options

All generators accept `ParametricModelGenerationOptions`:

```csharp
var options = new ParametricModelGenerationOptions
{
    Material = new StructuralMaterial("S355", "Steel S355", 210_000_000.0, 78.5),
    Section = new StructuralSection("CHS", "Example section", 0.012, 9.0e-6),
    LoadCaseId = "G1",
    LoadCaseName = "Permanent actions"
};
```

This allows the caller to reuse the same generator with project-specific material, section and load-case identifiers.

## Design note

The generators intentionally live in `StructuralSolver2D.Core` because they create pure model data. They do not depend on analysis, reporting, persistence, GUI, CAD or viewer code.
