# M70 — CQC: combinazione modale nella risposta sismica esplorativa

**Stato:** implementato il 23/10/2026, **test .NET 8 su Windows non ancora eseguiti**. **Non normativo**: non produce verifiche NTC, Eurocodice, forze di progetto o criteri di sicurezza.

## Perché aggiungiamo CQC

L'analisi spettrale M60 usa SRSS, cioè la radice della somma dei quadrati dei contributi modali, ragionevole come approssimazione quando i modi sono sufficientemente separati. Modi con frequenze vicine possono essere correlati. M70 rende selezionabile CQC (*Complete Quadratic Combination*) senza modificare il comportamento predefinito SRSS.

È disponibile **solo** per spettri di **pseudo-accelerazione elastica** Sa(T) **forniti dall'utente**, già coerenti con lo smorzamento fisico ipotizzato. Il coefficiente di correlazione CQC **non** trasforma uno spettro definito per un certo smorzamento in uno spettro valido per uno smorzamento diverso.

## Convenzioni, equazioni e unità

Per il modo `i`, con forma `phi_i` normalizzata in massa, pulsazione `omega_i` [rad/s], partecipazione `Gamma_i` e spostamento spettrale `Sd_i = Sa(T_i)/omega_i²`, il contributo **con segno** al singolo grado di libertà `k` è:

`d_{ik} = phi_{ik} * Gamma_i * Sd_i`.

Per smorzamento modale relativo **identico** `zeta` in tutti i modi e `r = min(omega_i,omega_j)/max(omega_i,omega_j)`, la correlazione CQC adottata è:

`rho_ij = 8*zeta²*(1+r)*r^(3/2) / ((1-r²)² + 4*zeta²*r*(1+r)²)`.

`rho_ii=1`. La combinazione di ogni grado di libertà `k` è:

`D_k = sqrt(sum_i sum_j rho_ij * d_ik * d_jk)`.

È essenziale mantenere il segno dei singoli `d_ik` prima di eseguire il prodotto doppio; l'inviluppo finale `D_k` è non negativo. Frequenze e risultati restano nelle unità delle API M60: Hz, m, rad. I valori ai diversi nodi e gradi di libertà rappresentano **massimi non simultanei** e non una deformata firmata unica.

La formula riguarda smorzamento modale classico **uguale** per tutti i modi. L'input CQC richiede `modalDampingRatio` adimensionale **esplicito** nell'intervallo `[1e-6, 0.30]` per evitare parametri numericamente degeneri; non è una validazione fisica del valore scelto.

## Contratti pubblici

Nel blocco `seismic.spectrum` del JSON, `combination` vale `Srss` (predefinito) oppure `Cqc`. Solo per CQC è obbligatorio `modalDampingRatio`. In modalità SRSS la presenza di `modalDampingRatio` è rifiutata per evitare un parametro ignorato silenziosamente.

```json
"spectrum": {
  "modeCount": 6,
  "combination": "Cqc",
  "modalDampingRatio": 0.05,
  "points": [
    { "periodSeconds": 0, "pseudoAccelerationMetersPerSecondSquared": 2 },
    { "periodSeconds": 10, "pseudoAccelerationMetersPerSecondSquared": 2 }
  ]
}
```

API: `StructuralSeismicSpectrumRequest(..., Combination: SeismicModalCombination.Cqc, ModalDampingRatio: 0.05)`; risultato: `StructuralSeismicSpectrumOutput.Combination` e `.ModalDampingRatio`. È stato mantenuto il costruttore compatibile per chiamate M60 precedenti (parametri opzionali aggiunti in coda).

CLI **invariata**: `seismic-spectrum`, `seismic-spectrum-report`, `seismic-spectrum-csv`. Il CSV dei nodi presenta intestazioni `CQCAbsUx_m` ecc. per CQC, `SRSSAbsUx_m` ecc. per SRSS, così nessun lettore può confondere i metodi. Il report dichiara sempre **NON-NORMATIVE** e il metodo impiegato. La risposta temporale `seismic-time` non viene modificata.

Esempio: `examples/dynamics/m70-seismic-cqc-cantilever.json`.

## Validazione C# richiesta

- SDOF: CQC identica a SRSS.
- Due modi coincidenti: coefficiente di correlazione unitario, con combinazione **firmata** dei contributi.
- Modi vicini: confronto con espressione algebrica CQC valutata indipendentemente all'interno dei test xUnit.
- Modi lontani: correlazione trascurabile, risultato prossimo al limite SRSS.
- Per due modi con contributi di segno opposto, CQC può essere minore di SRSS: un uso erroneo dei valori assoluti sarebbe individuato.
- Modello strutturale a due modi, invarianti di massa e conservazione dei risultati M60 SRSS; errori espliciti per input non validi.
- CLI end-to-end con nuova fixture e controllo che il vecchio esempio M60 esporti ancora SRSS; report e CSV non normativi e con intestazioni corrette.

Eseguire `scripts/run-m70-tests.ps1`: **prima pulizia bin/obj di tutti i progetti**, poi build Release, test dedicati, regressione completa e CLI reale. Tutti i benchmark richiesti sono **C#/xUnit**, nessuno script Python.

## Limitazioni ed esclusioni

Nessuno spettro NTC/EC8 generato internamente; nessuna combinazione delle direzioni sismiche, CQC per smorzamento diverso da modo a modo, masse omesse/forze equivalenti normative, dinamica non lineare, moto differenziale degli appoggi, effetti torsionali 3D o verifiche di sicurezza. La partecipazione modale M60 continua a utilizzare la massa globale consistente; valori cumulati inferiori al 100% della massa pre-vincoli non sono automaticamente un errore. La troncatura modale richiede un'analisi di sensibilità anche con CQC.

**Bibliografia metodologica:** formulazione classica CQC per modi con smorzamento modale uguale (metodo di Der Kiureghian); il confronto con il libro di Carpinteri riguarda la teoria delle forme modali e le limitazioni di SRSS nella combinazione dei modi, non una certificazione di normative correnti.

## Correzione della fixture M70-FIX1

Il test a due campate richiede un ritegno trasversale sul nodo B intermedio, perché le due aste reticolari collineari sono prive di rigidezza verticale. L'appoggio `SimpleSupport("SB", "B")` conserva i due spostamenti assiali indipendenti B.Ux e C.Ux e rimuove il solo meccanismo spurio del benchmark. La diagnostica M54 resta invariata. Il controllo dell'output test PowerShell è documentato in [`m70-fix1-cqc-benchmark-console.md`](m70-fix1-cqc-benchmark-console.md). La regressione Windows della correzione deve ancora essere eseguita.
