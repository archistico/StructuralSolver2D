# M89-B — Verifiche parziali di sezione in acciaio (24 ottobre 2026)

## Ambito

M89-B aggiunge tre regole **distinte e isolate**, basate su EN 1993-1-1:2005+A1:2014. Il risultato riguarda **una sola sezione, una sola posizione, un solo caso/combinazione** proveniente da M87. Nessuna regola dimostra la conformità globale della struttura, dell'asta o della connessione. Sono utilizzate proprietà tabellate OPPO *indicative* e il catalogo caratteristico M88-C.

| Regola | Clausola | Resistenza calcolata | Limiti |
|---|---|---|---|
| `SteelGrossTensionYieldRuleM89B` | §6.2.3(2)(a), Eq. (6.6) | `Npl,Rd=A fy / γM0` | Solo snervamento **lordo** in trazione pura `N>0`, `V=M=0`; **NON** verifica rottura netta `Nu,Rd`, unioni o conformità a trazione completa |
| `SteelGrossCompressionRuleM89B` | §6.2.4(2), Eq. (6.10) | `Nc,Rd=A fy / γM0` | Solo compressione pura `N<0`, classi 1/2/3 dichiarate, fori pertinenti esclusi; **NON** verifica instabilità dell'asta né classe 4 |
| `SteelPureShearRuleM89B` | §6.2.6(2), Eq. (6.18) | `Vpl,Rd=Av fy /(√3 γM0)` | Solo taglio puro `N=M=0`; `Av` e orientamento **dichiarati**, torsione assente, instabilità a taglio esclusa mediante attestazione esterna, fori pertinenti esclusi |

**Segni FEM:** `N>0` trazione, `N<0` compressione; per il taglio il segno viene tracciato e la domanda è `|V|`. `N/V/M` devono essere *concomitanti* e riferiti esattamente alla stessa sezione. Una piccola componente non nulla non è arrotondata automaticamente a zero: le interazioni sono attività successive.

**Dati obbligatori:** identificativi di membro, sezione e acciaio; `fy` dal catalogo in base allo spessore nominale massimo tra ala/anima; `γM0` positivo con provenienza normativa dichiarata, senza valore implicito; edizione e Annesso Nazionale espliciti. La scelta di `γM0` resta responsabilità di chi esegue la verifica: il motore non garantisce che il coefficiente fornito corrisponda all'annesso dichiarato.

In compressione sono richiesti classe, giustificazione della classe e assenza di fori pertinenti. Per il taglio si richiede **in aggiunta** `Av` in cm² con motivazione esterna, corrispondenza tra asse di taglio FEM e `Av`, conferma di assenza torsione, di non necessità della verifica di instabilità a taglio e assenza di fori pertinenti.

Se mancano dati: `MissingData`; se l'azione/condizione è fuori campo: `NotApplicable`; se cambia norma/edizione/annesso: `Unsupported`. Nessuna di queste condizioni viene trasformata in `Passed`.

## Esempio verificabile a mano: IPE 200 / S235

Dalla tabella OPPO: area lorda `A=28,48 cm²`; per questa sezione `fy=235 MPa` dal catalogo M88-C. Scegliendo **esplicitamente e solo per l'esempio** `γM0=1,0`:

- snervamento lordo a trazione: `Npl,Rd = 28,48 × 235 × 0,1 = 669,28 kN`;
- compressione della sezione (classe 1–3 con evidenza indipendente): `Nc,Rd = 669,28 kN`;
- per il taglio puro parallelo all'anima, un esempio di area calcolata esternamente con la formula della sezione laminata I in §6.2.6(3)(a) è `Av = [2848 − 2×100×8,5 + (5,6+2×12)×8,5] / 100 = 13,996 cm²`; la condizione sul minimo `η hw tw` è responsabilità del valutatore, non automatizzata;
- `Vpl,Rd = 13,996 × 235 × 0,1 / √3`, circa **189,89 kN**; valore applicabile **solo** dopo aver documentato l'area, l'asse, l'assenza di torsione e l'esclusione dell'instabilità a taglio.

**Attenzione per la trazione:** EN 1993-1-1 §6.2.3 prescrive il controllo della resistenza netta a rottura `Nu,Rd=0,9 Anet fu/γM2`, non implementato. Un `Passed` della prima regola **non significa mai trazione completamente verificata**.

## Limitazioni e criticità note

1. Edizione supportata solo `EN 1993-1-1 / 2005+A1:2014` e annesso dichiarato; non sono implementate automaticamente EN 1993-1-1:2022, NTC o verifiche giuridiche di applicabilità dell'edizione.
2. I profili OPPO non sostituiscono tabelle certificate di progetto: fonte indicativa, masse/spessori e valori caratteristici devono essere confermati per l'impiego.
3. Classificazione sezioni, rottura netta, area netta, instabilità (asta e anima), torsione e interazioni `N/M/V` NON sono calcolate da queste regole; niente presunzioni su ritegni fuori piano.
4. Il contratto M87 permette `DesignOverallStatus.Passed` quando **tutte le regole richieste** passano: **non** lo si deve interpretare come verifica normativa completa. Un controllo non implementato va richiesto esplicitamente, ottenendo `Unsupported/Incomplete`.
5. L'accuratezza della ricerca della sezione più sollecitata resta distinta dall'accuratezza della verifica locale. M87-C restituisce sezioni candidate; non garantisce il massimo di tutte le interazioni future.

## Test

Eseguire in PowerShell dalla root del progetto:

```powershell
.\scripts\verify-m89b-manifest.ps1
.\scripts\run-m89b-tests.ps1
```

Il gate comprende build Release con `-warnaserror`, suite Verification completa M87/M89, cataloghi, regressioni FEM/CLI/report e smoke M86 opzionale (`-WithM86Smoke`). Il gate .NET deve essere confermato su Windows dopo la consegna del pacchetto.

## Fonti usate per la verifica delle formule

- EN 1993-1-1:2005, testo di riferimento per §§6.2.3, 6.2.4 e 6.2.6, inclusa l'errata/corrigenda AC:2006; nelle versioni adottate occorre sempre verificare gli aggiornamenti applicabili.
- EISEKO/RUNET, *Steel Sections Eurocode 3*, https://www.eiseko.it/public/mat/file/Runet/SteelSectionsEC3ENG.pdf
- JointLib, approfondimento dell'area di taglio secondo §6.2.6, https://jointlib.com/en/steel/resistance/shear-force
- OPPO IPE/HEA/HEB/INP, dati geometrici di catalogo di natura indicativa, https://www.oppo.it/tabelle/profilati_ipe.htm

## Successive tappe

M90 classificazione delle sezioni e interazioni N/M/V; M91 instabilità locale/globale, con dati sui vincoli fuori piano espliciti. M92–94 legno; M95 SLE; M96–99 validazione e release normativa. GUI Avalonia alla fine.
