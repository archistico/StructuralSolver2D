#!/usr/bin/env python3
"""Regenerate/check M56 *analytical* modal references and JSON geometric fixtures.

Uses only Python's standard library, and never calls the C# modal solver.
Usage: python scripts/m56-reference-data.py --check
       python scripts/m56-reference-data.py --write
"""
import argparse
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TARGET = ROOT / 'benchmarks' / 'modal'
E, A, I, GAMMA, L = 210_000_000.0, 0.02, 0.0001, 77.0, 4.0
G = 9.80665
MU = GAMMA * A / G
ROOTS = (1.875104068711961, 4.694091132974175, 7.854757438237613)
MESHES = (1, 2, 4, 8, 16)


def frequency(family, n):
    if family == 'pinned':
        omega = ((n * math.pi) ** 2 / L**2) * math.sqrt(E * I / MU)
    elif family == 'cantilever':
        beta = ROOTS[n - 1]
        omega = beta**2 / L**2 * math.sqrt(E * I / MU)
    elif family == 'axial-fixed-fixed':
        omega = n * math.pi / L * math.sqrt(E * A / MU)
    else:
        raise ValueError(family)
    return omega / (2 * math.pi)


def fixture(family, n):
    members = []
    nodes = [{'id': f'N{i}', 'x': L * i/n, 'y': 0.0} for i in range(n+1)]
    for i in range(n):
        members.append({'id': f'E{i}', 'startNodeId': f'N{i}', 'endNodeId': f'N{i+1}',
                        'materialId': 'MAT', 'sectionId': 'SEC',
                        'type': 'Truss2D' if family == 'axial-fixed-fixed' else 'Frame2D'})
    if family == 'pinned':
        supports = [dict(id='A', nodeId='N0', restrainedUx=True, restrainedUy=True, restrainedRz=False),
                    dict(id='B', nodeId=f'N{n}', restrainedUx=False, restrainedUy=True, restrainedRz=False)]
    elif family == 'cantilever':
        supports = [dict(id='A', nodeId='N0', restrainedUx=True, restrainedUy=True, restrainedRz=True)]
    else:
        supports = [dict(id='A', nodeId='N0', restrainedUx=True, restrainedUy=True, restrainedRz=False),
                    dict(id='B', nodeId=f'N{n}', restrainedUx=True, restrainedUy=True, restrainedRz=False)]
        supports += [dict(id=f'R{i}', nodeId=f'N{i}', restrainedUx=False, restrainedUy=True,
                          restrainedRz=False) for i in range(1, n)]
    return {'title': f'M56 {family}: {n} elements',
            'description': 'Educational analytical modal fixture; static JSON commands do not compute modes. '
                           'Dynamic tests create equivalent StructuralModel objects internally.',
            'nodes': nodes,
            'materials': [{'id':'MAT', 'name':'M56 benchmark material',
                           'elasticModulus': E, 'unitWeight': GAMMA}],
            'sections': [{'id':'SEC', 'name':'M56 benchmark section',
                          'area': A, 'momentOfInertia': I}],
            'members': members, 'supports': supports, 'dynamics': {'schemaVersion':1, 'nodalMasses':[]}}


def write_or_check(path, data, write):
    payload = json.dumps(data, ensure_ascii=False, indent=2) + '\n'
    if write:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(payload, encoding='utf-8')
    else:
        if not path.exists() or path.read_text(encoding='utf-8') != payload:
            raise SystemExit(f'M56_FIXTURE_DRIFT: {path} differs. Regenerate with --write.')


def main():
    parser = argparse.ArgumentParser()
    options = parser.add_mutually_exclusive_group(required=True)
    options.add_argument('--write', action='store_true')
    options.add_argument('--check', action='store_true')
    args = parser.parse_args()
    expected = {
        'milestone': 'M56',
        'referenceType': 'independent analytic continuum equations (not C# modal solver outputs)',
        'units': {'E': 'kN/m^2', 'A': 'm^2', 'I': 'm^4', 'gamma': 'kN/m^3',
                  'length': 'm', 'lineMass': 'kN*s^2/m^2', 'frequency': 'Hz'},
        'inputs': {'youngModulus': E, 'area': A, 'inertia': I, 'unitWeight': GAMMA,
                   'gravity': G, 'length': L, 'lineMass': MU},
        'references': {
            'pinned': 'f_n = n^2*pi^2/(2*pi*L^2)*sqrt(EI/mu)',
            'cantilever': 'f_n = beta_n^2/(2*pi*L^2)*sqrt(EI/mu); cos(beta_n)*cosh(beta_n)=-1',
            'axial-fixed-fixed': 'f_n = n*pi/(2*pi*L)*sqrt(EA/mu)',
            'shear-two-story': 'lambda_(1,2)=(3∓sqrt(5))*k/(2*m)'
        },
        'cantileverRoots': list(ROOTS),
        'shearTwoStory': {
            'stiffness': [[11200.0, -5600.0], [-5600.0, 5600.0]],
            'mass': [[3.25, 0.0], [0.0, 3.25]],
            'eigenvaluesRadiansSquaredPerSecondSquared': [
                (3 - math.sqrt(5)) * 5600 / (2 * 3.25),
                (3 + math.sqrt(5)) * 5600 / (2 * 3.25)
            ]
        },
        'frequenciesHz': {family: [frequency(family, i) for i in (1,2,3)]
                          for family in ('pinned','cantilever','axial-fixed-fixed')},
        'meshElementCounts': list(MESHES),
        'note': 'The FEM values in the convergence report are separately calculated estimates; do not mark C# tests passed until .NET suite runs.'
    }
    write_or_check(TARGET / 'expected' / 'm56-analytic-continuum.json', expected, args.write)
    for family in ('pinned', 'cantilever', 'axial-fixed-fixed'):
        for n in MESHES if family != 'axial-fixed-fixed' else MESHES[1:]:
            write_or_check(TARGET / 'models' / f'm56-{family}-{n:02d}.json', fixture(family,n), args.write)
    write_or_check(TARGET / 'models' / 'm56-shear-two-storey-matrices.json',
                   {'title': 'M56 shear system (2 DOFs, matrix-only)',
                    'description': 'Independent two-storey eigenproblem: no StructuralModel translation implied',
                    **expected['shearTwoStory']}, args.write)
    print('M56 analytical formulas, 14 geometric fixtures and shear matrix fixture verified.' if args.check else
          'M56 analytical formulas, 14 geometric fixtures and shear matrix fixture written.')

if __name__ == '__main__':
    main()
