# StructuralSolver2D — M91-B (2026-10-24)

## Introduzione

- **M91-A confermato dall’utilizzatore:** compilazione e test della release precedente superati.
- **M91-B da collaudare su Windows .NET 8.** Nessuna compilazione/esecuzione .NET effettuata in questo ambiente.
- Aggiunto `SteelBucklingEffectiveLengthsM91B`, preparatore di lunghezze efficaci d'instabilità per due assi, con prove esterne esplicite su schema dei vincoli, rigidezza e resistenza dei controventi.
- Schemi ammessi: asta isolata biappoggiata, tratti idealmente biappoggiati fra ritegni lateralmente efficaci, mensola ideale, o analisi esterna documentata per i casi complessi.
- Esclusi calcolo della rigidezza e resistenza dei controventi, instabilità globale con spostamenti laterali, lunghezze automatiche da FEM, instabilità flesso-torsionale, pressoflessione e conformità complessiva EC3/NTC.
- I sorgenti precedenti del motore FEM, cataloghi, regole EC3 M89–M91-A e test esistenti sono invariati.

## Test

Eseguire dalla cartella `StructuralSolver2D`:

```powershell
.\scripts\verify-m91b-manifest.ps1
.\scripts\run-m91b-tests.ps1
```

Le misure principali sono `Lcr,y`, `Lcr,z` in metri. Vedi `docs/structural/M91B_LUNGHEZZE_EFFICACI_CONTROVENTI.md`.

## Riferimenti

- Bernuzzi, cap. 7, §7.4, libro fornito dall’utilizzatore (non ridistribuito).
- SCI P360 e ArcelorMittal Steel Academy, risorse pubbliche indicate nella nota tecnica.
