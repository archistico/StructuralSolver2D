# P0-C2C1 — Pilota normativo: massimo costante della sola trazione lorda EC3

**25 ottobre 2026 — codice, xUnit e golden aritmetici predisposti; gate Windows C2C1 IN ATTESA.**
Anche **C2B e C2C0 restano PENDING**, perché l'utente non può eseguire test ora.

## Scopo realmente coperto

Si utilizza **una regola già esistente**, `SteelGrossTensionYieldRuleM89B`:
EN 1993-1-1:2005+A1:2014, §6.2.3(2)(a), Eq. 6.6,
**solo snervamento della sezione lorda in trazione**. La regola è richiamata
tramite `IDesignCheckRule` e `DesignCheckEngine`; il nuovo modulo non contiene
né sostituisce la formula di resistenza. Occorrono catalogo, qualità dell'acciaio
e `gamma_M0` dichiarato con la sua fonte, come nei test preesistenti M89-B.

Si considera **un unico membro `Frame2D`**, **un solo caso/combinazione esplicito**,
**prima analisi lineare statica**. Il risultato documenta il valore della
specifica funzione normativa lungo il membro **soltanto se la funzione è
costante nell'intero dominio per le ipotesi algebriche del FEM in uso**. Il
nome `RuleSpecificMaximumUnderConstantActionAssumptions` è intenzionale:
non significa che sia stata effettuata la verifica di conformità dell'asta.

## Perché il pilota ha un massimo riconoscibile

La legge usata realmente dal `Frame2DInternalForceSampler`, su un membro
privo di carichi applicati direttamente, è:

\[
N(x)=-N_{i},\qquad V(x)=V_{i},\qquad M(x)=-M_{i}+V_{i}x.
\]

La condizione verificata **senza arrotondamenti permissivi** ai due estremi
è `N(0)==N(L)>0`, `V(0)==V(L)==0`, `M(0)==M(L)==0`. Nessun carico
concentrato o distribuito sul membro è ammesso nei casi costituenti;
`SelfWeight` attivo è escluso anche se apparentemente trascurabile.
Da queste ipotesi segue, per il *modello matematico implementato*,
`N(x)=N0>0`, `V(x)=M(x)=0` in ogni ascissa. La regola di snervamento lordo
calcola quindi ovunque lo stesso rapporto `N0/Npl,Rd`. Entrambe le sezioni
estreme vengono realmente valutate con il `DesignCheckEngine`, poi sottoposte
al controllo C2C0 e confrontate domanda, resistenza e utilizzo.

**Limite numerico:** i numeri sono `double`; non è implementata aritmetica
intervallare o un limite rigoroso dell'errore di arrotondamento. In particolare,
un taglio/momento anche piccolo **ma non esattamente nullo** dà
`ScreeningIncomplete`: nessuna approssimazione arbitraria a zero permette di
attivare una regola di azione pura.

## Contratto e risultati

Componente: `Verification.Integration/FemSteelGrossTensionMaximumC2C1.cs`.
Il modulo non introduce dipendenze del motore FEM verso Steel o Catalogs:
riceve dall'esterno l'implementazione `IDesignCheckRule`, controlla
`RuleId`, clausola, edizione e materiale ammessi e la valuta nel motore
normativo esistente. La **autenticità dell'implementazione fornita** non è
verificata indipendentemente: il flag `RuleImplementationIndependentlyAuthenticated`
resta **sempre falso**. Anche il chiamante deve utilizzare la vera regola
M89-B; i test di integrazione lo fanno.

| Esito C2C1 | Significato |
|---|---|
| `RuleSpecificMaximumUnderConstantActionAssumptions` | Rapporto **di sola Eq. 6.6** uniforme nel membro, domanda/resistenza calcolate e ≤1; **nessun pass dell'asta** |
| `ObservedRuleFailure` | La regola di snervamento lordo risulta `Failed`; il fallimento non viene nascosto da altre lacune |
| `ScreeningIncomplete` | Carichi sul membro, sforzi misti, compressione, dati/valutazione mancanti o incoerenti; non si asserisce alcun massimo |
| `InvalidInput` | Modello, assegnazione del membro, norma/regola, target o provenienza dichiarata non validi |
| `AnalysisFailed` | Il solver, il trasferimento FEM o la lettura delle sezioni non riescono |

