# M90-A — classificazione delle sezioni I/H in acciaio

**Stato:** implementazione candidata, test .NET 8 da confermare su Windows.

## Scopo

Primo classificatore secondo **EN 1993-1-1:2005+A1:2014**, §5.5, prospetto 5.2, fogli 1 e 2, limitato a:

- profili laminati a caldo **IPE, HEA, HEB ad ali parallele e doppiamente simmetrici**;
- **compressione uniforme pura** oppure **flessione pura intorno all'asse forte**;
- geometrie di catalogo OPPO M88-B2 (dati indicativi, non certificati) e tensioni minime del catalogo M88-C.

**NON supportati:** INP con ali inclinate, pressoflessione N+M, flessione asse debole, parti forate, compositi, saldati non verificati, tubolari, carichi locali, classe 4 efficace, torsione, instabilità globale o latero-torsionale, verifiche di interazione e resistenza. Il risultato è una *classificazione locale*, non un esito di conformità EC3/NTC.

L'API rifiuta espressamente EN 1993-1-1:2022 e le NTC 2018: richiedono un perimetro ed una validazione propri, non la riutilizzazione silenziosa dei coefficienti M90-A. L'Annesso Nazionale deve essere identificato esplicitamente; il classificatore non ne certifica l'applicazione legale.

## Calcolo

Per profili I/H laminati con raccordo di raggio `r`:

- Ala sporgente: `c_f = (b - t_w - 2 r) / 2`, rapporto `c_f / t_f`.
- Anima interna: `c_w = h - 2 t_f - 2 r`, rapporto `c_w / t_w`.
- `epsilon = sqrt(235 MPa / fy)`.

| Sollecitazione e parte | Classe 1 | Classe 2 | Classe 3 | Superamento classe 3 |
|---|---:|---:|---:|---|
| Ala compressa, entrambe le configurazioni | 9 ε | 10 ε | 14 ε | Classe 4 |
| Anima in compressione uniforme | 33 ε | 38 ε | 42 ε | Classe 4 |
| Anima in flessione pura asse forte | 72 ε | 83 ε | 124 ε | Classe 4 |

Le soglie sono **massimi del rapporto `c/t`**. I valori al limite rientrano nella classe meno gravosa; la classe globale è il massimo tra ala e anima.

Nel catalogo M88-C la `fy` di riferimento viene selezionata dalla fascia dello spessore maggiore fra ala e anima, operando in modo prudenziale quando sono diverse, senza estrapolare i dati. Per le prove analitiche `ClassifyGeometry` richiede geometria e `fy` dichiarate esplicitamente.

## Validazione indipendente: Bernuzzi

Riferimento didattico fornito dall'utente: Claudio Bernuzzi, *Progetto e verifica delle strutture in acciaio secondo le NTC 2018 e l'Eurocodice 3*, seconda edizione, Hoepli (2018), **capitolo 4**. Il libro **non** è incluso nel pacchetto ZIP.

- **Applicazione A4.1**, pagine stampate 129–130 (PDF 145–146): IPE 550 S275 a compressione: ala classe 1, anima classe 4, sezione classe 4; rapporto `c_w/t_w ≈ 42,13`, limite classe 3 ≈ 38,83.
- **Applicazione A4.2**, pagine stampate 130–131 (PDF 146–147): HEA 280 S420 a flessione forte: ala classe 3, anima classe 1, sezione classe 3; rapporto ala ≈ 8,62. **S420 è fornito come dato esplicito del test**, perché il catalogo M88-C contiene oggi solo S235/S275/S355.
- **Tabelle 4.4–4.6**, pagine stampate 125–127 (PDF 141–143): confronto a campione di classi IPE/HEA/HEB con geometrie ottenute **indipendentemente** da OPPO. Le fonti possono differire nei decimali per gli arrotondamenti dei dati tabellati.

Questi confronti non sono un'estensione autorizzata della norma, né una certificazione del libro o dei profili.

## API

`SteelISectionClassifierM90A.ClassifyCatalogProfile(edition, profiles, materials, sectionId, steelGrade, action)`

Il risultato conserva `Flange`, `Web`, `Class`, `GoverningParts`, `Epsilon`, `YieldStrengthMpa`, `Evidence` e riferimenti. **Non modifica le regole M89-A/B** e non assume automaticamente che una trave sia verificata. Per una classe 4 la determinazione della sezione efficace richiede moduli futuri, non implementati.

## Gate Windows

```powershell
.\scripts\verify-m90a-manifest.ps1
.\scripts\run-m90a-tests.ps1
```

Il gate esegue compilazione Release con avvisi come errori, verifiche M90 e M89, test cataloghi, e tutta la regressione FEM/Reporting/CLI. Lo script non richiede il PDF originale.

## Avanzamenti successivi

1. **M90-B**: classificazione di sezioni presso-inflesse con parametri `alpha`, `psi` e dimostrazione di concomitanza N/M; verificare su esempi indipendenti.
2. **M90-C**: classificazione e interazioni coerenti con la classe; N, V, M, riduzioni e casi classe 4, come moduli isolati e fail-closed.
3. **M91**: stabilità delle aste e dati indispensabili fuori piano.
4. **M92–M94**: verifiche per il legno; **M95–M99**: SLE, validazione e release normativa; GUI C#/Avalonia soltanto dopo.

**Semplificazione:** mantenere una sottoclasse di problemi risolvibili rigorosamente invece di applicare un classificatore generico a qualsiasi profilo o combinazione.
