# Two-storey steel house with timber pitched roof

This example is a **design-oriented** 2D frame intended to show a realistic workflow for StructuralSolver2D.

It models:

- four steel columns along the frame axis;
- bays `5 m + 2 m + 5 m`;
- two storeys with `3 m` interstorey height;
- two first-floor balconies as `1.2 m` cantilevers;
- a timber pitched roof with ridge at the frame centre;
- manual permanent, live, snow and wind actions;
- manual ULS/SLS combinations for first sizing.

## Geometry

- Columns at `x = 0, 5, 7, 12 m`
- Storey levels at `y = 0, 3, 6 m`
- Roof ridge at `x = 6 m`, `y = 8 m`
- Tributary depth for load conversion: `4.0 m`

## Provisional sections

These sections are **starting sections**, not optimized final design.

- Steel columns: `HEB 220`
- Steel room beams: `IPE 330`
- Steel corridor and balcony beams: `IPE 220`
- Timber rafters: `GL24h 160x360 mm`

## Adopted area loads

### Floors

- permanent load on internal floors: `5.5 kN/m²`
- room live load: `2.0 kN/m²`
- corridor live load: `3.0 kN/m²`

Converted to line loads with tributary depth `4.0 m`:

- internal floor permanent load: `22.0 kN/m`
- room live load: `8.0 kN/m`
- corridor live load: `12.0 kN/m`

### Balconies

- permanent load: `6.0 kN/m²` -> `24.0 kN/m`
- live load: `4.0 kN/m²` -> `16.0 kN/m`

### Roof

The roof slope is approximated with a rise of `2.0 m` over a horizontal half-span of `6.0 m`.

For a horizontal area load `p`, the vertical uniform line load on the sloped rafter is approximated as:

`q = p * tributaryDepth * cos(theta)`

with `cos(theta) ≈ 0.949`.

The example uses:

- roof permanent load `1.5 kN/m²` -> `5.7 kN/m`
- snow load `1.5 kN/m²` -> `5.7 kN/m`
- roof uplift for wind: `+2.5 kN/m`

### Wind

Wind is represented here by **equivalent nodal horizontal forces** at each floor level and at the ridge.
This is practical for a first frame design example, but it is still a simplification.

## Manual combinations included

- `ULS_GRAVITY`
- `ULS_WIND_POS`
- `ULS_WIND_NEG`
- `SLS_RARE`

These are **illustrative manual combinations** prepared to extract forces and deformations from the frame. They are not an automatic normative generator.

## Suggested workflow

1. Analyze `ULS_GRAVITY` to size room beams, corridor beam, balconies and roof rafters under downward actions.
2. Analyze `ULS_WIND_POS` and `ULS_WIND_NEG` to size columns and verify frame sway effects.
3. Analyze `SLS_RARE` to inspect deflections.
4. Use the resulting force envelopes to drive steel and timber verification modules.
5. Iterate section sizes if needed.

## CLI commands

```powershell
# quick analysis with default combination
 dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed\two-storey-steel-house-timber-roof.json

# explicit combinations
 dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed\two-storey-steel-house-timber-roof.json ULS_GRAVITY
 dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed\two-storey-steel-house-timber-roof.json ULS_WIND_POS
 dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed\two-storey-steel-house-timber-roof.json ULS_WIND_NEG
 dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed\two-storey-steel-house-timber-roof.json SLS_RARE

# report and viewer
 dotnet run --project src\StructuralSolver2D.Cli -- report examples\mixed\two-storey-steel-house-timber-roof.json reports\house-uls-gravity.md ULS_GRAVITY
 dotnet run --project src\StructuralSolver2D.Cli -- export-viewer examples\mixed\two-storey-steel-house-timber-roof.json reports\house-wind-pos.html ULS_WIND_POS
```

## Important limits

This is still a **2D frame** example.

It does **not** automatically prove:

- slab diaphragm behaviour;
- 3D load redistribution;
- local connection design;
- out-of-plane stability of members;
- unilateral contact behaviour;
- automatic code-compliant generation of all design combinations.

The model is meant to demonstrate a realistic workflow and provide a useful starting point for manual engineering judgement.
