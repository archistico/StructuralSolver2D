# M83-A — Misure di scalabilità Frame2D e decisione solver sparso

**Data:** 24 ottobre 2026. **Base:** M82 con test Windows confermati dall'utente dopo correzione del sorgente obsoleto CS0246.

## Obiettivo

Non introdurre un solver sparso senza misure. Il baseline M73 misurava una matrice SPD sintetica, una catena Truss2D non lineare e trial di cerniere plastiche. M83-A aggiunge misure su **modelli FEM Frame2D reali**, con crescita della dimensione e due percorsi distinti: calcolo statico elastico `Frame2DAnalyzer` e calcolo M82 a rigidezza geometrica linearizzata `Frame2DGlobalSecondOrderM82`.

M83-A **non cambia** il motore numerico, l'assemblaggio, il contratto JSON o l'API. È un gate strumentale, non la conclusione di M83 né una prova di fattibilità del solver sparso.

## Casi riproducibili

Mensola verticale `L=4 m`, acciaio ideale elastico `E=210 000 000 kN/m²`, `A=0,006 m²`, `I=0,00002 m⁴`, base incastrata, carico nodale orizzontale `H=+2 kN` in sommità; `n` elementi di lunghezza uguale. Il benchmark di primo ordine confronta lo spostamento di sommità con la soluzione esatta di Euler–Bernoulli **u = H L³ / (3 E I)** e verifica le reazioni. Ogni caso misura la chiamata effettiva al solver, non la sola fattorizzazione.

Per M82-A si aggiunge `P=100 kN` verticale verso il basso allo stesso nodo. Si controllano: spostamento del primo ordine rispetto alla soluzione esatta; equilibrio delle reazioni; sforzi di riferimento `N=-P`; amplificazione laterale positiva; moltiplicatore critico maggiore di uno. **Non** si dichiara che lo spostamento M82 sia la soluzione geometrica non lineare esatta né che il moltiplicatore rappresenti una verifica di sicurezza.

| Serie | Modalità `--quick` | Modalità completa | DOF liberi |
|---|---|---|---|
| Frame2D-first-order | 2, 4 aste | 8, 16, 32, 64 aste | 3n |
| Frame2D-M82-linearized | 2, 4 aste | 2, 4, 8, 16 aste | 3n |

Si eseguono un riscaldamento e tre ripetizioni (configurabili 1–20). Si riportano mediana del tempo e dei byte allocati sul thread di chiamata; l'input è creato **prima** della misura; le verifiche numeriche sono incluse in entrambi i campionamenti, per garantire l'assenza di campioni numericamente errati. Gli overhead di controllo sono contenuti ma non nulli; il timing non rappresenta solo le operazioni floating point. Il conteggio delle allocazioni non equivale al picco di memoria del processo. Misure brevi e sistemi occupati richiedono più run.

## Esecuzione

Dalla radice del progetto, su Windows con SDK .NET 8:

```powershell
.\scripts\run-m83-tests.ps1
```

Lo script gestisce l'eventuale sorgente M82 obsoleto rimasto nella cartella, pulisce `bin/obj`, compila Release, esegue il gate xUnit M73/M82/M83, la regressione completa dei quattro progetti test e **solo dopo** produce:

- `artifacts/benchmarks/m83/m83-scalability.csv`
- `artifacts/benchmarks/m83/m83-scalability.json`

La compatibilità storica è preservata: il tool M73 continua a partire **senza** `--m83` e salva `m73-baseline.csv/json`. Per misure rapide separate:

```powershell
dotnet run -c Release --project tools/StructuralSolver2D.PerformanceM73 -- --m83 --quick --repetitions 3
```

## Decisione tecnica condizionata

1. Confrontare M73 e M83-A sullo **stesso PC**, stesso SDK .NET e build Release; evitare conclusioni da una sola esecuzione.
2. Cercare trend di crescita di **tempo e allocazione** al crescere dei DOF, confrontando ogni scenario **con sé stesso** (non equiparare prestazioni M82 e primo ordine: risolvono problemi diversi).
3. Se piccoli telai/reticolari restano adeguati, **non** introdurre ora un nuovo sistema di matrici. Il percorso utile per l'utente rimane M85/M86.
4. Se emerge un collo di bottiglia rilevante, M83-B deve essere una prova isolata con matrici sparse/banded: stessa soluzione entro tolleranze, controlli di instabilità affidabili, fallback denso, benchmark prima/dopo su casi identici. Niente sostituzione generalizzata prima di questo gate.

Il calcolo non produce verifiche SLU/SLE e non valida per uso professionale modelli fuori dal perimetro dichiarato. In particolare, M82 non produce N/V/M corretti al secondo ordine, modo critico e ricerca di equilibrio iterativo.

**Gate M83-A:** tutte le regressioni, tutti i controlli numerici e gli output CSV/JSON devono passare sul Windows dell'utente. I tempi misurati sono *dati per decidere*, non soglie automatiche.
