# StructuralSolver2D — G1 NTC 2018 (24 ottobre 2026)

## Baseline confermata
M92-B: gate Windows dichiarato superato dall'utente (selettore EC5, integrazione M92-A). Non equivale alla validazione dell'Annesso Nazionale né a conformità dell'asta.

## Nuove funzionalità
- Profilo *nativo* NTC 2018 per coefficienti legno (massa/lamellare; gamma_M A, B ed eccezionale) e `kmod` da Tab. 4.4.IV, indipendente dal catalogo EC5.
- Provenienza delle azioni e fingerprint del manifest; durata governante da fattori non nulli.
- Riduzione larghezza taglio `kcr` secondo §C4.4.8.1.9, quando pertinente e dichiarata.
- Coefficienti azioni A1 (senza combinazioni psi) e gamma_M informativi acciaio.
- Casi xUnit di dati invalidi, completezza azioni, colonna B, edizioni, esempi analitici e separazione EC5/NTC.

## Limiti deliberati
Non sono state implementate verifiche di resistenza NTC. G1 produce SOLO coefficienti e provenienza, non un risultato Passed. M92-A conserva l'identità EC5 e non può consumare profili G1 come se fossero EC5. Nessuna presunzione sulla completezza della combinazione o sul controllo produttivo.

## Test Windows
```
.\scripts\verify-g1-manifest.ps1
.\scripts\run-g1-tests.ps1
```
Tutti i test e l'intera soluzione sono da compilare su Windows con .NET 8; qui sono disponibili solo controlli statici e numerici indipendenti.

## G1 FIX1 — correzione test di instradamento normativo (24 ottobre 2026)

- Riprodotto il fallimento segnalato sul Windows dell'utente: compilazione pulita, 345/346 test superati; un'asserzione G1 confrontava `NotApplicable` anziché `Unsupported`.
- Corretto esclusivamente `Ntc2018TimberCoefficientSelectorG1Tests.NtcCoefficientProfileCannotBeReusedAsAnEc5M92AResult`: per profilo NTC rispetto a regola EC5, `DesignCheckEngine` risponde correttamente `Unsupported` prima dell'invocazione della regola.
- Il test ora assicura anche che non siano prodotti neppure per errore `Utilization` o `Resistance`.
- Nessuna modifica ai sorgenti di produzione, ai coefficienti NTC/EC5 o ai benchmark. G1 rimane selettore coefficienti, non verifica di resistenza NTC né attestazione di conformità dell'asta.
- Il manifest SHA-256 e il pacchetto completo sono stati rigenerati. Eseguire su Windows:

```powershell
.\scripts\verify-g1-manifest.ps1
.\scripts\run-g1-tests.ps1
```

**Stato:** controlli ZIP, manifest e confronti sorgenti eseguiti qui; compilazione e test FIX1 ancora da confermare su Windows.
