# M92-B — Coefficienti EC5 per il legno, prima generazione

Stato: **codice e test predisposti, gate Windows .NET 8 da eseguire**. Baseline M92-A confermata superata dall'utente il 24 ottobre 2026.

## Perimetro

- Estensione isolata di `StructuralSolver2D.Verification.Timber`: `TimberEc5CoefficientTablesM92B` e `TimberEc5CoefficientSelectorM92B`.
- Legno massiccio di conifera (C16/C24/C30) e legno lamellare omogeneo (GL24h/GL28h) del catalogo M88-C.
- EN 1995-1-1:2004+A1:2008+A2:2014: **versione EC5 di prima generazione**, NON la versione EN 1995-1-1:2025.
- `kmod` da tabella 3.1 modificata con A1:2008; valori raccomandati `gamma_M` da tabella 2.3 per **combinazioni fondamentali**. Nessuna selezione automatica per NTC 2018, norme 2025, situazioni accidentali o un Annesso Nazionale specifico.
- Sono escluse prescrizioni nazionali non accertate, interazioni, instabilità, deformazioni SLE, giunti, dimensione/volume (`kh`, `kl`), effetto dei fori, `kcr` automatico, collegamenti.

## Tabella `kmod` fornita (prima generazione EC5)

| Famiglia | Classe di servizio | Permanente | Lunga | Media | Breve | Istantanea |
|---|---:|---:|---:|---:|---:|---:|
| Massiccio e lamellare | 1 | 0,60 | 0,70 | 0,80 | 0,90 | 1,10 |
| Massiccio e lamellare | 2 | 0,60 | 0,70 | 0,80 | 0,90 | 1,10 |
| Massiccio e lamellare | 3 | 0,50 | 0,55 | 0,65 | 0,70 | 0,90 |

`gamma_M` **raccomandato**, per situazioni fondamentali: **1,30** per massiccio e **1,25** per lamellare. I valori raccomandati non equivalgono all'Annesso Nazionale italiano. Un diverso Annesso Nazionale deve essere identificato e verificato dal progettista; M92-B richiede un valore `gamma_M` fornito esplicitamente con evidenza, senza attribuire al software la verifica del documento.

### Azioni di durata diversa

La selezione del `kmod` di combinazione segue l'azione di **durata più breve fra quelle partecipanti**; il programma non deduce la partecipazione dal solo risultato FEM N/V/M. Devono essere forniti:

1. Identificativi univoci e coefficienti non nulli, finiti, per ogni azione partecipante, inclusi quelli negativi.
2. Classe di durata e provenienza per ogni azione.
3. Conferma esplicita della completezza dell'elenco e del controllo con la combinazione FEM.
4. Classe di servizio documentata e evidenza dell'applicabilità della tabella 3.1 all'edizione/annesso dichiarati.

Fattori nulli vengono **rifiutati** (non fanno parte della lista da passare), coefficienti non finiti o identificativi ripetuti non sono ammessi. Non vengono inventate assegnazioni favorevoli.

### Due modalità `gamma_M`

- `EurocodeRecommendedNotNationalAnnex`: attivabile solo con `NationalAnnexId = EC5-RECOMMENDED-NOT-NATIONAL-ANNEX`. Il dato `gamma_M` è prelevato dalla tabella 2.3 raccomandata per combinazioni fondamentali; **non costituisce un valore verificato dell'Annesso Nazionale italiano**.
- `ExternallyDeclaredNationalAnnex`: richiede identificativo nazionale distinto, valore `gamma_M` finito positivo e fonte dichiarata dall'utente. Il software **non controlla** che quella fonte sia autentica o che il valore sia realmente applicabile; ciò è indicato nell'audit.

`kcr` per taglio rimane facoltativo in fase di preparazione dei coefficienti, ma M92-A ne richiede un valore esplicito e documentato prima di eseguire la verifica a taglio. Nessun valore predefinito.

## Integrazione con M92-A

`Select(...)` restituisce **una coppia** `TimberSelectedCoefficientsM92B`: un `NormativeProfile` con `kmod`, `gamma_M` e `kcr` facoltativo, più un'assegnazione `TimberRectangularAssignmentM92A` che porta la durata determinata dalla lista delle azioni.

Usare `selected.CreateBoundRule(effect, materials)` come regola `IDesignCheckRule` all'interno di `DesignCheckEngine`. L'adapter verifica che **profilo, valori, provenienza, asta, analisi e target** coincidano con la selezione prima di invocare la regola originale M92-A. Questo riduce il rischio di applicare coefficienti da una combinazione differente. Non altera M92-A né il FEM.

Esempio concettuale (dati di progetto da stabilire separatamente): una combinazione G permanente + Q di breve durata, classe di servizio 1, richiede `kmod=0,90` nel perimetro della tabella; il valore di `gamma_M` può essere raccomandato soltanto attivando esplicitamente la modalità non-NA.

## Fonti, provenienza e caveat

- [EN 1995-1-1:2004/A1:2008, §3.1.3, Tabella 3.1 modificata](https://www.iteh.us/catalog/standards/cen/ba742a43-887a-4891-ac7c-604e2144afd4/en-1995-1-1-2004-a1-2008)
- [EN 1995-1-1:2004+A1:2008, §2.4.1 e Tabella 2.3](https://www.phd.eng.br/wp-content/uploads/2015/12/en.1995.1.1.2004.pdf)
- [CNR-DT 206 R1/2018, Tabella B-6: confronto informativo NTC 2018](https://www.certifico.com/component/attachments/download/11641). È una fonte informativa di confronto, **non abilita automaticamente NTC** nel software.

Archivio dati: `catalogs/m92/ec5-first-generation-coefficients.json` (schema 1). Validazione di 6 righe `kmod`, 2 righe raccomandate `gamma_M`, unità adimensionali, valori finiti e tracciabilità. Ogni valore va usato solo nel perimetro della norma applicabile.

## Gate e limiti

Da cartella `StructuralSolver2D`:

```powershell
.\scripts\verify-m92b-manifest.ps1
.\scripts\run-m92b-tests.ps1
```

Il gate comprende build Release con `-warnaserror`, test Verification (M89-M92), cataloghi e regressioni FEM/Core/CLI/Reporting. **I test non costituiscono certificazione dell'affidabilità per impiego su opere reali.**

## Roadmap

- M92-B: selezione e tracciabilità dei coefficienti — gate da eseguire.
- M93-A/B: interazioni e stabilità del legno, separate dalle verifiche pure; grande difficoltà di gestione degli assi e dei ritegni fuori piano.
- M94: riferimenti numerici indipendenti e casi limite EC5.
- M95: SLE, freccia iniziale/finale e deformazioni differite.
- M96–M99: integrazione CLI/report, audit di copertura e release normativa preview.
- GUI Avalonia soltanto al termine del nucleo normativo.
