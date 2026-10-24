# M87-B — Collegamento FEM → verifiche normative (24 ottobre 2026)

## Stato

- **M86 RC1:** gate Windows completato (1.334 test, nessun warning/error).
- **M87-A:** gate Windows completato (19 test Verification, 1.334 regressione, nessun warning/error); fonte: log 2026-10-24 15:13.
- **M87-B:** implementato e controllato staticamente, **gate Windows ancora da eseguire**.
- Non esistono formule EC3, EC5 o NTC implementate. Un risultato FEM non significa "verificato".

## Scelte architetturali

1. `StructuralSolver2D.Analysis`: aggiunto un piccolo metodo pubblico di post-processing, `Frame2DInternalForceSampler.EvaluateMemberSection`, che riusa gli stessi polinomi del campionatore esistente; nessuna modifica ad assemblaggio, matrice di rigidezza o soluzione FEM.
2. `StructuralSolver2D.Verification`: conserva il registro di regole e gli esiti fail-closed M87-A; tre campi opzionali di provenienza sono aggiunti a `DesignEffectSnapshot`: `MemberId`, `TargetKind`, `SectionSide`. Il validator respinge campi contraddittori o valori enum sconosciuti. I contratti precedenti restano compatibili.
3. **Nuovo progetto** `StructuralSolver2D.Verification.Integration`: collega Core/Analysis al modulo Verification. È l'unico progetto che dipende da entrambi: Verification non dipende dal risolutore.
4. `FemStationVerificationBridgeM87B.Capture` ricalcola *un solo* caso o combinazione **statica elastica di primo ordine** usando l'API pubblica; legge N, V, M in una sezione esplicita dall'output coerente con il modello e conserva segni locali e unità. Non usa massimi indipendenti né inviluppi.
5. `FemStationVerificationBridgeM87B.Evaluate` trasferisce il risultato al motore M87-A, senza creare resistenze: regole assenti → **Unsupported**, aggregato **Incomplete**.

## API essenziali

```csharp
var bridge = new FemStationVerificationBridgeM87B();
DesignCheckRequest request = bridge.Capture(
    model,
    new DesignMemberContext("M", DesignMaterial.Steel, "SEC", 4.0),
    DesignLimitState.Ultimate,
    profile, // document, edition, annex and coefficients explicitly supplied
    new[] { "RULE-NOT-YET-IMPLEMENTED" },
    new FemStationSelectionM87B(
        StructuralAnalysisTargetKind.LoadCombination, "ULS-MANUAL", 1.3));

var report = new DesignCheckEngine(Array.Empty<IDesignCheckRule>()).Evaluate(request);
// OverallStatus == Incomplete; results == Unsupported.
```

Importare namespace `StructuralSolver2D.Analysis.PublicApi`, `StructuralSolver2D.Verification.Contracts`, `StructuralSolver2D.Verification.Engine`, `StructuralSolver2D.Verification.Integration`, `StructuralSolver2D.Verification.Rules`.

## Provenienza e discontinuità

- `Effects.AnalysisId = "FEM:linear-static:first-order"` identifica la famiglia di analisi, **non** un hash del modello. `Effects.CombinationId` conserva l'ID esplicito, anche quando si seleziona un solo caso; `Effects.TargetKind` distingue caso e combinazione. Il nome legacy `CombinationId` non è stato cambiato per compatibilità M87-A.
- `Effects.MemberId`, `Effects.PositionMetres`, `Effects.SectionSide` tracciano sezione e lato. Tutti gli effetti N/V/M sono simultanei nella stessa sezione, nello stesso risultato e sullo stesso lato.
- Nei punti di applicazione di forze concentrate su asta il valore del taglio **salta**. È obbligatorio scegliere `SectionLimitM78.BeforePointLoad` oppure `AfterPointLoad`; la richiesta `Continuous` fallisce invece di mediare. Fuori dai punti di salto è richiesto `Continuous`. La distanza può essere calcolata o snap-ata soltanto entro un limite di arrotondamento floating-point estremamente ristretto.
- Le caratteristiche elastiche usate dal FEM **non** sono convertite automaticamente in proprietà nominali per EC3/EC5; le lunghezze libere e i ritegni laterali restano espliciti e nullable.

## Controlli fail-closed

- Il `memberId`, la sezione e la lunghezza dichiarati devono coincidere con il modello FEM; nessuna assegnazione automatica di materiale normativo.
- Distinzione esplicita del tipo di target; ID duplicati tra caso/combinazione sono rifiutati per evitare di applicare carichi diversi da quelli desiderati.
- Modelli Truss2D, secondo ordine M82, dinamica, risultati da inviluppi e forze fuori piano non transitano in questo adapter M87-B. Serviranno adapter espliciti successivi.
- Il caller deve valutare **tutte** le sezioni e i target significativi per la verifica richiesta; il bridge non afferma di aver trovato una posizione governante né produce una verifica dell'intera asta.
- `DeflectionMetres` rimane null: le verifiche SLE richiederanno selezioni e misure dedicate.
- Le eccezioni di `Capture` rappresentano un trasferimento FEM non riuscito: **non trasformarle in Passed**.

## Test

Nuova suite xUnit `FemStationVerificationBridgeM87BTests`: sezione esatta fuori griglia con carico di punta, combinazione firmata, salto per carico concentrato, rigetto incoerenze, stato Unsupported e mismatch di provenienza. I 19 test M87-A sono mantenuti. Gate Windows:

```powershell
.\scripts\verify-m87b-manifest.ps1
.\scripts\run-m87b-tests.ps1
```

Build Release (`-warnaserror`), test Verification e regressione dei quattro progetti storici, con opzione `-WithM86Smoke`. Il manifest rappresenta esattamente il pacchetto scaricato; eseguire la verifica prima di modificarne i file.

## Roadmap successiva e complessità

- **M87-C**: selezione documentata delle *candidate sections* per controlli globali, con N/V/M concomitanti; estremi indipendenti **mai** fusi nello stesso snapshot.
- **M88**: cataloghi materiali e sezioni acciaio/legno con provenienza, unità e proprietà richieste.
- **M89–M95**: verifiche limitate EC3/EC5/NTC e SLE validate contro esempi indipendenti. La stabilità fuori piano è il punto più delicato e richiede dati aggiuntivi.
- **M96–M99**: test, documentazione, tracciabilità e release normativa; **solo successivamente GUI Avalonia**.

Per semplificare, mantenere un'asta e una sezione per richiesta, cataloghi di profili comuni, nessuna nuova formula normativa prima dei relativi benchmark e niente solver sparso/plasticità nella prima versione.
