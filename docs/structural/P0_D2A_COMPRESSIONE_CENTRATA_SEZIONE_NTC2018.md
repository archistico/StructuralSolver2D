# P0-D2A — Resistenza sezionale a compressione centrata, acciaio NTC 2018

**Data:** 25 ottobre 2026. **Stato implementativo:** nuovo codice, xUnit e riferimento indipendente predisposti. **Ultimo gate .NET confermato dall'autrice:** tutti i test del FULL P0-D0M8 **PASS** (conferma testuale, senza log numerici). I nuovi xUnit **P0-D2A PENDING**: nell'ambiente di confezionamento non è disponibile `dotnet`.

## Risultato esatto e fonte primaria

- **DM 17/01/2018, NTC 2018**, §4.2.4.1.2.2, **[4.2.9]–[4.2.10]**: per classi **1, 2, 3**, `N_c,Rd = A · f_yk / γ_M0`; verifica del rapporto `N_Ed/N_c,Rd ≤ 1`.
- §4.2.4.1.1, **Tabella 4.2.VII**: per resistenza della sezione `γ_M0=1,05`; per instabilità delle membrature `γ_M1=1,05` (**stesso numero, ma fenomeno e formula differenti**). D2A usa solo `γ_M0`.
- Per classe **4** l'equazione richiede **`A_eff`**, non l'area lorda; D2A non calcola `A_eff`, quindi rifiuta la verifica.
- Riferimento **primario ufficiale**: [Gazzetta Ufficiale, decreto e allegato capitolo 4, pagine 97–99](https://www.gazzettaufficiale.it/eli/id/2018/02/20/18A00716/sg), [PDF capitolo 4](https://www.gazzettaufficiale.it/do/atto/serie_generale/caricaPdf?art.codiceRedazionale=18A00716&art.dataPubblicazioneGazzetta=2018-02-20&art.num=1&art.tiposerie=SG&cdimg=18A0071600100010110005&dgu=2018-02-20).

## Campo ammesso (tutti i requisiti obbligatori)

1. **Solo SLU NTC 2018, membri ordinari**, profilo esatto `Ntc2018SteelTensionProfileD1.Create()` già presente e immutabilmente validato (non è un profilo EC3).
2. **Un'unica stazione/lato** di una combinazione risultante da FEM identificato: `MemberId`, `AnalysisId`, `CombinationId`, `PositionMetres`, `SectionSide` corrispondono uno a uno a evidenza esterna; viene usato il segno del risolutore **`N < 0` compressione**, e contemporaneamente `V = 0`, `M = 0`. Nessuna inferenza di zero per valori null.
3. Profilo commerciale **IPE, HEA o HEB** laminato con ali parallele, doppiamente simmetrico, **senza fori rilevanti**, geometria e acciaio documentati. Il vincolo «senza fori» è **intenzionalmente più restrittivo** delle eccezioni ammesse dal testo NTC per alcuni fori occupati da bulloni/chiodi.
4. La prova esterna dichiara: sezione e **area nominale numerica** in mm², grado e fascia di spessore, assenza fori, compressione centrata, assenza di ulteriori azioni spaziali/torsione. Il FEM piano da solo **non prova** questi dati. La superficie dichiarata deve accordarsi con OPPO entro lo **0,1%** (confronto di arrotondamento, **non** tolleranza di certificazione), e il calcolo usa il **minore** dei due valori. Disaccordo sostanziale o area sconosciuta ⇒ `MissingData`.
5. Classificazione **nativa NTC 2018 D0** calcolata con tabelle 4.2.III–IV sulla geometria del catalogo e sul `f_yk` nominale della fascia di spessore: vengono ammesse **soltanto classi 1–3**.

I cataloghi OPPO/M88-C hanno dati **indicativi, non certificati**, utili ad esempi e valutazioni preliminari; l'identità del prodotto reale richiede verifica professionale.

## Calcolo di riferimento: IPE 200, S235

Da catalogo nominale OPPO: `A = 28,48 cm² = 2.848 mm²`. Materiale dichiarato `S235`, spessore nominale minore di 16 mm, `f_yk = 235 MPa`. Per compressione pura la sezione è **classe 1** nel campo della classificazione D0.

```
Nc,Rd = 2848 · 235 / (1,05 · 1000)
      = 637,4095238095238 kN

FEM NEd = -600 kN   => domanda +600 kN => u=0,941311... => Passed (solo sezione)
FEM NEd = -650 kN   => domanda +650 kN => u=1,01975...  => Failed (solo sezione)
```

La conversione da N/mm²×mm² a kN divide per 1000. Se la domanda è esattamente `Nc,Rd`, l'uguaglianza rientra nella condizione `≤1`. L'esempio non è una verifica del pilastro.

## API e risultati

- File: `src/StructuralSolver2D.Verification.Steel/Ntc2018SteelPureCompressionResistanceD2A.cs`.
- Record `Ntc2018SteelPureCompressionEvidenceD2A`: prova per stazione/lato/azione/contesto, incluso `DeclaredGrossAreaMm2` e la sua fonte. Rifiuta duplicati del medesimo target.
- Regola `Ntc2018SteelPureCompressionResistanceRuleD2A`: implementa `IDesignCheckRule` e produce `Demand`, `Resistance`, `Utilization`, fonte normativa e traccia. `EstablishesCompleteMemberCompliance=false`.
- `Passed`/`Failed` descrivono **solo questa formula sezionale**. **`NotApplicable`** per diversa azione o classe 4; **`MissingData`** per assenza di prova; **`Unsupported`** per edizione errata gestita dall'engine; nessuna traduzione silenziosa da EC3, nessun fallback di classe/area.
- Traccia: area, resistenza, grado, classe anima/ala, gamma, fonte geometrica nominale, prova di assenza fori, stazione/lato e l'esplicita frase **NO MEMBER BUCKLING**.

## Test e validazione

- xUnit `tests/StructuralSolver2D.Verification.Tests/D2/Ntc2018SteelPureCompressionD2ATests.cs`: valori `Passed/Failed`, uguaglianza, classi 1/2/3, simulacro **sintetico solo di test** classe 4, carichi combinati non ammessi, NaN/null, provenienza, stazione/lato, acciaio/INP ignoti, duplicati, cambio di gamma/versione, obbligo di instabilità non registrato e rapporto globale **Incomplete**.
- Golden autonomi in `benchmarks/independent/p0d2a/verify_p0d2a_ntc_pure_compression.py`: razionali esatti per unità/fattori/capacità, soglie geometriche con decimali ad alta precisione, tutte le sezioni IPE/HEA/HEB e tutti i tre gradi del catalogo. Lo script **non importa né esegue codice C#**.
- Eseguire sul PC con SDK .NET: `dotnet test StructuralSolver2D.sln --configuration Release` (oppure seguire il comando di regressione della roadmap se la soluzione ha un nome diverso). Prima di promuovere il gate va verificato l'esito reale su Windows.

## Limiti non negoziabili

Questo modulo **non verifica l'instabilità a compressione di una membratura**, la snellezza/curva di stabilità, il taglio/flessione concomitanti, eccentricità, seconde deformazioni, imperfezioni, contatti, nodi, fori con bulloni, resistenza fuori piano, azioni sismiche, completezza di sezioni/combinazioni o idoneità dell'edificio. La parte §4.2.4.1.3 di NTC sulla stabilità resta da implementare, con `γ_M1` solo quando sarà giustificato. La sola regola registrata può restituire `Passed` sezionale; **l'esempio xUnit registra anche l'obbligo di instabilità ancora `Unsupported` e ottiene `OverallStatus=Incomplete`**.

## Roadmap dopo D2A

1. **Gate Windows D2A:** compilazione e tutti i test; regressioni complete G7, B, C, D0–D2A.
2. **P0-D2B**: flessione retta pura NTC 2018 per una sola sezione con modulo `Wpl` documentato per classi 1–2, `Wel,min` per classe 3; classe 4 senza `Weff` esclusa. Non anticipare la flessione/plasticità se i dati di catalogo non bastano.
3. **P0-E + C3/C4:** precondizioni fisiche, matrice di verifiche obbligatorie e tracciatura delle mancanze/rapporti globali.
4. **P0-D3:** altre formule normative, stabilità e N/M/V solo con riferimenti validati. **P3 finale:** interfaccia C#/Avalonia leggera dopo il calcolo e le verifiche, non prima.
