# P0-D0M8 — Ramo α=0: zone elastiche compresse, senza classi normative inventate

**Data:** 25 ottobre 2026  
**Stato:** nuovo codice e xUnit predisposti; **232/232** nuovi golden e **705/705** regressioni aritmetiche indipendenti R2–D0M8 PASS; **gate .NET D0M8 PENDING**.  
**Checkpoint di partenza:** l'autrice ha confermato «tutti i test passati» per il FULL **D0M7** il 25/10/2026, senza log numerico allegato. D0M7 e tutti i gate precedenti sono quindi registrati come **PASS per conferma dell'utente**.

## Il problema che D0M8 risolve

Nei moduli precedenti, D0M6 trova geometricamente la posizione **nominale plastica** dell'asse neutro. Il limite α=1 è stato classificato in D0M7 con equazioni NTC applicabili e prove della stessa stazione FEM. Il caso opposto, **α=0**, è diverso: N FEM **di trazione** domina e la posizione plastica cade nel raccordo o nell'ala **dal lato compresso** nominale, lasciando la parte piatta dell'anima interamente nel lato plastico teso.

Le **NTC 2018, §4.2.3.1 Tabella 4.2.III**, danno per le classi 1 e 2 delle anime nella regione α≤0,5 rispettivamente le espressioni **36ε/α** e **41,5ε/α**. Per **α=0** sono **indefinite**, non numeri finiti: non è prudente dichiarare automaticamente anima «classe 1» e quindi sezione conforme. Non si sostituisce α con un numero piccolissimo né si assegna un infinito senza motivazione normativa. Per la classe 3 entra in gioco **ψ**, rapporto di **tensioni elastiche**, insieme alla nota normativa per `ψ<−1`.

**Fonte primaria:** [NTC 2018, D.M. 17 gennaio 2018, testo della Gazzetta Ufficiale](https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf), §4.2.3.1, Tabelle 4.2.III/IV, pp. 95–96 del fascicolo tecnico.

D0M8 realizza **uno schermo fisico distinto dalla classificazione NTC**. Il risultato non è un giudizio di idoneità, una classe 1–4 o un calcolo di resistenza.

## Condizioni obbligatorie

`Ntc2018SteelTensionDominantElasticZonesD0M8` considera esclusivamente:

1. una singola sezione nominale laminata simmetrica ad ali parallele **IPE, HEA o HEB**, senza fori, con geometria, grado e fyk documentati;
2. un risultato FEM **concomitante** alla stessa stazione e lato, con **N>0**, momento forte **M≠0**, **V=0**, norma/edizione NTC2018 identificata, analisi e combinazione dichiarate;
3. la prova di **D0M6** di asse neutro nominale plastico realmente **oltre il tratto piatto dell'anima sul lato compresso**, con `α=0`, controllo area geometrica e margine numerico intorno alla transizione con D0M4;
4. area lorda **A** e momento d'inerzia forte **I** ricavati da fonte esterna, verificabili e fisicamente plausibili (OPPO nominale **non certificativo**).

Quando una precondizione manca o si raggiunge un confine numericamente ambiguo, il servizio restituisce `Unsupported` tramite `NotSupportedException`.

## Tre zone elastiche possibili

Usando tensione **positiva in compressione** e N positivo in trazione, nel modello nominale elastico di primo ordine:

\[
\sigma_0=-\frac{1000N}{A},\qquad
\sigma_{M,\max}=\frac{10^6|M|}{I}\frac h2,\qquad
\sigma_{M,\mathrm{web}}=\frac{10^6|M|}{I}\frac c2,
\quad c=h-2(t_f+r).
\]

Le unità sono N in **kN**, M in **kNm**, A in **mm²**, I in **mm⁴**, geometria in **mm**, tensioni in **MPa**.

Si confrontano gli estremi massimi:

\[
\sigma_{\mathrm{outer}}=\sigma_0+\sigma_{M,\max},\qquad
\sigma_{\mathrm{web}}=\sigma_0+\sigma_{M,\mathrm{web}}.
\]

| Stato restituito | Condizione di tensione elastica | Interpretazione consentita |
|---|---|---|
| `NoCompressedElasticFibres` | σouter < 0 | Tutte le fibre **nominali elastiche** sono tese nella sezione di primo ordine esaminata |
| `CompressionConfinedToFlangeOrRoot` | σouter > 0 e σweb < 0 | Compaiono fibre elastiche compresse nell'ala o nel raccordo; **non** attribuisce classe all'ala |
| `CompressionReachesFlatWeb` | σweb > 0 | Una parte dell'anima piatta è elasticamente compressa: viene attivato **D0M3** per lo schermo di sola classe 3, con nota NTC sul ramo ψ<−1 |

Il controllo rifiuta una fascia di quasi-uguaglianza attorno a σouter=0 e σweb=0 anziché attribuire la regione in presenza di arrotondamenti.