**Sempre `false`:** `CompleteActionSetEstablished`, `AllApplicableRulesChecked`,
`MemberAdequacyEstablished`, `WholeStructureVerified`,
`NumericalIntervalErrorBoundEstablished`,
`RuleImplementationIndependentlyAuthenticated`.

La proprietà `ConstantGrossYieldUtilization` è valorizzata **solo** per
`RuleSpecificMaximumUnderConstantActionAssumptions` o `ObservedRuleFailure`.
Il risultato C2C0 interno rimane intenzionalmente `ScreeningIncomplete`
anche per tutte le stazioni locali `Passed`: la giustificazione algebrica
di C2C1 è separata e valida esclusivamente per questo pilota.

## Golden indipendenti

Dati test illustrativi: IPE 200 nominale OPPO, `A=28,48 cm²`, S235,
`fy=235 MPa` nel campo di spessore pertinente, `gamma_M0=1` **fornito
esplicitamente nel test**. Il riferimento aritmetico non viene estratto
dall'output del software:

\[
N_{pl,Rd}=28,48\cdot235\cdot0,1/1=669,28\ \mathrm{kN}.
\]

| Caso indipendente | `N_Ed` [kN] | Rapporto atteso | Esito **della sola regola** |
|---|---:|---:|---|
| Trazione costante | +200 | 0,2988285919 | Passed |
| Combinazione illustrativa `1,5×300 + 0,5×(-100)` | +400 | 0,5976571838 | Passed |
| Trazione oltre la resistenza lorda | +700 | 1,0459000717 | Failed |

Script senza dipendenze: `python3 benchmarks/independent/p0c2c1/verify_p0c2c1_gross_yield.py`.
**Risultato locale:** 3/3 riferimenti aritmetici PASS. Questo **non testa**
il collegamento C# né il FEM.

Nuovi xUnit:
`tests/StructuralSolver2D.Verification.Tests/M87/FemSteelGrossTensionMaximumC2C1Tests.cs`.
Oltre ai tre valori, testano taglio minimo non nullo, momento da coppia,
compressione, carichi puntuali/distribuiti, SelfWeight, coefficienti/dati
mancanti, rule ID e target errati.

Quando sarà possibile, da Windows:

```powershell
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~FemSteelGrossTensionMaximumC2C1Tests' --logger 'console;verbosity=normal'
```

Verificare anche i gate P0-C2B/P0-C2C0 ancora aperti e la regressione G7.

## Cosa resta escluso — requisiti prima di un risultato professionale

- **Rottura netta** `Nu,Rd`, fori, collegamenti, bulloni/saldature, piatti e
  distacco: **non implementati nel pilota**. Un valore di Eq. 6.6 ≤1 non basta
  a certificare la trazione resistente richiesta dalle norme.
- Stati di compressione, interazione N–M–V, instabilità globale e fuori piano,
  torsione, effetti geometrici/secondo ordine: **non coperti**.
- Carichi distribuiti, peso proprio, salti, rapporti che variano con la
  posizione: **ritorno incompleto**, non interpolazione di verifiche locali.
- Validazione della provenienza esterna dei materiali/profili, autenticità
  del `IDesignCheckRule` fornito, scelta normativa di `gamma_M0`, tutte le
  condizioni reali di montaggio e la completezza di combinazioni: **richieste
  ulteriori verifiche**.
- Le quattro casette G8B **non entrano nel dominio del pilota** per semplice
  somiglianza dei profili; non sono dichiarate conformi.

**Prossimo:** collaudo Windows C2B/C2C0/C2C1; poi C2C2 interazioni con rami
di applicabilità, C3 copertura obblighi, C4 report, P0-E fisica, P0-D
ampliamento delle norme. Avalonia soltanto P3.
