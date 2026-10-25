# F1.1-A — Validazione FEM G8B con golden congelato e test C#

**25 ottobre 2026 — P0-F1A.** Primo incremento tecnico successivo al riordino P0-F0. Nessuna modifica al solutore FEM, ai modelli G8B o ai profili normativi. **Aggiornamento successivo P0-F1B: l'autrice ha comunicato «test passati» per F1A**; PASS confermato testualmente senza allegare log o numero preciso di test. Nell'ambiente di preparazione non è disponibile l'SDK .NET 8.

## Obiettivo e materiale indipendente

L'input è costituito dai **quattro modelli JSON G8B** in `examples/mixed/g8b-house/`, ciascuno con le quattro combinazioni **manuali e illustrative** `ULS_GRAVITY`, `ULS_WIND_POS`, `ULS_WIND_NEG`, `SLS_RARE`: **16 scenari**. Non costituiscono un insieme NTC esaustivo.

Riferimento: `benchmarks/independent/g8b-house/g8b-risultati-elastici-indipendenti.json`, prodotto in una fase precedente con un algoritmo numerico separato dal C# di produzione. I numeri restano **dati statici congelati**, non riscritti né rigenerati; il relativo file Python è **storico**, non viene eseguito né richiesto da build/test. SHA256 del JSON:

`db41c1afbe08614ac62d1dcb7ef7ba2b0aa8fdc5af5fba7c3e94d9099021de47`

Il test controlla anche questo hash: una variazione accidentale dei dati attesi rende il gate rosso, senza adattare l'oracolo ai risultati del solver.

## Test aggiunti

`tests/StructuralSolver2D.Analysis.Tests/Benchmarks/G8BIndependentReferenceF1ATests.cs`:

| Controllo per scenario | Fondamento | Unità |
|---|---|---|
| `Ux` di A2/B2/C2/D2/colmo; `Uy` A1/B1/C1/D1 | Valori congelati del riferimento indipendente | mm |
| Massimi assoluti nodali `Ux` e `Uy` | Valori congelati su tutti i nodi | mm |
| `Rx`, `Ry`, `Mz` dei quattro appoggi | Valori congelati per supporto | kN, kNm |
| Somma `Rx`, `Ry`, momento rispetto all'origine | Resultanti globali indipendenti `Fx`, `Fy`, `Mz`; equilibrio del corpo libero | kN, kNm |
| `N(x)` di COL_A/B/C/D_1 a base, metà e cima | Deformazione verticale congelata a quota +3 m; formula di Hooke e peso proprio distribuito noto | kN |
| `M(0)` delle quattro colonne inferiori | Reazione momentanea indipendente al vincolo: i controventi Truss2D non portano momenti | kNm |
| `V(x)` e `M(x)` di COL_C/D_1 a base, metà e cima | Reazioni indipendenti; solo una colonna a ciascuno dei due appoggi, nessun carico distribuito trasversale | kN, kNm |

Per una colonna inferiore **verticale**, vincolata alla base a `Uy=0`, con lunghezza `L`, area `A`, modulo `E`, `δ=Uy` al piano +3 m congelato e `w=γ A × fattore(G_SELF)` in kN/m:

`N(x) = E A δ / L + w (x - L/2)`, con `0 ≤ x ≤ L` e trazione positiva.

Per le colonne **C e D**, i soli elementi collegati alla base C0 e D0, con sezione lungo l'asta priva di carichi trasversali:

`V(x) = -Rx_base` e `M(x) = -Mz_base + V x`, con `x` misurata verso l'alto dalla base, secondo le convenzioni locali del solver. Il momento iniziale di tutte le colonne inferiori è `M(0)=-Mz_base`, perché eventuali diagonali Truss2D non trasferiscono momento.

Queste sono **equazioni elementari separate dal FEM di produzione**, applicate a quantità indipendenti già congelate; non chiamano la matrice di rigidezza o le funzioni di carico del motore sotto test. Si applicano soltanto quando le premesse geometriche e di carico restano quelle dei quattro modelli G8B.

## Tolleranze e controprove

Per ogni misura: `|actual−expected| ≤ absTol + 10⁻⁴ |expected|`.

- Spostamenti: `absTol = 0,0005 mm`.
- Forze: `absTol = 0,005 kN`.
- Momenti: `absTol = 0,005 kNm`.

Le tolleranze sono fissate **prima del primo test .NET del checkpoint** e devono essere confermate o motivatamente corrette in base alla precisione effettiva dei riferimenti, mai ampliate soltanto per nascondere mismatch.

Tre tipi di controprove automatiche alterano volontariamente il modello mantenendo invariato il golden: **+10 kN** su un carico `WIND_POS` per ciascuna variante B con/senza X; **rimozione del termine `WIND_POS`** dalla combinazione ULS per la variante A; **release flessionale introdotta alla base di COL_C_1** nella variante B senza X. Il confronto deve rilevare lo scarto.

## Come provare su Windows

Estrarre il FULL in una **directory pulita**. Verificare il manifest e lanciare il test di fase:

```powershell
.\scripts\verify-g7-manifest.ps1
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln -c Release
dotnet test tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --filter "FullyQualifiedName~G8BIndependentReferenceF1ATests" --logger "console;verbosity=normal"
dotnet test StructuralSolver2D.sln -c Release
```

Il test usa solo **xUnit/.NET**: i 4 modelli e il JSON golden sono copiati nella directory di output del progetto `Analysis.Tests` come contenuto versionato. **Nessuno script Python da eseguire**.

## Cosa manca prima di dichiarare F1.1 completamente conclusa

1. I test F1A su Windows sono stati **confermati PASS dall'autrice**, senza log allegato; rimane da confermare il nuovo checkpoint F1B.
2. Copertura indipendente più ampia di **N/V/M per travi, copertura, balconi e diagonali**, compresi punti e salti significativi, senza derivare i golden dal solutore sotto test. I test F1B ora aggiungono valori analitici assoluti per **i balconi** e verifiche di equilibrio per gli altri membri; **mancano** ancora golden N/V/M assoluti indipendenti per le altre aste (vedere [rapporto F1B](F1_1B_G8B_SOLLECITAZIONI_EQUILIBRIO_CSHARP.md)).
3. Controlli G8B di viewer/report come rappresentazioni distinte dalla validazione FEM; quindi gli ulteriori casi F1.2 (aste ruotate, carichi/release) e F1.3 (robustezza numerica).
4. Presupposti fisici G8B restano non dimostrati: diagonali bilatere ideali, ritegni/connessioni, appoggi e risposta 3D. Nessuna verifica completa NTC/EC3/EC5 o certificazione della struttura.

La prossima evoluzione è **F1.1-B**, estensione delle verifiche sui membri non coperti e conferma su PC, mantenendo gli stessi dati congelati e la policy C#.
