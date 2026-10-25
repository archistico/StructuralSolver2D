# Changelog — StructuralSolver2D

Registro **sintetico e cronologico**. Ogni stato «gate superato» è basato sulle conferme dei test Windows dell'utente, non su una certificazione della conformità normativa dell'opera. I dettagli delle milestone si trovano in `docs/structural/` e nell'[archivio](docs/archive/README.md).

## 25 ottobre 2026 — P0-D2B: flessione pura di una sezione acciaio, normativa NTC2018 (gate .NET pending)

- Nuova regola `Ntc2018SteelPureBendingResistanceRuleD2B`: NTC2018 §4.2.4.1.2.3 [4.2.11–14], SLU, γM0=1,05; solo FEM combinazione/stazione/lato provati, `Mz!=0`, `N=Vy=0` simultanei e profilo I/H nominale senza fori. Classificazione NTC nativa D0; **classe 1/2 Wpl da fonte indipendente obbligatoria**, classe 3 Wel,min esterno incrociato con OPPO Wx entro 0,1% e scelta conservativa, classe 4 senza Weff rifiutata. Wpl mai inferito dal catalogo o da Wel. Verifica sezionale e non di stabilità flesso-torsionale, membro o struttura.
- Esempi numerici tracciati con fornitori STAD (IPE200 Wpl 220,64 cm³) e ArcelorMittal Orange Book (HEA180 Wpl arrotondato 325 cm³). Numeri nominali, non certificazioni del prodotto. Moduli catalogo/solutore FEM e regole precedenti invariati.
- Nuovi xUnit `D2/Ntc2018SteelPureBendingD2BTests.cs`: classi 1/2/3, classe 4 sintetica, soglia pass/fail e segni, provenienza FEM, gamma/edizione, fori, Wpl mancante/non fisico, distinzione Wel e report d'obbligo instabilità incompleto. **Nuovo golden indipendente Python 852/852 PASS; regressione aritmetica R2–D2B 2.409/2.409 PASS (13 script)**. **Test .NET nuovi D2A+D2B IN ATTESA**: SDK assente nell'ambiente di assemblaggio e autrice momentaneamente impossibilitata a testarli. Ultimo gate .NET PASS dichiarato: **D0M8** e precedenti, senza log allegati.
- Documento [P0-D2B](docs/structural/P0_D2B_FLESSIONE_PURA_SEZIONE_NTC2018.md), handoff e roadmap aggiornati. Prossime D2C/E/C3/C4/D3, GUI Avalonia soltanto in P3.

## 25 ottobre 2026 — P0-D2A: compressione sezionale pura NTC 2018 (nuovi xUnit .NET da confermare)

- Conferma dell'autrice: **tutti i test .NET del FULL D0M8 e precedenti PASS**, senza log numerici; nuovo gate D2A non ancora eseguito su Windows.
- Nuova regola `Ntc2018SteelPureCompressionResistanceRuleD2A` di **sola resistenza sezionale** sotto N di compressione pura, M=V=0 allo stesso FEM target/stazione/lato, IPE/HEA/HEB nominali non forate, grado e fonti esterne dichiarati. Classificazione NTC pura D0 classe 1–3; `Nc,Rd=A fyk/γM0`, `γM0=1,05` dal DM 17/01/2018 §4.2.4.1.2.2 [4.2.9]–[4.2.10] e Tab.4.2.VII. Classe 4 `NotApplicable` senza Aeff; no fallback EC3.
- Nuovi xUnit `D2/Ntc2018SteelPureCompressionD2ATests.cs` con classi 1/2/3, fixture sintetico per classe 4, valori di soglia, provenienza negativa, confronto dell'area dichiarata entro lo 0,1%, fori, profilo e gamma, incompletezza di stabilità d'asta. Golden Python indipendenti `p0d2a` **852/852 PASS**; regressione R2–D2A **1.557/1.557 PASS su 12 script** (solo calcolo matematico, non .NET).
- Nessun motore FEM/Core, catalogo OPPO, classificatore D0/D0M1–D0M8, EC3, legno, UI o coefficienti normativi esistenti alterati. **Questo PASS non certifica la membratura né l'opera**. GUI C#/Avalonia sempre P3, dopo analisi e verifiche normative.

## 25 ottobre 2026 — P0-D0M8: α=0, regioni elastiche e limite dichiarato della classificazione NTC (D0M7 .NET PASS, D0M8 .NET pending)

- **Conferma dell'autrice:** «tutti i test passati» sul FULL **D0M7**, senza log numerici allegati; da questo momento D0M7 e precedenti **GATE PASS confermati**, D0M8 ancora da testare.
- Riletta la **NTC2018 §4.2.3.1 Tab.4.2.III** sul ramo plasticamente teso α≤0,5. Poiché i limiti di classe 1/2 dell'anima sono `36ε/α` e `41,5ε/α`, l'estremo **α=0 non è valutabile per divisione per zero**: nessuna classe plastica o idoneità assegnata per continuità arbitraria.
- Nuovo diagnostico `Ntc2018SteelTensionDominantElasticZonesD0M8`: per il solo I/H nominale IPE/HEA/HEB simmetrico non forato con **N>0 di trazione e M forte≠0, V=0**, PNA nominale α=0 provato da D0M6, gross A/I/fonti/edizione/grade e stessa stazione FEM verificati, confronta tensioni elastiche nominali di primo ordine agli **estremi della sezione** e ai **bordi dell'anima piatta**. Ritorna tre distinzioni: tutto teso, compressione in ala-raccordo, compressione che raggiunge l'anima. Nell'ultimo caso richiama D0M3 con ramo `ψ<−1` e relativa nota NTC. Con confini quasi nulli, prove incomplete o condizioni non coperte restituisce `Unsupported`.
- **232/232** nuovi golden aritmetici indipendenti PASS sui **30** profili del catalogo, regioni di compressione elastica, momento positivo/negativo, condizioni della nota e negativi; **705/705** regressioni Python totali R2–D0M8 PASS; nuovi xUnit D0M8 **PENDING** perché SDK .NET non disponibile qui. Non modifica D0M1–D0M7, FEM, EC3, legno o G8B. **Nessuna classe 1–4 definitiva, resistenza di sezione, interazione, instabilità, verifica d'asta/struttura o copertura combinazioni.** Rapporto `P0_D0M8_DIAGNOSTICA_ALPHA_ZERO_ZONE_ELASTICHE_NTC2018.md`. Prossimo **D2A** resistenza NTC acciaio in ambito dimostrabile, eventuale D0M9 α=0 solo con ulteriore fondamento, poi E/C3/C4/D3, GUI ultima P3.

