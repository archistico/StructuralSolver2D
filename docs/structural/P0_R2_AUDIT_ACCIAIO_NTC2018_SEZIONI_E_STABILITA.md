# P0-R2 — Acciaio: audit NTC 2018/Circolare 2019 contro M89–M91

**25 ottobre 2026 — audit documentale e controprove aritmetiche indipendenti.**
**Non è stato aggiunto un verificatore NTC per l'acciaio.** Nessun codice C#,
test xUnit, golden storico, benchmark FEM o esempio G8B modificato. Il gate
.NET dei nuovi C2B/C2C0/C2C1 resta **PENDING**.

## 1. Identità delle fonti e regola di interpretazione

- **D.M. 17 gennaio 2018 (NTC 2018)**, testo primario fornito dall'utente,
  pubblicato nel [S.O. 8 della Gazzetta Ufficiale del 20/02/2018](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf):
  §§4.2.3.1–4.2.3.5, §§4.2.4.1.1–4.2.4.1.3, §4.2.4.2,
  Tabelle 4.2.III, IV, VI–IX e XII–XIII.
- **Circolare C.S.LL.PP. n. 7/2019**, allegato consegnato dall'utente,
  [Gazzetta Ufficiale 11/02/2019](https://www.gazzettaufficiale.it/eli/id/2019/02/11/19A00855/sg):
  §§C4.2.3.1, C4.2.3.3–C4.2.3.4, C4.2.4.1.3.2–C4.2.4.1.3.3.
- Identità, SHA-256 e provenienza dei due PDF: [registro P0-R0](P0_R0_REGISTRO_FONTI_NTC2018_CIRCOLARE2019.md).
- **Codice effettivamente letto:** `src/StructuralSolver2D.Verification.Steel/`:
  `BasicSectionResistanceRulesM89B.cs`, `ElasticBendingRuleM89A.cs`,
  `SteelISectionClassificationM90A.cs`, `SteelMixedISectionClassificationM90B.cs`,
  `SteelClass3ElasticNmvRuleM90C.cs`, `SteelFlexuralBucklingRuleM91A.cs`,
  `SteelBucklingEffectiveLengthsM91B.cs`,
  `SteelLateralTorsionalBucklingRuleM91C.cs`, `SteelMemberCoverageAuditM91D.cs`;
  test corrispondenti nelle cartelle `M89/`, `M90/`, `M91/` del progetto Verification.Tests.

**Vincolo di metodo:** M89–M91 identificano precisamente
`EN 1993-1-1:2005+A1:2014` e richiedono un identificativo di annesso nazionale.
Sono regole **EC3 delimitate**, non regole native NTC 2018. Una somiglianza
algebrica tra le due norme, oppure fornire manualmente `γM0=1,05` dentro
un profilo EC3, **non** modifica l'identità e il campo normativo del report.
Questo audit confronta solo le prescrizioni NTC/Circolare disponibili;
**non** convalida autonomamente il testo completo EC3 o l'annesso nazionale.

## 2. Matrice normativa ↔ codice ↔ copertura

Legenda: **A** = formula/limite algebrico corrisponde nel sottocaso confrontato,
**P** = funzioni parziali EC3 con condizioni esterne, **M** = controllo NTC
mancante, **E** = fenomeno fisico/prova necessariamente esterni al FEM 2D.
Le classificazioni sono **per la capacità NTC**, non promozioni degli EC3 a NTC.

| ID | NTC / Circolare | Evidenza nel codice e nei test | Esito NTC | Rischio e priorità |
|---|---|---|---|---|
| R2-01 | §4.2.3.1, Tab. 4.2.III–IV: classe governante della parte compressa peggiore | `SteelISectionClassifierM90A`: `max(Flange.Class,Web.Class)`, epsilon e limiti c/t per laminati I/H; `M90/SteelISectionClassifierM90ATests.cs`. Le tabelle NTC, grafiche nel PDF, sono state **lette visivamente** | **A/P** per compressione uniforme o flessione forte pura IPE/HEA/HEB | **Alta:** creare confronto con prova di fonte e dominio NTC indipendente dalla classe EC3 |
| R2-02 | Tabelle 4.2.III–IV: per S235, ali esterne compresse `9ε/10ε/14ε`, anime compresse `33ε/38ε/42ε`, anime in flessione pura `72ε/83ε/124ε` per classi 1/2/3 | Gli stessi limiti nel ramo puro `M90A`; 13 scenari di confine indipendenti P0-R2 | **A/P**, solo configurazioni di carico e geometrie sopra dichiarate | **Alta:** non generalizzare a pressoflessione, altri assi o profili INP |
| R2-03 | Circolare C4.2.3.1: parti compresse in campo di sollecitazione combinato e classe massima; possibili metodi alternativi circoscritti | `SteelMixedISectionClassifierM90B` tratta rapporti α/ψ da sollecitazioni dichiarate o schermatura elastica; non è un classificatore NTC di ogni campo tensionale | **P/M** | **Alta:** scegliere ramo e stazione in modo tracciato; non trasformare uno screening conservativo in classe definitiva |
| R2-04 | §4.2.3.1, §4.2.3.2 e Circolare C4.2.3.1: classe 4, aree/moduli efficaci o metodologie ammesse, con limitazioni delle semplificazioni | M90A identifica `Class=4` ma `RequiresEffectiveSectionProperties=true`; M89/M90/M91 non calcolano Aeff/Weff né pannelli | **M** | **Alta:** assenza di resistenza classe 4; progettare riferimento EN 1993-1-5 e prove applicative specifiche |
| R2-05 | §§4.2.3.2–3, Tab. 4.2.VI: analisi globale elastica o plastica, ammissibilità classi, capacità rotazionale e cerniere | FEM Euler–Bernoulli 2D elastico lineare; M89A classe 3 e M91C Wpl esterno per classi 1/2; **nessuna** analisi globale plastica con cerniere conforme a queste condizioni | **P/M** | **Alta:** non chiamare analisi plastica un controllo sezionale che adopera Wpl; Circolare C4.2.3.3 impone cautele aggiuntive sui ritegni |
| R2-06 | §4.2.3.4 Eq. 4.2.1: αcr≥10 per il primo ordine elastico, ≥15 per plastico nei casi indicati; §4.2.3.5 imperfezioni | Nessun gate generale NTC di αcr, spettro di instabilità globale, imperfezioni e sensibilità del telaio; M82 secondo ordine soltanto linearizzato | **M/E** | **Critica P0-E:** imporre evidenza esterna o rifiuto esplicito, senza dedurre αcr dal solaio/telaio FEM 2D |
| R2-07 | §4.2.4.1.1 Tab. 4.2.VII: γM0=1,05, γM1=1,05 per membrature ordinarie, γM2=1,25; ponti γM1=1,10 fuori scope | Costanti in `Ntc2018SteelPartialFactorsG1`, nel progetto Timber, **non** un profilo regole NTC steel; M89/M91 leggono coefficienti profilo EC3 | **A nei valori / M nella regola NTC** | **Critica:** non usare un test EC3 `γM0=1,00` per NTC né accettare γ arbitrari senza profilo vincolato alla norma |
| R2-08 | §4.2.4.1.2.1 Eqs 4.2.5–7: trazione; con fori `min(A fyk/γM0;0,9 Anet ftk/γM2)` | `SteelGrossTensionYieldRuleM89B` verifica **solo** snervamento lordo in trazione pura; `C2C1` usa esclusivamente quella regola; nessuna regola netta | **A/P sul termine lordo; M sulla rottura netta e sul minimo** | **Critica P0-D1:** senza Anet, ftk e γM2 può risultare un falso positivo sul membro |
| R2-09 | §4.2.4.1.2.1 Eq. 4.2.8: gerarchia lorda/netta quando richiesta dal progetto, es. sisma | Nessun selettore di gerarchia o controllo `Npl,Rd≤Nu,Rd` | **M** | **Alta**, scenario sismico e collegamenti fuori perimetro attuale; segnalare `Unsupported` e prove esterne |
| R2-10 | §4.2.4.1.2.2 Eq. 4.2.10: compressione classe 1–3 A, classe 4 Aeff; eccezioni sui fori riempiti | `SteelGrossCompressionRuleM89B` tratta solo gross classi 1–3, assenza fori dichiarata e pure N; nessuna Aeff | **A/P sul ramo lordo; M per classe 4** | **Alta:** classe dichiarata e pressione concentrica non chiudono stabilità di colonna |
| R2-11 | §4.2.4.1.2.3 Eqs 4.2.11–14: flessione monoassiale Wpl classe 1/2, Wel,min classe 3, Weff,min classe 4 | `ElasticBendingRuleM89A` esegue classe 3, flessione pura forte senza N/V; `M91C` usa Wpl esterno **per LTB EC3**, non realizza il generale momento resistente NTC classe 1/2 | **A/P sul ramo elastico; M per rami generali plastico/efficace** | **Alta:** non usare Wel come equivalente sempre a Wpl; classe 4 esclusa |
| R2-12 | §4.2.4.1.2.3 Eq. 4.2.15: presenza di fori in zona tesa e condizione per ignorarli | M89A chiede `NoRelevantHolesConfirmed=true`; nessuna verifica esplicita `Af,net`/`Af` | **M** | **Alta:** appoggio/giunto bullonato può alterare resistenza anche per flessione apparentemente pura |
| R2-13 | §4.2.4.1.2.4 Eqs 4.2.16–26: Vc,Rd=Av fyk/(√3 γM0) **senza torsione**, aree a taglio per famiglie di profilo | `SteelPureShearRuleM89B` calcola il rapporto in pura V con Av **fornita e provata dall'esterno**, niente calcolo NTC delle formule di Av e niente torsione | **A/P nel caso puro; M per Av e torsione** | **Alta:** nessuna capacità per V con N/M concomitanti salvo altri rami parziali |
| R2-14 | §4.2.4.1.2.4 Eq. 4.2.27 e §4.2.4.1.3.4: instabilità dell'anima per taglio | M89B/M90C richiedono `ShearBucklingExcludedConfirmed`, ma non controllano soglie né instabilità dei pannelli | **M/E** | **Alta:** evidenza indipendente su geometrie/anima/irrigidimenti; classi sottili non certificate |
| R2-15 | §4.2.4.1.2.6 Eqs 4.2.30–32: se V>0,5 Vc,Rd riduzione della resistenza a M, salvo altre instabilità | M90C applica solo ramo `V≤0,5 Vpl,Rd` **per classe 3 elastica N/M**; sopra soglia restituisce non applicabile. Non implementa ρ né §4.2.32 | **P/M** | **Critica P0-D2:** rischio da ignorare il taglio elevato nell'inviluppo di progetto |
| R2-16 | §§4.2.4.1.2.7–9 Eqs 4.2.33–40: N+M (mono/biassiale) e N+M+V | `SteelClass3ElasticNmvRuleM90C` copre solo elastico classe 3, asse forte, V≤metà capacità con precondizioni; M90B è classificazione/screening, non resistenza plastica generale | **P/M** | **Critica P0-D2:** non combinare tre massimi separati N/V/M; servono concomitanza e massimo della **regola** |
| R2-17 | §4.2.4.1.3.1 Eqs 4.2.41–47: instabilità aste compresse nei modi pertinenti, γM1, classi e lunghezze reali | `SteelFlexuralBucklingRuleM91A`: buckling EC3 sui due assi per laminati I/H classi 1–3 e compressione pura; `Lcr,y,z`/fonte dall'esterno; torsionale e flesso-torsionale esclusi | **P/M/E** | **Critica P0-E:** 2D non stabilisce ritegni, lunghezze reali e modi spaziali |
| R2-18 | §4.2.4.1.3.2 Eqs 4.2.48–53: LTB trave con ala compressa non ritegnata, Mcr e curva adeguata | `SteelLateralTorsionalBucklingRuleM91C`: general curve **EC3** puro M, classe 1–3 e segmento non ritegnato; Mcr esterno o caso uniforme molto ristretto; non è verifica NTC della formula/curva | **P/M/E** | **Critica:** posizione/carico/diagramma momento, rigidezze ritegni ed esatte curve NTC da provare; nessuna inferenza da viewer 2D |
| R2-19 | §4.2.4.1.3.3 e Circolare C4.2.4.1.3.3: membrature inflesse-compresse, metodi A/B e alternative comprovate | M91A richiede `M=V=0`; M91C richiede `N=V=0`. Nessuna regola completa di **pressoflessione con instabilità** | **M/E** | **Critica P0-D/E:** caso comune nei pilastri di telai e casetta G8B; positivo di due prove pure è irrilevante se N e M coesistono |
| R2-20 | §4.2.4.1.4, §4.2.4.1.5 e §4.2.5: fatica, tenacità, fasi transitorie | Non oggetto delle regole M89–M91; per normali edifici fatica spesso non pertinente, ma non automaticamente esclusa | **M/E se applicabili** | **Media/Alta secondo progetto**: obblighi espliciti/esclusioni motivate in P0-C3 |
| R2-21 | §4.2.4.2, Tab. 4.2.XII–XIII: SLE **acciaio** verticali/laterali, monta, combinazioni caratteristiche, limiti in funzione dell'uso | FEM fornisce spostamenti e B3 candidati SLE; non risulta regola NTC acciaio che scelga limiti per coperture/solai/finiture/colonne e verifichi i massimi | **M** | **Alta P0-D3/C2:** non riutilizzare automaticamente L/300/L/200 del legno G5 per acciaio; NTC acciaio distingue tipologia e deformazione |
| R2-22 | §4.2.4.2.3: vibrazioni e comfort di impalcati, azioni dinamiche | Nessun pass normativo di frequenze/accelerazioni degli impalcati in acciaio in M89–M91; il FEM piano elastostatico non sostituisce il modello dinamico | **M/E** | **Alta dove applicabile:** indicare il vincolo esterno, non generare esiti Passed simulati |
| R2-23 | §4.2.4.1.2 (provenienza della domanda) e insieme delle azioni Capitoli 2/3 | P0-B2/B3/B4 validano inventario/candidati/piano **dichiarato**, P0-C1 conserva N/V/M concomitanti, P0-C2C0 confronta stazioni finite; C2C1 copre un caso speciale EC3 | **P**, non requisito globale dimostrato | **Critica P0-C3:** tutte le combinazioni ed effetti pertinenti non vengono scoperte automaticamente |
| R2-24 | Requisiti di applicabilità delle verifiche, incluse zone d'unione e appoggi | `SteelMemberCoverageAuditorM91D` cataloga **solo obblighi dichiarati** (inclusa net rupture come area senza regola); non autentica completezza del piano, né dimensiona nodi | **P/M/E** | **Critica P0-C3/E:** rifiutare certificazione membro/edificio, preservare `Failed`, `MissingData` e `Unsupported` |

### Il problema pratico dei coefficienti

Esempio **aritmetico fittizio**, non una sezione commerciale certificata:
`A=3000 mm²`, `Anet=2400 mm²`, `fyk=235 MPa`, `ftk=360 MPa`.

- NTC `Npl,Rd=3000×235/1,05/1000 = 671,428571 kN`.
- NTC `Nu,Rd=0,9×2400×360/1,25/1000 = 622,08 kN`.
- Con i fori ipotizzati, `Nt,Rd=622,08 kN`: **governa la rottura netta**.
- Se `NEd=650 kN`, la sola verifica di snervamento lordo parrebbe
  favorevole (`650 < 671,43`), ma **la sezione netta è insufficiente**
  (`650 > 622,08`). La gerarchia della Eq. 4.2.8 non risulta soddisfatta.
- La stessa A e fyk con `γM0=1,00` producono `705 kN`: questa è una
  illustrazione della differenza numerica, **non** un risultato normativo NTC.

È pericoloso presentare il risultato positivo di `C2C1` come resistenza a
trazione completa: C2C1 verifica soltanto il primo dei due possibili limiti e,
per di più, invoca un'identità di regola EC3 separata.

## 3. Controprove realmente eseguite

Script standalone
`benchmarks/independent/p0r2/verify_p0r2_ntc_steel_sections.py`:
**34/34 PASS** in Python standard library, con frazioni esatte (per il taglio
viene controllata un'uguaglianza sui quadrati, evitando approssimazioni di √3).

Copre: fattori parziali NTC, trazione lorda/netta e cambio di ramo governante,
gerarchia delle resistenze, compressione A/Aeff, flessione Wpl/Wel/Weff,
taglio senza torsione, soglia `0,5Vc,Rd`, riduzione ρ e area del contributo
plastico; frontiere delle classi 1–4 per piattabande esterne e anime di S235
in compressione/flessione pura; confronto illustrativo `γM0=1,00`.

Questi test **confrontano i risultati delle formule del PDF con valori
indipendenti**, non testano M89–M91 .NET, non calcolano né verificano
automaticamente una sezione resistente da catalogo e non determinano
idoneità dell'asta. I golden e i test Windows storici non sono cambiati.

## 4. Proposte attuabili senza oltrepassare il FEM 2D

| Priorità | Ticket/ordine | Implementazione futura e condizione di uscita |
|---|---|---|
| **Critica** | **P0-D1** | Un modulo **nativo `Verification.Steel.Ntc2018`** o equivalente isolato dalle regole EC3: edizione/versione fissa, γM0/M1/M2 prescritti in base all'ambito, input `Anet`, `ftk`, percorso fori/giunto comprovati; trazione con `min` dei due stati limite e branch espliciti, gerarchia se pertinente; golden che riproducano entrambi gli esiti governanti e `MissingData` per sezione forata senza dati |
| **Critica** | **P0-C2C2/P0-D2** | Resistenze sezionali NTC per classe (Wpl/Wel/Weff) e interazioni N+M+V con taglio elevato e cambi di ramo; massimo continuo del **rapporto reale**, N/V/M alla stessa stazione e lato, applicabilità controllata; risultati incompleti finché tutti i rami fisicamente richiesti non sono verificati |
| **Critica** | **P0-E1 / P0-D4** | Contratto per stabilità spaziale, Lcr sui due assi, modi torsionali e flesso-torsionali, Mcr, ritegni, collegamenti, imperfezioni e sensibilità globale `αcr`; richiedere calcolo o documento esterno, respingere in modo trasparente i casi non coperti, **senza dichiarare che il FEM 2D li risolve** |
| **Alta** | **P0-D0** | Classificatore NTC tipizzato e provato su Tab. 4.2.III–V, carico/asse/fy/spessori/fori, con limiti espliciti; prova indipendente per classi confine e campi α/ψ; test negativi per INP, classe 4 senza Aeff e carichi misti non coperti |
| **Alta** | **P0-D3** | Verifiche SLE dell'acciaio in modulo distinto da G5 legno: Tab. 4.2.XII–XIII, tipologia e stato finale/monta, combinazione, massimo reale, limiti del progetto; dinamica/vibrazioni marcate fuori scope se non disponibili |
| **Alta** | **P0-C3/C4** | Registro delle verifiche obbligatorie per membro e combinazione con `Required/Passed/Failed/Unsupported/MissingEvidence`, produzione di report con fonte, profilo e motivi di esclusione; nessuna deduzione di esaustività dal solo piano dichiarato M91D |
| **Alta** | **P0-E/G8B** | Matrice G8B per ogni pilastro/trave: quali domande N/M/V disponibili da FEM e quali prove sono fisicamente esterne, incluse vincolature laterali, stabilità globale, nodi, impalcati e controventi; non trasformare demo elastica in progetto verificato |
| **Sempre** | **Regressione** | Conservare tutti i test M89–M91, G7 e P0 precedenti; aggiungere xUnit NTC e benchmark esterni per rami positivi/negativi. Un pass Python o un confronto tra formule simili non sostituisce i test C# sul proprio motore |

**Criterio vincolante per qualsiasi nuova regola:** identificativo univoco
`Norma/Edizione/Profilo`, clausola, campo di validità, dati fisici obbligatori,
provenienza geometrica e materiale, N/V/M concomitanti e stato conservativo.
Quando un ramo è mancante restituire `Unsupported`/`MissingData` e impedire
la promozione dell'intera asta. In particolare, test positivi di compressione
pura e flessione pura non sostituiscono la pressoflessione della stessa colonna.

## 5. Stato e prossima attività

- Confermati dall'utente, con il loro perimetro storico: **G7**, P0-B2,
  P0-B3, P0-B4, P0-C1 FIX1, P0-C2A.
- **C2B, C2C0, C2C1**: nuovi xUnit predisposti ma non ancora eseguiti su
  Windows; **PENDING**, senza promozione.
- Controprove locali: P0-B1 **14+10**, C2C1 **3**, P0-R1 **12**,
  nuovo P0-R2 **34/34** (tutti Python indipendenti, non C#).
- G8B: quattro varianti di casetta acciaio/legno, riscontri Python elastici
  16/16; confronto numerico completo C#↔Python e limiti normativi non chiusi.
- **Prossimo:** gate Windows C2B/C2C0/C2C1 e G7 quando disponibile;
  quindi C2C2/C3/C4, contratti P0-E e regole NTC acciaio P0-D con
  benchmark e fonti EN per i relativi rami. Eventuali ulteriori
  verifiche della Circolare seguono le stesse regole di tracciabilità.
- **GUI C#/Avalonia esclusivamente P3**, ultima fase.

L'audit non è una dichiarazione di conformità legale/tecnica di un'opera,
e non elimina la necessità di valutazione progettuale specialistica.
