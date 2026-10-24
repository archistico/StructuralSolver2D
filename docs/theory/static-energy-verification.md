# M51 — Verifica energetica e invarianti del FEM statico

**Stato: implementazione iniziale, NON ancora validata dalla suite .NET nel pacchetto consegnato.**
Riferimento teorico: Alberto Carpinteri, *Dinamica delle strutture*, cap. 3, §§3.3–3.8 (energia potenziale, lavori virtuali, condizioni al contorno) e cap. 4, §§4.4–4.6 (telai e reticolari piani). Le formule e la proposta di test sono una rielaborazione indipendente; non includere il PDF nel pacchetto.

## 1. Dove avvengono i passaggi nell'implementazione

- `Frame2DElementMatrices.BuildLocalStiffness`: `k_local` 6×6, con `EA/L` e termini flessionali `EI` per trave Euler–Bernoulli.
- `ApplyMomentReleases` / `ApplyMomentReleasesToStiffness`: condensazione statica di *entrambe* rigidezza e forze nodali locali equivalenti; righe/colonne relative alle rotazioni rilasciate azzerate.
- `BuildTransformation` e `TransformStiffnessToGlobal`: `u_local = T u_global`, `K_global = T^T K_local T`.
- `Truss2DElementMatrices.BuildGlobalStiffness`: matrice 4×4 globale assiale, solo DOF `Ux,Uy`.
- `PlaneStructureAnalyzer.AssembleGlobalStiffness`: assembla matrici globali di elementi telaio + reticolari, con 3 DOF/nodo, includendo rotazioni inattive per gli elementi di reticolare.
- `PlaneStructureAnalyzer.AssembleLoads`: forze/momenti nodali e *carichi equivalenti nodali* delle forze distribuite uniformi, lineari e concentrate sui membri. I carichi di membro sono condensati in presenza di svincoli, trasformati e sommati in `globalLoadVector`.
- `SupportConstraintSystem.SolveConstrainedSystem`: impone vincoli omogenei orientati per mezzo di un sistema aumentato con moltiplicatori di Lagrange; ortonormalizza e deduplica direzioni ridondanti. Può eliminare DOF rotazionali veramente inattivi senza sopprimere meccanismi traslazionali.
- `PlaneStructureAnalyzer.AnalyzeFactoredLoadCases`: calcola `u` dal sistema vincolato, quindi `r = K u - F_equiv` e le reazioni; i risultati pubblici restano invariati.

Gli analyzer `Frame2DAnalyzer` e `Truss2DAnalyzer` condividono gli elementi e `SupportConstraintSystem`, ma hanno assemblaggi autonomi. Il percorso opzionale di audit M51 usa **l'assemblaggio reale di `PlaneStructureAnalyzer`** per modelli puri e misti; in una successiva verifica di chiusura M51 va garantita la copertura incrociata degli altri due percorsi.

## 2. Principio energetico corretto

Per il problema statico lineare con supporti a spostamento imposto *nullo*:

`K u + C^T λ = F_equiv`, `C u = 0`.

Si pone `U = 1/2 u^T K u`, `W = u^T F_equiv`. Moltiplicando l'equilibrio per `u^T` e usando `C u = 0` segue `W = 2 U`.

**Importante:** `F_equiv` deve essere il vettore globale **completo**, non il solo vettore dei carichi nodali espliciti. Un carico distribuito viene già trasformato in forze e momenti nodali equivalenti; uno svincolo cambia sia rigidezza sia vettore di carico. L'energia di deformazione qui definita è la **energia discreta del sistema condensato**. Per carichi applicati all'interno dell'elemento e interpolazione non esatta, non coincide necessariamente con l'energia elastica esatta della trave continua: valutare a parte l'energia particolare degli spostamenti interni se necessario.

L'energia non rappresenta il **lavoro storico** eseguito durante il caricamento (che per una crescita lineare da zero è `W/2`), bensì il prodotto generalizzato di carichi finali e spostamenti finali.

Con spostamenti imposti non nulli, cedimenti, precarichi o termini cinematici, `u^T C^T λ` non è zero e la relazione va estesa includendo il lavoro delle reazioni. L'attuale modello usa solo vincoli omogenei: **non riutilizzare questa formula per future condizioni al contorno senza rivalutarla**.

## 3. Verifica residui e reazioni

`r = K u - F_equiv` è il residuo del sistema **non aumentato**. Su DOF vincolati non deve necessariamente annullarsi: contiene le reazioni. Per evitare la falsa condizione `||r||≈0`, costruire una base ortonormale `Q` per lo spazio delle righe di vincolo, con doppia ortogonalizzazione. Il residuo libero è:

