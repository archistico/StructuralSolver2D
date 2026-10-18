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

All dimensions must be positive and finite. Zero, negative values, NaN and infinity are rejected with `ArgumentOutOfRangeException`.

Beam interpolation and parabolic ordinates normalize the position before multiplying large dimensions, avoiding overflow when the final coordinate is representable. Rounded coincident panel/division nodes, a collapsed curved-chord ordinate, and Gerber span endpoints that overflow or collapse onto the preceding endpoint are rejected with `ArgumentOutOfRangeException`. Member lengths outside the numeric range are diagnosed by the model validator.

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
Its suspended middle span rests between two outer cantilevers. The left end restrains `Ux`, `Uy` and `Rz`;
the right end restrains `Uy` and `Rz`, allowing `Ux` to slide. These five restraints stabilize the two internal hinges without adding an axial redundancy.

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

The parabolic generators share the deck and curved-chord end nodes: the supports are `B0` and `B{panels}`,
and `T1` through `T{panels - 1}` are internal curved-chord nodes. End panels are triangles, so they do not
receive duplicate diagonal members. Existing callers referring to `T0` or `T{panels}` should use the corresponding `B` node.

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
