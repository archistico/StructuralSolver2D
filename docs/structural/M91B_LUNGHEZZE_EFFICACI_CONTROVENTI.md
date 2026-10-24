# M91-B — Lunghezze efficaci e condizioni di controvento per aste compresse

**Stato:** codice e casi xUnit predisposti; richiede collaudo su Windows .NET 8. Nessuna modifica al solutore FEM o alla regola M91-A.

## Scopo

M91-A richiede due lunghezze efficaci documentate: `Lcr,y` e `Lcr,z`. Il nuovo `SteelBucklingEffectiveLengthsM91B` costituisce un **preparatore di ipotesi geometriche**: verifica la completezza della descrizione dei ritegni e ricava entrambe le lunghezze per un insieme limitato di schemi ideali. Non dimensiona controventi o collegamenti, non misura la rigidezza trasversale effettiva, non deduce la stabilità fuori piano dal FEM 2D e non dichiara conforme l'asta. Prima di usare una lunghezza favorevole, l'ingegnere deve giustificare e verificare **resistenza e rigidezza** dei ritegni per l'asse pertinente.

L'**edizione della norma EC3 2005+A1:2014**, l'Annesso Nazionale, `gamma_M1`, classe a compressione e azioni FEM rimangono controllati da M91-A. Un'approvazione di sola instabilità flessionale non copre instabilità torsionale, flesso-torsionale, pressoflessione, instabilità globale o requisiti NTC complessivi.

## Metodi di preparazione (separati per asse)

| Modalità | Lunghezza efficace risultante | Condizioni restrittive |
| --- | --- | --- |
| `PinnedPinned` senza brace | `Lcr=L` (`k=1`) | Asta isolata idealmente incernierata e traslazioni laterali degli estremi impedite nel piano di instabilità; conferma esterna obbligatoria |
| `PinnedPinned` con brace intermedie | `Lcr=max(s_i)` (`k=1` per campata ideale) | Ogni ritegno blocca la traslazione sull'asse dichiarato; resistenza, rigidezza e ancoraggi verificati esternamente; membro prismatico con compressione uniforme; comportamento a campate idealizzate semplicemente appoggiate esplicitamente giustificato |
| `FixedFreeCantilever` | `Lcr=2L` | Mensola isolata con base perfettamente incastrata e sommità realmente libera; nessuna brace intermedia |
| `IndependentStabilityAnalysis` | `Lcr` fornita dall'ingegnere | Calcolo di stabilità indipendente esplicito e tracciato: utile per incastri elastici, telai deformabili, coefficienti non ideali, controventi con rigidezza finita |

**NON sono selezionati automaticamente** coefficienti di lunghezza efficace per giunti semirigidi, nodi mobili, telai multipiano, colonne presso-inflesse o controventature elastiche. La presenza di un nodo nel FEM, di una lunghezza geometrica `UnbracedLength*` o di un simbolo grafico di vincolo **non** equivale a un ritegno fisicamente efficace. I valori `0,5L`, `0,7L`, `0,8L` presenti nelle tabelle didattiche non vengono assegnati a partire da soli simboli: in situazioni diverse dallo schema ideale ammesso si richiede una lunghezza ricavata e documentata dall'analisi esterna.

Tutti gli input sono in **metri**. Le posizioni dei ritegni devono essere finite, interne all'asta e non coincidenti; il codice ordina le posizioni e conserva gli intervalli nella traccia. I piani di asse forte e debole devono riferirsi allo **stesso membro e alla stessa lunghezza geometrica**. La mappatura degli assi deve essere esplicitamente dichiarata.

## Esempio A7.1 Bernuzzi, HEB 200/S235

Un elemento lungo 7,50 m, idealmente trattenuto alle estremità sull'asse `y-y`, ha `Lcr,y=7,50 m`. Per l'asse `z-z`, una controventatura **effettivamente verificata** in mezzeria a 3,75 m genera due campate laterali ideali di 3,75 m, dunque `Lcr,z=3,75 m`. I valori vengono trasferiti all'assegnazione M91-A, che continua a calcolare curva, snellezza adimensionale e capacità secondo il proprio perimetro. Il benchmark di confronto è Bernuzzi, applicazione A7.1, pp. 192–193; per le condizioni di efficacia dei controventi, consultare §7.4, pp. 194–197 (libro fornito dall'utilizzatore, NON redistribuito).

Per la stessa inerzia e modulo elastico, dimezzare la lunghezza efficace moltiplica per quattro il carico critico di Eulero, **ma soltanto se il nuovo schema di vincoli risulta fisicamente giustificato**: `Ncr=π²EI/Lcr²`.

## Integrazione senza cambiare M91-A

La chiamata `SteelBucklingEffectiveLengthsM91B.PrepareAssignment(plan, m91aOriginal)` restituisce un risultato strutturato (`Resolved`, `MissingData`, `InvalidInput`, `Unsupported`). Solo quando lo stato è `Resolved`, il campo `Assignment` viene popolato con `Lcr,y`, `Lcr,z` e le tracce delle giustificazioni. Il resto dell'assegnazione M91-A **non viene modificato**. L'esito favorevole di una regola EC3 successiva rimane *parziale*.

Se è presente una brace non verificata, manca la prova sugli estremi, non coincide la lunghezza dei due assi oppure una modalità non supportata contiene brace che verrebbero ignorate, la preparazione **non** produce l'assegnazione. Non si usa mai un fattore `k` di fallback.

## Cosa non copre e successive priorità

1. M91-B non verifica in autonomia né la resistenza né la rigidezza richiesta ai controventi. Un flag con evidenza rappresenta un'**attestazione** esterna, non una prova numerica effettuata dal software.
2. Non calcola deformazioni laterali e coefficienti di lunghezza efficace di telai a nodi mobili o con vincoli elastici: richiede analisi indipendente tracciata.
3. Non verifica instabilità flesso-torsionale o instabilità laterale di travi; sarà oggetto di M91-C e dei necessari dati fuori piano.
4. Non considera diversa forza assiale nelle campate oppure effetti di carichi eccentrici: se tali condizioni interessano il progetto, questo modello ideale non è applicabile.

## Riferimenti

- Bernuzzi, *Progetto e verifica delle strutture in acciaio secondo NTC 2018 e Eurocodice 3*, seconda edizione (2018), cap. 7 §§7.3–7.4; in particolare A7.1, pagg. 192–193, e pp. 194–197. **Fonte tecnica didattica, non norma vigente**.
- EN 1993-1-1:2005+A1:2014, §6.3.1, calcolo di instabilità flessionale del membro; edizione e annesso obbligatori nella richiesta della regola M91-A.
- SCI, *Stability of Steel Beams and Columns*, P360: definizione di lunghezza efficace e importanza delle condizioni di ritegno: https://www.steelconstruction.info/images/0/0e/Sci_p360.pdf
- ArcelorMittal Steel Academy, *Buckling lengths*: https://constructalia.arcelormittal.com/en/sica-steel-academy/design-with-steel/columns-and-beams/buckling-lengths
