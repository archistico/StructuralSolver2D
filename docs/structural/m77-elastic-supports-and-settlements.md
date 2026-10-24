# M77 — Appoggi elastici e cedimenti vincolari (statica lineare 2D)

**Stato: implementato, NON validato con dotnet su Windows.** M75 e M76 restano pendenti. Ultimo milestone confermato con test Windows: M73. Questo documento non costituisce validazione di impiego professionale.

## Obiettivo e perimetro

Estendere `StructuralSupport` e `SupportConstraintSystem` esistenti, senza sostituire l'algoritmo FEM lineare, con rigidezze elastiche nodali e spostamenti imposti **per caso di carico**. I solutori statici `Frame2DAnalyzer`, `Truss2DAnalyzer` e `PlaneStructureAnalyzer` riutilizzano lo stesso servizio. Non comprende cedevolezza del collegamento trave-nodo, molle non lineari, suolo distribuito, appoggi con giochi, attrito, isteresi, cedimenti che variano nel tempo.

## Modello fisico

Le molle sono riferite agli assi **locali del supporto**, non agli assi locali dell'asta. Per un supporto ruotato di angolo θ, le direzioni sono:

- `StiffnessUx` lungo `(cos θ, sin θ)` in **kN/m**;
- `StiffnessUy` lungo `(-sin θ, cos θ)` in **kN/m**;
- `StiffnessRz` attorno all'asse Z in **kNm/rad**, solo Frame2D/strutture miste che possiedono rotazioni fisiche.

Per ogni molla, si aggiunge alla rigidezza globale `k d dᵀ`. La forza esercitata dall'appoggio sul nodo è `R_s = -k(dᵀu)d`. L'analisi restituisce questa reazione separatamente per ciascun supporto, sommata alle componenti di eventuali vincoli rigidi presenti nel medesimo `StructuralSupport` (su direzioni diverse). **Non applicare molla e vincolo rigido sulla stessa direzione locale**.

## Cedimenti imposti

Una nuova azione standard `SupportSettlement` ha `targetType: "Support"`, `targetId: <id supporto>`, `direction: "LocalX" | "LocalY" | "MomentZ"`, `value` in m o rad. Il supporto di destinazione deve avere la corrispondente direzione **rigidamente vincolata**; una molla non equivale a un cedimento cinematico imposto. Il cedimento appartiene a un caso di carico e viene moltiplicato per i coefficienti della combinazione, anche negativi. Per la statica, i vincoli diventano `C u = g` e la soluzione usa l'augmentazione di Lagrange già esistente; l'ortogonalizzazione gestisce ridondanze compatibili e rigetta un sistema di prescrizioni incompatibili (`M77_INCOMPATIBLE_SETTLEMENT`).

Per i cedimenti il carico equivalente **non** si aggiunge al vettore delle forze esterne. Le loro conseguenze sono recuperate negli spostamenti, nelle azioni interne e nelle reazioni. Il checker dell'equilibrio globale ignora correttamente il cedimento come forza esterna.

## Compatibilità

Nuove proprietà opzionali `StiffnessUx`, `StiffnessUy` e `StiffnessRz`, con zero predefinito. Nessuna modifica ai JSON storici è necessaria. I nuovi campi vengono preservati da lettore e writer canonici M75. Il runner M77 prova anche la CLI su [`fixtures/m77-elastic-support-settlement.json`](fixtures/m77-elastic-support-settlement.json).

In **analisi dinamica**, l'attuale contratto di masse e vincoli non incorpora ancora le molle e i cedimenti M77. Il `StructuralDynamicsValidator` li segnala come `DYNAMIC_M77_STATIC_ONLY`. Non interpretare i risultati di analisi dinamiche o non lineari di questi modelli come validati. Le verifiche M51 basate su **vincoli omogenei** non costituiscono un audit energetico appropriato per i cedimenti non nulli: il relativo audit richiederà termini di lavoro alle frontiere.

## Benchmark analitici C#

`ElasticSupportSettlementM77Tests`: asta assiale con rigidezza `k_b = EA/L` in parallelo a una molla al nodo estremo; spostamento `u = F/(k_b + k_s)`; cedimento al nodo A `δ`, senza forze, spostamento B `u_B = k_b δ/(k_b+k_s)`; reazione di molla `-k_s u_B`; combinazione con segni opposti; molla inclinata 45°, molla rotazionale Frame2D; ripetibilità della soluzione; ridondanza cinematica incompatibile. `ElasticSupportJsonM77Tests`: round-trip JSON completo.

I test sono **C#/xUnit**, non Python. Non sono stati eseguiti nel presente ambiente, dove `dotnet` non è disponibile; test Windows, compilazione, test di regressione e revisione manuale sono passaggi pendenti.

## Esecuzione futura

```powershell
.\scripts\run-m77-tests.ps1
```

Il runner esegue la pulizia iniziale di `bin` e `obj`, build Release, gate M77, regressioni JSON M75/M76, prova CLI e intera suite. Mantiene l'output testuale di `stable-dotnet-console.ps1`.

## Prossimo

M78: completamento dei diagrammi di sforzo e spostamento, dei punti caratteristici e dei risultati governanti. **Prima di iniziare M78, verificare su Windows M75–M77 e risolvere eventuali problemi.**
