# M66 — Gate di validazione numerica non lineare 2D

**Data:** 23 ottobre 2026  
**Stato:** implementato; **in attesa dei test C# sul Windows dell'utente**.  
**Prerequisiti:** M63, M64 e M65 confermati dall'utente come superati.  
**Ambito:** Truss2D a geometria iniziale fissa, piccole deformazioni, plasticità assiale bilineare a incrudimento **isotropo**, masse consistenti, Newmark medio (β=1/4, γ=1/2), Newton–Raphson, vincoli omogenei; nessuna API pubblica non lineare.

## Obiettivo e filosofia dei benchmark

M66 è un **gate di verifica**: aggiunge riferimenti costruiti con algebra e metodi indipendenti dal solver, senza introdurre nuove funzionalità ingegneristiche. I riferimenti eseguibili sono esclusivamente **C#/.NET xUnit**. Non è necessario Python; PowerShell serve soltanto a ripulire `bin`/`obj`, compilare ed eseguire i test.

I test non devono copiare il risultato della stessa procedura Newton–Raphson usata nella produzione. Per questo la suite introduce `NonlinearM66AlgebraicReference.cs`, una piccola implementazione *solo nei test* che risolve il problema dinamico scalarizzato scegliendo analiticamente il ramo elastico, plastico positivo o plastico negativo. Per le due aste statiche in parallelo usa la soluzione costitutiva monotona algebrica e la bisezione, senza assemblare la matrice tangente globale.

## Riferimenti e formule

### Statica: due aste parallele con snervamento differente

Aste di lunghezza L=2 m, area A=0,01 m², E=200.000 kN/m², H=10.000 kN/m², limiti Y₁=150 e Y₂=300 kN/m². Applicazione di forza assiale crescente F=4λ kN.

Per ogni asta, in **caricamento monotono positivo da stato vergine**:

- finché `u/L ≤ Y/E`: `N(u)=AEu/L`;
- dopo snervamento: `N(u)=A[Y+(EH/(E+H))(u/L−Y/E)]`.

Il riferimento cerca la radice di `N₁(u)+N₂(u)=F`. Il primo snervamento si verifica a `u=0,0015 m`, `F=3 kN`, mentre il secondo avviene successivamente: non è corretto applicare simultaneamente lo snervamento alle due aste. I test confrontano spostamenti, forze singole, somme e reazioni, non solo il risultato finale. Al variare del numero di incrementi positivi, la risposta finale deve rimanere invariata entro le tolleranze.

### Dinamica: soluzione esatta per rami del passo Newmark scalare

Una singola asta con estremità iniziale bloccata e estremità libera solo lungo l'asse ha `k=EA/L=1.000 kN/m` e, per densità lineare `μ=1,5 kN·s²/m²`, massa ridotta **consistente** `m=μL/3=1 kN·s²/m`. Sotto un carico `F(t)=4λ(t) kN` e viscosità `c=αm+β_d k`, per ogni passo Newmark si risolve direttamente la legge affine del ramo ammesso:

`m(4/Δt²)(uₙ₊₁−u_pred) + c[v_pred+(2/Δt)(uₙ₊₁−u_pred)] + N(uₙ₊₁,zₙ) = Fₙ₊₁`.

Nel riferimento `N` è scritto separatamente per ramo elastico, plastico positivo e plastico negativo; la radice algebrica viene accettata solo se soddisfa il relativo criterio di snervamento. Lo stato `zₙ` cambia soltanto dopo che il ramo è stato scelto. È quindi un algoritmo distinto dal Newton–Raphson iterativo del risolutore.

Si confrontano **tutti i campioni temporali**: spostamento, velocità, accelerazione, forza assiale, deformazione plastica, plasticità accumulata, energia elastica, di incrudimento e dissipata, oltre ai residui di equilibrio e del lavoro discreto. Il riferimento utilizza cicli con scarico e inversione e smorzamento `C=αM+β_d K₀`.

### Invarianti e limiti

- Due aste identiche in parallelo, ciascuna con metà area e metà massa lineare, devono riprodurre la risposta dinamica della singola asta originaria: massa, rigidezza, forza, energia e storia plastica totale non devono cambiare.
- Traslazione rigida di un elemento: estensione e forza assiale nulle. La tangente è simmetrica e deve coincidere con la derivata direzionale delle forze.
- Nel limite elastico, convergenza del Newmark sotto carico a rampa verso la soluzione chiusa dell'oscillatore `u(T)=(F_max/k)[1−sin(ωT)/(ωT)]`.
- Meccanismi trasversali senza sufficiente vincolo devono essere identificati **anche quando la matrice di massa è invertibile**.
- Stati plastici corrotti (`NaN`, plasticità accumulata negativa o inferiore al modulo della deformazione plastica) devono essere rifiutati.
- La dissipazione plastica cumulativa non può diminuire; il fallimento di Newton non può modificare gli stati accettati.

## Tolleranze fissate prima dei risultati

| Grandezza | Tolleranza preimpostata | Perché |
|---|---:|---|
| Spostamento di riferimento [m] | `2e-10` assoluta + `1e-8` relativa | Soluzioni dirette scalarizzate |
| Velocità [m/s] | `2e-9` assoluta + `1e-8` relativa | Propagazione Newmark |
| Accelerazione [m/s²] | `2e-8` assoluta + `1e-8` relativa | Due divisioni per Δt nei termini dinamici |
| Forza / residuo [kN] | `2e-8` assoluta + `1e-8` relativa | Equilibrio e somme di elementi |
| Stato plastico e dissipazione | `1e-10` per deformazioni, `1e-9` kN·m per energia | Memoria storica ed energie |
| Convergenza elastica a rampa | errori strettamente decrescenti, errore fine `<3e-5` m | Confronto con funzione analitica |

Le soglie sono **criteri numerici di questa fixture**, non limiti normativi o garanzie di accuratezza per altre strutture. Non modificarle per far passare un test senza documentarne l'origine.

## Esecuzione e gate

Dal progetto estratto in una cartella pulita:

```powershell
.\scripts\run-m66-tests.ps1
```

Il runner **prima di tutto** richiama `clean-project-build.ps1` per eliminare `bin` e `obj` appartenenti ai progetti. Poi esegue build Release, test M66 e precedenti M63–M65, infine la regressione **di tutta la soluzione**. Non invoca Python o altri interpreti per i benchmark.

**M66 supera il gate** soltanto quando la compilazione e ogni test C# sono confermati sull'ambiente .NET 8 dell'utente. L'integrità ZIP, la revisione delle formule e la presenza delle fixture non equivalgono all'esecuzione dei test.

## Esclusioni (da non presentare come funzioni validate)

- Analisi di collasso, grandi spostamenti, instabilità o effetto P–Δ.
- Plasticità delle travi Frame2D, interazione tra sforzo normale e momento, frattura e danno.
- Masse concentrate nodali arbitrarie, vincoli mobili, substepping e integratori adattativi.
- Risolutori sparsi per strutture grandi, validazione normative sismiche, verifica di sicurezza.
- Ispezione grafica dei viewer HTML: richiede un controllo distinto nel browser reale.

**Prossimo M67:** reazioni e sollecitazioni interne nel dominio del tempo, distinguendo carichi, forze interne, inerzia e smorzamento. **M68 (API pubblica non lineare) rimane bloccato** finché M66 non sarà validato, a prescindere dai benchmark locali.
