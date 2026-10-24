# M71 — Scelta motivata del modello di plasticità flessionale Frame2D

**Data:** 23 ottobre 2026  
**Stato:** studio e benchmark di progetto C# implementati; build e test da eseguire su .NET 8. M72 rimane **condizionato all'approvazione di questo progetto**.  
**Baseline:** M70-FIX1, test automatici C# confermati superati dall'utente.  
**Perimetro:** analisi piana a piccoli spostamenti/deformazioni, nessun collasso, nessuna verifica NTC/Eurocodici.

## 1. Perché dobbiamo scegliere un modello prima di scrivere Frame2D non lineare

Il codice `Frame2DElementMatrices` usa la trave di Euler–Bernoulli a sei gradi di libertà locali `[ux_i, uy_i, rz_i, ux_j, uy_j, rz_j]`, materiale elastico lineare e trasformazioni ortogonali. M63–M65 introducono già plasticità **assiale** per Truss2D, ma non forniscono una legge verificata per **momento–curvatura**, **momento–rotazione** o **interazione N–M**. Uno svincolo cinematica di momento in un elemento lineare non è una cerniera plastica: il primo ha capacità trasmissiva nulla, la seconda ha capacità e storia dissipativa.

M71 seleziona la formulazione da implementare nel primo prototipo M72, definisce unità e vincoli, i riferimenti indipendenti, i rischi numerici e il gate. **Non abilita nuove capacità di produzione.**

## 2. Alternative esaminate

| Opzione | Idee fondamentali | Vantaggi | Costi / limiti |
|---|---|---|---|
| **A. Cerniere plastiche concentrate alle estremità** | Trave centrale elastica + una o due molle rotazionali storiche in serie | Semplice da isolare e confrontare con soluzioni chiuse, si può controllare esattamente il commit | Rigidezza iniziale aggiuntiva e dipendenza da ipotesi di lunghezza di cerniera, non cattura diffusione della plasticità |
| **B. Integrazione della plasticità distribuita** | Curvatura e legge costitutiva per sezioni, punti di Gauss/fibre lungo l'asta | Migliore localizzazione lungo l'elemento, naturale per N–M | Richiede integrazione sezionale, stato per fibra, sottopassi e controlli accurati di oggettività della mesh |
| **C. Elemento a plasticità diffusa tramite rigidezza secante ad hoc** | Riduzione empirica di EI in base al momento massimo | Economico | Non garantisce tangente coerente, energia o scarico; **non proposto** |

**Decisione proposta:** prototipo di ricerca M72 con **A**, cerniere di estremità *finite* (molle in serie) e trave elastica Euler–Bernoulli, inizialmente solo flessione e carichi quasi statici. Non introdurre modelli sezionali distribuiti né dinamica non lineare prima di validare questo prototipo. L'opzione B resta obiettivo successivo, non impegno automatico.

## 3. Definizioni fisiche e unità

- Matrice di base: `K_beam` 6×6 locale esistente, con `E` [kN/m²], `A` [m²], `I` [m⁴], `L` [m].
- Cerniera rotazionale di estremità: `k_theta` [kN·m/rad], momento [kN·m], rotazione relativa [rad], soglia `M_y` [kN·m], incrudimento `H_theta` [kN·m/rad].
- Trial interno: `M_trial = k_theta (theta_rel - theta_p_committed)`.
- Funzione di snervamento isotropa per il **solo prototipo**: `f = |M_trial| - (M_y + H_theta alpha_committed)`.
- Se `f <= 0`: momento trial, tangente `k_theta`, stato immutato.
- Se `f > 0`: `delta_gamma = f / (k_theta + H_theta)`, `theta_p_trial = theta_p_committed + sign(M_trial) delta_gamma`, `alpha_trial = alpha_committed + delta_gamma`; `M = M_trial - sign(M_trial) k_theta delta_gamma`; tangente coerente `k_theta H_theta / (k_theta + H_theta)`.
- Dissipazione plastica ideale del ramo scelto `D_increment = M_y delta_gamma >= 0`; energia di incrudimento separata. Non chiamarla energia di collasso o danno.

**Avvertenza fisica:** l'incrudimento isotropo produce un allargamento simmetrico della superficie di snervamento, non riproduce l'effetto Bauschinger. Per cicli strutturali realistici andrà valutato un modello cinematico o combinato con test propri. `H_theta=0` può rendere singolare la rigidezza tangente globale sotto controllo di carico: l'arresto deve essere diagnosticato, mai mascherato da un incremento di rigidezza artificiale.

## 4. Accoppiamento trave–cerniera

Sia `theta_ext` la rotazione nodale strutturale e `theta_b` la rotazione dell'estremità della **trave elastica interna**. La rotazione della cerniera è `theta_h = theta_ext - theta_b` (segno da fissare coerentemente col sistema locale). Equilibrio locale: momento della trave e momento della cerniera sono uguali in modulo e coerenti in segno.

Per un riferimento scalare con traslazioni bloccate e un'estremità opposta incastrata, la trave elastica presenta `k_b = 4EI/L`. In regime elastico la coppia molla–trave in serie produce:

`k_eff = (k_b k_theta)/(k_b + k_theta)`.

