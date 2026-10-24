# StructuralSolver2D G3 — NTC 2018, controlli combinati su sezione

Baseline: **G2 GATE PASSED**, confermato dall'utente. Aggiunto il modulo locale `Ntc2018TimberCombinedSectionRuleG3` con tre modalità: tensoflessione N>0, pressoflessione N<0, flessione con taglio N=0. N e M concomitanti; quando V≠0, taglio verificato indipendentemente sul medesimo punto FEM. Seconda disuguaglianza k_m=0,7 calcolata sempre. Niente formule inventate per interazioni V–M o N–V.

Aggiunto file sorgente C# G3, una suite di test xUnit indipendenti, benchmark JSON numerici e nota tecnica; **G2/G1/EC5/FEM invariati**. Non valida instabilità, torsione, eccentricità, collegamenti, altre combinazioni o conformità dell'asta.

Prima della prova Windows: `scripts/verify-g3-manifest.ps1`. Gate: `scripts/run-g3-tests.ps1` (build Release warning-as-error; tests normativa, cataloghi, Core, FEM, report e CLI; `-WithM86Smoke` facoltativo).

**Compilazione e test .NET 8 non eseguibili in questo ambiente; gate G3 aperto.**

G3 FIX1: corretto il test sulla traccia delle disuguaglianze NTC, confrontando valori numerici con tolleranza invece di testi `G17` byte-identici. Vedere `G3_FIX1_RELEASE_NOTES.md`. Nessuna modifica agli algoritmi.
