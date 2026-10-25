# P0-D0M2 — classificazione NTC 2018, ramo elastico ψ < −1

**Stato al 25/10/2026:** ramo aggiuntivo implementato con controllo esplicito della nota di Tab. 4.2.III, rapporto ψ tra le tensioni elastiche ai bordi e compatibilità elementare col segno di N; **33/33 golden indipendenti PASS in Python**, test C#/.NET **PENDING** (SDK indisponibile). Non è verifica di resistenza.

## Perché serve un ramo distinto

Il precedente P0-D0M1 gestiva anime I/H soggette a **N di compressione e M forte concomitanti** con `−1 < ψ < 1`; nel restante caso `ψ ≤ −1` la Tabella NTC adotta un'espressione **diversa** per la classe 3, corredata da una condizione in nota. Il caso era deliberatamente `Unsupported` senza una verifica autonoma.

**Condizione fisica individuata prima di abilitare il ramo:** per una I/H doppiamente simmetrica con tensioni elastiche lineari in una singola sezione, indicando con `C` la tensione media **di compressione positiva** e con `B>0` il contributo di flessione al bordo dell'anima, i due bordi hanno tensioni `C+B` e `C−B`. Con N FEM in kN, M in kNm, A in mm² e inerzia I in mm⁴, le grandezze coerenti in MPa sarebbero `C=−1000 N_FEM/A` e `B=10⁶ |M_FEM| y_bordo/I`; l'implementazione attuale **non le ricava automaticamente**. Quando un bordo è compresso e l'altro teso:

\[
\psi=\frac{C-B}{C+B}\,.
\]

- Se `N_FEM < 0` (compressione netta), allora `C>0` e **ψ>−1**: è il ramo originario D0M1.
- Se `N_FEM > 0` (trazione netta) e la flessione crea comunque una zona compressa, `C<0` e **ψ<−1**: è il nuovo ramo D0M2, con `0<α<0,5` per il blocco plastico compresso.
- Se `N_FEM = 0`, **ψ=−1** è il limite di **flessione pura**, già trattato da D0. La tabella include matematicamente l'uguaglianza, ma D0M2 non usa quel caso come finta sollecitazione mista.

Perciò **D0M2 ammette solo N positivo di trazione, M forte non nullo, V nullo e ψ strettamente minore di −1**. Una coppia N negativo/ψ<−1 è respinta, anche se la nota tabellare fosse soddisfatta.

Ho confrontato la **scansione visuale ufficiale** del D.M. 17/01/2018, NTC 2018, §4.2.3.1, Tab. **4.2.III**, pagina stampata 95, e Tab. **4.2.IV** nella pagina successiva. Fonte primaria: [Gazzetta Ufficiale, NTC 2018 (PDF)](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf).

La nuova formula è:

\[
 \frac{c_w}{t_w} \leq 62\varepsilon(1-\psi)\sqrt{-\psi},\qquad \psi\leq -1
\]

con `ε = √(235/fyk)`. Il simbolo `ε` qui è **il fattore normativo di snervamento**, non la deformazione a trazione `ε_t` usata nella nota. Il ramo già disponibile per `ψ > −1` rimane `42 ε/(0,67+0,33 ψ)` e **non** va extrapolato a `ψ = −1`.

### La condizione in nota è obbligatoria

Per usare il ramo normativo `ψ ≤ −1` (sottocaso fisico implementato `ψ < −1`), la nota di Tab. 4.2.III prevede **almeno una** delle condizioni:

1. tensione di compressione `σ_c ≤ fyk`; **oppure**
2. deformazione a trazione `ε_t > fyk/E`.

Sono disgiuntive (OR); il secondo confronto è **strettamente maggiore**. Il classificatore controlla **numericamente** i dati positivi/finiti forniti e rifiuta l'operazione se entrambi i confronti falliscono o se mancano i dati. **Richiede inoltre le due tensioni elastiche ai bordi dell'anima, come magnitudini positive di compressione e trazione, e verifica che `ψ = −σt,elastica/σc,elastica`** entro tolleranza numerica esplicita. Il modulo `E` in MPa è **dichiarato e documentato esternamente**, non letto o ipotizzato dal FEM. La validità fisica delle misure di tensione/deformazione e la loro compatibilità con N/M e α rimangono **prove esterne**: una dichiarazione non equivale a certificazione del campo tensionale.

## Modifica software minima e retrocompatibile

