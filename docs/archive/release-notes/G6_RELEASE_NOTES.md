# G6 — Audit della copertura NTC 2018 del legno e dei limiti del modello

**Data:** 24 ottobre 2026  
**Baseline:** G5 completo (`G5 GATE PASSED`, dichiarato dall'utente su Windows).  
**Stato G6:** codice e test predisposti; **non collaudato in .NET 8 in questo ambiente**.

## Scopo reale

G6 NON aggiunge nuove formule normative, NON modifica il FEM e NON dichiara verificata una struttura. Introduce **un audit post-processo** dei `DesignCheckReport` generati dal `DesignCheckEngine` con regole native NTC 2018 G2–G5 (legno massiccio/lamellare supportato). Non sostituisce e non mescola il precedente auditor M91D per acciaio EC3.

File aggiunto: `src/StructuralSolver2D.Verification.Timber/Ntc2018TimberCoverageAuditG6.cs`.

### Funzioni

- 12 categorie di regole implementate: le quattro G2, le tre G3, le due G4, le tre SLE G5. Le due disuguaglianze di ciascuna interazione sono **interne alla regola G3**, non due risultati audit separati.
- 14 ulteriori categorie di fenomeni e ipotesi che richiedono una posizione esplicita: azioni perpendicolari alla fibratura, appoggi, intagli, connessioni, torsione, effetti fuori piano, secondo ordine, scorrimenti e taglio Euler–Bernoulli, controventi monolateri, sollevamento, vibrazioni e altro.
- Verifica di **identità esatta** (asta, sezione, lunghezza, edizione NTC, stato limite SLU/SLE, `ProfileId`, `AnalysisId`, `CombinationId`, stazione senza arrotondamento, lato `BeforePointLoad`/`AfterPointLoad`/`Continuous`, origine `FemAnalysis`, target `LoadCombination`, regola richiesta).
- Confronto di coerenza numerica `η = domanda / resistenza`, con risultati finiti e coerenti con `Passed`/`Failed`; non autentica indipendentemente una verifica dichiarata.
- Nessun credito per `Unsupported`, `NotApplicable`, `MissingData`, `InvalidInput` o `CalculationError` restituiti dal motore.
- Esclusioni distinguibili: `PhysicallyNotApplicable` con motivazione e fonte **attestate dall'esterno**, oppure `UnsupportedOrNotEvaluated` che **mantiene l'audit incompleto**.
- Verifica delle categorie non dichiarate, degli obblighi doppi, delle esclusioni contraddittorie, dei risultati ripetuti, degli esiti non associati al piano. Un fallimento numericamente valido non è annullato da altre lacune.
- Attestazioni obbligatorie sulla completezza delle combinazioni, delle stazioni critiche e delle ipotesi fisiche. L'auditor verifica la presenza della dichiarazione **non la sua veridicità**.
- `NtcTimberCoverageMarkdownG6.Render`: rapporto leggibile con obblighi, stazioni, risultati, esclusioni, limiti e diagnostica, con avvertenza esplicita sulla NON conformità complessiva.

### Separazione fra esiti

`FailedWithinDeclaredScope` prevale quando c'è almeno un fallimento valido anche se il resto è incompleto. `Incomplete` copre assenze, contraddizioni e mancanza di prova. `ChecksPassedWithinDeclaredScope` / `ChecksPassedWithExplicitPhysicalExclusions` esprimono **solo la copertura di un piano scritto dall'utente** e mai la conformità normativa di un'asta, di un collegamento o di una struttura. Il piano è una checklist FINITA, non un inventario certificato NTC. Un'esclusione fisica ha affidabilità subordinata a una revisione tecnica esterna.

### Letture Ballio–Mazzolani G5-L1/L2/L3

I rischi ricavati dal manuale, seconda edizione Hoepli 2020, diventano aree da dichiarare nel piano, **non formule normative prese dal libro**: teoria Euler–Bernoulli versus Timoshenko (cap. 2.5.3, p. 99; cap. 8.3.1, p. 419), diagonali attive solo a trazione (cap. 2.4), appoggi con sollevamento (cap. 1.3.1, fig. 1.12), modelli di stabilità/effetti del secondo ordine (cap. 2.5.4, cap. 9) e collegamenti eccentrici (cap. 2.4.3). Restano escluse dichiarazioni automatiche su carichi/spostamenti fuori piano.

`benchmarks/G6_COVERAGE_REFERENCE_CROSSWALK.json` censisce i **21 candidati L2, 14 candidati L3 e 9 scenari M92-N6**. Questo è un indice di riferimenti tecnici: i 35 candidati del libro **non sono test .NET eseguiti**; i numeri dei casi del libro possono essere sintetici o approssimati e devono essere convalidati caso per caso in G7. Le formule della NTC restano prevalenti, incluso il probabile refuso di φ nell'eq. 9.43 del manuale (p. 522).

### Limiti deliberati

- G6 NON costruisce automaticamente un piano completo per le NTC, né decide autonomamente se la singola norma si applichi a un elemento reale.
- Non usa i PDF del manuale come base normativa; NTC 2018 DM 17/01/2018, Circolare 7/2019 e l'edizione degli Eurocodici rimangono autorità distinte.
- Non accerta il vincolo reale, lo scorrimento, le resistenze dei collegamenti, la certificazione di classe resistente, le azioni sismiche/incendio, l'origine fisica di una sezione netta, l'equivalenza 2D/3D e l'effettiva esaustività dei casi/stazioni.
- M91D (acciaio EC3) è già disponibile e resta invariato; G6 non è un sistema universale EC3 + EC5 + NTC, né una convalida automatica di nuovi controlli dell'acciaio.
- Un `Passed` sintetico fabbricato dall'esterno e coerente aritmeticamente NON diventa autentico grazie a G6. Il rapporto avvisa esplicitamente di questa limitazione.

### Test

Aggiunto `tests/StructuralSolver2D.Verification.Tests/M92/Ntc2018TimberCoverageAuditorG6Tests.cs`, con riscontri su: categorie G2–G5, profili distinti SLU/SLE, stazione e lato esatti, duplicati, valori anomali o falsamente positivi, insufficienza dei dati, casi non supportati, esclusioni documentate, origine NTC ed esito di fallimento prevalente. I risultati di input dei test audit sono **sintetici**, senza sostituire i calcoli reali già collaudati in G2–G5.

### Gate Windows

```powershell
.\scripts\verify-g6-manifest.ps1
.\scripts\run-g6-tests.ps1
```

Il runner esegue build Release con `-warnaserror` e regressioni Verification, Catalogs, Core, Analysis, Reporting e CLI. `G6 GATE PASSED` deve essere riportato **solo dal computer Windows** dopo tutti i test. Il manifest dello ZIP permette di verificare l'immutabilità della baseline.

### Roadmap

Dopo G6: **G7** verifica i nuovi benchmark candidati numerici, le verifiche di copertura dei report e il manuale di validazione. Anche dopo G7 resteranno valutazioni ingegneristiche e funzionalità normative da sviluppare prima di poter dichiarare copertura generale; la GUI rimane fase finale.
