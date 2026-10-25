# F1.2-A — Orientamenti, segni dei carichi e release: prove analitiche C#

**25 ottobre 2026 — P0-F12A: 12 nuovi test C#/.NET, PENDING.** L'autrice ha comunicato **F1E PASS** su Windows e un solo warning `xUnit2031`. Il warning è stato corretto con `Assert.Single(enumerable, predicate)`, senza modificare cosa viene verificato. La nuova suite F1.2-A **non è ancora stata compilata o eseguita** in questo ambiente, privo di SDK .NET.

## Perimetro e motivazione

F1A–F1E hanno validato in profondità quattro varianti di una casa mista acciaio-legno G8B. In F1.2 iniziamo a verificare **con formule statiche autonome** che la rotazione delle aste non modifichi la meccanica, che `LocalY` conservi il verso fisico corretto e che le release non trasferiscano momenti indesiderati. Non usiamo matrici o recuperatori di forze del FEM per generare gli attesi.

Tutti i casi sono **2D Frame2D Euler–Bernoulli, statici, elastici, a piccoli spostamenti**, con E, A e I costanti; nessun peso proprio in questi esempi. Non sono controlli normativi.

## Matrice delle 12 prove

| Prove xUnit | Numero | Grandezze confrontate |
|---|---:|---|
| Mensola inclinata con forze di punta assiali e trasversali `(+/-)` | 6 | `Ux`, `Uy`, `Rz`, tre reazioni e `N/V/M` firmati a cinque sezioni. Angoli: 0°, 30°, 90°, 145°, −65°, −20° |
| Trave inclinata a due cerniere, una su carrello verticale, con `LocalY` negativo | 4 | Reazioni, `Mz` nullo alle due release, `N/V/M` in cinque sezioni. Angoli: 0°, 30°, −35°, 60° |
| Combinazione manuale con coefficiente negativo | 1 | Azione assiale `1,2 H` e trasversale `−0,75 P`, confrontate con soluzioni elastiche indipendenti |
| Controprova d'inversione del carico trasversale | 1 | Il segno del momento alla base deve cambiare e discostarsi dall'atteso fisso |

### Riferimento delle mensole

Si consideri un'asta di lunghezza `L`, inclinazione `θ`, versore assiale `e=(cosθ,sinθ)` e normale locale `n=(−sinθ,cosθ)`. Alla punta agiscono `H` lungo `e` e `P` lungo `n`. Per un incastro all'origine:

- Dilatazione assiale `u=H L/(E A)`, deflessione trasversale `v=P L³/(3 E I)` e rotazione `rz=P L²/(2 E I)`.
- Spostamenti globali `Ux=u cosθ−v sinθ` e `Uy=u sinθ+v cosθ`.
- Reazioni `Rx=−H cosθ+P sinθ`, `Ry=−H sinθ−P cosθ`, `Mz=−P L`.
- Sollecitazioni, con `x` misurato dalla base: `N(x)=H`, `V(x)=−P`, `M(x)=P(L−x)`, secondo le convenzioni operative del risolutore.

Questi valori derivano dalle formule chiuse della trave elastica e dall'equilibrio; non sono copiati dalle soluzioni numeriche della classe produttiva.

### Riferimento delle travi rilasciate

Trave con cerniera in `A`, carrello con sola reazione verticale in `B`, release dei momenti locali a entrambe le estremità. Carico uniforme `−q` in `LocalY` con `q>0` e `cosθ>0`:

- Risultante globale `(+q L sinθ, −q L cosθ)`.
- `RAx=−q L sinθ`, `RBy=q L/(2 cosθ)`, `RAy=q L cosθ−RBy`.
- `M_A=M_B=0`; `N(x)=RBy sinθ`; `V(x)=q(L/2−x)`; `M(x)=q x(L−x)/2`.

Il carrello resta **verticale globale** anche se l'asta è inclinata: la sua reazione non equivale a un appoggio ruotato con la membratura. Le prove tengono conto di questa distinzione fisica.

## Comandi per il gate Windows

Usare il nuovo FULL in una cartella pulita:

```powershell
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln -c Release
dotnet test tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --filter FullyQualifiedName~F12AInclinedAnalyticalReferenceTests
dotnet test StructuralSolver2D.sln -c Release
```

**Stato corretto:** F1A, F1B, F1C, F1D-FIX1, F1E **PASS riferiti dall'autrice**; F1.2-A **test realizzati, .NET PENDING**. Per il golden F1D resta da ricevere e approvare il file `g8b-member-nvm-f1d.proposed.json` con SHA256. Perciò anche il **gate complessivo F1.1 rimane aperto**, sebbene F1.2-A possa procedere parallelamente come gruppo di validazione indipendente.

## Cosa resta non validato

- Release singole in telai con vincoli misti, carichi trapezoidali/locali complessi, supporti orientati ed eccentricità.
- Meccanismi e diagnostica in nuove combinazioni complesse oltre ai casi limite già verificati nello storico.
- Convergenza sotto spostamenti grandi, instabilità 3D, comportamento non lineare generale, effetti fuori piano.
- Completezza delle combinazioni SLU/SLE e verifiche normative di membratura/opera: **F2–F7 sono ancora aperte**.

Prossimo dopo gate F1.2-A: completare le altre famiglie di F1.2 e la robustezza F1.3; ricevere e congelare il golden G8B F1D. Nessun codice Python nuovo; NTC 2018, EC3 ed EC5 separati; GUI C#/Avalonia soltanto dopo F9.