Una sezione può avere **α plastico=0 ma nessuna fibra elastica compressa sotto il M effettivamente applicato**: non è una contraddizione. L'α nominale plastico deriva dalla costruzione a tensioni ±fyk per la condizione plastica limite, mentre le zone elastiche dipendono dall'**intensità** del momento FEM reale. Queste due costruzioni non sono intercambiabili.

### Esempio: IPE 200, S235, N=+220 kN, V=0

Con A=2848 mm² e I=19.430.000 mm⁴ pubblicati nel catalogo nominale, i momenti indicativi di inizio compressione sono:

- **≈ 15,0 kNm**: prima fibra estrema dell'ala raggiunge tensione elastica nulla.
- **≈ 18,9 kNm**: il bordo dell'anima piatta raggiunge tensione elastica nulla.

| M forte FEM | Localizzazione nominale elastica | Schermo D0M3 | Classe definitiva |
|---:|---|---|---|
| +10 kNm | Tutto teso | Non applicabile | **Non stabilita** |
| +17 kNm | Solo ala/raccordo compresso | Non applicabile | **Non stabilita** |
| +20 kNm | Compressione fino all'anima piatta | Necessario, ramo ψ<−1 | **Non stabilita** |
| −20 kNm | Compressione fino all'anima piatta sul lato opposto | Necessario, ramo ψ<−1 | **Non stabilita** |

Per tensioni elastiche dell'anima compresse oltre fyk, D0M3 conserva la condizione della nota: occorre provare **σc≤fyk** oppure **εt>fyk/E** con evidenza esterna. Il risultato del terzo ramo resta uno **schermo di classe 3 della sola anima** (classe fino a 3 oppure 4), mai una classificazione intera.

## Prove e limiti

- **232/232 golden indipendenti PASS** su frazioni esatte del ramo α≤0,5, tutte le **30** sezioni IPE/HEA/HEB nominali in tre condizioni elastiche e rapporti ψ del ramo teso, soglie critiche, cambio di verso M, nota ψ<−1 ed esclusioni fuori dominio. I contatori sono fissati dallo script eseguibile.
- **705/705 PASS** dopo riesecuzione completa di tutti gli 11 script indipendenti R2, D1, D0, D0M1–D0M8 il 25/10/2026. Si tratta di verifiche aritmetiche in Python, **non** di compilazione/esecuzione dei nuovi test C#.
- Script: `benchmarks/independent/p0d0m8/verify_p0d0m8_alpha_zero_elastic_regions.py`.
- Nuovi xUnit: `tests/StructuralSolver2D.Verification.Tests/D0/Ntc2018SteelTensionDominantElasticZonesD0M8Tests.cs` includono condizioni con PNA in raccordo e ala, tre zone elastiche, ψ<−1/nota, confini, rifiuti di FEM/NTC/geometria e **flag negativi di certificazione**.
- **Nessuna modifica** alla classificazione del ramo α=1 D0M7, α in (0,1) D0M5, asse neutro D0M6, schermo D0M3 o motore FEM. Un risultato positivo qui non può essere etichettato `Passed` di verifica strutturale.

Le sollecitazioni elastiche sono **nominali di primo ordine**; residui di laminazione, imperfezioni geometriche, instabilità, plasticizzazione, carichi fuori piano e redistribuzioni non sono modellati. Le proprietà OPPO vanno confrontate con i dati del prodotto reale se usate professionalmente. Nemmeno una sezione nominalmente tutta tesa esclude **rottura netta**, collegamenti, fatica, copertura incompleta delle combinazioni e altri stati limite.

## Gate Windows da eseguire

```powershell
dotnet test tests/StructuralSolver2D.Verification.Tests/StructuralSolver2D.Verification.Tests.csproj --filter FullyQualifiedName~Ntc2018SteelTensionDominantElasticZonesD0M8Tests
dotnet test StructuralSolver2D.sln
```

L'ambiente che confeziona il checkpoint non dispone dell'SDK .NET. La conferma dell'autrice «tutti i test passati» riguarda **fino a D0M7**, non i nuovi xUnit D0M8.

## Prossime milestone

1. **Confermare gate D0M8 Windows** e correggere eventuali errori misurati.
2. **P0-D2A**, primo sottoinsieme di resistenza nativa NTC acciaio selezionato dopo confronto formula-per-formula, con ingressi FEM, proprietà/categorie di sezione e `Unsupported` rigoroso. **D0M9** per l'eventuale classificazione α=0 resta subordinato a fondamento normativo/prove, non è un'estensione automatica.
3. **P0-E** vincoli fisici, ritegni e instabilità, **P0-C3/C4** completezza obblighi/report, **P0-D3** SLE acciaio.
4. **Interfaccia Avalonia C# soltanto P3**, dopo motore e verifiche validate.
