# Visione di prodotto — StructuralSolver2D

**Aggiornata al 24/10/2026 (G7 gate superato).** La pianificazione operativa aggiornata è in [roadmap.md](roadmap.md); la versione precedente della visione con checkpoint storici è conservata in [archivio](../archive/vision-and-roadmap_PRE_G7.md).

## Missione

Un risolutore strutturale FEM **2D**, open source e modulare, rivolto a studenti, docenti e professionisti per studio e valutazioni preliminari di **travi, reticolari e telai in acciaio e legno**. Il software deve spiegare *che cosa ha calcolato*, *con quali ipotesi* e *che cosa non è stato verificato*.

## Confini

Inclusi elementi monodimensionali in modelli piani; strumenti di validazione; cataloghi comuni; verifiche **SLU/SLE** secondo profili normativi **distinti** (EC3/EC5/NTC 2018), con campi di applicazione espliciti. Esclusi FEM 3D, calcestruzzo armato, muratura, X-LAM, BIM, piastre, pareti e progettazione esecutiva dei collegamenti.

## Architettura e metodo

1. Motore matematico FEM autonomo dalla normativa e dalla GUI.
2. Regole di progetto indipendenti con prova d'identità (asta, profilo normativo, edizione, combinazione, sezione e provenienza del risultato).
3. Test numerici riproducibili, benchmark con calcoli indipendenti e audit della copertura.
4. Report chiari: `Passed` nel proprio campo, `Failed` per superamento, `Unsupported` per regola assente, `NotApplicable` solo con motivazione fisica verificabile.
5. **GUI C#/Avalonia esclusivamente dopo** la maturazione del motore, delle verifiche e della documentazione.

L'ultima baseline software collaudata è **G7**; nessuna certificazione generale di sicurezza delle opere è implicata. Fonte di verità per il prossimo lavoro: [roadmap corrente](roadmap.md) e [handoff](../../HANDOFF_NUOVA_CHAT.md).