## 25 ottobre 2026 — P0-D0M7: classificazione NTC mista con α=1 comprovato (D0M6 .NET PASS, D0M7 .NET pending)

- **Nuova conferma dell'autrice:** «tutti i test passati» per il FULL precedente **D0M6** con regressioni; D0M6 e tutti i gate precedentemente confermati diventano **PASS su dichiarazione dell'utente** (senza log dettagliati). Non estendere tale esito a D0M7.
- Nuovo servizio indipendente `Ntc2018SteelMixedCompressionDominantClassifierD0M7`, **senza modificare D0M1–D0M6**. Classificazione NTC2018 §4.2.3.1 Tab.4.2.III/IV per **una singola sezione** IPE/HEA/HEB simmetrica, non forata e documentata, con **N<0, M forte≠0 e V=0 concomitanti**. D0M6 deve provare PNA plastico oltre l'anima piatta **dal lato teso** (`α=1`) e D0M3 deve provare rapporto elastico `−1<ψ<1` da gross A/I. Anima: limiti `33ε`, `38ε`, `42ε/(0,67+0,33ψ)`; ala: `9/10/14ε`; classi separate e massima di sezione. **α=0/tensione dominante resta `Unsupported`**, così come evidenze mancanti, geometrie fuori dominio e altre azioni.
- Restano esclusi resistenza/azione combinata N/M/V, classi 4 con proprietà efficaci, instabilità, verifica di membratura/opera ed esaustività di stazioni/combinazioni. Nuovi xUnit e **131/131 golden aritmetici indipendenti PASS**, nuovo gate .NET **PENDING** per assenza SDK locale. Guida `docs/structural/P0_D0M7_CLASSIFICAZIONE_COMPRESSIVA_ALPHA_UNO_NTC2018.md`. Prossimo D0M8 analisi α=0 o chiusura `Unsupported`, poi D2/E/C3/C4/D3; GUI sempre P3.

## 25 ottobre 2026 — P0-D0M6: geometria plastica nominale raccordi/ali (gate .NET D0M5 PASS, D0M6 in attesa)

- **Nuova conferma dell'autrice:** «tutti i test passati» per il checkpoint completo D0M5 e le regressioni eseguite su Windows. Da questo momento C2B/C2C0/C2C1/D1/D0/D0M1–D0M5 risultano **GATE PASS per conferma dell'utente**, senza dettagli numerici del log. Non trasferire tale conferma ai nuovi test D0M6.
- Aggiunto `Ntc2018SteelPlasticNeutralAxisCalculatorD0M6` per una **diagnosi geometrica** dell'asse neutro plastico anche dentro i raccordi circolari nominali e le ali IPE/HEA/HEB. Equilibrio `Acompressa=(Anom−1000N/fyk)/2`, area modello `Anom=2btf+tw(h−2tf)+4r²(1−π/4)`, bisezione in cinque regioni; area pubblicata OPPO confrontata con soglia nominale di arrotondamento 0,1%, **non** tolleranza di prodotto. Tutti i **30** profili esaminati concordano con scarto massimo sotto 0,05%.
- Mantiene il D0M4 come controprova dell'α dentro l'anima piatta, segnala α geometrico 0/1 quando l'asse la abbandona, **ma D0M5 non è esteso arbitrariamente alla classificazione normativa con α ai limiti**. Restano fuori Mpl, interazione N/M/V, verifica resistente, instabilità, certificazione geometrica, aste/struttura. Nuovi xUnit e **63/63 golden numerici indipendenti PASS**; suite .NET D0M6 **PENDING** senza SDK qui. Rapporto `P0_D0M6_ASSE_NEUTRO_PLASTICO_RACCORDI_ALI_NTC2018.md`; prossimo D0M7 qualificazione dei nuovi rami con fonti/prove, poi D2/E/C3/C4/D3, GUI solo P3.

## 25 ottobre 2026 — P0-D0M5: composizione automatica α plastico + ψ elastico per classi I/H NTC2018 (gate .NET in attesa)

- Completato il raccordo conservativo nella **singola sezione** FEM concomitante N/M forte (V=0) con modello I/H laminato simmetrico non forato, PNA strettamente dentro anima piatta, A/I/grade/fonti e appartenenza edizione NTC2018 attestati. `Ntc2018SteelMixedAutomaticSectionClassifierD0M5` ricalcola α D0M4 e ψ D0M3, riconcilia tutti i riferimenti/valori e richiama **le tabelle NTC già codificate nel classificatore D0M1/D0M2**, senza duplicare formule: classi 1–4 di anima/ala, classe governante e necessità di proprietà efficaci per la classe 4.
- **Correzione normativa compatibile D0M1:** prima alternativa della nota Tab.4.2.III `ψ<−1`, `σc≤fyk`, ora accetta prova delle tensioni **senza strain/E fittizi**; seconda alternativa `εt>fyk/E` continua a esigere prova esterna. Doppie evidenze, stress discordanti, profili incompleti, N/ψ incompatibili o instabilità della classificazione falliscono in modo chiuso. Vecchio contratto D0M2 e sue prove mantenuti.
- xUnit nuovi D0M5 e golden esatti indipendenti **49/49 PASS**; regressioni Python R2–D0M5 **279/279 PASS** ma test C#/.NET **NON eseguiti** (SDK indisponibile), D0M5 **PENDING Windows** come C2B/C2C0/C2C1/D1/D0/D0M1/D0M2/D0M3/D0M4. Una classe 1 NON prova resistenza o adeguatezza della sezione/membratura. FEM/EC3/legno/G8B/GUI invariati. Rapporto `P0_D0M5_CLASSIFICAZIONE_AUTOMATICA_MISTA_IH_NTC2018.md`; roadmap D0M6→D2/E/C3/C4/D3, Avalonia solo P3.

## 25 ottobre 2026 — P0-D0M4: α plastico NTC dall'equilibrio assiale con PNA nell'anima piatta (gate .NET in attesa)

