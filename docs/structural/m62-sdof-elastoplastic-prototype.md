# M62 — Prototipo sperimentale SDOF elasto-plastico

**Data:** 2026-10-23. **Stato:** implementato e confrontato con un riferimento numerico indipendente; compilazione e suite .NET 8 da eseguire su Windows. **Base accettata:** M61, con conferma dell'utente del superamento dello script completo, inclusa la pulizia automatica `bin`/`obj`.

## 1. Perché questo prototipo è isolato

Questo milestone realizza **solo** il benchmark B1–B4 delineato in `m61-nonlinear-feasibility.md`, con una molla e una massa aventi un solo grado di libertà. Serve a stabilire se il metodo costitutivo (memoria di plasticizzazione), il correttore di Newton e il bilancio energetico sono implementati correttamente **prima** di studiare aste 2D non lineari. Non aggiunge tipi JSON, comandi CLI, API pubbliche, risultati di membri, verifiche di sicurezza, cerniere plastiche o capacità di collasso. Le analisi lineari statiche/M53–M60 sono intatte.

Percorsi:

- `src/StructuralSolver2D.Analysis/Dynamics/Nonlinear/BilinearIsotropicSpring.cs`: legge bilineare unidimensionale con incrudimento isotropo lineare, stato committed immutabile e valutazione trial.
- `src/StructuralSolver2D.Analysis/Dynamics/Nonlinear/NonlinearSdofNewmark.cs`: moto transitorio e aggiornamento atomico dello stato a convergenza raggiunta.
- `tests/StructuralSolver2D.Analysis.Tests/Dynamics/M62/NonlinearSdofM62Tests.cs`: soluzioni chiuse, confronto con M59, storia ciclica, carico impulsivo di durata finita, verifica di non-convergenza, confronto indipendente e raffinamento temporale.
- `scripts/m62-reference-piecewise-sdof.py`: riferimento **indipendente** che risolve i rami elastico e plastico con formule algebriche, senza iterazioni Newton.
- `scripts/run-m62-tests.ps1`: pulizia centralizzata prima di tutto, build Release, test M62, regressione completa, benchmark Python opzionale.

## 2. Unità e modello

Nel prototipo valgono esclusivamente le **unità interne kN–m–s**:

| Grandezza | Unità |
|---|---|
| massa `m` | kN·s²/m |
| spostamento `u`, spostamento plastico `u_p`, plasticità cumulata `α` | m |
| rigidezze `k` e `H` | kN/m |
| forza di snervamento `Fy`, forza esterna e di richiamo | kN |
| viscosità `c` | kN·s/m |
| velocità e accelerazione | m/s, m/s² |
| energia, lavoro, dissipazione | kN·m (moltiplicare per 1000 per ottenere joule) |

Equazione del moto:

\[
 m\ddot u+c\dot u+f_s(u,z)=F(t), \qquad z=(u_p,\alpha).
\]

Per ogni valutazione si parte **sempre** dallo stato confermato al passo precedente:

\[
 f_{trial}=k(u-u_p^n),\quad
 g=|f_{trial}|-(F_y+H\alpha^n).
\]

- Se `g ≤ 0`: forza `f_s=f_trial`, tangente `k`, stato invariato.
- Se `g > 0`: `Δλ=g/(k+H)`; `u_p^{trial}=u_p^n+sign(f_trial)Δλ`, `α^{trial}=α^n+Δλ`, `f_s=f_trial−sign(f_trial)kΔλ`; tangente `kH/(k+H)`.
- Incremento dissipato irreversibilmente `ΔD_p=Fy Δλ≥0`; energia elastica recuperabile `½k(u-u_p)²`; energia di incrudimento accumulata `½Hα²`.

**Limitazioni costitutive:** piccole deformazioni, snervamento simmetrico in trazione/compressione, incrudimento **isotropo** (non cinematico), niente danno, softening, fatica o rottura. Il caso `H=0` modella plasticità perfetta.

## 3. Newmark e Newton

Passo fisso `Δt>0`, costanti `β=1/4`, `γ=1/2`; carico esterno al tempo finale del passo. Lo spostamento al tempo `t_{n+1}` è la sola incognita iterativa. Il predittore è

\[
 u_p^*=u_n+\Delta t v_n+\frac{\Delta t^2}{4}a_n,\quad
 v_p^*=v_n+\frac{\Delta t}{2}a_n.
\]

Qui l'apice `*` contraddistingue il **predittore cinematico**, non lo spostamento plastico `u_p`. Per ogni candidato `u`,

\[
 a(u)=\frac{u-u_p^*}{\beta\Delta t^2},\quad
 v(u)=v_p^*+\gamma\Delta t\,a(u),
\]

\[
 R(u)=m a(u)+c v(u)+f_s(u,z_n)-F_{n+1},
\]

\[
 K_{eff}=\frac{m}{\beta\Delta t^2}
   +\frac{\gamma c}{\beta\Delta t}+k_t.
\]

L'iterazione aggiorna `u ← u-R/Keff`. Accetta la soluzione se

\[
 |R|\le \varepsilon_{abs}+\varepsilon_{rel}\max(1,|F|,|f_s|,|ma|,|cv|).
\]

