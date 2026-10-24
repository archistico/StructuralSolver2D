# M73 — Prestazioni, scalabilità e diagnostica numerica

**Data:** 23 ottobre 2026  
**Ambito:** benchmark riproducibili C#/.NET 8, senza cambiare gli algoritmi di produzione.
**Stato:** implementato, **in attesa di compilazione e prova su Windows**. M51–M72 hanno superato i test secondo conferma dell'utente (M72-FIX2).

## Motivazione e decisione

Il risolutore lineare `DenseLinearSystemSolver` memorizza una matrice quadrata piena (`O(n²)` elementi `double`) e usa eliminazione gaussiana con pivot parziale (`O(n³)` operazioni). Il sistema vincolato e le matrici di Newton per M64/M65 sono anch'essi densi. Aumentare il numero di aste comporta quindi costi sostanziali anche per strutture topologicamente sparse. **Non è ancora stato dimostrato con misure che una riscrittura con matrici sparse renda più veloce questo codice per le dimensioni effettivamente usate.**

M73 introduce una baseline riproducibile, senza promesse di prestazioni e senza modificare il solver. Qualunque futura implementazione sparsa o fattorizzazione ottimizzata dovrà essere confrontata con questa baseline **sulla stessa macchina, versione .NET, input e modalità Release**, mantenendo i gate di accuratezza e rollback.

## I quattro scenari

| Scenario | Dimensioni (modalità normale) | Controllo numerico indipendente | Dati raccolti |
|---|---|---|---|
| Dense-SPD | 16, 32, 64, 96 incognite | Soluzione nota `x[i]=1`; matrice definita positiva | DOF, tempo, allocazioni, errore |
| Truss-static | 4, 8, 12, 20 aste in serie | `u_terminal = F·L/(EA)` e forze interne da equilibrio | DOF, Newton, tempo, allocazioni, errore |
| Truss-Newmark | 3, 6, 10, 16 aste con massa coerente | Residuo dinamico ridotto e plasticità nulla nel riferimento elastico | DOF, Newton, tempo, allocazioni, residuo |
| Frame-hinge-trials | 25, 100, 250 valutazioni di elemento | Simmetria della tangente e assenza di commit durante il trial | DOF, iterazioni locali, tempo, allocazioni, errore |

**Nota sui DOF:** si riportano sia i gradi di libertà fisici sia quelli attivi per le reticolari. Il caso Frame2D misura la *valutazione locale isolata* M72, non un assemblaggio globale di telai. Per il caso Dense-SPD viene usata una matrice tridiagonale **memorizzata come densa** per caratterizzare deliberatamente il backend attuale; non è una misura di un backend sparso.

I tempi comprendono l'allocazione e la preparazione del caso in ciascuna esecuzione; **non sono tempi della sola fattorizzazione o del solo assemblaggio**. M73 non riporta ancora quelle due fasi separatamente. Una esecuzione di riscaldamento precede ogni scenario; per default si registrano tre ripetizioni e si riporta la mediana (con tre prove coincide con il valore centrale). Le allocazioni sono stimate tramite `GC.GetAllocatedBytesForCurrentThread` e non rappresentano la memoria di picco. I tempi sono sensibili a macchina, JIT, antivirus, frequenza CPU e carico di sistema. I benchmark sono sequenziali, non concorrenti.

## Esecuzione

```powershell
.\scripts\run-m73-tests.ps1
```

Il runner pulisce `bin`/`obj` all'inizio, compila in Release, esegue prima i test M73, poi **l'intera regressione**, infine la misurazione. Tutto il calcolo è in C#; PowerShell gestisce soltanto l'orchestrazione. Nessuno script Python.

Per misurazioni autonome senza pulire e senza rifare i test:

```powershell
dotnet run --project tools/StructuralSolver2D.PerformanceM73 -c Release -- --repetitions 5 --output artifacts/benchmarks/m73-manuale
```

Per una verifica veloce:

```powershell
dotnet run --project tools/StructuralSolver2D.PerformanceM73 -c Release -- --quick --repetitions 1
```

Due output nel percorso scelto: `m73-baseline.csv` (numeri in cultura invariante) e `m73-baseline.json` (runtime, sistema operativo, processori, data UTC e risultati). La directory `artifacts/` è esclusa da Git; **conservare i file tra una release e la successiva** se si desidera confrontare due implementazioni.

## Criteri di accettazione

1. Ciascun caso deve essere risolto correttamente e soddisfare i controlli di accuratezza specifici **senza basarsi sul tempo trascorso**.
2. I risultati devono essere finiti, i passi Newmark conclusi, la risposta di riferimento elastica non deve plasticizzare, i trial Frame2D non devono modificare lo stato committato.
3. La compilazione della soluzione e **tutti i test precedenti** devono rimanere verdi.
4. I CSV/JSON devono essere generati e validi, con dimensioni e unità chiare.
5. **Nessuna soglia millisecondi fissa**: si possono introdurre regressioni prestazionali solo dopo più misure stabili sulla stessa infrastruttura, con una tolleranza motivata.

## Limiti / lavoro futuro

- Nessun backend sparso è stato introdotto. La misura non equivale alla certificazione di scalabilità su grandi strutture.
- Profilazione distinta di assemblaggio, proiezione dei vincoli, fattorizzazione e solve non è ancora disponibile.
- Le tangenti plastiche dei telai possono diventare indefinite o singolari; un futuro solver deve gestirle esplicitamente, senza presumere SPD.
- Il prototipo M72 **non** risolve telai non lineari multi-elemento; la sua misurazione copre soltanto la parte locale.
- Prima dell'integrazione OpenCad2D (M74) è necessario verificare contratto JSON, viewer e unità senza dipendenze CAD nel core.
