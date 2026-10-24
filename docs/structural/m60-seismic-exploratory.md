# M60 — Risposta sismica esplorativa (NON NORMATIVA)

**Stato:** implementata, in attesa dei test .NET 8 Windows. Non fornisce verifiche NTC/Eurocodici, spettro normativo, resistenze, duttilità, danno, drift di verifica, combinazioni di azioni o giudizi di sicurezza.

## Fondamento matematico

Gli spostamenti **relativi al suolo** sono `u`, con massa FEM consistente `M`, rigidezza `K` e smorzamento `C`:

`M u¨ + C u˙ + K u = -M r a_g(t)`.

`r` è unitario per le traslazioni **GlobalX** oppure **GlobalY** di tutti i nodi, nullo per le rotazioni. La matrice globale `M`, inclusi i suoi termini misti traslazione/rotazione, agisce su `r` prima dell'applicazione delle condizioni di vincolo (base `Z`). Questo evita il modello scorretto con una stessa forza nodale uguale per tutti i nodi. Si suppone uno scuotimento uniforme degli appoggi, non cedimenti differenziali o SSI. La massa rotazionale aggiuntiva non implica un'accelerazione rotazionale del terreno.

Unità: `a_g` in m/s², `M` in kN s²/m, `-M*r*a_g` in kN o kN·m per componenti rotazionali. La risposta di M59 restituisce `u` **relativo**, con accelerazione relativa `u¨`. L'accelerazione traslazionale assoluta è `u¨ + r*a_g`: è calcolata e distinta nel report e CSV M60. Il terreno non presenta una rotazione imposta.

### Partecipazione modale

Per `phi_i^T M phi_i = 1`, `Gamma_i = phi_i^T M r`, `M_eff,i = Gamma_i²`. Il denominatore percentuale riportato è `M_ground = r^T M r` **prima dei vincoli**. La somma delle masse efficaci dei modi ammessi può essere inferiore a questa massa anche usando **tutti** i modi, perché i vincoli impediscono parte del moto uniforme. La percentuale modale cumulativa non è una verifica di adeguatezza antisismica: esaminare anche massa non partecipante, troncamento e direzione.

### Storia temporale M60

Gli accelerogrammi sono campioni `(timeSeconds, accelerationMetersPerSecondSquared)` con tempi crescenti che partono da 0 e arrivano almeno al tempo finale. L'accelerazione è interpolata linearmente ai passi temporali. M60 crea il pattern nodale equivalente usando `-M*r` e utilizza il Newmark a accelerazione media M59 con **tutti** i modi ammessi e smorzamento esplicito (uniforme oppure Rayleigh). Il bilancio energetico M59 è riferito alle **coordinate relative** e al lavoro equivalente dell'inerzia; non si deve interpretare come bilancio completo di energia assoluta durante un sisma. È necessario ripetere lo studio della sensibilità a `dt` e all'interpolazione dell'accelerogramma; il rispetto del bilancio discreto non basta a garantire precisione.

### Spettro elastico pseudodinamico

Input: coppie `(periodSeconds, pseudoAccelerationMetersPerSecondSquared)` con periodi crescenti e ordinate non negative. Interpolazione **lineare rispetto al periodo**, senza estrapolazione oltre l'intervallo fornito. L'utente deve fornire esplicitamente uno spettro già appropriato allo smorzamento che intende rappresentare: il codice non modifica le ordinate per smorzamento, suolo o norma.

Per ogni modo: `Sd_i = Sa(T_i) / omega_i²`; il massimo modale relativo nodale è `abs(phi_ij * Gamma_i * Sd_i)`. SRSS: `sqrt(sum_i (phi_ij Gamma_i Sd_i)^2)` separatamente per Ux, Uy e Rz. Si ottengono solo **inviluppi positivi non simultanei**: non sono spostamenti firmati di un'unica configurazione, né forze o tensioni interne. Non è implementata CQC; se modi con partecipazione significativa distano meno del 10% in frequenza, compare un avviso perché SRSS può essere inadeguato.

## API e CLI

API: `StructuralSolver2DService.AnalyzeSeismicTime(StructuralSeismicTimeRequest)` e `AnalyzeSeismicSpectrum(StructuralSeismicSpectrumRequest)`.

JSON in `examples/dynamics/m60-seismic-cantilever.json`, sezione separata `seismic.direction`, `seismic.timeHistory` e `seismic.spectrum`. La sezione `dynamics` resta obbligatoria. Nessuna modifica al contratto del solver statico o ai vecchi blocchi `timeHistory`/`harmonic`.

- `seismic-time <file.json>`
- `seismic-time-report <file.json> <output.md>`
- `seismic-time-csv <file.json> <output.csv>`
- `seismic-spectrum <file.json>`
- `seismic-spectrum-report <file.json> <output.md>`
- `seismic-spectrum-csv <file.json> <output-directory>` (due CSV)

## Validazione richiesta

`powershell -ExecutionPolicy Bypass -File scripts/run-m60-tests.ps1` in una cartella **pulita** su Windows con .NET 8. Copertura: asta a un grado di libertà con massa FEM consistente (`M_eff / M_ground = 75%`), confronto esatto frequenza/spostamento pseudo-spettrale, segno dell'accelerazione equivalente e accelerazione assoluta iniziale, input invalidi e CLI, oltre a regressione completa M1–M59.1.

La revisione numerica indipendente in Python e i controlli ZIP/JSON non sostituiscono il build o i test .NET. La prima verifica visiva dei viewer precedenti resta un controllo separato.

## Possibili lavori futuri

- Validare CQC indipendentemente prima di renderla disponibile.
- Trattare più componenti sismiche/direzioni e relative combinazioni.
- Confrontare spettro numerico derivato da accelerogramma con spettro fornito dall'utente, con smorzamento definito.
- Controllare i modi rigidi, masse senza rigidezza, supporti mobili e svincoli con formulazioni dedicate.
- Valutare normative **vigenti** soltanto con un milestone separato, specifiche nazionali e verifiche di sicurezza indipendenti.
