# G3 FIX1 — correzione confronto test xUnit (24/10/2026)

## Diagnosi Windows

Dal log del collaudo G3 originale: build **Release 0 warning / 0 errori**; verifiche **405 passati su 406**. Un solo test non superato: `Ntc2018TimberCombinedSectionG3Tests.BothNtcEquationsAreCalculated_KmOnlyOnSecondEquation`, linea 139. La traccia numerica viene emessa in formato `G17`, ma il test la confrontava con una rappresentazione testuale esatta prodotta da un'espressione indipendente che può differire di pochi ULP (ordine delle operazioni `double`).

## Correzione

- Modificato **solo** `tests/StructuralSolver2D.Verification.Tests/M92/Ntc2018TimberCombinedSectionG3Tests.cs`: estrae `equation a` e `equation b` dalla traccia con formato numerico invariant culture; verifica entrambe con `Assert.Equal(..., precision: 12)` e controlla che `equation b < equation a` per applicazione di `k_m=0.7` esclusivamente alla seconda disuguaglianza.
- **Nessun file C# di produzione è cambiato**, nessun coefficiente o formula normativa alterati, nessun test disabilitato, regressioni G2/G1/EC5/FEM preservate.
- Manifest SHA-256 rigenerato e archivio completo G3 FIX1 ricreato.

## Gate Windows

```powershell
.\scripts\verify-g3-manifest.ps1
.\scripts\run-g3-tests.ps1
```

Il gate G3 FIX1 resta **da confermare sul tuo Windows**; l'ambiente di creazione non dispone di dotnet.
