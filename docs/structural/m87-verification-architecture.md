# M87-A — Architettura verifiche normative 2D (contratti, esiti, fail-closed)

**Checkpoint:** 24 ottobre 2026. **Prerequisito confermato su Windows:** M86 RC1 ha superato compilazione Release (`0` errori/avvisi), **1.334/1.334 test**, prove CLI per trave, reticolare e combinazione e generazione Markdown/CSV/XLSX/PDF. Il nuovo gate M87-A non è stato ancora eseguito sul PC dell'utente.

## Decisione architetturale

Il nuovo assembly **`StructuralSolver2D.Verification`** non è citato da `Core` né da `Analysis`; contiene contratti tipizzati e un motore di dispatch delle regole. Nella fase M87-A non è presente **nessuna regola normativa reale**. Nessun certificato, nessuna dichiarazione di conformità, nessuna formula EN 1993 / EN 1995 / NTC. Le prove di calcolo del progetto test sono chiaramente sintetiche (`TEST-CODE`).

- `DesignCodeEdition` obbliga a dichiarare **documento, edizione, annesso nazionale**: nessuna selezione implicita della norma più recente.
- `NormativeProfile` richiede identificatore e lista esplicita di `NormativeCoefficient` (`Id`, valore, clausola/fonte, descrizione); la lista vuota è ammessa in M87-A, non implica valori di default.
- `DesignMemberContext` identifica asta, materiale (Steel/Timber), sezione e lunghezza; **lunghezze libere in piano/fuori piano e ritegni laterali sono nullable**. Mancanza non significa verifica superata.
- `DesignEffectSnapshot` è un **singolo punto dell'asta** con identificatori di analisi, combinazione, origine, ascissa ed effetti con segno in assi locali. Non si combinano automaticamente inviluppi di N/V/M non concomitanti; questa API non esegue estrazione FEM.
- `IDesignCheckRule` limita le regole a un materiale, uno stato limite (Ultimate/Serviceability), una precisa edizione/annesso e clausola. La sua implementazione normativa verrà solo dopo cataloghi, benchmark e revisione di ciascuna formula.
- `DesignCheckEngine`: nessuna regola registrata significa **Unsupported**. Edizione errata => **Unsupported**; materiale/stato limite non pertinente => **NotApplicable**; input invalidi o non finiti => **InvalidInput**; parametri mancanti dichiarati dalla regola => **MissingData**; calcolo invalido/eccezione => **CalculationError**. Mai convertire tali stati in Passed.
- Un calcolo `Calculated(demand, resistance, unit, trace)` produce `Passed` se `demand / resistance <= 1`, `Failed` se `>1`, solo quando i dati sono finiti, la domanda è non negativa, la capacità è positiva e ci sono unità e traccia. Questa è **solo la semantica matematica** di un rapporto: NON stabilisce come determinare le resistenze.
- `DesignCheckReport.OverallStatus` vale **Passed solo se sono presenti controlli e TUTTI sono Passed**; **Failed** se almeno uno fallisce; **Incomplete** altrimenti. `IsComplete` è indipendente: se coesistono Failed e MissingData, lo stato è Failed ma il report resta incompleto.

## Unità, limiti e ambiti non automatizzati

Unità: kN, m, kNm, rotazioni rad. Effetti trasmessi con segno come nel modello locale FEM; una futura regola dovrà esplicitare le trasformazioni (es. valore assoluto o tensione di fibra). Non sono ammesse conversioni tacite da MPa/N/mm o condizioni fuori piano inventate. La verifica viene identificata sempre per **asta, sezione, ascissa, combinazione, origine e versione normativa**. Non esiste ancora un adapter che estragga gli effetti dai diagrammi del FEM, né un repository normativo versionato delle proprietà sezionali o delle classi acciaio/legno. Non vengono dedotte correlazioni tra diversi picchi di inviluppo.

**Scopo del prossimo incremento M87-B:** adapter FEM→snapshot con controlli sulle convenzioni locali e correlazione N/V/M allo stesso punto e alla stessa combinazione, catalogo di input obbligatori per tipologia di verifica, esportazione JSON degli esiti; nessuna verifica può essere dichiarata normativa senza formule e riferimenti indipendenti. **M88–M95:** cataloghi e regole EC3/EC5/NTC per le sole casistiche esplicite, con edizione e annessi effettivi e benchmark manuali. **M97–M99:** rapporto verifiche, audit trail, revisione e release del modulo normativo; GUI Avalonia per ultima.

## Test e gate

Da una copia estratta in una cartella vuota, nella radice `StructuralSolver2D`:

```powershell
.\scripts\verify-m87-manifest.ps1
.\scripts\run-m87-tests.ps1
# Opzionale: include anche smoke end-to-end M86
.\scripts\run-m87-tests.ps1 -WithM86Smoke
```

La nuova suite di `StructuralSolver2D.Verification.Tests` copre: richiesta senza regole, regola non registrata, edition/annex mismatch, materiali/stati limite, dati mancanti fuori piano, NaN e input errati, tracciabilità coefficienti, duplicati, eccezioni, rapporto sintetico e aggregazione corretta. La regressione precedente rimane parte del gate. **La compilazione M87-A deve ancora essere verificata in .NET 8 sul PC Windows.**
