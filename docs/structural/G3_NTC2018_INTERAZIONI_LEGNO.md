# G3 — NTC 2018, interazioni N–M e taglio concomitante (solo sezione rettangolare)

Data: 24/10/2026. Baseline: **G2 GATE PASSED**, confermato dall'utente. Questo documento specifica il nuovo calcolo e i limiti. Nessuna modifica al motore FEM, al profilo G1, alle verifiche pure G2 o alle regole EC5 precedenti.

## Riferimenti normativi

- **NTC 2018 (D.M. 17 gennaio 2018)**, §§4.4.6, 4.4.8.1.6, 4.4.8.1.7, 4.4.8.1.8, 4.4.8.1.9. Equazioni [4.4.5a,b], [4.4.6a,b], [4.4.7a,b], [4.4.8].
- **Circolare 21 gennaio 2019 n.7**, §C4.4.8.1.9: riduzione della larghezza di taglio `kcr` secondo famiglia del materiale e resistenza caratteristica (MPa).
- **G1**: selezione distinta NTC di `gamma_M` colonna A/B e `kmod` per singola combinazione; **G2**: resistenze locali pure, utilizzate come regressione, non come implementazione delle interazioni.

## Ambito di G3

Solo aste di legno massiccio C16/C24/C30 o lamellare GL24h/GL28h già presenti nel catalogo M88-C, sezioni **rettangolari integre e prismatiche**, asse longitudinale parallelo alle fibre e flessione solo sull'asse forte. Per l'asse debole fuori piano, momento **My=0 deve essere attestato esternamente**; il FEM 2D non fornisce questa attestazione. L'analisi deve essere elastico-lineare valida e le forze N, V, M **firmate e concomitanti nella stessa combinazione e nella stessa sezione (coordinate esatte e lato del salto entrambi attestati esternamente)**. Per la tensoflessione A_net=A_lorda certificato, senza fori, eccentricità o intagli.

### Relazioni

Definire `A=b*h` e `W=b*h²/6`; `σ_m=|M|/W`, `σ_N=|N|/A` (unità coerenti). Le resistenze di progetto sono `f_d=kmod*f_k/gamma_M`, con `kh=1` deliberatamente conservativo e documentato.

Per un'asta rettangolare `km=0,7`. Con solo momento forte `My=0`, entrambe le disuguaglianze normate restano nel calcolo:

- **Tensoflessione** (N>0, Mz≠0): `u_a=σ_t/f_t,0,d + σ_m/f_m,d` e `u_b=σ_t/f_t,0,d + km*σ_m/f_m,d`; rif. [4.4.6a,b].
- **Pressoflessione** (N<0, Mz≠0): `u_a=(σ_c/f_c,0,d)^2 + σ_m/f_m,d` e `u_b=(σ_c/f_c,0,d)^2 + km*σ_m/f_m,d`; rif. [4.4.7a,b]. **NON** sostituire il quadrato con un termine lineare.
- **Flessione con taglio** (N=0, Mz≠0, V≠0): `u_a=σ_m/f_m,d`, `u_b=km*σ_m/f_m,d`; rif. [4.4.5a,b]. Taglio verificato separatamente.
- Se **V≠0**, deve essere verificato anche `u_v=[1,5*|V|/(kcr*A)]/f_v,d` secondo [4.4.8], con `kcr` G1/Circolare documentato; l'indicatore numerico locale è `max(u_a,u_b,u_v)`.

**Non esiste qui una formula nuova di interazione N–V–M**: massimo significa solamente controllo congiunto dello stato (si conservano in traccia tutte le disuguaglianze). Le azioni pure restano G2. Le azioni con N+V ma senza M, e le sollecitazioni fuori dal perimetro, restano da gestire in moduli successivi.

## Benchmark indipendenti C24, b=100 mm, h=200 mm

Classe di servizio 1, durata media, NTC colonna A, `kmod=0,80`, `gamma_M=1,50`, `kcr=0,50`, `km=0,70`, `kh=1`. Resistenze `ft,0,d=7.7333333333 MPa`, `fc,0,d=11.2000000000 MPa`, `fm,d=12.8000000000 MPa`, `fv,d=2.1333333333 MPa`.

| Caso | N (kN) | V (kN) | Mz (kNm) | u_NM/flex | u_taglio | u governante |
|---|---:|---:|---:|---:|---:|---:|
| TensionBending | 20 | 0 | 3 | 0.48087284 | 0.00000000 | 0.48087284 |
| CompressionBending | -20 | 0 | 3 | 0.35953444 | 0.00000000 | 0.35953444 |
| BendingAndShear | 0 | 10 | 3 | 0.35156250 | 0.70312500 | 0.70312500 |
| TensionBending | 20 | 10 | 3 | 0.48087284 | 0.70312500 | 0.70312500 |
| CompressionBending | -20 | 10 | 3 | 0.35953444 | 0.70312500 | 0.70312500 |
| TensionBending | 20 | 20 | 3 | 0.48087284 | 1.40625000 | 1.40625000 |
| BendingAndShear | 0 | 10 | 12 | 1.40625000 | 0.70312500 | 1.40625000 |

`G3_NTC2018_BENCHMARKS.json` conserva gli input e tutti i rapporti (anche le due equazioni) per future verifiche incrociate.

## Obblighi e rifiuti fail-closed

1. Niente combinazioni costruite automaticamente: G1 richiede fingerprint e manifest documentato. G3 ricontrolla fingerprint, durata e tabelle normative.
2. No applicazioni EC5/NTC scambiate, né coefficienti di un'altra combinazione/asta/sezione.
3. Niente buchi, incastri o intagli con un'area netta ipotizzata; per N+M serve sezione integra e nessun momento secondario.
4. Manca My o non è comprovato My=0? Risposta **MissingData**, non flessione biassiale dichiarata assente.
5. Assenza di coefficienti kcr certificati, di una prova riferita a coordinata e lato del salto esatti, di una componente N/V/M (anche se zero) o d'origine FEM? Non calcolare esiti positivi.
6. La verifica è **locale**; non copre svergolamento della trave, instabilità a compressione della colonna, effetti del 2° ordine, appoggi, collegamenti, verifiche perpendicolari alle fibre, durabilità, incendi, deformazioni a SLE o copertura integrale delle combinazioni.
7. Il `DesignCheckReport.OverallStatus=Passed` significa che hanno superato le **regole richieste** a quella sezione; **non** significa conformità dell'asta o dell'opera.

## Prossimo incremento

**G4** — stabilità delle colonne compresse e travi inflesse, con input esterni verificati di lunghezze di libera inflessione, ritegni torsionali e proprietà al frattile 5%. Preceduto dal gate Windows G3.
