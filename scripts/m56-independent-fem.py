#!/usr/bin/env python3
"""Optional SECOND implementation of 1-D consistent-mass element matrices.

Computes reference FE eigenvalues with SciPy (not StructuralSolver2D).
Analytic continuum values are calculated separately from matrix results.
Requires NumPy and SciPy; not required for .NET test execution.
"""
import math
import numpy as np
from scipy.linalg import eigh

E, AREA, INERTIA, WEIGHT, G, LENGTH = 210000000.0, .02, .0001, 77.0, 9.80665, 4.0
MU = WEIGHT * AREA / G
ROOTS = (1.875104068711961, 4.694091132974175, 7.854757438237613)


def reference(family, order):
    if family == 'pinned':
        return (order * math.pi)**2 / LENGTH**2 * math.sqrt(E * INERTIA / MU) / (2 * math.pi)
    if family == 'cantilever':
        return ROOTS[order - 1]**2 / LENGTH**2 * math.sqrt(E * INERTIA / MU) / (2 * math.pi)
    return order * math.pi / LENGTH * math.sqrt(E * AREA / MU) / (2 * math.pi)


def solve_reference_elements(count, family):
    h = LENGTH / count
    n = 3 * (count + 1)
    stiffness = np.zeros((n, n))
    mass = np.zeros((n, n))
    bar_k = E * AREA / h * np.array([[1, -1], [-1, 1]], dtype=float)
    bar_m = MU * h / 6 * np.array([[2, 1], [1, 2]], dtype=float)
    bending_k = E * INERTIA / h**3 * np.array([
        [12, 6*h, -12, 6*h], [6*h, 4*h*h, -6*h, 2*h*h],
        [-12, -6*h, 12, -6*h], [6*h, 2*h*h, -6*h, 4*h*h]], dtype=float)
    bending_m = MU * h / 420 * np.array([
        [156, 22*h, 54, -13*h], [22*h, 4*h*h, 13*h, -3*h*h],
        [54, 13*h, 156, -22*h], [-13*h, -3*h*h, -22*h, 4*h*h]], dtype=float)
    for index in range(count):
        axial = [3*index, 3*(index+1)]
        bending = [3*index+1, 3*index+2, 3*(index+1)+1, 3*(index+1)+2]
        stiffness[np.ix_(axial, axial)] += bar_k
        mass[np.ix_(axial, axial)] += bar_m
        if family != 'axial-fixed-fixed':
            stiffness[np.ix_(bending,bending)] += bending_k
            mass[np.ix_(bending,bending)] += bending_m
    if family == 'pinned':
        fixed = [0, 1, 3*count+1]
    elif family == 'cantilever':
        fixed = [0, 1, 2]
    else:
        fixed = [0, 3*count]
        fixed += [3*i+1 for i in range(count+1)]
        fixed += [3*i+2 for i in range(count+1)]
    if family in ('pinned', 'cantilever'):
        # Bending-only continuum comparisons: one-element frames have only two
        # bending eigenmodes. Never mislabel an axial mode as bending mode 3.
        active = sorted(set(i for i in range(n) if i % 3 != 0) - set(fixed))
    else:
        active = sorted(set(range(n)) - set(fixed))
    if not active:
        return []
    lambdas = eigh(stiffness[np.ix_(active,active)], mass[np.ix_(active,active)], eigvals_only=True)
    if np.min(lambdas) <= 0:
        raise RuntimeError('Not an SPD FE benchmark; correct the reference setup.')
    return np.sqrt(lambdas) / (2 * math.pi)


def main():
    print('Independent SciPy reference; values below are NOT C# solver results.')
    print('μ = %.12f kN·s²/m²' % MU)
    for family in ('pinned','cantilever','axial-fixed-fixed'):
        print('\n%s — continuous analytical Hz: %s' %
              (family, ', '.join('%.9f' % reference(family,i) for i in (1,2,3))))
        print('| elements | first Hz | first error (%) | second Hz | second error (%) | third Hz | third error (%) |')
        print('| ---: | ---: | ---: | ---: | ---: | ---: | ---: |')
        for n in (1,2,4,8,16):
            frequencies = solve_reference_elements(n,family)
            output = [str(n)]
            for index in range(3):
                if index >= len(frequencies):
                    output += ['—','—']
                else:
                    f = float(frequencies[index])
                    error = 100 * (f / reference(family,index+1) - 1)
                    output += ['%.9f' % f, '%+.6f' % error]
            print('| ' + ' | '.join(output) + ' |')

if __name__ == '__main__':
    main()
