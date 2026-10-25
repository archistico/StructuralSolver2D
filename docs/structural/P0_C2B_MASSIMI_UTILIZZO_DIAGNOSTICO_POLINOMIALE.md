# P0-C2B — Estremi degli indicatori diagnostici con carichi distribuiti

**25 ottobre 2026 — implementazione e test C# predisposti; gate .NET Windows da confermare.**

Questo documento estende [P0-C2A](P0_C2A_MASSIMI_UTILIZZO_DIAGNOSTICO_AFFINE.md). Il risultato rimane **matematico/diagnostico**, non una verifica di resistenza EC3/EC5/NTC e nemmeno un certificato numerico con intervalli garantiti.

## Qual è il problema risolto

Con carico uniforme, il taglio non è più costante e il momento non è più affine; con carico distribuito linearmente variabile, il momento è un polinomio cubico. Ricercare il massimo del solo `|N|`, del solo `|V|` o del solo `|M|` **non** identifica necessariamente il massimo delle loro funzioni di interazione.

P0-C2B usa le leggi esatte già presenti nel postprocessing del FEM del primo ordine, senza modificare `Core` o `Analysis`. Per **una singola asta Frame2D** e **un solo caso/combinazione** supporta:

- carichi nodali, momenti nodali, carichi concentrati in asta;
- carichi uniformemente distribuiti e distribuiti linearmente variabili, locali o globali nei tipi già supportati dal solver;
- peso proprio esplicitamente dichiarato nel modello, espanso dal software in carichi distribuiti;
- combinazioni lineari, anche con fattori nulli o negativi, entro le ipotesi del FEM esistente.

Le condizioni ai bordi possono variare a causa dei carichi presenti sulle altre aste; questo non altera il grado della legge polinomiale **dell'asta esaminata**. I carichi puntuali sulla stessa asta suddividono la ricerca in intervalli, con lati distinti in ogni salto.

Il componente è `FemPolynomialDiagnosticExtremaC2B` in `StructuralSolver2D.Verification.Integration`. Accetta il contratto di scale/target diagnostici di C2A, ma restituisce un **risultato e uno status diversi** per impedire di chiamare "affine" un tratto cubico.

## Fondamento matematico e calcolo

In un intervallo senza forze concentrate interne, indicando con `x` la distanza lungo l'asta:

- `N(x)` e `V(x)` hanno **grado al più 2** (costanti con il solo carico puntuale; lineari con distribuito uniforme; quadratiche con distribuito lineare);
- `M(x)` ha **grado al più 3**.

Per ogni intervallo viene introdotta la coordinata `t=(x−a)/(b−a)`, con `0≤t≤1`. Quattro valutazioni delle **sollecitazioni firmate e concomitanti** con `Frame2DInternalForceSampler.EvaluateMemberSection` ricostruiscono il polinomio cubico univoco in aritmetica esatta. Il grado massimo di `N/V` viene riportato a 2 e si ripetono **tre confronti con sezioni FEM ulteriori** (`t=0,19; 0,41; 0,73`) per rifiutare ricostruzioni numericamente incoerenti.

| Indicatore sintetico | Metodo sul tratto polinomiale |
|---|---|
| `|N|/Ns+|V|/Vs+|M|/Ms` | Identità `|n|+|v|+|m|=max(sn·n+sv·v+sm·m)` per le **otto** combinazioni di segni. Cerca gli stazionari delle otto funzioni polinomiali e valuta il vero indicatore in ogni candidato. |
| `|(N/Ns)·(M/Ms)|` | Prodotto di polinomi fino al **grado 5**; cerca gli zeri della derivata fino al **grado 4**, non soltanto gli estremi. |

Le radici reali in `[0,1]` vengono isolate ricorsivamente attraverso le radici della derivata, che individuano tratti monotoni, e tramite bisezione. Vengono inclusi gli zeri multipli che non cambiano segno. Sono sempre valutati **entrambi gli estremi** di ciascun tratto e i limiti monolaterali nei punti di carico concentrato. Non viene utilizzata una griglia fissa per decidere i massimi.

### Limite numerico importante

L'algoritmo opera con `double` IEEE-754 e tolleranze esplicite; non usa aritmetica intervallare né un sistema di stima garantita dell'errore. Un punto stazionario ricostruito viene confrontato con una **valutazione FEM diretta**; un'incoerenza produce `ScreeningIncomplete`, non un risultato positivo. La corretta classificazione di casi ill-condizionati non è dimostrata universalmente e rimane un limite da affrontare se si vorranno utilizzare massimi in verifiche normative.

