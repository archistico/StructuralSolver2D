# F1.1-B — Sollecitazioni della casetta G8B: sbalzi, travi, copertura e diagonali

**25 ottobre 2026 — P0-F1B.** Estensione dei test FEM in **C#/.NET 8**, senza modifiche ai solutori, ai modelli G8B, ai parametri normativi o al golden numerico storico. **Aggiornamento successivo P0-F1C: l'autrice ha comunicato «test passati» per F1B**; PASS confermato testualmente senza log allegato. Il nuovo checkpoint F1C rimane PENDING, perché il sistema di confezionamento non dispone di `dotnet`.

## Perimetro esatto

La nuova classe `tests/StructuralSolver2D.Analysis.Tests/Benchmarks/G8BMemberForceF1BTests.cs` esegue **quattro famiglie di test × 16 scenari**, più una controprova negativa. Gli scenari sono i quattro JSON di `examples/mixed/g8b-house/` incrociati con `ULS_GRAVITY`, `ULS_WIND_POS`, `ULS_WIND_NEG`, `SLS_RARE`. Queste **combinazioni manuali illustrative non sono combinazioni normative complete**.

| Famiglia | Aste/nodi e risultati controllati | Natura del controllo |
|---|---|---|
| Sbalzi | `BAL_L`, `BAL_R`; `N`, `V`, `M` agli estremi e a 0, 1/4, 1/2, 3/4 e 1 della lunghezza | **Valori analitici assoluti indipendenti** da carichi G8B congelati e formule di equilibrio dello sbalzo |
| Equilibrio delle aste `Frame2D` | Colonne, travi dei due piani, entrambi i balconi, puntoni inclinati `RAFTER_L/R`; tre risultanti agli estremi e sezioni interne | **Identità di equilibrio e integrazione** del carico distribuito; non sono golden FEM autonomi per le reazioni locali incognite |
| Equilibrio dei nodi | Tutti i nodi privi di vincoli: proiezione delle forze locali delle aste in coordinate globali, equilibrio di `Fx`, `Fy`, `Mz` | **Equilibrio di corpo libero**, inclusi vento nodale e peso proprio delle diagonali applicato ai nodi |
| Diagonali `Truss2D` | Tutti i controventi presenti: `N` dal rapporto `EA ΔL/L`, `V=M=0`, `N` costante a tre sezioni | **Legge di Hooke e identità del tipo di elemento**; usa spostamenti ottenuti dal solver, quindi non è un golden numerico indipendente |
| Controprova | Alterazione intenzionale di `G_BAL_L` nella variante B | Il valore analitico predefinito del balcone deve **rifiutare** il modello alterato |

La nuova famiglia **non sostituisce F1A**: gli spostamenti/reazioni dei 16 scenari continuano a confrontarsi con il JSON indipendente e con il suo SHA256 congelato.

## Formule analitiche dello sbalzo

I balconi sono mensole orizzontali da `L=1,2 m`, con estremità esterna libera e nessuna forza concentrata di punta. Il carico distribuito verticale verso il basso ha intensità positiva in valore assoluto:

`p = 24 γ(G_BALC) + 16 γ(Q_BALC) + γ(G_SELF) × γ_materiale × A_sezione`  **[kN/m]**.

Le due intensità **24 e 16 kN/m sono valori fissi di riferimento G8B**, non letti da `StructuralLoad.Value` nella costruzione degli attesi; così una modifica indesiderata di un carico del balcone è rilevabile. Il peso proprio usa il peso specifico del materiale e l'area nominale del modello specifico. I fattori congelati per `G_BALC`, `Q_BALC`, `G_SELF` sono rispettivamente: `(1,35;1,50;1,35)` per ULS_GRAVITY, `(1,00;0,70;1,00)` per entrambi i casi vento e `(1,00;1,00;1,00)` per SLS_RARE.

- Reazione tagliante in valore assoluto all'incastro della mensola: `p L`.
- Momento di incastro in valore assoluto: `p L² / 2`.
- A distanza `d` dall'estremità **libera**: `N=0`; `|V|=p d`; `M=−p d² / 2` con la convenzione di sezionamento adottata; segno di `V` dipendente dall'orientamento dell'asta.
- Sull'asta sinistra `BAL_L` la coordinata va dalla punta verso A1; sulla destra `BAL_R` va da D1 verso la punta. Per questo le forze e i momenti **di estremità** del modello FEM non hanno lo stesso segno a sinistra e a destra.

