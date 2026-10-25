# P0-D2B — Resistenza sezionale a flessione pura NTC 2018, asse forte

Data: 25/10/2026. **Milestone implementata; test xUnit .NET PENDING, non eseguiti nel presente ambiente.**

## Risultato e formula

La nuova regola indipendente `Ntc2018SteelPureBendingResistanceRuleD2B` verifica **il solo momento flettente di una sezione** doppiamente simmetrica IPE/HEA/HEB laminata a caldo, asse forte, senza fori rilevanti. Richiede lo **stesso** membro, analisi FEM, combinazione SLU, coordinata e lato; N e Vy concomitanti devono essere esplicitamente **zero**, non null; Mz deve essere diverso da zero.

Riferimento principale: **D.M. 17 gennaio 2018**, NTC 2018, **§4.2.4.1.2.3, [4.2.11]–[4.2.14]**, e **Tab. 4.2.VII**, `γM0 = 1,05`. Le classi sono calcolate dal precedente classificatore NTC **P0-D0**, §§4.2.3.1 Tabelle 4.2.III/IV, con fonti e geometria esplicite.

| Classe della sezione | Resistenza `Mc,Rd` | Condizione nel software |
|---|---|---|
| 1 e 2 | `Wpl,strong · fyk / γM0` | **Wpl deve essere fornito e documentato da fonte esterna**. OPPO non contiene Wpl nel nostro JSON; è vietato usare Wel al suo posto. |
| 3 | `Wel,min,strong · fyk / γM0` | Modulo dichiarato indipendente e confrontato con OPPO Wx entro lo **0,1%**; si usa il minore fra i due dati nominali. |
| 4 | `Weff,min · fyk / γM0` | **NotApplicable**: Weff non è implementato. Nessuna sostituzione con Wpl/Wel. |

Conversioni: `W [mm³] × fyk [N/mm²] / γM0 / 1.000.000 = Mc,Rd [kNm]` e `MEd = abs(Mz,FEM)`.

Per i soli profili doppiamente simmetrici adottati, il modulo elastico minimo delle due fibre estreme nell'asse forte è il Wx nominale della tabella. Nelle classi 1–2, a scopo di controllo preliminare dell'input, deve valere `Wpl >= Wel_usato` e `Wpl <= A×h/2`; **questo intervallo è necessario ma non dimostra affatto** che il Wpl dichiarato sia corretto. Sono obbligatorie evidenze esterne e responsabilità di verifica del professionista.

## Fonti primarie consultate

