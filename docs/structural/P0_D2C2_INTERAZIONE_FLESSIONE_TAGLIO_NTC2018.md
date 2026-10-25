# P0-D2C2 — Interazione fra flessione forte e taglio d'anima, NTC 2018

Data: 25 ottobre 2026. **Implementazione C# e xUnit pronta; 946/946 golden indipendenti PASS; test .NET D2C2 in attesa** perché l'SDK `dotnet` non è disponibile qui. L'autrice ha confermato **«test passati» per D2C1 e precedenti**; la conferma, senza log numerico, non si estende a D2C2.

## Ambito ristretto e formule

La regola indipendente `Ntc2018SteelBendingShearInteractionRuleD2C2` verifica **una sola sezione, un solo lato e una sola combinazione FEM SLU**, con azioni **simultanee** e firmate **N=0, Vy≠0 e Mz≠0**. `Mz` è la flessione forte nel piano dell'anima, `Vy` è il taglio nella medesima anima. Sono ammessi soltanto profili laminati IPE/HEA/HEB ad ali parallele, doppiamente simmetrici, nominalmente non forati, con anima non irrigidita e assenza attestata di torsione o azioni spaziali. Gli INP e gli altri domini non vengono ricondotti artificiosamente a questo modello.

Riferimento normativo: **D.M. 17 gennaio 2018 (NTC 2018), §4.2.4.1.2.6, equazioni [4.2.30]–[4.2.32]**; sono inoltre applicati §§4.2.4.1.2.3–4 per le resistenze sezionali e lo schermo di instabilità d'anima [4.2.27]. Per le sezioni ordinarie **γM0=1,05** secondo Tab. 4.2.VII.

\[
V_{c,Rd}=\frac{A_v f_{yk}}{\sqrt{3}\,\gamma_{M0}},\qquad
A_v=A-2bt_f+(t_w+2r)t_f
\]

Con **VEd ≤ 0,5 Vc,Rd** la riduzione del momento può essere trascurata, a condizione che non intervenga l'instabilità a taglio. Per classi 1/2 si impiega la capacità plastica documentata `Mpl,Rd=Wpl,y fyk/γM0`; per classe 3 la capacità elastica `Mel,Rd=Wel,y fyk/γM0`. **Non si sostituisce mai Wpl con Wel.**

Se **0,5 Vc,Rd < VEd ≤ Vc,Rd**, la formula NTC esplicita, **per sezioni I/H doppiamente simmetriche di classe 1/2**, è:

\[
\rho=\left(\frac{2V_{Ed}}{V_{c,Rd}}-1\right)^2,
\qquad
M_{y,V,Rd}=\min\left(M_{y,c,Rd},
\frac{\left(W_{pl,y}-\rho\frac{A_w^2}{4t_w}\right)f_{yk}}{\gamma_{M0}}\right).
\]

`Aw` è l'**area geometrica dell'anima**, distinta dall'**area resistente a taglio `Av`**. Non sono intercambiabili: un inserimento del solo `Av` come `Aw` è rifiutato. L'area `Aw` deve risultare documentata e compatibile con i limiti geometrici nominali, fra `tw·[h−2(tf+r)]` e `A−2btf` (si ammette solo uno scarto nominale di arrotondamento 0,1%). La regola usa **conservativamente il maggiore fra l'area documentata e il limite superiore `A−2btf`**, comprensivo della materia nella zona dei raccordi: sottrae quindi una penalità **non minore** nella [4.2.32]. Tale maggiorazione prudenziale può condurre a un falso allarme e **non sostituisce una verifica su geometria certificata**.

La domanda restituita è il rapporto **adimensionale** `max(abs(Vy)/Vc,Rd, abs(Mz)/My,V,Rd)`, con resistenza convenzionale `1` e giudizio `Passed`/`Failed` della **sola stazione**. Per `VEd > Vc,Rd` la regola fallisce già per taglio, **senza estrapolare ρ** al di fuori del dominio normativo.

### Limitazioni obbligatorie

- Per **classe 3 e VEd > 0,5 Vc,Rd** ⇒ `NotApplicable`: [4.2.32] non autorizza ad applicare una riduzione plastica alla classe 3; non viene inventata una legge alternativa.
- Per **classe 4**, priva di proprietà efficaci e metodologia adeguata ⇒ `NotApplicable`, anche a taglio basso.
- Per anime che superano `hw_upper/tw > 72 sqrt(235/fyk)` con `hw_upper=h−2tf` e `η=1` ⇒ `NotApplicable`; resta obbligatoria la verifica dell'instabilità dell'anima, **non implementata**. La verifica dello schermo di accesso non è un calcolo di stabilità.
- Assenza di prove numeriche indipendenti di `Av`, `Wel` e, per classi 1/2, `Wpl`, identità FEM o condizioni fisiche ⇒ `MissingData` o stato bloccante previsto dal contratto. Scarto `Av`/`Wel` rispetto ai dati nominali oltre lo 0,1% ⇒ `MissingData`, mai capacità improvvisata.
- Restano **non coperte** N–V, N–M–V, fori e collegamenti, profilati saldati/asimmetrici/INP, anima irrigidita, torsione, forze fuori piano, instabilità flesso-torsionale, instabilità di membratura, effetti locali, fatica, sismica, altre sezioni o combinazioni, SLE e verifica dell'intera opera.

## Tre esempi numerici verificabili

