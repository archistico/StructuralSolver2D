# M89-A — Prima verifica EC3 circoscritta alla flessione elastica

## Stato
Questa è una prima **regola numerica di resistenza di sezione**, non una verifica completa di asta né una certificazione normativa. Motore FEM M86 e cataloghi M88 invariati.

## Fonte e versione
- EN 1993-1-1:2005, con A1:2014, §6.2.5(2), equazione (6.14), **sezione di classe 3**: `M_el,Rd = W_el,min · fy / gamma_M0`.
- La presenza di EN 1993-1-1:2022 (seconda generazione) **non** autorizza a usare la vecchia numerazione per il 2022.
- Fonte tecnica di confronto: https://www.eiseko.it/public/mat/file/Runet/SteelSectionsEC3ENG.pdf ; scheda normativa: https://store.uni.com/en/en-1993-1-1-2022 .
- Il catalogo OPPO è indicativo: https://www.oppo.it/tabelle/profilati_ipe.htm e relativi HEA, HEB, INP. **Nessuna certificazione di prodotto**.

## Condizioni OBBLIGATORIE
- Asse forte OPPO `Wx` collegato *esplicitamente* al momento locale `Mz` del FEM 2D, senza rotazioni implicite.
- Classificazione **3 dichiarata e giustificata esternamente**; M89-A non calcola la classe.
- Assenza di fori rilevanti dichiarata; M89-A non calcola la riduzione ai fori.
- Sollecitazioni **concomitanti** `N=0`, `V=0`, `M` noto, prese dalla medesima sezione e combinazione. Se `N` o `V` non è zero, la regola restituisce **NotApplicable**, non passa.
- Profilo `EN 1993-1-1`, edizione `2005+A1:2014`, Annesso Nazionale dichiarato da chi usa la libreria; nessuna impostazione nazionale implicita. Coefficiente `gamma_M0` **positivo, con fonte esplicita**, aggiunto dall'utilizzatore; nessun valore automatico.
- `fy` è selezionato dal catalogo M88-C in base allo spessore massimo tabellato `max(tf, tw)`. Fuori banda non viene estrapolato.

## Unitá / benchmark indipendente
Per IPE 200: `Wx=194,3 cm³`, `tf=8,5 mm`, S235 `fy=235 MPa`, `gamma_M0=1` dichiarato solo come numero di esempio. `M_el,Rd = 194,3 × 235 / 1000 = 45,6605 kNm`. A `MEd=20 kNm`, `eta = 20/45,6605 = 0,438...`.

## Limiti espliciti
- Assenti: instabilità flesso-torsionale, instabilità di asta, taglio, interazione N-M-V, torsione, verifiche dei collegamenti, combinazioni automatiche SLU, resistenza al fuoco, plasticità, sezioni di classe 4, classificazione della sezione.
- **Il risultato Passed riguarda esclusivamente la regola nominata e la singola sezione.** Il generico `DesignCheckReport.OverallStatus` riassume le sole regole richieste: non implica conformità normativa globale. Per un set incompleto di verifiche occorre includere esplicitamente i codici mancanti: l'engine restituisce `Incomplete` per `Unsupported`.
- I test sono casi sintetici indipendenti e prove dei dati mancanti, non benchmark commerciali di intera struttura.
- In applicazioni reali servono annesso nazionale applicabile, verifica indipendente, dati prodotti qualificati e responsabilità professionale.

## Roadmap
M89-B: resistenze assiali e taglio con dati espliciti; M90: interazioni/classificazione; M91: instabilità fuori piano con ritegni verificabili. M92–M94 legno, M95 SLE, M96–M99 release normativa e validazione esterna; GUI solo dopo.
