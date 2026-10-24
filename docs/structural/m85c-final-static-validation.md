# M85-C — Gate di validazione finale del nucleo FEM elastico statico 2D

**Data:** 24 ottobre 2026  
**Stato aggiornato 24/10/2026:** **test M85-C superati su Windows .NET 8**, build 0 avvisi/errori, 8 test mirati e 1.334 regressione (log utente 14:42); descrizione dell’implementazione sottostante resta storica.  
**Baseline:** M85-B FIX1, compilazione Release senza errori/avvisi, 5 test integrati, 1.326 test di regressione, 8 benchmark opzionali M83 passati, confermati dal log dell'utente del 24/10/2026 alle 14:25 circa.

## Obiettivo

Consolidare il rilascio del *nucleo statico elastico* attraverso casi analitici ulteriori e proprietà metamorfiche. Nessuna funzione normativa, GUI o solutore nuovo. Nessuna modifica ai 174 file sorgente di produzione `src/`.

**Avvertenza:** il titolo "finale" indica l'ultimo gate interno pianificato per la **prima release limitata**, non una qualificazione generale per uso professionale. Anche un esempio numerico pubblicato sul web non equivale a una validazione incrociata esaustiva con un programma FEM indipendente, versione e input congelati.

## Matrice M85-C: 8 nuovi casi xUnit

Test: `tests/StructuralSolver2D.Analysis.Tests/Validation/M85/FinalReferenceAndRobustnessM85CTests.cs`.

| Casi | Modello | Riferimento indipendente / proprietà |
|---|---|---|
| 1 | Reticolare triangolare isostatica A(0,0), B(6,0), C(3,4), P=40 kN in C, EA differenti | Equilibrio dei nodi: reazioni verticali 20+20 kN, diagonali in compressione −25 kN e corrente basso in trazione +15 kN; `uCy=−Σ(Nᵢ²Lᵢ/(EAᵢ))/P` da lavoro virtuale; Truss2D e PlaneStructure2D confrontati |
| 2–3 | Trave continua a due campate uguali: L=4 m/q=8 kN/m e L=3 m/q=5 kN/m | Teorema dei tre momenti: `M_B=−qL²/8`; `R_A=R_C=3qL/8`, `R_B=5qL/4`; integrazione Euler–Bernoulli: `θ_A=−qL³/(48EI)`, `θ_B=0`, `θ_C=+qL³/(48EI)` |
| 4 | Esempio esterno pubblicato: L=4 m, q=3 kN/m in ciascuna campata | Reazioni 4,50/15,00/4,50 kN; momento in appoggio B con modulo 6,00 kNm; confronto con valori numerici pubblicati, non solo con una formula interna |
| 5–6 | Stessa trave: E moltiplicato per 0,1 o 10 | Per K→sK, con forze assegnate e nessun cedimento imposto, rotazioni → rotazioni/s; reazioni e momenti di estremità invariati |
| 7 | Reticolare triangolare con nodi, aste e supporti in ordine invertito | Confronto per identificatore, non indice: spostamenti, reazioni e sforzi assiali invariati |
| 8 | Stessa reticolare traslata rigidamente di (10⁸,−10⁸) m | Invarianza per traslazione delle coordinate (test numerico, non modello geometrico reale) |

### Fonti esterne effettivamente consultate (24 ottobre 2026)

- **ToolFoundry, Continuous Beam Calculator:** <https://www.toolfoundry.app/engineering/continuous-beam-calculator> — esempio pubblico con **L=4 m, w=3 kN/m**, risultati **R_A=R_C=4,50 kN, R_B=15,00 kN, |M_B|=6,00 kNm**. Numeri registrati come asserzioni esplicite nel test `PublishedWorkedExample_TwoSpans4mAnd3kNperM_MatchesIndependentNumbers`. La pagina è dinamica e non versionata: l'esempio fissato nelle asserzioni rimane riproducibile, ma la provenienza non è un dataset certificato.
- **Engineering LibreTexts / Alderliesten, Virtual Work Method:** <https://eng.libretexts.org/Bookshelves/Mechanical_Engineering/Introduction_to_Aerospace_Structures_and_Materials_%28Alderliesten%29/03%253A_Analysis_of_Statically_Indeterminate_Structures/08%253A_Deflections_of_Structures-_Work-Energy_Methods/8.01%253A_Virtual_Work_Method> — formula generale `Δ=Σ nᵢ Nᵢ Lᵢ/(Eᵢ Aᵢ)` per reticolari, utilizzata per derivare il riferimento analitico del nostro modello geometrico (non è il medesimo esempio numerico del manuale).
- **Structure Bench, A continuous beam:** <https://structurebench.com/learn/beams-and-frames/a-continuous-beam> — confronto della convenzione del momento negativo su supporto interno per due campate sotto carico uniforme.

