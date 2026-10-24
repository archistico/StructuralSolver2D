# M64 — Analisi statica incrementale di reticolari elasto-plastiche 2D

**Data:** 23 ottobre 2026  
**Stato:** implementato come componente `internal`, **in attesa di compilazione e test .NET 8 su Windows**.  
**Base validata:** M63, test C# confermati dall'utente.  
**Nessuno script Python nuovo né richiesta di Python per validare M64.**

## Perimetro

`src/StructuralSolver2D.Analysis/Truss2D/Nonlinear/IncrementalElastoplasticTruss2DSolver.cs` implementa un primo solutore di reticolari 2D (solo Truss2D) **a piccoli spostamenti e piccole deformazioni**. Riutilizza `ElastoplasticTruss2DElement` e `BilinearIsotropicAxialMaterial` di M63, senza modificare `Truss2DAnalyzer`, `Frame2DAnalyzer`, le API pubbliche, la CLI o il formato JSON. Restano esclusi geometria aggiornata, grandi rotazioni, P–Δ, buckling, rottura, controllo di spostamento, arc-length, interventi normativi e risposta dinamica non lineare.

L'input interno usa nodi (coordinate in m), elementi (area in m²; E, snervamento e H in kN/m²), vincoli omogenei direzionali e carichi nodali di riferimento (kN). `LoadFactors` definisce la storia completa dei moltiplicatori di carico e può includere scarico e inversione.

## Equazioni e algoritmo

Per ogni asta `e`, si ricavano trial indipendenti dallo stato plastico **committed** del precedente incremento:

- `f_int,e = N_e [-c, -s, c, s]`, in kN;
- `K_t,e = A E_t / L₀ · d dᵀ`, in kN/m;
- la rigidezza tangente e il vettore delle forze interne globali risultano dall'assemblaggio di tutte le aste.

Per il fattore di carico λ si cerca l'equilibrio proiettato `Zᵀ[λ F_ref − f_int(u, z_n)] = 0`, dove `Z` è una base ortonormale del sottospazio degli spostamenti compatibili con i vincoli. Ogni correzione Newton risolve `(Zᵀ K_t Z) Δq = Zᵀ[λ F_ref − f_int]` usando il risolutore denso del progetto, senza assumere positività definita. `u ← u + Z Δq`.

L'algoritmo termina quando `||r_r||₂ ≤ tol_abs + tol_rel · max(1, ||λ Zᵀ F_ref||₂)`, con default 1e−9 kN assoluto e 1e−9 relativo; il controllo della singolarità della tangente è effettuato **anche quando il residuo è nullo**, così un meccanismo scarico non passa. Queste tolleranze sono scelte sperimentali, non una certificazione di accuratezza o di sicurezza.

## Vincoli e stati

Ogni supporto impone `nx Ux + ny Uy = 0`. Le direzioni sono normalizzate; i vincoli ridondanti vengono scartati con ortogonalizzazione in doppio passaggio. Nessuna molla fittizia e nessuna penalizzazione numerica. Il risolutore è limitato inizialmente a 256 nodi, 1024 aste e 1000 incrementi (matrici dense, solo ricerca).

Durante Newton nessun elemento esegue commit. **Tutti i trial vengono confermati soltanto dopo il raggiungimento dell'equilibrio globale e la verifica delle grandezze finite**. Se la tangente è singolare, manca la convergenza, oppure il passo non è calcolabile, il solver restituisce un esito esplicito `Completed=false`, l'errore (`M64_SINGULAR`, `M64_NONCONVERGENCE`, ecc.), e soltanto gli snapshot degli incrementi precedenti già convergenti. Lo stato plastico dell'incremento rifiutato non viene adottato.

Per gli incrementi accettati vengono restituiti fattore λ, iterazioni, norma del residuo, spostamenti nodali, reazioni, forze assiali, stato plastico per asta e dissipazione plastica incrementale/cumulativa. Le reazioni sono il residuo globale `f_int − F_ext` e vanno interpretate come **risultanti nodali** compatibili con gli appoggi (in presenza di più vincoli ridondanti nello stesso nodo non viene proposta una ripartizione arbitraria tra vincoli).

## Benchmark C# xUnit

`tests/StructuralSolver2D.Analysis.Tests/Truss2D/IncrementalElastoplasticTruss2DM64Tests.cs` copre:

1. Un'asta: soluzione elastica chiusa `u=F/(EA/L)` e ramo plastico chiuso `u_y+(F−F_y)/k_t`, verifica reazione e plasticità.
2. Due aste parallele con diversi snervamenti: ripartizione analitica del carico e verifica che soltanto la prima plasticizzi nell'intervallo scelto.
3. Reticolare triangolare: forza nelle diagonali di valore analitico `−1/√2 kN`, forza nell'asta inferiore `+0.5 kN`, reazioni e residuo.
4. Asta inclinata con appoggio trasversale e vincolo ridondante: spostamento assiale e compatibilità verificati.
5. Scarico, ripetizione del carico e inversione senza doppi commit né dissipazione inventata.
6. Plasticità perfetta oltre carico limite: singolarità esplicita senza spostamenti artificiali.
7. Rollback **multi-asta**: incremento non equilibrabile dopo snervamento differenziato, conservando entrambi gli stati committati del passo precedente.
8. Arresto per limite iterativo, cinematismi scarichi o caricati, validazioni degli input.

I valori di confronto sono formule analitiche **incorporate in C#**; non dipendono dall'output del solver che stanno verificando. Il confronto numerico delle forze globali con l'analizzatore lineare e ulteriori casi limite restano possibili ampliamenti per M66.

## Gate e prova Windows

Dalla radice del progetto:

```powershell
.\scripts\run-m64-tests.ps1
```

L'esecuzione parte da `scripts/clean-project-build.ps1`: cancella le cartelle `bin`/`obj` appartenenti ai progetti, compila in Release, esegue i test M64 focalizzati e l'intera regressione .NET. **Non viene eseguito Python.**

L'ambiente di confezionamento non dispone del runtime/SDK .NET 8: compilazione, test xUnit e regressione M64 restano **non verificati**, finché l'utente non comunicherà l'esito Windows. I sorgenti lineari e l'integratore Newmark rimangono invariati.

**Passaggio seguente M65:** dinamica non lineare nello spazio dei gradi di libertà fisici, Newmark–Newton, matrici di massa consistenti, smorzamento dichiarato, controllo di `dt` e validazione contro i casi M59/M62. Non aprire l'API pubblica fino al gate M66.
