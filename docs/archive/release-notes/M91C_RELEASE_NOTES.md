# StructuralSolver2D — M91-C / instabilità latero-torsionale (2026-10-24)

- **Baseline M91-B:** test confermati dall’utilizzatore. Nessuna modifica ai sorgenti FEM precedenti.
- Nuovo `SteelLateralTorsionalBucklingRuleM91C`: EN 1993-1-1:2005+A1:2014 **metodo generale** §6.3.2.2, profili IPE/HEA/HEB a doppia simmetria, flessione pura sull’asse forte, classi 1–3.
- La `class 3` usa `W_el` catalogato; classi 1–2 richiedono esplicitamente `W_pl` con fonte. `Mcr` va documentato da analisi indipendente oppure calcolato esclusivamente per schema standard a forche, ingobbamento libero, momento uniforme e proprietà torsionali fornite.
- La regola **non** assegna in automatico proprietà torsionali mancanti, altezze di carico, ritegni laterali, coefficienti C1, coefficienti parziali o conformità generale.
- Nuovi test xUnit, casi numerici del Bernuzzi A8.2, confronto JRC, controlli di fail-closed e regressioni esistenti.
- **Da collaudare su Windows .NET 8**: eseguire `scripts\verify-m91c-manifest.ps1` e `scripts\run-m91c-tests.ps1` dalla cartella principale, preferibilmente estratta in una directory pulita.
- Dettagli: `docs/structural/M91C_INSTABILITA_LATERO_TORSIONALE_EC3.md`.

**Riferimenti:** Bernuzzi (2018), cap. 8 §8.1.4, 8.2.3 e A8.2 pp. 233–235 (PDF fornito, non incluso), JRC Simões 2014 presentazione *Eurocodes Design of Steel Buildings with Worked Examples*; EC3 2005+A1:2014 §6.3.2.