**Livello di indipendenza:** una prova confronta valori numerici esterni pubblicati; le altre sono formule manualmente derivate / proprietà matematiche, non dati di un secondo solutore. Il confronto intersoftware su più modelli resta un punto aperto per M86.

### Prove indipendenti e ambito

- Equilibrio statico, teorema dei tre momenti per trave prismatico Euler–Bernoulli, teorema dei lavori virtuali per reticolari. Le formule sono presenti nei commenti del codice: non derivano dalle matrici implementative.
- Lo spostamento virtuale è stato anche ricontrollato con un sistema matriciale di sole tre incognite costruito separatamente in fase di preparazione; questo controllo non sostituisce .NET/xUnit.
- Unità: `kN, m, kNm, rad`, `E` in `kN/m²`, `A` in `m²`, `I` in `m⁴`.
- Test deterministici: tolleranza sui valori restituiti `max(tol_assoluta, 10⁻⁸ · |atteso|)` con `tol_assoluta` tipicamente `10⁻¹⁰` per spostamenti/rotazioni e `10⁻⁸` per reazioni/sollecitazioni. Non si forza il passaggio dei test modificando il motore.
- La traslazione di `10⁸ m` verifica soltanto il corretto utilizzo di coordinate relative con differenze esattamente rappresentabili, non una robustezza arbitraria su tutte le scale possibili.

## Come eseguire

```powershell
.\scripts\run-m85c-tests.ps1
```

Il runner: pulizia sicura di eventuali `bin/obj`, build Release, **8 casi M85-C**, regression numerica M85-A/M82/M83 e casi limite, 5 casi cross-layer M85-B, poi suite completa dei 4 progetti Core/Analysis/Reporting/CLI. Errori o warning di build devono essere riportati al progetto; lo script termina con errore se un gate fallisce. Log testuale in cartella temporanea Windows, come i runner precedenti.

## Risultati prestazionali ricevuti (benchmark M85-B FIX1)

Test eseguiti su Windows, .NET 8.0.29, in modalità Release, 20 ripetizioni. Tempi mediani:

| Frame2D primo ordine | 8 aste: 0,188 ms | 16 aste: 0,311 ms | 32 aste: 0,537 ms | 64 aste: 1,207 ms |
|---|---|---|---|---|
| Frame2D M82 | 2 aste: 0,191 ms | 4 aste: 0,397 ms | 8 aste: 0,901 ms | 16 aste: 2,268 ms |

La scala è troppo piccola per stabilire la necessità di un solutore sparse; M83-B rimane rinviato. Il benchmark temporale è diagnostico, non costituisce un gate di sicurezza o accuratezza.

## Gate di M86 e rischi residui

**M86 può essere avviato** una volta superato il gate xUnit M85-C su Windows; per *pubblicare* la prima release sono necessari anche:

1. Revisione delle limitazioni funzionali per ogni API: statico elastico di primo ordine come nucleo stabile; M82 globalmente linearizzato, senza aggiornamento iterativo dell'equilibrio e con input limitati; moduli dinamici/non lineari isolati e non qualificati dal solo gate M85-C.
2. *README* operativo con esempi JSON, versione .NET, sistema unità, convenzioni di segno, interpretazione dei carichi e diagnostica delle strutture instabili.
3. Test di installazione pulita (estrazione o clone), `dotnet build`, comandi CLI d'esempio e verifiche di apertura di PDF/XLSX/CSV; copertura numerica delle tabelle PDF e collaudo visuale manuale ancora aperti.
4. Referenze esterne tracciabili: un confronto intersoftware o dataset pubblicato con identificazione precisa della versione, condizioni al contorno, proprietà e risultati comparabili resta **aperto**; gli attuali casi analitici non possono essere presentati come controllo esterno già svolto.
5. Licenza, dipendenze e third-party notices, commit/tag, riproducibilità della build, report della regressione e archivio con manifest/checksum.

**Non necessari per M86**: solver sparse, plasticità globale Frame2D, secondo ordine generale non lineare, verifiche EC3/EC5/NTC, UI Avalonia. Gli ultimi due blocchi sono successivi per scelta di roadmap, non eliminati.
