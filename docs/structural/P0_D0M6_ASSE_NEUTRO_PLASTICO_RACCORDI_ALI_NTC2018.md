# P0-D0M6 — Asse neutro plastico nominale anche nei raccordi e nelle ali

**Data:** 25 ottobre 2026  
**Stato:** codice e test xUnit aggiunti; suite .NET **da eseguire**; 63/63 nuovi controlli geometrici indipendenti **PASS**.  
**Conferma dell'autrice:** tutti i test del checkpoint precedente **P0-D0M5** e delle regressioni eseguite su Windows risultano **PASS** (conferma verbale, senza log numerico allegato).

## Esito concreto

È ora disponibile il servizio `Ntc2018SteelPlasticNeutralAxisCalculatorD0M6`, distinto da D0M4 e dal classificatore D0M5. Per un profilo I/H **laminato a caldo, ad ali parallele, doppiamente simmetrico, non forato**, determina la posizione dell'asse neutro plastico **anche quando non appartiene più al tratto rettilineo dell'anima**. Lo colloca esplicitamente in uno di cinque domini:

1. ala dal lato compresso;
2. raccordo dal lato compresso;
3. anima piatta;
4. raccordo dal lato teso;
5. ala dal lato teso.

Il carico proviene da **una stessa stazione FEM** con `N` e `M` forte entrambi non nulli, `V=0`, identificativi di membro/analisi/combinazione/stazione/lato documentati. Si usa la convenzione FEM già esistente: **N positivo è trazione**, N negativo compressione. Il momento determina l'orientazione del lato compresso, ma **non** viene confuso con il momento plastico di questo blocco ideale.

## Geometria dichiarata e controllata

Altezza `h`, larghezza ala `b`, spessore anima `tw`, spessore ala `tf`, raggio del raccordo `r`, tutte in mm. Il raccordo è modellato mediante **arco circolare nominale concavo di un quarto di cerchio**; non viene assunto che una forma commerciale reale abbia tolleranze di fabbricazione esattamente uguali a questo modello.

L'area lorda nominale risultante è

\[
A_{nom}=2bt_f+t_w(h-2t_f)+4r^2\left(1-\frac{\pi}{4}\right).
\]

Quest'ultima componente, assente nelle sole strisce rettangolari, rappresenta i quattro raccordi. **Non** è una nuova formula normativa di resistenza: è un'integrazione geometrica dell'ipotesi dichiarata. La conformità del modello è verificata confrontando `A_nom` con la **separata** area pubblicata nel catalogo. Se la differenza relativa supera **0,1%**, il servizio respinge il profilo invece di estrapolare; la soglia considera soltanto l'arrotondamento della tabella e **non rappresenta una tolleranza di prodotto ammessa dalla norma**.

Il controllo sull'intero catalogo locale dà **30/30 profili IPE, HEA, HEB compatibili**, con massimo scarto dell'area pari a circa **0,04451% (IPE 80)**. I dati OPPO rimangono *indicativi, non certificati*; profili INP a ali inclinate, saldati, asimmetrici, forati e senza geometria verificabile restano fuori dominio.

## Equilibrio plastico risolto

Definendo `d` come distanza dell'asse neutro dal bordo della sezione **dal lato compresso** e `A_c(d)` come area nominale cumulata fino a tale profondità, vale

\[
A_c(d)=\frac{A_{nom}-1000N/f_{yk}}{2}.
\]

La funzione `A_c(d)` è integrata nei tratti rettangolari e nei raccordi circolari; la sua inversa è ricercata con bisezione monotona, confrontando il residuo di equilibrio e respingendo stati fuori dal limite plastico assiale lordo `|N| < fyk·A_nom/1000`.

Nel tratto piatto, D0M6 confronta automaticamente l'α calcolato con **l'espressione esatta D0M4**:

\[
\alpha=\frac12-\frac{1000N}{2 f_{yk}t_wc},\qquad c=h-2(t_f+r).
\]

Al di fuori del tratto piatto l'α geometrico dell'anima è indicato convenzionalmente come **0** (anima non compressa dal blocco) oppure **1** (intera anima compressa dal blocco). **Questi estremi sono risultati diagnostici, non valori automaticamente ammessi nel classificatore D0M1/D0M5**, che richiede `0<α<1` e altre condizioni su ψ. D0M5 resta intenzionalmente invariato e continua a rifiutare queste situazioni.

