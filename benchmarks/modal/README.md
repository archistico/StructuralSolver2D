# M56 — Benchmarks modali analitici e convergenza (gate interno)

**Stato:** M56 corretto dopo la prima esecuzione utente (27 test superati, 1 test fallito per aliasing nel MAC nodale della trave a 4 elementi). Correzione M56-FIX1 in attesa di nuova build e test .NET 8 dell'utente. Non è un'abilitazione di calcolo sismico o una certificazione del software.

## Origine e indipendenza dei riferimenti

La formulazione fisica segue le nozioni su modi propri, travi vibranti e telai nei capitoli 1, 2 e 4 di Alberto Carpinteri, *Dinamica delle strutture* (edizione Esculapio, 2023; lezioni originali precedenti). Le formule qui sotto sono esplicitate come **soluzioni analitiche della teoria classica**, non come output del solver né come riproduzione di esempi specifici del volume.

Tutte le frequenze sono in Hz, `ω = 2πf`. Ogni caso è **elastico lineare**, con deformazioni piccole, sezione uniforme, massa distribuita e elemento trave di Euler–Bernoulli (nessuna deformazione a taglio Timoshenko, nessuna inerzia rotazionale della sezione indipendente).

Costanti comuni: `L = 4 m`, `E = 210 000 000 kN/m²`, `A = 0,02 m²`, `I = 0,0001 m⁴`, `γ = 77 kN/m³`, `g₀ = 9,80665 m/s²`. Densità lineare inerziale `μ = γ A/g₀ = 0,157036296799 kN·s²/m²`. Dati `dynamics.nodalMasses = []`; nessuna massa aggiuntiva.

### Riferimenti continui indipendenti

- **Trave semplicemente appoggiata (Frame2D):** `ω_n = (nπ/L)² sqrt(EI/μ)`; spostamento trasversale `y_n(x) ∝ sin(nπx/L)`. Ai nodi: cerniera all'origine (`Ux=Uy=0`) e carrello all'estremo (`Uy=0`), rotazioni libere.
- **Mensola (Frame2D):** `ω_n = (β_n²/L²) sqrt(EI/μ)`, con `cos β_n cosh β_n = −1`; `β = [1,875104068711961; 4,694091132974175; 7,854757438237613]`. Forma `y_n(x) ∝ cosh(βx/L) − cos(βx/L) − η[sinh(βx/L) − sin(βx/L)]`, `η=(cosh β + cos β)/(sinh β + sin β)`. Incastro all'origine.
- **Asta assiale bicerniera, bloccata agli estremi in direzione assiale (Truss2D):** `ω_n = (nπ/L) sqrt(EA/μ)`, `u_n(x) ∝ sin(nπx/L)`. Per isolare il fenomeno assiale tutti i `Uy` sono vincolati; `Ux` di entrambi gli estremi fissato. Gli `Rz` non appartengono all'asta reticolare e sono eliminati solo se realmente inerti/uncoupled dalla procedura M54.
- **Shear type a due piani:** `K = [[2k,-k],[-k,k]]`, `M = diag(m,m)`; `ω² = [(3−√5)/2,(3+√5)/2] k/m`, `φ₂/φ₁ = 2 − λ m/k`, con `k=5600`, `m=3,25` in unità interne coerenti. È un caso a matrice assegnata, non una ricostruzione non verificata del caso numerico del libro.

## Confronti analitici per le prime tre frequenze

| Famiglia | Prima frequenza | Seconda | Terza |
| --- | ---: | ---: | ---: |
| Trave appoggiata | 35,901245794 | 143,604983177 | 323,111212147 |
| Mensola | 12,789704961 | 80,151712821 | 224,427116778 |
| Asta assiale bloccata agli estremi | 646,449547177 | 1292,899094354 | 1939,348641531 |

I dati JSON sotto `expected/` sono generati **soltanto dalle formule continue** dal programma standard-library `scripts/m56-reference-data.py`; non si auto-validano con numeri prodotti dal solver C#.

## Mesh, selezione dei modi e convergenza

Le prove C# costruiscono la catena reale **M53 assemblaggio → M54 condizioni al contorno → M55 autovalori**. Usano 1/2/4/8/16 elementi per le travi, 2/4/8/16 per l'asta assiale. L'asta con un solo elemento ed entrambe le estremità assialmente vincolate non ha gradi di libertà assiali liberi e **non è un benchmark modale valido**.

**Attenzione alla mescolanza delle famiglie:** una Frame2D include modi **assiali e flessionali**; il terzo autovalore globale **non è sempre** il terzo modo flessionale. Inoltre la sola MAC degli spostamenti trasversali *ai nodi* può soffrire di **aliasing spaziale**: con quattro elementi, i modi di flessione 3 e 5 hanno forme nodali equivalenti (a segno vicino), pur essendo differenti dentro ciascun elemento. La selezione basata solo sul MAC nodale può dunque scegliere un modo ad alta frequenza sbagliato.

**Correzione M56-FIX1:** nei test della trave appoggiata, selezionare il modo con una MAC di forma ottenuta campionando l'**interpolazione cubica di Hermite** di `Uy` e `Rz` ai punti interni di ogni elemento (`x/L` locale 1/4, 1/2, 3/4). Escludere gli autovettori prevalentemente assiali confrontando le energie quadratiche campionate di `Ux` e della deformata trasversale. La MAC resta adimensionale e invariante per segno e scala; **non** è una MAC pesata per la massa. Gli altri benchmark assiali/mensola mantengono la MAC nodale dove appropriata.

