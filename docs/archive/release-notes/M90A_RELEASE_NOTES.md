# StructuralSolver2D M90-A — Release candidate

- Base confermata dall'utente: **M89-B test superati**.
- Nuova funzionalità: classificazione pura EC3 §5.5 Tabella 5.2 per profili IPE/HEA/HEB, compressione uniforme o flessione forte.
- Confronti con le due applicazioni svolte A4.1 e A4.2 del testo Bernuzzi allegato dall'utente; tabelle 4.4-4.6 confrontate a campione con catalogo OPPO.
- Fail-closed per geometrie incoerenti, fy non valide, edizione non supportata e profili INP.
- **Non** completa nessuna verifica di interazione, resistenza o stabilità e non certifica conformità NTC/EC3.
- Sorgenti di produzione già collaudati: **invariati**. Solo un nuovo file C# in Verification.Steel e i relativi test, documentazione e runner.
- PDF Bernuzzi usato come documento di riferimento; **non redistribuito nello ZIP**.
- Verifiche statiche del pacchetto: SHA-256 e integrità ZIP. Compilazione/test .NET 8: **da eseguire su Windows**.

Procedura:

```powershell
.\scripts\verify-m90a-manifest.ps1
.\scripts\run-m90a-tests.ps1
```