## Esempio indipendente: IPE 200, S235

Il benchmark Python integra le strisce dei raccordi mediante quadratura angolare, diversa dalla primitiva circolare implementata in C#.

| N [kN] | Profondità asse neutro d [mm] | Posizione | α diagnostico |
|---:|---:|---|---:|
| −300 | 192,14093 | Ala dal lato teso | 1 |
| −220 | 183,30835 | Raccordo dal lato teso | 1 |
| −100 | Circa 137,99392 | Anima piatta | D0M4 |
| +100 | Circa 62,00608 | Anima piatta | D0M4 |
| +220 | 16,69165 | Raccordo dal lato compresso | 0 |
| +300 | 7,85907 | Ala dal lato compresso | 0 |

N positivo tende a spostare l'asse plastico **verso il bordo compresso**; con N negativo si sposta **verso il bordo teso**. Gli esempi positivo/negativo dello stesso valore sono speculari perché la geometria è doppiamente simmetrica. L'esempio non dimostra la sufficienza della sezione ai momenti applicati.

## Test e regressioni

- **63/63** nuovi golden numerici/geometrici indipendenti **PASS**: 30 geometrie da catalogo, equilibrio numerico, raccordi, flange, casi speculari, continuità al limite dell'anima e capacità assiale nominale.
- Suite precedenti R2–D0M5: **279/279** controlli aritmetici indipendenti, da rieseguire se mutano le formule pregresse. Totale teorico dopo regressione: **342** controlli.
- Test xUnit D0M6 predisposti: parità D0M4, profondità indipendenti, IPE/HEA/HEB, inversione del momento, `Unsupported` per INP/campo assiale/geometrie incoerenti/mancanza prova, e prova che D0M5 **non** estende la classe senza fondamento normativo.
- D0M6 **non compilato né testato in .NET nell'ambiente di generazione**, privo dell'SDK. La conferma Windows dell'autrice riguarda il checkpoint **D0M5**, non il nuovo D0M6.

Per eseguire la verifica su Windows dalla radice del progetto:

```powershell
dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj --filter FullyQualifiedName~Ntc2018SteelPlasticNeutralAxisD0M6Tests
dotnet test StructuralSolver2D.sln
```

Verificare il nome effettivo della soluzione se diverso nella cartella radice; per la regressione completa resta valido lo script di test del progetto.

## Non coperto e roadmap

**Il D0M6 non attribuisce una classe normativa 1–4 agli stati con asse plastico fuori dall'anima piatta, non calcola Mpl o le verifiche N/M/V, non produce proprietà efficaci di classe 4, non controlla instabilità flesso-torsionale, ritegni, nodi o l'insieme delle stazioni/azioni.** Le verifiche NTC restano distinte dalle regole precedenti EC3. La geometria teorica non sostituisce il certificato di laminazione, i dati del costruttore o le dimensioni effettive dell'elemento.

Fonti: **DM 17 gennaio 2018**, NTC 2018 §4.2.3.1 e Tabella 4.2.III/IV (classe dipendente dalla distribuzione di tensioni e dal rapporto c/t); tabelle nominali [OPPO IPE](https://www.oppo.it/tabelle/profilati_ipe.htm), [HEA](https://www.oppo.it/tabelle/profilati_hea.htm), [HEB](https://www.oppo.it/tabelle/profilati_heb.htm). Il manuale Bernuzzi (2018), cap. 4, rimane riferimento didattico, non sostituisce il testo normativo.

**Dopo il gate D0M6:** P0-D0M7 valuterà i rami di classificazione normativamente applicabili alle distribuzioni *effettive* quando l'asse plastico cade in raccordi/ali, senza usare acriticamente α=0/1 nelle formule miste; in parallelo, P0-D2 resistenza/interazioni N/M/V, P0-E precondizioni fisiche fuori piano, P0-C3/C4 completezza obblighi e report, P0-D3 SLE acciaio. **La GUI Avalonia rimane P3, ultimo modulo.**
