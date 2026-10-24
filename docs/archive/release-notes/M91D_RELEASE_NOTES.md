# StructuralSolver2D M91-D — Audit copertura acciaio

**Baseline:** M91-C ha superato i test eseguiti dall'utente (24 ottobre 2026).

## Variazioni

- Nuovo sorgente `SteelMemberCoverageAuditM91D.cs` nel progetto `StructuralSolver2D.Verification.Steel`.
- Nuovi test `SteelMemberCoverageAuditorM91DTests.cs` in `Verification.Tests/M91`.
- Nuova documentazione `docs/structural/M91D_AUDIT_COPERTURA_ACCIAIO.md`.
- Runner `scripts/run-m91d-tests.ps1` e nuovo manifest SHA-256.
- Tutti i sorgenti di produzione preesistenti e i test preesistenti lasciati invariati.

## Nota di sicurezza

**Non sono state introdotte verifiche di conformità complessiva della membratura o dell'opera.** Il report valuta soltanto evidenze associate a controlli dichiarati, escludendo automatismi normativi non giustificati.

## Collaudo

```powershell
.\scripts\verify-m91d-manifest.ps1
.\scripts\run-m91d-tests.ps1
```

I nuovi test necessitano di compilazione/esecuzione .NET 8 sul PC Windows. I soli controlli statici di archivio non sostituiscono il gate.

## FIX1 — 24 ottobre 2026

- Corretto l’analizzatore xUnit2013 alla riga 71 di `SteelMemberCoverageAuditorM91DTests.cs`: `Assert.Equal(0, report.UndeclaredAreas.Count)` ora è `Assert.Empty(report.UndeclaredAreas)`.
- Solo il test e queste note sono stati modificati; i sorgenti di produzione, le formule EC3 e la logica dell’audit sono invariati.
- Il build .NET 8 e la regressione devono essere rieseguiti su Windows.
