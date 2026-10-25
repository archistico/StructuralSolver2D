# P0-C2C0 — Contratto fra verifiche normative di sezione e massimi sulla membratura

**25 ottobre 2026. Implementazione e xUnit predisposti; test .NET Windows in attesa.** Il gate precedente **P0-C2B non è stato eseguito dall'utente**, che ha chiesto di continuare senza poter collaudare. Si tratta quindi di un avanzamento **indipendente e conservativo**, non della promozione a PASS di C2B o dell'inizio delle verifiche normative globali.

## Perché serve un contratto separato

Il `DesignCheckEngine` esistente valuta `IDesignCheckRule` in **una stazione**: può restituire `Passed` oppure `Failed`, con domanda, resistenza e utilizzo. Un insieme di stazioni con esito `Passed` **non** dimostra che tra quelle stazioni la funzione di utilizzo normativa sia inferiore a 1. Il massimo di `N`, `V`, `M` separati non colma la lacuna; lo stesso vale per i due indicatori **sintetici** di C2A/C2B.

Il nuovo `NormativeRuleStationEnvelopeAuditorC2C0`, isolato in `StructuralSolver2D.Verification.Integration`, mantiene una separazione netta tra:

1. **regola realmente calcolata in una sezione** da `DesignCheckEngine`;
2. **utilizzo massimo osservato** tra un insieme finito di quelle sezioni;
3. **massimo della funzione normativa su tutti gli intervalli**, tuttora **non dimostrato**;
4. **idoneità dell'intera membratura**, che richiede inoltre copertura di tutte le combinazioni, tutte le regole applicabili e ipotesi fisiche valide.

L'API non dichiara mai `Passed` per la membratura; **non** implementa nuove equazioni EC3/EC5/NTC.

## Funzionamento di C2C0

`NormativeRuleStationPlanC2C0` contiene **un membro**, **una versione esplicita di norma e annesso**, **un solo RuleId**, **uno stato limite**, **un target FEM dichiarato** e una collezione di veri `DesignCheckReport` per singola stazione.

Il controllo:

- pretende identità coerenti di membro/sezione, materiale, norma/edizione/annesso, regola, caso/combinazione, `AnalysisId`, posizione e **lato** della sezione;
- rifiuta duplicati della stessa ascissa e lato (consente i **due lati diversi** di un carico concentrato);
- impedisce di unire `NormativeProfile` diversi, coefficienti/sorgenti differenti o una lettura `ExternalManual` spacciata per dato `FemAnalysis`;
- per le regole `Passed`/`Failed` controlla l'esistenza di **domanda, resistenza, utilizzo, unità, clausola e traccia**, la finitezza e la coerenza `utilization ≈ demand/resistance`; rifiuta `Passed` con utilizzo superiore a 1 oppure `Failed` con utilizzo non superiore a 1;
- raccoglie il **massimo solo osservato** fra le stazioni effettivamente calcolate. Se una stazione è `MissingData`, `Unsupported` o `NotApplicable`, non inventa un valore di utilizzo;
- se esiste almeno un `Failed` calcolato, restituisce **`ObservedRuleFailure`** anche quando altre stazioni sono incomplete. Nessuna mancanza nasconde il fallimento osservato;
- con tutte le stazioni `Passed` restituisce **`ScreeningIncomplete`**, sempre: non esiste ancora una prova della funzione normativa **tra** le sezioni.

La verifica di coerenza del rapporto **non autentica la provenienza FEM**: i report possono essere costruiti manualmente da chiamanti esterni. Per la concomitanza **N/V/M reale** resta necessario l'auditor FEM **P0-C1**, mentre **P0-B4** può esaminare soltanto un insieme dichiarato di combinazioni. Il risultato C2C0 espone `CapturedReportsIndependentlyAuthenticated=false`.

| `Status` C2C0 | Significato e limite |
|---|---|
| `InvalidInput` | Identificativi/valori/provenienza incoerenti o risultato manipolato. Non si espone un massimo osservato fondato su dati inconsistenti. |
| `ObservedRuleFailure` | **Una specifica regola** risulta fallita in almeno una stazione dichiarata; non si certificano il massimo continuo o la struttura. |
| `ScreeningIncomplete` | Nessuna violazione osservata, oppure dati mancanti/non applicabili/non implementati. **Mai un pass dell'asta.** |

Tutti i seguenti flag sono **sempre `false`**: `ContinuousNormativeMaximumEstablished`, `NormativeRuleIntervalCoverageEstablished`, `CompleteActionSetEstablished`, `MemberAdequacyEstablished`, `WholeStructureVerified`, `CapturedReportsIndependentlyAuthenticated`.

## Matrice tecnica delle famiglie normative esistenti

Questa tabella riflette **i sorgenti esistenti nel checkpoint** e specifica quali ulteriori obblighi servono. Non rappresenta un'interpretazione aggiornata delle norme o una certificazione del codice:

