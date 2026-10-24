# StructuralSolver2D — HANDOFF NUOVA CHAT

**Ultimo aggiornamento:** 24 ottobre 2026 · **checkpoint documentale G7-DOC1** · **ultima baseline software G7 GATE PASSED** (conferma dell'utente su Windows). Questo documento è il punto di ripresa autorevole e **deve essere aggiornato a ogni nuovo incremento o cambio di gate, prima della consegna dello ZIP**.

## 1. Obiettivo non negoziabile

Software **FEM 2D open source** in C#/.NET 8, modulare, per travi/reticolari/telai in **acciaio e legno**, destinato a didattica, ricerca e valutazioni preliminari. Ordine: **calcolo e validazione → verifiche normative EC3/EC5/NTC SLU/SLE → GUI desktop Avalonia per ultima**. Fuori perimetro: 3D, c.a., muratura, piastre, pareti, X-LAM, BIM, collegamenti esecutivi completi. UI da non avviare anticipatamente.

## 2. Versione autorevole e gate

- **Baseline di codice:** G7 (`StructuralSolver2D_G7_VALIDAZIONE_INTEGRATA_BENCHMARK_NTC2018_2026-10-24_FULL.zip`), **G7 GATE PASSED** confermato dall'utente.
- **Pacchetto documentale successivo:** `StructuralSolver2D_G7_DOC1_DOCUMENTAZIONE_PULITA_2026-10-24_FULL.zip` — ripulita la documentazione, **nessuna modifica a produzione/test/script/modelli o golden**. Questo è il pacchetto preferito per aprire una nuova chat dopo questo riordino.
- Gate M92-A, M92-B, G1 (FIX1), G2, G3 (FIX1), G4, G5, G6, **G7**: tutti **confermati superati** dall'utente entro il 24/10/2026, ciascuno nel proprio perimetro. Non confondere con conformità di un intero elemento o struttura.
- File `VERSION=0.1.0` invariato: non attribuire una nuova versione semantica al solo riordino documentale.

## 3. Capacità realmente disponibili

- `Core`, `Analysis`, `Catalogs`, `Verification`, `Verification.Steel`, `Verification.Timber`, `Reporting`, `Cli`, test e benchmark C#.
- FEM piano elastico, supporti e carichi, combinazioni manuali, post-processing, reazioni N/V/M. Moduli dinamici e prototipi avanzati delimitati (M82 = secondo ordine globalmente **linearizzato** in ambito ristretto; nessun solver FEM 3D).
- **Acciaio EC3:** verifiche parziali M89–M91 e auditor M91-D; non completo profilo NTC acciaio.
- **Legno EC5:** M92-A (4 azioni **pure**) + M92-B coefficienti della prima generazione; non Annesso Nazionale validato per default.
- **Legno NTC 2018:** G1 coefficienti, G2 4 verifiche pure rettangolari, G3 N–M e taglio concomitanti locali, G4 instabilità pure con dati esterni, G5 frecce SLE, G6 audit su piano dichiarato, G7 benchmark e integrazione.
- **G7:** casi FEM B13/B14 di sbalzo con reazioni positive/negative e test reali selettore→regola→audit; `docs/G7_MANUALE_VALIDAZIONE_INDIPENDENTE.md`.

## 4. Limiti e presidi da mantenere

**Nessuna conformità normativa completa** dell'asta/struttura, nessuna generazione e validazione esaustiva di combinazioni, nessuna garanzia sulle attestazioni esterne di ritegni/stazioni, nessuna copertura totale dei giunti/fori/torsione/instabilità fuori piano. Differenza tassativa: *non applicabile* fisico giustificato ≠ *non implementato*; fallimenti prevalgono sui controlli mancanti; nessuna combinazione di valori `N`, `V`, `M` presi da inviluppi diversi. Un appoggio del FEM può essere bilatero anche se l'appoggio reale potrebbe sollevarsi. Euler–Bernoulli non copre sempre taglio deformativo; Timoshenko 2D è studio futuro.

Norme: **NTC 2018, Circolare 7/2019**, EC3 prima generazione EN 1993-1-1:2005+A1:2014, EC5 EN 1995-1-1:2004+A1:2008+A2:2014, senza accettazione implicita di nuove edizioni o annessi. Il manuale Ballio–Mazzolani–Bernuzzi–Landolfo 2020 è **fonte tecnica secondaria** di esempi e limiti fisici, non sostituisce i testi normativi; attenzione al possibile refuso sulla formula d'instabilità della sua equazione 9.43 (usare la norma come riferimento).

## 5. Come verificare il pacchetto

```powershell
.\scripts\verify-g7-manifest.ps1
.\scripts\run-g7-tests.ps1
```

Il runner G7 pulisce `bin/obj`, compila Release con warning-as-error, verifica benchmark FEM e pipeline normativa, poi esegue regressioni. Dopo un cambio **solo documentale** confrontare gli SHA-256 delle cartelle `src/`, `tests/`, `benchmarks/`, `catalogs/`, `scripts/`, `examples/`, `tools/` e rigenerare `RELEASE_SHA256SUMS.txt`. Nessuna compilazione .NET disponibile nell'ambiente di produzione del pacchetto; mai dichiarare nuovi gate superati senza conferma Windows. Il gate G7 precedente è confermato.

## 6. File da leggere per primi

1. **Questo file** per lo stato corrente e il prossimo lavoro.
2. [`README.md`](README.md) per panoramica e comandi.
3. [`docs/structural/roadmap.md`](docs/structural/roadmap.md) per le priorità post-G7.
4. [`CHANGELOG.md`](CHANGELOG.md) per la cronologia sintetica.
5. [`docs/G7_MANUALE_VALIDAZIONE_INDIPENDENTE.md`](docs/G7_MANUALE_VALIDAZIONE_INDIPENDENTE.md) e [`docs/structural/G6_IMPLEMENTATION_GUIDE.md`](docs/structural/G6_IMPLEMENTATION_GUIDE.md) per benchmark e copertura.
6. [`docs/archive/README.md`](docs/archive/README.md) **solo se servono** fonti storiche; diffidare delle vecchie frasi «gate da eseguire».

## 7. Passo successivo (NON iniziato)

**Audit post-G7 dei gap fisici e normativi**; decidere priorità fra: combinazioni SLU/SLE NTC complete, copertura delle stazioni critiche, verifiche ancora mancanti dell'acciaio, limiti Euler–Bernoulli, ritegni/giunti/contatto, benchmark indipendenti. Mantenere il motore attuale finché le modifiche non siano motivate da analisi e test. Niente GUI.

## 8. Protocollo obbligatorio a ogni nuova milestone

- Partire dall'**ultimo ZIP FULL effettivamente collaudato**, non da un vecchio checkpoint, e conservare le API/casi precedenti se non vi è un cambiamento deliberato.
- Distinguere formule normative, assunzioni e fonte dei benchmark. Aggiungere test sia positivi sia negativi (fail-closed); non inventare dati che il FEM 2D non conosce.
- Consegnare **ZIP FULL** con manifest SHA-256 e runner Windows, annotando ciò che è stato eseguito davvero.
- Aggiornare **sempre questo handoff** (baseline, gate, moduli, file, prossimo passo, eventuali errori), **CHANGELOG** con voce sintetica e **roadmap** se le priorità mutano; aggiornare il README solo quando cambiano funzionalità, comandi o architettura.
- Spostare note storiche in `docs/archive/`, non accumulare milestone alla radice e non ripristinare annunci scaduti nel README.
