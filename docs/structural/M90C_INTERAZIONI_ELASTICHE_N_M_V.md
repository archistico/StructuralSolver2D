# M90-C — interazioni elastiche N–M–V per sezioni I/H classe 3

**Stato:** codice + test aggiunti; compilazione e gate Windows ancora da confermare. Nessun solver FEM modificato.

## Perimetro

- EN 1993-1-1:2005+A1:2014, **§6.2.9.2(1) equazione (6.42)** (interazione elastica N–M in classe 3) e **§6.2.10(2)** (nessuna riduzione della resistenza a flessione/assiale quando il taglio non supera metà della resistenza plastica, a condizione che l'instabilità a taglio non governi). Capacità a taglio da §6.2.6(2).
- Profili **IPE, HEA, HEB ad ali parallele**, flessione nel piano attorno all'asse forte, una sola stazione e un solo caso/combinazione FEM; esclusi INP/IPN ad ali inclinate.
- Classe **3 esplicitamente motivata** dall'utilizzatore; il controllo elastico M90-B deve inoltre indicare che, in modo conservativo, l'anima non richiede la classe 4. Questo secondo controllo può rifiutare conservativamente casi in realtà ammissibili; non può attribuire classe 1 o 2.
- Acciaio S235/S275/S355 da M88-C, con snervamento dipendente dallo spessore e proprietà **tabellate** dei profili OPPO M88-B2. Le tabelle sono **indicative**, non certificati dei produttori.
- Senza mai dichiarare conformità completa di membratura/struttura.

## Formule, unità e condizione del taglio

Sezione bi-simmetrica, N (kN, trazione positiva), M (kNm) concomitanti:

```
σ_max,Ed = |N_Ed| * 10 / A[cm²] + |M_Ed| * 1000 / Wel,forte[cm³]   [MPa]
σ_Rd = fy[MPa] / γM0
η = σ_max,Ed / σ_Rd
```

Il valore assoluto somma i contributi alle fibre estreme ed è **esattamente** il massimo del modulo delle due tensioni longitudinali elastiche in una sezione doppiamente simmetrica. Non combina massimi ottenuti da posizioni o combinazioni differenti.

Quando V != 0 (segno conservato nel report), è indispensabile l'area resistente a taglio `Av`, documentata, l'orientamento confermato, l'assenza di torsione e la dichiarazione esterna che l'instabilità a taglio non governi:

```
Vpl,Rd[kN] = Av[cm²] * fy[MPa] / (10 * √3 * γM0)
|V_Ed| <= 0,5 * Vpl,Rd
```

**Sopra** questa soglia la regola restituisce `NotApplicable`: la riduzione della tensione di snervamento/area efficace al taglio prevista per le interazioni più gravose non è implementata. Se V=0 `Av` non serve, ma l'esito rimane parziale.

## Condizioni obbligatorie

- Edizione + Annesso Nazionale identificati, `γM0` esplicito e fonte tracciata (nessun valore automatico).
- Identificativo di asta/sezione/caso o combinazione, lato di carico concentrato e stazione coerenti; niente inviluppi misti.
- Classificazione classe 3 giustificata da evidenza esterna, oltre allo screening M90-B.
- Orientamento dell'asse forte, assenza di fori rilevanti, assenza di torsione documentati.
- Nel caso di taglio non nullo: `Av` positiva e non superiore ad A, sua provenienza, asse tagliante confermato ed esclusione dell'instabilità a taglio mediante verifiche esterne.
- Richiede sia N che M diversi da zero; le azioni pure restano trattate in M89-A/M89-B.

## Esempio aritmetico indipendente (non benchmark normativo integrale)

IPE 200, A=28,48 cm², Wel,forte=194,3 cm³, S235 con fy=235 MPa, γM0=1,00 *esplicitamente dichiarato per il test*, N=-100 kN, M=20 kNm, V=10 kN con Av=13,996 cm² dichiarata:

- σN = 100*10/28,48 = **35,1124 MPa**
- σM = 20*1000/194,3 = **102,9336 MPa**
- σmax ≈ **138,0460 MPa**
- η ≈ **0,58743**, entro il limite di snervamento elastico della sola sezione
- Vpl,Rd ≈ **189,89 kN**, quindi 10 kN è sotto 0,5*Vpl,Rd.

I valori devono essere confermati dalla suite xUnit Windows. Il libro di Claudio Bernuzzi (Hoepli, 2018) supporta l'inquadramento generale, ma questo specifico esempio numerico è un calcolo di test costruito separatamente e **non è spacciato per un esempio tratto dal libro**.

## Limitazioni e prossimi incrementi

- **Non** valuta: instabilità in piano o latero-torsionale; classi 1/2 plastiche e classe 4 efficace; carichi trasversali/local buckling e patch loading; forature e rottura netta; torsione; taglio elevato; collegamenti; imperfezioni del membro; resistenza normativa globale; applicazione automatica NTC2018 o EN 2022.
- Anche una singola riga `Passed` del motore verifiche non è conformità di struttura: occorre registrare tutte le regole dovute. Una regola mancante comporta esito complessivo `Incomplete`.
- M90-D/M91: verifiche con taglio elevato, interazioni plastiche e instabilità richiedono percorsi separati con benchmark validati.

## Fonti tecniche

- EN 1993-1-1:2005+A1:2014, §§6.2.6, 6.2.9.2, 6.2.10 (da verificare nel testo ufficiale adottato per il caso reale).
- Bernuzzi, *Progetto e verifica delle strutture in acciaio*, Hoepli 2018, capitoli 4, 8, 9 e 11. Volume di proprietà dell'utente: **non redistribuito** in questo pacchetto.
- Catalogo geometrico OPPO M88-B2 (dati indicativi, non certificati di prodotto).
