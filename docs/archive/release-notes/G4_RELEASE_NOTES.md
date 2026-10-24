# G4 — Stabilità del legno NTC 2018 (gate Windows da eseguire)

Data: 24 ottobre 2026. Baseline: **G3 FIX1 — G3 GATE PASSED comunicato dall'utente**.

## Scopo preciso

Introduce due **regole distinte e parziali**, identificate come NTC 2018 (D.M. 17/01/2018), esclusivamente per **legno massiccio di conifera** e **legno lamellare omogeneo**, sezione **rettangolare piena, prismatica, non intagliata**, orientamento verificato:

1. **Compressione centrata pura e stabilità flessionale sui due assi** secondo NTC §4.4.8.2.2, formule [4.4.13]–[4.4.16]. Il `kcrit,c` è ottenuto per entrambi gli assi e governa il valore minore. `βc=0,2` per massiccio, `βc=0,1` per lamellare, **solo** con rettilineità documentata (§4.4.15). È usato il modulo di elasticità **E0,05**, non `Emean`.
2. **Svergolamento di trave inflessa sull'asse forte** secondo NTC §4.4.8.2.1, formule [4.4.11]–[4.4.12]. `σm,crit` deve essere calcolata e verificata esternamente, nel segmento delimitato da due ritegni torsionali documentati e usando moduli caratteristici al frattile 5%. G4 implementa **soltanto** `λrel,m = sqrt(fm,k / σm,crit)`, `kcrit,m` a tratti e il controllo relativo.

G4 **non calcola** lunghezze libere d'inflessione con una formula implicita ricavata dal FEM e **non determina** un momento/tensione critica di svergolamento dall'asta 2D. Questi richiedono informazioni fisiche e verifiche indipendenti su supporti, connessioni, ritegni e rigidezza. I flag di evidenza sono dichiarazioni esterne; il software non certifica che siano vere.

## Requisiti bloccanti

- Profilo italiano G1 NTC, coefficienti e tabella italiana verificati, combinazione e impronta SHA-256 coerenti.
- Identità esatta di asta/sezione/analisi/combinazione/posizione **e lato del carico concentrato**; nessun inviluppo inconcomitante.
- Sezione intatta, geometria e materiale documentati, resistenze di catalogo utilizzate come **riferimenti** (non certificati), `kh=1` conservativo.
- Assenza di azioni fuori piano certificata esternamente; rettilineità iniziale (§4.4.15) e proprietà `E0,05` documentate.
- Colonna: `N<0, V=0, M=0`, compressione centrata sull'intera asta e **due lunghezze efficaci esterne** dimostrate.
- Trave: `N=0, M ≠ 0`, segmento contenente la stazione critica, rito torsionale su entrambe le estremità e `σm,crit` esterna positiva verificata; con `V ≠ 0` serve evidenza di un **controllo a taglio separato superato** (G4 non lo esegue).

Se le condizioni non sono soddisfatte: `MissingData` oppure `NotApplicable`, mai un finto `Passed`. Un rapporto G4 positivo equivale **soltanto** alla verifica locale di stabilità nello scenario dichiarato. Non implica la sicurezza delle unioni/ritegni, dell'intera asta o della struttura.

## Esempi analitici indipendenti

C24 100 × 200 mm; colonna 3 m con lunghezze efficaci indipendenti `Ly=Lz=3,0 m`, `N=-100 kN`; classe servizio 1, durata media, `kmod=0,8`, `γM=1,50`, `E0,05=7400 MPa`. Raggi di inerzia 57,735 e 28,868 mm, snellezze relative 0,8811 e 1,7622. `kcrit,y≈0,77436`, `kcrit,z≈0,28457`. Rapporto **1,5688023186 > 1 (Failed)**. Il risultato dipende dalle lunghezze efficaci dichiarate esternamente.

C24 100 × 200 mm; trave, `M=3 kNm`, `N=V=0`, tensione critica **esterna** `σm,crit=12 MPa`, `fm,k=24 MPa`: `λrel,m=√2`, `kcrit,m≈0,50`. `fm,d=12,8 MPa`, `σm,d=4,5 MPa`, rapporto **0,703125 < 1 (Passed nella sola verifica G4)**.

Dati machine-readable: `benchmarks/G4_NTC2018_STABILITY_GOLDEN.json`.

## Codice nuovo

- `src/StructuralSolver2D.Verification.Timber/Ntc2018TimberStabilityRulesG4.cs`
- `tests/StructuralSolver2D.Verification.Tests/M92/Ntc2018TimberStabilityG4Tests.cs`
- `benchmarks/G4_NTC2018_STABILITY_GOLDEN.json`
- `scripts/run-g4-tests.ps1`, `scripts/verify-g4-manifest.ps1`

I sorgenti C# della baseline **G3 FIX1 sono mantenuti byte per byte**.

## Collaudo richiesto (Windows, .NET 8)

```powershell
.\scripts\verify-g4-manifest.ps1
.\scripts\run-g4-tests.ps1
```

Il runner esegue build Release con avvisi come errori; regressioni Verification, Catalogs, Core, Analysis/FEM, Reporting, CLI. **Nessuna compilazione o test xUnit G4 è stata eseguita nell'ambiente di preparazione** (SDK .NET non disponibile). I test precedenti G3, G2 e G1 sono stati confermati dall'utente, non re-eseguiti qui.

## Fuori ambito

Pressoflessione e buckling combinato, effetti del secondo ordine non documentati, verifiche complete dei sistemi di controvento e collegamenti, calcolo automatico dei vincoli reali, instabilità torsionale complessa, deformazioni SLE, incendi, combinazioni NTC complete, conformità normativa generale o utilizzabilità per progetto esecutivo.

Prossimo gate, **G5**: deformazioni e deformazioni differite del legno ai sensi §4.4.7 e Circolare C4.4.7, solo dopo collaudo Windows G4.
