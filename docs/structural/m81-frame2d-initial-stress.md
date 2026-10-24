# M81 — Rigidezza geometrica e pre-instabilità Frame2D (gate .NET in attesa)

**Data:** 24 ottobre 2026. **Base:** M80-FIX3, test dell'intera suite Windows confermati dall'utente. **M81:** implementato come componente isolato; non ancora compilato/verificato nel runtime Windows. Eseguire `scripts/run-m81-tests.ps1`.

## Scopo

Preparare un contributo iniziale di analisi del secondo ordine **nel piano**. Per ogni elemento Frame2D Euler–Bernoulli prismatico con **forza assiale di riferimento costante e prescritta**, fornire la rigidezza geometrica consistente, la tangente linearizzata materiale + geometrica e un piccolo riferimento di equilibrio a due gradi di libertà per una mensola. L'obiettivo è verificare la convenzione fisica e il comportamento vicino alla perdita di stabilità **prima** di intervenire sull'assemblaggio globale in M82.

### Convenzioni

- DOF locali ordinati `[u_i, v_i, θ_i, u_j, v_j, θ_j]`.
- Coordinate locali e trasformazione coerenti con `Frame2DElementMatrices`.
- **N > 0 trazione**, stabilizzante; **N < 0 compressione**, destabilizzante.
- N in kN, L in m, E in kN/m², A in m², I in m⁴. Matrice K in unità generalizzate coerenti (kN/m, kNm/rad ecc.).
- **N è passato esplicitamente**. Non viene ricavato da un'analisi al primo ordine né da un generatore di carichi.

Per spostamento trasversale interpolato con polinomi cubici di Hermite, la parte di energia quadratica associata allo sforzo normale iniziale è:

`Π_g = 0.5 N ∫[0,L] (dv/dx)^2 dx`.

Sui DOF flessionali `[v_i, θ_i, v_j, θ_j]` si ottiene:

```text
             [ 36    3L   -36    3L ]
  Kg = N/30L  [ 3L   4L²   -3L   -L² ]
             [-36   -3L    36   -3L ]
             [ 3L   -L²   -3L   4L² ]
```

Le componenti assiali u_i,u_j sono zero nel contributo Kg adottato. La tangente del modello linearizzato è `Kt = Ke + Kg`, e in assi globali `Ktg = Tᵀ Kt T`. La convenzione `N < 0` porta a riduzione della rigidezza flessionale.

## Codice

- `src/StructuralSolver2D.Analysis/Frame2D/SecondOrder/Frame2DInitialStressM81.cs`
  - `BuildLocalGeometricStiffness(N,L)`
  - `BuildLocalLinearizedTangent(E,A,I,L,N)`
  - `BuildGlobalLinearizedTangent(E,A,I,L,c,s,N)`
  - `SolveCantileverTipLoad(E,A,I,L,N,Fy,Mtip)` — riferimento a **un solo elemento**, con start bloccato e tip con 2 DOF flessionali.
- Nessuna API pubblica/JSON/CLI nuova; nessuna modifica ai solver del primo ordine, alle combinazioni, alle release o al calcolo M72.

Il dimostratore `SolveCantileverTipLoad` verifica definitezza positiva e condizionamento elementare del blocco libero 2×2. Un valore critico/supercritico non viene silenziosamente restituito come normale equilibrio: segnala `M81_LINEARIZED_INSTABILITY`. Non gestisce l'equilibrio post-critico, la plasticità o la storia di carico.

## Benchmark C#/xUnit indipendenti

`tests/StructuralSolver2D.Analysis.Tests/Frame2D/M81/Frame2DInitialStressM81Tests.cs`:

1. Coefficienti di Kg confrontati con una **integrazione indipendente Gauss–Legendre a tre punti** del prodotto delle derivate Hermite; positiva, negativa e N = 0.
2. Simmetria, assenza di rigidezza assiale artificiale, energia del modo di traslazione laterale rigida.
3. Inversione del segno della forza e controllo dell'energia geometricamente corrispondente.
4. N = 0 recupera esattamente la matrice elastica precedente.
5. Orientamento in piano: identità energetica sotto trasformazione locale/globale.
6. Mensola senza precarico: freccia `Fy L³/(3EI)` e rotazione `Fy L²/(2EI)`.
7. Compressione prima del carico critico amplifica la freccia; trazione la riduce.
8. Carico critico **del modello discretizzato con una mensola/elemento**, derivato dal determinante del blocco 2×2: con `x=P L²/EI`, `0.15 x² - 5.2 x + 12=0`, prima radice circa `2.486`, da confrontare con il valore continuo `π²/4≈2.467` (differenza di discretizzazione, non coincidenza esatta).
9. Colonna a cerniere agli estremi, **un elemento**: modo rotazionale antisimmetrico e carico critico discreto `12EI/L²`, sopra Euler continuo `π² EI/L²`.
10. Validazione di lunghezza, materiale, forza non finita e coseni direttori non unitari.

I test impongono correttezza numerica, mai soglie temporali; nessuno script Python.

## Limiti fondamentali — da non omettere nei risultati

- **NON** è un solver globale P–Δ: N è un dato di riferimento, non un equilibrio risolto iterativamente; non gestisce redistribuzione delle azioni assiali.
- Piccoli spostamenti e rotazioni; elementi prismatici Euler–Bernoulli con giunti rigidamente collegati al livello elementare. Nessuna condensazione/geometria di release introdotta in M81.
- Il solo termine geometrico linearizzato non sostituisce una formulazione corotazionale/total-Lagrangian per grandi rotazioni.
- Pre-instabilità soltanto. Non si possono interpretare i carichi critici discreti come verifica normativa, portata resistente o fattore di sicurezza.
- La perdita di definitezza della matrice di un modello *vincolato* è un problema globale: in M81 si controlla soltanto il cantilever dimostrativo; verifiche/modi globali sono obiettivo di **M82**, con benchmark e diagnosi indipendenti.
- Stabilità **fuori piano**, instabilità flesso-torsionale e verifiche EN 1993/1995 sono ancora fuori dal perimetro.

## Gate di validazione

1. `scripts/run-m81-tests.ps1` pulisce `bin/obj` all'inizio.
2. Compila l'intera soluzione Release .NET 8.
3. Esegue i test dedicati M81, poi i quattro progetti di regressione (`Core`, `Analysis`, `Reporting`, `Cli`) uno alla volta, con stato chiaro e log testuale.
4. M81 viene segnato **validato** soltanto dopo il risultato positivo confermato sul Windows dell'utente.

**Prossimo M82:** assemblaggio globale di Kgeometrica, proiezione sui vincoli, analisi lineare P–Δ e problema agli autovalori di stabilità su strutture piane, con controllo del precarico, modi instabili e limiti applicativi.
