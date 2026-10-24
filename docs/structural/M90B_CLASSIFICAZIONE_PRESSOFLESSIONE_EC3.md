# M90-B — Classificazione di sezioni I/H presso-inflesse (EC3, perimetro circoscritto)

Stato: **candidata M90-B; test .NET 8 su Windows da eseguire**.

## Scopo

Proseguire M90-A senza modificare i solver FEM. Si considerano sezioni I/H laminate a caldo, ad ali parallele e doppiamente simmetriche, sotto sforzo normale e flessione nel piano sull'asse forte. INP a ali inclinate, flessione debole, profili sottili formati a freddo, torsione, aperture e instabilità fuori piano sono esclusi.

**Attenzione: classificazione non equivale a verifica di resistenza. Non genera alcun esito `Passed` normativo o approvazione dell'asta.**

## Riferimenti espliciti

- EN 1993-1-1:2005+A1:2014, §5.5, prospetto 5.2, fogli 1–2 (non sostituisce le NTC 2018 né l'edizione EC3 2022).
- Claudio Bernuzzi, *Progetto e verifica delle strutture in acciaio*, 2ª ed., Hoepli, 2018, §4.3.1, pp. 131–138, e applicazione A4.3, pp. 136–138.
- Proprietà geometriche delle famiglie IPE/HEA/HEB: catalogo indicativo OPPO M88-B2; limiti elastici del materiale: M88-C.

Il libro PDF dell'utente **non è redistribuito** in questo archivio.

## 1. Classificazione con parametri di distribuzione dichiarati

Il metodo `ClassifyDeclaredStressParameters` prende due **informazioni distinte**, con giustificazione esterna obbligatoria:

- `alpha`: frazione compressa nel modello di tensioni plastiche dell'anima, `0 < alpha <= 1`;
- `psi`: rapporto fra tensione minore e maggiore nel modello elastico dell'anima, `psi <= 1`, con compressione positiva.

Per l'anima piatta `c = h - 2 tf - 2 r`, `epsilon=sqrt(235/fy)`, `lambda=c/tw`, si applicano le soglie:

| Classe | Condizione | Soglia massima `c/tw` |
|---|---|---|
| 1 | alpha > 0,5 | `396 ε/(13 alpha - 1)` |
| 1 | alpha <= 0,5 | `36 ε/alpha` |
| 2 | alpha > 0,5 | `456 ε/(13 alpha - 1)` |
| 2 | alpha <= 0,5 | `41,5 ε/alpha` |
| 3 | psi > -1 | `42 ε/(0,67+0,33 psi)` |
| 3 | psi <= -1 | `62 ε(1-psi)sqrt(-psi)` |

Per l'ala a sbalzo laminata, in compressione, rimangono i limiti `9 ε`, `10 ε`, `14 ε`. La classe della sezione è il massimo fra ala e anima; la classe 4 **richiede successivamente proprietà efficaci**. Se i due parametri dichiarati portano a soglie non ordinabili, l'API rifiuta la classificazione: non li riconcilia inventando un diverso schema di carico.

La stessa coppia N/M può essere associata a diverse modalità d'incremento dei carichi: il Bernuzzi, fig. 4.4, distingue esplicitamente percorsi diversi. Pertanto alpha e psi **non vengono dedotti automaticamente** da un generico N/M iniziale.

## 2. Punti di transizione del dominio N–M dell'anima (test indipendente)

`ComputeStrongAxisWebTransitions` riproduce le coordinate di transizione 1/2, 2/3 e 3/4 dell'anima del modello trattato nell'applicazione A4.3. Richiede `A`, `Wel`, **`Wpl` tabellato o documentato**, `fy`, spessori e raggi; non ricava `Wpl` da una sezione senza raccordi, e non interpreta i punti come resistenza completa dell'asta.

**Esempio A4.3: IPE 600, S275, h=600, b=220, tw=12, tf=19, r=24 mm, A=156 cm², Wel=3069 cm³, Wpl=3512 cm³**. Il confronto numerico accetta le differenze di arrotondamento dell'esempio del libro (es. epsilon pubblicato con tre cifre). Valori pubblicati indicativi:

| Transizione dell'anima | Compressione N (kN) | Momento (kNm) |
|---|---:|---:|
| 1 → 2 | 794,16 | 918,02 |
| 2 → 3 | 1131,91 | 868,74 |
| 3 → 4 | 3681,8 | 120,09 |

**Non è una curva continua di capacità**: mancano porzioni governate dalle ali, aree efficaci e percorso di incremento dei carichi.

## 3. Controllo elastico delle azioni FEM concomitanti

`ScreenConcomitantElasticFEM` utilizza `DesignCheckRequest` di M87-B, riferito a una singola sezione e a un unico caso/combinazione, compreso il lato della sezione in caso di salto di taglio. Sono obbligatori: codice e annesso identificati, membro coerente, `N`, `V`, `M` finiti, `N` e `M` non nulli; profilo IPE/HEA/HEB presente nel catalogo.

Convensione del FEM: **N positivo in trazione**, quindi la componente di compressione del modello elastico è `-N/A`. La componente flessionale nelle estremità dell'anima piatta è `|M| * (c/2) / I_forte`, usando tutte le conversioni SI esplicite. Dalle tensioni `sigma_high` e `sigma_low` si forma `psi=sigma_low/sigma_high`. Se non c'è una compressione nell'anima, il caso non è classificato: non è considerato «automaticamente sicuro».

Il controllo produce una **classe elastica conservativa indicativa: «classe 3 o migliore» oppure «4, da approfondire»**. Non deduce plasticità né un'esatta classe 1/2 e non sostituisce la successiva verifica delle resistenze. Con carichi diversi, combinazioni diverse o posizioni diverse, lo screening deve essere ripetuto.

## Test e distribuzione

- Test di riferimento A4.3 e soglie esatte del prospetto 5.2;
- test di rifiuto per dati incoerenti, classe plastica priva di evidenza, INP o edizioni non ammesse;
- test di provenienza FEM e non mescolanza di N/V/M, confronto fra i due lati di una discontinuità e cattura reale da Frame2D;
- build Release con warning come errori, test Verification/Catalogs e regressione FEM/Reporting/CLI.

Per eseguire:

```powershell
.\scripts\verify-m90b-manifest.ps1
.\scripts\run-m90b-tests.ps1
```

**Limiti M90-B aperti:** classificazione dipendente dal percorso di carico, pressoflessione debole, classificazione generalizzata con tensioni non uniformi nelle ali, proprietà efficaci classe 4, resistenza combinata N–M–V, instabilità membro, azioni secondo ordine generali, NTC e edizione EC3 2022. Queste non sono verificate dal pacchetto.
