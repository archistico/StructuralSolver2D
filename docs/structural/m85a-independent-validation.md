# M85-A — Validazione indipendente del motore statico 2D

**24 ottobre 2026 — Stato:** implementato; esecuzione del gate Windows .NET 8 da confermare.

**Base:** M83-A, test Windows dichiarati superati dall'utente il 24/10/2026. Sono presenti benchmark di scalabilità e relativa strumentazione, ma **i numeri CSV del computer Windows non sono disponibili** in questo checkpoint: il passaggio M83-B a matrici sparse non è approvato né escluso definitivamente.

## Obiettivo e perimetro

Questa tranche di M85 introduce **riferimenti indipendenti** e **proprietà metamorfiche** per l'analisi statica elastica di aste piane. I risultati attesi sono ricavati senza chiamare i generatori di matrici FEM dell'applicazione. Si controllano spostamenti, rotazioni, reazioni e azioni di estremità secondo un sistema di unità coerente: **kN, m, kNm, rad**, E in kN/m², A in m², I in m⁴. Non è un confronto con un software certificato esterno e non è una validazione professionale dell'opera.

Il codice di produzione `src/` non è modificato. Non sono implementate nuove funzionalità di calcolo, norme o GUI.

## Matrice riproducibile dei controlli aggiunti

| ID test C# | Caso | Verifica indipendente | Limite dichiarato |
|---|---|---|---|
| `InclinedCantilever_AxialShearAndTipMoment_MatchClosedFormBeamTheory` | Mensola inclinata 3–4–5 m, carichi Fx=+11 kN, Fy=−7 kN, Mz=+5 kNm | Decomposizione analitica N=Fx·3/5+Fy·4/5, V=−Fx·4/5+Fy·3/5; `u=NL/(EA)`, `v=VL³/(3EI)+ML²/(2EI)`, `θ=VL²/(2EI)+ML/(EI)`; equilibrio completo alla base | Euler–Bernoulli, elastico, singola asta prismatica |
| `TwoNonIdenticalTrussBarsInSeries_MatchElementaryAxialCompliance` | Due aste reticolari in serie, lunghezze 2+3 m, A differenti, P=20 kN | `u_B=PL1/(EA1)`, `u_C=u_B+PL2/(EA2)`; forze di estremità ±P, reazioni e confronto fra Truss2D e PlaneStructure2D | Nodo intermedio e finale trattenuti trasversalmente per eliminare meccanismi |
| `IndeterminatePortal_RespectsMaxwellBettiAndSignedLoadSuperposition` | Telaio incastrato a due colonne, trave superiore; tre azioni P, Q, R | Reciprocità `f_Qᵀu_P=f_Pᵀu_Q`; sovrapposizione indipendente di spostamenti, reazioni e forze di estremità per combinazione `1.25 P − 0.4 Q + 0.3 R` | Solo sistema lineare con vincoli omogenei, nessuna plasticità/secondo ordine |
| `GlobalRigidRotationOfPortalAndLoads_PreservesLocalEndForces` (4 scenari) | Stesso portale ruotato di +90°, carichi ruotati con il modello | `u'ₓ=−uᵧ`, `u'ᵧ=uₓ`, `θ'=θ`; stessa trasformazione per reazioni, invarianti tutte le sei forze locali di estremità | Rotazione rigida propria, basi locali coerenti, appoggi incastrati |

**Provenienza attesa:** formule elementari di teoria elastica delle travi, lavoro reciproco di Maxwell–Betti e invarianza tensoriale/covarianza per rotazione; qui dichiarate e riproducibili senza dipendenza numerica dalla matrice implementativa. Non sono presentate come benchmark esterni con un documento di terza parte.

**Tolleranze:** controllo assoluto 1e−10 m/rad sugli spostamenti dove previsto, 1e−8 kN/kNm su reazioni e azioni. `Close` applica anche il maggiore fra tolleranza assoluta e 1e−8 × |valore atteso| per limitare falsi negativi a scale differenti. Sono controllati valori finiti; nessuna soglia prestazionale sui millisecondi.

## Gate operativo

Da PowerShell nella radice del progetto:

```powershell
.\scripts\run-m85-tests.ps1
```

Sequenza: riparazione non distruttiva di eventuali sorgenti M82 obsoleti; pulizia `bin/obj`; `dotnet build` Release; gate mirati M85, M82, M83, verifica energetica; regressione completa separata per Core, Analysis, Reporting e CLI. Si interrompe con errore se qualunque gate fallisce. I benchmark cronometrati M83 **non** vengono ri-eseguiti inutilmente in M85-A; i loro file sono prodotti da `run-m83-tests.ps1`.

Nell'ambiente di confezionamento .NET 8 non è disponibile. Pertanto **non dichiarare il gate superato** fino al risultato Windows dell'utente.

## Criticità e cosa manca alla chiusura di M85 / M86

1. **Confronti esterni:** aggiungere in M85-B esempi pubblicati e versionati con riferimento completo, ipotesi e grandezze confrontabili; distinguere riferimenti analitici e dati di altri solver.
2. **Cross-layer:** verificare sistematicamente gli stessi numeri tramite JSON→CLI→API→report, compresi segni, casi e combinazioni (M85-B / M86).
3. **Perimetro dei metodi avanzati:** M82 rimane linearizzato a sforzo assiale congelato, non un secondo ordine generale né una verifica di instabilità normativa. Le limitazioni vanno esplicitate nei report.
4. **Scalabilità:** la decisione di sviluppare M83-B richiede il CSV reale sul PC target; evitare di introdurre matrici sparse per ipotesi.
5. **Semplificazioni:** differire M84 plasticità globale e ulteriori ampliamenti di dinamica o interfaccia; mantenere il rilascio Core centrato su analisi elastica, diagnostica e validazione.

**Criterio di promozione:** tutte le nuove prove e la regressione completa superate su Windows; dopo M85-A restano aperti test esterni/cross-layer, documentazione dei limiti e gate release M86. "Test superati" non significa "software certificato".