- Confrontata la distribuzione plastica della Tab.4.2.III NTC 2018 con la geometria nominale OPPO; calcolata `α=0,5−1000N/(2·fyk·tw·c)`, `c=h−2(tf+r)`, **esclusivamente** per I/H laminati simmetrici non forati con **asse neutro plastico strettamente nel tratto piano dell'anima** (`|N|<fyk·tw·c/1000`, banda numerica di sicurezza), N/M forte FEM concomitanti e V=0. Il contributo dei raccordi cancella per simmetria nel bilancio di area e NON viene approssimato come rettangolo; se l'asse neutro entra nei raccordi o nelle ali, `Unsupported`.
- Nuovo diagnostico indipendente `Ntc2018SteelMixedPlasticWebAlphaCalculatorD0M4`, con gross A/grade/geometria/provenienza documentati, OPPO **indicativo non certificato** e flag espliciti che negano classe definitiva, ψ, resistenza e idoneità. Il blocco ±fyk non è la reale distribuzione elastica e **non** dimostra interazione N/M né il momento resistente corrispondente al momento FEM. D0M1/D0M2/D0M3, FEM, EC3, G8B e altri moduli invariati.
- Nuovi xUnit `D0/Ntc2018SteelMixedPlasticWebAlphaD0M4Tests.cs` positivi/negativi, inclusa coerenza degli identificativi con ψ D0M3 per lo stesso IPE200; **PENDING** .NET. Script razionale indipendente **56/56 PASS** (IPE200/IPE300, simmetria, bordi e bilancio esatto), non confronto C#↔Python. Rapporto `docs/structural/P0_D0M4_ASSE_NEUTRO_PLASTICO_ALPHA_NTC2018.md`. Prossimo D0M5 per raccordare α/ψ e classificazione nel sottoinsieme realmente provato; poi D2/E/C3/C4/D3; GUI ultima P3.

## 25 ottobre 2026 — P0-D0M3: ψ elastico NTC derivato dal FEM, solo schermo dell'anima (gate .NET in attesa)

- Aggiunto **solo** `Ntc2018SteelMixedElasticWebScreenerD0M3`, indipendente da EC3 M90B. Dalla stessa stazione FEM N/M forte e gross A/I con fonte documentata ottiene `σc`, tensione all'altro bordo e **ψ elastico**; applica Tab.4.2.III NTC 2018 per limite di classe 3 dell'anima, con nota obbligatoria su ψ<−1 (`σc≤fy` oppure `εt>fy/E`). Limiti di plausibilità geometrica, materiale/prodotto, provenienza FEM, segno N e dati mancanti verificati; profili OPPO IPE/HEA/HEB **non certificati**.
- Risposta limitata a **soglia elastica classe 3** e bound web 3/4, senza ricavare **α plastico**, classi 1/2, classe finale I/H, proprietà efficaci, resistenza o idoneità d'asta. Preesistenti D0M1/D0M2, FEM, EC3, G8B, Core, CLI e integrazioni non modificati.
- Nuovi xUnit `D0/Ntc2018SteelMixedElasticWebScreenD0M3Tests.cs` positivi/negativi, inclusa controprova OPPO IPE200, **non eseguiti** senza SDK; golden indipendenti razionali **40/40 PASS**. Guida `P0_D0M3_SCHERMO_ELASTICO_PSI_NTC2018.md`. Gate C2B/C2C0/C2C1/D1/D0/D0M1/D0M2/D0M3 **PENDING**. Prossimo D0M4 modello plastico α verificato, poi D2/E/C3/C4/D3; GUI solo P3.

## 25 ottobre 2026 — P0-D0M2: ramo ψ ≤ −1 con nota NTC2018 obbligatoria (gate .NET in attesa)

- Controllata **visivamente** la formula NTC §4.2.3.1 Tab.4.2.III `62ε(1−ψ)√(−ψ)` per il limite di **classe 3 dell'anima** con ψ≤−1 e l'asterisco relativo: `σcompressiva≤fyk` **oppure** deformazione a trazione `εt>fyk/E`. Non usata l'espressione del ramo ψ>−1 fuori dal proprio dominio.
- Esteso **solo** `Ntc2018SteelMixedIClassifierD0M1` con record opzionale `Ntc2018SteelMixedPsiBelowMinusOneEvidenceD0M2`: due tensioni elastiche ai bordi (verifica `ψ=−σt/σc`), strain/E/fonte; senza prove complete il ramo resta `Unsupported`. **Correzione fisica essenziale:** I/H simmetrica con ψ<−1 esige N FEM **positivo di trazione**, α<0,5; N negativo è incongruente, mentre ψ=−1 è il limite di flessione pura D0. Preservato ramo D0M1 N<0/ψ>−1, ali e limite resistenza/membro.
- Nuovi xUnit `Ntc2018SteelMixedPsiBelowMinusOneD0M2Tests.cs` con limiti tabellari ψ=−1/−2/−4/−9 (ψ=−1 solo nel golden aritmetico), due alternative della nota, segno N, compatibilità α, coerenza delle tensioni e valori non finiti. Nuovo golden Python a **frazioni razionali esatte e radici delimitate 33/33 PASS**; **C#/.NET non compilato**, gate D0M2 **PENDING**, come C2B/C2C0/C2C1/D1/D0/D0M1. Guida `P0_D0M2_PSI_MENO_UNO_NTC2018.md`, handoff e roadmap aggiornati.
- Nessuna deduzione automatica di tensioni/deformazioni, α/ψ, Aeff/Weff, resistenza N/M/V o idoneità di asta. FEM, EC3, D1/D0 puri, G8B e precedenti golden/test preservati. GUI solo P3.

## 25 ottobre 2026 — P0-D0M1: classificazione nativa NTC 2018 di sezioni I/H sotto N+M (gate .NET in attesa)

- Verificati visivamente sulla fonte ufficiale NTC 2018 §4.2.3.1, Tab.4.2.III/IV i **due rami di α plastico** per classi 1/2 e il **ramo ψ elastico >−1** per classe 3 delle anime soggette a N+M; ali laminate 9/10/14ε. Nuovo servizio `Ntc2018SteelMixedIClassifierD0M1`, separato dai moduli di classificazione pura D0, trazione NTC D1 e verifica EC3 M89–M91.
- Per soli I/H laminati ad ali parallele, non forati, con N<0, M forte≠0, V=0 **concomitanti nella stessa stazione/lato FEM**. Necessarie evidenze dichiarate **distinte** su distribuzioni plastiche/elastiche, α/ψ, grade/fy e geometria; il software non ricostruisce autonomamente α/ψ né attesta la veridicità delle evidenze esterne. Rifiuta domini non coperti, soglie non ordinate, ψ≤−1, geometrie impossibili e provenienze incomplete.
- Nuovi xUnit `D0/Ntc2018SteelMixedClassificationD0M1Tests.cs` e golden Python `benchmarks/independent/p0d0m1/verify_p0d0m1_mixed_alpha_psi.py`, **26/26 calcoli aritmetici esatti PASS**; nessuna compilazione .NET locale, **P0-D0M1 PENDING Windows**, come C2B/C2C0/C2C1/D1/D0. Nessuna resistenza, interazione normativa N/M/V, proprietà efficaci classe 4 o giudizio di membro `Passed`.
- Preservati FEM/G8B/precedenti moduli e test; documentati perimetro, gate e roadmap. Guida `docs/structural/P0_D0M1_CLASSIFICAZIONE_MISTA_NM_NTC2018.md`. GUI solo P3.

