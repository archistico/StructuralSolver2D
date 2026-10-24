# M72-FIX2 — Pulizia degli avvisi xUnit (23/10/2026)

Questa correzione parte **esclusivamente** da `M72-FIX1_POWERSHELL_ARGUMENTS`.

## Motivo

La compilazione Windows ha segnalato sei avvisi del pacchetto `xunit.analyzers`:
`xUnit2012` (due), `xUnit2031` (tre) e `xUnit2013` (uno). Gli avvisi riguardano
solamente la forma delle asserzioni presenti nei test, non il comportamento del
solutore strutturale o la correttezza delle formule.

## Correzioni

| Test | Regola | Sostituzione |
| --- | --- | --- |
| `NonlinearTruss2DNewmarkM65Tests` | xUnit2012 | `Assert.True(steps.Any(predicate))` diventa `Assert.Contains(steps, predicate)` |
| `NonlinearValidationM66Tests` | xUnit2012 | Stessa correzione |
| `DynamicForceRecoveryM67Tests` (tre punti) | xUnit2031 | `Assert.Single(reactions.Where(predicate))` diventa `Assert.Single(reactions, predicate)` |
| `NonlinearPublicApiM68Tests` | xUnit2013 | `Assert.Equal(1, forces.Count)` diventa `Assert.Single(forces)` |

Le nuove asserzioni conservano l'intento originario: verificare l'esistenza di
almeno un passo plastico, l'unicità della reazione per nodo e la singolarità
della forza assiale attesa. Non sono stati cambiati soglie numeriche, dati di
input, API, solutori, script di test o configurazione degli analyzer.

## Validazione richiesta

Da PowerShell, alla radice della soluzione:

```powershell
.\scripts\run-m72-tests.ps1
```

Il runner già esistente effettua prima la pulizia `bin/obj`, poi la build Release,
i test M71/M72 e l'intera regressione. Controllare che siano scomparsi i sei
avvisi elencati e che la suite passi. Questo checkpoint non certifica i test
M71/M72, che non sono stati eseguiti nell'ambiente di preparazione.
