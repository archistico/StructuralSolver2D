"""Independent constitutive feasibility spike; NOT a structural solver feature.

Run: python scripts/m61-check-bilinear-return-map.py
Pure standard-library uniaxial isotropic hardening return mapping; no .NET dependency.
Stress units MPa, total/plastic strain dimensionless, H and E in MPa.
"""
from dataclasses import dataclass
from math import isclose, isfinite


@dataclass(frozen=True)
class State:
    plastic_strain: float = 0.0
    accumulated_plastic_strain: float = 0.0


def update(total_strain: float, committed: State, E: float, yield_stress: float, H: float):
    if not all(map(isfinite, (total_strain, E, yield_stress, H))):
        raise ValueError('Nonfinite input')
    if E <= 0 or yield_stress <= 0 or H < 0:
        raise ValueError('Require E>0, yield_stress>0 and H>=0')
    trial = E * (total_strain - committed.plastic_strain)
    f = abs(trial) - (yield_stress + H * committed.accumulated_plastic_strain)
    if f <= 0:
        return trial, E, committed, 0.0
    sign = 1 if trial >= 0 else -1
    dgamma = f / (E + H)
    next_state = State(committed.plastic_strain + sign * dgamma,
                       committed.accumulated_plastic_strain + dgamma)
    stress = trial - E * dgamma * sign
    tangent = E * H / (E + H)
    plastic_dissipation_density = yield_stress * dgamma
    return stress, tangent, next_state, plastic_dissipation_density


def run():
    E, sy, H = 200_000.0, 250.0, 10_000.0
    base = State()
    # Elastic branch and trial-state immutability.
    stress, tangent, trial_state, dissip = update(0.001, base, E, sy, H)
    assert isclose(stress, 200.0) and tangent == E and trial_state == base and dissip == 0
    # Post-yield monotonic branch compared with closed-form expression.
    strain = 0.002
    stress, tangent, committed, dissip = update(strain, base, E, sy, H)
    expected = sy + (E * H / (E + H)) * (strain - sy/E)
    hardening_tangent = tangent
    assert isclose(stress, expected, rel_tol=1e-13)
    assert isclose(tangent, E*H/(E+H), rel_tol=1e-13)
    assert committed.accumulated_plastic_strain > 0 and dissip > 0 and base == State()
    # Elastic unloading from the yielded state: no new plasticity in this increment.
    unloaded, unloading_tangent, after, extra_dissip = update(0.001, committed, E, sy, H)
    assert unloading_tangent == E and after == committed and extra_dissip == 0
    assert isclose(unloaded, stress - E*(strain-0.001), abs_tol=1e-10)
    # Repeated strain path: cumulative dissipated energy density non-negative.
    steps = [0., 0.001, 0.002, 0.003, 0., -0.003, 0., 0.003, -0.003]
    state, total_dissipation = State(), 0.
    for e in steps:
        s, Et, next_state, de = update(e, state, E, sy, H)
        assert isfinite(s) and isfinite(Et) and de >= 0
        assert next_state.accumulated_plastic_strain >= state.accumulated_plastic_strain
        total_dissipation += de
        state = next_state
    assert total_dissipation > 0
    # Perfect plasticity special case: tangent zero, no hardening.
    s, tangent, new_state, de = update(0.004, State(), E, sy, 0)
    assert isclose(s, sy, abs_tol=1e-10) and tangent == 0 and de > 0
    # Reject malformed constitutive data.
    for args in ((0.1, State(), 0., sy, H), (0.1, State(), E, sy, -1.),
                 (float('nan'), State(), E, sy, H)):
        try:
            update(*args)
        except ValueError:
            pass
        else:
            raise AssertionError(f'Expected rejection for {args}')
    print('M61 isolated bilinear return-map feasibility checks: PASS')
    print(f'monotonic strain={strain}, stress={stress:.9f} MPa, post-yield tangent={hardening_tangent:.9f} MPa, '\
          f'cumulative test dissipation={total_dissipation:.6f} MPa')
    print('Research-only scalar constitutive check; not integrated into StructuralSolver2D.')


if __name__ == '__main__':
    run()
