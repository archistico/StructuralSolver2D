# M76 — Peso proprio automatico nei modelli statici 2D

**Stato:** implementato, in attesa dei test Windows .NET 8. **M75** resta a sua volta da validare. Ultimo milestone di codice confermato dall'utente: **M73**.

## Scopo

Attivare esplicitamente il peso proprio degli elementi di un modello strutturale 2D, mantenendo l'integrità delle combinazioni, delle forze nodali equivalenti, dei diagrammi delle travi e del controllo d'equilibrio. M76 **non** introduce combinazioni normative, verifiche SLU/SLE o analisi dinamiche con gravità prestress.

## Contratto fisico, unità e segni

- Sistema globale: **+Y verso l'alto**; la gravità è **−GlobalY**.
- `materials[].unitWeight`: peso specifico **γ in kN/m³**, *non* densità di massa. M75 offre conversioni esplicite da kg/m³ e g in m/s².
- `sections[].area`: area **A in m²**.
- L'azione genera per ciascuna asta il carico lineare uniforme **qᵧ = −γ · A · scale**, in **kN/m**. Il peso totale è `W = γ A L · scale`, in kN.
- Il carico `SelfWeight.value` è un **moltiplicatore positivo adimensionale** (normalmente `1.0`). Non è un'intensità in kN/m. Una combinazione con coefficiente negativo può invertirne l'effetto, come già avviene per qualsiasi altro carico.
- Il carico è **opt-in** e va assegnato a un caso di carico esplicito. Nessun peso proprio viene applicato quando `SelfWeight` è assente. Non aggiungere manualmente lo stesso carico distribuito se vuoi evitarne la duplicazione fisica.
- Al massimo una azione `SelfWeight` per caso di carico (errore `LOAD_SELF_WEIGHT_DUPLICATE_CASE`); azioni in casi distinti possono essere combinate e vengono fattorizzate come i carichi ordinari.

### JSON minimo

```json
{
  "id": "SW_G",
  "loadCaseId": "G",
  "type": "SelfWeight",
  "targetType": "Model",
  "targetId": "",
  "direction": "GlobalY",
  "value": 1.0
}
```

Questo oggetto si inserisce nell'array `loads` di un modello completo con `unitWeight` su tutti i materiali utilizzati. Esempio eseguibile: [`fixtures/m76-self-weight-beam.json`](fixtures/m76-self-weight-beam.json).

### Rappresentazione per tipologia di elemento

- **Frame2D:** un carico uniforme **GlobalY** per ogni asta, trasformato nelle coordinate locali e condensato con gli svincoli come un normale carico distribuito. Contribuisce a forze equivalenti, reazioni, diagrammi N/V/M e visualizzazione. Un elemento inclinato riceve componenti locali assiale e trasversale.
- **Truss2D:** le funzioni di forma lineari forniscono **due forze globali verticali da `qᵧ·L/2`**, una a ogni estremità. Gli elementi continuano a resistere **solo assialmente**: le forze trasversali *non* creano una rigidezza flessionale inesistente. Il campo continuo interno del carico di volume lungo il truss non viene ricostruito; i risultati delle aste rimangono quelli del modello FEM assiale.
- **Misti:** Frame2D e Truss2D sono espansi nello stesso modello; i vincoli e la topologia restano invariati.

`SelfWeightStaticLoadExpander.Expand` crea una copia del modello con carichi statici equivalenti **senza mutare l'originale**, e restituisce il modello invariato quando non contiene `SelfWeight`. L'espansione non persiste nel JSON: l'input conserva la singola azione esplicita.

## Validazione e limiti

Il `StructuralModelValidator` verifica coefficiente positivo, tipo/direzione/target, presenza di `UnitWeight` valido su tutti i membri, finitezza del prodotto, e una sola azione per caso. Le unità rimangono quelle interne del progetto (kN, m). Il post-processing dei risultati con peso proprio usa la stessa espansione statica: verifiche d'equilibrio, campionamento diagrammi e viewer. Non cambiano le matrici di massa e non viene generata automaticamente un'azione gravitazionale per le analisi armoniche, modali, sismiche o transitorie.

**Limite importante:** il nuovo validatore generale conosce il tipo `SelfWeight`, ma l'interpretazione fisica implementata in M76 vale soltanto per **l'analisi statica lineare** `Frame2DAnalyzer`, `Truss2DAnalyzer` e `PlaneStructureAnalyzer`. Non interpretare questo come supporto alle analisi non lineari o dinamiche.

## Benchmark indipendenti (C#/xUnit)

- Trave appoggiata `L=4m`, `γ=78.5kN/m³`, `A=0.005m²`: `q=-0.3925kN/m`, `R_A=R_B=0.785kN`, `M(L/2)=0.785kNm`.
- Confronto automatico/esplicito con asta Frame2D inclinata, in presenza e assenza dello svincolo di momento all'estremità.
- Asta verticale Truss2D: `F_A=F_B=-γAL/2`, `R_A=γAL`, `u_B=-γL²/(2E)` (soluzione della discretizzazione assiale con carico consistente).
- Telaio misto con controvento reticolare: carico globale `∑γAL`, equilibrio e confronto con il modello espanso.
- Combinazioni con fattori negativi, ripetizione del solver senza duplicazioni, convalida preventiva e round-trip M75 nel JSON.

**Gate Windows:** `scripts/run-m76-tests.ps1`: pulizia `bin/obj`, compilazione Release, test mirati e CLI reale, poi regressione completa della soluzione. I risultati **non sono ancora verificati in .NET 8** nell'ambiente di preparazione.

## Prossimo milestone M77

Vincoli elastici, molle nodali/rotazionali e cedimenti vincolari con benchmark di equilibrio, rigidezza e precisione (sottoposti a validazione prima del passaggio ai diagrammi/inviluppi M78–M79).
