# P0-D0M7 — Classificazione NTC 2018 del ramo compressivo α = 1

**Data:** 25 ottobre 2026  
**Stato:** codice e xUnit predisposti; **131/131** nuovi golden aritmetici indipendenti e **473/473** totale R2–D0M7 PASS; test .NET **PENDING** del nuovo D0M7.  
**Gate precedente:** conferma dell'autrice «tutti i test passati» per il FULL **D0M6**, dopo analoga conferma per D0M5; test Windows fino a D0M6 segnati PASS su dichiarazione, senza log numerico allegato.

## In sintesi

Il nuovo `Ntc2018SteelMixedCompressionDominantClassifierD0M7` estende la **classificazione della sola sezione** per IPE/HEA/HEB laminati, simmetrici, ad ali parallele e non forati, sottoposti a **N compressivo e M forte concomitanti, V=0**, quando:

1. il geometrico **D0M6** dimostra che l'asse neutro plastico è *oltre il bordo teso* del tratto rettilineo dell'anima, dunque l'intera anima piatta è in compressione plastica nominale (**α esattamente 1**);
2. lo schermo elastico **D0M3**, che usa gli **stessi N/M della stessa stazione FEM** e proprietà gross A/I indipendenti, determina `−1<ψ<1`;
3. codice/edizione NTC, materiale, sezione, analisi, combinazione, stazione/lato, sollecitazioni, fy e snellezza web coincidono esattamente nei due risultati; geometria/fonte e grade sono attestati;
4. il confine D0M4/D0M5 è superato *strettamente*, con margine numerico a tutela di eventuali casi tangenti.

Non cambia il servizio D0M1/D0M5 che continua a richiedere `0<α<1`. Per il solo estremo fisicamente provato `α=1`, D0M7 implementa esplicitamente il caso limite della **Tabella 4.2.III NTC 2018**. In questo modo la promozione a classe normativa dipende dall'evidenza geometrica e FEM, non da un parametro `α=1` inserito arbitrariamente.

## Equazioni e fonti normative

NTC 2018, **§4.2.3.1, Tabelle 4.2.III–IV**, DM 17 gennaio 2018, testo ufficiale [Gazzetta Ufficiale](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf), p. 95–96 (numerazione del fascicolo). Nel ramo `α>0,5`, i limiti misti plastici di anima per classe 1/2 sono:

\[
\frac ct\le\frac{396\,\varepsilon}{13\alpha-1}
\qquad\text{(classe 1)}
\]

\[
\frac ct\le\frac{456\,\varepsilon}{13\alpha-1}
\qquad\text{(classe 2)}
\]

Inserendo l'estremo **dimostrato** α=1:

\[
\boxed{(c/t)_1=33\varepsilon},\qquad
\boxed{(c/t)_2=38\varepsilon},\qquad
\varepsilon=\sqrt{235/f_{yk}}.
\]

Per la classe 3 il criterio non è deducibile dal solo α plastico: richiede il rapporto delle **tensioni elastiche** che D0M3 calcola e prova:

\[
(c/t)_3=\frac{42\varepsilon}{0,67+0,33\psi},
\qquad -1<\psi<1.
\]

L'ala a sbalzo compressa resta soggetta ai limiti della Tabella 4.2.IV **9/10/14 ε**, in funzione del rapporto geometrico tra sbalzo piatto e spessore. La classe della sezione è il **massimo** tra le classi di anima e ala; se è **4**, servono proprietà efficaci della sezione **non ancora calcolate**.

Non si usa la formula speciale di classe 3 per `ψ≤−1`: nel campo FEM di **compressione assiale su I/H doppiamente simmetrica**, lo schermo D0M3 accetta il ramo `ψ>−1`; i casi `ψ≤−1` restano nel percorso di trazione D0M2/D0M5, alle condizioni già implementate.

## Esempio IPE 200 — S235, M = 10 kNm, V = 0

