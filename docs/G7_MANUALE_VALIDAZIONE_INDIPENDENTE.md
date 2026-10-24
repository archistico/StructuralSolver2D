# StructuralSolver2D — G7 Manuale di validazione indipendente e limiti

**Milestone:** G7, 24 ottobre 2026. **Baseline:** G6 — GATE PASSED confermato dall’utente su Windows. **Stato G7:** **G7 GATE PASSED**, test Windows confermati dall’utente il 24/10/2026. Le limitazioni riportate nel manuale restano valide.

## 1. Scopo e natura delle prove

G7 prepara una base riproducibile per validare **singoli calcoli** e la loro interpretazione nei report; non è una certificazione di conformità NTC 2018, EC3 o EC5. Il superamento dei test attesta che **i test selezionati** passano con un determinato runtime e set di input; non dimostra la correttezza per tutti i modelli o l’esaustività delle verifiche progettuali.

**Fonti separate per ruolo:**

1. **NTC 2018** DM 17/01/2018 §§2.5.3, 4.4.6, 4.4.8 e 10.2: riferimenti cogenti delle verifiche del legno applicate e della valutazione del software.
2. **Circolare 7/2019** §C4.4.8.1.9, §C4.4.7: chiarimenti per taglio del legno e deformazioni differite.
3. **Ballio, Mazzolani, Bernuzzi, Landolfo, *Strutture di acciaio*, Hoepli 2020**, cap. 1 fig. 1.12 e cap. 2, 8, 9: contesto fisico e ispirazione per benchmark (NON testo normativo e NON fonte dei valori artificialmente fissati per G7).
4. **Derivazioni G7 indipendenti:** equazioni dell’equilibrio e formule analitiche elementari dichiarate sotto, calcolate senza interrogare il solver.

I dati aggiunti sono in `benchmarks/G7_INDEPENDENT_GOLDEN.json`, nei modelli `benchmarks/beams/B13…`, `B14…` e nei test G7. Restano disponibili i 9 riferimenti M92-N5, la matrice M92-N6, i casi candidati del libro L2/L3 e i test già collaudati G1–G6.

## 2. Matrice di validazione: ciò che è coperto da G7

| ID | Livello | Sorgente del risultato atteso | Assert realmente eseguiti | Limitazione |
|---|---|---|---|---|
| B13 | FEM Euler–Bernoulli, linea spezzata A-B-C | Equilibrio indipendente | Reazioni A/B, segni, momento globale di equilibrio, Mmax all’appoggio B nel catalogo | **A è un vincolo bilatero che resiste a trazione**; nessuna separazione/contatto |
| B14 | FEM Euler–Bernoulli, campata+sbalzo | Equilibrio indipendente | Reazioni A/B, momento globale, differenza tra casi | Il carico è nominale sintetico e non una combinazione NTC certificata |
| G7-T-NM | NTC legno G1→G3→G6 | Tensioni di Navier e formula NTC 4.4.6a/b + controllo V distinto | kmod, gammaM, rapporto max, stato G3, audit G6 incompleto | Prove fisiche esterne nei fixture **simulate**, non validate |
| G7-T-BENDING-FAIL | NTC legno G1→G2→G6 | M/W contro fm,d calcolati a mano | η>1 e prevalenza di Failed sull'incompletezza | Non è verifica del membro né di collegamenti |
| G7-NEAR-STATION | Identità FEM e tracciabilità | Contratto G6 stazione esatta | Un risultato a x=2 m NON copre x=2.000001 m | Stazioni critiche non scelte automaticamente |
| G2–G5 previsti | Regole normative già collaudate | Test numerici esistenti e tracce indipendenti | Regressione completa | Limitazioni intrinseche di ogni regola restano |
| M91D | Audit acciaio EC3 | Regressioni preesistenti | Un risultato locale non diventa approvazione completa | G7 non introduce profilo NTC acciaio |

### 2.1 B13 e B14: trave con sbalzo, carichi a tratti

Coordinate: A=(0,0), B=(6,0), C=(8,0) **metri**. Appoggio A: cerniera che trattiene anche lo spostamento verticale positivo (modello bilatero); appoggio B: carrello verticale. Elasticità lineare; tratti AB=6 m e BC=2 m, collegati con rotazione continua in B. Modulo E=210000000 kN/m², inerzia I=2e−5 m⁴ (per questi controlli di equilibrio non incidono sulle reazioni, perché lo schema è esternamente isostatico). Carichi ripartiti verticali verso il basso.

**B13, solo BC:** q=5 kN/m su 2 m, risultante 10 kN a x=7 m. Equilibrio:

- ΣM_A=0: `R_B × 6 = 10 × 7` → `R_B = 35/3 = +11.6666666667 kN`.
- ΣFy=0: `R_A = 10 − 35/3 = −5/3 = −1.6666666667 kN`.
- Momento flettente in B, modulo: `q × 2² / 2 = 10 kNm`; segno fisico di inflessione negativo.

**B14, AB e BC:** oltre al carico precedente, 5 kN/m su AB produce una risultante 30 kN a x=3 m.

- ΣM_A=0: `R_B × 6 = 10×7 + 30×3 = 160` → `R_B=80/3=+26.6666666667 kN`.
- ΣFy=0: `R_A=40−80/3=40/3=+13.3333333333 kN`.
- Differenza B14−B13: +15 kN a ciascun appoggio. Un test specifico controlla questa proprietà.

