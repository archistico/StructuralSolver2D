# M72 — Primo elemento Frame2D con cerniere plastiche concentrate

**Data:** 23 ottobre 2026  
**Stato:** prototipo **interno sperimentale** implementato; M71-AUDIT-FIX1 e M72 **ancora da testare con .NET 8 su Windows**.  
**Baseline:** M71-AUDIT-FIX1. Nessuna modifica al risolutore elastico Frame2D o alla sua serializzazione JSON.

## Obiettivo effettivamente raggiunto

Sono disponibili due nuovi componenti interni a `StructuralSolver2D.Analysis`:

- `Frame2D/Nonlinear/BilinearRotationalHinge.cs`: cerniera concentrata con rigidezza rotazionale elastica **finita**, soglia di momento, incrudimento isotropo, prova costitutiva pura e stato plastico storico.
- `Frame2D/Nonlinear/ElastoplasticFrame2DElement.cs`: trave elastica Euler–Bernoulli a sei DOF esterni, con una o due cerniere rotazionali alle estremità. Risolve le rotazioni interne della trave tramite equilibrio iterativo, poi condensa la **tangente consistente** 6×6. Restituisce forze e tangenti locali/globali, energia recuperabile, dissipazione incrementale, stato trial e commit separato.

Non è stato collegato a `Frame2DAnalyzer`, ai solver lineari, ai modelli JSON pubblici o alla CLI. Non comprende ancora l'**assemblaggio incrementale globale di telai**. Tale integrazione, indicata come fase successiva nel piano M71, necessita di un gate distinto con test per meccanismi, vincoli, carichi distribuiti e rollback di più elementi. Per una mensola con unico DOF rotazionale libero, un test M72 risolve un percorso di momenti mediante Newton **soltanto nel test**.

## Formulazione e segni

Coordinata locale `[ux_i, uy_i, rz_i, ux_j, uy_j, rz_j]`; la rigidezza della trave centrale è la matrice elastica di M71. Alle estremità attive, `theta_h = theta_nodo - theta_trave`; lo sforzo interno della cerniera agisce con momento `+M_h` sul DOF esterno e `-M_h` sul DOF di rotazione della trave. L'equilibrio locale richiede `f_trave[rz] - M_h = 0`.

Per una cerniera con parametri **indipendenti** `kθ [kN·m/rad]`, `My [kN·m]`, `H [kN·m/rad]`:

- `M_trial = kθ (theta_h - theta_p)`;
- `f = |M_trial| - (My + H alpha)`;
- se `f <= 0`, `M = M_trial`, `k_t = kθ`, nessun incremento plastico;
- se `f > 0`, `gamma = f / (kθ + H)`, aggiornamento trial di `theta_p` e `alpha`, `M = M_trial - sign(M_trial) kθ gamma`, `k_t = kθ H/(kθ + H)`;
- dissipazione incrementale **non negativa** `D = My gamma`; energia recuperabile nella trave elastica e nelle cerniere, compresa l'energia di incrudimento.

La soluzione locale usa al massimo due rotazioni interne `q`, con Jacobiano `A = K_beam[rot,rot] + diag(k_t)`. Terminato Newton, la tangente esterna è lo Schur complement `K_cond = K_uu - K_uq A⁻¹ K_qu`. Nessuna stabilizzazione fittizia. Il caso `H=0` può produrre **una direzione esterna a tangente nulla**: non va nascosta. Fallimenti dell'equilibrio locale o valori non fisici sollevano errori senza cambiare gli stati committed.

## Benchmark automatici solo C#

`tests/StructuralSolver2D.Analysis.Tests/Frame2D/M72/ElastoplasticFrame2DM72Tests.cs` verifica:

1. Comportamento identico alla matrice Frame2D elastica preesistente quando non ci sono cerniere.
2. Rigidezza di una molla elastica in serie con la flessione di estremità `k_eff = (4EI/L · kθ)/(4EI/L + kθ)` per rapporti di rigidezza diversi.
3. Snervamento, momento e tangente dopo plasticizzazione rispetto a una **soluzione algebrica indipendente** del sistema in serie.
4. Lavoro esterno monotono = incremento di energia recuperabile + dissipazione plastica.
5. Consistenza della tangente condensata tramite differenze finite delle forze locali.
6. Due cerniere attive, simmetria della tangente, equilibrio dei momenti all'interfaccia, equilibrio tagli.
7. Rotazione rigida dell'orientamento dell'asta e coerenza delle grandezze globali.
8. `EvaluateTrial` puro; commit corretto; rifiuto di commit stale/di altro elemento; scarico senza doppio conteggio.
9. Piccolo percorso carico–scarico con singolo DOF, Newton esterno e commit per stati convergenti (**solo test**).
10. Cerniera perfettamente plastica priva di rigidezza tangente artificiale.
11. Mancata convergenza locale che lascia intatto lo stato committed; rifiuto di parametri non fisici.

I riferimenti matematici sono incorporati in C#/xUnit; **non** sono stati creati script Python. La verifica indipendente completa di mesh, scalabilità e assemblaggio di telai rimane aperta.

## Ipotesi e limitazioni

- Geometria iniziale fissa e piccoli spostamenti/rotazioni, Euler–Bernoulli senza deformazione a taglio; non applicabile a instabilità o P–Δ.
- Soltanto cerniere *agli estremi*: non modella plasticità distribuita, danno, frattura o collasso.
- **Nessuna interazione N–M**: l'assiale della trave resta elastico e non riduce la capacità a momento.
- **Cerniere con rigidezza elastica finita**: il ramo elastico dell'elemento è più flessibile dell'incastro rigido; non usare `kθ = ∞`. Parametri da dati sperimentali o modello sezionale, non derivati automaticamente da `I`.
- Legge a incrudimento **isotropo**: non descrive l'effetto Bauschinger; cicli complessi richiedono una legge più avanzata.
- Nessun carico distribuito, release legacy, massa, smorzamento, risposte dinamiche o certificazione NTC/Eurocodice nel nuovo elemento pilota.
- L'API sperimentale è `internal`: nessuna compatibilità pubblica promessa finché i test non siano superati.

## Come verificare

Dalla cartella radice su Windows con .NET 8 e PowerShell:

```powershell
.\scripts\run-m72-tests.ps1
```

Lo script pulisce **prima di ogni altra operazione** le cartelle di build dei progetti, utilizza il wrapper testuale M71 per i log, compila in Release, esegue il gate C# M71 insieme ai test M72 e infine l'intera regressione. In caso di errore i log rimangono disponibili nel percorso temporaneo indicato dalla console.

**Gate per sviluppo futuro:** superare i test Windows M71 e M72, poi verificare l'assemblaggio globale incrementale su più telai, le reazioni, i carichi distribuiti, il rollback atomico e il condizionamento. Solo dopo considerare API/JSON/report/viewer per Frame2D non lineari.