| N FEM [kN] | Dove è la PNA nominale | α | ψ da A/I indipendenti | Classe anima/ala | Classe sezione |
|---:|---|---:|---:|---|---:|
| −100 | anima piatta | 0,739 circa | `ψ>-1` | D0M5 | D0M5 |
| −220 | raccordo lato teso | 1 | 0,307465 | 1 / 1 | **1** (D0M7) |
| −300 | ala lato teso | 1 | 0,440476 | 1 / 1 | **1** (D0M7) |

La classificazione è una proprietà del rapporto fra larghezza e spessore in quella condizione di sollecitazione; **non** confronta `N_Ed/M_Ed` con `N_Rd/M_Rd`. Anche la classe 1 non dimostra che la domanda applicata sia resistente.

## Golden e xUnit

- **131/131** nuovi golden aritmetici Python indipendenti PASS: derivazione razionale `396/12=33`, `456/12=38`, continuità per α→1 dal basso, 30 profili OPPO con N/M e ψ indipendenti, soglie di classe 3 per ψ diverse, quattro profili sintetici con classi **1, 2, 3 e 4**, più respingimenti di rami fuori campo.
- Regressioni indipendenti precedenti R2–D0M6: **342/342 PASS**. Rieseguiti tutti e dieci gli script R2, D1, D0, D0M1–D0M7 il 25/10/2026: **473/473 PASS** (verifiche aritmetiche indipendenti, **non** test C# né confronto di conformità).
- xUnit `Ntc2018SteelMixedCompressionDominantD0M7Tests`: casi positivi per PNA nel raccordo e nell'ala, classi 1–4, riconciliazione provenienze, test di non sovrapposizione a D0M5, rifiuti N>0/α=0, N insufficiente o alla soglia, M=0, V≠0, EC3, INP, A/I o evidenze incoerenti.
- Perimetro di prova ancora da collaudare in **.NET**: l'ambiente di confezionamento non ha SDK; la conferma utente riguarda D0M6 e le versioni precedenti, **non D0M7**.

Comandi suggeriti su Windows dalla radice del progetto:

```powershell
dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj --filter FullyQualifiedName~Ntc2018SteelMixedCompressionDominantD0M7Tests
dotnet test StructuralSolver2D.sln
```

## Ciò che NON è coperto

- **α=0**: N di trazione dominante e asse plastico nella flangia/raccordo dal lato compresso. Non basta assegnare semplicemente all'anima classe 1: servono l'interpretazione specifica delle parti compresse e controlli delle ali realmente compresse. Il ramo è **Unsupported**.
- **PNA al confine esatto dell'anima piatta**: guardia numerica, non promozione automatica.
- Profili con geometria non certificabile o diversa da IPE/HEA/HEB nominali paralleli; fori, unioni, saldature non dichiarate, deformazioni da taglio o azioni fuori piano.
- Nessun calcolo di **resistenza plastica**, **interazione N/M/V**, instabilità flesso-torsionale/colonne, proprietà `Aeff/Weff` della classe 4, completezza automatica di tutte le sezioni o combinazioni FEM; nessuna idoneità della membratura o della struttura.

OPPO ([IPE](https://www.oppo.it/tabelle/profilati_ipe.htm), [HEA](https://www.oppo.it/tabelle/profilati_hea.htm), [HEB](https://www.oppo.it/tabelle/profilati_heb.htm)) fornisce dimensioni e proprietà nominali **indicative, non certificazioni di prodotto**. Il libro di Claudio Bernuzzi (Hoepli 2018), cap. 4, è riferimento didattico per la classificazione, non sostituisce la fonte normativa primaria.

## Prossimi passi

1. **Gate D0M7 Windows** ed eventuali correzioni fondate sui risultati.
2. **P0-D0M8**: esaminare con un'interpretazione normativa rigorosa il ramo plastico α=0 e l'effettiva presenza di parti compresse (tensione dominante). Se le precondizioni non sono dimostrabili, conservare `Unsupported`.
3. **P0-D2**: resistenza e interazioni N/M/V, con sottodomini certificabili.
4. **P0-E**: ritegni, geometria fuori piano e precondizioni fisiche di instabilità.
5. **P0-C3/C4 e P0-D3**: copertura obblighi, report e SLE acciaio; GUI C#/Avalonia **solo fase P3**.
