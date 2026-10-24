# M61 — Dinamica non lineare: studio di fattibilità

**Data:** 23 ottobre 2026  
**Stato:** studio architetturale consegnato; **nessuna funzionalità strutturale non lineare attiva**.  
**Base verificata dall'utente:** M60 (test Windows superati); revisione browser dei viewer storici ancora manuale.  
**Fonti teoriche di progetto:** A. Carpinteri, *Dinamica delle strutture*, cap. 1 §§1.7–1.8 e cap. 6, richiamati nella roadmap; principi generali di plasticità incrementale e FEM non lineare da validare con casi indipendenti prima dell'implementazione.

## 1. Decisione e perimetro

**Studio fattibile, implementazione differita.** L'attuale StructuralSolver2D usa travi/aste elastiche lineari, matrici di rigidezza costanti e, in dinamica, un'espansione nella base modale lineare. Non è sufficiente aggiungere una curva tensione–deformazione non lineare al materiale: servono memoria storica, risoluzione incrementale, iterazioni, criteri di arresto, aggiornamento della rigidezza e verifiche energetiche. Gli attuali risultati statici, modali, sismici e i relativi JSON restano invariati.

Questa fase **non** supporta né promette: plasticizzazione reale dei telai, cerniere plastiche, instabilità post-critica, grandi deformazioni, crollo, fatica, verifiche agli stati limite o analisi sismiche normative. Le attuali diagnostiche numeriche non sono verifiche di sicurezza.

## 2. Distinguere le non linearità

| Fenomeno | Variabili aggiuntive | Prima scelta | Fuori dallo spike |
|---|---|---|---|
| Materiale elasto-plastico 1D | Deformazione plastica, variabile di incrudimento, energia dissipata | Modello bilineare monotono/ciclico isolato | Multiasse e danno |
| Geometria (P–Δ, grandi rotazioni) | Configurazione aggiornata, rigidezza geometrica | Studio successivo separato | Inserimento silenzioso nel modello lineare |
| Contatto, apertura/chiusura | Stato attivo e vincoli variabili | Rimandato | Supporti unilaterali attivi in produzione |
| Smorzamento non lineare | Legge e storia dissipativa | Rimandato | Interpretare Rayleigh come isteresi plastica |

**Sequenza prudente proposta:** (1) oscillatore 1DOF e legge plastica uniaxiale; (2) asta 2D con plasticità assiale e piccoli spostamenti; (3) trave con cerniere plastiche concentrate o plasticità distribuita **solo dopo una scelta esplicita**, validazione e modello delle sezioni; (4) geometria non lineare solo in un percorso autonomo.

## 3. Contratto matematico da progettare

Nel dominio delle coordinate fisiche libere, non nella base modale lineare:

\[
M\ddot u(t)+C\dot u(t)+f_{\mathrm{int}}(u(t),z(t))=F_{\mathrm{ext}}(t),
\]

dove `z` rappresenta lo stato interno irreversibile per elemento/punto di integrazione. A differenza di `K u`, `f_int` dipende dalla storia di carico. Per vincoli non omogenei, accelerazioni del suolo e inerzia occorre riformulare coerentemente il termine esterno; M60 non può essere riutilizzato semplicemente sostituendo `K`.

Al passo `n→n+1`, con Newmark medio (`β=1/4`, `γ=1/2`) come possibile predittore/correttore, una iterazione di Newton sullo spostamento incognito richiede:

- Calcolo di `a(u)` e `v(u)` compatibili con la discretizzazione temporale.
- Stato *trial* di ciascun materiale **partendo sempre dallo stato confermato `z_n`**; non aggiornare irreversibilmente lo stato a ogni iterazione.
- Residuo `R=F_ext − M a − C v − f_int(u,z_trial)`.
- Rigidezza tangente `K_t=∂f_int/∂u` e rigidezza efficace `K_eff=K_t + γ/(βΔt) C + 1/(βΔt²) M` nel caso indicato.
- Risoluzione di `K_eff Δu=R`; incremento con controllo di norma e possibilmente ricerca lineare.
- Commit atomico del nuovo stato `z_{n+1}` **solo** a equilibrio e criteri di convergenza raggiunti; su fallimento rollback e dimezzamento del passo con limiti espliciti.

**Attenzione:** il tangent elastoplastico può perdere definitezza positiva, soprattutto con ammorbidimento, meccanismi e instabilità. Le ipotesi SPD del solver modale Cholesky–Jacobi M55 non sono applicabili al risolutore non lineare generale; valutare LU con pivoting e diagnostiche del rango. In caso di punti limite può essere necessario controllo di spostamento / arc-length, non previsto per un primo prototipo.

### Stato e interfacce (proposta, non implementata)

- `NonlinearMaterialState` **immutabile/committed**: `PlasticStrain`, `AccumulatedPlasticStrain`, ed eventuali variabili di incrudimento; `TrialUpdate(totalStrain, committedState)` restituisce `Stress`, `ConsistentTangent`, `TrialState`, `DissipationIncrement`.
- `INonlinearElement`: valutazione delle forze interne e della tangente con stato committabile/annullabile; orientamenti locali/globali verificati come per M51–M54.
- `NonlinearStepResult`: tempo, iterazioni, norme del residuo, norma dell'incremento, substep, bilancio energetico, problemi diagnostici, stati convergenti.
- **Separazione:** nuovo namespace / servizio esplicitamente sperimentale e nuovo JSON opzionale; nessuna sostituzione degli attuali `Frame2D`, `Truss2D`, `AnalyzeTimeHistory`, `AnalyzeSeismicTime` o output di produzione.

## 4. Un primo materiale di riferimento: bilineare 1D