## 25 ottobre 2026 — P0-D0: classificazione nativa sezioni acciaio NTC 2018 (gate .NET in attesa)

- Rilette **visivamente** le Tabelle NTC 2018 4.2.III/IV, §4.2.3.1, e confrontate con M90A EC3 e i casi didattici Bernuzzi già presenti. Creato `Ntc2018SteelISectionClassifierD0`, separato per identità/fonte dal vecchio classificatore EC3, valido soltanto per IPE/HEA/HEB non forati e laminati ad ali parallele, in pura compressione o flessione forte. Identifica classi 1–4 con ε=√(235/fyk) e classe governante del piatto più snello secondo le tabelle, senza inventare una resistenza.
- Input geometrici/materiali e prove obbligatorie; INP ad ali inclinate, N+M combinato, asse debole, saldati e fori **non classificati da questo ramo**. Classe 4 marcata `RequiresEffectiveSectionProperties` ma senza Aeff/Weff; nessun `Passed` di sezione resistente o membro.
- Nuovi xUnit `D0/Ntc2018SteelISectionClassificationD0Tests.cs` con soglie, classi limite, casi catalogo, errori/provenienza e isolamento EC3. Nuovo script indipendente Python **25/25 PASS**. Nessuna compilazione .NET locale, gate D0 **PENDING**, come C2B/C2C0/C2C1/D1. Conservati i PASS confermati precedenti.
- Handoff, roadmap, messaggio ripresa e manifest aggiornati. FEM, G8B, M89–M91, D1, golden preesistenti e README invariati. GUI solo P3. Guida `docs/structural/P0_D0_CLASSIFICAZIONE_SEZIONI_ACCIAIO_NTC2018.md`.

## 25 ottobre 2026 — P0-D1: primo verificatore nativo NTC2018 per sola trazione acciaio (gate .NET in attesa)

