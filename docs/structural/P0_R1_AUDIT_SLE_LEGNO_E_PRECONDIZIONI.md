# P0-R1 — Audit NTC/Circolare: esercizio del legno e precondizioni G1/G3/G4

**25 ottobre 2026 — confronto statico di fonti primarie, sorgenti e test.**
**Nessun gate .NET nuovo:** P0-C2B/C2C0/C2C1 restano in attesa di Windows.
Il presente documento affina il registro [P0-R0](P0_R0_REGISTRO_FONTI_NTC2018_CIRCOLARE2019.md),
non estende il FEM piano, non dichiara verificata alcuna asta e non sostituisce
NTC, Circolare, Eurocodici o valutazioni professionali del caso reale.

## Fonti e perimetro del controllo

Fonti **lette direttamente** dagli allegati già identificati tramite SHA-256 in P0-R0:

- **D.M. 17/01/2018**, NTC 2018: §2.5.3 e Tab. 2.5.I, §§4.4.4–4.4.7,
  Tab. 4.4.V, §§4.4.8.1.7–1.9 e 4.4.8.2.1–2.
- **Circolare C.S.LL.PP. n. 7 del 21/01/2019**: §§C4.4.4–C4.4.7,
  formule **[C4.4.1]–[C4.4.3]**, e richiamo alle verifiche delle vibrazioni.
- Codice corrente: `Ntc2018TimberServiceabilityG5.cs`,
  `Ntc2018TimberCoefficientSelectorG1.cs`,
  `Ntc2018TimberCombinedSectionRulesG3.cs`,
  `Ntc2018TimberStabilityRulesG4.cs` e corrispondenti test xUnit in `tests/StructuralSolver2D.Verification.Tests/M92/`.

Le fonti italiane sono state confrontate nella **versione identificata**:
un'eventuale verifica rispetto al quadro normativo vigente in altra data
richiede un controllo ulteriore su modifiche, applicabilità e annessi.

## Risultati analitici

Legenda: **C** = valore/formula corrispondente nel sottoinsieme esaminato;
**P** = funzione condizionata da premesse non verificabili automaticamente;
**G** = funzione non implementata. **C non significa certificazione**.

| ID | Fonte primaria | Cosa fa il codice esaminato | Esito e confine |
|---|---|---|---|
| R1-01 | NTC §4.4.7, Tab. 4.4.V | `ReferenceKdef`: massiccio/lamellare, classi 1/2/3 → `0,60/0,80/2,00` | **C** valori; altre famiglie/materiali escluse esplicitamente |
| R1-02 | Nota dopo Tab. 4.4.V | Se materiale installato vicino alla saturazione e soggetto a essiccazione sotto carico, selettore richiede maggiorazione `AdditionalWetKdef ≥ 2,0` | **C/P** vincolo aritmetico corretto; constatazione di umidità, carico ed efficacia di provvedimenti resta esterna |
| R1-03 | NTC §4.4.7 e Circolare C4.4.7 | Requisiti `Emean`, `Gmean`, medesimo modello FEM, deflessioni firmate e scorrimento dei giunti | **P**: sono dichiarazioni/prove esterne, **non** misurazioni ricavate dal solver né verifica autonoma di Kser |
| R1-04 | NTC §2.5.3 Tab. 2.5.I | G5 applica `ψ0` alle variabili accompagnatrici della rara e `ψ2` al contributo differito | **C** per le 12 categorie codificate; non garantisce che tutti i casi di azione siano presenti né scelti correttamente |
| R1-05 | Circolare C4.4.7, [C4.4.3] | `u_fin = u_G(1+kdef) + u_Q,lead(1+ψ2,lead kdef) + Σ u_Q,i(ψ0,i+ψ2,i kdef) − u0` | **C** nella sola semplificazione per elementi di **identico comportamento viscoelastico**; camber iniziale e segni richiedono prova fisica |
| R1-06 | NTC §4.4.7; Circolare [C4.4.2] | Freccia netta deduce una volta la controfreccia; le sollecitazioni firmate si combinano prima del valore assoluto | **C/P** per formula adottata; validare convenzioni segni, corda degli appoggi, punti di massimo, camber reale |
| R1-07 | NTC §4.4.7 | `L/300` variabile rara istantanea; `L/200` totale finale; per mensola `L=2 × sbalzo` | **C** come **raccomandazioni in mancanza di indicazioni più precise**, non limiti universali: il progetto può richiederne di più severi |
| R1-08 | Circolare C4.4.7 | Controllo separato di freccia istantanea rara **totale**, con criterio di progetto dichiarato | **C/P**: la Circolare ne raccomanda il controllo, ma non impone automaticamente un unico denominatore universale |
| R1-09 | Circolare C4.4.7 | Per carichi permanenti che inducono flessione indicativamente oltre il **30%** della resistenza caratteristica, G5 richiede attestazione aggiuntiva prima del finale | **P**: il software non misura autonomamente la soglia; non interpretare il semplice `true` come dimostrazione |
| R1-10 | NTC §4.4.7 e Circolare C4.4.7 | Stabilità delle finiture/compatibilità di spostamenti, scorrimento dei collegamenti e taglio sono evidenziati come prove esterne | **P**: carichi, deformabilità reale dei nodi e dettagli costruttivi possono governare |
| R1-11 | Circolare C4.4.7, vibrazioni degli impalcati | Nessuna analisi dinamica del comfort da sola regola G5; esplicita esclusione nel trace | **G**: frequenze, accelerazioni, urti da calpestio e massa quasi-permanente richiedono altro modello e verifiche dedicate |
| R1-12 | Circolare C4.4.4, durata neve | Sopra 1000 m durata **almeno media**, sotto almeno **breve**, da affinare secondo sito; G1 accetta durata dichiarata nel manifesto e la usa per `kmod` | **P/gap di automazione**: la categoria in G1 non distingue neve/altitudine; classificazione fisica e coerenza con fonte non sono inferibili dal fingerprint |
| R1-13 | NTC §4.4.8.1.7–1.9 (G3) | Due condizioni N–M e taglio separato per sezione rettangolare e unico asse di flessione | **C/P** formula nel sottoinsieme; richiede dichiarazione esterna di momenti fuori piano assenti, sezione intatta, N/V/M concomitanti, e stabilità G4 separata |
| R1-14 | NTC §4.4.8.2.1–2 (G4) | Kernel `BeamReduction` e `ColumnReduction` corrispondono ai rami `kcrit` esaminati; usa `E0,05` per la colonna | **C/P**: lunghezze efficaci sui **due assi**, ritegni torsionali, `σm,crit`, imperfezioni ed eccentricità non provengono dalla soluzione 2D; nessun pass completo di membratura |

