# P0-D0M5 — raccordo automatico α/ψ e classe NTC dell'I/H sotto N+M forte

**25/10/2026 — nuova implementazione, NON compilata né testata con .NET in questa sessione.**

## Risultato e riferimento normativo

Per una **sezione** IPE/HEA/HEB laminata doppiamente simmetrica, senza fori, soggetta a **sforzo normale non nullo + flessione non nulla intorno all'asse forte** nella **stessa stazione FEM**, è disponibile un'unica procedura che:

1. ricava `α` con D0M4 dall'equilibrio plastico **solo se** l'asse neutro rimane strettamente nell'anima piatta;
2. ricava `ψ` con D0M3 dalla distribuzione di tensioni **elastiche** determinata da N/M e dalle proprietà gross `A`/`I` con fonte;
3. verifica l'identità rigorosa dei due risultati (norma/edizione, membro/sezione, grado, analisi, combinazione, ascissa, lato, N/M, fyk e snellezza geometrica);
4. usa il **classificatore nativo NTC D0M1/D0M2 esistente** per i limiti di **classe 1/2/3/4 dell'anima** e dell'**ala**; la classe della sezione è la peggiore delle due;
5. controlla nuovamente che il limite di classe 3 dell'anima coincida con la precedente valutazione elastica D0M3.

La fonte vincolante è il **D.M. 17/01/2018**, NTC 2018 **§4.2.3.1, Tabelle 4.2.III e IV**, Gazzetta Ufficiale 20/02/2018, Supplemento n. 8, PDF p. 98 e seguenti:

<https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf>

Il riferimento dei profili commerciali è OPPO IPE/HEA/HEB, **indicativo e non certificato**, ad esempio <https://www.oppo.it/tabelle/profilati_ipe.htm>. La proprietà reale del prodotto va documentata separatamente.

### Una sottigliezza normativa risolta: nota `ψ≤−1`

La Tab. 4.2.III stabilisce una vera alternativa **OR** nel ramo di classe 3 `ψ≤−1`:

- tensione di compressione al bordo dell'anima `σc ≤ fyk`; **oppure**
- deformazione di trazione `εt > fyk/E`, con strain/E dimostrati indipendentemente.

Il vecchio contratto D0M2 chiedeva sempre `εt` ed `E`, anche quando `σc≤fyk` era già dimostrato. D0M5 introduce per il classificatore D0M1 la **nuova evidenza opzionale stress-only**, con due tensioni di bordo positive in modulo e fonte; in questo ramo devono essere verificate sia `−σt/σc=ψ` sia `σc≤fyk`. **Non si inventano deformazioni o moduli elastici.** La vecchia strada D0M2 con `εt/E` esterni rimane invariata e accessibile. Evidenze simultanee incompatibili o numericamente scorrette sono rifiutate.

## Ambito e limiti irrinunciabili

| Requisito | Trattamento |
|---|---|
| Normativa | Solo profilo **NTC2018 nativo**, non regole EC3 riutilizzate come se fossero NTC |
| Geometria | I/H laminata ad ali parallele, doppia simmetria, non forata, raccordi geometricamente plausibili |
| Asse neutro plastico | **Strettamente dentro l'anima piatta**: `|N| < fyk·tw·c/1000`, `c=h−2(tf+r)` |
| Proprietà elastiche | A/I lorde con fonte, non ricostruite con un'area rettangolare che cancella i raccordi |
| Effetti FEM | Stesso membro, analisi, combinazione, ascissa e lato, N≠0/M forte≠0, **V=0** |
| Nota `ψ≤−1` | Deve riuscire una delle due alternative, senza inferire strain dai soli stress elastici |
| Classe 4 | **Identificata**, ma NON calcola Aeff, Weff, shift del baricentro o capacità |
| N/M/V e stabilità | Nessuna verifica resistente o interazione, nessuna instabilità locale/globale, ritegno o trave intera |
| Copertura | La classe riguarda **questa sezione in questa combinazione**, non tutte le stazioni né tutte le combinazioni |

Con un asse neutro nei raccordi/ali o una distribuzione fuori dalle ipotesi di D0M4/D0M3 il calcolo **si ferma**. Una **sezione di classe 1** può comunque **non resistere** a N/M e fallire per instabilità, combinazioni mancanti o geometria reale non attestata. Il nuovo output espone flag di conformità/resistenza sempre **false**.

## Esempi incrociati

Per **IPE200 S235 nominale**, da tabella OPPO: `h=200, b=100, tw=5,6, tf=8,5, r=12 mm`, `A=2848 mm²`, `I_forte=19.430.000 mm⁴`.