**Caso riproducibile N=4 (risultati FEM indipendenti SciPy, non C#):** `f₃,teorica=323,111212 Hz`, terzo modo flessionale FEM `329,015270 Hz` (`+1,827252%`, tolleranza originale 2%), modo flessionale superiore confondibile ai nodi `1013,397282 Hz`. La MAC interpolata rispetto a `sin(3πx/L)` vale circa **0,99927** per il modo giusto contro **0,12851** per quello superiore; un modo assiale vicino a 325,305 Hz è escluso dall'indicatore di prevalenza assiale. Il test `FourElementPinnedFrame_InterpolatedShapesRejectHighModeNodalAliasing` impedisce la regressione. Nessuna tolleranza numerica è stata aumentata.

### Stime FEM effettuate con implementazione indipendente Python/SciPy

**Queste stime NON sono risultati del codice C#**. Sono un calcolo separato con matrici consistent-mass 1D e `scipy.linalg.eigh`, riproducibile mediante `python scripts/m56-independent-fem.py`. Non sono condizioni di accettazione basate sul confronto del solver con sé stesso: i test C# usano le **formule continue** e i propri limiti documentati. Per `n=1` esistono soltanto due modi di flessione per l'elemento di trave; non si deve confondere l'eventuale modo assiale con un terzo modo flessionale.

Independent SciPy reference; values below are NOT C# solver results.
μ = 0.157036296799 kN·s²/m²

pinned — continuous analytical Hz: 35.901245794, 143.604983177, 323.111212147
| elements | first Hz | first error (%) | second Hz | second error (%) | third Hz | third error (%) |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 39.847437374 | +10.991796 | 182.603898017 | +27.157076 | — | — |
| 2 | 36.042944311 | +0.394690 | 159.389749497 | +10.991796 | 400.639240945 | +23.994224 |
| 4 | 35.910567992 | +0.025966 | 144.171777242 | +0.394690 | 329.015269557 | +1.827252 |
| 8 | 35.901836100 | +0.001644 | 143.642271969 | +0.025966 | 323.526919409 | +0.128658 |
| 16 | 35.901282810 | +0.000103 | 143.607344402 | +0.001644 | 323.137961320 | +0.008279 |

cantilever — continuous analytical Hz: 12.789704961, 80.151712821, 224.427116778
| elements | first Hz | first error (%) | second Hz | second error (%) | third Hz | third error (%) |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 12.850511357 | +0.475432 | 126.612047862 | +57.965492 | — | — |
| 2 | 12.795887982 | +0.048344 | 80.831873759 | +0.848592 | 273.388152393 | +21.816007 |
| 4 | 12.790123288 | +0.003271 | 80.245106156 | +0.116521 | 226.164698005 | +0.774230 |
| 8 | 12.789731602 | +0.000208 | 80.158121664 | +0.007996 | 224.563622563 | +0.060824 |
| 16 | 12.789706632 | +0.000013 | 80.152122224 | +0.000511 | 224.436038603 | +0.003975 |

axial-fixed-fixed — continuous analytical Hz: 646.449547177, 1292.899094354, 1939.348641531
| elements | first Hz | first error (%) | second Hz | second error (%) | third Hz | third error (%) |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | — | — | — | — | — | — |
| 2 | 712.812629582 | +10.265779 | — | — | — | — |
| 4 | 663.166140891 | +2.585908 | 1425.625259164 | +10.265779 | 2316.697843248 | +19.457523 |
| 8 | 650.610938847 | +0.643730 | 1326.332281781 | +2.585908 | 2052.443303016 | +5.831580 |
| 16 | 647.488485800 | +0.160715 | 1301.221877694 | +0.643730 | 1967.494505117 | +1.451305 |

## Gate di accettazione e limiti

1. Errori relativi della prima frequenza entro le tolleranze specifiche della mesh espresse nei `[InlineData]` dei test; l'errore deve ridursi nei raffinamenti selezionati (non si assume come teorema la monotonicità di ogni modo su qualsiasi mesh).
2. Prime tre forme flessionali/assiali su mesh fini con `MAC > 0,995` (`MAC > 0,99` per le aste). I primi tre **modi flessionali** non coincidono necessariamente con i primi tre **autovalori globali**.
3. Controllo numerico con `relativeResidual < 1e-7`, ortogonalità in massa e rigidezza < `1e-7`, nelle metriche definite internamente da M55. Verifica del quoziente di Rayleigh nella suite preesistente M55.
4. Invarianza delle frequenze sotto traslazione/rotazione, inversione dell'orientamento degli elementi e leggi di scala di `E`, `γ` e `L` per la famiglia fisica considerata.
5. Errori espliciti per `UnitWeight` assente, struttura free-free non supportata, rilascio di momento non trattato dinamicamente e rigidezza flessionale non valida.
6. `dotnet build` dell'intera soluzione, esecuzione del filtro M56, dei test M52–M55 e di **tutte** le regressioni .NET 8 senza anomalie. Non modificare il core per «far tornare i numeri» senza prima comprendere la discrepanza e documentarla.

**Non rientrano in M56:** elementi Timoshenko, libera vibrazione free-free, release dinamici, modelli massless con condensazione, verifica sismica, grandi deformazioni, damping, transiente, convergenza garantita per geometrie generiche, garanzia normativa/sicurezza di progetto. Il calcolo rimane **interno** fino a M57.

### Come riprodurre

```powershell
.\scripts\run-m56-tests.ps1
```

```bash
python scripts/m56-reference-data.py --check
python scripts/m56-independent-fem.py   # facoltativo, richiede numpy/scipy
```

I file `models/*.json` sono fixture geometriche documentali. I test C# ricostruiscono in memoria geometrie e vincoli equivalenti: questo gate non abilita un comando CLI `modal` e non esegue analisi modali partendo direttamente dai file JSON. Tale integrazione appartiene a M57.
