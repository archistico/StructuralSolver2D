# G1 — Profilo NTC 2018: coefficienti e tracciabilità

**Stato:** implementazione per gate Windows .NET 8. **Non è una regola di verifica NTC né un certificato di conformità.**

## Normativa implementata (perimetro dichiarato)

- Decreto ministeriale 17 gennaio 2018, NTC 2018, §4.4.6 e tabelle 4.4.III (gamma_M legno), 4.4.IV (kmod), §§4.4.4–4.4.5 (durata e classe di servizio).
- Circolare n. 7 C.S.LL.PP. 21 gennaio 2019, §§C4.4.6 (requisiti colonna B), C4.4.8.1.9 (coefficiente di riduzione larghezza per taglio, kcr).
- NTC 2018, §2.6.1 tabella 2.6.I colonna A1: **solo** fattori parziali delle azioni per STR ordinario non geotecnico, non coefficienti psi né generazione di combinazioni.
- NTC 2018, §4.2.4.1.1 tabella 4.2.VII: costanti informative per acciaio (senza integrare regole EC3 come NTC).
- Dati di riferimento NTC: `catalogs/m92/ntc2018-timber-coefficients-g1.json`, con valori controllati dal codice per evitare modifiche silenziose.

Le clausole appartengono all'edizione NTC 2018 e non diventano automaticamente conforme a successive revisioni né a Eurocodici/Annessi Nazionali.

### Tabella dei coefficienti legno

| Materiale | gamma_M A SLU fondamentale | gamma_M B SLU fondamentale | gamma_M eccezionale |
|---|---:|---:|---:|
| Massiccio (C16/C24/C30) | 1,50 | 1,45 | 1,00 |
| Lamellare omogeneo (GL24h/GL28h) | 1,45 | 1,35 | 1,00 |

La colonna B è subordinata a: attestazione di produzione continuativa e controllo, sistema di qualità/documentazione pertinente NTC 11.1/11.7, coefficiente di variazione delle resistenze <= 15%. Il selettore controlla la presenza e la coerenza formale delle evidenze **dichiarate dal chiamante**, non la loro autenticità. In assenza delle evidenze non sceglie implicitamente la colonna A: l'input B viene rifiutato.

`kmod` classe 1/2: 0,60 / 0,70 / 0,80 / 0,90 / 1,10; classe 3: 0,50 / 0,55 / 0,65 / 0,70 / 0,90 (permanente, lunga, media, breve, istantanea). Se più azioni di diverse classi partecipano, si usa la più breve fra **quelle con coefficiente non nullo**. I fattori negativi non sono ignorati. Le azioni a coefficiente zero restano nel manifest, ma non governano `kmod`.

Per il taglio il §C4.4.8.1.9 propone `kcr=2,0/fv,k` per massiccio e `2,5/fv,k` per lamellare, con `fv,k` espresso numericamente in MPa. G1 produce `kcr` soltanto se l'applicabilità della formula è dichiarata e documentata. L'applicabilità può anche essere negata esplicitamente: non equivale a una dichiarazione di verifica al taglio soddisfatta.

### STR A1 (fatti normativi, non motore di combinazione)

| Azione | Favorevole | Sfavorevole |
|---|---:|---:|
| G1 permanente strutturale | 1,00 | 1,30 |
| G2 permanente non strutturale | 0,80 | 1,50 |
| Q variabile | 0,00 | 1,50 |

L'eccezione per G2 di intensità ben definita richiede attestazione esplicita; quando la si invoca, si applicano i valori del G1. `psi0/psi1/psi2`, casi favorevoli/sfavorevoli, combinazioni conduttrici, generatori di scenari, EQU/GEO/sisma e altre condizioni non sono convalidati da G1. In particolare **i fattori di ogni azione nel manifest provengono dall'esterno**: il codice ne verifica finitezza e identità, non l'intero processo di combinazione.

## API

- `Ntc2018TimberCoefficientTablesG1.Load(path)` / `.FromJson` — tabella autonoma NTC.
- `Ntc2018TimberCoefficientSelectorG1.Fingerprint(analysisId, combinationId, actions)` — impronta SHA-256 deterministica dei dati dichiarati. **L'uguaglianza di due fingerprint non dimostra che l'elenco sia davvero completo o corrisponda al FEM**; ciò richiede attestazione esterna. Non utilizzare il fingerprint come firma crittografica/autenticazione.
- `Ntc2018TimberCoefficientSelectorG1.Select(input)` — produce `NormativeProfile` NTC 2018, assegnazione corredata e audit; nessuna prova di resistenza e nessun `IDesignCheckRule`.
- `Ntc2018StrA1ActionFactorsG1.Factor(...)` — gamma_F di base, esclusi psi e fattori composti.
- `Ntc2018SteelPartialFactorsG1` — informazioni tabellari, senza cambiare i precedenti risultati acciaio EC3.

Il chiamante deve fornire **identità FEM** (`AnalysisId`, `TargetId`, `MemberId`, `SectionId`), `ServiceClass` e motivo, provenienza del materiale, situazione di progetto, dati sulle azioni e impronta attesa dal contesto di analisi. Combinazioni con fattori nulli possono esistere; il selettore richiede almeno una componente non nulla. Gli errori di dati non possono essere convertiti in `Passed`.

### Cosa non fa G1

1. Non crea le combinazioni NTC §2.5.3 complete, non calcola i coefficienti psi e non verifica che la combinazione sia quella più gravosa.
2. Non certifica prodotti, classificazioni ambientali o correttezza delle dichiarazioni di produzione.
3. Non esegue verifiche di resistenza NTC: il motore M92-A **rimane EC5**, non viene ribattezzato NTC.
4. Non verifica interazioni N-M-V, instabilità, SLE, ritegni laterali, collegamenti, torsione, sisma, incendio né conformità globale.
5. Non copre automaticamente un nuovo Eurocodice (EN 1995-1-1:2025) o aggiornamenti normativi.

## Sviluppo successivo

- **G2**: regole di sezione **native NTC**, con `kh` conservativo/giustificato, `kcr`, geometria, trazione netta, contemporaneità FEM.
- **G3**: formule NTC per flessione, tensoflessione e pressoflessione con controlli di concomitanza.
- **G4**: verifiche di stabilità anche fuori piano solo con evidenza reale dei ritegni.
- **G5**: deformazioni istantanee/differite e SLE.
- **G6-G7**: copertura, report §10.2 NTC e validation manual (M92-N5/N6).
- **GUI**: solo dopo il completamento dei moduli numerici e normativi.

## Esempi di riferimento per il futuro collaudo

Con C24, classe servizio 1, durata media, SLU STR fondamentale, colonna A:
`kmod=0,80`, `gamma_M=1,50`, `kcr=2/4=0,50`; pertanto `fm,d=24×0,8/1,5=12,8 MPa` e una sezione 100×200 mm sotto **sola** flessione 6 kNm ha tensione 9 MPa, rapporto 0,703125. Questo è un controllo aritmetico dimostrativo, non l'esito della verifica NTC di una trave reale.

Eseguire su Windows:

```powershell
.\scripts\verify-g1-manifest.ps1
.\scripts\run-g1-tests.ps1
```

La cartella include tutti i sorgenti di produzione e test della baseline M92-B senza modifiche a file `.cs` già esistenti. Sulla macchina che genera questo pacchetto .NET non è installato; compilazione/test Windows sono il gate reale.