- Servizio preesistente `Ntc2018SteelMixedIClassifierD0M1` esteso al solo secondo ramo della **classe 3 dell'anima**; le formule per α plastico (classi 1/2) e per l'ala (9/10/14 ε) restano invariate.
- Record nuovo `Ntc2018SteelMixedPsiBelowMinusOneEvidenceD0M2`: `CompressionStressMagnitudeMpa`, `OppositeElasticTensileStressMagnitudeMpa`, `TensileStrain`, `YoungModulusMpa`, fonte documentale. Campo **opzionale** `PsiBelowMinusOneEvidence` sul record D0M1, così i vecchi chiamanti e i test D0M1 continuano a usare la stessa API.
- Il ramo `ψ < −1` è **negato** senza questo record completo e verificabile numericamente. `ψ > −1` ignora del tutto il nuovo campo: non cambiano i calcoli precedenti. `ψ=−1` con N non nullo è rifiutato per incompatibilità fisica con il modello elastico simmetrico.
- `Class3Branch` e `Trace` indicano il ramo, i due estremi elastici e le alternative della nota; `SignedAxialForceKn` espone il segno N anche nel nuovo caso a trazione. Il vecchio campo posizionale `SignedCompressionKn` resta per compatibilità binaria/sorgente, ma non va interpretato come sempre negativo.
- Permangono le guardie di geometria e provenienza D0M1: laminati I/H paralleli e senza fori, `M` forte non nullo e `V = 0` esattamente, ID e stazione/lato FEM documentati, provenienza indipendente di α e ψ, classi ordinate e fattore `fyk` documentato. **Il segno di N distingue i due rami**: N<0 con ψ>−1 per D0M1, **N>0 con ψ<−1 e 0<α<0,5** per D0M2. INP, saldati, condizioni fuori piano e geometrie/azioni non ammesse sono esclusi.

## Controprove aritmetiche indipendenti

Lo script `benchmarks/independent/p0d0m2/verify_p0d0m2_ntc_psi_footnote.py` esegue **33** controlli con `fractions.Fraction` e disuguaglianze esatte per delimitare `√2`: formule, rami della nota, coppie di tensioni, compatibilità **segno N↔ψ** e casi fisici illustrativi. Non importa e non esegue il C#.

| ψ | Formula limite classe 3, con fyk=235 MPa e ε=1 | Esito esemplificativo |
|---|---:|---|
| −1 | 124 | limite tabellare aritmetico; caso fisico di flessione pura gestito da D0 |
| −2 | `186√2 ≈ 263,044` | `250` in classe 3, `300` in classe 4 |
| −4 | 620 | `620` in classe 3, `621` in classe 4 |
| −9 | 1860 | limite aritmetico esatto per il solo caso tabellare |

Controlli della nota: compressione `235 MPa` con `fyk=235` ammessa per uguaglianza; `σ_c=300 MPa`, `ε_t=0,003`, `E=210000 MPa` ammessa per seconda alternativa; `ε_t=fyk/E` **non** supera la seconda alternativa. Due coppie coerenti a ψ=−2: compressione/tensione elastica **100/200 MPa** (soddisfa la prima alternativa) e **300/600 MPa** (soddisfa la seconda con εt=600/E). E e deformazioni sono **dati di esempio dichiarati**, non derivati dal FEM.

Gli xUnit nuovi `tests/StructuralSolver2D.Verification.Tests/D0/Ntc2018SteelMixedPsiBelowMinusOneD0M2Tests.cs` includono formula, radice, limiti inclusivi di classe, entrambe le alternative, dati mancanti/non finiti, uguaglianza nella nota, **rifiuto di ψ=−1 con N≠0**, segni N/α incoerenti, preservazione del ramo precedente e uso del catalogo OPPO. Non sono stati eseguiti in .NET.

### Comandi Windows, quando disponibili

```powershell
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~Ntc2018SteelMixedPsiBelowMinusOneD0M2Tests|FullyQualifiedName~Ntc2018SteelMixedClassificationD0M1Tests|FullyQualifiedName~Ntc2018SteelISectionClassificationD0Tests' --logger 'console;verbosity=normal'
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --logger 'console;verbosity=normal'
.\scripts\run-g7-tests.ps1
python .\benchmarks\independent\p0d0m2\verify_p0d0m2_ntc_psi_footnote.py
```

**Nuovo gate D0M2 PENDING**, con predecessori C2B/C2C0/C2C1/D1/D0/D0M1 ancora PENDING; G7/B2/B3/B4/C1 FIX1/C2A superati nei perimetri già confermati dall'utente.

## Cosa resta realmente aperto

1. Calcolo autonomo, confrontato con esempi di meccanica delle sezioni, di **α plastico** e **ψ elastico** a partire da una stessa sezione e coppia N/M, compresa la compatibilità delle ipotesi sull'elasticità e sulle deformazioni limite.
2. Verifica tecnica delle **misure fisiche della nota** dal reale prodotto/profilo; attualmente sono dati esterni, non certificati. Il software rifiuta omissioni evidenti ma non identifica una dichiarazione fisicamente falsa.
3. Classi 4 **senza** `Aeff/Weff` calcolate; **D2** per resistenza e interazioni N/M/V, **E** per stabilità e ritegni fuori piano, **C3/C4** per completezza delle regole/stazioni, **D3** SLE. Nessuna promozione di una classe 1/2/3 a sicurezza della sezione o della membratura.
4. Test .NET reali e confronto con riferimenti ingegneristici indipendenti per casi misti; **non chiedere un PASS all'utente finché non può collaudare**.

**Gerarchia del significato:** classificazione del piatto → eventuali proprietà efficaci → resistenza di sezione → stabilità di membro → copertura di tutte le verifiche. D0M2 copre soltanto il **primo** anello del sottoinsieme esaminato.
