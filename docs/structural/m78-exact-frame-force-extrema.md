# M78 — Estremi analitici dei diagrammi N/V/M, statica elastica Frame2D

**Stato al 23/10/2026: implementato; compilazione e test Windows ancora non eseguiti.**
**Ultimo gate confermato dall'utente: M73. M75–M77 restano pendenti.**

## Perché

Il post-processore già campiona N, V, M e propone punti caratteristici per interpolazione. Un numero fisso di campioni può non individuare un estremo tra i campioni, e una forza concentrata crea un salto di N e/o V. Non basta aumentare la densità della discretizzazione grafica per avere estremi esatti.

M78 aggiunge `Frame2DExactExtremaFinderM78` e `StructuralSolver2DService.FindExactStaticMemberForceExtrema(model, result)`: su ciascuna asta, per il **singolo risultato statico** (caso o combinazione manuale) si trovano minimo, massimo e massimo assoluto di N [kN], V [kN] e M [kNm], con ascissa [m], posizione normalizzata e lato del salto.

## Metodo

Il nuovo finder usa lo *stesso valutatore analitico* del diagramma campionato: `Frame2DInternalForceSampler.PrepareMember` costruisce il profilo, senza ricalcolare l'analisi FEM.

- Carichi uniformi e linearmente variabili producono funzioni N e V quadratiche (al massimo), M cubica (al massimo).
- I carichi concentrati interni suddividono l'asta in intervalli lisci. Per il valore al carico sono considerate separatamente la sezione **prima** e **dopo** la forza puntuale. I momenti restano continui nel modello di carico concentrato di forza.
- Per N e V si valutano gli estremi agli estremi dell'intervallo e le eventuali ascisse in cui si annulla la derivata; per M si risolve `V(x)=0` nel tratto, compresa la radice di secondo grado nel caso di carico lineare.
- Si confrontano tutti i candidati col segno, anche quando una combinazione ha coefficienti negativi. Non si sostituisce il risultato esatto con il massimo dei campioni grafici.
- In corrispondenza di un carico esattamente al nodo di estremità la convenzione restituisce il limite fisico interno all'asta; le due facce di un carico puntuale **interno** sono etichettate `BeforePointLoad` / `AfterPointLoad`.

Le ascisse sono espresse dall'inizio dell'asta secondo la sua direzione locale. Tutti i valori rimangono nelle unità standard e nelle convenzioni di segno già usate dal Frame2D sampler.

## API (C#)

```csharp
var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "G");
var exact = new StructuralSolver2DService()
    .FindExactStaticMemberForceExtrema(model, output.Result);

var member = exact.Single(x => x.MemberId == "M1");
var bending = member.MaximumAbsolute(InternalForceQuantity.BendingMoment);
Console.WriteLine($"|M|max = {Math.Abs(bending.Value)} kNm a x={bending.Distance} m");
```

La vecchia proprietà `Summary.MemberInternalForceExtrema` **rimane campionata** per retrocompatibilità. Utilizzare il nuovo servizio M78 per gli estremi analitici e non confondere i due insiemi di dati.

## Benchmark C# (xUnit)

1. Trave isostatica, carico uniforme `q=10 kN/m`, `L=5 m`: massimo `M=31,25 kNm` esattamente a `L/2`, anche con due soli campioni del diagramma grafico.
2. Trave isostatica con carico triangolare da zero a `w0=12 kN/m`, `L=6 m`: `x=L/√3`, `Mmax=w0 L²/(9√3)`; verifica della radice quadratica del taglio.
3. Carico puntuale `P=12 kN` a `x=2 m` in una luce di `8 m`: taglio prima `+9 kN`, dopo `−3 kN`, massimo `M=18 kNm` all'ascissa del carico. **A parità di taglio lungo tutto il primo tratto, il massimo restituito è quello di inizio asta (`Continuous`)**: il criterio deterministico M78 esamina gli estremi di asta prima dei limiti al salto. Il minimo `−3 kN` è costante fino a `x=8 m` e, per lo stesso criterio, viene rappresentato dalla **fine asta** (`x=8 m`, `Continuous`). I due limiti effettivi del salto in `x=2 m` rimangono `+9/−3 kN`, con momento continuo `18 kNm`; l’esempio con estremi univoci del punto seguente verifica invece entrambe le etichette `BeforePointLoad` e `AfterPointLoad`.
4. Carico concentrato `−12 kN` in `x=2 m` più carico distribuito verticale **verso l’alto** `+1 kN/m` sulla luce `8 m`: `Vmax=+7 kN` **BeforePointLoad** e `Vmin=−5 kN` **AfterPointLoad**, entrambi in `x=2 m` e univoci.
5. Combinazione manuale con fattore negativo `−2`: segno di M, valore governante e ID combinazione conservati.
6. Due aste continue su mensola: massimo al vincolo di incastro, restituzione ordinata per asta.

I test includono anche la regressione dei precedenti M75/M76/M77 e l'intera soluzione in `scripts/run-m78-tests.ps1`. Lo script inizia con la pulizia `bin/obj` e usa la console testuale senza progress bar. **Solo C#/.NET**, non Python.

## Limiti da non nascondere

- Valido **soltanto** per le sollecitazioni del Frame2D lineare statico di primo ordine, sotto le tipologie di carico membri già supportate dal sampler: uniforme, lineare, puntuale e peso proprio espanso. Non è un risultato sismico, dinamico o plastico.
- Non sostituisce lo stato tensionale lungo la sezione, una verifica normativa o un calcolo fuori piano.
- M78 trova gli estremi all'interno di **un risultato**. L'inviluppo governante fra più combinazioni e la corrispondente tracciabilità sono previsti in **M79**.
- L'algoritmo assume carichi concentrati di forza, non coppie interne applicate lungo l'asta. Il momento di estremità nodale è già incluso nella soluzione agli estremi.
- Il solver FEM resta denso e non è stato ottimizzato o modificato nella formulazione.
- Gate Windows non eseguito qui: qualsiasi avviso o fallimento di M75–M78 va corretto prima di promuovere i milestone a validati.
