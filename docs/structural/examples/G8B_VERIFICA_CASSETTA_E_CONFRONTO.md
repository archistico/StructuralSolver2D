# G8B — Confronto di casetta a 2 piani: controventi e sezioni

**Stato al 25/10/2026:** esempio didattico e riferimento elastico indipendente. **F1.1-A** ha introdotto il confronto automatico in **C#/.NET** sui 16 scenari ([rapporto F1A](../F1_1A_G8B_VALIDAZIONE_CSHARP_16_SCENARI.md)) con **PASS riferito dall'autrice**. **F1.1-B** ha aggiunto le formule analitiche dei balconi e controlli di equilibrio ([rapporto F1B](../F1_1B_G8B_SOLLECITAZIONI_EQUILIBRIO_CSHARP.md)), **PASS riferito dall'autrice**. **F1.1-C** ha introdotto il secondo risolutore C# ([rapporto F1C](../F1_1C_G8B_RIFERIMENTO_VARIAZIONALE_CSHARP.md)), anch'esso **PASS riferito dall'autrice**. **F1.1-D** prepara [un esportatore C# del golden N/V/M completo](../F1_1D_G8B_ESPORTAZIONE_GOLDEN_NVM_CSHARP.md), **non ancora eseguito**. Il confronto non implica che l'analisi sia normativa o che le sezioni siano sufficienti. I passaggi storici seguenti descrivono l'origine del benchmark e non autorizzano nuovo Python.

## 1. Obiettivo e geometria

Confrontare un telaio metallico bidimensionale di edificio a due piani con travi di copertura in GL24h. Schema: x pilastri = 0–5–7–12 m, livelli y = 0–3–6 m, colmo y = 8 m alla coordinata x = 6 m. Due balconi a quota +3,0 m, lunghi 1,20 m, alle estremità. Fascia di influenza dei carichi superficiali = 4,0 m.

La campata centrale x = 5–7 m (corridoio) è libera. I controventi a X sono collocati nella **campata x = 0–5 m**, al piano inferiore e superiore, nelle tre alternative controventate. Questo configura una variante in cui lo spazio della campata sinistra accoglie una linea di controvento compatibile con l'architettura.

## 2. Sezioni candidate

| Serie | Colonne | Travi 5 m | Corridoio e balconi | Copertura | X |
|---|---|---|---|---|---|
| B senza X | HEB 220 | IPE 330 | IPE 220 | GL24h 160×360 | No |
| A leggera | HEB 200 | IPE 300 | IPE 200 | GL24h 140×320 | Sì |
| B riferimento | HEB 220 | IPE 330 | IPE 220 | GL24h 160×360 | Sì |
| C robusta | HEB 240 | IPE 360 | IPE 240 | GL24h 180×400 | Sì |

Gli input FEM rappresentano le **proprietà elastiche forti** delle sezioni: area A e momento d'inerzia I; la verifica normativamente completa richiede anche modulo resistente, classe, stato di tensione, materiale e proprietà deboli/torsionali per la stabilità, ritegni, collegamenti e azioni combinate. I profili HEB e IPE 200/220/240 derivano dal catalogo M88 a valori indicativi; le grandezze delle IPE 300/330/360 sono **valori nominali preliminari da confermare dal catalogo di fornitura**, non autorizzazioni al dimensionamento. La sezione rettangolare delle travi GL24h è calcolata per geometria.

Ogni X ha sezione astratta A = 1500 mm², materiale acciaio, tipo Truss2D: **bilaterale**, ossia funziona elastically in compressione e trazione. Gli incroci NON sono nodi. Non rappresenta aste tese unicamente, instabilità delle barre compresse, connessioni o piastre di nodo. L'aggiunta di controventi cambia le condizioni di trasmissione dei carichi alla fondazione.

## 3. Carichi adottati, trasformazioni 4 m e peso proprio

