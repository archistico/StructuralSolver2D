# M80-FIX2 — Diagnosi verificabile dei due fallimenti Analysis.Tests

**Data: 24 ottobre 2026. Stato: correzioni implementate, compilazione e test Windows ancora da eseguire.**

## Esito riportato dall'utente

La regressione separata M80-FIX1 ha eseguito `StructuralSolver2D.Analysis.Tests`: **541 test totali, 539 superati, 2 falliti**. Questo risultato identifica in modo affidabile i due casi sotto. Non implica un fallimento generalizzato del FEM. I 231 test CLI riportati in precedenza erano riusciti, ma la regressione completa resta **non approvata** finché tutti e quattro i progetti non passano.

## 1. M78 — Pareggio fisico del massimo taglio

Prova: trave semplicemente appoggiata, `L=8 m`, carico verticale `P=-12 kN` in `x=2 m`. Le reazioni sono `R_A=+9 kN`, `R_B=+3 kN`. Il taglio vale `+9 kN` per ogni sezione su `[0,2)` e `-3 kN` per le sezioni a destra del carico. Quindi esistono infinite sezioni con massimo `+9 kN`: **non è fisicamente obbligatorio** che il rappresentante selezionato sia `BeforePointLoad`.

`Frame2DExactExtremaFinderM78` inserisce candidati in ordine deterministico: inizio asta, fine asta, due lati dei salti, zeri/estremi nei tratti. `MaxBy` mantiene il primo candidato in caso di parità. Pertanto `Maximum(ShearForce)` restituisce **`x=0, Side=Continuous`**, mentre `Minimum(ShearForce)` restituisce **`x=8, Side=Continuous`** (la fine asta precede i limiti del salto nell’ordine dei candidati). Le aspettative originarie sui lati di massimo e minimo erano errate a causa dei valori costanti su due tratti, non per un errore dell'algoritmo. **Errata corrige M80-FIX3:** il test FIX2 aveva corretto soltanto il massimo; per il minimo attendeva ancora `x=2/AfterPointLoad`, ma la prova Windows ha confermato `x=8/Continuous`.

**Correzione:** il test descrive esplicitamente questa parità e controlla le ascisse. Aggiunto test indipendente con `L=8 m`, carico uniforme verticale `+1 kN/m` e puntuale `-12 kN` in `x=2 m`. Per equilibrio `R_A=+5 kN`, `R_B=-1 kN`; il taglio cresce da `+5` a `+7 kN` prima della forza, scende a `-5 kN` subito dopo, e arriva a `+1 kN` prima dell'appoggio B. **I valori estremi sono univoci:** `Vmax=+7 kN` a `x=2 m, BeforePointLoad`, `Vmin=-5 kN` a `x=2 m, AfterPointLoad`. Questo verifica effettivamente le due etichette.

## 2. M79 — Fattore di combinazione zero vietato

La fixture `REV` conteneva `G × (-1)` e `Q × 0`. Il validatore comune `StructuralModelValidator` emette `LOAD_COMBINATION_INVALID_FACTOR` per coefficienti `0` o non finiti, come richiesto dai test precedenti. La combinazione desiderata è perciò **solo `G × (-1)`**: il contributo di Q è assente, non rappresentato da una voce a zero. I risultati previsti non cambiano: per carico G di `10 kN/m` su `L=5 m`, `Mmin=-31,25 kNm`; la combinazione STRONG mantiene `Mmax=+65,625 kNm`.

Non si cambia il validatore, non si autorizzano valori non ammessi e non si modifica il calcolo degli inviluppi.

## Esecuzione raccomandata

```powershell
.\scripts\run-m80-tests.ps1
```

Il runner continua a pulire `bin/obj` come **prima azione**, compila Release, esegue benchmark M80 e regressioni catalogo, poi lancia un filtro **M78/M79** prima dei test completi separati `Core`, `Analysis`, `Reporting`, `Cli`. I test completi usano il logger `console;verbosity=minimal` per evitare centinaia di righe `Superato`, ma i fallimenti e il riepilogo restano visibili e sono salvati nel log testuale.

**Criterio di accettazione:** build OK, gate mirati OK, quattro progetti test OK e codice di uscita `0` del runner. Fino al tuo risultato Windows, M75–M80 restano **test pendenti**. Il prossimo lavoro di calcolo resta M81, secondo ordine geometrico nel piano, senza anticipare normative o desktop Avalonia.
