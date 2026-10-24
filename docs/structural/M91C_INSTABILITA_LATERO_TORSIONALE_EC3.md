# M91-C — Instabilità latero-torsionale, travi I/H laminate

**Stato:** implementazione predisposta; **test .NET 8 da eseguire su Windows**. Il motore FEM e le precedenti regole M89–M91-B restano invariati. Nessuna certificazione di conformità globale.

## Perimetro

La nuova regola `SteelLateralTorsionalBucklingRuleM91C` implementa il *metodo generale* della **EN 1993-1-1:2005+A1:2014**, §§6.3.2.1–6.3.2.2, equazioni (6.54)–(6.56), utilizzando le curve **raccomandate** per I/H laminati (tab. 6.3–6.4). **Non implementa** il metodo alternativo del §6.3.2.3 né le edizioni EC3 del 2022. L'Annesso Nazionale applicabile al progetto e l'adozione del metodo devono essere dichiarati e giustificati; il sistema NON seleziona o presume l'annesso. L'identificativo di test `IT-TEST-ANNEX-EXPLICIT` non costituisce una conferma della normativa nazionale applicabile.

Sono considerate esclusivamente:

- travi IPE, HEA, HEB **a doppia simmetria**, ad ali parallele, prismatiche, con proprietà M88-B2 **indicative, non certificate**;
- flessione pura attorno all'asse forte, nella stazione verificata (`N=V=0`, `M≠0`);
- classi 1–3 sotto **flessione pura sull'asse forte**, ricalcolate con M90-A; classe 4 non implementata;
- per classe 3, `W_y=W_el,y` dal catalogo; per classi 1–2, `W_y=W_pl,y` **fornito e documentato dall'esterno** (il catalogo M88-B2 NON lo contiene): controlli dimensionali di plausibilità NON certificano la proprietà;
- acciai S235, S275, S355 con `f_y` selezionato per spessore e `gamma_M1` con fonte esplicita;
- sollecitazioni provenienti da **un unico risultato FEM**, con membro, posizione, lato e combinazione identificati.

Una verifica superata si riferisce **solo alla sezione dichiarata nel segmento dichiarato**; non è una verifica dell'inviluppo dei momenti dell'intera asta. L'utilizzatore deve esaminare tutti i segmenti e tutte le combinazioni e la condizione di fase costruttiva. Il flag di validazione delle condizioni al contorno non sostituisce un controllo progettuale dell'effettiva **resistenza e rigidezza** di controventi e collegamenti.

## Determinazione obbligatoria del momento critico Mcr

Una e una sola delle seguenti fonti è ammessa.

**A. Momento critico esterno**, positivo, con attestazione di origine, schema di vincolo, andamento del momento, altezza e posizione dei carichi, condizioni torsionali e all'ingobbamento. Va ottenuto da uno strumento di stabilità o da una procedura indipendente appropriata all'effettiva distribuzione di azioni e vincoli. Un dato non documentato produce esito `MissingData`.

**B. Caso analitico standard**, definito soltanto per trave a doppia simmetria, forche alle estremità (impediscono spostamento laterale e rotazione torsionale; permettono l'ingobbamento), senza ritegni intermedi, sotto **soli momenti alle estremità che producono un momento uniforme**. Non sono ammessi carichi trasversali e non si presume un coefficiente equivalente `C1` da altri diagrammi. Sono richiesti E [MPa], G [MPa], I_t [cm⁴], I_w [cm⁶], I_z [cm⁴] tabellato e la lunghezza L [m].

\[
M_{cr}=\frac{\pi}{L}\sqrt{E I_z\left(G I_t+\frac{\pi^2 E I_w}{L^2}\right)}
\]

Con le conversioni esplicite: `cm4 -> mm4 ×10^4`, `cm6 -> mm6 ×10^6`, `m -> mm ×1000`, `Nmm -> kNm /10^6`.

Questa equazione è il caso standard della presentazione didattica della **Commissione Europea – JRC**, Rui Simões, *Eurocodes – Design of steel buildings with worked examples*, Bruxelles 2014, slide 22 (pagina PDF 22):
https://eurocodes.jrc.ec.europa.eu/sites/default/files/2022-06/05_Eurocodes_Steel_Workshop_SIMOES.pdf

## Riduzione per instabilità latero-torsionale

\[
\bar\lambda_{LT}=\sqrt{\frac{W_y f_y}{M_{cr}}},\quad
\Phi_{LT}=\frac12[1+\alpha_{LT}(\bar\lambda_{LT}-0.2)+\bar\lambda_{LT}^2]
\]

\[
\chi_{LT}=\min\left(1,\frac{1}{\Phi_{LT}+\sqrt{\Phi_{LT}^2-\bar\lambda_{LT}^2}}\right),\quad
M_{b,Rd}=\frac{\chi_{LT}W_yf_y}{\gamma_{M1}}
\]

Le curve generali raccomandate per sezioni I/H **laminate** sono `a` per `h/b <= 2` (α=0,21), `b` per `h/b > 2` (α=0,34). Si usa una formula e un insieme di curve omogenei: NON si mescolano curve del metodo alternativo e formula del metodo generale.

## Controlli indipendenti dal Bernuzzi

Bernuzzi, *Progetto e verifica delle strutture in acciaio secondo NTC 2018 e EC3*, Hoepli, seconda edizione (2018), applicazione **A8.2**, pagine a stampa 233–235:

- IPE 330 S275: `I_z=788,1 cm⁴`, `I_t=28,15 cm⁴`, `I_w=199,9×10⁹ mm⁶`, `E=210000 MPa`, `G=80769 MPa`, `L=2,5m`;
- formula standard con `C1=1`: **Mcr≈482,36 kNm**; nell'esempio di momento non uniforme si assume **C1=1,141** (da fonte/calcolo esterno), ottenendo **Mcr≈550,37 kNm** (testo ≈550,4 kNm);
- `W_pl,y=804,3 cm³`, `fy=275 MPa`, `gamma_M1=1,05`, curva generale b → `lambda_LT≈0,634`, `chi_LT≈0,820`, `M_b,Rd≈172,66 kNm`, a fronte dei 172,5 kNm arrotondati del testo.

**ATTENZIONE:** il codice standard M91-C implementa **C1=1 soltanto**. Il coefficiente `1,141` viene usato unicamente nel test matematico di confronto, non inferito automaticamente in produzione. Il profilo IPE 330 non è introdotto fittiziamente nel catalogo M88-B2.

Il libro dell'utente è un riferimento didattico e **non** viene incluso nello ZIP di distribuzione.

## Non coperto

Interazioni N–M–V e taglio importante; instabilità flesso-torsionale sotto compressione; verifica di collegamenti e controventi; torsione da carichi eccentrici; vincoli torsionali elastici; sezioni monosinmetriche o INP; classe 4; proprietà torsionali automatiche; scelta nazionale dei coefficienti e conformità NTC 2018; inviluppo completo di aste e combinazioni; metodi 2022; metodo alternativo 6.3.2.3; fase costruttiva. Nessuno di questi controlli viene implicitamente dichiarato superato da M91-C.

## Roadmap

1. **M91-D:** audit integrato sulle regole acciaio: copertura verifiche mancanti, dichiarazioni di completezza per asta, combinazioni, stazioni e report; validazione indipendente con ulteriori modelli.
2. **M92–M94:** legno EC5/NTC, separando resistenze caratteristiche, classi di servizio, kmod e kdef, e controlli di instabilità.
3. **M95–M99:** SLE, report normativi, regressione, release delle verifiche.
4. **GUI C#/Avalonia:** esclusivamente dopo consolidamento dei moduli normativi.