I valori iniziali del prototipo sono `εabs=1e−10 kN`, `εrel=1e−10`, massimo 20 iterazioni (tutti configurabili con limiti). Ogni trial costitutivo legge lo stato **committed** del passo `n`; stato, tempo, forze ed energie cambiano soltanto dopo convergenza. Mancata convergenza, overflow o input malformati generano un errore diagnostico, senza mutazioni. Nessun substepping adattivo è fornito in M62. La condizione iniziale plastificata senza storia nota è rifiutata.

## 4. Due bilanci diversi

1. **Bilancio del lavoro discreto Newmark**: lavoro esterno integrato trapezoidalmente meno incremento di energia cinetica, dissipazione viscosa discreta e lavoro interno trapezoidale. Deve risultare prossimo a zero per lo schema risolto.
2. **Bilancio energetico fisico**: lavoro esterno meno incrementi di energia cinetica, elastica, energia immagazzinata nell'incrudimento, dissipazione plastica e viscosa. **Non** è garantito esattamente nullo se durante un passo temporale viene attraversata la soglia di snervamento: la quadratura a estremi del lavoro interno non integra esattamente il cambio di ramo. Il suo scarto è un indicatore da studiare raffinando `Δt`, non un motivo per modificare le leggi fisiche o nascondere il risultato.

Un residuo discreto minuscolo **non** certifica accuratezza temporale, né prestazioni antisismiche.

## 5. Riferimento numerico indipendente (storia impulsiva finita)

Parametri `m=1 kN·s²/m`, `k=1000 kN/m`, `Fy=0,4 kN`, `H=100 kN/m`, `c=0,5 kN·s/m`, `Δt=0,005 s`. Forza `5 kN` agli estremi dei passi `1..20`, poi `0 kN` ai passi `21..160`; carico iniziale `F(0)=0`. Si tratta di una **forza a gradino di durata finita**, non di un impulso ideale. Il riferimento indipendente risolve per rami in forma chiusa l'equilibrio dinamico di Newmark, senza riutilizzare Newton.

| Tempo [s] | Spostamento [m] | Velocità [m/s] | `α` [m] | Dissipazione plastica [kN·m] |
|---:|---:|---:|---:|---:|
| 0,100 | 0,02039450801738481 | 0,3818684252850392 | 0,01817682547034983 | 0,007270730188139931 |
| 0,200 | 0,04316810104112660 | 0,02985101093318743 | 0,03888009185556964 | 0,015552036742227860 |
| 0,400 | 0,04298928366248342 | 0,02463528877175392 | 0,03896689307140128 | 0,015586757228560510 |
| 0,800 | 0,04264419779621661 | 0,01580238952113522 | 0,03896689307140128 | 0,015586757228560510 |

Il riferimento ha un massimo residuo di equilibrio di circa `1,6e−12 kN` in questo caso. La verifica d'indipendenza non sostituisce la compilazione .NET del modulo C#.

Per il medesimo problema, il valore di `u(0,8 s)` passa da `0,0423959528 m` (`dt=0,01`) a `0,0426441978` (`dt=0,005`), `0,0427080385` (`dt=0,0025`) e `0,0427186614` (`dt=0,00125`). Ciò illustra la sensibilità al passo temporale; per un giudizio affidabile occorre verificare anche velocità, accelerazioni, istanti di snervamento, dissipazione e altri scenari.

## 6. Test, gate e sviluppo futuro

Comando Windows, dalla radice del progetto:

```powershell
.\scripts\run-m62-tests.ps1
```

**Primo passo dello script:** `clean-project-build.ps1`, che cancella soltanto `bin`/`obj` locali ai progetti `.csproj` / `.vbproj` / `.fsproj`, secondo la policy introdotta in M61. Segue build Release, test M62, **intera regressione precedente** e confronto Python opzionale (i valori di riferimento sono già inclusi nei test C#).

**Gate M62:** compilazione senza errori, superamento del benchmark indipendente su 4 tempi, test di ritorno elastico M59, stato invariato in caso di errore Newton, dissipazione non negativa, controllo del bilancio discreto e raffinamento, nessuna regressione. Non ancora eseguito nel tool environment privo di SDK .NET.

**Da non dedurre:** supporto per plasticità delle aste/travi, capacità reale a flessione, cerniere plastiche, accelerogrammi non lineari, collasso o conformità normativa. L'eventuale fase M63 dovrebbe prima progettare una **singola asta assiale 2D** con test indipendenti di trasformazione, stato e forze interne e un gate formale; non modificare direttamente il solver lineare.


## M62-FIX1 — Diagnostica opzionale del benchmark Python

Il benchmark matematico è invariato. Il runner verifica un interprete Python 3.7+ realmente funzionante (`python`, quindi `py -3`), mostra traceback e codice di uscita se il benchmark parte ma fallisce, e segnala chiaramente **SKIPPED** se Python non è disponibile. La validazione C# resta obbligatoria e precede sempre questa verifica. Leggere `docs/structural/m62-python-benchmark-runner-fix.md`. La correzione attende ancora prova PowerShell su Windows.
