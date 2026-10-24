# M51 — independent reference values

The test cases are built from the actual model constructors in
`tests/StructuralSolver2D.Analysis.Tests/Verification/StaticEnergyVerificationTests.cs`.
Analytical quantities below are independent of the FEM solve:

| Case | Input | Closed-form target | Assumptions |
| --- | --- | --- | --- |
| Cantilever tip transverse load | L=3 m, P=10 kN, E=210000000 kN/m², I=0.00002 m⁴ | `u_y(B)=-P L³/(3 E I)`, `U=P² L³/(6 E I)` | Euler–Bernoulli, perfect clamp, tip load |
| Horizontal axial truss | L=5 m, P=100 kN, E=210000000 kN/m², A=0.003 m² | `U=P² L/(2 E A)` | Axial linear elasticity, roller restrains transverse DOF |
| Uniformly loaded single beam | L=5 m, q=-10 kN/m | `W=2U`, `U>0` | Test *discrete* energy, not continuous exact strain energy |
| Released beam under uniform load | L=4 m, q=-8 kN/m, both ends moment-released | `W=2U=0` for all nodal DOF fixed/inactive | Element has member-load fixed-end response, but zero *nodal* displacement energy |
| Inclined mixed portal | A=(0,0), B=(4,0), C=(0,3), D=(4,3); member T=AD | `W=2U`, `||r_free||≈0` | Mixed frame/truss; use full assembled loads |

Also exercised: arbitrary element rotations, reverse element orientation, support redundancy,
negative combination factors, zero load and tampered matrices/loads. These are **verification
scenarios**, not proof of validity for nonlinear, dynamic or design-code analysis.