`r_free = r - Q(Q^T r)`.

Si controllano quindi `||r_free||_inf`, `||Q^T u||_inf` e l'energia. Le righe rappresentano vincoli orientati e rotazioni inattive. Le direzioni ridondanti vengono deduplicate; non sono dichiarate instabilità. Le reazioni pubbliche restano quelle già ripartite tra supporti, la cui somma restituisce la reazione nodale.

La verifica verifica l'equilibrio proiettato e non pretende di diagnosticare da sola singolarità, cattivo condizionamento o instabilità; il solver lineare deve continuare a rifiutare i sistemi singolari.

## 4. Invarianti degli elementi

- Simmetria numerica `K≈K^T`, finitezza.
- Conservazione dell'energia per cambio di coordinate `u_g^T K_g u_g = (T u_g)^T K_l (T u_g)`.
- Annullamento delle forze elastiche per movimenti rigidi dell'elemento libero (nel telaio: 2 traslazioni e 1 rotazione; nel reticolare piano: analogamente). Non si applicano questi test al sistema vincolato per cercare nullspace non fisiche.
- Dopo condensazione degli svincoli, simmetria e assenza di deformazione per movimenti rigidi rimangono invarianti.
- Invertendo l'orientazione dell'elemento con carichi espressi nel sistema globale non devono cambiare le risposte globali.

## 5. Tolleranze, scale, casi nulli

Valori iniziali in `StaticSystemVerifier` (da rivedere con test multiscala):

| Grandezza | Disuguaglianza applicata |
| --- | --- |
| Simmetria | `max|Kij-Kji| <= 1e-12 + 2e-8 max|Kij|` |
| Energia | `|u^T K u-u^T F| <= 1e-9 kNm + 2e-8 * Σ_i(|ui(Ku)i|+|uiFi|)` |
| Residuo libero | `||r_free||inf <= 1e-8 + 2e-8*(||Ku||inf+||F||inf)` generalizzato |
| Vincoli | `||Q^T u||inf <= 1e-9 + 2e-8 ||u||inf` |

