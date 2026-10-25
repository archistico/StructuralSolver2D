# P0-D0M3 — rapporto elastico ψ ricavato dal FEM, NTC 2018

**Checkpoint 25/10/2026.** Nuovo diagnostico NTC per la **sola anima** di IPE/HEA/HEB laminate, ad ali parallele, doppiamente simmetriche, non forate, sotto **N e M forte concomitanti** in una **singola stazione FEM**, **V=0**. Converte i risultati FEM in tensioni ai bordi dell'anima; non richiede più che l'utente dichiari `ψ` per questo diagnostico. **40/40 casi Python esatti PASS**, xUnit C# **PENDING**: non è disponibile l'SDK .NET. Nessuna modifica al FEM e alle regole EC3.

## Riscontro normativo e confine

Fonte primaria: [NTC 2018, D.M. 17 gennaio 2018, §4.2.3.1, Tabella 4.2.III (Gazzetta Ufficiale)](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf). Soglia di **classe 3** per la parte interna (anima), con `ε=√(235/fyk)`:

- `ψ>−1`: `c/t ≤ 42 ε /(0,67+0,33ψ)`;
- `ψ≤−1`: `c/t ≤ 62 ε (1−ψ)√(−ψ)` **solo** con nota verificata (`σ_compressione ≤ fyk` **oppure** `ε_trazione > fyk/E`).

Il diagnostico implementa **`ψ>−1` con N FEM negativo** (compressione netta), **`ψ<−1` con N FEM positivo** (trazione netta, ma un bordo ancora compresso). Esclude `ψ=−1` in presenza di N non nullo: in una I/H doppiamente simmetrica, con modello elastico lineare, quel valore corrisponde a **N=0, flessione pura**, già coperta da P0-D0. Se nessun bordo piatto dell'anima è compresso, l'analisi mista viene rifiutata.

**Distinzione fondamentale:** conoscere il rapporto elastico `ψ` basta a interrogare il limite di **classe 3**, ma **non** a determinare le classi plastiche **1 o 2**: occorre `α`, ricavato da un distinto modello plastico verificato. La risposta è quindi *anima classe 3 o migliore nel modello elastico dichiarato* oppure *anima sopra il limite di classe 3 (classe 4 nel modello assunto)*; **non** è la classe definitiva dell'intera sezione, non verifica l'ala né la stabilità, non restituisce Aeff/Weff o capacità resistente.

## Come si ricava ψ senza dichiararlo

Il modello usa area **lorda** `A` in mm² e momento d'inerzia sull'asse **forte** `I` in mm⁴, entrambi **provenienti da una fonte identificata**; la geometria nominale fornisce `c_w=h−2(tf+r)` e la distanza `y=c_w/2` dei bordi **piatti** dell'anima dal baricentro della sezione simmetrica.

Per la **stessa** combinazione, stazione, lato e asta FEM, con compressione N negativa e trazione N positiva:

\[
C=-\frac{1000N_{FEM}}{A},\qquad
B=\frac{10^6|M_{FEM}|\,(c_w/2)}{I}
\]

\[
\sigma_c=C+B,\qquad \sigma_{altro}=C-B,\qquad
\psi=\frac{\sigma_{altro}}{\sigma_c}.
\]

Le tensioni sono in MPa, N in kN e M in kNm. `σ_c` deve essere **positiva**; `B>0`. L'algoritmo verifica i valori finiti e rifiuta caratteristiche incompatibili con i limiti geometrici elementari: `A<h·b` e `I<A·(h/2)²`. Non inventa l'inerzia a partire da rettangoli, perché i raccordi dei laminati ne impedirebbero una ricostruzione esatta dalla sola terna h/t/r.

I valori OPPO `AreaCm2` e `StrongInertiaCm4` del catalogo esistente vengono convertiti rispettivamente con ×100 e ×10.000 e marcati **NON CERTIFICATI**; serve comunque prova esterna di geometria, acciaio, assenza di fori e allineamento asse forte ↔ momento FEM. Per esempi analitici si possono fornire A/I indipendenti dalla tabella.

### Nota NTC per ψ<−1

La prima alternativa `σc≤fyk` è verificata **direttamente** grazie alla tensione compressiva calcolata. Se è rispettata, l'altra alternativa non è necessaria. Se **non** lo è, servono **entrambi** deformazione a trazione e modulo elastico E, con provenienza esterna; `εt>fyk/E` è strettamente maggiore. Il codice **non** presume che la deformazione reale si ottenga dividendo la tensione teorica oltre snervamento per E.

Questo controllo della nota **non trasforma** una tensione elastica nominale in prova di idoneità a SLU. Le ipotesi su materiale, deformazioni effettive, comportamento plastico e stabilità richiedono ulteriori verifiche.

## Casi numerici verificati indipendentemente

Per l'esempio di sola aritmetica `h=224 mm`, `tf=8 mm`, `r=4 mm`, `c_w=200 mm`, `A=10000 mm²`, `I=50.000.000 mm⁴`, `fyk=235 MPa`, `M=10 kNm`:

