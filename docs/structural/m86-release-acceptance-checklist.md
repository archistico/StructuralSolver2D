# M86 — Criteri per la prima release Core FEM 2D (checklist preliminare)

**Stato 24/10/2026 — RC1:** M85-C superato su Windows (8 casi mirati e 1.334 test completi; 0 warning, 0 error). Il gate M86 RC1 è predisposto e deve ancora essere eseguito; non è una release stabile. Vedere `m86-release-preview.md` e `m86-acceptance-results.md`.

- [x] M85-C: build Release con 0 errori, 0 warning; test di riferimento, casi limite e regressione completa superati (log utente 24/10/2026).
- [ ] Contratto modello JSON/API/CLI e convenzioni kN–m–kNm–rad documentati con esempio riproducibile.
- [ ] Analisi statica elastica Frame2D/Truss2D, combinazioni, carichi, supporti, reazioni, N/V/M e inviluppi: scope e test rappresentativi mappati.
- [ ] Diagnostica: meccanismi, vincoli insufficienti, non-finite, errori di modello, messaggi di arresto verificati.
- [ ] Report: CSV, Markdown, XLSX, PDF; collaudo PDF numerico e visuale da dichiarare separatamente se incompleto.
- [ ] M82 esplicitamente **sperimentale / globalmente linearizzato**, carichi ammessi e casi non supportati dichiarati.
- [ ] Moduli dinamici e non lineari separati dai gate della release elastica.
- [ ] Manifest dei file, licenze terze parti, SBOM/dipendenze ove applicabile, checksum di release, tag di versione.
- [ ] Prova di installazione su Windows senza `bin`/`obj` preesistenti e prova CLI completa su un caso analitico.
- [ ] Casi analitici tracciati; eventuale mancanza di confronto con altro software FEM dichiarata, non taciuta.
- [ ] Test di scalabilità su almeno un modello più grande prima di pubblicizzare prestazioni su strutture complesse.

**Esclusioni deliberate:** dimensionamento normative acciaio/legno, capacità fuori piano, progettazione esecutiva, FEM 3D, BIM, plasticità globale del telaio, secondo ordine iterativo generale e GUI. Tali capacità non sono implicitamente validate dal completamento di M85.

**Criterio decisionale:** M86 può essere una **release tecnica preview del Core elastico** con limiti dichiarati; non un prodotto di verifica strutturale qualificato né un parere di sicurezza su opere reali.

### Gate M86 RC1 automatico

`./scripts/run-m86-release-gate.ps1` controlla build warning-as-error, regressione completa, esecuzione CLI da JSON, ULS1 e generazione dei quattro tipi di report principali. **Restano manuali** l’audit numerico/visivo dei PDF, l’intersoftware, le verifiche licenze/asset per eventuali binari e il tag Git.