- Regole nuove e separate da EC3: `Ntc2018SteelPureTensionResistanceRuleD1` (§4.2.4.1.2.1, [4.2.5–7]) calcola lo snervamento lordo e, se fori di collegamento documentati, anche la **rottura netta** con `0,9·Anet·ftk/γM2` e governa il minimo; `Ntc2018SteelTensionCapacityHierarchyRuleD1` controlla separatamente [4.2.8] quando applicabile.
- Profilo e coefficienti normativi **bloccati** a NTC 2018, Tab.4.2.VII, per membrature ordinarie: `γM0=γM1=1,05`, `γM2=1,25`. Evidenze esplicite per geometria, prodotto, fori/Anet/ftk, trazione concentrica e limiti del FEM 2D, membro/analisi/combinazione; rami misti/ignoti restituiscono `NotApplicable`/`MissingData`. **Mai idoneità globale di asta/giunto.**
- Nuovi xUnit `D1/Ntc2018SteelTensionRulesD1Tests.cs` positivi e negativi: gross/net, controesempio `630 kN`, mancanza dati, identità, gamma, provenance, gerarchia e scope. **Non eseguiti** per assenza SDK .NET; gate D1 **PENDING**. Golden aritmetici indipendenti Python **16/16 PASS**. Rapporto in `docs/structural/P0_D1_TRAZIONE_ACCIAIO_NTC2018.md`.
- FEM, Core, integrazione C2C1, vecchie regole EC3, G8B, benchmark storici e README invariati. Gate utente G7/B2/B3/B4/C1 FIX1/C2A confermati in perimetro; C2B/C2C0/C2C1/**D1** ancora da eseguire. Roadmap/handoff/manifest aggiornati, GUI Avalonia soltanto P3.

## 25 ottobre 2026 — P0-R2: audit acciaio NTC 2018/Circolare, sezioni e stabilità (nessun nuovo gate .NET)

- Letti NTC §§4.2.3–4.2.4, tabelle di classificazione/resistenza/SLE e Circolare C4.2.3/C4.2.4; confrontati con le regole effettive EC3 M89–M91 e relativi test. Nuovo rapporto `docs/structural/P0_R2_AUDIT_ACCIAIO_NTC2018_SEZIONI_E_STABILITA.md`: **24 riscontri** con perimetro, lacune e priorità P0-D/E/C.
- Corrispondono **solo nei casi I/H puri confrontati** le soglie c/t di classe; M89–M91 restano EC3, **non NTC steel**. Gap critici: frattura netta con fori e `γM2`, gerarchia ove pertinente, resistenza classe 4, rami N+M+V e taglio elevato, pressoflessione instabile, sensibilità globale αcr/ritegni, SLE dell'acciaio e vibrazioni.
- Aggiunto benchmark indipendente `benchmarks/independent/p0r2/verify_p0r2_ntc_steel_sections.py`: **34/34 aritmetici e frontiere PASS** con frazioni esatte, senza richiamare C#. Controesempio documentato: 650 kN supera rottura netta (622,08 kN) ma non lo snervamento lordo (671,43 kN). Nessuna funzione NTC acciaio implementata o millantata.
- Handoff, roadmap, messaggio nuova chat e manifest aggiornati; README, C#, xUnit, benchmark storici e G8B invariati. **P0-C2B/C2C0/C2C1 ancora PENDING** Windows; GUI soltanto P3.

## 25 ottobre 2026 — P0-R1: audit normativo approfondito SLE legno G5, precondizioni G1/G3/G4 (nessun nuovo gate .NET)

- Confrontati direttamente NTC §4.4.7/Tab. 4.4.V e Circolare C4.4.7, Eq. [C4.4.3], con codice e test G5: `kdef` per classi 1/2/3, maggiorazione per posa umida, coeff. ψ, deformazione rara e finale, controfreccia, limiti indicativi `L/300` e `L/200` e doppia proiezione delle mensole risultano coerenti **nel perimetro ristretto**.
- Aggiunto `docs/structural/P0_R1_AUDIT_SLE_LEGNO_E_PRECONDIZIONI.md` (14 riscontri) e un riferimento matematico indipendente Python a frazioni esatte: **12/12 aritmetici PASS**, senza esecuzione C# o nuove dichiarazioni di conformità dell'asta.
- Individuato un **gap di automazione**, non errore di formula: Circolare C4.4.4 richiede neve sopra 1000 m almeno di media durata e sotto almeno breve; G1 richiede classificazione manuale/manifesto ma non lega in modo tipizzato quota, neve e durata. Restano esterne la prova di ritegni e stabilità G4, gli effetti reali dei giunti, i massimi SLE, il superamento della soglia 30% di flessione permanente e il comfort vibratorio degli impalcati.
- Nessun difetto algebrico G5 dimostrato in questo controllo; **nessuna modifica** a codice .NET, test xUnit, FEM, benchmark storici o README. **C2B/C2C0/C2C1 PENDING**; aggiornati handoff, roadmap, messaggio di ripresa e manifest.

## 25 ottobre 2026 — P0-R0: confronto documentato con NTC 2018 e Circolare 7/2019 (nessun gate .NET)

- Esaminati direttamente i PDF NTC 2018 e Circolare 7/2019 consegnati dall'utente: identità, SHA-256, clausole e riscontri nel codice registrati in `docs/structural/P0_R0_REGISTRO_FONTI_NTC2018_CIRCOLARE2019.md` (16 voci con stati e priorità).
- Confrontati puntualmente i coefficienti **γ/ψ** delle combinazioni NTC, **γM/kmod** del legno, requisiti di sezione netta per trazione legno e coefficienti `kcr` del taglio; coincidenze specifiche non equivalgono a verifica normativa completa dei moduli.
- **Gap prioritario acciaio:** NTC Tab. 4.2.VII prescrive `gammaM0=1,05`, `gammaM1=1,05` e `gammaM2=1,25` per le condizioni citate; il pilota **EC3** C2C1 con `gammaM0=1,00` non è una verifica NTC. Per sezioni forate manca il controllo di rottura netta e restano necessari dati e verifiche sui collegamenti.
- Segnalati audit formula-per-formula ancora aperti su G3/G4/G5, EC3/EC5 con fonti EN e annessi, esaustività delle azioni e limiti fisici. **P0-C2B/C2C0/C2C1 PENDING**: nessuna esecuzione .NET, nessuna promozione di gate, nessuna modifica a C#, golden, modelli G8B o README. Aggiornati handoff, roadmap e messaggio di ripresa.

## 25 ottobre 2026 — P0-C2C1: primo caso reale EC3, trazione lorda pura costante (gate Windows in attesa)

- L'utente non può ancora testare: **C2B, C2C0 e il nuovo C2C1 sono PENDING**. G7 e i gate precedentemente confermati conservano il loro stato nei rispettivi perimetri.
- Nuovo `FemSteelGrossTensionMaximumC2C1` in Verification.Integration: usa il FEM lineare reale e il `DesignCheckEngine` con la regola EC3 M89-B **già implementata**, limitatamente allo snervamento della sezione lorda in trazione pura. Nessuna modifica alle equazioni normative esistenti, al motore FEM o ai modelli G8B.
- Domanda costante e pura dimostrabile dalla legge interna del `Frame2D` **solo quando non esistono carichi sul membro/selfweight e N è positivo e costante con V=M esattamente zero** ai due estremi. `Failed` osservato conservato; dati mancanti, momenti, taglio e carichi fuori perimetro → `ScreeningIncomplete`. **Sempre esclusi** net fracture, collegamenti, completezza delle azioni e idoneità dell'asta; nessun limite numerico intervallare certificato.
- Nuovi xUnit C2C1 e script aritmetico `benchmarks/independent/p0c2c1/verify_p0c2c1_gross_yield.py`: **3/3 golden Python superati** (200/400/700 kN contro 669,28 kN nominali); test C# **non compilati** in assenza SDK. Guida `P0_C2C1_PILOTA_EC3_TRAZIONE_LORDA.md`. README invariato. Prossimi gate Windows C2B/C2C0/C2C1 e G7; poi C2C2/C3/C4/E/D. GUI solo P3.

## 25 ottobre 2026 — P0-C2C0: audit conservativo dei report delle regole, senza gate .NET

- L'utente non può eseguire i test ora: **P0-C2B resta esplicitamente PENDING**. Proseguito su un contratto C2C0 indipendente dal suo risultato, senza promuovere i massimi sintetici a utilizzi normativi.
- Nuovo `NormativeRuleStationEnvelopeAuditorC2C0` in Verification.Integration: un solo membro, regola, edizione/annesso, target FEM e profilo/coefficienti; controlla ascissa/lato, identificatori, unità, clausola, domanda, resistenza e coerenza `Utilization`. Conserva `ObservedRuleFailure` anche se altre stazioni sono `MissingData`/`Unsupported`; **un insieme di Passed rimane `ScreeningIncomplete`**, mai membro conforme.
- Nuovi xUnit con report del vero `DesignCheckEngine` e regola sintetica soltanto per test; controprove su mismatch target/edizione/coefficienti, risultati manomessi, limiti dei salti, mancanti e duplicati. Nessun motore FEM o formula EC3/EC5/NTC modificata; nessun SDK .NET locale, **C2C0 PENDING**.
- Nuovo documento `P0_C2C0_CONTRATTO_MASSIMI_NORMATIVI.md` con matrice per regole EC3 M89–M91, EC5 M92 e NTC G2–G5; prossimo C2C1 su una vera regola singola strettamente supportata, dopo i gate. README invariato.

## 25 ottobre 2026 — P0-C2B: ricerca di estremi di indici sintetici su diagrammi polinomiali (gate Windows in attesa)

- Ricevuta la conferma **«test passati»** per P0-C2A: **GATE C2A PASS** nel suo perimetro diagnostico affine, senza contatori allegati.
- Nuovo `FemPolynomialDiagnosticExtremaC2B` solo in `Verification.Integration`: `N/V` di grado al più 2, `M` di grado al più 3 con distribuzioni uniformi/lineari, peso proprio, concentrazioni e combinazioni negative. Quattro sezioni per ricostruire i polinomi, altre tre per la verifica; salti con lati separati.
- Massimo dell'indice somma-abs da otto combinazioni di segni e loro stazionari; prodotto assiale–momento di grado fino a 5 con isolamento ricorsivo delle radici della derivata. Nuovi xUnit con golden manuali per massimo interno `2,25` (carico uniforme), stazione `4/√3` (triangolare), peso proprio, radici multiple, salti e fattori negativi.
- **NESSUNA formula normativa, idoneità dell'asta, completezza di combinazioni o certificazione di errore numerico**; risultato conservativo `ScreeningIncomplete` in caso di incoerenza. SDK .NET non installato: C2B da collaudare su Windows. Core/Analysis/G8B/golden storici/README invariati.

## 25 ottobre 2026 — P0-C2A: estremi analitici di metriche diagnostiche affini (gate Windows in attesa)

- Conferma dell'utente **«test passati»** per C1 FIX1: segnato **P0-C1 GATE PASS** senza log numerico della nuova esecuzione; i 527/528 del precedente FIX0 restano storico.
- Nuovo componente separato `FemAffineUtilizationExtremaC2A` in Verification.Integration: sui **soli** Frame2D con tratti a sollecitazioni affini, massimizza due indicatori matematici (`|N|/Ns+|V|/Vs+|M|/Ms` e `|(N/Ns)(M/Ms)|`), conservando i due lati di carichi puntuali. Sum-abs convessa → estremi; prodotto quadratico → estremi + stazionari analitici.
- Carichi uniformi, linearmente distribuiti e peso proprio del modello attivi → **`ScreeningIncomplete`**, nessun massimo. Indicatori/scale **NON sono formule/resistenze EC3/EC5/NTC**: nessun risultato di idoneità di membro, struttura o norma.
- Test xUnit nuovi con golden manuale P=12 kN a L/2, N=-9 kN, `ηmax=3` su entrambe le facce del punto; combinazioni con fattori negativi, più salti, matematica prodotto con massimo interno, rigetti distribuiti/selfweight/truss/invalidi. SDK .NET assente qui, **gate Windows C2A richiesto**; Core/Analysis, esempi G8B, vecchi test/golden e README invariati.

## 25 ottobre 2026 — P0-C1 FIX1: corregge test sul lato del carico puntuale (gate da ripetere)

- Analizzato log Windows: compilazione Release **0 avvisi/0 errori**; 3/3 Analysis G7, 3/3 integrazione G7, **527/528** Verification. Unico errore: test nuovo `AContinuousSectionAtAConcentratedMemberLoad_CannotBeValidated` attendeva `Invalid` ma otteneva `MatchedDeclaredStations`.
- Causa: il test modificava la stazione **indice 0, x=0, già Continuous**, anziché la **indice 1, x=2 m, BeforePointLoad**. La simulazione M87-FEM rifiuta già correttamente un punto di salto richiesto come `Continuous`: nessun difetto riscontrato nel solver o nell'auditor.
- Corretto il test per selezionare il salto e aggiunte asserzioni di geometria/lato e una regressione positiva per la sezione continua a x=0. **Soltanto file test + documentazione/manifest modificati**. Nessun golden, FEM, Core/Analysis, C1 produzione, G8B o README alterato.
- Gate Windows **P0-C1 FIX1 in attesa**; non dedurre `Passed` da una compilazione riuscita o dalla suite parziale.

## 25 ottobre 2026 — P0-C1: audit terne FEM concomitanti (gate Windows in attesa)

- L'utente ha confermato **«test passati»** per P0-B4: registrato **P0-B4 GATE PASS** senza log numerico allegato; nessuna promozione dell'esito a completezza normativa globale.
- In `Verification.Integration` aggiunto `FemConcomitantStationAuditorC1`: piano finito di stazioni/casi/combinazioni/lati/regole/edizione e controllo delle richieste M87-B/C contro il **FEM ricalcolato** una volta per target. Conferma N/V/M firmati **della stessa sorgente** e rifiuta mescolanza degli inviluppi.
- Aggiunti xUnit con combinazioni multiple, fattori negativi, salti V, mancanze, duplicati, manomissioni N/V/M e mutazioni di provenienza; controesempio `η(x)=|x(1−x)|` con massimo interno. **Non prova massimo utilizzo continuo né idoneità dell'asta**.
- Documento `P0_C1_AUDIT_SOLLECITAZIONI_CONCOMITANTI.md`; README invariato, nessun cambio ad Analysis/Core, G8B, benchmark G7 o coefficienti normativi. Nuovi test .NET **in attesa di Windows** per assenza SDK nell'ambiente di assemblaggio.

## 25 ottobre 2026 — P0-B4: audit di piano dichiarato, fingerprint FEM (gate Windows in attesa)

- L'utente ha confermato **«tutti i test passati»** per P0-B3: segnato **P0-B3 GATE PASS**, senza conteggi del log allegato. G7 e B2 restano superati nel proprio perimetro; G8B non diventa un certificato di verifica.
- Aggiunto in Combinations/Audit un piano finito per **un effetto dichiarato**, validator per famiglie, azioni classificate, guide Q, vento alternativo, assenze/favorevolezze motivate; `Capture` congela fattori attesi e SHA-256 di inventario, piano, geometria, sezioni, materiali, vincoli, carichi e termini FEM.
- `Audit` legge **direttamente** `StructuralModel.LoadCombinations`, controlla termini reali, duplicati, assenti o imprevisti e fingerprint cambiati; stati `Invalid`, `Incomplete`, `Unsupported`, `MatchedDeclaredPlan`, **mai completezza normativa o idoneità della membratura**.
- Aggiunti xUnit B4 positivi, scenari vento ± e mutazioni negative; manuale `P0_B4_AUDIT_INSIEME_DICHIARATO.md`. Python golden P0-B1 riconfermato **14/14 + 10/10**. **SDK .NET assente in preparazione: i test B4 sono nuovi e in attesa di gate Windows.** Solver Core/Analysis, G8B, README e benchmark immutati; manifest FULL SHA256 rigenerato.

## 25 ottobre 2026 — P0-B3: generatore NTC di combinazioni candidate (gate Windows in attesa)

- Ricevuta conferma dell'utente «test passati» relativa al checkpoint P0-B2: registrato **P0-B2 GATE PASSED** per le prove richieste, senza conteggi/trace del log. Conservata la distinzione dal completamento normativo e dal confronto G8B.
- Implementati Ntc2018BuildingActionCoefficients (γ STR A1 e tutte le 12 righe determinate ψ NTC 2018) e Ntc2018BuildingCombinationGenerator (ULS STR A1, SLS rara/frequente/quasi permanente) in Combinations, senza dipendenze inverse dal FEM.
- Input obbligatorio di scenario/effetto e scelta favorevolezza documentata; alternative incompatibili rifiutate, termini zero omessi nel manifesto, omissioni fisiche/non selezionate distinte. Ogni risultato resta un **candidato, FullCombinationSetAudited=false**.
- Aggiunti xUnit B3 che leggono **senza modifiche** i 14 golden P0-B1 copiati nell'output test e altri casi negativi; documentato P0_B3_GENERATORE_COMBINAZIONI_CANDIDATE. README e modelli FEM invariati. **B3 non compilato/testato qui**: SDK .NET assente, gate Windows richiesto prima di P0-B4.

## 25 ottobre 2026 — P0-B2: modulo C# azioni tipizzate (gate Windows in attesa al momento della consegna)

- Aggiunto StructuralSolver2D.Combinations (.NET 8, sola dipendenza verso Core): ActionDefinition G1/G2/Q, categorie ψ NTC 2018, base caratteristica, evidenze, alternative vento±, durata legno opzionale e deroga G2 motivata.
- Aggiunti ActionInventoryValidator (stati Valid/Incomplete/Unsupported/Invalid, codici B2_*) e ActionInventoryJsonSerializer con JSON autonomo, enum stringa e rifiuto campi sconosciuti. Nessuna generazione SLU/SLE né modifica alla semantica della CLI o dei fattori manuali.
- Integrato il progetto nella soluzione e referenziato da Core.Tests; nuovi xUnit per caso positivo, round-trip, omissioni, duplicati, categoria I/K, neve senza quota, gruppi e regressione fattori zero/negativi. Documento tecnico P0_B2_CONTRATTO_AZIONI_E_VALIDAZIONE.md.
- SDK .NET non disponibile nell'ambiente di preparazione: **nessuna compilazione o esecuzione xUnit dichiarata**. P0-B1 Python confermato; G7 ancora ultimo gate Windows storico confermato. Handoff/roadmap/manifest aggiornati, README immutato. Prima il gate Windows B2/G7, poi P0-B3.

## 25 ottobre 2026 — P0-B1: specifica NTC 2018 e golden indipendenti (non gate .NET)

- Consultata la fonte primaria D.M. 17/01/2018: §2.5.3, Tab. 2.5.I, §2.6.1 Tab. 2.6.I. Specificato il profilo delimitato SLU STR A1 per edifici correnti non geotecnici e SLE rara/frequente/quasi permanente; EQU/GEO/sismici/eccezionali esclusi.
- Aggiunto docs/structural/P0_B1_CONTRATTO_AZIONI_NTC2018.md: identità e categoria azione, guide/accompagnatrici, vento e alternative, favorevolezza rispetto all'effetto, gamma/psi e omissioni tracciate senza termini zero nel FEM legacy.
- Aggiunti benchmarks/independent/p0b: **14/14 golden manuali e 10/10 controlli negativi superati** dal verificatore Python Decimal, senza dipendenze esterne; nessun confronto con generatore C# ancora inesistente.
- Aggiornati handoff, roadmap, messaggio di ripresa e manifest SHA256; README invariato. Nessuna modifica a sorgenti/test .NET, quattro modelli G8B o benchmark esistenti; **G7 resta l'ultimo gate Windows confermato**. Segue P0-B2, poi B3/B4.

## 25 ottobre 2026 — P0-A: audit tecnico documentale completato

- Esaminati sorgenti e test reali del FEM, carichi/combinazioni, bridge, EC3/EC5/NTC, cataloghi, reporting e G8B. Creati i tre documenti P0_A_AUDIT_TECNICO, P0_A_MATRICE_COPERTURA e P0_A_PRIORITA_E_CRITERI_DI_TEST in docs/structural, con stato, rischio, prove e ticket P0-B/C/E/D.
- Gap prioritari: generatore SLU/SLE assente; fattore zero rifiutato come termine FEM ma conservabile nel manifesto G1; massimo di utilizzo non garantito dalle stazioni candidate; verifiche parziali; ritegni e collegamenti fisici esterni.
- Ricalcolati nel riferimento Python/NumPy G8B **16/16 scenari** senza discrepanze nei campi di spostamenti, reazioni e risultanti rispetto al dataset. Nessun confronto C#↔Python o nuovo gate .NET (SDK assente).
- Aggiornati handoff, roadmap, piano e manifest. README invariato; nessuna modifica a codice, test, benchmark, modelli, cataloghi, script o VERSION. Prossimo P0-B; Avalonia soltanto P3.

## 25 ottobre 2026 — P0-HANDOFF: preparazione nuova chat e avvio P0-A (solo documentazione)

- Confermato nuovamente dall'utente **`G7 GATE PASSED`** dopo la sequenza G8B FIX2; la conferma riguarda la suite G7, **non** l'esecuzione documentata dei 16 report e del confronto numerico G8B.
- Riscritto `HANDOFF_NUOVA_CHAT.md` con versione, architettura, gate, G8A/G8B, incidenti FIX1/FIX2, regole normative, limitazioni e istruzioni di ripresa.
- Aggiunto `docs/structural/P0_A_PIANO_OPERATIVO.md`: audit del codice e della copertura di FEM/EC3/EC5/NTC, matrice dei gap e criteri per P0-B…E.
- Aggiunto `docs/NUOVA_CHAT_MESSAGGIO.txt` da incollare nella conversazione successiva; aggiornata la roadmap con **P0-A come prossima attività**, senza aggiunte al motore.
- Nessuna modifica a sorgenti, test, benchmark, esempi, cataloghi, script o `VERSION`; rigenerato il manifest SHA-256. **Non è un nuovo gate .NET.**

## 25 ottobre 2026 — G8B FIX2: migrazione sicura dei JSON obsoleti (gate Windows da ripetere)

- Il secondo log Windows ha mostrato gli stessi **due fallimenti CLI**: FIX1 era corretto, ma l'estrazione sopra la cartella G8B precedente non aveva eliminato i vecchi file in `examples/`.
- Aggiunto `scripts/repair-g8b-legacy-examples.ps1`, eseguito **prima della compilazione** da `scripts/run-g7-tests.ps1`: riconosce per struttura JSON soltanto i due file di riferimento obsoleti, li sposta in `.g8b-legacy-backup/` senza sovrascrivere backup e non tocca alcun modello valido. Lo script e' idempotente.
- Nessuna modifica a motore FEM, test C#, benchmark numerici, sezioni, carichi o combinazioni; `README.md`, guida G8B, handoff e manifest aggiornati. Il gate G8B resta **in attesa del nuovo test Windows**.

## 25 ottobre 2026 — G8B FIX1: file numerici separati dai modelli (gate Windows da eseguire)

- Corretto il difetto emerso dai **2 test CLI** del log Windows: due JSON di benchmark analitici in `examples/mixed/g8b-house/` venivano enumerati erroneamente come modelli con nodi/aste.
- Spostati i due JSON e il CSV in `benchmarks/independent/g8b-house/`; aggiornato il generatore Python perché scriva nella nuova destinazione e non ripresenti il problema.
- Aggiornata la documentazione della casetta e l'handoff. **Nessuna modifica** a sorgenti .NET, test C#, quattro modelli di input, coefficienti o benchmark precedentemente validati.
- Log originale: build Release 0 errori/0 avvisi, suite FEM e verifiche superate; CLI 241/243 con 2 fallimenti dovuti esclusivamente ai file non-modello. **FIX1 da ricollaudare su Windows**.

## 24 ottobre 2026 — G8B: quattro modelli casetta e confronto elastico (gate Windows da eseguire)

- Tre varianti controventate con sezioni A leggera, B riferimento e C robusta; quarta variante B priva di X, per isolare l'effetto della controventatura. Geometrie, carichi d'uso e neve/vento identici; peso proprio esplicito diverso con A e sezioni.
- Controventi X `Truss2D` **bilaterali** nel campo 0–5 m; non simulano tiranti a sola trazione, nodi/connessioni o instabilità a compressione.
- Dati CSV/JSON dei pesi propri e delle risultanti; seconda analisi elastica Euler–Bernoulli in Python/NumPy riproducibile, dichiarata **indipendente ed esplorativa**, non gate software.
- Runner `scripts/run-g8b-house-demo.ps1` per 16 report e 2 viewer; guida `docs/structural/examples/G8B_VERIFICA_CASSETTA_E_CONFRONTO.md`; `HANDOFF_NUOVA_CHAT.md` aggiornato. Nessuna modifica a `src/`, test G7, cataloghi o benchmark preesistenti.

## 24 ottobre 2026 — G8A: esempio casetta acciaio-legno (modello dimostrativo)

- Aggiunto l'esempio `examples/mixed/two-storey-steel-house-timber-roof.json`: telaio a 2 piani con colonne a interassi `5 + 2 + 5 m`, interpiano `3 m`, balconi a sbalzo `1,2 m` al primo piano e copertura inclinata in legno.
- Inclusi carichi permanenti, variabili, neve, vento laterale e sollevamento di copertura, con combinazioni manuali `ULS_GRAVITY`, `ULS_WIND_POS`, `ULS_WIND_NEG`, `SLS_RARE`.
- Aggiunta guida `docs/structural/examples/two-storey-steel-house-timber-roof.md` con ipotesi di carico, sezioni preliminari e workflow di dimensionamento.
- Aggiornato `examples/README.md` e il manifest SHA-256. **Nessuna modifica** al motore FEM o ai moduli normativi; collazione utile per mostrare il flusso di lavoro su una struttura realistica.

## 24 ottobre 2026 — G7-DOC1: riordino documentale (solo documentazione)

- Riscritto `README.md` come presentazione e guida d'uso, eliminando avvisi scaduti, duplicazioni e note di rilascio accumulate.
- Consolidato lo stato dei gate fino a **G7 GATE PASSED**; introdotto [`HANDOFF_NUOVA_CHAT.md`](HANDOFF_NUOVA_CHAT.md) come riferimento operativo da mantenere aggiornato.
- Sostituiti i checkpoint storici in cima alla [roadmap](docs/structural/roadmap.md) con priorità reali post-G7; archiviate le versioni precedenti senza perderne la storia.
- Spostate le note di release nella documentazione storica e le guide operative G6/G7 in `docs/structural/`.
- **Nessuna modifica** a `src/`, `tests/`, `benchmarks/`, `catalogs/`, `scripts/`, `examples/` e `tools/`; nuove prove .NET non necessarie per questa operazione documentale, ma manifest aggiornato da verificare sul PC.

## 24 ottobre 2026 — G7 (gate Windows superato)

- Benchmark FEM **B13/B14**: trave con sbalzo, carichi a tratti e reazione negativa sul vincolo bilatero.
- Test integrati reali **G1 → G2/G3 → G6**: coefficienti e risultati normativi con audit di copertura conservativo.
- Manuale di validazione e valori numerici indipendenti: `docs/G7_MANUALE_VALIDAZIONE_INDIPENDENTE.md`, `benchmarks/G7_INDEPENDENT_GOLDEN.json`.
- Il gate superato **non prova** contatto unilaterale, generazione completa delle combinazioni o conformità dell'asta.

## 24 ottobre 2026 — G6 (gate Windows superato)

- Audit dei risultati legno NTC 2018 G2–G5 su piano esplicito di controlli SLU/SLE.
- Differenza obbligatoria tra verifiche **mancanti/non supportate** e **fisicamente non applicabili**; provenienza esatta e prevalenza dei fallimenti.
- Audit acciaio EC3 M91-D indipendente; nessuna falsa conformità complessiva.

## 24 ottobre 2026 — G5 (gate Windows superato)

- SLE legno: frecce istantanee, combinazione rara e deformazioni finali con `kdef`, `ψ0`, `ψ2` e provenienza delle azioni.
- Limiti di deformabilità esplicitamente adottati; verifiche parziali.

## 24 ottobre 2026 — G4 (gate Windows superato)

- Instabilità colonne in compressione pura (due assi) e svergolamento delle travi, con vincoli e tensioni critiche documentati esternamente.

## 24 ottobre 2026 — G3 FIX1 / G3 (gate Windows superato)

- Tensoflessione e pressoflessione NTC del legno, controllo separato del taglio concomitante.
- G3 FIX1 ha corretto una **asserzione xUnit sul testo della traccia numerica**, non le formule di produzione.

## 24 ottobre 2026 — G2 e G1 FIX1 (gate Windows superati)

- **G2:** quattro verifiche pure di sezione rettangolare in legno secondo NTC 2018.
- **G1:** selettore autonomo NTC 2018 (`γM`, `kmod`, `kcr` quando previsto), rigorosamente distinto da EC5.
- **G1 FIX1:** corretto un test che si aspettava `NotApplicable` invece del corretto `Unsupported` per normativa incompatibile.

## 24 ottobre 2026 — Milestone precedenti

- **M92-A/B:** verifiche pure EC5 e selezione coefficienti EC5 — gate Windows superati nel perimetro dichiarato.
- **M89–M91:** moduli parziali acciaio EC3, classificazione sezioni, instabilità e audit; regole con ambito limitato.
- **M86:** consolidamento e validazione del core FEM elastico; le milestone M51–M85 includono analisi statiche, dinamiche e prototipi avanzati delimitati.
- Riferimenti storici: [`CHANGELOG_PRE_G7.md`](docs/archive/CHANGELOG_PRE_G7.md) e [note archiviate](docs/archive/release-notes/).

## Regole per le prossime consegne

Ogni incremento aggiorna **questa cronologia (poche righe)**, [roadmap](docs/structural/roadmap.md) se le priorità cambiano e **sempre** [`HANDOFF_NUOVA_CHAT.md`](HANDOFF_NUOVA_CHAT.md). Lo storico dettagliato resta in un documento tecnico della milestone, non nel README.
