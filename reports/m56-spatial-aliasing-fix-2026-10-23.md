# M56-FIX1 — Riparazione test MAC modale (2026-10-23)

## Esito ricevuto dall'utente

- `ModalBenchmarkM56Tests`: 27 passati, 1 fallito, 28 totali nel pacchetto M56 originale.
- Fallimento: `PinnedBeam_LowThreeFlexuralModes_ConvergeAndShapesAgreeWithSines`
  con `elements=4`, terzo modo flessionale teorico `323.11121214749176 Hz`,
  modo scelto erroneamente `1013.3972824709062 Hz`, soglia relativa `2%`.
- Il fallimento è nel *test di selezione della forma modale*: un indice di correlazione
  calcolato solo sui valori `Uy` ai nodi non distingue due modi di flessione
  che sulla mesh a quattro elementi presentano lo stesso pattern campionato.

## Riferimenti FEM indipendenti (Python/SciPy, non output del codice C#)

| Quantità | Risultato |
| --- | ---: |
| Terzo modo flessionale continuo (Euler–Bernoulli) | 323.111212147 Hz |
| Terzo flessionale FEM con 4 elementi | 329.015269557 Hz |
| Errore relativo FEM / continuo | +1.827252% |
| Modo alto flessionale con alias nodale | 1013.397282471 Hz |
| MAC interno (Hermite, 3 campioni per elemento) per il modo corretto | ~0.99927137 |
| MAC interno per il modo alto | ~0.12851172 |
| Primo modo longitudinale intercalato (ordine globale 3) | ~325.305469 Hz |

Le due forme flessionali producono MAC nodale praticamente 1 rispetto
alla terza sinusoide teorica, ma mostrano oscillazioni differenti dentro
gli elementi. La piccola componente numerica `Uy` di un modo assiale potrebbe
anche generare una correlazione nodale fuorviante quando normalizzata da sola.

## Modifica C# effettuata

- Solo `tests/StructuralSolver2D.Analysis.Tests/Dynamics/ModalBenchmarkM56Tests.cs`:
  `InterpolatedFlexuralMac` valuta la funzione cubica Hermite
  `v(t) = (1−3t²+2t³)v₀ + Lₑ(t−2t²+t³)θ₀ + (3t²−2t³)v₁ + Lₑ(−t²+t³)θ₁`,
  per `t = 1/4, 1/2, 3/4` in ciascun elemento.
- Il confronto MAC `(v·s)² / [(v·v)(s·s)]` è fatto sui campioni interni.
  Il filtro per modi assiali confronta `Σv²` e `Σu²` (con interpolazione
  assiale lineare). Il test si riferisce **solo alla trave di benchmark orizzontale**;
  non è un algoritmo universale di classificazione modale di strutture generiche.
- Nuova regressione `FourElementPinnedFrame_InterpolatedShapesRejectHighModeNodalAliasing`:
  dimostra il problema con il MAC nodale, poi esige selezione distinta con MAC
  interno e terza frequenza entro la tolleranza originaria.
- Tolleranze, materiali, sezioni, geometrie, benchmark analitici e algoritmo M55 invariati.

## Stato e istruzioni

- Cross-check indipendente Python/SciPy: valori soprariportati riprodotti.
- Integrità e preservazione del pacchetto M56 originale: controllare nel manifest ZIP.
- **Non eseguiti** compilazione o test .NET: SDK 8 non disponibile nell'ambiente
  di preparazione. È necessaria la nuova esecuzione su Windows:

```powershell
.\scripts\run-m56-tests.ps1
```

Non avviare M57 finché questo gate non passa completamente.
