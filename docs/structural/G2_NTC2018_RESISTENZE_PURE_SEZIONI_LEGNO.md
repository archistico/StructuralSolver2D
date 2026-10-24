# G2 — Verifiche locali PURE di sezioni rettangolari in legno secondo NTC 2018

**Stato: implementato in sorgenti, non ancora collaudato su Windows; baseline G1 FIX1 superata sul sistema dell'utente.**

## 1. Norma e campo ammesso

Riferimenti primari: *D.M. 17 gennaio 2018, NTC 2018*, §§4.4.6 e 4.4.8.1.1/1.3/1.6/1.9; *Circolare 21 gennaio 2019 n. 7*, §C4.4.8.1.9. Materiali documentati dal catalogo M88-C (EN 338:2016 e EN 14080, dati di riferimento non certificati per l'opera).

G2 esegue quattro **verifiche di una singola sezione rettangolare** con fibratura parallela all'asse dell'asta:

| ID regola | Sollecitazioni locali N (kN), V (kN), M (kNm) | Formulazione |
|---|---|---|
| `NTC2018-TIMBER-RECT-PURE-TENSION-G2` | N>0; V=M=0 | `sigma_t=N*1000/A_net <= ft,0,d` |
| `NTC2018-TIMBER-RECT-PURE-COMPRESSION-G2` | N<0; V=M=0 | `sigma_c=abs(N)*1000/A_gross <= fc,0,d` |
| `NTC2018-TIMBER-RECT-PURE-BENDING-G2` | N=V=0; M!=0 | `sigma_m=abs(M)*1e6/W_el <= fm,d`, `W_el=b*h^2/6` |
| `NTC2018-TIMBER-RECT-PURE-SHEAR-G2` | N=M=0; V!=0 | `tau=1.5*abs(V)*1000/(kcr*b*h) <= fv,d` |

`fd=kmod*fk/gamma_M`. `b`, `h` in **mm**, `A` in **mm²**, `W_el` in **mm³**, tensioni in **MPa**. Per trazione e flessione **kh=1** (nessun aumento favorevole): decisione conservativa esplicita. La prova della **sezione netta** è richiesta per la trazione, anche senza fori (`A_net=A_gross` solo se documentato). Compressione, flessione e taglio richiedono conferma della sezione lorda intatta; non trattano fori, intagli o riduzioni dell'area resistente. Il `kcr` deve provenire dalla selezione G1 e, quando previsto dalla Circolare, corrispondere a `2.0/fv,k` (massiccio) o `2.5/fv,k` (lamellare), con `fv,k` numericamente in MPa.

**La flessione NTC §4.4.8.1.6 impone in generale due disuguaglianze biaxiali**: per l'unica componente forte qui contemplata, quella principale è governante; la verifica dell'asse debole è fuori campo. **La compressione richiede anche una verifica di stabilità**, non trattata da G2. In presenza di N/M o V/M simultanei, ogni regola pura restituisce `NotApplicable`, non `Passed`. Questo non afferma che il taglio debba essere combinato per forza mediante una formula generica: significa soltanto che G2 non ha ancora un controllo completo dei risultati concomitanti.

## 2. Architettura e provenienza

- Nuovo `Ntc2018TimberRectangularSectionRuleG2 : IDesignCheckRule` nel progetto esistente `Verification.Timber`.
- Prerequisito **reale**: `Ntc2018TimberCoefficientSelectorG1.Select(...)`; il suo risultato individua norma NTC, gamma A/B/eccezionale, classe di servizio, durata, `kmod`, `kcr` e fingerprint del manifest.
- Il costruttore G2 richiede anche le tabelle NTC originali, così può **rivalidare numericamente** i coefficienti, anche se il record G1 ricevuto è stato alterato.
- L'oggetto `Ntc2018TimberSectionEvidenceG2` documenta geometria, orientamento della fibratura, corrispondenza asse forte, sezione netta e taglio. Tali prove sono **asserzioni esterne**; il programma non ispeziona certificati reali.
- I risultati FEM devono conservare `MemberId`, `TargetKind=LoadCombination`, `SectionSide`, `AnalysisId`, `CombinationId` e N/V/M **concomitanti** per la stessa ascissa.
- Risultati e coefficienti con norme, identificativi, dati, coefficienti, fonti o combinazioni incompatibili vengono rifiutati; non esiste fallback EC5.
- Il valore `DesignOverallStatus.Passed` del motore significa **solo** che le regole richieste hanno dato esito positivo. Non certifica la completezza dell'asta.

## 3. Esempio di riferimento C24

Rettangolo `b=100 mm, h=200 mm`, classe 1, durata media, fondamentali STR colonna A, `kmod=0.8`, `gamma_M=1.5`, `kcr=2/4=0.5`.

| Singola azione (non concomitante con altre) | Domanda (MPa) | Resistenza (MPa) | Utilizzo | Esito locale |
|---|---:|---:|---:|---|
| Trazione `N=+100 kN`, area netta 20 000 mm² | 5.000000 | 7.733333 | 64.6552% | Superata |
| Compressione `N=-100 kN` | 5.000000 | 11.200000 | 44.6429% | Superata, **senza** instabilità |
| Flessione `M=6 kNm` | 9.000000 | 12.800000 | 70.3125% | Superata, **senza** svergolamento |
| Taglio `V=10 kN` | 1.500000 | 2.133333 | 70.3125% | Superata |
| Taglio `V=20 kN` | 3.000000 | 2.133333 | 140.6250% | **Non soddisfatta** |

NB: per `A_net=16 000 mm²`, trazione `N=100 kN`, la domanda aumenta a `6.25 MPa`. Tutti i valori sono verificabili tramite aritmetica elementare indipendente dal FEM e dai nuovi algoritmi.

## 4. Test, limiti e gate

Nuovi test: `tests/StructuralSolver2D.Verification.Tests/M92/Ntc2018TimberRectangularSectionG2Tests.cs`.

Coprire: quattro azioni pure; taglio oltre resistenza; sezione netta; C24/GL24h; colonna A/B; situazione eccezionale; N/V/M concomitanti e segni; no kh; `kcr` mancante; fingerprint e fattori alterati; combinazione, asta, sezione e lato FEM errati; regressione EC5; `DesignCheckEngine` senza regole; disambiguazione `Passed` locale vs conformità dell'asta.

**Esclusioni**: N-M, V-M, presso/tensoflessione, flessione asse debole, instabilità fuori piano e di colonna, perpendicolare alla fibratura, appoggi, giunzioni, intagli, sezione variabile, carichi eccentrici, torsione, SLE, incendio, materiali non elencati, intero processo di combinazione NTC e attestazione di conformità complessiva.

Dalla radice del progetto su Windows PowerShell con .NET 8:

```powershell
.\scripts\verify-g2-manifest.ps1
.\scripts\run-g2-tests.ps1
```

Se i test falliscono, fornire il log completo; **il gate non si considera superato** fino alla conferma reale sul sistema Windows. Il motore FEM e i moduli EC5/G1 già validati rimangono invariati a livello di sorgente C#.
