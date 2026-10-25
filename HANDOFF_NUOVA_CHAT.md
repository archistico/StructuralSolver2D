# StructuralSolver2D — Handoff di ripresa

**25 ottobre 2026 — P0-F0, sola documentazione.** Archivio FULL da usare: `StructuralSolver2D_P0F0_DOCUMENTAZIONE_E_POLICY_CSHARP_2026-10-25_FULL.zip` (estrarre **pulito**). Il codice C# è identico al precedente **P0-D2C2**.

## Leggere in quest'ordine

1. [`docs/structural/CURRENT_STATE.md`](docs/structural/CURRENT_STATE.md): situazione verificabile, ultimo test confermato e limiti.
2. [`docs/structural/roadmap.md`](docs/structural/roadmap.md): sequenza e gate F0–F10.
3. [`docs/structural/ENGINEERING_AND_VALIDATION_RULES.md`](docs/structural/ENGINEERING_AND_VALIDATION_RULES.md): regole di sviluppo vincolanti.
4. [`docs/structural/PIANO_DETTAGLIATO_POST_D2C2.md`](docs/structural/PIANO_DETTAGLIATO_POST_D2C2.md): valutazione approfondita dei gap e delle priorità.
5. [`CHANGELOG.md`](CHANGELOG.md) e documentazione specifica di milestone per i dettagli storici.

## Situazione

- **P0-D2C2 .NET: PASS comunicato dall'autrice** («test passati»), senza log allegato; anche precedenti confermati. In F0 **non** sono stati aggiunti o eseguiti test .NET.
- La regressione indipendente **4.203/4.203** di D2C2 è un **dato storico Python**, non un conteggio xUnit e non un nuovo gate F0.
- Motore FEM 2D già ampio ma copertura normativa **sezionale/parziale**; attenzione a stazioni, concomitanze, ritegni fisici, completamento combinazioni e `Passed` soltanto delle regole richieste. Nessuna GUI desktop ancora.
- **NTC 2018, EC3 ed EC5 separati** per norma/edizione/annesso e coefficienti; nessuna conversione implicita fra esiti.
- **Nuovo codice, test, benchmark harness, runner e script soltanto C#/.NET.** Non scrivere/eseguire nuovo Python; i file Python e PowerShell preesistenti sono storici/compatibilità e non devono entrare nel flusso futuro.

## Prossimo passo autorizzato

**F1.1 G8B: 16 scenari FEM con verificatore C#/.NET** e dati attesi **indipendenti e congelati**. Definire misure/tolleranze prima di scrivere il confronto, includere controprove negative, confermare build e test .NET sul PC dell'autrice. **Nessuna nuova verifica normativa o GUI nel prossimo passaggio.**

## Disciplina dei checkpoint

Ogni nuovo avanzamento deve indicare: **stato/milestone e test confermati; modifica in corso; passi ancora da fare; parti non implementate o fisicamente non dimostrate**. Aggiornare i documenti di competenza e confezionare uno ZIP FULL con manifest SHA256 verificato. Mantenere il README essenziale. Le versioni precedenti di roadmap e handoff sono conservate in [`docs/archive/`](docs/archive/README.md).