- **Testo normativo ufficiale**: Gazzetta Ufficiale, *D.M. 17 gennaio 2018 — Aggiornamento delle norme tecniche per le costruzioni*, G.U. Serie Generale n. 42 del 20 febbraio 2018, §4.2.4.1.2.3, equazioni [4.2.11–14] e Tabella 4.2.VII. <https://www.gazzettaufficiale.it/eli/id/2018/02/20/18A00716/sg>
- **OPPO** (catalogo nominale esterno, **non certificato**): [IPE](https://www.oppo.it/tabelle/profilati_ipe.htm), [HEA](https://www.oppo.it/tabelle/profilati_hea.htm), [HEB](https://www.oppo.it/tabelle/profilati_heb.htm); gli INP hanno ali inclinate e non rientrano nel classificatore D0.
- **STAD** (pubblicazione tecnica del fornitore, 13 ottobre 2026, dati dichiaratamente *indicativi*), tabella IPE, riga IPE 200: `Wel,y=194,30 cm³`, `Wpl,y=220,64 cm³`. <https://www.stad.fr/downloads/pdf/stad-technical-data-of-sections-ipe.pdf>
- **ArcelorMittal Orange Book**, tabella `HE 180 A` (HEA 180): `Wpl,y` arrotondato a `325 cm³`; il catalogo OPPO riporta `Wel,y=293,6 cm³`. <https://orangebook.arcelormittal.com/node/16>

Le fonti esterne supportano il **valore nominale d'esempio**; un prodotto reale e la sua conformità richiedono documentazione pertinente e verificata per il caso concreto. Il software **non** collega automaticamente una pagina a un certificato, non controlla la sua autenticità né valida l'attribuzione fisica del profilato.

## Golden di controllo (valori puramente sezionali)

| Esempio | Classe di flessione | Modulo di calcolo | `fyk` | `Mc,Rd`, kNm |
|---|---:|---:|---:|---:|
| IPE 200 S235 | 1 | Wpl 220.640 mm³, STAD | 235 MPa | `220640×235/(1,05×10⁶)` ≈ **49,3813** |
| HEA 180 S355 | 2 | Wpl 325.000 mm³, ArcelorMittal arrotondato | 355 MPa | `325000×355/(1,05×10⁶)` ≈ **109,8810** |
| HEA 280 S355 | 3 | Wel,min 1.013.000 mm³, OPPO + evidenza esterna | 355 MPa | `1013000×355/(1,05×10⁶)` ≈ **342,4905** |

Gli esempi non sono un dimensionamento, non sono un certificato e **non** includono condizioni di vincolo fuori piano né effetti del secondo ordine.

## Contratto di verifica e fail-closed

`Ntc2018SteelPureBendingEvidenceD2B` identifica indipendentemente membro, sezione, grade, FEM, combinazione, x e lato (`Continuous`, `BeforePointLoad`, `AfterPointLoad`). Richiede assenza di fori rilevanti dichiarata e documentata, geometria I/H doppiamente simmetrica, mappatura **locale Mz ↔ asse forte OPPO Wx** attestata, esclusione delle azioni spaziali/torsione, fonte per `Wel`, e — per classi 1/2 — valore e fonte per `Wpl`.

L'engine segnala `MissingData` per prova, modulo, profilo, grade o fattori normativi insufficienti; `NotApplicable` per azioni non pure, sezioni/rami non trattati; `Unsupported` per edizione EN o norme differenti. Ogni calcolo ha traccia con input nominali, classe, fonte, `γM0`, domanda e capacità.

**Casi non coperti:** taglio non nullo; sforzo normale concomitante; flessione composta o biassiale; fori e collegamenti; asse debole; sezioni saldate o rastremate/INP; classe 4 efficace; instabilità flesso-torsionale NTC **§4.2.4.1.3.2**; instabilità d'asta; effetto P–Δ; interazioni N/M/V; sismica; ogni altro punto e combinazione non richiesti. `EstablishesCompleteMemberCompliance=false` per design: il PASS di una sezione **non approva asta o struttura**.

## Collaudi

1. Script aritmetico indipendente `benchmarks/independent/p0d2b/verify_p0d2b_ntc_pure_bending.py`: **852/852 PASS**. Classificazione in alta precisione e capacità con frazioni esatte; confronto fonti produttore nei casi IPE200/HEA180; rifiuto classe 4 e Wpl inappropriato.
2. Nuovi test `tests/StructuralSolver2D.Verification.Tests/D2/Ntc2018SteelPureBendingD2BTests.cs`: preparati, **non eseguiti qui** perché `dotnet` assente; conferma utente **PENDING**.
3. Gate .NET della milestone D2A: **ancora PENDING** per il medesimo motivo. Ultima conferma utente di regressione .NET: **D0M8 e precedenti PASS**, senza log dettagliato allegato.
4. Script indipendenti delle milestone precedenti: devono restare PASS alla regressione complessiva; il report finale di consegna deve registrare il risultato effettivo, non presumerlo.

## Prossimo avanzamento

Collaudare su Windows xUnit D2A+D2B, poi **P0-D2C**: taglio sezionale come regola distinta e possibili riduzioni con M nel campo documentato; **P0-E**: limiti delle evidenze fisiche sui ritegni e stabilità; **P0-C3/C4**: audit di copertura, report e segnali di incompletezza; **P0-D3**: verifiche SLE solo con riferimenti e ipotesi tracciati. Il motore FEM rimane prioritario per qualità e prova; GUI C#/Avalonia soltanto fase **P3**.
