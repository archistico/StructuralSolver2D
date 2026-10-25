# P0-D0M1 — classificazione NTC 2018 di anime I/H sotto N + M

**Stato al 25/10/2026:** implementazione circoscritta e prove aritmetiche indipendenti **26/26 PASS**; **compilazione e test xUnit .NET PENDING**. Non costituisce verifica di resistenza né conformità della membratura.

**Estensione successiva P0-D0M2:** il precedente rifiuto di `ψ≤−1` è stato studiato con la nota NTC e con un controllo fisico essenziale. Il ramo speciale **ψ<−1 richiede N di trazione positivo, α<0,5** ed evidenze addizionali sulle tensioni ai bordi: vedere [P0-D0M2](P0_D0M2_PSI_MENO_UNO_NTC2018.md). Il ramo originale D0M1 con **N<0 di compressione e ψ>−1 rimane invariato**; ψ=−1 esatto appartiene alla flessione pura D0. Le formule e i golden D0M1 qui sotto riguardano esclusivamente il ramo originale.

## Scopo preciso

Estensione **separata** da P0-D0 (sola compressione o sola flessione) e da M90-B (EC3). Classifica l'anima interna e l'ala sporgente di un **profilo I/H laminato a caldo, ad ali parallele, doppiamente simmetrico e privo di fori**, sottoposto **nella stessa sezione FEM** a compressione assiale firmata `N < 0` e momento sull'**asse forte** `M ≠ 0`, con `V = 0` nel perimetro pilota. La classe globale è il massimo delle due classi, non una capacità resistente.

Il classificatore usa geometria dichiarata oppure catalogo IPE/HEA/HEB; non include INP ad ali inclinate, saldati, forati, sezioni asimmetriche, debole asse, torsione, taglio concomitante, carichi puri o altro ambito 3D. Il **ramo originale D0M1** non include `ψ≤−1` senza le prove previste dall'estensione D0M2. Non deduce la validità di condizioni fuori piano dal solo FEM 2D.

## Fonte verificata direttamente

- **D.M. 17 gennaio 2018, NTC 2018**, §4.2.3.1, **Tab. 4.2.III** (parti interne sotto flessione e compressione, distribuzioni plastiche ed elastiche), **Tab. 4.2.IV** (parti sporgenti di laminati). Fonte ufficiale: [Gazzetta Ufficiale — NTC 2018, PDF](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf).
- Controllo visivo della tabella, compresi i confini `α > 0,5` e `α ≤ 0,5`, non semplice inferenza da EC3. Non si riutilizza l'identità normativa EN 1993.
- I valori numerici OPPO dei cataloghi commerciali sono **riferimenti geometrici indicativi, non certificati**: la geometria e il prodotto reali vanno documentati.

Convenzioni NTC: compressione del piatto positiva **nelle figure normative**; nella convenzione **FEM** di StructuralSolver2D la compressione assiale è **N negativo**. Si mantiene il segno FEM nel tracciato, senza alterare N per ottenere il rapporto geometrico.

## Formule implementate, senza interpolazioni arbitrarie

Si definiscono `ε = √(235/fyk)`, `c_w = h − 2 t_f − 2 r`, `c_f = (b − t_w − 2 r)/2`; i rapporti sono rispettivamente `c_w/t_w` e `c_f/t_f`. Tutte le lunghezze sono in millimetri, `fyk` in MPa.

`α` è la **frazione compressa della larghezza d'anima secondo distribuzione plastica**; `ψ` è il **rapporto fra tensioni longitudinali elastiche ai due bordi dell'anima**. Non sono lo stesso coefficiente: **α e ψ devono derivare da due distribuzioni di tensione indipendentemente giustificate, entrambe compatibili con il medesimo N/M, profilo, stazione e lato FEM**. Il software riceve i due valori e dichiarazioni/evidenze esterne; **non li calcola** né ne prova autonomamente la coerenza fisica.

| Classe | Anima se `α > 0,5` | Anima se `0 < α ≤ 0,5` |
|---|---|---|
| 1 | `c_w/t_w ≤ 396 ε/(13α − 1)` | `c_w/t_w ≤ 36 ε/α` |
| 2 | `c_w/t_w ≤ 456 ε/(13α − 1)` | `c_w/t_w ≤ 41,5 ε/α` |

Per la **classe 3**, nel solo sottocaso D0M1 `−1 < ψ < 1`, la Tab. 4.2.III prescrive `c_w/t_w ≤ 42 ε/(0,67 + 0,33ψ)`. La norma contiene un **secondo ramo ψ ≤ −1** con formula e condizioni proprie, **implementato successivamente in D0M2 dietro prova obbligatoria**. La classe 4 è restituita quando vengono superati i tre limiti del sottoinsieme supportato; **le proprietà efficaci Aeff/Weff non vengono calcolate**.