| N FEM | C | B | Tensioni ai bordi | ψ | Formula classe 3 |
|---|---:|---:|---|---:|---|
| −100 kN | +10 MPa | 20 MPa | +30 / −10 MPa | −1/3 | `42/(0,67−0,11)=75` |
| +100 kN | −10 MPa | 20 MPa | +10 / −30 MPa | −3 | `248√3≈429,55` |

Con `N=+3000 kN` e `M=300 kNm`, la tensione compressiva nominale diviene `300 MPa`: **la prima alternativa della nota fallisce** e il diagnostico richiede la seconda (ad esempio `εt=0,003`, `E=210000 MPa`). L'uguaglianza `εt=fyk/E` **non** basta. Con N positivo ma M insufficiente a produrre un bordo compresso, l'operazione fallisce chiusa.

Un caso con `c/t=100` e `ψ=−1/3` supera il limite 75 e viene riportato come **limite elastico di classe 3 superato** senza tentare una verifica resistente di classe 4. Gli esempi con A/I scelti per avere risultati razionali semplici sono **esercizi aritmetici**, non sezioni reali certificate.

**Controprova su geometria di catalogo (dati nominali OPPO, non certificati):** IPE 200 `h=200`, `tw=5,6`, `tf=8,5`, `r=12 mm`, `A=28,48 cm²`, `Ix=1943 cm⁴`; con `N=−100 kN` e `M=10 kNm`, `ψ=−8029/105179≈−0,07633653106`, limite classe 3 `≈65,13557285747`. Il web `159/5,6≈28,39` è sotto questa soglia. **Non** significa che una IPE 200 così caricata sia verificata o che la sua classe definitiva sia 3.

## Programma, test e condizioni di sicurezza

- Nuovo `Ntc2018SteelMixedElasticWebScreenerD0M3` nella libreria `Verification.Steel`, con percorsi `ScreenGeometry` e `ScreenCatalogProfile`.
- Reimpiegate soltanto strutture dati esistenti per sezione e prove documentali; **nessun risultato EC3 M90-B viene promosso a risultato NTC**.
- Risultato dedicato `Ntc2018SteelMixedElasticWebScreenD0M3`, che espone `ψ`, le due tensioni, la soglia e l'esito di ciascuna alternativa della nota. I flag `EstablishesDefinitiveSectionClass`, `ProvidesSectionResistanceOrNmInteraction` ed `EstablishesCompleteMemberCompliance` restano sempre **false**.
- Nuovi test xUnit positivi/negativi: segno N/M, nota OR, soglia stretta, assenza compressione, M/N/V vietati, input NaN/∞, A/I, provenienza, catalogo OPPO e separazione EC3.
- Golden indipendenti **40/40 PASS** in `benchmarks/independent/p0d0m3/verify_p0d0m3_ntc_elastic_psi.py`, basati su frazioni razionali e maggiorazioni/minorazioni esatte di `√3`, con controllo aggiuntivo dei dati nominali IPE 200. **Non importano né eseguono il C#**.
- Test .NET **PENDING** per D0M3 **e** per C2B/C2C0/C2C1/D1/D0/D0M1/D0M2; nessun nuovo PASS .NET dichiarato.

Quando disponibili Windows e SDK:

```powershell
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~Ntc2018SteelMixedElasticWebScreenD0M3Tests|FullyQualifiedName~Ntc2018SteelMixedPsiBelowMinusOneD0M2Tests|FullyQualifiedName~Ntc2018SteelMixedClassificationD0M1Tests' --logger 'console;verbosity=normal'
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --logger 'console;verbosity=normal'
.\scripts\run-g7-tests.ps1
python .\benchmarks\independent\p0d0m3\verify_p0d0m3_ntc_elastic_psi.py
```

## Prossime tappe, in ordine rigoroso

1. Confronto indipendente degli stress FEM e delle sezioni OPPO e collaudo della suite .NET quando possibile.
2. **P0-D0M4:** ricavare in modo verificabile `α` dalla posizione dell'asse neutro plastico, considerando la geometria reale, i raccordi, il grado d'acciaio e le coppie N/M compatibili; solo dopo integrare con D0M1/D0M2 per una classificazione 1–4 sostenibile.
3. **P0-D2:** resistenza della sezione/interazioni N-M-V, poi **P0-E** instabilità e ritegni, **P0-C3/C4** copertura globale, **P0-D3** SLE; documentare tutte le esenzioni e condizioni fuori dominio.
4. **GUI C#/Avalonia** esclusivamente nella fase finale, dopo validazione del motore e dei verificatori.

**Messaggio di sicurezza:** un responso positivo sul limite elastico dell'anima non dimostra la sicurezza della sezione, della membratura o dell'edificio. Chi legge un output D0M3 deve vedere questo confine senza ambiguità.