Sono formule di equilibrio senza uso della matrice di rigidezza. Valgono per il **modello piano idealizzato**, non dimostrano capacità dei collegamenti o stabilità laterale.

## Equilibrio degli altri membri

La convenzione FEM usa l'asse locale `x` dall'inizio alla fine e il versore locale `y` ortogonale antiorario. Con `c=ΔX/L`, `s=ΔY/L` e carico `qY` globale uniforme: `qx=s qY`, `qy=c qY`. Sono inclusi peso proprio esplicito del materiale e carichi distribuiti manuali pesati con i coefficienti della combinazione.

Per ogni `Frame2D`, indicando con `Fi` e `Fj` le azioni agli estremi e con `x` la sezione interna:

1. `N(x) = −Fi,N − qx x`
2. `V(x) = Fi,V + qy x`
3. `M(x) = −Fi,M + Fi,V x + (qy x²)/2`

Il test usa un calcolo diretto in C# dei carichi a partire dal **JSON non espanso**, differente dal servizio che espande il peso proprio durante il calcolo. Controlla anche le tre equazioni di equilibrio di ogni singola asta e la corrispondenza con il post-processore di sezioni. Per i puntoni inclinati questo esercita sia la componente longitudinale `qx` sia quella trasversale `qy`. **Le forze ignote di estremità provengono tuttavia dal FEM da verificare:** è un controllo indipendente delle identità fisiche, **non** una fonte indipendente di valori assoluti N/V/M per tutte le travi e la copertura.

L'equilibrio nodale usa le azioni alle estremità trasformate con `Fx=c N−s V` e `Fy=s N+c V` e confrontate con le azioni nodali applicate. Il peso proprio dei `Truss2D` è contato come carico nodale `−γ A L/2` per ciascun estremo, **non** come flessione della diagonale. Le diagonali sono aste assiali **bilatere ideali**, non controventi tesi soltanto né collegamenti reali verificati.

Tolleranza dei confronti: **0,005 kN/kNm + 10⁻⁴ × |valore atteso|**; per la lunghezza della mensola tolleranza geometrica `10⁻⁷ m`.

## Verifica su Windows

Estrarre **uno ZIP FULL in una cartella pulita** e lanciare:

```powershell
.\scripts\verify-g7-manifest.ps1
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln -c Release
dotnet test tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --filter "FullyQualifiedName~G8BMemberForceF1BTests" --logger "console;verbosity=normal"
dotnet test StructuralSolver2D.sln -c Release
```

**Atteso da confermare:** 64 casi parametrizzati F1B e una controprova; non attribuire loro un PASS prima dell'esecuzione. Nessun nuovo codice di produzione e nessun nuovo runner o script Python/PowerShell.

## Gate F1.1 non ancora chiuso

1. **F1B e regressioni: PASS confermato dall'autrice**, senza log allegato.
2. Il successivo **F1.1-C** introduce un secondo risolutore indipendente **solo in C#**, con input ad hash bloccati e confronto preliminare con il golden numerico già congelato, per derivare N/V/M di travi, copertura e diagonali senza ricopiare risultati dal solver produttivo. Leggere [rapporto F1C](F1_1C_G8B_RIFERIMENTO_VARIAZIONALE_CSHARP.md); i suoi test sono PENDING.
3. Verificare il **viewer/report** come rappresentazione fedele e non come prova delle equazioni FEM; poi **F1.2** orientamenti/carichi/release e **F1.3** sensibilità/robustezza numerica.
4. Restano da definire e dimostrare vincoli reali dei collegamenti, ritegni, ancoraggi, limiti 2D e validità dei controventi ideali; **nessuna certificazione NTC 2018 / EC3 / EC5** viene ottenuta dal solo gate FEM.

Sequenza invariata dopo F1: **F2 fisica → F3 combinazioni → F4 massimi di utilizzo → F5 copertura → F6/F7 norme distinte → F8 robustezza/prestazioni → F9 API/release → F10 GUI C#/Avalonia**.
