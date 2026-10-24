# G7 — Checklist di validazione e passaggi successivi

- [x] Congelata baseline G6 testata (immutati sorgenti C# precedenti).
- [x] Definita sorgente indipendente delle reazioni B13/B14, con segni e momento globale.
- [x] Aggiunti file modello FEM isostatico 2D, con unità coerenti.
- [x] Esteso il catalogo dei benchmark senza alterare B01–B12, T01–T03, F01–F04, M01–M02.
- [x] Predisposti test xUnit specifici su reazione negativa e pattern di carico.
- [x] Predisposti test di pipeline G1→G3→G6 e G1→G2→G6 con regole reali.
- [x] Registrati limiti fisici e documentali, compresa la natura *bilatera* del vincolo A.
- [x] Predisposti manuale e script di gate.
- [x] Compilazione Release .NET 8 Windows e suite cumulativa — G7 GATE PASSED confermato dall’utente il 24/10/2026.
- [x] Gate G7 superato sul Windows dell’utente; nessun fallimento G7 comunicato. I golden restano invariati.
- [x] Conferma esplicita `G7 GATE PASSED` ricevuta il 24/10/2026.

## Successivi incrementi NON compresi nel gate G7

**P1 — fondazioni di affidabilità ancora scoperte:** generazione e verifica completa combinazioni NTC; stazioni critiche da certificare; verifica delle ipotesi di modello; connessioni e appoggi reali; validazione fuori piano, vulnerabilità Euler–Bernoulli a taglio, verifica di fessurazioni/intagli/fori e effetti di rigidezza.

**P2 — miglioramenti numerici da progettare e validare:** elemento trave Timoshenko 2D opzionale, monolateralità di contatto/tension-only se mantenuta nello scope, benchmark di instabilità/svergolamento non basati solo su dati esterni, affidabilità del calcolo del secondo ordine per le classi di struttura definite.

**P3 — report e GUI:** solo dopo la delimitazione e validazione del dominio del motore e delle norme; GUI Avalonia resta fase finale.