### Controprova aritmetica indipendente G5

Nuovo script `benchmarks/independent/p0r1/verify_p0r1_timber_sle.py`
basato su **frazioni esatte**, senza importare codice C#. Riferimento:
trave semplicemente appoggiata di 4 m, `100×200 mm`, `Emean=11000 MPa`,
tre carichi uniformi indipendenti `G=0,5`, `Q=0,7`, `W=0,2 kN/m`;
`u = 5qL⁴/(384EI)`. Si ottengono `uG=25/11`, `uQ=35/11` e `uW=10/11 mm`.

| Caso | Valore indipendente | Verifica |
|---|---:|---|
| Rara complessiva lorda (W accompagnatore ψ0=0,60) | `6 mm` | PASS |
| Rara variabile istantanea | `41/11 mm` | PASS |
| Rara totale netta con controfreccia di 1 mm | `5 mm` | PASS |
| Finale classe 1, `kdef=0,60`, controfreccia 1 mm | `763/110 mm` | PASS |
| Finale classe 3, `kdef=2,00` | `126/11 mm` | PASS |
| Finale classe 1 installazione umida, `kdef=0,60+2,00` | `1473/110 mm` | PASS |
| Finale con vento opposto | `643/110 mm` | PASS |
| Finale neve sopra 1000 m accompagnatrice | `157/22 mm` | PASS |
| Finale manutenzione copertura H, `ψ0=ψ2=0` | `703/110 mm` | PASS |
| Indicazioni `L/300`, `L/200`, e `2L/300` per sbalzo 4 m | `40/3`, `20`, `80/3 mm` | 3 PASS |

**Esito: 12/12 controlli Python indipendenti PASS.** Essi riproducono
l'aritmetica prescritta per l'ipotesi semplice, non confrontano il risultato
effettivo di C# con Python né danno prova di combinazioni complete o del
comportamento reale di giunti e finiture. Suite Windows invariata.

## Decisioni e priorità tecniche

1. **Nessun errore algebrico G5 dimostrato** nei coefficienti, nella formula
   della deformazione finale e nei golden esaminati. Non effettuare
   modifiche alla produzione o ai golden esistenti senza un caso che
   riproduca una divergenza verificabile.
2. **Alta — classificazione della neve:** durante P0-C3/P0-D introdurre un
   collegamento tipizzato tra `actionId`, tipo neve, quota/contesto,
   durata minima e combinazione effettiva. Non dedurre la durata dal solo ψ
   (sono due concetti diversi). Per ora documentare l'evidenza esterna.
3. **Alta — G4:** vietare qualunque esito di stabilità globale sulla base
   del solo FEM 2D. Progettare prove indipendenti di ritegni, lunghezze
   efficaci sui due assi e momento critico flesso-torsionale.
4. **Alta — G3 e completezza:** un controllo locale N–M e V positivo non
   chiude le verifiche di colonna, trave, connessioni o membratura.
   Le verifiche richieste dipendono dallo stato fisico del membro.
5. **Media — G5:** nel contratto P0-C3/C4 legare prova del massimo di freccia
   sul tratto, freccia riferita alla corda, segni e controfreccia, limiti
   prestazionali dell'opera e scorrimento degli appoggi/collegamenti.
6. **Fuori ambito presente:** una verifica delle vibrazioni richiede
   modellazione dinamica/comfort aggiuntiva, da non simulare mediante il
   solo rapporto statico di freccia.

**Prossimo audit normativo suggerito:** acciaio NTC §§4.2.3–4.2.4,
classificazione sezioni, trazione lorda/netta, flessione, taglio e
stabilità, sempre distinto dalle regole EC3 con i propri annessi.
**UI C#/Avalonia solo alla fine del progetto (P3).**
