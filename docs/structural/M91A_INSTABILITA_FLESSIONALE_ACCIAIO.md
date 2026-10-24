# M91-A — Instabilità flessionale delle aste compresse (EC3)

**Stato:** codice e test predisposti. Test Windows .NET 8 **ancora da eseguire**. Nessun solver FEM modificato.

## Perimetro e fonti

- EN 1993-1-1:2005+A1:2014, §6.3.1.1–6.3.1.3, equazioni (6.46), (6.47), (6.49), (6.50), prospetti 6.1 e 6.2. Edizione e Annesso Nazionale **dichiarati**, senza passaggi impliciti alle edizioni 2022 o NTC2018.
- Profili **laminati a caldo IPE, HEA, HEB** tabellati nel catalogo OPPO M88-B2 (valori indicativi, non certificazione dei prodotti), acciai S235, S275, S355, sezione in **classe 1, 2 o 3** sotto compressione uniforme. La classe dichiarata deve corrispondere alla classificazione geometrica automatica M90-A; la classe 4 viene esclusa.
- **Sola compressione concentrica uniforme lungo l'asta**, N<0, V=0 e M=0 con una stazione FEM identificata (N,V,M con provenienza completa). La costanza di N e il carattere prismatico devono essere confermati dall'utilizzatore, **non inferiti da una sola sezione**.
- Le lunghezze **efficaci di libera inflessione** Lcr,y e Lcr,z devono essere determinate e motivate separatamente. I campi opzionali geometrici UnbracedLength del contesto FEM **non vengono usati come Lcr**. È indispensabile una verifica indipendente di rigidezza/resistenza del sistema di controventatura.
- Coefficiente `gamma_M1` e modulo E (MPa) espliciti e con fonte. `gamma_M0` non viene mai usato in sostituzione di `gamma_M1`.

## Calcolo (due assi principali)

Per ogni asse j ∈ {y,z}:

```text
Ncr,j [kN] = π² * E[MPa] * Ij[cm⁴] * 1e-5 / Lcr,j[m]²
Ny [kN]   = A[cm²] * fy[MPa] * 0.1
lambdaBar,j = sqrt(Ny / Ncr,j)
phi,j = 0.5 * [1 + alpha_j * (lambdaBar,j - 0.2) + lambdaBar,j²]
chi_j = min(1, 1/[phi,j + sqrt(phi,j² - lambdaBar,j²)])
Nb,Rd,j = chi_j * Ny / gamma_M1
Nb,Rd,flexural = min(Nb,Rd,y, Nb,Rd,z)
utilization = abs(N_Ed) / Nb,Rd,flexural
```

I fattori alfa da prospetto 6.1: a0 0,13; a 0,21; b 0,34; c 0,49; d 0,76. Le curve sono **determinate** da §6.3.1.2, tabella 6.2 sulla base di h/b, spessore dell'ala, tipologia laminata e classe acciaio; non sono selezionabili arbitrariamente dall'utilizzatore. La serie INP con ali inclinate rimane esclusa.

Il superamento della prova flessionale su entrambi gli assi **non prova** che l'asta sia sicura rispetto a instabilità torsionale o flesso-torsionale, interazione con momenti, snellezza locale, imperfezioni globali, collegamenti o applicabilità normativa complessiva. Tali verifiche dovranno essere richieste separatamente: se manca una regola, il `DesignCheckEngine` restituisce `Incomplete`.

## Benchmark indipendente di riferimento: Bernuzzi, A7.1

Claudio Bernuzzi, *Progetto e verifica delle strutture in acciaio secondo le Norme Tecniche per le Costruzioni 2018 e l'Eurocodice 3*, II ed., Hoepli 2018, applicazione A7.1, pp. 192–193. Libro fornito dall'utilizzatore; **non incluso nel progetto o nello ZIP**.

- HEB 200, S235; A=78,1 cm² nel testo, Iy=5696 cm⁴, Iz=2003 cm⁴, E=210000 MPa.
- Lcr,y=7,5 m; Lcr,z=3,75 m (controvento intermedio). Curve: y-y **b**, z-z **c**.
- Ncr,y=2098,78 kN; Ncr,z=2952,1 kN.
- lambdaBar,y≈0,935; lambdaBar,z≈0,788; chi_y≈0,6387; chi_z≈0,6693.
- Resistenza governante riportata ≈ **1116,1 kN** con coefficiente parziale esplicito del caso di test `gamma_M1=1,05`. La tabella OPPO riporta A=78,08 cm², pertanto la differenza di centesimi/arrotondamenti è ammessa. Il calcolo impiega la formula analitica di chi, mentre il libro interpola una tabella.

Questa prova verifica la scelta dell'asse governante e la conversione corretta delle unità. Non costituisce convalida universale delle condizioni di vincolo o dell'efficacia reale del controvento.

## Quali casi restituiscono non applicabile/incompleto

- Classe 4, profili INP, trazione, flessione o taglio non nulli: `NotApplicable`.
- Classe a compressione contraddittoria rispetto a M90-A, mancanza di uno dei due Lcr, E, gamma_M1, fonti o giustificazioni: `MissingData`.
- Edizione/Annesso Nazionale non coincidenti: `Unsupported` dal motore di verifiche.
- Valori nulli/non finiti o test fuori dall'ambito numerico: mai `Passed`.

## Prossimi step

**M91-B:** relazione fra controventi/condizioni di estremità e lunghezze efficaci documentate, più approfondimento della validazione. **M91-C:** instabilità flesso-torsionale e instabilità laterale di travi, dove tecnicamente e normativamente compatibile con un'analisi 2D supportata da dati esterni. In seguito: verifiche legno M92–M94, SLE e report M95–M99, GUI Avalonia soltanto alla fine.

## Fonti

- EN 1993-1-1:2005, §§6.3.1.1–6.3.1.3, tabelle 6.1 e 6.2 (documento normativo da applicare nella versione effettivamente pertinente e con Annesso Nazionale valido).
- Bernuzzi, Hoepli 2018, A7.1, pp. 192–193, confronto svolto su dati tabellati, non redistribuito.
- OPPO, cataloghi IPE/HEA/HEB, M88-B2 (dati indicativi).
