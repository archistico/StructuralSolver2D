# M80-FIX3 — Pareggio del taglio minimo nel benchmark M78

**Data:** 24 ottobre 2026. **Stato:** correzione test e documentazione predisposte, gate Windows completo ancora da rieseguire.

## Esito del log Windows ricevuto

Il file `dotnet-20261024-113452-19084-371a6c90.log` contiene: build Release completata senza avvisi o errori; 17/17 test `SectionGeometryM80Tests`; 25/25 test Core dei cataloghi/unità; gate mirato M78/M79 con **14 test complessivi, 13 riusciti, 1 fallito**. La procedura si arresta quindi prima della regressione completa dei quattro progetti.

Unico fallimento: `Frame2DExactExtremaM78Tests.InteriorPointLoad_OneSidedShearAndExactMomentPeakArePreserved`, in precedenza linea 72: ascissa del minimo di taglio attesa 2 m, restituita 8 m.

## Analisi del caso e causa

Trave appoggiata L=8 m, forza concentrata verticale verso il basso P=12 kN a x=2 m. Per equilibrio: reazioni RA=+9 kN, RB=+3 kN. Con la convenzione interna M78: 

- `V(x)=+9 kN` in tutte le sezioni prima del carico (`0 ≤ x < 2`).
- `V(2⁻)=+9 kN`, `V(2⁺)=−3 kN`.
- `V(x)=−3 kN` lungo tutte le sezioni dopo il carico (`2 < x ≤ 8`).
- `M(2⁻)=M(2⁺)=18 kNm`.

Il valore massimo +9 e quello minimo −3 **non hanno una posizione unica**. L'implementazione di `Frame2DExactExtremaFinderM78` registra prima i candidati agli estremi (`x=0`, `x=L`), quindi i due limiti del salto (`x=2⁻`, `x=2⁺`): il criterio `MinBy/MaxBy` restituisce il primo candidato di pari valore. Per questa versione della API sono perciò corretti `Vmax: x=0, Side=Continuous` e `Vmin: x=8, Side=Continuous`. La posizione scelta è un rappresentante di un tratto, **non l'unica posizione fisica del valore estremo**.

Il test FIX2 aveva rettificato il pareggio del massimo, ma **non** il pareggio del minimo. Un fallimento del test non costituisce in questo caso prova di errore di equilibrio o di taglio.

## Intervento e regressione

Modificato soltanto `tests/StructuralSolver2D.Analysis.Tests/Frame2D/Frame2DExactExtremaM78Tests.cs`:

1. Atteso `minShear.Distance=8` e `Side=Continuous` per il benchmark con due plateaux.
2. Valutazione indipendente dei limiti locali attraverso `Frame2DInternalForceSampler.PrepareMember(...).Evaluate(2, after:false/true)`, con assert su `+9/−3 kN`, salto `−12 kN` e continuità di `M=18 kNm`.
3. Il test già esistente `PointLoadWithDistributedGradient_UniqueShearExtremaRetainBothOneSidedLimits` **non cambia**: la forza distribuita verso l'alto da +1 kN/m rende unici i valori di V prima/dopo il salto a `x=2 m`, e verifica esplicitamente `BeforePointLoad` e `AfterPointLoad`.

Nessun altro test, file `src/`, output FEM, solutore o wrapper `dotnet` modificato. Non sono stati abbassati limiti o tolleranze. Le rettifiche documentali rimuovono l'errata affermazione che il minimo debba essere etichettato `AfterPointLoad` in presenza di plateaux.

## Verifica richiesta

```powershell
.\scripts\run-m80-tests.ps1
```

Lo script pulisce `bin/obj` per prima cosa, compila Release, esegue la geometria M80, regressione Core preesistente, gate M78/M79, poi Core, Analysis, Reporting e Cli separatamente. **L'esito positivo deve coprire l'intero runner**, non solo i 14 test M78/M79. Ambiente di confezionamento senza .NET 8: nuova suite non eseguita localmente.
