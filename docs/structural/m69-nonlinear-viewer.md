# M69 — Viewer HTML per reticolari elasto-plastiche 2D

**Stato al 23/10/2026 (M69-FIX1):** M68 validato. La prima esecuzione M69 sul Windows dell'utente ha superato la compilazione fino ai test di Reporting, ma si è fermata su una **aspettativa errata del test di escaping** (`&lt;/script&gt;` dentro un blocco JSON). Il test è stato corretto in M69-FIX1; **il gate .NET completo e la QA visiva degli HTML reali restano da ripetere**. Il codice dell'exporter e lo script JavaScript non sono stati modificati.

## Scopo

Il viewer M69 è uno strumento di esplorazione dei **risultati già calcolati da M64/M65 attraverso l'API M68**. Non introduce algoritmi FEM nuovi, non effettua verifiche di sicurezza e non deve inventare stati intermedi. La sua rappresentazione deformata è amplificata per comodità grafica, ma la curva carico–spostamento, le forze e gli stati plastici sono ricavati dai valori fisici restituiti dal solver.

Sono disponibili due comandi dedicati:

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- nonlinear-static-viewer docs/structural/fixtures/m68-nonlinear-static.json reports/nonlinear-static.html
dotnet run --project src/StructuralSolver2D.Cli -- nonlinear-time-viewer docs/structural/fixtures/m68-nonlinear-time.json reports/nonlinear-time.html
```

Entrambi producono **HTML autonomi**, con CSS, JavaScript e dati già incorporati. Funzionano offline senza librerie o servizi esterni.

## Contenuti

- Geometria iniziale tratteggiata e deformata ingrandita con fattore grafico regolabile (Truss2D, rette tra i nodi deformati).
- Selezione e riproduzione dei soli **incrementi accettati**; per la dinamica l'etichetta visualizza il tempo fisico del campione, per la statica il fattore di carico.
- Elementi **senza plasticità accumulata**, con **plasticità accumulata** oppure **selezionati** distinguibili anche mediante testo numerico; la soglia grafica è `p > 1e-13` e non è un criterio di collasso o verifica strutturale.
- Selettori di nodo, componente Ux/Uy e asta; spostamenti, reazioni, forza assiale, deformazione plastica con segno e deformazione plastica accumulata.
- Curve carico–spostamento per un nodo, forza assiale nell'asta, deformazione plastica accumulata nell'asta ed energia plastica **cumulata a livello globale**.
- L'HTML mostra esplicitamente `Completed` e l'eventuale causa dell'arresto. In caso di analisi parziale mostra soltanto stati accettati e il comando termina con **codice 2** anche se l'HTML è stato generato.

## Limiti espliciti

1. Solo Truss2D, piccole deformazioni, incrudimento isotropo, senza instabilità geometrica, plasticità flessionale, danno o rottura.
2. Non vengono interpolate costitutivamente nuove grandezze tra due passi; le linee dei grafici collegano campioni calcolati. La riproduzione è a velocità grafica costante e non rappresenta i tempi reali tra campioni.
3. Il colore rappresenta lo **stato plastico accumulato**, non l'attuale attivazione della superficie di snervamento; un'asta scaricata può rimanere contrassegnata come plastificata.
4. In un'analisi dinamica la curva carico–spostamento mostra il fattore moltiplicativo dei carichi imposto dall'utente e lo spostamento, non un'azione globale di taglio alla base. Per la storia in tempo le altre curve usano l'ascissa in secondi.
5. L'energia plastica rappresentata è quella cumulata **dell'intero modello**, non per singola asta.
6. Non è una certificazione o verifica strutturale secondo NTC/Eurocodici. Alcune altre viste precedenti rimangono da ispezionare manualmente nei browser target.

## Sicurezza e riproducibilità

I testi dinamici sono inseriti nel DOM con `textContent`, senza interpretazione come HTML. `System.Text.Json` serializza con encoder HTML-safe la sezione di dati incorporata nell'HTML. Il report HTML non acquisisce risorse di rete, non richiede JavaScript da CDN e non modifica file dell'utente. Il codice verifica la corrispondenza fra geometria e ordine degli identificatori/array del risultato.

## Prove previste (solo C# e test manuali del browser)

1. `scripts/run-m69-tests.ps1`: prima pulizia `bin` e `obj`, poi compilazione Release, test xUnit per output statico/temporale, input malevoli, history vuota/parziale e mappature incoerenti; test CLI ed intera regressione.
2. Esecuzione reale dei due comandi `nonlinear-*-viewer` sulle fixture M68, verifica che l'HTML incorpori i dati e che la CLI restituisca codice 2 in caso di mancata convergenza.
3. Apertura dei due HTML in Edge/Chrome/Firefox, spostamento dello slider, clic sull'asta, cambio nodo e componente, controllo dei valori al primo e all'ultimo passo, prova Play/Pausa, vista mobile.
4. Confronto a campione dei valori con CSV e Markdown M68, soprattutto reazioni, forza assiale, deformazione plastica, energia e intervallo dei passi accettati.

**Gate di chiusura:** build e regressione .NET 8 superate, export reali confrontati con gli output API/CSV, nessuna eccezione JavaScript nel browser target. La verifica browser con dati sintetici non sostituisce questo gate.


## M69-FIX1 — Correzione del test di sicurezza dei dati incorporati

Il caso `MaliciousLabels_AreEscapedInsideStandaloneHtml` falliva perché attendeva la sottostringa HTML `&lt;/script&gt;`. I titoli e gli identificatori dei nodi, però, non sono inseriti direttamente nel markup: l'exporter li serializza in un elemento `<script type="application/json" id="dataset">` tramite `System.Text.Json`. L'encoder JSON predefinito codifica `<` e `>` come `\u003C` e `\u003E`, proteggendo dall'iniezione di un tag `</script>`. Sul DOM il viewer usa `textContent`, non `innerHTML`. Non bisogna sostituire la codifica JSON con entità HTML: ciò altererebbe i valori all'interno del JSON.

Il test ora verifica **sia la protezione sia la fedeltà dei dati**: nessun payload letterale nel file, nessun tag script aggiuntivo, JSON realmente parsabile, titolo e nodo restituiti identici dopo deserializzazione, uso di `textContent` per il titolo. Il codice di produzione, gli algoritmi FEM e le interfacce pubbliche restano invariati.

**Da verificare su Windows:** `scripts/run-m69-tests.ps1` su directory pulita (lo script rimuove `bin` e `obj` prima della build), poi aprire gli HTML reali in Edge per il collaudo manuale.
