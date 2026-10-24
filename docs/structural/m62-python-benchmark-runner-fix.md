# M62-FIX1 — gestione affidabile del benchmark Python opzionale

**Data:** 2026-10-23  
**Ambito:** esclusivamente `scripts/run-m62-tests.ps1` e documentazione; nessuna modifica a solver, modelli, test C#, formule o fixture.

## Segnalazione ricevuta

L'utente ha superato i blocchi `M62 1/4`, `2/4` e `3/4`, ma lo script PowerShell si è interrotto in `M62 4/4` con il messaggio generico `M62 independent Python benchmark failed.` Non è stato fornito il traceback o l'output originale del comando Python. **La causa specifica sulla macchina Windows non è verificata.**

Il file `scripts/m62-reference-piecewise-sdof.py` è stato eseguito con Python 3.13.5 nel sistema di confezionamento con codice di uscita zero, marcatura `M62 independent piecewise-analytical reference: PASS` e residuo di equilibrio `1.585e-12 kN`. Il confronto indipendente non prova l'esecuzione su Windows, ma esclude una rottura deterministica di quel riferimento nell'ambiente testato.

## Difetto nello script precedente

Il controllo `Get-Command python` non è sufficiente per stabilire che un interprete Python funzioni: Windows può esporre un alias `python.exe` verso Microsoft Store. Versioni incompatibili, associazioni non utilizzabili o interpreti Python incompleti possono produrre un esito analogo. In questi casi lo script precedente considerava ogni uscita non-zero un fallimento numerico, senza distinguere l'ambiente dal benchmark.

## Comportamento dopo la correzione

1. Rimangono obbligatori, nell'ordine, **pulizia `bin` e `obj`**, build Release, test M62 C#, regressione completa.
2. La fase 4 controlla se `python` è un interprete **Python 3.7+** utilizzabile; in caso contrario prova `py -3` (launcher di Windows). Il preflight verifica anche l'importazione di `dataclasses`, necessaria al riferimento.
3. Se entrambi gli interpreti sono assenti o inutilizzabili, emette un warning esplicito `SKIPPED`, senza invalidare i test C# già superati; il risultato globale segnala quindi la limitazione del controllo opzionale.
4. Se un interprete valido viene trovato, **esegue il benchmark** e stampa tutto l'output, inclusi gli errori. Codice non-zero o marcatura `PASS` mancante rimangono **fallimenti bloccanti**: non mascherare anomalie numeriche reali.
5. Le stringhe d'errore identificano l'interprete, il codice di uscita e il comando esatto da lanciare a mano.

## Verifica su Windows

```powershell
.\scripts\run-m62-tests.ps1
```

Per verificare soltanto il riferimento Python e isolare un errore nell'interprete, dalla radice del progetto:

```powershell
python --version
python scripts\m62-reference-piecewise-sdof.py
# oppure, se python non funziona:
py -3 --version
py -3 scripts\m62-reference-piecewise-sdof.py
```

Se il benchmark Python viene eseguito e fallisce, serve l'**output completo precedente all'eccezione PowerShell** per stabilire se il problema sia di ambiente, path o algoritmo. Non si deve modificare la fisica del prototipo sulla base del solo messaggio generico.

## Verifica effettuata per FIX1

- Riferimento Python eseguito con Python 3.13.5: **PASS**, exit code 0.
- Ordine di pulizia `bin` e `obj` come prima azione del runner: mantenuto.
- Non è disponibile PowerShell/.NET nell'ambiente di confezionamento: la logica di selezione degli interpreti e la regressione completa richiedono verifica su Windows.
- Nessuna affermazione di superamento dei test di FIX1 su Windows prima del riscontro dell'utente.
