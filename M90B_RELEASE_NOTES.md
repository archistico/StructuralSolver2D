# StructuralSolver2D — M90-B release candidate

- Baseline: **M90-A confermato dall'utente, test passati**.
- Nuova funzione: classificazione limitata a parametri di stress dichiarati alpha/psi e studio delle transizioni N–M dell'anima, con test indipendenti tratti dal Bernuzzi A4.3.
- Integrazione FEM: screening elastico conservativo di una singola sezione, senza mischiare i risultati di combinazioni o posizioni diverse.
- Risultati `Passed` normativi: **nessuno**. La classificazione e lo screening non costituiscono certificazioni di resistenza, stabilità o conformità.
- Tutti i sorgenti C# della baseline M90-A: **invariati**, salvo aggiunta del nuovo file M90-B.
- L'originale PDF Hoepli fornito dall'utente non è nel progetto distribuito.
- Controlli statici dello ZIP e manifest: eseguiti. Compilazione Windows/.NET 8: da confermare.

```powershell
.\scripts\verify-m90b-manifest.ps1
.\scripts\run-m90b-tests.ps1
```

## M90-B FIX1 — Correzione CS8602 (24 ottobre 2026)

- Il gate Windows originale segnalava `CS8602` a riga 240 di `SteelMixedISectionClassificationM90B.cs`.
- La stringa di tracciabilità ora usa `code.EditionId` e `code.NationalAnnexId` dalla variabile locale con null-check esplicito anziché dereferenziare nuovamente `request.Profile.Code`.
- Nessuna modifica alle formule EC3, agli input/output numerici, ai risultati o ai solver FEM.
- La modifica riguarda soltanto la tracciabilità e l'analisi nullable del compilatore.
- L'archivio completo è stato ricontrollato staticamente. Il gate .NET 8 dovrà essere ripetuto su Windows.

```powershell
.\scripts\verify-m90b-manifest.ps1
.\scripts\run-m90b-tests.ps1
```