| Caso FEM, V=0 | α plastico D0M4 | ψ elastico D0M3 | Classe entro l'ambito |
|---|---:|---:|---:|
| N=−100 kN, M=+10 kNm | ≈0,739 | ≈−0,07634 | 1 |
| N=+100 kN, M=+10 kNm | ≈0,261 | inferiore a −1 | 1, nota `σc≤fyk` |
| N=+100 kN, M=+68,5 kNm | ≈0,261 | inferiore a −1 | Calcolabile **solo** con strain/E esterni che soddisfino la nota; il primo ramo non vale |

Il terzo esempio **non** è una verifica di resistenza: il momento scelto serve a esercitare la nota su tensione compressiva maggiore di `fyk` e non è dichiarato ammissibile.

Una famiglia di quattro geometrie I/H sintetiche simmetriche, con lo stesso `N=−100kN` e momenti scelti per fissare `ψ=−0,5`, produce classi dell'anima **1,2,3,4** per `c/tw = 60,70,80,90`, con l'ala in classe 1. Sono **benchmark aritmetici**, non profili commerciali certificati.

## Implementazione, non regressione e test

- `src/StructuralSolver2D.Verification.Steel/Ntc2018SteelMixedAutomaticSectionD0M5.cs`: API `ClassifyCatalogProfile` / `ClassifyGeometry`, risultato composto con tre diagnosi, classe entro l'ambito, necessità di proprietà efficaci, tracciabilità e flag prudenti.
- Modifica **mirata e compatibile** di `Ntc2018SteelMixedIClassificationD0M1.cs`: nuova proprietà opzionale `PsiBelowMinusOneStressOnlyEvidence`, ramo normativo OR basato sugli stress; tutti i record e casi originali D0M1/D0M2 restano supportati.
- `tests/StructuralSolver2D.Verification.Tests/D0/Ntc2018SteelMixedAutomaticSectionD0M5Tests.cs`: catalogo OPPO, classi 1–4 sintetiche, N compressivo/tensile, prima/seconda alternativa della nota, invalidità delle prove, mismatch FEM/edizione e fail-closed per raccordi, taglio, carichi puri e bordi non compressi.
- `benchmarks/independent/p0d0m5/verify_p0d0m5_ntc_mixed_section_class.py`: **49/49 golden aritmetici indipendenti PASS** con quantità razionali ed esiti delle classi; non importa C#.

**Non sono stati modificati** motore FEM, servizi EC3 M89–M91, legno EC5/NTC, G8B, contratti di integrazione C2B/C2C0/C2C1, visualizzatore, GUI né i cataloghi.

Da eseguire su Windows con SDK .NET 8:

```powershell
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~Ntc2018SteelMixedAutomaticSectionD0M5Tests|FullyQualifiedName~Ntc2018SteelMixedPlasticWebAlphaD0M4Tests|FullyQualifiedName~Ntc2018SteelMixedElasticWebScreenD0M3Tests|FullyQualifiedName~Ntc2018SteelMixedPsiBelowMinusOneD0M2Tests|FullyQualifiedName~Ntc2018SteelMixedClassificationD0M1Tests' --logger 'console;verbosity=normal'
```

Nell'ambiente corrente il tool `dotnet` è assente: **D0M5 e gli altri gate .NET recenti restano PENDING**. I PASS precedenti di G7/B2/B3/B4/C1 FIX1/C2A sono conferme **storiche dell'utente**, ciascuna nel proprio ambito. Non attribuire a 49 golden Python la compilazione, il test dell'assembly C# o l'idoneità di un membro.

## Roadmap

1. **P0-D0M6**: estendere in modo giustificato la posizione dell'asse neutro quando attraversa i raccordi e le ali, solo con **geometria reale integrabile** (oppure dichiarare esplicitamente il gap come non supportato). Verificare limiti di arrotondamento e stati prossimi alle soglie.
2. **P0-D2**: proprietà efficaci di classe 4 e verifiche vere di resistenza N/M/V/interazioni con fattori NTC, incluse condizioni per taglio elevato.
3. **P0-E**: stabilità e ritegni fuori piano, imperfezioni; **P0-C3/C4** audit di copertura su tutte le stazioni/combinazioni; **P0-D3** SLE acciaio.
4. **GUI Avalonia soltanto P3**, dopo motore, verifiche e validazione.

**Nessun risultato D0M5, anche di classe 1, deve essere pubblicato come «struttura verificata» o «membro conforme».**
