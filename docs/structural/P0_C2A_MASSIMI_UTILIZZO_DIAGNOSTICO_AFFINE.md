# P0-C2A — Ricerca analitica di massimi per indicatori affini delimitati

**25 ottobre 2026 — codice C# e xUnit predisposti, gate Windows da eseguire.** Primo passo di P0-C2, non sostituisce le regole di verifica EC3/EC5/NTC.

## Perché questa fase è necessaria

M78 riconosce gli estremi di **N, V e M presi singolarmente**. M87-C riunisce stazioni di screening; C1 confronta le risultanti firmate e concomitanti con il FEM. Però il massimo di una funzione di interazione può trovarsi altrove.

Esempio **puramente algebrico**, non una verifica normativa: con `N(t)=t`, `M(t)=1−t` per `t∈[0,1]`, i massimi individuali sono ai due estremi, mentre `η(t)=|N(t)·M(t)|` ha massimo **1/4 nel punto interno t=1/2**. Un test indipendente fissa e controlla questa posizione.

La soluzione corretta deve distinguere il massimo **matematicamente individuato per una precisa funzione e ipotesi** dalla semplice osservazione di una griglia di campioni. Se il dominio fisico o la funzione non appartiene alla famiglia implementata, l'esito resta `ScreeningIncomplete`.

## Scopo stretto implementato in C2A

Nuovo file di produzione **solo** in `StructuralSolver2D.Verification.Integration`:

`FemAffineUtilizationExtremaC2A.cs`

`FemAffineUtilizationExtremaC2A.Locate(model, request)`:

1. Richiede `StructuralModel` valido, **un membro `Frame2D`**, un caso/una combinazione esistente e tipizzata, e tre scale positive e finite in kN/kNm. Le scale sono denominatori di analisi **NON** resistenze EC3/EC5/NTC.
2. Se esiste un carico distribuito uniforme/lineare **sull'asta studiata**, oppure una voce di **peso proprio sull'intero modello**, non usa l'ipotesi affine e restituisce `ScreeningIncomplete` senza massimo. Carichi distribuiti su *altre* aste non sono per questo motivo esclusi: agiscono sulle condizioni d'estremità dell'asta in esame.
3. Riconosce tutti i carichi puntuali attivi sull'asta in quel target, ricavandone i punti di salto. Considera ogni intervallo positivo tra due discontinuità, gli estremi della membratura e **i due limiti separati** nel salto. La combinazione ammette fattori negativi: i segni reali restano nel FEM.
4. Il solver lineare viene eseguito **una volta per target**. Le terne N/V/M alle estremità dei tratti sono lette con `Frame2DInternalForceSampler.EvaluateMemberSection`, non da diagrammi grafici campionati; un controllo al punto medio presidia la coerenza affine del tratto.
5. Per le **sole due funzioni diagnostiche esplicite** qui sotto determina il massimo sui tratti mediante argomento analitico, non con una griglia fissa. Espone tutte le posizioni candidate con ascissa, lato e valore, più il massimo per l'unico target scelto.

### Famiglie matematiche implementate

| `AffineDiagnosticMetricC2A` | Funzione adimensionale | Criterio di massimo su un tratto affine |
|---|---|---|
| `AbsoluteNormalizedForceSum` | `η = |N|/Ns + |V|/Vs + |M|/Ms` | Somma di valori assoluti di funzioni affini: **convessa**. Massimo agli estremi del tratto, compresi entrambi i lati delle discontinuità. |
| `AbsoluteNormalizedAxialMomentProduct` | `η = |(N/Ns)·(M/Ms)|` | Prodotto di due funzioni affini: polinomio di grado al più 2. Controllo degli **estremi e della radice interna della derivata**, se presente. |

`Ns`, `Vs`, `Ms` sono soltanto scale diagnostiche positive stabilite dall'utente. **Nessuna di queste espressioni è una formula normativa di interazione o una verifica di resistenza del membro.** I coefficienti e le formule reali EC3/EC5/NTC devono essere implementati dalle regole normative con dati e limiti fisici propri: non si può trasformare un massimo di questo indicatore in un `DesignOverallStatus.Passed`.

