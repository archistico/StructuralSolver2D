# P0-D0M4 — α plastico dall'equilibrio assiale, anima I/H NTC 2018

**25 ottobre 2026 — implementazione diagnostica; gate .NET PENDING.**

## 1. Che cosa è stato verificato sulla norma

Riferimento primario: **D.M. 17 gennaio 2018**, NTC 2018 **§4.2.3.1, Tab. 4.2.III**, pubblicato in **Gazzetta Ufficiale, 20/02/2018, S.O. n. 8**, pagina stampata 95 / pagina PDF 98:

<https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf>

La tabella distingue la distribuzione **plastica** a due blocchi ±fyk (classi 1/2) e il rapporto delle tensioni **elastiche** ai due estremi dell'anima (classe 3). Nel primo caso **αc** è la parte della larghezza libera **c** dell'anima sottoposta a **compressione nel modello plastico**, per cui `0<α<1`. Non è ψ.

Il riferimento geometrico nominale per il controesempio IPE200 è la tabella OPPO: <https://www.oppo.it/tabelle/profilati_ipe.htm>. Il sito dichiara i propri dati **indicativi**; non sono prova di conformità o certificazione del prodotto.

## 2. Derivazione controllabile — senza «rettangolarizzare» i raccordi

Ambito: sezione **I/H laminata a caldo ad ali parallele, doppiamente simmetrica, non forata**, flessione attorno all'asse forte, N e M **concomitanti** alla stessa stazione/lato del FEM, `V=0`, `N≠0`, `M≠0`. Si presume la distribuzione ausiliaria **perfettamente plastica** con moduli di tensione uniformi `fyk` in trazione/compressione. Non si assume che il momento effettivo del FEM coincida con il momento plastico di tale distribuzione: ciò richiederebbe una **verifica di resistenza N/M** qui assente.

- `h`, `b`, `tw`, `tf`, `r`: altezza, larghezza, spessore anima/ala, raggio di raccordo in **mm**.
- `c=h−2(tf+r)`: altezza del solo tratto piano dell'anima in **mm** (intervallo tra i raccordi).
- `δ`: spostamento dell'asse neutro plastico dal centro, **positivo verso il bordo teso determinato dal segno del momento**. Vale `|δ|<c/2` rigorosamente.
- `Ac=A/2+tw·δ` e `At=A/2−tw·δ` sono le **aree totali** compresse/tese per una sezione simmetrica con asse neutro nel tratto piano dell'anima. Le aree dei due flange+raccordi si elidono esattamente nella differenza; non occorre **inventare** un loro raggio equivalente per ricavare δ.
- Convenzione FEM: `N>0` **trazione**, `N<0` **compressione**.

Dal bilancio plastico `N·1000=fyk·(At−Ac)=−2·fyk·tw·δ` si ottiene:

```text
δ       = −N·1000 / (2·fyk·tw)        [mm]
α       = 0,5 + δ/c
        = 0,5 − N·1000/(2·fyk·tw·c)  [adimensionale]
|N|     < fyk·tw·c/1000               [kN; limite asse neutro in anima piatta]
|N|     < fyk·A/1000                  [kN; controllo plausibilità snervamento assiale lordo]
```

L'area lorda `A` proviene da una **fonte separata**; controlla la plausibilità della sezione e del campo plastico, **non** entra nel rapporto α perché, nel caso rigorosamente simmetrico, le parti non piane si cancellano. La verifica quantitativa dell'area confronta la base rettangolare `2b·tf+tw(h−2tf)` e il massimo ingombro dei quattro quadrati di raccordo `4r²`, con un margine nominale dell'1% dedicato **solo** al controllo di plausibilità, **mai** alla forza o ad α.

**Regola fail-closed:** se `|N|≥fyk·tw·c/1000` (compresa una banda numerica relativa `1e−10` intorno al limite), l'asse neutro tocca o supera la tangente ai raccordi: la formula sopra **non è valida** e il servizio emette `NotSupportedException`. Un metodo futuro dovrà integrare la geometria vera del raccordo e delle ali. Il superamento di questa soglia **non significa** cedimento dell'acciaio: significa **modello diagnostico non applicabile**. Anche sezioni asimmetriche, INP con ali inclinate, fori, saldati, V≠0, assi non attestati, momento assente, stati indefiniti e provenienza FEM insufficiente vengono respinti.

## 3. Riscontro numerico indipendente — OPPO IPE200 S235

