#!/usr/bin/env python3
"""Independent M63 reference; standard-library only. Optional, not needed for xUnit.

Closed-form branch evaluation in physical axial extension (different parameterization
from the C# stress/strain code) and a numerical differentiation of 4x4 forces.
"""
from math import cos, sin, pi, isfinite

E, AREA, L, Y, H = 200000., 0.01, 2.0, 250., 10000.
K = AREA * E / L
FY = AREA * Y
KH = AREA * H / L


def reference(extension, plastic_extension=0.0, accumulated=0.0):
    trial = K * (extension - plastic_extension)
    excess = abs(trial) - (FY + KH * accumulated)
    if excess <= 0.0:
        return trial, K, plastic_extension, accumulated, 0.0
    increment = excess / (K + KH)
    direction = 1.0 if trial >= 0.0 else -1.0
    force = trial - direction * K * increment
    return (force, K * KH / (K + KH),
            plastic_extension + direction * increment,
            accumulated + increment, FY * increment)


def vector(angle, extension):
    c, s = cos(angle), sin(angle)
    f, tangent, *_ = reference(extension)
    d = [-c, -s, c, s]
    return [f * item for item in d], [[tangent * p * q for q in d] for p in d]


def check_close(actual, expected, tol=1e-9):
    assert isfinite(actual) and abs(actual - expected) <= tol, (actual, expected, tol)


def run():
    f, kt, p, a, d = reference(0.004)
    check_close(f, 2.5714285714285716)
    check_close(kt, 47.61904761904762)
    check_close(p, 0.0014285714285714286)
    check_close(a, p)
    assert d > 0.0
    for angle in (0, pi/6, pi/4, pi/2, 3*pi/4):
        forces, stiffness = vector(angle, 0.004)
        c, s = cos(angle), sin(angle)
        check_close(forces[0], -c*f)
        check_close(forces[1], -s*f)
        check_close(sum(forces[:4:2]), 0.0)
        check_close(sum(forces[1:4:2]), 0.0)
        for i in range(4):
            for j in range(4):
                check_close(stiffness[i][j], stiffness[j][i])
        # Finite-difference derivation in physical displacement coordinates.
        dvec = [-c, -s, c, s]
        u = [0., 0., .004*c, .004*s]
        h = 1e-7
        for j in range(4):
            up, um = u[:], u[:]
            up[j] += h
            um[j] -= h
            fp, _ = vector(angle, sum(x*y for x,y in zip(dvec,up)))
            fm, _ = vector(angle, sum(x*y for x,y in zip(dvec,um)))
            for i in range(4):
                check_close((fp[i]-fm[i])/(2*h), stiffness[i][j], 1e-7)
    print('M63 independent axial-branch analytical reference: PASS')
    print(f'M63 fixture: N={f:.12g} kN, Kt={kt:.12g} kN/m, plasticExtension={p:.12g} m')


if __name__ == '__main__':
    run()