Le grandezze generalizzate affiancano kN e kNm, m e rad (la norma aggregata va interpretata come un **indicatore numerico**, non come un'unica misura fisica). In presenza di zero carico, zero spostamento o perfetta cancellazione non si divide per valori nulli. `NaN`, infinito e dimensioni incompatibili producono esito negativo/errore esplicito.

**Limite importante:** le soglie qui sono conservative e non equivalgono a una certificazione di accuratezza; grandi differenze di scala tra DOF o sistemi mal condizionati richiederanno tolleranze per DOF/scaling o stime del condizionamento. Non accettare silenziosamente esiti negativi.

## 6. API, test e stato

- `Analysis.Verification.StaticSystemVerifier.Verify(...)` restituisce un `StaticVerificationResult` **interno** (nessuna variazione di JSON, CLI o API pubblica).
- `PlaneStructureAnalyzer.AnalyzeWithStaticVerification` e `AnalyzeCombinationWithStaticVerification` sono **interni** e riusano lo stesso percorso di assemblaggio e soluzione del metodo pubblico, con callback diagnostico solo se richiesto.
- `SupportConstraintSystem.GetHomogeneousConstraintRows` ricrea le stesse condizioni al contorno già usate dal solver, soltanto per l'audit.
- Test in `tests/StructuralSolver2D.Analysis.Tests/Verification/` e casi in `benchmarks/verification/README.md`.

### Gate prima di dichiarare M51 completato

1. Eseguire `dotnet test StructuralSolver2D.sln` su .NET 8 (in questo ambiente non è presente l'SDK).
2. Validare i nuovi confronti su differenti valori di `E`, `A`, `I` e lunghezze, inclusi elementi estremamente sottili e sistemi debolmente vincolati.
3. Aggiungere confronti incrociati dei percorsi `Frame2DAnalyzer` / `Truss2DAnalyzer` rispetto a `PlaneStructureAnalyzer` (oltre alle regressioni preesistenti).
4. Valutare diagnostica dettagliata di near-mechanisms e numeri di condizionamento, senza modificare il criterio di stabilità dei modelli validi.
5. Eseguire la suite reporting/viewer/browser e verificare la stabilità dell'output pubblico.

Il primo nucleo M51 è **implementato ma non dichiarato completo** finché questi gate non sono verdi.

Per lanciare build, test M51 e suite completa in Windows: `powershell -ExecutionPolicy Bypass -File scripts/run-m51-tests.ps1` (PowerShell 7: `pwsh -File scripts/run-m51-tests.ps1`). Lo script interrompe l'esecuzione al primo risultato negativo e non assume che i test siano già stati eseguiti.


## 7. M51 — Analisi opzionale dei meccanismi e del condizionamento (seconda tranche)

È stato introdotto `ConstrainedStiffnessInspector.Inspect(...)`, invocabile internamente su matrici e righe di vincolo; `PlaneStructureAnalyzer.InspectStaticStability(model, loadCaseId)` richiama gli **stessi assemblaggi di rigidezza, carichi e vincoli** della soluzione ordinaria, ma lavora **prima** della soluzione, quindi può ispezionare anche un sistema per cui `Analyze(...)` fallirebbe perché singolare.

### Procedimento

1. Controllo di finitezza, dimensioni e simmetria della matrice `K`.
2. Costruzione di una base ortonormale `Q` dello spazio dei vincoli omogenei (due passaggi di Gram–Schmidt, con scarto delle righe duplicate). Non sono dichiarati meccanismi gli appoggi ridondanti validi.
3. Costruzione di una base ortonormale `Z` degli spostamenti ammissibili, `C Z = 0`, includendo vincoli inclinati e i DOF rotazionali auto-inattivi del modello misto.
4. Proiezione `K_r = Z^T K Z`. **Non** basta estrarre la sottomatrice dei DOF non vincolati quando i vincoli sono inclinati.
5. Equilibrazione diagonale `S = D^(-1/2) K_r D^(-1/2)` con `D = diag(K_r)`, seguita da una decomposizione agli autovalori Jacobi per i piccoli sistemi. Si usano gli autovalori per distinguere `Healthy`, `PoorlyConditioned`, `PossibleMechanism` o `Indefinite`. Gli esiti non conclusivi sono separati (`TooLarge`, `NotConverged`, `InvalidInput`, `FullyConstrained`).
6. Per il minimo autovalore si ricava una direzione di spostamento critica e si riporta il **DOF dominante** (identificativo nodo e `Ux`/`Uy`/`Rz`). Si tratta di un indizio, **non** di una localizzazione certa del guasto o di una forma modale fisica.

### Soglie iniziali e interpretazione

| Indicatore su `S` | Classe diagnostica |
| --- | --- |
| `lambda_min / lambda_max <= 1e-10` | `PossibleMechanism` (meccanismo oppure degenerazione numerica) |
| `1e-10 < lambda_min / lambda_max < 1e-5` | `PoorlyConditioned` (attenzione alla sensibilità) |
| `lambda_min / lambda_max >= 1e-5` | `Healthy` **soltanto** per l'ispezione numerica |
| `lambda_min < -1e-10 lambda_max` | `Indefinite` (rigidezza non definita positiva) |

Vengono intercettate immediatamente le direzioni con rigidezza diagonale proiettata non positiva. Valori prossimi alle soglie possono cambiare classificazione per errori di arrotondamento. Il rapporto è quello della matrice **equilibrata e adimensionale `S`**: non è il numero di condizionamento fisico della matrice originaria `K`, non è indipendente dalla scelta della base `Z` e non dà alcuna garanzia di stabilità geometrica o sicurezza. La base non viene trasformata per normalizzare unità traslazionali e rotazionali: non attribuire significato dimensionale alla norma di `Z`.

Limite di prestazioni: al massimo **60 DOF liberi** per la decomposizione densa esplorativa. Oltre questa dimensione si restituisce `TooLarge`, senza bloccare il solver statico. L'ispezione non è usata in nessun ramo del risolutore ordinario: non modifica tolleranze di pivot, interpretazione dei vincoli, risultati o API/CLI/JSON pubbliche. Per componenti instabili occorrono verifiche ingegneristiche aggiuntive; il primo controllo non è un'analisi di buckling né sostituisce il calcolo non lineare.

### Nuove regressioni

`ConstrainedStiffnessInspectorTests.cs` include matrici analitiche 2×2 (matrici positive definite, senza rango, negative e quasi singolari), fattori uniformi fino a `1e±100`, vincoli inclinati, righe ripetute, tutte le DOF vincolate, valori non finiti e limite di dimensione. Le verifiche sul modello reale comprendono telaio libero, telaio con soli appoggi a rullo, reticolare assiale stabile e telaio incastrato con supporto inclinato ridondante.

**Stato al momento della scrittura:** file sorgenti e test aggiunti, **ma compilazione e suite .NET 8 non eseguite** nell'ambiente di sviluppo. M51 rimane **in corso**: la validazione richiede `scripts/run-m51-tests.ps1`, regressioni multi-scala su modelli e il gate di benchmark/report/browser previsto dalla roadmap.