| Grandezza | Valore nominale |
|---|---:|
| `h / b / tw / tf / r` | 200 / 100 / 5,6 / 8,5 / 12 mm |
| `A` | 2.848 mm² |
| `c` | 200−2·(8,5+12)=**159 mm** |
| `fyk` | 235 MPa per esempio S235 con spessore ammesso |
| `N / M / V` | −100 kN / +10 kNm / 0 kN |
| `|N|` massimo ammesso **solo** dal modello D0M4 | `235·5,6·159/1000 = 209,244 kN` |
| `δ` | `+100000/(2·235·5,6) ≈ 37,994 mm` |
| `α` | `0,5+100000/(2·235·5,6·159) ≈ 0,739` |

Il segno opposto di N (+100 kN), se resta disponibile un bordo compresso per M, porta **α≈0,261**, come previsto dalla simmetria. L'inversione del segno di M scambia il bordo compresso, non il valore numerico α. D0M3 sul primo caso, separatamente, aveva ricavato `ψ≈−0,07634`, **da non confondere con α**.

## 4. Codice e test

- `src/StructuralSolver2D.Verification.Steel/Ntc2018SteelMixedPlasticWebAlphaD0M4.cs`: `CalculateGeometry` (dati/prove esterne) e `CalculateCatalogProfile` (IPE/HEA/HEB nominali). Oggetto risposta distinto e flag **sempre falsi** per classe definitiva, resistenza al momento, ψ e conformità dell'asta. Trace contiene N/M/V/FEM, grandezze, limite di validità e fonti.
- `tests/StructuralSolver2D.Verification.Tests/D0/Ntc2018SteelMixedPlasticWebAlphaD0M4Tests.cs`: compressione, trazione, specularità e orientamento momento, casistiche raccordi/ali, confini numerici, geometrie/area/provenienza impossibili, EC3 e identificativi FEM errati, catalogo IPE200 e **coerenza identificativa con D0M3 su N/M uguali**.
- `benchmarks/independent/p0d0m4/verify_p0d0m4_ntc_plastic_alpha.py`: **56/56 controlli con frazioni esatte PASS**, inclusi IPE200 e IPE300 nominali, conservazione `At−Ac`, α, limiti e simmetria. Non importa assembly o risultati C#.

Eseguire in ambiente Windows dotato di .NET 8:

```powershell
dotnet test .\tests\StructuralSolver2D.Verification.Tests\StructuralSolver2D.Verification.Tests.csproj -c Release --filter 'FullyQualifiedName~Ntc2018SteelMixedPlasticWebAlphaD0M4Tests|FullyQualifiedName~Ntc2018SteelMixedElasticWebScreenD0M3Tests|FullyQualifiedName~Ntc2018SteelMixedPsiBelowMinusOneD0M2Tests|FullyQualifiedName~Ntc2018SteelMixedClassificationD0M1Tests' --logger 'console;verbosity=normal'
```

**Stato:** SDK `dotnet` assente in questo ambiente; **test C# non compilati/eseguiti**, gate D0M4 **PENDING**. Restano PENDING Windows anche C2B/C2C0/C2C1/D1/D0/D0M1/D0M2/D0M3. Storicamente l'utente ha confermato PASS per G7, B2/B3/B4, C1 FIX1 e C2A nel loro specifico perimetro.

## 5. Prossime attività e sicurezza d'uso

1. **P0-D0M5:** costruire un raccordo **verificabile e conservativo** α/ψ, controllare la coerenza esatta degli identificativi/proprietà/edizione e le note NTC; solo allora interrogare le soglie delle classi 1–4 di anima **e ala** in un risultato unico, con controprove indipendenti. Il D0M1/D0M2 manuale **non è stato modificato** in D0M4.
2. **Fuori dal sottoinsieme dell'anima piana:** considerare una rappresentazione della sezione completa capace di integrare realmente raccordi e zone di ala, evitando false estrapolazioni dell'equazione sopra.
3. **P0-D2/E/C3/C4/D3:** resistenze e interazioni N/M/V, aree efficaci classe 4, stabilità e ritegni fuori piano, copertura normativa/provenienza per membro, stati limite di esercizio acciaio e infine la GUI Avalonia **solo nella fase P3**.

**Avvertenza:** una classificazione del singolo piatto, una distribuzione plastica di riferimento e un esito positivo di uno script aritmetico **non** attestano la sicurezza della sezione, della membratura o dell'opera.
