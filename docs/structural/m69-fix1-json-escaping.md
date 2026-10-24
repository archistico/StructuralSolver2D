# M69-FIX1 — Verifica dell'escaping JSON nel viewer standalone

**Stato:** test aggiornati; esecuzione completa .NET 8 e controllo manuale del browser ancora da confermare. Data: 23/10/2026.

## Errore osservato

Il test xUnit `InteractiveNonlinearViewerM69Tests.MaliciousLabels_AreEscapedInsideStandaloneHtml` terminava con `Assert.Contains()` perché attendeva `&lt;/script&gt;` nell'HTML generato. Questo è un confronto errato: il valore ostile è inserito in un elemento `<script type="application/json" id="dataset">`, non in un nodo testuale HTML.

## Contratto di sicurezza corretto

- **Serializzazione:** `System.Text.Json` (encoder predefinito) codifica i caratteri `<` e `>` con le escape JSON `\u003C` e `\u003E`, impedendo che una stringa malevola chiuda il blocco di dati ed esponga nuovo codice.
- **Parsing:** il dataset deve essere JSON valido e, una volta deserializzato, deve restituire *esattamente* gli identificatori e il titolo di origine, senza conversioni spurie in entità HTML.
- **DOM:** il client assegna i valori di testo via `textContent` e crea gli elementi SVG tramite le API DOM; non usa `innerHTML` con dati non fidati.
- **Struttura HTML:** restano soltanto i due tag script progettati (dataset JSON e JavaScript interno).

## Modifica

Unico file C# modificato: `tests/StructuralSolver2D.Reporting.Tests/Nonlinear/InteractiveNonlinearViewerM69Tests.cs`. Il test mantiene lo stesso nome e controlla payload non presente in chiaro, escape JSON, numero di script, parsing JSON e recupero fedele di `title` e `nodes[1].id`. Gli altri file aggiornati sono solo documentazione. Nessuna modifica al solver o agli exporter.

## Verifiche da eseguire

Dalla radice del progetto estratto in una directory pulita:

```powershell
.\scripts\run-m69-tests.ps1
```

Lo script inizia eliminando `bin` e `obj`, compila Release, esegue i test dedicati e la regressione completa, poi prova l'export CLI reale. Aprire successivamente gli HTML prodotti in Edge/Chrome e confrontare i risultati con i report M68. Un test di escaping non equivale da solo a un audit di sicurezza esaustivo.
