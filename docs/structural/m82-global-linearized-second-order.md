> **Esito aggiornato 24/10/2026:** test .NET 8 Windows M82 superati, confermati dall’utente dopo riparazione CS0246. Le diciture «pendenti» sotto sono il checkpoint storico precedente. Prossimo gate: M83-A, benchmark prima di un eventuale solver sparso.

# M82 — Secondo ordine globale LINEARIZZATO Frame2D (gate Windows aperto)

**Data:** 24 ottobre 2026. **Base:** M81, suite completa Windows confermata dall'utente. **M82:** implementato, ma non compilato/verificato nell'ambiente di confezionamento. Per accettarlo eseguire `scripts/run-m82-tests.ps1` su Windows .NET 8.

## Cosa aggiunge

M82 introduce un primo calcolo **globale, opt-in** della deformabilità di un telaio precompresso. Riutilizza il risultato statico del primo ordine per ottenere la forza assiale di ciascuna asta e costruisce `Ke` e `Kg` globali. Risolve poi `Kt u = F` con `Kt = Ke + Kg(N¹)`, mantenendo **costanti** le forze assiali ricavate dal primo ordine `N¹`: questa è una *linearized initial-stress analysis*; non un equilibrio geometrico non lineare convergente. L'analisi lineare precedente e tutte le sue API restano intatte.

Segni: sforzo normale **N > 0 trazione**, **N < 0 compressione**. Per ogni asta, senza carichi assiali distribuiti, il riferimento è `N¹ = (EndAxial - StartAxial)/2` [kN] ricavato dalle azioni locali elastiche del primo ordine. La deformabilità materiale conserva E [kN/m²], A [m²], I [m⁴], geometria in metri. Le equazioni di vincolo sono quelle del solver statico, ma l'API M82 ammette solo vincoli rigidi e non inclinati.

### Perimetro ammesso al gate M82

- Telai con **sole aste Frame2D prismatiche**, nodi rigidi; nessun release alle estremità.
- **Carichi nodali** globali X/Y e momenti nodali Z di un **singolo caso di carico**, esplicitamente selezionato.
- **Appoggi rigidi paralleli agli assi globali**; no molle, orientazioni degli appoggi, cedimenti imposti o appoggi elastici.
- Primo ordine calcolabile e stabile. Analisi su un singolo caso, non su combinazioni e non su modelli misti Frame2D/Truss2D.
- Carichi distribuiti, concentrati sull'asta, peso proprio e predeformazioni NON gestiti da questa nuova API. Il normale solver elastico conserva il supporto di queste azioni.

I casi fuori ambito generano errori espliciti `M82_UNSUPPORTED_MEMBER`, `M82_UNSUPPORTED_SUPPORT`, `M82_UNSUPPORTED_LOAD` invece di risultati incompleti.

## Rilevamento instabilità globale

Viene testata la **definitezza positiva** del blocco libero della matrice globale `Ke + λ Kg`. Il primo `λ` in cui viene perduta è stimato tramite raddoppio dell'intervallo e bisezione (64 passi, intervallo scandito finito). L'esito `CriticalLoadMultiplier` è il moltiplicatore del **campo di sforzo normale congelato**, non un fattore di sicurezza; `null` significa soltanto *nessuna perdita rilevata nell'intervallo esplorato*.

Se la matrice a `λ=1` non è positiva definita, M82 arresta il calcolo con `M82_LINEARIZED_INSTABILITY` **prima** di produrre spostamenti supercritici ingannevoli. La prova di Cholesky è relativa alle diagonali dei singoli DOF; non fornisce modi propri e non sostituisce una verifica completa del condizionamento.

## API e risultati

```csharp
var result = new StructuralSolver2DService()
    .AnalyzeFrameSecondOrder(model, "LC");

var tip = result.GetDisplacement("B");
var baseline = result.FirstOrder.GetDisplacement("B");
var axial = result.MemberAxialReferences;
var multiplier = result.CriticalLoadMultiplier;
```

