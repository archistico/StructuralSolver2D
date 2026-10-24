"""Independent piecewise-analytical Newmark SDOF reference (no Newton iterations).

For each time step, solve the effective equilibrium separately on the elastic and
positive/negative plastic branches of a bilinear isotropically hardening spring.
This is deliberately different from the C# M62 tangent-Newton prototype.
Run: python scripts/m62-reference-piecewise-sdof.py
Internal mechanical units: kN, m, s, kN*s²/m; all results research-only.
"""
import math
from dataclasses import dataclass


@dataclass(frozen=True)
class State:
    u: float = 0.0
    v: float = 0.0
    a: float = 0.0
    plastic_u: float = 0.0
    alpha: float = 0.0
    internal: float = 0.0
    applied: float = 0.0
    plastic_diss: float = 0.0
    viscous_diss: float = 0.0
    external_work: float = 0.0
    internal_work: float = 0.0


def spring(u, old, k, fy, h):
    trial = k * (u - old.plastic_u)
    limit = fy + h * old.alpha
    if abs(trial) <= limit:
        return trial, old.plastic_u, old.alpha, 0.0
    delta = (abs(trial) - limit) / (k + h)
    sign = math.copysign(1.0, trial)
    return trial - sign * k * delta, old.plastic_u + sign * delta, old.alpha + delta, fy * delta


def step(old, fnext, dt, m, c, k, fy, h):
    beta, gamma = .25, .5
    pred_u = old.u + dt * old.v + .25 * dt * dt * old.a
    pred_v = old.v + .5 * dt * old.a
    B = m / (beta * dt * dt) + c * gamma / (beta * dt)
    rhs = B * pred_u + fnext - c * pred_v
    # First solve the elastic branch exactly, then choose a yielding branch if required.
    elastic_u = (rhs + k * old.plastic_u) / (B + k)
    elastic_trial = k * (elastic_u - old.plastic_u)
    limit = fy + h * old.alpha
    if abs(elastic_trial) <= limit:
        unew = elastic_u
    else:
        sign = math.copysign(1.0, elastic_trial)
        tangent = k * h / (k + h)
        unew = (rhs + tangent * old.plastic_u - sign * k * limit / (k + h)) / (B + tangent)
    q, up, alpha, dp = spring(unew, old, k, fy, h)
    anext = (unew - pred_u) / (beta * dt * dt)
    vnext = pred_v + gamma * dt * anext
    res = m * anext + c * vnext + q - fnext
    meanv = .5 * (old.v + vnext)
    return State(unew, vnext, anext, up, alpha, q, fnext,
                 old.plastic_diss + dp,
                 old.viscous_diss + c * meanv * meanv * dt,
                 old.external_work + .5 * (old.applied + fnext) * (unew - old.u),
                 old.internal_work + .5 * (old.internal + q) * (unew - old.u)), res


def run(dt=.005, steps=160, force_duration=.1, magnitude=5.0):
    mass, c, k, fy, h = 1., .5, 1000., .4, 100.
    state = State()
    points = {}
    max_res = 0
    for i in range(1, steps + 1):
        next_force = magnitude if i * dt <= force_duration + 1e-12 else 0.
        state, res = step(state, next_force, dt, mass, c, k, fy, h)
        max_res = max(max_res, abs(res))
        if i in (20, 40, 80, 160):
            points[i] = state
    assert max_res < 1e-9, max_res
    assert state.plastic_diss > 0
    assert state.viscous_diss > 0
    assert state.alpha >= 0
    return points, max_res


def main():
    points, residual = run()
    print('M62 independent piecewise-analytical reference: PASS')
    print(f'Max force equilibrium residual: {residual:.3e} kN')
    for step_id, s in points.items():
        print(f'step={step_id:3d}, t={step_id*.005:.3f} s, u={s.u:.16g} m, '
              f'v={s.v:.16g} m/s, alpha={s.alpha:.16g} m, '
              f'Dp={s.plastic_diss:.16g} kN*m')
    # Branch symmetries and state continuity with perfect plasticity.
    base, _ = step(State(), 5., .005, 1., 0., 1000., .4, 0.)
    inverted, _ = step(State(), -5., .005, 1., 0., 1000., .4, 0.)
    assert math.isclose(base.u, -inverted.u, abs_tol=1e-12)
    assert math.isclose(base.plastic_diss, inverted.plastic_diss, abs_tol=1e-12)


if __name__ == '__main__':
    main()
