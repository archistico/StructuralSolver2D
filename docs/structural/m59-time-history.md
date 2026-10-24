# M59 — Analisi dinamica lineare nel dominio del tempo (implementazione da validare)

## Finalità e perimetro

`M59` estende la pipeline FEM dinamica M53–M58 con una risposta **transitoria lineare** nel dominio del tempo. Implementa Newmark a accelerazione media costante, con `β=1/4`, `γ=1/2`, in una base modale **completa** normalizzata rispetto alla massa, ricavata dalle matrici ridotte `M54` e dai modi `M55`.

L'equazione è `M ü + C u̇ + K u = F(t)` in coordinate ammissibili. Per ciascun modo `j` si integra:

`q¨_j + 2 ζ_j ω_j q˙_j + ω_j² q_j = φ_j^T F(t)`.

**Nessuna riduzione modale arbitraria:** si usano tutti i modi restituiti da M55, nei limiti M54 (massimo 120 gradi di libertà ridotti, matrici consistenti positive definite, vincoli omogenei, niente meccanismi, svincoli di momento o modelli free-free). Gli input in JSON non richiedono un caso di carico statico. I comandi statici continuano a richiederlo.

## Schema e unità

Aggiungere un campo top-level `timeHistory`, separato da `loadCases`, `dynamics`, `harmonic`. L'esempio completo si trova in `examples/dynamics/m59-time-cantilever.json`.

```json
"timeHistory": {
  "timeStepSeconds": 0.002,
  "stepCount": 300,
  "damping": { "uniformModalRatio": 0.03 },
  "loads": [
    {
      "forces": [{"nodeId":"N8", "fy":-1}],
      "history": [
        {"timeSeconds":0,"factor":0},
        {"timeSeconds":0.01,"factor":1},
        {"timeSeconds":0.03,"factor":1},
        {"timeSeconds":0.04,"factor":0},
        {"timeSeconds":0.6,"factor":0}
      ]
    }
  ],
  "initialStates": []
}
```

- `timeStepSeconds`: `Δt > 0` in secondi. `stepCount`: da 1 a 2000. Output incluso a `t=0`, quindi esattamente `stepCount+1` frame; fine a `stepCount*Δt`.
- `loads`: 0–64 **pattern nodali** indipendenti. Ogni pattern definisce un vettore di forze `Fx/Fy [kN]`, `Mz [kN*m]`, e una funzione adimensionale a tratti lineari `factor(t)`; la somma dei pattern determina il carico applicato. Da due a 10.000 campioni per pattern. I tempi sono strettamente crescenti, cominciano esattamente a `0` e coprono il tempo finale; nessuna estrapolazione. Forze nulle/assenza di pattern ammesse per vibrazioni libere.
- `initialStates`: facoltativi, valore iniziale zero se assente. `Ux,Uy` in m, `Rz` in rad, `Vx,Vy` in m/s, `VrZ` in rad/s. Devono rispettare i vincoli e i gradi di libertà effettivamente presenti; ingressi duplicati o riferimenti ignoti sono rifiutati.
- `damping`: **obbligatorio**, stesso contratto di M58. Un rapporto uniforme `uniformModalRatio` oppure **entrambi** `rayleighAlphaInverseSeconds` [s⁻¹] e `rayleighBetaSeconds` [s]; anche i valori zero devono essere dichiarati esplicitamente. Non confondere `beta` del modello di Rayleigh con il `β` numerico di Newmark.
- Lo spettro modale è elaborato interamente; non vengono ricavate forze distribuite, reazioni o azioni interne dal modello statico.

## Stato al passo temporale

Per ogni nodo si esportano spostamenti fisici (`Ux, Uy, Rz`), velocità (`Vx, Vy, VrZ`), accelerazioni (`Ax, Ay, ArZ`) e **forza nodale esterna applicata** (`Fx, Fy, Mz`). L'accelerazione iniziale è ricavata per equilibrio modale *prima* di effettuare il primo passo.

Gli inviluppi `MaxAbs*` sono i massimi assoluti **dei campioni temporali calcolati**, non massimi analitici continui. Le forze nodali applicate non comprendono automaticamente le reazioni ai vincoli. Gli inviluppi di sforzo normale, taglio e momento **non sono ancora calcolati**: servirà un postprocessore dinamico coerente che tenga conto di inerzia, smorzamento e recupero delle forze degli elementi.

Energia `[kN*m]`: `Kinetic=1/2 Σ(q˙²)`, `Elastic=1/2 Σ(ω² q²)`; `DissipatedEnergy=∫ Σ(2ζω q˙²)dt`, `ExternalWork=∫ Σ(P_j(t)q˙_j)dt`. La quadratura è **consistente con Newmark**: `ΔW_ext = (F_n+F_{n+1})/2 * (q_{n+1}-q_n)`, `ΔW_diss = Δt * c * ((v_n+v_{n+1})/2)²` per ogni modo, in modo che il bilancio discreto sia rispettato (al netto degli arrotondamenti). `BalanceResidual = E(t) + W_diss(t) - E(0) - W_ext(t)`. **Un residuo piccolo non prova la convergenza temporale**, che richiede confronti con passi temporali più piccoli. La risoluzione temporale va verificata con almeno un dimezzamento di Δt; la stabilità numerica del metodo, per sistemi lineari ammissibili, non prova l'accuratezza alle alte frequenze.

## Comandi

```powershell
dotnet run -c Release --project src/StructuralSolver2D.Cli -- time examples/dynamics/m59-time-cantilever.json
dotnet run -c Release --project src/StructuralSolver2D.Cli -- time-report examples/dynamics/m59-time-cantilever.json risposta.md
dotnet run -c Release --project src/StructuralSolver2D.Cli -- time-csv examples/dynamics/m59-time-cantilever.json csv-m59
dotnet run -c Release --project src/StructuralSolver2D.Cli -- time-viewer examples/dynamics/m59-time-cantilever.json risposta.html
```

`time-csv` crea `time-nodes.csv` e `time-energy.csv` nella directory indicata. Le esportazioni usano `InvariantCulture`; gli identificatori sono protetti dalla formula injection CSV. Il viewer incorpora fino a circa 500 frame **solo per la visualizzazione**, traccia una deformata interpolata con Hermite cubico per `Frame2D` (lineare tra nodi per `Truss2D`) e mostra gli andamenti temporali per nodo/componente; i CSV mantengono tutti i passi. Il viewer **amplifica graficamente** gli spostamenti, non anima lo spazio dei modi normalizzati.

## Validazione e limiti

Test inclusi: SDOF analitico libero non smorzato con energia conservata e convergenza quadratica, oscillatore smorzato dissipativo, gradino con limite statico, struttura FEM con condizioni iniziali e diversi pattern, compatibilità dei vincoli, input errati, report e CLI. Script: `scripts/run-m59-tests.ps1` (clean, build, M57/M58 regressione, M59, tutta la soluzione, esportazioni CLI, verifica sintattica JS se Node è presente).

Non sono supportati in M59: vincoli in movimento, eccitazione sismica del terreno, velocità/deformazioni finite, plasticità/isteresi, carichi distribuiti variabili nel tempo, condizioni al contorno non omogenee, matrici di massa singolari, svincoli flessionali dinamici, modello free-free. Nessuna valutazione normativa/sismica o idoneità della struttura.

**Stato di rilascio:** sorgenti e controlli indipendenti disponibili; compilazione e test .NET 8 in attesa dell'esecuzione dell'utente su Windows. M58 automatizzato validato dall'utente il 23/10/2026; ispezione visiva dei viewer ancora aperta.