Il risultato contiene spostamenti e reazioni di equilibrio della **tangente linearizzata**, il risultato completo del primo ordine per confronto e la forza assiale di riferimento di ciascuna asta. **Non** produce diagrammi N/V/M aggiornati del secondo ordine: riutilizzare quelli del primo ordine come sollecitazioni concomitanti del secondo ordine sarebbe fisicamente scorretto.

## Validazione predisposta in C#

Test di `Frame2DGlobalSecondOrderM82Tests`:

1. Mensola verticale a una asta: `Kglobal` e spostamento coincidenti con il benchmark indipendente 2×2 M81 (con rotazione locale/globale corretta).
2. Forza assiale nulla: recupero della risposta elastica al primo ordine su due aste.
3. Colonna a una/due aste: moltiplicatore critico, convergenza verso Euler continuo `π²EI/(4L²)`, sforzi assiali e reazioni.
4. Carico oltre la biforcazione: rifiuto esplicito prima di restituire un risultato normale.
5. Trazione: irrigidimento, assenza di moltiplicatore critico compressivo nell'intervallo previsto.
6. Esecuzione ripetuta: stessa risposta, nessun caricamento duplicato o modifica ai dati del modello.
7. Test negativi per carichi distribuiti, release e molle.
8. Telaio a portale di tre aste tramite la nuova API pubblica, equilibrio globale dei carichi nodali e amplificazione da compressione.

Tutti i benchmark sono C#/.NET/xUnit, senza test numerici Python. `scripts/run-m82-tests.ps1` pulisce immediatamente `bin` e `obj`, compila Release, esegue il gate M81+M82 e poi la regressione Core/Analysis/Reporting/CLI con esito separato.

## Limiti e passi successivi

Questa prima implementazione **non** calcola un equilibrio corotazionale o a grandi spostamenti e **non** aggiorna iterativamente gli assiali, i vettori di carico o la geometria; non considera imperfezioni geometriche né effetti P-δ distribuiti con elemento esatto. La perdita di definitezza non fornisce il **modo critico**; una soluzione agli autovalori generalizzata e il controllo dei modi restano un'estensione dedicata.

Le verifiche normative di stabilità (EN 1993/EN 1995/NTC), compresa l'instabilità fuori piano, non esistono: **nessun `CriticalLoadMultiplier` è un'approvazione strutturale**. Occorre estendere test e solutore a carichi sull'asta, appoggi inclinati/molle, svincoli e recupero coerente delle sollecitazioni prima di dichiarare M82 completo per le strutture generiche.

**Gate:** M81 validato dall'utente; M82 implementato e in attesa del test Windows. Prossimo: revisione estensione secondo ordine o M83 (benchmark prestazioni, con solver sparso soltanto se giustificato).

## Nota per aggiornamenti estratti sopra una vecchia versione (CS0246)

L'archivio M82 completo contiene `Frame2DGlobalSecondOrderM82.cs` e **non**
`Frame2DGlobalLinearizedM82.cs`. Se si estrae lo ZIP in una cartella già
esistente, il vecchio file `.cs` può rimanere sul disco, causando errori
`CS0246` per `PreparedStaticSystemM82` (righe 99, 128, 148 e 182).
L'inclusione automatica dei sorgenti del progetto SDK-style compila anche
i file residui: cancellare solo `bin` e `obj` non basta.

La correzione è eseguire dalla radice del progetto:

```powershell
.\scripts\repair-m82-legacy-source.ps1
dotnet build StructuralSolver2D.sln -c Release --tl:off
```

Lo script **non cancella** il sorgente: lo sposta in `.m82-legacy-backup`
con nome univoco; non interviene su altri sorgenti. È anche richiamato
automaticamente da `scripts/run-m82-tests.ps1`. Se la copia proviene da una
cartella pulita, la procedura non modifica nulla.
