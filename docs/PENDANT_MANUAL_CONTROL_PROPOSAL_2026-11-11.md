# Pulsantiera Omron — proposta PLC prima dell'applicazione

Data: 2026-11-11. Stato: proposta offline da approvare; nessuna modifica applicata al PLC/HMI nativo, nessuna compilazione di questa proposta, download o attivazione.

## Evidenza e versioni

Il controllo nativo è documentato in `runs/2026-11-11-pendant-manual-review/conclusione.md` e nelle relative schermate. Il progetto salvato è `programmer_progetv19/schlenkers 36-10 190036-7-8v2.12.backup/Backup Schlenkers 36-10 190036-7-8v2.12 10-11/Backup Schlenkers 36-10 190036-7-8v2.12 10-11.ap19`.

I sorgenti `REV12/PLC_Sources` sono una seconda evidenza, NON una copia dimostrata identica al nativo. Nel nativo la chiamata Jog usa `Cmd.JogPB`; REV12 usa `EffectiveCmd.JogPB` filtrato da `AL102Process.RotationInhibited`. Prima dell'applicazione occorre esportare/confrontare i blocchi effettivi della copia salvata e riconciliare solo le differenze pertinenti. Non importare integralmente REV12.

## Blocchi e modifiche proposte

| Blocco/percorso | Riscontro | Modifica proposta |
|---|---|---|
| FB1 MainState, stato 20 | Ingresso negli stati altezza 60/70 prima del controllo MANUAL | Condizionare gli ingressi altezza alla selezione MANUAL valida. Preservare priorità sicurezza e gestione automatica. |
| FB1, stati 60 e 70 | Uscita solo alla revoca HeightEnable; manca controllo esplicito ManualSelect | Alla perdita MANUAL revocare subito le richieste pendant; uscire verso READY/POWER_UP secondo gli stessi prerequisiti già usati dallo stato 50. Non entrare automaticamente in produzione. |
| FB13 Application: acquisizione comandi | Riferimenti diretti nativi mostrano scrittura Startup, non una sorgente ciclica fisica dimostrata | Definire un adattatore ciclico separato con sorgenti verificate, validità e diagnostica. Nessun nuovo indirizzo fisico. Non considerare eventuali comandi HMI equivalenti a un abilitatore verificato. |
| FB13: EffectiveCmd | Differenza nativo/REV12 | Generare le richieste effettive mantenute dalla sorgente verificata e dal consenso MANUAL. Preservare filtro RotationInhibited e verificarne aggiornamento/validità. Nessun latch. |
| FB5 JogPendant | AND dei consensi già presente, ma parametri chiamante possono bypassarli | Usare consensi reali, richiesta effettiva, interblocco altezza e drive ready. Revoca immediata della richiesta software al rilascio, pressione completa, perdita MANUAL/validità/consensi. |
| FB5 tracking | RecoveryComplete può ripristinare TrackingValid nello stesso ciclo di JOG | Dare priorità all'invalidazione durante JOG; ripristinare soltanto con recupero valido e JOG inattivo. Verificare dipendenze homing/tracking prima dell'applicazione. |
| FB13 chiamate FB_HeightMode | Entrambe leggono Cmd.PendantUp/Down raw | Passare richieste effettive UP/DOWN; richiedere selettore MANUAL valido e stato altezza pertinente. Conservare SafetyOK, finecorsa, conferma sollevamento, fermo e mutua esclusione. |
| FB_HeightMode | Conflitto direzioni e finecorsa presenti; timeout è un'indicazione, non un taglio locale | Conservare le protezioni esistenti. Tracciare la reazione esterna a MovementTimeout e il feedback reale di fermo prima di dichiarare completa la protezione. Non inventare tempi o polarità. |
| FB13 + FB_OpenPoints | REV12 contiene bypass bench e successive riscritture dei comandi | Separare simulazione logica da pubblicazione fisica; eliminare dal percorso fisico le eccezioni di sicurezza bench. Verificare tutte le assegnazioni successive prima del gate finale. |
| DB/UDT parametri e diagnostica | JogSpeedPct non dimostra 3000 bph | Preparare richiesta in bph, validità conversione e motivo di inibizione. Nomi dei nuovi campi da definire esplicitamente nell'implementazione, senza spacciarli per tag già esistenti. |
| OB1/Startup | OB1 REV12 chiama DB_Application; Startup scrive comandi | Verificare ordine ciclico e inizializzazione a richieste revocate. Controllare anche assegnazioni aggregate/HMI, non solo cross-reference dei singoli campi. |

`Machine.ManualActive` vale solo nello stato 50: usarlo come unico consenso UP/DOWN bloccherebbe anche i legittimi stati 60/70. Il consenso comune deve derivare dalla selezione MANUAL verificata, mentre ogni funzione conserva il proprio stato operativo.

## Regole funzionali da tradurre in codice

Queste sono condizioni progettuali, non nuovi tag fisici né sorgente importabile:

1. Acquisire ad ogni ciclo selettore, abilitatore operativo intermedio, UP, DOWN e validità dalle sorgenti documentate. In assenza di sorgente verificata: richiesta fisica disabilitata e diagnostica esplicita.
2. MANUAL non valido/AUTO: tutte e tre le richieste effettive pendant FALSE nello stesso ciclo di elaborazione; nessuna memoria di JOG/UP/DOWN. Il tempo reale di arresto dipende dalla funzione di sicurezza separata e non è certificato dal ciclo PLC.
3. JOG: MANUAL valido, abilitazione intermedia mantenuta, SafetyOK reale, pendant valido/connesso, drive ready, assenza modo altezza, nessun inibitore AL102 e conversione velocità verificata. Nessuna autoritenuta.
4. UP/DOWN: MANUAL valido, abilitazione e segnali validi, stato altezza selezionato e tutti i consensi altezza. UP e DOWN contemporanei: nessuna direzione. Altezza e rotazione restano mutuamente escluse.
5. All'uscita da MANUAL nessun comando pendant deve essere recuperato da una successiva assegnazione raw o bench. Il percorso automatico mantiene la propria logica e non eredita richieste pendant; il passaggio ad AUTO non costituisce da solo START produzione.
6. Simulazione/bench: risultati solo logici, non autorizzazione di uscite fisiche. Non è sufficiente cambiare un singolo OR nella chiamata FB5.

