# Registro di accettazione M86 RC1

- Stato all'impacchettamento: **in attesa del gate Windows RC1**.
- Baseline M85-C: compilazione 0 warning/0 error, 8/8 nuovi test e 1.334/1.334 regressione, confermati dal log utente del 24 ottobre 2026.
- Sorgenti di produzione `src/` M86 RC1: **immutati** rispetto a M85-C.

| Verifica | Criterio | Stato RC1 |
|---|---|---|
| Integrità del pacchetto | Manifest SHA-256 valido | Verificato nel confezionamento; ricontrollare su Windows |
| Build Release | Zero warning e zero error | In attesa |
| Suite Core / Analysis / Reporting / CLI | Tutti i test superati | In attesa |
| CLI in modalità Frame2D e Truss2D | `Analysis completed.` | In attesa |
| Selezione combinazione ULS1 | Elaborazione completata | In attesa |
| Markdown / almeno 6 CSV / XLSX / PDF | File presenti; magic PDF/XLSX validi | In attesa |
| Audit numerico PDF ed esame visivo | Controllo manuale separato | **Non incluso** |
| Interoperabilità con altro software FEM | Multi-caso con input/versione congelati | **Non incluso** |
| Matrici sparse, analisi non lineare generale, normative | Fuori dal perimetro M86 | **Differito** |

Per chiudere M86 RC1, eseguire `scripts/run-m86-release-gate.ps1` su Windows e conservare il log. Non sostituire i gate con il solo `dotnet build`.