Per l'**ala sporgente compressa**, Tab. 4.2.IV: limiti di classe 1/2/3 `9 ε`, `10 ε`, `14 ε`. Se le tre soglie dell'anima risultano non ordinate (`classe 1 < classe 2 ≤ classe 3`), il risultato viene **rifiutato**: è necessario riesaminare le evidenze e il sottocaso normativo, senza assegnare arbitrariamente una classe.

## Contratto software e arresti sicuri

- Nuovi modelli: `Ntc2018SteelMixedStressParametersD0M1`, `Ntc2018SteelMixedClassificationEvidenceD0M1`, `Ntc2018SteelMixedIClassificationD0M1`; servizio `Ntc2018SteelMixedIClassifierD0M1` in `Verification.Steel`.
- Edizione esatta `DM 17 gennaio 2018 / NTC2018`; richiesta **SLU**, `DesignMaterial.Steel`, rapporto `N/M` proveniente da `FemAnalysis`, combinazione esplicita, ID membro/analisi/combinazione, stazione e `SectionSide`. Il servizio non permette di comporre massimi di N e M presi da inviluppi diversi.
- Evidenze indispensabili: sezione senza fori, ali parallele, laminazione e doppia simmetria, corrispondenza asse forte↔M FEM, geometria e grado acciaio, distribuzione plastica da cui si trae `α`, distribuzione elastica da cui si trae `ψ`, attestazione di coerenza sullo stesso N/M. Le attestazioni **sono fornite dall'utente e non certificate dal programma**.
- Rifiuto esplicito di `α≤0`, `α≥1`, `ψ≥1`, valori NaN/∞, sforzi fuori ambito e geometrie non fisiche; per `ψ≤−1` è obbligatorio il record esterno D0M2. Un `NotSupportedException` **non equivale a esito negativo della verifica strutturale**.
- Risultato: classe 1–4 di ala e anima e classe governante, provenienza, soglie, spiegazione; `ProvidesResistanceOrNmInteractionCheck=false` e `EstablishesCompleteMemberCompliance=false` per definizione.

## Casi di controllo separati dalla produzione

Lo script `benchmarks/independent/p0d0m1/verify_p0d0m1_mixed_alpha_psi.py` usa **frazioni razionali esatte** (`fractions.Fraction`), non chiama il codice C#, e ha superato **26/26** controlli: soglie e due rami di α, ψ per classe 3, classi 1–4, inclusività, incoerenze e casi esclusi.

Due gruppi esemplificativi con `fyk=235 MPa` (`ε=1`):

| α | ψ | C1 anima | C2 anima | C3 anima | `c_w/t_w`: classi 1 / 2 / 3 / 4 |
|---|---|---:|---:|---:|---|
| 0,6 | −0,5 | 58,2353 | 67,0588 | 83,1683 | 55 / 60 / 70 / 90 |
| 0,5 | −0,6 | 72 | 83 | 88,9831 | 70 / 76 / 85 / 90 |

Le coppie α/ψ sono **esempi numerici dei rami di tabella**, non attestazioni dell'effettiva distribuzione delle tensioni di una particolare trave. Gli xUnit `tests/StructuralSolver2D.Verification.Tests/D0/Ntc2018SteelMixedClassificationD0M1Tests.cs` verificano anche contratto FEM, cataloghi, attestazioni e rifiuto di input incompleti.

## Gate da eseguire su Windows appena possibile

```powershell
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~Ntc2018SteelMixedClassificationD0M1Tests' --logger 'console;verbosity=normal'
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --logger 'console;verbosity=normal'
.\scripts\run-g7-tests.ps1
python .\benchmarks\independent\p0d0m1\verify_p0d0m1_mixed_alpha_psi.py
```

**Gate P0-D0M1 PENDING** finché non si dispone dell'output .NET Windows. Restano PENDING anche P0-C2B/C2C0/C2C1/D1/D0; i gate confermati G7/B2/B3/B4/C1 FIX1/C2A conservano il loro stato. Non presentare 26/26 Python come prova della compilazione C#.

## Cosa manca prima di un software di verifica utilizzabile

1. Calcolo **autonomo e verificato** di α e ψ dai dati di sezione e dagli effetti concomitanti, inclusa coerenza plastica/elasticità, e **derivazione autonoma delle prove del ramo ψ≤−1 già introdotto da D0M2**.
2. Proprietà efficaci delle classi 4 e classificazione di geometrie/assi ulteriori con fonti e test appropriati.
3. **P0-D2** capacità di sezione per N/M/V e taglio elevato, interazioni con classi e instabilità; **P0-E** prove dei ritegni/vincoli reali, **P0-C3/C4** obblighi, stazioni e rapporto di copertura.
4. **P0-D3** SLE acciaio e verifiche sulle intere combinazioni; confronto indipendente con casi di letteratura determinati. GUI Avalonia **solo P3**.

**Distinzione da preservare:** *classificazione geometrico-tensionale di un piatto* ≠ *resistenza di una sezione* ≠ *stabilità/idoneità di una membratura* ≠ *conformità di un'opera*.
