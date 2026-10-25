# Visione di prodotto — StructuralSolver2D

**Visione confermata il 25/10/2026 (checkpoint documentale P0-F0).** Lo stato software aggiornato è in [CURRENT_STATE](CURRENT_STATE.md), la pianificazione operativa in [roadmap.md](roadmap.md) e le [regole C#/separazione normativa](ENGINEERING_AND_VALIDATION_RULES.md) governano i prossimi incrementi. Una versione precedente è conservata in [archivio](../archive/vision-and-roadmap_PRE_G7.md).

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

L'ultimo gate .NET **confermato** è **P0-D2C2**, con test **PASS dichiarati dall'autrice**. F0 ha aggiornato solo la documentazione; F1.1-A introduce i primi confronti indipendenti G8B **in C#/.NET, ancora PENDING su Windows**. Nessuna certificazione generale di sicurezza delle opere è implicata; la GUI resta ultima.
