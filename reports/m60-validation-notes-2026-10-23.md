# M60 — controlli preliminari del pacchetto

Data: 23 ottobre 2026. Baseline: `StructuralSolver2D_M591_AUDIT_FIX1_2026-10-23_FULL.zip`; la suite M59.1 è stata dichiarata superata dall'utente.

## Riscontri disponibili senza .NET 8

- Verificato il parsing dei 79 file JSON distribuiti, compreso l'esempio `m60-seismic-cantilever.json`.
- Verificata la presenza dei nuovi contratti, della API, della CLI, dei report, di test focalizzati e dello script Windows.
- Controllati il bilanciamento lessicale di delimitatori nei nuovi sorgenti C# (controllo statico limitato, **non compilazione**).
- Ricostruzione indipendente via SciPy del caso di una barra assiale 2D con due nodi e massa consistente: massa al suolo `0.15703629679860096 kN s²/m`, quota massa efficace `75%`, prima frequenza `712.8126295821168 Hz`; Sa=2.5 m/s² => spostamento pseudo-spettrale all'estremità libera `1.8694797237928684e-7 m`.
- Verifica segno accelerazione del terreno: ag(0)=+1.7 m/s² -> a_rel della punta = -2.55 m/s², a_abs = -0.85 m/s².
- Ricostruzione SciPy per barra a due campate e due modi axiali: SRSS spostamento di punta `7.708062366148531e-7 m` con Sa uniforme 2.5 m/s². Reference inserito come fixture indipendente nei test xUnit.

## Limiti e gate aperti

**Non è disponibile .NET 8 in questo ambiente:** compilazione, esecuzione xUnit, end-to-end CLI e regressione globale **non effettuate**. `scripts/run-m60-tests.ps1` esegue queste verifiche su Windows in una cartella pulita. I controlli numerici in SciPy non sono un sostituto della suite C#.

M60 è un'estensione esplorativa non normativa. Nessun calcolo di capacità, vulnerabilità, resistenza o conformità antisismica; CQC non implementata. La massa partecipante è divisa per `rᵀ M r` del sistema globale prima dei vincoli, perciò può essere <100% anche con la base ammissibile completa. Le ordinate di spettro sono pseudo-accelerazioni **fornite dall'utente**, senza interpolazione fuori dominio né correzioni automatiche di smorzamento. Gli inviluppi SRSS non sono simultanei. L'accelerogramma richiede verifica di sensibilità a campionamento e passo Newmark.
