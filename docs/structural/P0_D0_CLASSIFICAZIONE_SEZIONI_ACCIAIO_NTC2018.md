# P0-D0 — Classificazione delle sezioni in acciaio secondo NTC 2018

**25 ottobre 2026 — prima implementazione C#; xUnit Windows non ancora eseguiti.**

## 1. Fonti e limite deliberato

Fonte primaria verificata anche **visivamente** nella Gazzetta Ufficiale:
[D.M. 17 gennaio 2018, NTC 2018](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf),
**§4.2.3.1, Tabella 4.2.III (parti interne) e Tabella 4.2.IV
(piattabande esterne)**, pagine stampate 94–95; apertura PDF pp. 98–99.
Il capitolo 4 del testo di Claudio Bernuzzi, *Progetto e verifica delle strutture
in acciaio secondo le NTC 2018 e l'Eurocodice 3*, seconda edizione, è
un riferimento didattico **secondario**, non una norma sostitutiva.

Questa prima regola riguarda **esclusivamente** sezioni I/H **laminate a
caldo, doppiamente simmetriche e ad ali parallele, senza fori**, con:

1. **compressione uniforme pura** (N di compressione, M=0), oppure
2. **flessione pura sull'asse forte** (M, N=0).

Per queste due condizioni **la parte di anima considerata compressa cambia**
e quindi anche le soglie. La classe finale è il massimo fra classe
dell'anima e classe della piattabanda compressa. L'esito identifica una
**classe geometrica**, non una resistenza né un esito di adeguatezza.

## 2. Soglie normative codificate

Si utilizza **ε = √(235/fyk)** calcolato dal valore di fyk (MPa), non
il valore ε arrotondato esposto accanto alla tabella stampata.

| Parte e stato di tensione | Classe 1 | Classe 2 | Classe 3 | Classe 4 |
|---|---:|---:|---:|---:|
| Piattabanda esterna laminata, compressa | c/t ≤ 9ε | ≤ 10ε | ≤ 14ε | c/t > 14ε |
| Anima, compressione uniforme | c/t ≤ 33ε | ≤ 38ε | ≤ 42ε | c/t > 42ε |
| Anima, flessione pura sull'asse forte | c/t ≤ 72ε | ≤ 83ε | ≤ 124ε | c/t > 124ε |

Per geometrie I/H laminate ad ali parallele, con dimensioni **mm**:

- `c_flange = (b - tw - 2r)/2`
- `c_web = h - 2tf - 2r`
- `classFinale = max(classFlange, classWeb)`.

Soglie per **altri** stati tensionali N+M dipendono da `α`/`ψ` e,
nelle configurazioni pertinenti, da formule di tabella o
**EN 1993-1-5**. Non si possono ricondurre a una delle tre righe
precedenti senza verificare le ipotesi.

## 3. Codice e integrazione

- Nuovo `Ntc2018SteelISectionClassifierD0` nel progetto
  `StructuralSolver2D.Verification.Steel`, ma con identità
  **`DM 17 gennaio 2018 / NTC2018 / IT-NTC2018-NATIONAL-CODE`**, distinta
  da `SteelISectionClassifierM90A` identificato come **EC3 2005+A1:2014**.
- `ClassifyCatalogProfile` recupera i profili da M88-B2 OPPO e la resistenza
  allo snervamento da M88-C, rispettando la banda di spessore.
  **Solo** famiglie IPE/HEA/HEB. INP ad ali inclinate respinti.
- `ClassifyGeometry` permette prove con geometrie esplicitamente fornite,
  senza inventare un prodotto certificato.
- `Ntc2018SteelClassificationEvidenceD0` richiede dichiarazione ed evidenza
  per assenza fori, tipologia geometrica, materiale, stato di sollecitazione
  e orientamento dell'asse forte quando necessario.
- Il risultato contiene **due rapporti c/t**, i sei limiti soglia applicabili,
  il valore ε, la classe della piattabanda e dell'anima, la classe
  governante, fonti e tracciato di esclusione.