## Riscontri critici nei sorgenti REV12 da riconciliare nel nativo

- `11_FB_Application.scl:250`: assegnazione incondizionata `SimulationMode := TRUE`; il commento che la limita a SMC non corrisponde a tutti i consumatori del flag.
- Chiamata Jog, circa 955–977: SafetyOK/PendantConnected e recupero sono influenzati dalla simulazione.
- Arbitraggio principale, circa 1250–1283: può riscrivere MainRun e riferimento velocità dopo FB5.
- BenchManualMainJogPermit, circa 1466–1474: rilegge Cmd.JogPB raw; `10O_FB_OpenPoints.scl:567–570` contiene un'eccezione all'inibizione movimento per perdita SafetyOK.
- Ulteriori override bench dopo OpenPoints, circa 1536–1568; preservare contenimento AL102 e verificare il percorso finale completo.
- Pubblicazione drive circa 1574–1659 e altezza 1668–1671: l'inibizione fisica deve coprire comandi e riferimenti effettivamente pubblicati. Le uscite assolute presenti in questi sorgenti non costituiscono verifica del cablaggio.
- Il percorso SMC ha anch'esso una pubblicazione fisica: non estendere una modifica pendant ad altri attuatori senza audit, ma non dichiarare isolamento globale della simulazione finché tale percorso non è verificato.

## Velocità

Richiesta approvata: **3000 bottiglie/ora**. Calcolo cinematico: `3000 / 36 / 60 = 1,388889 RPM` del carosello. Non è la velocità motore e non determina Hz o percentuale VSD. Non sostituire JogSpeedPct con 3000 o 1,389. Servono rapporto completo motore/carosello e scala drive verificati; fino ad allora la proposta non abilita JOG fisico con una conversione presunta. L'ammissibilità della velocità manuale protetta richiede verifica separata.

## Segnali fisici ancora da verificare

| Segnale | Dato disponibile | Verifica necessaria |
|---|---|---|
| Selettore MANUAL/AUTO | ManualSelect usato dalla logica | Ingresso/tag effettivo, polarità, validità e transizione reale; escludere scrittura HMI non controllata. |
| Abilitatore canale 1 | Contatti Omron 1–2; PILZ I12 atteso da documentazione precedente | Continuità nelle tre posizioni, fili/pin, test pulse, ingresso e diagnostica PILZ effettivi. Nessuna approvazione del circuito. |
| Abilitatore canale 2 | Contatti Omron 3–4; PILZ I13 atteso | Stesse prove, indipendenza/discrepanza e funzione di arresto prevista. |
| Abilitazione operativa intermedia | Deve generare richiesta JOG mantenuta | Sorgente valida dal sistema verificato verso PLC e comportamento su perdita comunicazione; non sostituirla con SafetyOK globale o un solo canale presunto. |
| UP | Operatore: sinistro 7–8 | Traccia Omron → filo/pin → morsettiera → ingresso PLC e prova di commutazione. DI14/DI15 non sono ancora assegnazioni funzionali certificate. |
| DOWN | Operatore: destro 5–6 | Stessa tracciatura e prova. I numeri manoscritti sono pin/fili, non morsetti Omron. |
| Comune pulsanti | Ponte originale 5–7 da mantenere | Alimentazione comune e corrispondenza fili da verificare elettricamente; nessuna modifica proposta. |
| PendantConnected/validità | Campo presente nel software | Come viene acquisito realmente e stato su distacco/guasto; non usare SimulationMode per renderlo vero. |
| SafetyOK e catena PILZ/VSD | Consenso previsto | Sorgente reale, diagnostica e funzione sicura indipendente dal RUN operativo. |
| DriveReady, fermo e velocità | Campi/interblocchi presenti | Feedback effettivi e validità. `NOT Out.MainRun` è assenza di comando, non prova di fermo meccanico. |
| Altezza | Finecorsa, conferma sollevamento e selezione filler/capper | Mappature, polarità, mutua esclusione, gestione timeout e permessi reali. |
| AL102 RotationInhibited | Maschera presente in REV12 | Sorgente ciclica, validità comunicazione, reazione alla perdita dati e presenza equivalente nel nativo. |
| Scala VSD | Parametri percentuali/Hz nel programma | Rapporto meccanico completo, parametri drive e conversione approvata; nessun valore Hz proposto. |

## Verifica prevista dopo approvazione della proposta

Su copia separata: confronto/esportazione dei blocchi nativi; implementazione circoscritta; compilazione PLC senza errori; verifica delle dipendenze HMI se cambiano DB/UDT. Prove logiche isolate dalle uscite: MANUAL/AUTO da stati 50/60/70, rilascio/full squeeze, perdita validità/SafetyOK/drive ready/AL102, conflitto UP+DOWN, finecorsa, altezza contro rotazione, bench/simulazione e assenza latch. Verificare che nessuna scrittura successiva riabiliti uscite o riferimenti.

La proposta è pronta per revisione. Compilazione, applicazione nativa e verifica fisica non sono state eseguite né dichiarate superate. Download e azionamenti restano esclusi.
