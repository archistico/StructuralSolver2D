# Displacement interpolation and deformed shape sampling

StructuralSolver2D can sample the deformed shape along `Frame2D` members.

The sampled values are useful for:

- drawing a deformed shape;
- creating Markdown reports;
- educational visualization;
- future graphical integration.

---

## What the sampler computes

The current `Frame2D` displacement sampler computes interpolated FEM displacement values from nodal displacements.

For each sampled position it can provide:

```text
u(x)   = local axial displacement
v(x)   = local transverse displacement
rz(x)  = local rotation
Ux(x)  = global X displacement
Uy(x)  = global Y displacement
```

---

## Important limitation

The sampler performs finite-element interpolation of the solved nodal degrees of freedom.

It is not always the same as the exact closed-form internal displacement field of a continuously loaded beam.

This distinction is important.

Example:

- a simply supported beam modeled with a single element under uniform distributed load can have correct reactions and internal forces;
- however, the interpolated midspan displacement from only the end nodal degrees of freedom may not equal the closed-form midspan deflection;
- if midspan deflection is a required benchmark value, the midspan should be modeled as an explicit node.

---

## Nodal results vs internal interpolated results

Nodal displacements are solved directly by the global system.

Internal displacement samples are interpolated from nodal values.

Therefore:

```text
nodal value        = primary FEM unknown
internal sample    = interpolation/post-processing value
closed-form value  = theoretical continuum result, when available
```

These three values can differ depending on discretization and loading.

---

## Mesh refinement

Mesh refinement benchmarks document how the result changes as the number of elements increases.

Typical discretizations:

```text
1 element
2 elements
4 elements
8 elements
```

For many cases, increasing the number of elements improves agreement with the closed-form solution at important points.

See also:

- `docs/structural/mesh-refinement.md`
- `benchmarks/convergence/`

---

## Practical rule for benchmarks

If a benchmark checks a value at a structurally important point, prefer making that point an explicit node.

Examples:

- beam midspan;
- point-load application position;
- expected zero-shear location;
- support or internal hinge;
- free end of a cantilever.

This makes the benchmark more stable, clearer and easier to reason about.