- `Class==4` imposta `RequiresEffectiveSectionProperties=true`, **senza**
  produrre `Aeff` o `Weff`: servono ulteriori metodologie e verifiche.

Non è stata mutata **alcuna** regola EC3 M89–M91, alcuna equazione
del FEM o l'implementazione P0-D1 della trazione netta. Questo classificatore
non è ancora collegato automaticamente a una verifica di resistenza NTC:
il collegamento appartiene a P0-D2 e agli audit di copertura P0-C3.

## 4. Casi numerici indipendenti

**Confini esatti con S235:** 9/10/14/15 per le ali, 33/38/42/43
per l'anima compressa, 72/83/124/125 per l'anima inflessa.
Le soglie di classe sono **inclusive** (`≤`), il valore superiore
passa alla classe successiva solo se superato.

Due esempi geometrici scelti perché governano parti diverse:

| Geometria dichiarata | Stato | Anima | Piattabanda | Classe globale | Esito operativo |
|---|---|---:|---:|---:|---|
| IPE 550, S275, h550/b210/tw11,1/tf17,2/r24 | Compressione uniforme | 4 | 1 | **4** | Identificazione, servono proprietà efficaci |
| HEA 280, S420 dichiarato, h270/b280/tw8/tf13/r24 | Flessione asse forte | 1 | 3 | **3** | Identificazione, nessuna resistenza calcolata |

Questi esempi richiamano le applicazioni didattiche A4.1/A4.2 già presenti
nei benchmark M90, ma il calcolo **NTC D0** riproduce indipendentemente
le formule delle Tabelle 4.2.III/IV. Non sono verifiche di prodotti
reali, né esiti di un modello di telaio.

## 5. Controlli e loro stato

**Eseguiti localmente:**

```text
python3 benchmarks/independent/p0d0/verify_p0d0_ntc_steel_section_classes.py
25/25 PASS — aritmetica indipendente dalla libreria C#
```

**Predisposti e non ancora eseguiti:**

`tests/StructuralSolver2D.Verification.Tests/D0/Ntc2018SteelISectionClassificationD0Tests.cs`

Contiene casi per soglie 1/2/3/4, due esempi geometrici, cinque profili
comuni, qualità/spessori, attestazione del sistema normativo, differenza
tra flessione e compressione, INP e input invalidi, fori/prove assenti,
geometrie degeneri e isolamento dall'EC3.

Quando disponibili Windows e `dotnet`:

```powershell
dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj --filter FullyQualifiedName~Ntc2018SteelISectionClassificationD0Tests
dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj --filter FullyQualifiedName~Ntc2018SteelTensionRulesD1Tests
dotnet test StructuralSolver2D.sln
```

Tutti i nuovi test xUnit **P0-D0 e P0-D1** rimangono **PENDING** finché
il codice non viene effettivamente compilato e testato. I gate
precedentemente confermati dall'utente non vengono annullati.

## 6. Rischi aperti e prossima roadmap

| Priorità | Lavoro | Condizione di uscita |
|---|---|---|
| Alta | **P0-D0 estensione** | Classificazione in N+M tramite α/ψ, parti saldate, metodi per classe 4, asse debole solo dove normativamente provati |
| Critica | **P0-D2** | NTC sezioni in N/M/V concomitanti, minimi/resistenze plastico/elastico/efficaci e taglio alto; mai combinare casi puri come se bastassero |
| Critica | **P0-E** | Dati e verifiche **esterni** per instabilità fuori piano, αcr, ritegni e connessioni |
| Alta | **P0-C3** | Registro obbligazioni di ogni membro, classe/azione/giunto/caso; nessun singolo `Passed` equivale a idoneità |
| Alta | **P0-D3** | SLE acciaio con limiti di destinazione/monta, combinazioni e massimi veri |
| Ultima fase | **P3** | GUI C#/Avalonia dopo il consolidamento delle verifiche e del motore |

**Divieto di sovraestensione:** identificare una classe di sezione non
comprova automaticamente resistenza, capacità rotazionale, stabilità,
comportamento plastico della struttura o conformità di progetto.
