# M65 — Dinamica elasto-plastica delle reticolari Truss2D (prototipo interno)

**23 ottobre 2026 · Stato:** implementato nel codice, test Windows .NET 8 non ancora eseguiti. **Base validata:** M64, confermato dall'utente. **Perimetro:** piccole deformazioni, massa traslazionale consistente, solo Truss2D, nessuna verifica normativa.

## Problema fisico e algoritmo

Il modello risolve nel sottospazio cinematicamente ammesso l'equilibrio dinamico

\[
M_r\ddot q+C_r\dot q+Z^Tf_{int}(Zq,z)=Z^TF(t),
\quad M_r=Z^TMZ,\quad C_r=Z^TCZ,
\]

in cui `z` contiene la storia plastica delle aste e `Z` è una base ortonormale compatibile con vincoli omogenei anche inclinati/ridondanti. Gli stati di materiale sono aggiornati soltanto all'accettazione di un **intero** passo; durante le iterazioni tutti i trial sono valutati dallo stesso stato del passo precedente. Non vengono utilizzati modi lineari né introdotte masse artificiose.

La massa traslazionale consistente per ogni asta di lunghezza `L` e massa lineare `μ` è:

\[
M_e=\frac{\mu L}{6}\begin{bmatrix}
2&0&1&0\\0&2&0&1\\1&0&2&0\\0&1&0&2
\end{bmatrix}.
\]

Le unità sono **kN–m–s**: `μ` in kN·s²/m², `M` in kN·s²/m, `K` in kN/m e `C` in kN·s/m. Il peso specifico non viene automaticamente interpretato come massa lineare in questo modello di ricerca.

Lo smorzamento è opzionale ma sempre esplicito: `C = alpha*M + beta*K0`, con `K0` elastica **iniziale e fissa**, non ricalcolata dalla tangente plastica. Entrambi i parametri sono non negativi. Sono accettati soltanto vincoli omogenei a traslazione nulla.

A ogni incremento si usa Newmark ad accelerazione media (`beta=1/4`, `gamma=1/2`), seguito da Newton–Raphson con rigidezza tangente consistente, massa e viscosità:

\[
K_{eff}=K_{t,r}+\frac{M_r}{\beta \Delta t^2}+\frac{\gamma C_r}{\beta\Delta t}.
\]

In caso di mancata convergenza, overflow o errore del trial, il solver restituisce l'ultima storia accettata; nessuno degli stati elastoplastici del passo fallito viene confermato. Un fallimento non viene trasformato in una soluzione fittizia.

## Contratto interno e output

Il codice è in `src/StructuralSolver2D.Analysis/Truss2D/Nonlinear/NonlinearTruss2DNewmark.cs`. La richiesta riutilizza nodi, supporti e carichi M64, aggiungendo massa lineare per ogni elemento, passo temporale uniforme, fattori di carico campionati, coefficienti di smorzamento e opzionale velocità iniziale compatibile con i vincoli. L'assetto iniziale è indeformato con storia plastica vergine; il vettore di spostamenti iniziali non è configurabile.

Ogni campione espone spostamento, velocità, accelerazione fisici, reazioni nodali (compresi contributi inerziali e viscosi), forze assiali, stati plastici e iterazioni Newton. L'energia è divisa in cinetica, recuperabile elastica, incrudimento, dissipazione plastica e viscosa, lavoro esterno e lavoro trapezoidale delle forze interne.

**Il residuo del bilancio discreto non dimostra la precisione temporale.** Il residuo del bilancio energetico fisico resta separato, soprattutto attraverso lo snervamento. I picchi di accelerazione vanno controllati mediante raffinamento del passo temporale.

## Test C# xUnit

Tutti i nuovi benchmark sono C# nella classe `NonlinearTruss2DNewmarkM65Tests`. Non è richiesto alcuno script Python.

1. Asta con massa consistente e risposta elastica del primo passo confrontata con la formula Newmark SDOF chiusa, incluse le **reazioni inerziali** agli appoggi.
2. Risposta plastica del primo passo confrontata con soluzione algebrica indipendente su ramo plastico (non con un'altra esecuzione Newton).
3. Conservazione dell'energia elastica senza smorzamento, e decadimento energetico con viscosità positiva.
4. Scarico/inversione con dissipazione non negativa.
5. Rotazione a 45° con supporto inclinato, e invarianza della risposta assiale.
6. Fallimento controllato di un passo e preservazione integrale dei precedenti stati; prova multi-asta.
7. Convergenza della risposta elastica a rampa rispetto alla soluzione sinusoidale chiusa al diminuire di `dt`.
8. Rifiuto di masse mancanti/non positive e condizioni iniziali incompatibili.

## Limiti e prossimo gate

Il modello è **solo un prototipo interno**. Non supporta strutture Frame2D, instabilità geometrica o P–Δ, plasticità dipendente dalle rotazioni, cedimenti/appoggi mobili, rottura, spostamenti iniziali plastici, substepping automatico, grandi sistemi sparse, domini tempo continui arbitrari, spettro normativo o sicurezza sismica. Non è stata modificata la API pubblica, il JSON o la CLI.

Lo script `scripts/run-m65-tests.ps1` esegue **prima di tutto** `clean-project-build.ps1`, poi build Release, suite numerica M63–M65 e regressione completa. **M65 diventa validato solo dopo l'esecuzione .NET 8 e la conferma del risultato.** M66 approfondirà casi con mesh diverse, limiti del modello e convergenza energetica.
