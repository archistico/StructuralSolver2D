# M92-A — Verifiche parziali del legno, sezione rettangolare

Data: 24 ottobre 2026. Baseline: M91-D FIX1 (gate Windows superato come comunicato dall'utente). **M92-A: gate Windows da eseguire.**

## Scope

Nuovo assembly `StructuralSolver2D.Verification.Timber`, separato da FEM/Steel. Uso didattico e preliminare.
Implementata **soltanto la prima generazione EN 1995-1-1:2004+A1:2008+A2:2014**. UNI EN 1995-1-1:2025 è un'edizione nuova, NON trattata da queste formule. Non sono implementate le verifiche NTC 2018, gli Annessi Nazionali, i valori tabellari automatici di coefficienti o l'applicabilità normativa in un dato paese: ogni combinazione richiede valori di input giustificati esternamente.

- §6.1.2: trazione parallela alla fibratura, **solo N > 0**, V=M=0.
- §6.1.4: compressione parallela alla fibratura, **solo N < 0**, V=M=0. La stabilità di membratura §6.3 non è verificata.
- §6.1.6: flessione **solo sull'asse forte**, N=V=0, nessuna verifica di instabilità laterale.
- §6.1.7: taglio nel piano e parallelo alla fibratura, N=M=0. È obbligatorio un `kcr` motivato esternamente per la larghezza efficace; **nessun 0,67 implicito**.

Tutte le verifiche usano la resistenza caratteristica del catalogo M88-C e `fd = kmod * fk / gamma_M`. `kmod` (potenzialmente >1 per alcune durate di carico) e `gamma_M` sono coefficienti dichiarati **per specifica combinazione** con `SourceClause`. Non vengono desunti automaticamente né confrontati con l'annesso nazionale. Il controllo della correttezza del valore dichiarato resta esterno. 

Per flessione rettangolare: `W = b*h²/6` (mm³). Assiale: `A = b*h` (mm²). Taglio: `tau = 1.5*abs(V)*1000/(kcr*A)` (MPa). Resistenze/tensioni in MPa, N in kN, M in kNm. Convenzione del solver: N>0 trazione, N<0 compressione. Geometria dichiarata e documentata, parallela alla fibratura, senza fori/intagli e senza fattori benefici di taglia. `k_h` non è incrementato; in presenza di requisiti o condizioni speciali serve ulteriore verifica.

## Provenienza obbligatoria

Ogni assegnazione è vincolata a (`MemberId`, `AnalysisId`, `TargetId`, `TargetKind`); al momento del calcolo si controllano anche `SectionId`, `Effects.MemberId`, `Effects.SectionSide`. Il modello non trasferisce automaticamente durata o classe di servizio da una combinazione all'altra. Fornire `ServiceClass` 1/2/3, `LoadDuration` e motivazione, sezione piena, verso degli assi e prova geometrica. Non sono accettate combinazioni di N, V, M (M93 previsto).

## Non implementato

Pressoflessione; instabilità flessionale o flesso-torsionale delle aste; tensioni perpendicolari alle fibre; instabilità al taglio; fori, intagli, collegamenti, stabilità dei ritegni; `kdef`, deformazioni differite e SLE; incendio; combinazioni normative automatiche; verifica strutturale complessiva. Una regola soddisfatta **non è una certificazione EC5/NTC**. Richiedere altri RuleIds genera `Unsupported`, non un'approvazione tacita.

## Calcoli campione indipendenti (valori TEST non normativa italiana)

C24: b=100 mm, h=200 mm, A=20.000 mm², W=666.666,667 mm³; kmod=0,8; gamma_M=1,3; kcr=0,67.

| Azione pura | Domanda | Tensione di progetto richiesta | Resistenza disponibile |
|---|---:|---:|---:|
| Nt=100 kN | 100 kN | 5,000000 MPa | 14,5·0,8/1,3 = 8,923077 MPa |
| Nc=-100 kN | 100 kN | 5,000000 MPa | 21·0,8/1,3 = 12,923077 MPa |
| M=6 kNm | 6 kNm | 9,000000 MPa | 24·0,8/1,3 = 14,769231 MPa |
| V=20 kN | 20 kN | 2,238806 MPa | 4·0,8/1,3 = 2,461538 MPa |

I coefficienti test non attestano l'Annesso Nazionale; sono input espliciti di esercizio.

## Fonti di verifica tecnica (non incorporate nel pacchetto)

- EN 1995-1-1:2004, amendment A1:2008/A2:2014; §§2.4.1, 6.1.2, 6.1.4, 6.1.6, 6.1.7 (formule prima generazione).
- https://expydoc.com/doc/3959614/en-1995-1-1-manual — spiegazione applicativa delle verifiche.
- https://store.uni.com/uni-en-1995-1-1-2025 — edizione 2025 da tenere distinta (UNI).
- https://www.scia.net/sites/default/files/files/2022-03/Timber_Code_Check_Theory_enu.pdf — spiegazioni per la larghezza efficace nel taglio e gli Annessi Nazionali.
- M88-C: `catalogs/m88/characteristic-strength-classes.json`, dati di sola origine indicativa EN 338/EN 14080.

## Esecuzione e accettazione

PowerShell Windows dalla radice: `./scripts/verify-m92a-manifest.ps1` e `./scripts/run-m92a-tests.ps1`. Gate: build Release con warnings as errors, test Verification incluso M92-A, Catalogs e regressione FEM/Reporting/CLI. `-WithM86Smoke` opzionale. **Non dichiarare completati i nuovi test prima dell'esecuzione su Windows.**

## Prossimi incrementi

M92-B tabelle durata/classe di servizio e parametri normativi con annessi verificati; M93 interazioni N-M, instabilità nel/fuori piano; M94 casi indipendenti legno; M95 SLE e deformazioni differite; M96–99 audit e release; GUI soltanto dopo.
