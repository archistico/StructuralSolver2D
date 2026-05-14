# Mesh refinement and convergence benchmarks

This document describes the mesh-refinement validation strategy used by StructuralSolver2D.

Finite element analysis is a discretized approximation of a continuous structural problem. Some quantities, such as support reactions and nodal displacements at explicitly modeled nodes, can match closed-form results very accurately for simple beam problems. Other quantities, especially internal sampled displacements inside an element, depend on the interpolation field and therefore on the mesh.

## Purpose

The mesh-refinement benchmarks are intended to verify that:

- important results remain stable as the mesh is refined;
- critical points such as midspan or point-load positions should be modeled as explicit nodes when exact nodal comparisons are required;
- interpolated internal displacement samples are useful for drawing the deformed shape, but they should not be confused with exact closed-form beam deflections;
- the benchmark catalog can document convergence behavior, not only single expected values.

## Current convergence cases

The initial convergence catalog is stored in:

```text
benchmarks/convergence/
```

It currently includes:

```text
C01-simple-supported-udl-1-elements.json
C01-simple-supported-udl-2-elements.json
C01-simple-supported-udl-4-elements.json
C01-simple-supported-udl-8-elements.json
C02-point-load-single-element.json
C02-point-load-explicit-node.json
```

## C01 — Simply supported beam with uniform distributed load

The same beam is modeled with 1, 2, 4 and 8 elements.

Reference data:

```text
L = 5 m
q = 10 kN/m
E = 210000000 kN/m²
I = 2e-05 m⁴
```

Closed-form reference values:

```text
RA = RB = qL / 2 = 25 kN
Mmax = qL² / 8 = 31.25 kNm
v_mid = -5qL⁴ / (384EI) = -0.0193769841269841 m
```

Important note:

- with one element, the midspan displacement is an internal interpolated value;
- with two or more even divisions, midspan is an explicit node and the nodal value can be compared directly.

## C02 — Simply supported beam with eccentric point load

The same beam is modeled in two ways:

1. point load applied inside a single member;
2. point load applied at an explicit node.

Reference data:

```text
L = 6 m
P = 30 kN
a = 2 m
b = 4 m
```

Closed-form load-point deflection:

```text
v_P = -P a² b² / (3 E I L)
```

This benchmark documents the modeling difference between a member point load and an explicit structural node at the load position.

## Testing strategy

The automated tests check that:

- reactions and maximum bending moment remain consistent across mesh refinement;
- the explicit midspan node improves the midspan deflection comparison;
- further refinement preserves the expected nodal midspan displacement;
- an explicit load-point node improves the load-point displacement comparison.

## Educational note

A finite element model is not the real continuous beam. It is an approximation whose quality depends on:

- element formulation;
- mesh density;
- where structural nodes are placed;
- how loads are represented;
- which quantity is being compared.

For this reason, benchmark files should clearly state whether a checked value is:

- a nodal FEM result;
- an internal interpolated sample;
- a closed-form theoretical value;
- a convergence trend.
