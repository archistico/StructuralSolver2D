# Changelog — StructuralSolver2D

Registro **sintetico e cronologico**. Ogni stato «gate superato» è basato sulle conferme dei test Windows dell'utente, non su una certificazione della conformità normativa dell'opera. I dettagli delle milestone si trovano in `docs/structural/` e nell'[archivio](docs/archive/README.md).

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
