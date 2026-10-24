# M67 — Reazioni dinamiche e azioni elastiche di estremità

**Data:** 2026-10-23  
**Stato:** implementato, da validare con .NET 8 / xUnit sul computer dell'utente.  
**Base:** M66 — test C# validati dall'utente.  
**Perimetro:** analisi lineare transitoria M59 e aggregazione interna degli stati non lineari M65.

## Perché è necessario

M59 restituisce spostamenti, velocità, accelerazioni e forze nodali per istante. Questi valori non bastano a dedurre le reazioni con la formula statica `K*u-F`: durante il moto i nodi trasmettono anche contributi di inerzia e smorzamento. Inoltre un elemento Frame2D con massa distribuita può sviluppare diagrammi di taglio e momento che non si ricavano dai soli sei contributi elastici nodali.

## Implementazione lineare

`StructuralDynamicForceRecoveryService.Analyze(request)` riesegue la stessa analisi `M59`, costruisce la coppia di matrici globali consistenti `K,M` del modello e, a ogni campione, valuta:

`R(t) = M*a(t) + C*v(t) + K*u(t) - F(t)`.

`R` è il **risultante globale al nodo** delle forze vincolari. Non rappresenta la quota assegnata a ogni singolo vincolo in presenza di supporti ridondanti o coincidenti. Nei modelli a vincoli inclinati il risultante è espresso negli assi globali, mentre il test di equilibrio è svolto sulle coordinate libere `ZᵀR`.

Lo smorzamento di Rayleigh usa `C = alpha*M + beta*K` sulle matrici fisiche non ridotte. Per il rapporto modale uniforme viene definita un'estensione esplicita completa `C = M*Phi*diag(2*zeta*omega)*Phiᵀ*M`; le componenti di smorzamento trasmesse ai supporti in questa seconda convenzione sono **dipendenti dall'estensione scelta** e non sono una proprietà osservabile univocamente definita dai soli rapporti modali.

Per ogni Frame2D privo di release dinamiche, `f_local_elastic = K_local*T*u_element`; gli elementi Truss2D usano `N = EA/L*(u_B-u_A)·e`, con azioni locali `[-N,+N]`. Le quantità **NON** includono la correzione distribuita di inerzia o smorzamento per ricostruire `N(x,t),V(x,t),M(x,t)`. Non utilizzarle come diagrammi strutturali dinamici o valori di verifica delle sezioni.

Gli inviluppi sono massimi dei valori assoluti sui campioni di tempo, con tempo della prima occorrenza. I massimi non sono generalmente simultanei né garantiscono il massimo tra campioni.

### API e CLI

- `StructuralSolver2DService.AnalyzeTimeHistoryForces(request)` restituisce la risposta M59 originale, le reazioni, le azioni elastiche di estremità, gli inviluppi e il residuo libero massimo.
- `time-forces <file.json>` visualizza il report Markdown.
- `time-forces-report <file.json> <output.md>` esporta il report.
- `time-forces-csv <file.json> <cartella>` genera `reactions.csv` ed `elastic-member-ends.csv` con formattazione invariant.

Non viene modificato il contratto pubblico preesistente di M59, né il comportamento della CLI statica.

## Modelli non lineari

Il solver **interno** M65 ha già calcolato, per ogni passo accettato, `Reactions`, `AxialForces`, `MaterialStates` e `CumulativePlasticDissipation` dalla legge elasto-plastica effettiva. `NonlinearTruss2DForceEnvelopesM67.FromAcceptedSteps` aggrega queste informazioni senza rieseguire un modello elastico e senza includere passi di Newton rifiutati. Non esiste ancora un'API JSON/CLI pubblica per i risultati non lineari: sarà oggetto di M68.

## Benchmark C#/xUnit

1. Mensola orizzontale con carico statico applicato come stato iniziale esatto: reazione verticale, momento all'incastro, azioni elastiche di estremità e accelerazione iniziale nulla.
2. Transitorio con massa consistente, smorzamento nullo o Rayleigh: equilibrio nelle coordinate ammissibili.
3. Modello con smorzamento modale uniforme: estensione dichiarata, residuo coerente.
4. Reticolare con una sola asta: sforzi di estremità opposti e reazione assiale corretta.
5. Forza applicata su DOF vincolato: nessuna falsa risposta libera, reazione con segno opposto.
6. Fine asta con release non supportata: errore esplicito della pipeline M53/M59.
7. Storia elasto-plastica M65: inviluppo dagli stati accettati e rollback degli stati scartati.
8. CLI Markdown/CSV end-to-end, nomenclatura e unità esplicite.

## Limitazioni aperte — non dichiarare M67 completo per questi punti

- Non sono disponibili diagrammi continui dinamici `N(x,t), V(x,t), M(x,t)` con integrazione delle forze d'inerzia distribuite e dei carichi viscosi per Frame2D.
- Non sono disponibili forze interne per travi con release di momento dinamiche; il modello viene rifiutato.
- Non esiste ripartizione univoca delle reazioni tra supporti ridondanti né validazione di sicurezza o normativa.
- Il post-processing non lineare è interno, limitato a forze assiali delle reticolari M65. Nessuna plasticità di Frame2D o ricostruzione del taglio dinamico non lineare.
- La nuova suite xUnit **deve ancora essere compilata ed eseguita su Windows .NET 8**.

**Prossimo passo:** chiudere l'accettazione numerica M67, poi avviare M68 opt-in API/JSON/CLI per le reticolari non lineari; la ricostruzione di diagrammi dinamici distribuiti resta una sottofase separata.