**Interpretazione fisica:** B13 produce una forza di sollevamento all'appoggio A. Il benchmark suppone un ancoraggio in grado di sopportarla; non modella il distacco di un appoggio che reagisce soltanto a compressione. La bontà dei calcoli per questa ipotesi NON ne giustifica l'applicazione a un appoggio reale senza hold-down.

### 2.2 G7-T-NM: C24, interazione N–M e taglio concorrente

Sezione rettangolare **100×200 mm**, area 20.000 mm², modulo resistente forte `W=100×200²/6=666666,6667 mm³`.

Dati: `N=+20 kN`, `V=+10 kN`, `M=+3 kNm`, simultanei alla medesima stazione; `f_t,0,k=14,5 MPa`, `f_m,k=24 MPa`, `f_v,k=4 MPa`, `kmod=0,8`, `γM=1,5`, `kcr=2/4=0,5`.

- `σt,0=20000/20000=1,0 MPa`;
- `σm=3000000/666666,6667=4,5 MPa`;
- `σv=1,5×10000/(0,5×20000)=1,5 MPa`;
- `ft,d=14,5×0,8/1,5=7,7333333 MPa`;
- `fm,d=24×0,8/1,5=12,8 MPa`, `fv,d=4×0,8/1,5=2,1333333 MPa`;
- prima eq.: `ηa=σt/ft,d+σm/fm,d=0,480872844827586…`;
- seconda eq.: `ηb=σt/ft,d+0,7σm/fm,d`, meno gravosa;
- taglio separato: `ηV=σv/fv,d=0,703125`;
- governa `max(ηa,ηb,ηV)=0,703125`, parziale.

La verifica passa **localmente** nel caso assunto. G6, applicato al `DesignCheckReport` reale, DEVE restare `Incomplete` perché nel piano sintetico tutte le altre aree sono marcate **UnsupportedOrNotEvaluated**, mai false esclusioni fisiche.

### 2.3 G7-T-BENDING-FAIL

C24, `M=+20 kNm` puro, `σm=20×10⁶/(100×200²/6)=30 MPa`, `fm,d=12,8 MPa`, `η=2,34375 > 1`. Un report `Failed` reale prodotto da G2 deve conservare `FailedWithinDeclaredScope` nell'audit G6 anche se persistono altre lacune.

## 3. Risultati e prove non ancora coperti

**Lettura importante:** i test non validano automaticamente i seguenti aspetti; per un'applicazione reale sono necessari criteri, metodi o prove esterni appropriati.

- **FEM 2D:** vincoli reali che ammettono contatto/apertura, aste resistenti solo a trazione, reazioni di sollevamento, nodi con eccentricità, taglio deformativo di Timoshenko, imperfezioni fisiche, interazione fuori piano, non linearità materiale e meccanismi globali non nel modello.
- **NTC legno:** comportamento a compressione perpendicolare/inclinata, trazione perpendicolare, intagli, torsione, interazioni biassiali, collegamenti, pressoflessione con instabilità, vibrazioni, deformazioni da scorrimento/umidità non modellate, incendio, sisma, tracciabilità certificata del materiale.
- **Acciaio EC3/NTC:** G7 non aggiunge algoritmi per piatti/pareti, fori complessi, instabilità di sezioni sottili fuori dalla classe/proprietà supportate, collegamenti, torsione generale o validazione nazionale completa; resta l'audit M91D del solo ambito dichiarato.
- **Progettazione:** la completezza e la corretta scelta di azioni/combinazioni, della posizione di controllo e dei fattori favorevoli/sfavorevoli non derivano automaticamente dal superamento delle regole.
- **Secondo ordine:** la presenza nel progetto di un'analisi linearizzata M82 non costituisce attestazione di non linearità geometrica generale o di un'analisi di imperfezioni completa.

## 4. Come eseguire e documentare il collaudo G7 su Windows

Dalla radice dell'archivio **G7** estratto in cartella pulita:

```powershell
.\scripts\verify-g7-manifest.ps1
.\scripts\run-g7-tests.ps1
```

Il runner deve verificare, nell'ordine: build Release `-warnaserror`, suite mirata G7 FEM, suite mirata G7 NTC/audit, intera Verification, intera Core/Analysis/Catalogs/Reporting/CLI. Non assumere che la compilazione sia possibile su questa macchina: questo rilascio **non è compilato qui** perché il .NET SDK non è disponibile nell'ambiente di preparazione.

Conservare il testo `G7 GATE PASSED` solo dopo esito Windows. In caso di errore archiviare log completo, codice test, input, risultato atteso, risultato ottenuto e diagnosi, senza rilassare le tolleranze automaticamente.

Per ogni nuovo benchmark: indicare documento e pagina, versione norma, geometria, unità, vincoli, carichi con segno, combinazioni, risultato analitico derivato separatamente, tolleranza giustificata, limite fisico e assegnazione a un test che venga realmente eseguito. Numeri approssimati estratti da figure/abachi restano indicativi.

## 5. Criterio di accettazione e limite del gate

Un G7 positivo richiede che **tutti** i test Windows del runner terminino senza errori né warning di compilazione e che il manifest sia integro. Questa è una **validazione regressiva numerica selezionata**, non un'autorizzazione alla progettazione normativa completa. L'eventuale scelta della GUI rimane subordinata al consolidamento successivo dei limiti ancora aperti.