| Regole/moduli | Implementato ora | Ostacolo principale alla prova del massimo dell'asta | Priorità successiva |
|---|---|---|---|
| **Acciaio EC3 M89-A/B**: flessione elastica classe 3 e azioni pure | Esiti locali con ipotesi strette; le regole pure spesso esigono altre azioni **esattamente nulle** | Dimostrare che la **purezza dell'azione vale sull'intero intervallo**, oltre a mappa assi, sezione, fori, proprietà e coefficiente costanti. Massimo del solo momento non basta se compare taglio. | C2C1, primo pilota su un caso veramente puro |
| **Acciaio EC3 M90-C**: interazione elastica N–M con taglio delimitato | Sezione classe 3, `|N|/A+|M|/W` e soglia taglio; evidenze di classe e geometria | Riconoscere le **transizioni di ramo** (N/M nulli, V sopra soglia, classe ammissibile) e massimizzare la *specifica* funzione senza perdere l'ammissibilità locale. | C2C2, dopo C2C1 |
| **Acciaio EC3 M91-A/B/C**: stabilità | Verifiche delimitate da dati dichiarati e lunghezze/ritegni esterni | Il FEM piano non prova vincoli fuori piano, svergolamento, imperfezioni o collegamenti. Separare dati esterni da risultati derivabili da N/V/M. | P0-E, poi C2C dedicato |
| **Legno EC5 M92-A/B e NTC G2**: resistenze rettangolari pure | Formule locali e coefficienti selezionati, con limiti di applicabilità | Dimostrare sezione e coefficienti costanti e la purezza di N, V, M **lungo tutta l'asta**; considerare segni e stati esclusi. | C2C1, dopo pilota acciaio |
| **Legno NTC G3**: tenso/pressoflessione e taglio concomitante | Formule locali diverse per tensione/compressione; per il taglio un'inequazione distinta | Le formule contengono **potenze, massimi tra espressioni e rami fisici**; massimizzare ogni dominio applicabile e certificare l'unione. Inoltre G3 richiede evidenza per stazione/lato. | C2C2 per famiglie di interazione |
| **Legno NTC G4**: instabilità | Formule pure sotto ipotesi e ritegni dichiarati | Necessita prova fisica dei ritegni, lunghezze e legami fuori piano; senza P0-E non si può emettere membro `Passed`. | P0-E, poi riesame |
| **Legno NTC G5**: deformazioni SLE | Tre verifiche delimitate, combinazioni e deformazioni differite | C2A/C2B cercano estremi **di forze**, non di frecce SLE. Occorre un metodo distinto per profilo degli spostamenti e scenari temporali. | C2C-SLE specifico |
| **NTC G6 / EC3 M91-D / P0-B4**: auditor | Copertura di **insiemi dichiarati**, non inventario completo della realtà | Non dedurre che un elenco fornito dal chiamante sia esaustivo rispetto a tutto ciò che è fisicamente e normativamente richiesto. | P0-C3 |

## Test predisposti e limite attuale

Nuovo file `tests/StructuralSolver2D.Verification.Tests/M87/NormativeRuleStationEnvelopeC2C0Tests.cs`: esercita veri oggetti `DesignCheckReport` prodotti dal `DesignCheckEngine` con una **regola fittizia esclusivamente di test** (non è EC3/EC5/NTC), quindi controlla il contratto e non la correttezza di una norma. I casi includono: due `Passed` senza falso membro positivo, `Failed` non cancellato da `MissingData`, `Unsupported`, due lati nello stesso salto, duplicati, utilizzo manipolato, target/membro/edizione incoerenti, profilo mutato, provenienza manuale.

Su Windows, quando sarà possibile:

```powershell
# Gate precedente ancora PENDING: prima verificare C2B
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~FemPolynomialDiagnosticExtremaC2BTests'

# Nuovo gate C2C0 (separato dal precedente)
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~NormativeRuleStationEnvelopeC2C0Tests'

# Regressione storica
.\scripts\run-g7-tests.ps1
```

L'SDK .NET **non è presente nell'ambiente di preparazione**. Nessun test C2B/C2C0 è dichiarato superato.

## Roadmap

1. **C2C1:** selezionare una vera formula normativa **a dominio strettamente dimostrabile** (ad esempio un'azione pura costante lungo tutta l'asta), valutare la regola registrata con dati e fonti completi e dimostrare il suo massimo con un criterio matematico specifico e golden indipendenti. Nessuna estensione implicita a stati misti.
2. **C2C2:** interazioni N–M–V, rami e limiti applicativi con dimostrazioni per ogni intervallo e lato; casi fuori perimetro classificati `ScreeningIncomplete`/`Unsupported`.
3. **C3:** matrice obblighi membro × combinazione × norma/edizione × regola × stazione/lato, con preminenza `Failed` e incompletezza non occultabile.
4. **C4:** report espliciti su risultato sezionale, massimo osservato, massimo dimostrato e comportamento non supportato. **P0-E/D:** limiti fisici e verifica normativa ampliata.
5. **P3:** GUI C#/Avalonia **soltanto alla fine**.