Questa è una **riduzione reale della rigidezza elastica pre-snervamento** rispetto alla trave rigida di estremità. Non impostare `k_theta = infinito` in floating point; determinare se il modello M72 deve assumere la piccola compliance pre-snervamento come dato di input esplicito oppure progettare una formulazione a vincolo esatto. Un valore `k_theta` molto alto degrada il condizionamento: la scelta richiede test quantitativi in funzione di `k_theta/k_b`.

Nell'elemento completo: introdurre due rotazioni interne come incognite locali, assemblare le forze/residui e la tangente consistenti **prima** di condensare i gradi interni. Usare la condizione di equilibrio locale al punto convergente e la condensazione tangente coerente, evitando la condensazione **statica lineare** delle release per fingere una legge plastica. Se l'equilibrio locale fallisce, rollback dell'intero incremento e dello stato delle due cerniere.

## 5. Cosa NON considerare supportato in M72 v1

- Nessuna plasticità distribuita lungo la trave e nessuna identificazione automatica della posizione della cerniera al centro campata.
- **Nessuna interazione N–M**, nessuna verifica del criterio di resistenza della sezione; il comportamento assiale rimane elastico nell'elemento pilota. Imporre controlli espliciti sul dominio d'impiego; non dichiarare affidabilità per forze assiali elevate.
- Nessuna instabilità locale/globale, no P–Delta, grandi rotazioni, softening, danno, fatica, rottura o degradazione isteretica.
- Nessuna assimilazione automatica fra release `Frame2D` elastica e cerniera plastica; definizioni e serializzazioni devono rimanere disgiunte.
- Nessuna integrazione col ramo modale/armonico/sismico lineare per analisi non lineari senza una progettazione e validazione specifica.
- Nessuna derivazione di rigidezza o capacità di momento dal solo `I`/`A`: `M_y`, `k_theta` e `H_theta` richiedono dati di sezione/materiale dichiarati. In futuro N–M richiederà modello di sezione esplicito.

## 6. Matrice di benchmark per il gate M72

| ID | Configurazione | Proprietà da dimostrare | Origine del riferimento |
|---|---|---|---|
| H01 | Rotazione elastica di trave con altra estremità incastrata | `K[rz_j,rz_j] = 4EI/L`, `K[rz_i,rz_j] = 2EI/L` | Matrice Euler–Bernoulli esistente |
| H02 | Molla elastica in serie alla trave | `k_eff = k_b k_theta/(k_b+k_theta)` per più rapporti di rigidezza | Soluzione scalare algebrica |
| H03 | Singola cerniera monotona | `M_y`, `delta_gamma`, tangente del ramo plastico | Return mapping algebrico indipendente in test C# |
| H04 | Scarico e inversione ciclica | Stato committato immutato nel trial, dissipazione non negativa | Stato di prova C# |
| H05 | Derivata della tangente | Confronto con differenze finite *dentro il ramo*, lontano dalla soglia | Derivata numerica C# |
| H06 | Elemento completo con una/due cerniere | Matrice 6×6 simmetrica nei casi appropriati, equilibrio e energia | Equilibrio locale e differenze finite |
| H07 | Orientamento e inversione nodi | Stesse quantità fisiche con segni corretti | Trasformazione locale/globale |
| H08 | Due campate e storia incrementale | Iterazioni convergenti, redistribuzione, nessuna mutazione di passo rifiutato | Modello in serie / riferimento analitico |
| H09 | Cerniera perfettamente plastica | Non unicità/meccanismi identificati e arresto esplicito | Diagnostica singolarità |
| H10 | Mesh, rigidezza e limiti | Sensibilità alla lunghezza di elemento/cerniera e condizionamento | Serie di raffinamenti indipendenti |

Il file `tests/StructuralSolver2D.Analysis.Tests/Frame2D/M71/PlasticHingeFeasibilityM71Tests.cs` implementa **solo i riferimenti H01–H05** e non costituisce una prova della funzionalità M72, che non esiste ancora.

## 7. Piano di integrazione tecnica M72 se approvato

1. Definire DTO interno non lineare separato, con `MomentYield`, `RotationalStiffness`, `Hardening` e flag di estremità, con validazione numerica e dimensionale; niente modifica di massa o release legacy.
2. Implementare `HingeStateCommitted` immutabile / `EvaluateTrial` non mutante / `CommitTrial` con controllo delle revisioni, sul modello M63, ma usando correttamente momenti e rotazioni.
3. Integrare i due gradi interni nel Frame2D locale, risolvere l'equilibrio e condensare tangente e forze su sei gradi di libertà esterni. Benchmark con una sola cerniera prima di due.
4. Aggiungere un solutore statico incrementale di telai, riutilizzando M64 per i principi di Newton, vincoli, commit atomico e rollback, senza applicare automaticamente il solutore Truss alle rotazioni Frame.
5. Validare a livello di elemento, poi di struttura, poi studiare eventuali API/JSON/viewer; non esporre una feature pubblica prematuramente.

**Condizioni per considerare concluso M71:** documento approvato per ipotesi e scelta, H01–H05 eseguiti su .NET 8, compatibilità con M70 mantenuta; nessuna funzionalità Frame non lineare dichiarata come già presente.