Il metodo numerico usa `double`; i candidati sono ricavati da formule analitiche sotto l'ipotesi di andamento affine, con confronto numerico ai campioni FEM di controllo. **Non è un certificato formale di arrotondamento garantito** né prova che il modello strutturale rappresenti un'opera reale.

## Esiti e divieti di sovrainterpretazione

| Esito | Significato |
|---|---|
| `LocatedWithinAffineAssumptions` | Massimo dei **soli indicatori sintetici supportati** lungo quell'asta, target e famiglia di carichi, con test sui tratti. |
| `ScreeningIncomplete` | Carichi distribuiti/peso proprio, membro non Frame2D o difficoltà geometriche escluse dalla dimostrazione. **Nessun massimo restituito.** |
| `InvalidInput` | Modello FEM, identificatori, tipi, scale o vincoli di input incoerenti. |
| `AnalysisFailed` | FEM non risolvibile o calcolo dell'indicatore non numericamente affidabile. |

Per qualsiasi esito, i flag `NormativeRuleEvaluated`, `MemberAdequacyEstablished`, `WholeStructureVerified` e `CompleteActionSetEstablished` valgono **sempre `false`**. C2A non sostituisce P0-B4/C1/C3, verifiche EC3/EC5/NTC, verifiche dei collegamenti o degli effetti spaziali.

## Esempio manuale e golden indipendenti

Trave appoggiata di `L=4 m`, carico puntuale verticale `P=12 kN` verso il basso in mezzeria, e sforzo assiale concomitante costante `N=−9 kN` (segno convenzionale del FEM):

- reazioni verticali `RA = RB = +6 kN`;
- taglio `V(2−)=+6 kN`, `V(2+)=−6 kN`;
- momento di picco `M(2)=12 kNm`;
- scale sintetiche `Ns=9 kN`, `Vs=6 kN`, `Ms=12 kNm`;
- all'origine `η=1+1+0=2`, ai due lati di `x=2m` `η=1+1+1=3`.

Il nuovo test xUnit calcola questi valori **a mano come riferimento indipendente** e verifica che il percorso C# trovi `ηmax=3` a `x=2m`, conservando entrambi i lati del carico; una combinazione con fattore negativo è un'ulteriore controprova. La routine puramente algebrica del prodotto verifica `ηmax=0,25` nell'interno di `[0,1]` e casi degenere/costante con valori fissati indipendentemente.

## Test da eseguire su Windows

File: `tests/StructuralSolver2D.Verification.Tests/M87/FemAffineUtilizationExtremaC2ATests.cs`.

```powershell
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~FemAffineUtilizationExtremaC2ATests' --logger 'console;verbosity=normal'
.\scripts\run-g7-tests.ps1
```

Attesi: benchmark trave centrale, fattori negativi, massimo interno algebrico, casi costanti e degeneri, rigetto sicuro dei carichi distribuiti/selfweight, input non validi e truss `Unsupported` di fatto classificato `ScreeningIncomplete`. **L'ambiente di preparazione del pacchetto non dispone di dotnet SDK**; non dichiarare C2A superato prima del log Windows.

## Roadmap successiva

- **P0-C2B:** estendere con dimostrazione indipendente a carichi distribuiti e peso proprio, usando i polinomi effettivi N/V/M per ogni tratto (grado fino al cubico con q lineare), con test di massimi interni e discontinuità. Un interpolante non verificato non costituisce una prova di massimo.
- **P0-C2C:** interfaccia per le **vere funzioni di utilizzo** di singole regole EC3/EC5/NTC, con obbligo di un algoritmo di massimo validato per ogni famiglia. Dove manca la certificazione, classificare `SCREENING_INCOMPLETE` e non `MemberPassed`.
- **P0-C3:** copertura per membro × combinazione × stazione/lato × regola, comprensiva di esclusioni motivate e fallimenti non mascherabili. **P0-C4:** reporting esatto vs grafico.
- **P0-E:** limiti fisici dei ritegni, collegamenti, stabilità fuori piano. **P0-D:** ampliamento successivo verifiche normative. **P3:** GUI Avalonia, soltanto alla fine.