Piccole deformazioni; modulo elastico `E>0`, snervamento iniziale `σ_y>0`, incrudimento isotropo `H≥0`. Stato impegnato `(ε_p,α)` e prova elastica:

\[
\sigma_{trial}=E(\varepsilon-\varepsilon_p),\qquad
f_{trial}=|\sigma_{trial}|-(\sigma_y+H\alpha).
\]

Se `f_trial≤0`, `σ=σ_trial` e `E_t=E`. Altrimenti, con `s=sign(σ_trial)`:

\[
\Delta\gamma=\frac{f_{trial}}{E+H},\quad
\varepsilon_p^{trial}=\varepsilon_p+s\Delta\gamma,\quad
\alpha^{trial}=\alpha+\Delta\gamma,
\]

\[
\sigma=\sigma_{trial}-Es\Delta\gamma,\qquad
E_t=\frac{EH}{E+H},\qquad
\Delta D=\sigma_y\Delta\gamma\ge0.
\]

Lo spike riproducibile `scripts/m61-check-bilinear-return-map.py` usa **solo la libreria standard Python** ed esegue confronti contro i valori chiusi del ramo post-snervamento, unloading elastico, dissipazione non negativa, plasticità perfetta `H=0`, input non validi e immutabilità dello stato committed. Esempio dimensionale `E=200000 MPa`, `σ_y=250 MPa`, `H=10000 MPa`, `ε=0,002`: `σ=257,142857 MPa`, `E_t=9523,809524 MPa`. Il calcolo è **costitutivo uniaxiale**: NON integra il moto nel tempo e NON valida un telaio 2D.

L'energia di incrudimento è immagazzinata, la dissipazione plastica è distinta; non confondere integrale di lavoro plastico totale e sola dissipazione. Lo smorzamento viscoso, se presente, è un ulteriore termine energetico. I valori MPa dello spike non sono convertiti automaticamente nelle unità interne kN–m–s del solver.

## 5. Programma di validazione indipendente (shortlist per un eventuale M62)

| Ordine | Benchmark | Cosa verificare | Rifiuto / failure mode |
|---|---|---|---|
| B1 | Molla elasto-plastica 1D, deformazione monotona | Snervamento, tangente, soluzione chiusa bilineare | `E≤0`, `σ_y≤0`, `H<0` |
| B2 | Scarico, inversione ciclica, più passi | Stato storico, ramo elastico, `ΔD≥0`, commit/rollback | Nessuna convergenza: stato invariato |
| B3 | Oscillatore 1DOF sempre elastico | Identità con soluzione M59/SDOF analitica | Divergenza per passo inadatto |
| B4 | Oscillatore 1DOF plastico sotto impulso finito | Equilibrio a ogni passo, dissipazione e sensibilità a `Δt` | Erronea energia negativa / mancato equilibrio |
| B5 | Asta elastoplastica 2D inclinata | Trasformazioni, lavoro interno, soluzione assiale indipendente | Rotazioni rigide spurie / meccanismo |
| B6 | Telaio con plasticizzazione in un'estremità | Momento–rotazione e forze agli estremi; benchmark separato | Cerniera inventata con sola condensazione statica |
| B7 | Mesh refinement / regolarizzazione se softening | Dipendenza da discretizzazione e legge costitutiva | Dipendenza patologica dalla mesh |

**Criteri quantitativi da definire prima di implementare:** residuali di forza con scala fisica più tolleranza assoluta, incrementi di spostamento, massimo Newton iterations e substep, residuo energetico comprensivo di plasticità, ordine di convergenza temporale, controlli su dissociazione di scale e casi quasi singolari. Nessuna tolleranza fissata a posteriori per far passare i test.

## 6. Rischi di integrazione e decisioni aperte

1. **Modelli di sezione:** `E` e `A/I` non descrivono da soli capacità assiali/flessionali, combinazione N–M e incrudimento della sezione. Servono leggi e nuovi dati dimensionali, con validazione e niente valori default inventati.
2. **Doppia contabilità della dissipazione:** il modulo M59 bilancia energia elastica e viscosa. Un solutore non lineare deve distinguere energia recuperabile, plasticità, damping e lavoro esterno; il piccolo residuo M59 non certifica un algoritmo plastico.
3. **Soluzione numerica:** aggiornamento di rigidezza, solver non-SPD, criteri di convergenza robusti e rollback; impossibilità di ridurre sempre al problema modale lineare.
4. **Prestazioni:** fattorizzazione per iterazione / passo, matrici sparse e gestione memory/step state. Occorre misurare prima di fissare limiti di modello.
5. **Interpretazione ingegneristica:** nessuna modellazione affidabile di crisi strutturale senza dati di materiale, validazione di laboratorio e standard appropriati.

### Decision gate

M61 può essere **chiuso come studio** quando: questa nota e i benchmark sono revisionati, lo spike scalare è riproducibile e lo script centralizzato per la pulizia è verificato sul Windows dell'utente. Un eventuale lavoro post-M61 parte **solo** con specifica di uno spike SDOF dinamico isolato, criteri di accettazione e nuovi contratti; non sblocca automaticamente l'analisi non lineare dei telai o la verifica antisismica.


## Aggiornamento successivo: M62 (23/10/2026)

Lo studio M61 e la pulizia automatica sono stati validati dai test Windows confermati dall'utente. Il passo successivo **M62** implementa esclusivamente il prototipo dinamico SDOF indipendente descritto in `m62-sdof-elastoplastic-prototype.md`, con i benchmark B1–B4. I benchmark B5–B7 (asta 2D, estremità di telaio, dipendenza dalla mesh) **non sono stati implementati**. Le limitazioni e le esclusioni di questo studio continuano ad applicarsi.
