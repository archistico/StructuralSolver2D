# P0-B1 — Golden indipendenti delle combinazioni

Questa cartella contiene solo **riferimenti per la futura implementazione**, non modelli FEM né combinazioni normative attualmente prodotte dal programma.

File:

- `P0B1_NTC2018_ACTION_GOLDEN.json`: tabella NTC 2018 §§ 2.5.3/2.6.1 e 14 casi **aritmetici manualmente derivati**, per un singolo effetto scalare in ciascuno scenario.
- `verify_p0b1_golden.py`: script Python standard library, che ricalcola i fattori e l'effetto con `Decimal` e verifica anche 10 controlli negativi intenzionalmente alterati.

Da root del progetto:

```shell
python3 benchmarks/independent/p0b/verify_p0b1_golden.py
```

Riferimento: `docs/structural/P0_B1_CONTRATTO_AZIONI_NTC2018.md`.

**Esiti al 25 ottobre 2026:** 14/14 casi positivi + 10/10 mutazioni respinte dall'esecutore Python; nessuna nuova esecuzione .NET. La doppia indipendenza da implementazione C# e solutore FEM **non** prova che tutti gli scenari di carico di un edificio siano stati individuati né la correttezza dei dati di progetto. Il confronto obbligatorio con generatore C# e suite G7 sarà sviluppato in P0-B2/B3/B4. Non aggiungere questi JSON dentro `examples/`: la CLI li scambierebbe per modelli strutturali.