Per il caso di azioni non supportate dal modello, vincoli non fisicamente rappresentabili, truss, 3D, contatto, tension-only, instabilità non lineari, collegamenti o verifica globale: **C2B non introduce nuove capacità nel solver**. I limiti M82 restano i limiti M82. Il successo di questo passaggio non prova che gli input fisici, i ritegni, le combinazioni e tutte le regole applicabili siano completi.

## Esempi a riferimento indipendente

1. **Trave appoggiata `L=4 m`, uniforme `q=−2 kN/m`, N=−9 kN**. Le formule manuali danno `V=4−2x` e `M=4x−x²`. Con `Ns=9`, `Vs=4`, `Ms=4`, l'indicatore somma-abs ha **massimi `2,25` in `x=1 m` e `x=3 m`**, mentre il picco di `|M|` è a `x=2 m`. Questo è un caso determinante: richiede vere stazioni interne della **funzione di utilizzo**.
2. **Trave `L=4 m`, triangolare `q(x)=−x/2 kN/m`, N=−9 kN**. Dall'equilibrio `RA=4/3 kN`, `M=4x/3−x³/12`; il massimo di `|N/9·M/1|` è `32/(9√3)` nella sezione `x=4/√3 m`. Verifica il grado cubico e una stazione irrazionale interna.
3. **Peso proprio dichiarato** per `Area=0,003 m²`, `UnitWeight=78,5 kN/m³`, `L=4 m`: carico equivalente `q=−0,2355 kN/m`, massimo `M=0,471 kNm` al centro. Con `Ns=9`, `Ms=0,471` e `N=−9`, il prodotto normalizzato raggiunge `1` a `x=2 m`.
4. **Quintico ricavato dall'equilibrio:** due distribuzioni triangolari `qx(x)=qy(x)=−x/2` su `L=4 m`, più carico assiale terminale `−9 kN`: `N=−13+x²/4`, `M=4x/3−x³/12`. Per il prodotto `|N·M|`, la derivata soddisfa `5x⁴−204x²+832=0`. Massimo fisico interno in `x=√((204−√24976)/10)`, controprova del grado **5** completo.
5. **Golden puramente polinomiale indipendente dal FEM:** `p(t)=t(1−t)³` presenta massimo interno `27/256` a `t=1/4`; test anche con radice doppia senza cambio di segno. Altri test coprono combinazioni negative, carichi puntuali sovrapposti a distribuiti, input invalidi e flag di non-conformità sempre negativi.

Il riferimento per i numeri non è il risultato di un secondo algoritmo C#: sono le **equazioni di equilibrio** delle travi e la derivazione algebrica del polinomio.

## Contratto di risultato

| Stato | Significato |
|---|---|
| `LocatedWithinPolynomialAssumptions` | Massimo trovato per **uno dei due indicatori matematici**, **una sola asta** e **un solo target** nei tipi di carico supportati, con controlli interni superati. |
| `ScreeningIncomplete` | Ipotesi/ricostruzione non affidabili; **nessun massimo dichiarato**. |
| `InvalidInput` | Modello, identificatori, scale o tipi incoerenti. |
| `AnalysisFailed` | FEM o valutazione non disponibile. |

I flag `NormativeRuleEvaluated`, `MemberAdequacyEstablished`, `WholeStructureVerified`, `CompleteActionSetEstablished` e `NumericalIntervalCertificateEstablished` sono **sempre `false`**. Le scale `Ns`, `Vs`, `Ms` sono numeri diagnostici, **non** resistenze di calcolo. Vietato convertire l'esito in `DesignOverallStatus.Passed`.

## Comandi di collaudo su Windows

```powershell
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~FemPolynomialDiagnosticExtremaC2BTests' --logger 'console;verbosity=normal'
.\scripts\run-g7-tests.ps1
```

**Stato:** C2B nuovo, non compilato in questo ambiente perché l'SDK .NET non è installato. C2A è PASS **su conferma dell'utente**, senza log numerico allegato. La suite Python P0-B1 serve solo a monitorare il generatore di combinazioni e non è un gate C2B.

## Prossime priorità

1. **P0-C2C:** funzioni di utilizzo **vere** per regole EC3/EC5/NTC nelle singole combinazioni/materiali supportati. Richiedere ipotesi e procedura di massimo verificata **per ciascuna formula**; se non c'è, `SCREENING_INCOMPLETE` senza passaggio automatico a conforme.
2. **P0-C3:** matrice degli obblighi membro×combinazione×stazione/lato×regola, incluse mancanze e `Failed` che non scompaiono.
3. **P0-C4:** report che distingua campioni grafici, stazioni calcolate, massimi matematici e vera verifica normativa.
4. **P0-E/D:** vincoli e limiti fisici, collegamenti, instabilità e ampliamento EC3/EC5/NTC.
5. **P3:** GUI Avalonia, tassativamente ultima fase.
