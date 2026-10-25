"""Independent arithmetic references for the narrow C2C1 EC3 M89-B pilot.

These checks reproduce gross yield Npl,Rd=A*fy/gamma_M0, not a complete
tension-member resistance (Anet, fu, connections, etc. remain unverified).
They do NOT execute or validate the C# FEM/rule integration.
"""

from decimal import Decimal


def main() -> None:
    # From the project's cited OPPO nominal IPE-200 table and S235 strength
    # in the nominal thickness band, rather than from solver output.
    area_cm2 = Decimal("28.48")
    yield_mpa = Decimal("235")
    gamma_m0 = Decimal("1")  # Explicit illustrative test input, not annex lookup
    resistance_kn = area_cm2 * yield_mpa * Decimal("0.1") / gamma_m0
    assert resistance_kn == Decimal("669.280")

    examples = (
        ("pure_tension_200", Decimal("200"), False),
        ("pure_tension_400_combination", Decimal("1.5") * Decimal("300")
         + Decimal("0.5") * Decimal("-100"), False),
        ("pure_tension_700", Decimal("700"), True),
    )
    for label, force_kn, expected_failure in examples:
        utilization = force_kn / resistance_kn
        assert (utilization > 1) == expected_failure
        print(f"{label}: N={force_kn} kN | Npl,Rd={resistance_kn} kN | "
              f"utilization={utilization} | gross-yield-failure={expected_failure}")
    print("PASS: 3 independent gross-yield arithmetic references; NOT a FEM/.NET gate")


if __name__ == "__main__":
    main()
