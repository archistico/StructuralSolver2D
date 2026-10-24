# Guida G6 — Audit NTC legno: come interpretare ed estendere i risultati

Questo documento è operativo per G7. G6 NON è una certificazione NTC: è un controllo delle **verifiche dichiarate**, la cui selezione resta responsabilità del tecnico.

## Architettura

- Il FEM produce risultati e loro provenienza (`AnalysisId`, `CombinationId`, `MemberId`, stazione, lato, valori firmati).
- Il motore `DesignCheckEngine` applica regole NTC native G2–G5. G1/G5 selezionano coefficienti e il rispettivo profilo.
- **G6** prende un `NtcTimberCoveragePlanG6` redatto dal chiamante e gli `IEnumerable<DesignCheckReport>` realmente prodotti. Non invoca le formule: controlla l'esistenza, il tipo, l'identità e la coerenza numerica di ogni risultato.
- M91-D continua a svolgere il controllo analogo per le **verifiche EC3 in acciaio**; non è un report NTC acciaio.

## Piano e algoritmo G6

1. Dichiara identità dell'asta, sezione, lunghezza, edizione **NTC 2018**, codice e materiale legno.
2. Per ogni verifica selezionata dichiara una `NtcTimberCoverageObligationG6` con: `Area`, `RuleId` G2–G5, stato limite, `ProfileId`, `AnalysisId`, `CombinationId`, coordinata esatta in metri e lato del salto.
3. Tratta tutte le restanti aree previste nell'enumerazione come `NtcTimberCoverageExclusionG6`: soltanto `PhysicallyNotApplicable` con evidenza fisica verificata da un tecnico può chiudere un'area per il piano dichiarato. `UnsupportedOrNotEvaluated` rimane **incompleto**, mai automaticamente escluso.
4. Richiedi tre attestazioni esterne documentate: completezza delle combinazioni, completezza delle stazioni critiche e congruenza del modello fisico (2D, vincoli, rigidezza, deformabilità a taglio, secondo ordine, ecc.). L'auditor controlla che siano dichiarate **non verifica da sé il loro contenuto**.
5. Esegui `NtcTimberCoverageAuditorG6.Audit(plan, designReports)`.
6. Leggi `Outcome`, `Findings`, `UndeclaredAreas`, `Diagnostics`, `UnmatchedResults`, `UnmatchedVerifiedFailures` e `UnsupportedExclusions`; genera un resoconto con `NtcTimberCoverageMarkdownG6.Render(audit)`.

I risultati reali sono ottenuti **solo** dal `DesignCheckEngine`; i test del solo auditor usano `DesignCheckReport` sintetici per verificare il matching e le condizioni di sicurezza. Non vanno considerati un nuovo benchmark di resistenza.

## Cinque situazioni da verificare

| Scenario | Riscontro atteso | Conseguenza |
|---|---|---|
| Una resistenza locale passa, ma mancano svergolamento e collegamenti | `Passed` solo per la formula eseguita; altre aree incomplete | **Incomplete**, non "asta sicura" |
| Interazione N–M nella stessa combinazione e stessa sezione | Il G3 valido può coprire solo la specifica categoria | Non sostituisce controlli stabilità |
| SLE G5 con profilo diverso da SLU G2/G3 | Ricerca risultati separata per `LimitState` e `ProfileId` | Nessuna sostituzione tacita |
| Resistenza flessionale η>1, stabilità mancante | `FailedWithinDeclaredScope` con lacune riportate | Il fallimento prevale |
| Modello Euler–Bernoulli con taglio deformativo rilevante non valutato | `ShearDeformationEulerBernoulliLimit` unresolved | Incomplete; nessuna conversione automatica a Timoshenko |

## Tracciabilità dei casi del libro

Il documento `benchmarks/G6_COVERAGE_REFERENCE_CROSSWALK.json` censisce **35 candidature BAL2/BAL3** e **9 casi M92-N6**. Non riproduce tutte le pagine del libro e NON certifica i calcoli.

Lettura suggerita delle principali famiglie:

| Caso originale | Fenomeno per audit G6 | Impiego |
|---|---|---|
| BAL2-01, BAL2-03 | Segni e combinazioni variabili/permanenti | Attestazione inventario combinazioni; test numerici in G7 |
| BAL2-06 | Deformabilità a taglio Euler–Bernoulli/Timoshenko | Area specifica con esito incompleto in caso rilevante |
| BAL2-07, BAL3-02 | Scorrimenti/giunti ed eccentricità | Area connessioni non sostituibile con pass G3 |
| BAL2-14, BAL2-17, BAL3-10/11 | Lunghezze efficaci e vincoli reali | Prove di stabilità G4 con ritegni dichiarati |
| BAL2-20, BAL2-21 | Svergolamento e rigidezze torsionali delle sezioni d'acciaio | Audit M91D acciaio separato; G7 benchmark |
| BAL3-01 | Trave con sbalzo e possibile sollevamento | Modello reale vincolo contatto, senza inferire reazione di trazione fisicamente ammissibile |
| BAL3-03 | Controvento attivo solo in trazione | Ipotesi di non linearità unilaterale da esplicitare |
| BAL3-04/05 | Frecce e criteri SLE acciaio | Non equiparare ai valori NTC legno G5 |

## Limiti di ambito e riferimenti

- NTC 2018 §§2.5.3, 4.4.4–4.4.8 e §10.2; Circolare 7/2019 §§C4.4.7, C4.4.8. Le prove fisiche restano esterne.
- La seconda edizione del manuale Ballio–Mazzolani–Bernuzzi–Landolfo è un **manuale scientifico**, non una norma cogente. La sua equazione 9.43 (p. 522) presenta un possibile refuso in φ; fare riferimento alla formula ufficiale NTC.
- Il FEM 2D non assegna da sé rigidezza fuori piano, efficacia dei ritegni, comportamento monolatero, sollevamento, scorrimento, comportamento dinamico, incendio, sismica e compatibilità con i collegamenti reali.
- G6 non decide che tutte le categorie normativamente necessarie sono state elencate: lo schema è finito. Un esito `ChecksPassedWithExplicitPhysicalExclusions` resta un **risultato di audit sul piano proposto**.
- Per steel EC3, `SteelMemberCoverageAuditorM91D` resta la soluzione esistente; un profilo NTC non deve essere spacciato per un risultato EC3.

## Dati futuri G7

G7 dovrà aggiungere casi di FEM indipendenti con geometria e carichi completi, legare ogni `DesignCheckReport` alle dichiarazioni dei selettori G1/G5, verificare materialmente l'elenco stazioni/azioni e trasformare i casi BAL selezionati in benchmark solo dopo verifica delle unità, delle formule e delle condizioni fisiche. In nessun caso l'audit sostituisce la valutazione strutturale professionale.
