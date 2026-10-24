# M63 — Elemento Truss2D elasto-plastico isolato

**Versione:** 2026-10-23 · **Stato:** implementato, da compilare/testare su Windows .NET 8.  
**Perimetro:** singola asta, piccoli spostamenti, piccole deformazioni, direzione iniziale costante. NON è un solutore globale, un controllo di sicurezza o un modello di collasso.

## Obiettivo e collocazione

La roadmap M63–M75 prescrive che il prototipo M62 (molla SDOF, `k`, `Fy`, `H` espressi rispettivamente in kN/m, kN e kN/m) non venga copiato in un elemento finito senza conversioni dimensionali. M63 realizza un elemento di traliccio con parametri `E`, `sigmaY`, `H` espressi in kN/m², `A` in m² e lunghezza `L0` in metri, **senza modificare** l'analisi lineare già esistente.

Nuovi file di produzione:

- `src/StructuralSolver2D.Analysis/Truss2D/Nonlinear/BilinearIsotropicAxialMaterial.cs`
- `src/StructuralSolver2D.Analysis/Truss2D/Nonlinear/ElastoplasticTruss2DElement.cs`

Entrambe le classi sono `internal`: nessuna modifica a servizi pubblici, CLI, serializzazione JSON o analisi Frame2D. Il nuovo codice non viene ancora richiamato da un assemblatore strutturale.

## Unità e formule

Siano i nodi iniziali `(x1,y1)`, `(x2,y2)`, `L0 > 0`, `c=(x2-x1)/L0`, `s=(y2-y1)/L0`. I quattro gradi di libertà traslazionali sono `[Ux1,Uy1,Ux2,Uy2]`.

- Direzione globale `d=[-c,-s,c,s]` (adimensionale).
- Allungamento assiale `delta = d·u` [m] e deformazione `epsilon=delta/L0` [1].
- Stress trial `sigma_trial=E*(epsilon-epsilonP_old)` [kN/m²].
- Limite `sigmaY+H*alpha_old` [kN/m²].
- Se il trial è elastico, `sigma=sigma_trial`, `Et=E`, stato invariato.
- Se plastico, `DeltaGamma=(abs(sigma_trial)-sigmaY-H*alpha_old)/(E+H)` [1], `epsilonP_new=epsilonP_old+sign(sigma_trial)*DeltaGamma`, `alpha_new=alpha_old+DeltaGamma`.
- `sigma=sigma_trial-sign(sigma_trial)*E*DeltaGamma`, `Et=E*H/(E+H)`.
- Sforzo normale `N=A*sigma` [kN]. Forze interne `f_int=N*d` [kN].
- Rigidezza tangente materiale `Kt=(A*Et/L0)*d*dᵀ` [kN/m].
- Volume elementare `V=A*L0` [m³], energie [kN·m]: `Uel=V*E*(epsilon-epsilonP)^2/2`, `Uhard=V*H*alpha^2/2`, `DeltaDplastic=V*sigmaY*DeltaGamma`.

Per equivalenza con la molla M62 per un'estensione assiale `delta`, si ha `k=EA/L0`, `Fy=A*sigmaY`, `Hspring=A*H/L0`, `plasticDisplacement=L0*epsilonP`, `alphaDisplacement=L0*alpha`. Questa conversione viene verificata in xUnit su una storia ciclica completa.

## Gestione dello stato

`ElastoplasticTruss2DElement.EvaluateTrial(u)` valuta sempre rispetto allo **stato committed**; non lo modifica. `Commit(trial)` trasferisce il solo stato materiale alla generazione successiva. Un trial proveniente da un altro elemento o da una generazione precedente viene rifiutato. Se un trial non viene committato, lo stato precedente resta disponibile per rollback. È una semantica di elemento isolato: il commit atomico **multi-elemento** verrà progettato in M64.

Nella prima versione non sono supportati stato plastico iniziale fornito dall'esterno, degrado/rottura, grandi rotazioni, rigidezza geometrica, P–Delta, dilatazioni termiche, massa/damping accoppiati all'elemento o taglio/flessione.

## Benchmark M63

`tests/StructuralSolver2D.Analysis.Tests/Truss2D/ElastoplasticTruss2DM63Tests.cs` copre:

1. Return mapping chiuso (`sigma`, `Et`, `epsilonP`, `alpha`) e scarico elastico.
2. Asta orizzontale con valore analitico `N=2.5714285714285716 kN`, `Kt_xx=47.61904761904762 kN/m`.
3. Orientazioni 0°, 30°, 45°, 90°, 135°, -63°, inversione dei nodi, traslazioni e rotazioni rigide infinitesime.
4. Confronto diretto con `Truss2DElementMatrices.BuildGlobalStiffness` nel campo elastico; elementi in regime plastico e analisi della derivata numerica di `f_int`.
5. Corrispondenza indipendente con la molla M62 convertita dimensionalmente lungo un percorso ciclico; modulo di incrudimento nullo e accumulo dissipativo.
6. Lavoro di carico monotono e variazione degli incrementi, stato trial/commit e scarto dei commit obsoleti o di altri elementi.
7. Geometrie degeneri, materiali non fisici, valori non finiti e protezione dello stato committed da trial errati.

In più `scripts/m63-independent-axial-reference.py` è un **riferimento analitico opzionale**, realizzato nella coordinata di allungamento in metri e senza dipendenze esterne; nel container Python disponibile il riferimento ha prodotto `PASS`. Sul Windows dell'utente non è presente Python utilizzabile: il benchmark Python NON fa parte del gate obbligatorio e non va etichettato come eseguito localmente se viene saltato. I valori indipendenti principali sono già incorporati nei test C#.

## Esecuzione del gate Windows

Aprire PowerShell dalla radice del progetto ed eseguire:

```powershell
.\scripts\run-m63-tests.ps1
```

La **prima azione** è la pulizia centralizzata delle cartelle `bin` e `obj` appartenenti ai progetti MSBuild. Seguono build Release, test M63 focalizzati e regressione completa.

Il pacchetto è preparato e verificato per integrità, ma il container di confezionamento non dispone di .NET 8 / PowerShell: **non dichiarare M63 validato** prima della conferma Windows.

## Prossima tappa

**M64:** solutore di reticolare statica incrementale con più elementi, vincoli e rollback atomico dell'intero passo Newton. M63 non deve essere esposto come `AnalyzeNonlinear` pubblico né considerato già in grado di calcolare la risposta di una reticolare globale.
