# Verifica dell'animazione della deformata — 2026-10-18

## Aggiornamento: ciclo coerente con il report statico

La prima verifica riportata sotto assumeva intenzionalmente un ciclo con fattore positivo e negativo. Il riscontro sulla trave con carico uniforme ha evidenziato che questa scelta suggeriva un'inversione del carico. Il comportamento è stato corretto: **indeformata → deformata statica → indeformata**, con fattore `(1 - cos(2π × fase)) / 2` compreso fra zero e uno. Anche i frame preparati dal builder e il Fit usano questo intervallo; Pause continua a ripristinare la deformata statica.

Aggiunta la trave appoggiata con carico uniforme come quinto modello browser. Un test controlla 65 istanti di un ciclo intero: nessun punto sale oltre l'asse indeformato, l'ampiezza non supera quella statica, i vincoli restano fermi, il massimo è raggiunto a metà ciclo e la deformata torna a zero alla fine. Aggiunti quattro casi .NET con 2, 4, 17 e 24 frame. Il test browser falliva prima del cambio di ciclo a tutte e tre le dimensioni della finestra.

Verifica aggiornata: **774 test .NET e 333 verifiche browser superati, nessun fallimento**. Log: [suite completa](../out/DeformationAnimationAudit/loading-cycle-all-tests.log), [browser](../out/DeformationAnimationAudit/loading-cycle-browser.log). Rigenerati i due viewer di esempio. Le sezioni successive conservano la verifica precedente; i riferimenti alla fase negativa descrivono il comportamento sostituito da questo aggiornamento.

## Prima verifica

L'animazione dei viewer HTML è stata confrontata con gli spostamenti dell'analisi e, per due mensole, con una soluzione analitica indipendente. Individuati e corretti quattro problemi. I viewer di esempio della trave e del ponte sono stati rigenerati con il nuovo script incorporato.

## Criterio di correttezza

Per ogni campione, la posizione visualizzata deve essere:

```text
posizione(t) = origine + scala_base × percentuale/100 × sin(fase(t)) × spostamento
```

L'origine è la posizione indeformata del campione, non il centro del disegno. La stessa fase moltiplica Ux e Uy; la conversione SVG inverte solo l'asse Y. La fase zero restituisce la geometria indeformata, quella positiva segue i risultati statici e quella negativa ne inverte il vettore. I valori fisici nelle etichette restano invariati. L'ampiezza grafica non cambia i risultati dell'analisi.

L'animazione rappresenta ciclicamente una soluzione statica: non calcola frequenze proprie, inerzia o smorzamento. La deformata usa i campioni/interpolazioni forniti dall'analisi; le forme interne di elementi caricati distribuitamente mantengono l'approssimazione documentata del campionatore FEM.

## Problemi riprodotti e corretti

| Problema | Correzione | Prova |
|---|---|---|
| L'export ricostruiva le origini assumendo stazioni equidistanti. Con campioni non uniformi, i punti venivano traslati erroneamente durante la variazione di ampiezza/fase. | `DeformedMemberShape.BasePoints` conserva le posizioni reali dei campioni. Il builder e i frame preparati le mantengono; SVG incorpora vettori riferiti a quelle origini. | Tre test .NET su aste orizzontali, verticali e inclinate; mensola inclinata con campioni non uniformi nel browser. I test .NET fallivano sulle origini prima del fix. |
| Play/Fit adattavano la vista a una sola fase. Picco negativo e testo del massimo potevano uscire dal canvas. | Inquadratura dell'intero inviluppo positivo/negativo e del testo del massimo ai due estremi. Viene aggiornata al cambio di ampiezza, senza muovere la camera a ogni frame. | Picchi e bounding box delle etichette verificati al 100% e 500%, dopo cambio di ampiezza; screenshot delle fasi. |
| Cambiare velocità ricalcolava la fase su tutto il tempo trascorso, provocando un salto nella geometria. | Accumulo incrementale della fase fra i frame. | La geometria resta identica al medesimo timestamp dopo il cambio di velocità; il frame successivo segue la nuova velocità. |
| Cambiare modalità o nascondere la deformata lasciava attivo il ciclo e Play disabilitato. | Arresto condiviso con Pause quando si nasconde la deformata; cancellazione del frame pendente e ripristino della geometria statica. | Coda dei frame vuota, Play nuovamente disponibile e geometria statica verificata dopo cambio modalità e disattivazione del livello. |

Per forme costruite direttamente con il precedente costruttore a due argomenti, `BasePoints` vuoto mantiene la convenzione delle stazioni uniformi. Per campioni personalizzati non uniformi occorre fornire le origini. Una lista di origini non vuota deve avere lo stesso numero di punti della deformata.

## Verifiche

```powershell
dotnet test StructuralSolver2D.sln --no-restore --verbosity minimal
node scripts/run-reporting-browser-tests.mjs
```

| Suite | Superati | Falliti |
|---|---:|---:|
| Core | 192 | 0 |
| Analysis | 245 | 0 |
| Reporting | 156 | 0 |
| CLI | 177 | 0 |
| Totale .NET | 770 | 0 |
| Browser | 273 | 0 |

I test browser eseguono quattro modelli a tre dimensioni di finestra: mensola orizzontale, casa, ponte e mensola inclinata con stazioni non uniformi. Confrontano ogni punto nelle fasi zero, positiva e negativa, ampiezze 0/100/200%, ripristino dopo Pause, etichette, inviluppo, velocità e ciclo di vita dell'animazione. Per le mensole i valori attesi derivano dalla formula della mensola con forza in punta, includendo le componenti assiale e flessionale nel caso inclinato. Casa e ponte sono confrontati con i campioni fisici dell'analisi, senza ricavare l'atteso dai metadati SVG dell'animazione.

I callback `requestAnimationFrame` sono controllati dai test per scegliere timestamp ripetibili; trasformazioni SVG, layout e script applicativo vengono eseguiti nel browser reale. I test verificano la geometria e il comportamento, non misurano il frame rate. Tre ulteriori casi .NET verificano anche i frame preparati dal builder.

Artefatti locali sotto `out/DeformationAnimationAudit/`, ignorati da Git:

- [Log suite .NET](../out/DeformationAnimationAudit/all-tests.log).
- [Log browser finale](../out/DeformationAnimationAudit/fixed-browser.log).
- [Mensola: picco positivo](../out/DeformationAnimationAudit/viewer-tests-positive.png) e [negativo](../out/DeformationAnimationAudit/viewer-tests-negative.png).
- [Mensola inclinata: picco positivo](../out/DeformationAnimationAudit/inclined-nonuniform-positive.png) e [negativo](../out/DeformationAnimationAudit/inclined-nonuniform-negative.png).

Viewer di esempio aggiornati:

- [Trave appoggiata](viewer/simple-supported-beam.html).
- [Ponte, file di esempio con nome storico combination.html](viewer/combination.html), rigenerato dal modello `examples/mixed/truss-bridge-20m-deck-loads.json` con il caso predefinito `LC_VISUAL`, conservando il modello e il titolo del file precedente.

I file HTML esportati contengono una copia dello script: per aggiornare altri report già generati occorre esportarli nuovamente.
