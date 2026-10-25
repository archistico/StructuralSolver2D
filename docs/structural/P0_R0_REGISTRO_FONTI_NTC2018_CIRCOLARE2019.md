# P0-R0 — Registro di confronto con NTC 2018 e Circolare 7/2019

**25 ottobre 2026 — primo confronto diretto dei PDF forniti, sola documentazione.**
Il rapporto **non** promuove nuovi gate .NET e **non** attesta che l'intero
progetto sia conforme. Restano **PENDING** i test Windows C2B, C2C0, C2C1.

## Identità e precedenza delle fonti

1. **NTC 2018:** D.M. 17 gennaio 2018, pubblicato nella *Gazzetta Ufficiale*
   n. 42 del 20 febbraio 2018, S.O. n. 8. PDF consegnato dall'utente:
   `ntc2018(1).pdf`, 372 pagine, SHA-256
   `34cdd17fba656ee986fa13ec2f02ad48454628f46da3c468a7743e3a8903bbc5`.
   [Pubblicazione ufficiale](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf).
2. **Circolare esplicativa:** Circolare 21 gennaio 2019 n. 7 C.S.LL.PP.,
   pubblicata nella *Gazzetta Ufficiale* n. 35 dell'11 febbraio 2019,
   S.O. n. 5. PDF consegnato dall'utente:
   `circolare-ntc-2018(1).pdf`, 341 pagine, SHA-256
   `d8c4985019714df67677b73aaa9106330b6ce49c194e81add138220793c16620`.
   [Pubblicazione ufficiale](https://www.gazzettaufficiale.it/eli/id/2019/02/11/19A00855/sg).

La fonte prescrittiva nazionale per il profilo software `NTC2018` è il
**decreto**, con la Circolare quale testo applicativo e di chiarimento.
Il software deve citare **clausola, tabella/equazione, edizione, ipotesi e
test**; nessuna formula viene approvata in base alla sola somiglianza dei
simboli. Questo registro fissa soltanto le edizioni dei **due documenti
allegati**, senza affermare di aver censito tutte le modifiche legislative
successive o tutti gli annessi nazionali applicabili.

**Separazione obbligatoria:** i moduli acciaio `Verification.Steel` M89–M91
si dichiarano **EN 1993-1-1:2005+A1:2014**, non NTC2018; i moduli legno
M92 si dichiarano **EN 1995-1-1:2004+A1:2008+A2:2014**, non NTC2018.
I due PDF italiani **non sono il testo integrale** degli Eurocodici e non
permettono di chiuderne l'audit autonomo. Gli esempi e i valori EC3/EC5
restano separati finché non sono confrontati con le edizioni EN e gli annessi
esplicitamente scelti.

## Stati di questo registro

- **C — Coincidenza puntuale confermata:** valori/testo letti nei PDF e
  confrontati direttamente con costanti/formule **effettive nel codice**.
  Non è una validazione del flusso end-to-end o delle ipotesi fisiche.
- **P — Implementazione parziale:** regola o sottoinsieme esistente, ma
  almeno una precondizione, formula, esclusione o prova indipendente richiede
  ancora audit dedicato.
- **G — Gap dichiarato:** funzione non supportata o distinta, non autorizza
  alcun `Passed` di asta/opera.
- **D — Da esaminare:** non controllato direttamente in questa tornata.

## Confronti effettivamente svolti sui testi

| ID | Fonte NTC/Circolare | Riscontro diretto | Codice e test di riferimento | Stato e limite |
|---|---|---|---|---|
| R01 | NTC §2.5.3, Tab. 2.5.I | Psi `A/B=.7/.5/.3`, `C/D=.7/.7/.6`, `E=1/.9/.8`, `F=.7/.7/.6`, `G=.7/.5/.3`, `H=0/0/0`, vento `.6/.2/0`, neve ≤1000 m `.5/.2/0`, neve >1000 m `.7/.5/.2`, termiche `.6/.5/0` | `Combinations/Generation/Ntc2018BuildingActionCoefficients.cs`; test B3 | **C** per valori di tutte le **12 righe codificate**; categorie I/K da valutare caso per caso e quindi non compilate automaticamente |
| R02 | NTC §2.6.1, Tab. 2.6.I, colonna STR A1 | G1 fav./sfav. `1,0/1,3`, G2 `0,8/1,5`, Q `0/1,5`; eccezione documentabile G2 noto | `Ntc2018BuildingActionCoefficients.GammaStrA1`; B1/B3 | **C** per valori e condizione testuale; **P** per tutte le situazioni strutturali: A2/EQU/GEO e casi geotecnici fuori perimetro |
| R03 | NTC §2.5.3, Eq. 2.5.1–2.5.4 | SLU fondamentale, SLE rara, frequente e quasi permanente con guide e accompagnatrici; carichi favorevoli e omissioni secondo effetto | `Ntc2018BuildingCombinationGenerator`, P0-B2/B3/B4 | **C** per identificazione delle quattro famiglie; **P** per completezza autonoma di scenari, alternative e azioni fisiche: B4 audita soltanto un piano dichiarato |
| R04 | NTC §4.4.6, Eq. 4.4.1 | `Xd = kmod · Xk / gammaM`; dipende da servizio e durata | `Verification.Timber/Ntc2018TimberCoefficientSelectorG1`, G2 | **C** per relazione e uso dichiarato, **P** per validazione delle caratteristiche reali del prodotto |
| R05 | NTC §4.4.6, Tab. 4.4.III | GammaM fondamentali colonna A: legno massiccio `1,50`, lamellare `1,45`; colonna B `1,45` e `1,35`; situazioni eccezionali `1,0` | `Ntc2018TimberCoefficientTablesG1`, G1/G7 | **C** per le due famiglie ammesse; colonna B è subordinata alle evidenze di produzione/controllo e coefficiente di variazione non superiore al **15%**; non è un'opzione libera |
| R06 | NTC §4.4.6, Tab. 4.4.IV | Kmod legno massiccio/lamellare: classi servizio 1–2 `0,60/0,70/0,80/0,90/1,10`; classe 3 `0,50/0,55/0,65/0,70/0,90` (permanente→istantanea) | `Ntc2018TimberCoefficientTablesG1` | **C** per le 6 righe esplicite usate; **G** per altre famiglie di legno/pannelli non ricomprese in G1 |
| R07 | NTC §4.4.6, frase sulle combinazioni con più durate | Kmod riferito all'azione di durata **minore** nella combinazione | `Ntc2018TimberCoefficientSelectorG1`, manifesto | **C** per la regola dichiarata; **P** per autenticazione automatica dell'intera lista di azioni, distinta dal fingerprint dichiarato |
| R08 | NTC §4.4.8.1.1, Eq. 4.4.2 | Trazione parallela verificata sulla **sezione netta**; eventuale fattore `kh` e giunti da considerare | `Ntc2018TimberRectangularSectionRuleG2`, `VerifiedNetTensionAreaMm2` | **C** per area netta richiesta nel codice; `kh=1` deliberatamente non premiante e con evidenza esterna; **P** per giunti/eccentricità |
| R09 | NTC §4.4.8.1.3, Eq. 4.4.3 e §4.4.8.2.2 | Compressione parallela di sezione **e** ulteriore verifica di stabilità per elemento compresso | G2 compressione + G4 instabilità | **C** per distinzione delle due obbligazioni; **P**: un `Passed` G2 da solo non verifica la colonna |
| R10 | NTC §4.4.8.1.6, Eq. 4.4.5a–b e §4.4.8.2.1 | Flessione, possibile rapporto `km=0,7` rettangolare, anche stabilità laterale | G2 flessione + G4 | **P**: la famiglia G2 opera in forte asse e azione pura; non dichiara tutti gli assi né instabilità risolta automaticamente |
| R11 | NTC §4.4.8.1.9, Eq. 4.4.8; Circolare **C4.4.8.1.9** | Taglio Jourawski con larghezza ridotta per fessurazioni; `kcr=2,0/fv,k` legno massiccio e `2,5/fv,k` lamellare (`fv,k` in MPa) | `Ntc2018TimberCoefficientSelectorG1`, `Ntc2018TimberRectangularSectionRuleG2` | **C** per le formule dei due coefficienti nel codice; **P** per intagli, taglio/torsione e dettagli fisici esclusi |
| R12 | NTC §4.2.4.1.1, **Tab. 4.2.VII** | Per acciaio NTC: `gammaM0=1,05`, `gammaM1=1,05` (membrature ordinarie), `gammaM2=1,25`; ponti distinti | `Verification.Timber/Ntc2018DesignFactorsG1.cs` contiene **già** queste costanti NTC isolate; invece le regole `Verification.Steel` M89–M91 hanno identità **EC3**, con `gammaM0` da profilo/annesso esplicito | **C** per costanti NTC registrate; **G importante** per la loro integrazione in vere verifiche acciaio **NTC2018**, ancora assenti: C2C1 con `gammaM0=1` è **solo** test EC3, mai una verifica NTC italiana |
| R13 | NTC §4.2.4.1.2.1, Eq. 4.2.5–4.2.8 | Trazione di sezioni forate: confronto con **minore** tra snervamento lordo `A·fyk/gammaM0` e rottura netta `0,9·Anet·ftk/gammaM2`, più altre condizioni | `SteelGrossTensionYieldRuleM89B` e pilota C2C1 verificano **solo** snervamento lordo EC3 | **G critico** per verifica NTC dell'asta: manca il ramo frattura netta, prove Anet/ftk, collegamenti; un esito C2C1 ≤1 non è una verifica di trazione NTC |
| R14 | NTC §4.4.8.2.1, Eq. 4.4.11–4.4.12 | Instabilità di trave: `lambda_rel,m=sqrt(fm,k/sigma_m,crit)`; `kcrit,m=1` fino a `0,75`, `1,56−0,75 lambda` fino a `1,4`, poi `1/lambda²` | `Ntc2018TimberStabilityRulesG4.BeamReduction`, ramo `StrongAxisBendingBeam` | **C** per algoritmo a tratti e utilizzo `sigma_m,d/(kcrit,m fm,d)`; **P** per effettiva stima `sigma_m,crit` al frattile 5%, segmenti fra ritegni, massimo M, rettilineità e prove esterne non generate dal FEM |
| R15 | NTC §4.4.8.2.2, Eq. 4.4.13–4.4.16 | Colonna: `lambda_rel=(Lcr/r)/pi · sqrt(fc,0,k/E0,05)`; `kcrit,c=1` fino a `0,3`, oltre formula con `k=0,5·[1+beta_c(lambda−0,3)+lambda²]`, `beta_c=0,2` massiccio o `0,1` lamellare | `Ntc2018TimberStabilityRulesG4.ColumnReduction`, ramo `PureAxialColumn` sui due assi | **C** per kernel di riduzione e minimo fra i due assi; **P** per scelta delle due lunghezze efficaci, imperfezioni, vincoli e compressione pura dell'intera asta, dichiarati esternamente e non certificati dal solver piano |
| R16 | NTC §4.4.8.1.7–1.9, Eq. 4.4.6a–b, 4.4.7a–b, 4.4.8 | Tensoflessione e pressoflessione: contributo assiale lineare in trazione, quadratico in compressione; `km=0,7` nella sezione rettangolare; taglio verificato con disuguaglianza distinta | `Ntc2018TimberCombinedSectionRulesG3`, rami `TensionBending`/`CompressionBending`/`BendingAndShear` | **C** per sola riduzione al **momento su asse forte** e per rapporto taglio separato; **P** per validazione dell'assenza di momento sull'altro asse, stabilità G4, netti, collegamenti e verifica continua delle stazioni: G3 non introduce una fittizia formula d'interazione N–V–M |

**Esito del confronto puntuale R01/R02/R05/R06/R11:** i **valori letti
direttamente dai PDF** coincidono con le tabelle/relazioni citate nei
sorgenti. Per le **formule e i domini** R03/R04/R07–R10/R12/R13 la
coincidenza indicata non equivale a chiusura di tutte le verifiche necessarie.
Il secondo confronto statico R14–R16 trova corrispondenza dei **kernel
algebrici** G3/G4 nel loro sottoinsieme dichiarato, **non** una validazione
dei dati esterni, della copertura delle azioni, né della membratura intera.
I gate storici Windows restano quelli riportati nell'handoff; questo
confronto è **statico**, senza compilazione C#.

## Controlli ancora necessari, ordinati per rischio

| Priorità | Area normativa | Lavoro concreto e test di accettazione |
|---|---|---|
| **Alta** | Distinzione **EC3 vs NTC acciaio** | Introdurre, in P0-D, identità e profilo NTC autonomi; test contro `gammaM0=1,05`, `gammaM1=1,05`, `gammaM2=1,25` con confronto ai paragrafi e tutte le precondizioni; impedire un fallback implicito alle regole EC3 |
| **Alta** | Trazione acciaio NTC | Coprire **entrambe** le resistenze pertinenti per la sezione netta/forata, con golden indipendenti e dati reali obbligatori; un test senza `Anet` o `ftk` deve risultare incompleto, non `Passed` |
| **Alta** | **G3** legno presso/tensoflessione e taglio | Dopo il confronto base R16, approfondire domini, casi ai limiti, altre azioni e verifiche contemporanee; controprove di assi, segni, `km` e `MissingData`: nessun `Passed` di asta |
| **Alta** | **G4** legno stabilità | Dopo il confronto dei kernel R14–R15, audit indipendente di **derivazione e qualità delle prove fisiche** sui ritegni, sulle imperfezioni, sulle lunghezze efficaci e su `sigma_m,crit`; golden e casi di `MissingData` |
| **Alta** | Azioni e combinazioni | Allineamento B2/B3/B4 a §2.5.3–2.6.1 **per ogni effetto**, guida e alternanza vento/neve, carichi favorevoli, eccezioni, condizioni realmente più gravose; non dedurre completezza dai nomi dei casi |
| **Media** | **G5** SLE legno | Controllo §4.4.7 e C4.4, scorrimento delle connessioni, `kdef`, frequente/quasi permanente, frecce e sezioni realmente governanti; test separati |
| **Media** | **G1/G2** legno | Confronto degli ulteriori vincoli di materiale, umidità, giunti, coefficiente `kh`, netti, classi di durata, condizioni per colonna B e certificazioni prodotto, mantenendo `MissingData` se assenti |
| **Media** | **NTC acciaio** classi, flessione, taglio, interazioni e stabilità | Confronto §§4.2.3 e 4.2.4 voce per voce con tests/golden; niente assunzioni fuori piano dedotte da sola analisi 2D |
| **Da fonte aggiuntiva** | **EC3/EC5** specifici | Ottenere/consultare l'edizione EN effettivamente selezionata, relativi annessi nazionali e verifiche puntuali; la Circolare NTC non sostituisce i testi europei |

## Protocollo obbligatorio delle verifiche normative successive

Per **ogni nuova formula o modifica** creare o aggiornare una riga tracciabile:

1. Codice/edizione/annesso, fonte **primaria**, §/tabella/equazione;
2. natura del controllo: sezione, membratura, stabilità, collegamento o SLE;
3. materiale/profilo, classificazione, orientamento, unità, prove esterne;
4. N/V/M **concomitanti**, target/combinazione/coefficiente/favorevolezza,
   stazione/lato e caso fisico di applicabilità;
5. trascrizione algebrica con conversioni dimensionali controllate e **golden
   calcolato in maniera indipendente**;
6. **test positivi, limiti e negativi** (data missing, tensione vs
   compressione, classe non applicabile, segni, salti, stabilità, edizione);
7. esito `Passed` **solo nella verifica e nel dominio testati**;
   `Unsupported`, `MissingData`, `NotApplicable`, `Failed` distinti; nessuna
   promozione a idoneità dell'asta né dell'edificio senza l'audit C3;
8. gate C# confermato in Windows, riferimento indipendente, revisione
   documentale e regressioni FEM/G7 separati.

**Prossima applicazione pratica:** test C2B/C2C0/C2C1 quando l'utente potrà;
poi revisione normativa G3/G4 e progetto P0-C2C2 con le stesse regole di
tracciabilità. P0-E fisica prima di promettere verifiche strutturali complete;
interfaccia Avalonia solo P3.

**Prosecuzione P0-R1 (25/10/2026):** l'audit dettagliato di G5 e delle
precondizioni G1/G3/G4 è in
[P0_R1_AUDIT_SLE_LEGNO_E_PRECONDIZIONI.md](P0_R1_AUDIT_SLE_LEGNO_E_PRECONDIZIONI.md),
con 14 ulteriori riscontri e 12 golden aritmetici indipendenti. La nota
sulla durata minima della neve (§C4.4.4) aggiunge un gap di classificazione
da risolvere prima di dichiarare autonoma la selezione di `kmod`.