I coefficienti sono **ipotetici e soltanto didattici** (non derivati dalla quota/località, categoria d'uso normativa o coefficienti aerodinamici reali). Su ciascuna trave di piano: permanente 5,5 kN/m² ×4 = 22 kN/m; variabile camere 2,0×4 = 8 kN/m; variabile corridoio 3,0×4 =12 kN/m. Su ciascun balcone: permanente 6×4 =24 kN/m; variabile 4×4=16 kN/m. Sui due lati del tetto: permanente/neve 1,5×4×cos(arctan(2/6))≈5,69 kN/m, arrotondato a **5,70 kN/m** su ciascuna falda; per un carico verticale ripartito lungo la falda va considerata la *lunghezza inclinata*. I venti laterali sono un sistema illustrativo di forze nodali orizzontali (+44 kN totali nel caso positivo); il sollevamento del tetto è un carico verticale positivo distinto.

Il **peso proprio reale degli elementi FEM** viene ora caricato come `SelfWeight` nel caso `G_SELF`. La generazione non è implicita: `unitWeight` nei materiali non basta senza il carico esplicito. I permanenti dei pacchetti (solai, balconi, copertura) **escludono** il peso delle aste FEM, così da non duplicarlo.

La trasformazione delle azioni e la loro attribuzione a una singola sezione piana dipendono dal sistema portante ortogonale e dai diaframmi, che qui sono ipotizzati. Nessun solaio 3D è modellato.

## 4. Risultanti indipendenti dei casi di carico

| Caso | Fx (kN) | Fy (kN) | Significato |
|---|---:|---:|---|
| G_FLOOR | 0 | −528,0 | due livelli, 12 m ciascuno |
| Q_ROOM | 0 | −160,0 | 2 travi di 5 m per piano |
| Q_CORR | 0 | −48,0 | due tratti corridoio di 2 m |
| G_BALC | 0 | −57,6 | due sbalzi da 1,20 m |
| Q_BALC | 0 | −38,4 | due sbalzi da 1,20 m |
| G_ROOF | 0 | −72,10 | due falde da √40 m, q = 5,70 kN/m |
| SNOW | 0 | −72,10 | stesso carico didattico |
| WIND_POS | +44,0 | 0 | vento +X |
| WIND_NEG | −44,0 | 0 | vento −X |
| WIND_UP | 0 | +31,62 | sollevamento didattico della copertura |

I pesi propri calcolati esclusivamente da A×lunghezza×γ sono: **A 29,884 kN**, **B con X 34,682 kN**, **C 40,194 kN**, **B senza X 31,935 kN**. Le somme dei vincoli devono equilibrare queste forze *dopo* l'applicazione dei coefficienti della combinazione considerata.

## 5. Confronto elastico indipendente, NON un risultato del CLI

Nella fase storica G8B era stata realizzata una seconda implementazione minima in Python/NumPy (`benchmarks/independent/g8b-house/independent_frame2d_reference.py`), separata dal motore C#. Il codice storico è **congelato** e non deve essere eseguito o modificato per i prossimi gate. I risultati JSON già pubblicati costituiscono il **riferimento numerico esplorativo indipendente e statico** usato ora dai test C#; non è un benchmark normativo certificato.

Con le combinazioni **illustrative manuali** `ULS_WIND_POS`, il risultato sullo **spostamento orizzontale del colmo** è:

| Variante | Ux colmo, mm | Ux massimo assoluto del modello, mm |
|---|---:|---:|
| B senza X | +7,0403 | 7,1365 |
| A con X | +1,5850 | 1,7501 |
| B con X | +1,4475 | 1,5966 |
| C con X | +1,3138 | 1,4454 |

**Attenzione alla comparazione:** `ULS_WIND_POS` combina nel medesimo scenario anche carichi verticali con fattori illustrativi, incluso peso proprio variabile per sezione; per valutare lo *spostamento dovuto al solo vento* conviene eseguire anche il solo `WIND_POS`. Le due varianti B conservano lo stesso insieme di sezioni del telaio principale, salvo le quattro diagonali aggiunte. In quel confronto l'aggiunta della X riduce lo spostamento orizzontale elastico massimo calcolato indipendentemente da **7,1365 a 1,5966 mm**, ossia circa il **77,6%**. È una proprietà del modello ideale, **non una garanzia del comportamento reale** né un giudizio sullo stato limite di spostamento.

Il file `benchmarks/independent/g8b-house/g8b-risultati-elastici-indipendenti.json` contiene per **ogni modello e combinazione**: spostamenti di piano e colmo, massimi globali, carichi globali, reazioni vincolari e residui di equilibrio. Il residuo massimo misurato nel calcolo indipendente è numericamente trascurabile (ordine 10⁻¹¹ kN/kNm), ma l'equilibrio da solo non dimostra l'accuratezza di tutte le sollecitazioni interne.

## 6. Come eseguire con StructuralSolver2D

```powershell
.\scripts\verify-g7-manifest.ps1
.\scripts\run-g8b-house-demo.ps1
```

Lo script prova quattro modelli × quattro combinazioni e genera **16 report Markdown** più **due viewer HTML** (`reports/g8b-house/`), affinché sia immediato confrontare le deformate e le sollecitazioni della variante B con/senza X. Se un caso fallisce, lo script si arresta sul comando interessato. Per validare anche l'intera regressione precedente:

```powershell
.\scripts\run-g7-tests.ps1
```

Il test F1A confronta i **16 scenari** in C# per spostamenti, reazioni, equilibrio e alcuni N/V/M delle colonne inferiori (**PASS riferito dall'autrice**). F1B aggiunge attesi N/V/M analitici delle due mensole, oltre a equilibri e identità delle altre aste (**PASS riferito dall'autrice**). F1C aggiunge un risolutore indipendente variazionale per confrontare tutti i nodi e N/V/M delle aste (**PASS riferito dall'autrice**). **Il gate G8B completo non è ancora chiuso**: F1D deve esportare in C# il nuovo golden statico, farne rivedere i valori e l'hash e consentire controlli di viewer/report.

## 7. Dimensionamento progressivo richiesto

L'ordine proposto dopo il confronto FEM è:

1. Travi solai 5 m (F1/F2_AB, F1/F2_CD): M, V, N, frecce, classificazione EC3/NTC acciaio, stabilità flesso-torsionale, vincoli laterali e collegamenti.
2. Corridoio 2 m: M, V, frecce, appoggi e continuità con gli altri tratti.
3. Balconi 1,20 m: momento negativo alla radice, taglio e freccia alla punta, parapetti, stabilità locale, torsione fuori piano non risolta da 2D, dettaglio del nodo di incastro.
4. Colonne ai 4 assi x=0/5/7/12 (entrambi i piani): N/V/M concomitanti per tutte le combinazioni, snellezze, lunghezze efficaci lungo due assi, imperfezioni, secondo ordine e controventi.
5. Diagonali: N positivo/negativo, instabilità a compressione se necessaria, connessioni e trasferimenti alle fondazioni.
6. Elementi di copertura GL24h: M/N/V, interazioni, instabilità laterale, SLE differite, `kmod`, `kdef`, neve/vento locali, ritegni e connessioni alla carpenteria.

**Nessuna delle sezioni è stata dichiarata verificata o definitiva.** Mancano le effettive caratteristiche geometriche complete dei profili, ubicazione per neve/vento, destinazioni d'uso precise, schemi di solaio e di appoggio, dati dei ritegni e connessioni, sisma e scelta di tutte le combinazioni normative. La generazione automatica delle combinazioni è tuttora limitata. Le regole legno G1–G6 e acciaio M89–M91 sono parziali; la conformità globale non può essere derivata da questi quattro file.


## 8. Correzione G8B FIX1 — distinzione modelli / benchmark

Durante il collaudo Windows originale di G8B, i 2 test CLI falliti analizzavano per errore i file di dati indipendenti come modelli FEM: `g8b-controlli-analitici.json` e `g8b-risultati-elastici-indipendenti.json`. Compilazione Release e test FEM/verifiche precedenti erano superati. Non erano difetti del solver.

**Correzione:** entrambi i JSON e il CSV di confronto risiedono ora in `benchmarks/independent/g8b-house/`; il generatore Python scrive lì. In `examples/mixed/g8b-house/` rimangono solo i **4 JSON strutturali**. I 4 modelli, i sorgenti .NET, le verifiche normative e i test C# rimangono byte-per-byte identici a G8B. Eseguire `scripts/run-g7-tests.ps1` per riconfermare il test CLI e tutte le regressioni. Non dichiarare G8B FIX1 superato senza il log Windows.


## Aggiornamento G8B FIX2 — estrazioni sovrapposte

Il primo G8B distribuiva due file di **risultati/controlli numerici**, non modelli FEM, in `examples/mixed/g8b-house/`. FIX1 li ha trasferiti in `benchmarks/independent/g8b-house/`, ma un'estrazione sovrapposta non cancella i vecchi file: la suite CLI li trova ancora. FIX2 esegue automaticamente `scripts/repair-g8b-legacy-examples.ps1` all'inizio di `run-g7-tests.ps1`, **prima** della compilazione.

La riparazione accetta solo i due nomi noti e verifica che il JSON sia di riferimento (`models`, senza `nodes`/`members`); non elimina i file ma li archivia in `.g8b-legacy-backup/`. In caso di struttura diversa interrompe l'esecuzione. Non altera alcun modello strutturale o benchmark.