| Sezione e classe | Azioni nella sezione | Risultati nominali della regola | Giudizio delimitato |
|---|---|---|---|
| **IPE 200, S235, classe 1**; `Av=1399,6 mm²`, `Wpl=220640 mm³` (STAD) | `Vy=80 kN`, `Mz=40 kNm` | `Vc,Rd≈180,8514 kN`; `Mpl,Rd≈49,3813 kNm`; **ρ=0** | Sezione **PASS** (non membratura) |
| **IPE 200, S235, classe 1**; `Aw` bound usato `1148 mm²` (diverso da `Av`) | `Vy=150 kN`, `Mz=40 kNm` | `ρ≈0,4340`; `My,V,Rd≈43,6659 kNm`; con `Mz=46 kNm` → FAIL | Sezione **PASS** per 40 kNm; **FAIL** per 46 kNm |
| **HEA 180, S355, classe 2**; `Wpl≈325000 mm³` (ArcelorMittal) | `Vy=0,75 Vc,Rd`, `Mz=60 kNm` | `Vc,Rd≈282,4535 kN`; `My,V,Rd≈105,5807 kNm` | Sezione **PASS** |

Controesempio distinto: **HEA 280, S355, classe 3**, con `Vy=0,4 Vc,Rd` e `Mz=250 kNm` ammette il ramo elastico a taglio basso, mentre con `Vy=0,75 Vc,Rd` restituisce **`NotApplicable`**, non PASS. Valori riferiti a profili nominali e sezioni ipotetiche, mai certificazione di materiale, membro o opera.

## Provenienza, test e tracciabilità

L'evidenza `Ntc2018SteelBendingShearEvidenceD2C2` lega **membro, sezione, grado, ID analisi, ID combinazione, ascissa esatta e lato**. Richiede fonti/attestazioni separate per geometria, stato dei fori, grado, direzioni locali Mz/Vy, coppia di effetti FEM, `Av`, `Wel`, `Wpl` (quando necessario) e `Aw` (solo con taglio alto). Il classificatore nativo D0 determina la **classe della distribuzione di tensioni normali da flessione forte**: la concomitanza del taglio, controllata a parte, non trasforma arbitrariamente la formula di classificazione.

L'engine segnala anche per D2C2 `EstablishesCompleteMemberCompliance=false`: **perfino un `OverallStatus.Passed` delle sole regole selezionate non prova che l'asta o l'edificio siano verificati**. La futura copertura C3/C4 e le verifiche di instabilità, collegamenti, altre stazioni e combinazioni rimangono indispensabili.

1. **Nuovo golden aritmetico autonomo** `benchmarks/independent/p0d2c2/verify_p0d2c2_ntc_bending_shear.py`: **946/946 PASS** con frazioni esatte, `Decimal` ad alta precisione e controllo di 90 combinazioni I/H × grado, formule native, limiti di classe, soglia di buckling, branch alto/basso e prove di non intercambiabilità Av/Aw. Non esegue codice C#.
2. **Regressione aritmetica indipendente complessiva**, da R2 fino a D2C2: **4.203/4.203 PASS, 15 script exit 0**; questi numeri **non** indicano un collaudo .NET.
3. **Nuovi xUnit** `tests/StructuralSolver2D.Verification.Tests/D2/Ntc2018SteelBendingShearD2C2Tests.cs`: soglia esatta del 50%, V>Vc,Rd, classi 1/2/3/4, segni, Wpl/Wel/Av/Aw, condizioni fisiche, FEM, normativa e incompletezza del membro. **Da eseguire su Windows:** `dotnet test StructuralSolver2D.sln` (oppure il relativo `.sln` presente nell'archivio). **D2C2 xUnit PENDING**.
4. **Ultimo gate .NET dichiarato dall'autrice:** D2C1 e precedenti **PASS**, senza log/numero di test allegati. La conferma non viene propagata ai nuovi file D2C2.

## Fonti

- [Gazzetta Ufficiale — D.M. 17 gennaio 2018, NTC 2018](https://www.gazzettaufficiale.it/eli/id/2018/02/20/18A00716/sg), §4.2.4.1.2.6 [4.2.30]–[4.2.32], §§4.2.4.1.2.3–4, Tabella 4.2.VII.
- [Capitolo 4 NTC 2018 — copia PDF di consultazione](https://www.studiopetrillo.com/files/ntc2018/cap4.pdf), formule numerate.
- OPPO, dati **indicativi non certificativi**: [IPE](https://www.oppo.it/tabelle/profilati_ipe.htm), [HEA](https://www.oppo.it/tabelle/profilati_hea.htm), [HEB](https://www.oppo.it/tabelle/profilati_heb.htm).
- STAD (modulo plastico nominale IPE 200) e ArcelorMittal Orange Book (modulo plastico nominale HEA 180): riscontri indipendenti citati esplicitamente nelle fixture e nel golden.

## Roadmap successiva

1. **Conferma .NET D2C2** e regressione generale su Windows, registrando conteggi e eventuali errori esatti.
2. **P0-E** — dati, ritegni e assunzioni fisiche per instabilità d'asta, flesso-torsionale e d'anima; nessuna capacità senza prova delle premesse.
3. **P0-C3/C4** — catalogo degli obblighi mancanti, copertura reale di aste/sezioni/combinazioni e report espliciti.
4. **P0-D3** — SLE acciaio dopo copertura e criteri verificati; per interazioni N–M–V serve una nuova milestone dedicata e fonti specifiche.
5. **GUI leggera C#/Avalonia esclusivamente P3**, dopo completamento/validazione del motore FEM e fase normativa.
