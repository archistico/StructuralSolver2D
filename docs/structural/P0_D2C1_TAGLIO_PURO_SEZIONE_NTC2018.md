# P0-D2C1 — Taglio puro nel piano dell'anima, acciaio NTC 2018

Data: 25/10/2026. **Codice e test scritti; 848/848 verifiche aritmetiche indipendenti PASS, gate xUnit .NET PENDING** (SDK non presente nell'ambiente di assemblaggio). L'autrice ha comunicato **«test ok»** per D2A/D2B e le regressioni precedenti; il log Windows non è allegato.

## Ambito e calcolo

`Ntc2018SteelPureShearResistanceRuleD2C1` aggiunge una regola NTC nativa **solo sezionale**, autonoma dalle regole EC3 e dai rami NTC D2A (compressione) e D2B (flessione). Il suo unico impiego è il **taglio locale Vy nel piano dell'anima** di un profilato laminato a caldo **IPE, HEA, HEB** ad ali parallele, doppiamente simmetrico e senza fori rilevanti, in una sola **stazione e un solo lato** di una **combinazione FEM SLU**. Richiede valori simultanei e con segno: **N = 0, Mz = 0, Vy ≠ 0**. Le azioni spaziali e la torsione devono essere escluse sulla base di evidenze indipendenti. Gli INP ad ali inclinate sono esclusi.

Fonte: **D.M. 17 gennaio 2018 (NTC 2018)**, §4.2.4.1.2.4, equazioni **[4.2.16]–[4.2.18]**; coefficiente **γM0 = 1,05**, Tab. 4.2.VII.

Per il taglio parallelo all'anima di un profilato I/H laminato:

\[
A_v=A-2bt_f+(t_w+2r)t_f
\]

\[
V_{c,Rd}=\frac{A_v f_{yk}}{\sqrt{3}\,\gamma_{M0}},\qquad
\frac{V_{Ed}}{V_{c,Rd}}\leq 1
\]

- `A`, `Av` in mm², `b`, `tf`, `tw`, `r` in mm; `fyk` in N/mm²; `Vc,Rd` in N, poi convertito in **kN** dividendo per 1.000.
- Il valore `A` si ottiene dalla tabella nominale OPPO convertendo cm² → mm²; l'area `Av` **è ricalcolata** geometricamente, non inventata o dedotta dalla sola A.
- Si pretende inoltre un valore numerico `Av` documentato **indipendentemente** e coerente con quello ricalcolato entro **0,1%**. La capacità impiega il **minore** fra i due valori nominali, per evitare benefici da arrotondamento. Lo 0,1% non è una tolleranza di conformità del prodotto.
- Per il limite di snervamento, il catalogo `M88C` usa il grado dichiarato S235/S275/S355 e lo spessore maggiore tra anima e ala. Nessun fattore EC3 viene sostituito al profilo **NTC2018 D1 esatto**.

## Schermo di instabilità a taglio: obbligatorio

Il valore sezionale `Vc,Rd` **non autorizza** a ignorare l'instabilità dell'anima. In accordo a **NTC 2018 [4.2.27]**, per un'anima non irrigidita si deve valutare l'instabilità quando:

\[
\frac{h_w}{t_w}>\frac{72}{\eta}\sqrt{\frac{235}{f_{yk}}}
\]

La regola sceglie conservativamente **η = 1,00**, ammesso dalle NTC, e usa il limite superiore geometrico **`hw_upper = h − 2 tf`**, che comprende anche la porzione con raccordi anziché ridurre artificialmente la snellezza della parte piana. Se **`hw_upper/tw` supera la soglia**, il risultato è **`NotApplicable`**: si deve applicare la verifica d'instabilità del §4.2.4.1.3.4, **non implementata da D2C1**. Un valore inferiore alla soglia è soltanto un **controllo di accesso** alla formula di resistenza sezionale, non costituisce una verifica generale di stabilità.

L'evidenza `UnstiffenedWebConfirmed` è richiesta esplicitamente: l'ambito iniziale non copre anime irrigidite, pannelli con carichi localizzati, plasticizzazione da taglio con torsione né l'impiego di formule specialistiche alternative.

## Esempi nominali riproducibili

| Caso | Area da OPPO e dati sezione | `Av` NTC 2018 | `Vc,Rd` | Schermo conservativo |
|---|---|---:|---:|---|
| IPE 200, S235 | `A=2848 mm²`, `b=100`, `tf=8,5`, `tw=5,6`, `r=12 mm` | **1399,6 mm²** | **180,8514 kN** | `(200−17)/5,6=32,68 ≤ 72,00` |
| HEA 280, S355 | `A=9726 mm²`, `b=280`, `tf=13`, `tw=8`, `r=24 mm` | **3174,0 mm²** | **619,5628 kN** | `(270−26)/8=30,50 ≤ 58,58` |

Esempi di **sezione nominale**, non di prodotti verificati, montaggi reali, aste o opere. Il catalogo OPPO contiene dati indicativi di terzi, non certificati o accettazioni di materiali e prodotti.

## Contratto di prova e comportamento in caso di dati insufficienti

L'evidenza `Ntc2018SteelPureShearEvidenceD2C1` si lega per identità esatta a **membro, sezione, grado, analisi FEM, combinazione di carico, ascissa numerica e lato della sezione** (`Continuous`, `BeforePointLoad`, `AfterPointLoad`). Deve fornire documenti o identificativi descrittivi per stato dei fori, geometria, grado, carico puro, area di taglio e quattro attestazioni fisiche indipendenti: geometria I/H parallela doppiamente simmetrica; direzione Vy locale nell'anima; anima senza irrigidimenti; esclusione di torsione e azioni fuori piano.

L'engine restituisce:

- **`Passed` / `Failed`** soltanto quando le premesse sono complete e confronta `abs(Vy)` con `Vc,Rd`;
- **`MissingData`** per provenienza incompleta, area non coerente, fori non esclusi, grado ignoto, non contemporaneità dei valori, profilo normativo non esatto o evidenze mancanti;
- **`NotApplicable`** per N/M concomitanti, Vy nullo, INP o anima che richiede la verifica d'instabilità non implementata;
- **`Unsupported`** se il codice/edizione non coincidono con la NTC fissata; **`InvalidInput`** quando il contratto di input comune rifiuta NaN/∞ e altri dati invalidi.

Una singola riga `Passed` riguarda soltanto la resistenza a taglio della **stazione selezionata**. Anche se il report delle sole regole richieste mostra `OverallStatus.Passed`, **non significa mai asta o struttura verificata**: `EstablishesCompleteMemberCompliance=false`. Per provare la copertura del membro occorrono il modulo futuro C3, le altre verifiche obbligatorie e la scansione di stazioni e combinazioni.

**Non implementati né deducibili:** instabilità dell'anima, torsione, carichi concentrati locali, fatica, taglio con flessione o forza normale (`M–V`, `N–V`, `N–M–V`), sezioni asimmetriche/INP/saldate/irrigidite, fori/giunti, instabilità laterale o d'asta, aste/capriate in regime non lineare, situazione sismica, verifica di deformazione SLE, integrità della catena di fornitura, esaustività delle combinazioni. La classificazione D0 per compressione o flessione **non viene confusa** con lo schermo di instabilità a taglio.

## Verifiche e tracciabilità

1. Golden indipendente `benchmarks/independent/p0d2c1/verify_p0d2c1_ntc_pure_shear.py`: **848/848 PASS** con frazioni esatte / Decimal per aree, capacità al quadrato, soglia dell'anima; **90** combinazioni profilato I/H × grado e controprova con geometria sintetica snella. Python non esegue né certifica l'algoritmo C#.
2. Tutti gli script Python da `p0r2` a `p0d2c1`, inclusi D1/D0/D0M1–D0M8/D2A/D2B: **3.257/3.257 PASS in 14 script**.
3. Nuovi xUnit `tests/StructuralSolver2D.Verification.Tests/D2/Ntc2018SteelPureShearD2C1Tests.cs`: casi positivi IPE200/HEA280, V positivo/negativo, confini di capacità, area arrotondata, rifiuto anima snella, N/M concomitanti, assenza di provenienza, fori/torsione, materiale/edizione/fattori errati, incompletezza del rapporto globale. **Non eseguiti** in questa sessione perché manca il comando `dotnet`.
4. **Ultimo gate utente**: su sua dichiarazione, D2A+D2B e regressioni precedenti **PASS .NET**, senza log allegato. **D2C1 .NET PENDING**, non includerlo nel gate confermato.

## Fonti consultabili

- [Gazzetta Ufficiale — D.M. 17 gennaio 2018, Aggiornamento delle Norme tecniche per le costruzioni](https://www.gazzettaufficiale.it/eli/id/2018/02/20/18A00716/sg), testo normativo di riferimento, §§4.2.4.1.2.4, 4.2.4.1.3.4, Tab. 4.2.VII.
- [Capitolo 4 NTC2018, copia PDF di lettura](https://www.studiopetrillo.com/files/ntc2018/cap4.pdf), in particolare le formule numerate [4.2.16]–[4.2.18] e [4.2.27].
- Cataloghi OPPO **indicativi**, [IPE](https://www.oppo.it/tabelle/profilati_ipe.htm), [HEA](https://www.oppo.it/tabelle/profilati_hea.htm), [HEB](https://www.oppo.it/tabelle/profilati_heb.htm).

## Passi successivi

1. Su Windows: `dotnet test` della soluzione, registrando numero PASS/FAIL di D2C1 e regressione; fino a quel momento la milestone **non è collaudata .NET**.
2. **P0-D2C2**: studiare l'interazione taglio–momento per sezioni e classi provabili, sulla base delle NTC [4.2.30]–[4.2.32] e delle proprietà richieste; nessuna promozione automatica dai casi puri D2B/D2C1.
3. **P0-E**: contratto per ritegni reali, instabilità e condizioni fisiche del modello; **P0-C3/C4**: completezza delle verifiche, report e copertura; **P0-D3**: SLE documentati.
4. Interfaccia **C#/Avalonia soltanto in P3**, dopo la validazione numerica e normativa.
