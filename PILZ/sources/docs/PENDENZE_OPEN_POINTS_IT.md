# SCHLENKER 36/10 — Open Points PLC/HMI revisionati

**Data audit:** 2026-09-02

**Repository verificato:** `feltrinbernardo/Schlenker`

**Commit verificato:** `cec3144` (`codex/initial-agent-scaffold`)

**Target:** Siemens TIA Portal V19, S7-1512C-1 PN, MTP1500 Unified Comfort

**Uso del documento:** istruzione controllata per Rob e registro delle informazioni ancora necessarie.

**Stato esecuzione 2026-09-03:** il gate di sicurezza dell'ambiente di sviluppo
è stato confermato. Le correzioni verificabili delle sezioni 2.1–2.5 sono state
applicate al progetto nativo TIA esclusivamente offline. Sono state importate 30
fonti SCL con 0 errori e 0 warning; la schermata `safety_pilz_diagnostics` è
stata salvata e verificata tramite proprietà/oggetti TIA Openness. Nessuna API
online, download, controllo CPU o scrittura hardware è stata utilizzata.

Il rebuild completo conferma PLC 0/0. Il rilascio resta bloccato dagli stessi
finding presenti nel checkpoint pre-correzione: password display CPU oltre il
limite/protezione non approvata e `Parameter set type_1` senza versione UDT/tag.
La verifica visiva raster non è disponibile tramite il canale di cattura di
questa installazione; non è stata sostituita con click ciechi.

## 1. Risultato dell'analisi

Il file originale non deve essere usato direttamente come checklist di implementazione. Contiene richieste valide, ma mescola:

- dati già decisi e presenti nel progetto;
- decisioni che spettano al proprietario macchina;
- dati fisici che possono arrivare solo da disegni, targhette, GSDML/IODD, configuratori o commissioning;
- alcune associazioni errate o in conflitto con la baseline SCHLENKER.

Il repository pubblico contiene fonti PLC SCL, tag e specifiche HMI, strumenti TIA Openness, audit e documentazione. Non contiene il progetto nativo `.ap19` attualmente aperto sul laptop. Il manifest registra una copia controllata `v2.12` e un archivio precedente, ma non prova le modifiche che Rob può avere eseguito dopo lo snapshot.

### Stato verificato

| Area | Stato verificato |
|---|---|
| PLC SCL REV12/Open Points | Importazione e rebuild software registrati con 0 errori e 0 warning |
| Hardware TIA | Configurazione fisica ancora incompleta; protezione CPU/display da chiudere |
| HMI | Struttura e strumenti di audit presenti; `Parameter set type_1` rimane un errore/blocco noto |
| G120C | 4 funzioni logiche presenti, 0 drive fisici verificati nell'ultimo inventario offline |
| Pilz | Interfaccia diagnostica logica presente; mappa PROFINET fisica non disponibile |
| IO-Link | Allocazione logica AL100–AL104 presente; 0 AL1403 fisici verificati nell'hardware TIA |
| Anybus/SMC | Architettura definita; PDO e mapping fisico valvole non commissionati |
| EV230 | Correttamente forzata a `FALSE` finché TM Count/fast output non sono configurati |
| FAT/SAT | Ancora richiesti |

## 2. Correzioni obbligatorie prima di continuare l'HMI

Questi punti non sono semplici `WAITING FOR INPUT`: sono contraddizioni da correggere o isolare.

### 2.1 Porte Pilz: 11, non 12 — CORRETTO E IMPORTATO NEL TIA

La baseline controllata della macchina conferma **11 porte**:

- 4 frontali;
- 4 posteriori;
- 2 lato ingresso destro;
- 1 lato uscita sinistro.

Sono inoltre confermate **3 stazioni di richiesta apertura**:

- sotto il pannello HMI;
- fronte macchina;
- retro macchina.

Non è approvata una suddivisione funzionale in tre zone `Z1/Z2/Z3` e non è approvata una dodicesima porta `DR12`.

**Stato dopo la correzione:**

- `UDT_OpenPoints.Door` è stato corretto a `Array[1..11]`;
- `ZoneRequest[1..3]` è stato rimosso dalla struttura open-points;
- il generatore HMI ora mostra solamente inventario aggregato e
  `MAPPING NOT CONFIGURED`, senza porte, zone o canali Pilz presunti;
- il cable schedule `DS100–111` rimane un hold point perché dichiara 12
  dispositivi e non esiste una fonte approvata per decidere quale riga rimuovere.

**Istruzione a Rob:** visualizzare solo diagnostica aggregata finché non arriva la mappa Pilz approvata. Non mostrare porte, zone, moduli o indirizzi individuali come se fossero reali.

### 2.2 Infeed gate: DI-031 e DI-032 non sono i feedback gate — RICONCILIATO NEL TIA

Il file originale associa erroneamente `DI-031` e `DI-032` ai feedback dell'infeed gate.

| Funzione | Riferimento controllato |
|---|---|
| Gate OPEN feedback | `b_InfeedGateOpen`, `%I0.5` nella baseline REV11 |
| Gate CLOSED feedback | `b_InfeedGateClosed`, `%I0.6` nella baseline REV11 |
| Bottle tracking sensor infeed | `b_BottlePresent_Infeed`, `%I0.7` nella baseline REV11 |
| DI-031 | Bottle Shortage 1, AL100/P1 |
| DI-032 | Bottle Shortage 2, AL100/P2 |

Gli indirizzi REV11 devono comunque essere point-to-point verificati contro il disegno elettrico finale prima del commissioning.

Le fonti PLC utilizzano i riferimenti simbolici separati `GateOpenFB` e
`GateClosedFB`; DI-031 e DI-032 restano esclusivamente Bottle Shortage 1 e 2.

### 2.3 AL104 non è opzionale nell'architettura approvata — CORRETTO NEL TIA

REV18 e REV20 congelano l'architettura a **5 IFM AL1403**:

- `AL100` Bottle/accumulation;
- `AL101` Cap system/air pressure;
- `AL102` Process instrumentation;
- `AL103` Additional sensors/expansion;
- `AL104` Customer CIP Interface dedicata.

`AL104` è il quinto master dedicato al CIP e non deve essere descritto come
opzionale. Le fonti offline ora inizializzano
`OpenPoints.AL104.Required := TRUE`; `Configured`, `CommunicationOK` e
`DataValid` restano falsi fino al commissioning reale.

La comunicazione Customer CIP, i canali e l'interfaccia elettrica restano non commissionati. `Required := TRUE` non autorizza ad inventare indirizzi o a simulare comunicazione valida.

### 2.4 Feedback G120C non reale — ISOLATO FAIL-SAFE NEL TIA

Nel supervisore open-points, gli stati `RunningFeedback` dei drive non sono
ancora collegati a feedback reali. Le associazioni precedenti erano:

- Main machine usa attualmente il comando `Out.MainRun` come feedback;
- Product pump usa `Out.ProductPumpRun` come feedback;
- Conveyor e Cap Distributor usano il proprio stato `Running` come ingresso, creando un riferimento circolare.

Queste associazioni sono state rimosse. Tutti e quattro i manager ricevono ora
`RunningFeedback := FALSE` e `RunningFeedbackValid := FALSE`; pertanto restano
`NOT COMMISSIONED / DATA INVALID` fino al mapping delle status word e della
velocità reale dei G120C.

### 2.5 Indirizzi Pilz mostrati nell'HMI — RIMOSSI DAL TIA E DAL GENERATORE

Le specifiche REV22/REV23 dicono esplicitamente di non inventare:

- quantità porte o zone;
- moduli e canali Pilz;
- indirizzi PROFINET;
- bit di porta, lock, STO, air dump o EDM.

Il generatore `BuildUnifiedHomeScreen.cs` non genera più pannelli DR01–DR12,
zone Z1–Z3 o canali locali Pilz presunti. La pagina mantiene i sei binding
diagnostici aggregati e mostra `MAPPING NOT CONFIGURED` per porte, lock, zone,
STO, air dump ed EDM fino alla disponibilità della mappa Pilz approvata.

## 3. Decisioni macchina già confermate

Queste decisioni non devono ritornare a `TBD` salvo una nuova approvazione formale.

| Tema | Decisione confermata |
|---|---|
| Macchina | Riempitrice 36 valvole gravity + tappatore a vite 10 teste |
| Sciacquatrice | Rimossa dal refurbishment |
| Flusso | Destra verso sinistra |
| PLC | Siemens S7-1512C-1 PN |
| HMI | MTP1500 Unified Comfort, WinCC Unified V19 |
| Drive G120C | 4: macchina principale, product pump, bottle conveyor, cap distributor |
| Encoder | IFM RO3101 HTL A/B/Z, 2048 PPR, via TM Count |
| Zero macchina | Sensore sul riferimento valvola riempimento n. 1; encoder previsto sul riduttore principale |
| Anybus/SMC | S7 → PROFINET → ABC3113-A → EtherCAT → EX260-SEC1 → VQC manifold |
| Manifold | 8 × VQC1100N-51 + 2 × VQC1200N-51 |
| EV230 | Fast cap release separata dal manifold SMC |
| Safety | Pilz unica autorità di sicurezza; Siemens/HMI solo coordinamento e diagnostica |
| E-stop | 4 dispositivi in catena aggregata; identificazione HMI individuale non richiesta senza dati Pilz reali |
| Porte | 11 porte e 3 stazioni di richiesta apertura; nessun restart automatico |
| Cap system | Distributore attivo solo in produzione/AUTO e senza channel-empty; vibrator su channel demand; elevator su hopper low con distributore attivo |
| Product level | Setpoint controllo pompa serbatoio 50%; stop a pieno secondo la filosofia macchina, da riconciliare con i parametri PLC correnti |

## 4. Decisioni del proprietario macchina

Questi punti non devono essere risolti autonomamente da Rob.

### 4.1 Velocità nominale e SAT

Esiste un conflitto:

- decisione macchina precedente: target `10,000 bph`;
- sorgente PLC/release: `ProductionSpeedBPH = 12,000`, `MaxSpeedBPH = 12,000` e SAT `11,040 bph`.

**Decisione richiesta:** confermare separatamente velocità nominale, massimo meccanico e target SAT. Fino alla conferma, Rob non deve cambiare scaling, gauge o limiti HMI.

### 4.2 Protezione CPU e accesso engineering

Baseline raccomandata:

- HMI Runtime deve poter comunicare normalmente;
- modifica/download PLC non autorizzati devono essere bloccati;
- accesso engineering riservato al costruttore/proprietario e tecnici nominati;
- password display CPU conforme al limite TIA e inserita direttamente nel progetto;
- nessuna password in Git, chat, e-mail, screenshot o change-log;
- backup e recovery devono essere possibili da un engineer autorizzato.

Rob deve configurare l'esatto livello TIA solo dopo approvazione del proprietario.

### 4.3 `Parameter set type_1`

Nel repository non esiste una funzione approvata che richieda questo Parameter Set. La gestione ricette CIP è già PLC-owned tramite UDT/DB dedicati.

**Raccomandazione:** se la cross-reference nel progetto nativo conferma zero utilizzi, eliminare `Parameter set type_1`. Non collegarlo artificialmente a un UDT solo per eliminare l'errore di compilazione. Se risulta usato, fermarsi e documentare oggetto, UDT, versione e tag PLC prima della modifica.

### 4.4 Ruoli HMI proposti

La seguente matrice è una proposta da approvare, non una configurazione già commissionata.

| Ruolo | Consentito | Vietato |
|---|---|---|
| Operator | Produzione ON/OFF, reset richieste, selezione ricetta approvata, visualizzazione allarmi/diagnostica | Engineering, mapping, forcing, modifica ricette protette, bypass safety |
| Maintenance | Diagnostica avanzata, manuale/JOG con interlock, reset manutenzione, parametri manutenzione autorizzati | Modifica safety, mapping hardware, download PLC, forcing non autorizzato |
| Supervisor | Funzioni Operator + approvazioni operative e parametri di produzione autorizzati | Engineering hardware/safety e bypass interlock |
| Manufacturer / Engineer | Configurazione protetta, homing/tracking/drive/EV230/gateway, gestione utenti e commissioning autorizzato | Qualsiasi bypass della logica Pilz o test hardware senza autorizzazione |

Da approvare: timeout, logout, session reversion e chi può approvare/bloccare ricette CIP.

### 4.5 Autorità ricette e retention

**Architettura raccomandata:** PLC come unica autorità dei dati ricetta; HMI come interfaccia di selezione/modifica autorizzata.

Da rendere retentivi solo dopo test:

- ricetta CIP approvata e locked;
- ultima ricetta precedente/rollback;
- contatori manutenzione e ore macchina;
- parametri macchina approvati;
- audit ricetta ed execution history se richiesti dalla politica cliente.

Da inizializzare sempre in stato sicuro dopo restart:

- comandi motori e valvole;
- richieste CIP;
- EV230;
- `HomeValid` e `TrackingValid`;
- richieste manuali/JOG;
- sequenze non completate, che devono richiedere recovery deliberato.

Warm restart, cold restart e power cycle devono essere testati separatamente. Non dichiarare retention solo perché la variabile esiste nel DB.

## 5. Allarmi: cosa è già definito e cosa manca

La filosofia allarmi non è completamente `Da definire`. REV21 e `REV12_Alarm_List_EN_IT.csv` definiscono già classi, testi bilingui e reazioni per gli allarmi principali.

Già definito:

- classi Warning, Critical/Controlled Stop, Safety, Communication/Quality e Information;
- cap system, bottle shortage/accumulation, process, drives, CIP, network/SMC e safety diagnostics;
- acknowledgement separato dalla rimozione della causa;
- colore rosso per fault, ambra per warning e stato distinto per data invalid/no communication.

Ancora da chiudere:

- sorgente fisica e qualità per ogni allarme non mappato;
- delay/debounce specifico dove non già parametrizzato;
- acknowledgement richiesto o automatic return per ogni riga;
- navigazione alla pagina diagnostica;
- reset/recovery verificato;
- alarm flood/suppression;
- test con causa attiva: acknowledgement non deve creare falso stato healthy.

## 6. Open points che richiedono dati reali

| ID | Open point | Stato | Dati minimi per chiusura |
|---:|---|---|---|
| 1 | 4 × G120C | WAITING FOR HARDWARE DATA | Order number, firmware, device name, telegram, I/O, scaling, status/control word, motor nameplate |
| 2 | Homing | PARTLY DEFINED | Direzione forward-only coerente con meccanica, velocità, timeout, TM Count, polarità ZS100, procedura zero e perdita posizione |
| 3 | BottleID tracking | WAITING FOR COMMISSIONING | Offset encoder reali Infeed/Filler/Transfer/Cap Release/Capper/Outfeed, tolleranze, sensori conferma, stale/lost rules |
| 4 | Infeed gate | PARTLY DEFINED | Conferma `%I0.5/%I0.6`, polarità, finestra encoder, timeout, feedback contradiction, recovery |
| 5 | Cap system | PARTLY DEFINED | Mapping fisico DI-040/060/061/062, timeout/jam, feedback drive e canali reali |
| 6 | EV230 | WAITING FOR TM COUNT | TO/modulo/canale fast DQ, angolo/offset, tolleranza, pulse duration, scaling, safe polarity, single-pulse test |
| 7 | ABC3113-A | WAITING FOR DEVICE FILE | GSDML, order number/firmware, name/IP, process lengths, byte order, watchdog, config export |
| 8 | EX260-SEC1 | WAITING FOR ETHERCAT MAP | ESI/config export, PDO map, firmware, module order, safe communication-loss behavior |
| 9 | Valve 01–10 | WAITING FOR ELECTRICAL MAP | Funzione, EX260 channel, EtherCAT/PROFINET offset, PLC byte/bit, solenoid, drawing reference |
| 10 | Pilz | WAITING FOR PILZ PROJECT | GSDML/module, device name/IP, raw byte map, DataValid/watchdog, aggregate and individual diagnostics realmente esportati |
| 11 | AL100–AL104 | WAITING FOR TIA HARDWARE | 5 AL1403, device names/IP, GSDML, port modes, IODD, process data, polarity/scaling, diagnostics |
| 12 | Customer CIP | WAITING FOR CUSTOMER INTERFACE | 24 VDC PNP o dry contacts, relays, request/feedback map, handshake, timeout, fault ownership |
| 13 | Local controls | WAITING FOR ELECTRICAL DRAWING | Start, Stop, Reset, Auxiliary, MAN/AUT e Pendant Connected: indirizzi, polarità e autorità |
| 14 | TLS100/process instruments | WAITING FOR DEVICE SELECTION | Modello reale, IO-Link o 4–20 mA, range, unità, scaling, filtro e allarmi |
| 15 | Vacuum M103 | WAITING FOR MOTOR/INSTRUMENT DATA | Contactor o VFD, motor data, feedback, transmitter/scaling e verifica polarità `-400 mbar` |
| 16 | Hood M104 | WAITING FOR ELECTRICAL CONFIRMATION | Contactor/VFD, motor data, associazione `AirFilterRelayCmd`, pressure switch e fault feedback |
| 17 | SMC external wash/process valves | WAITING FOR MANIFOLD MAP | EV200-O/C, EV210, EV217, EV213, EV212, EV247 e wash valve: solenoide/bit/cavo e prova end-to-end |

## 7. Regole fail-safe che Rob deve mantenere

- `SystemConfigurationComplete` deve restare `FALSE` finché le mappe required non sono reali e verificate.
- Nessun device non configurato può produrre `READY`, `RUNNING`, `HEALTHY` o valore valido.
- Perdita comunicazione deve mostrare `COMMUNICATION FAULT / DATA INVALID`, mai un falso `OFF` o `CLOSED`.
- EV230 resta `FALSE` fino alla configurazione e prova del fast output.
- Mapping valvole generico resta a zero fino all'assegnazione fisica.
- Il Pilz resta unica autorità safety.
- Chiusura porta o reset safety non devono riavviare la macchina automaticamente.
- HMI reset e acknowledgement sono richieste; non eliminano la causa attiva.
- JOG invalida tracking e richiede re-homing/recovery secondo la logica approvata.
- Nessun indirizzo, telegramma, PDO, porta IO-Link, polarità o scaling deve essere inventato.

## 8. Ordine di lavoro raccomandato per Rob

1. **COMPLETATO:** checkpoint, archive ufficiale pre-correzione, archive
   ufficiale post-correzione, snapshot e manifest sono registrati nel vault.
2. **COMPLETATO NEL TIA:** correggere le contraddizioni
   documentali: 11 porte, 3 request stations, gate DI, AL104 required.
3. **COMPLETATO NEL TIA E NEL GENERATORE HMI:** eliminare qualsiasi indirizzo
   Pilz non provato e mostrare `MAPPING NOT CONFIGURED`.
4. Verificare cross-reference di `Parameter set type_1`; eliminare solo se realmente inutilizzato.
5. Applicare la protezione CPU/display approvata senza registrare password.
6. Configurare hardware TIA nell'ordine: Pilz, AL100–AL104, G120C, ABC3113-A, EX260, TM Count.
7. Importare e verificare le mappe reali senza attivare `Configured/Required/DataValid` anticipatamente.
8. **ISOLATO FAIL-SAFE:** i falsi feedback dei quattro drive sono stati
   rimossi; il completamento richiede i dati reali dei telegrammi.
9. Configurare homing, tracking, gate ed EV230 con parametri misurati.
10. Chiudere utenti/ruoli, retention e acknowledgement degli allarmi.
11. **ESEGUITO CON HOLD POINT:** PLC rebuild 0/0; hardware e HMI confermano i
    finding preesistenti descritti nelle sezioni 4.2 e 4.3.
12. Eseguire PLCSIM/negative tests, poi FAT autorizzato e I/O point-to-point.
13. **COMPLETATO:** nuovo `.zap19` post-correzione, manifest e report creati;
    il rilascio resta bloccato finché i finding preesistenti non sono risolti.

## 9. Acceptance gate

Un punto può diventare `CLOSED` solo quando:

1. la fonte è approvata e identificata;
2. il dato è configurato senza supposizioni;
3. PLC, hardware e HMI compilano senza errori rilevanti;
4. il comportamento fail-safe è testato;
5. l'HMI distingue command, feedback e data quality;
6. esiste evidenza di test e rollback;
7. il change-log è aggiornato;
8. per hardware/safety, il commissioning è stato autorizzato e documentato.

## 10. Fonti controllate usate per questa revisione

- `REV12/REV12_MANIFEST.json`
- `REV12/00_README_FIRST.md`
- `REV12/Documentation/REV11_to_REV12_Merge_Register.md`
- `REV12/Documentation/REV12_Guard_Inventory.csv`
- `REV12/Documentation/REV12_Access_Station_Inventory.csv`
- `REV12/Documentation/REV12_IO_Tag_Mapping.csv`
- `REV12/Documentation/REV20_AL1403_Master_Port_Allocation.csv`
- `REV12/Documentation/REV12_Motor_List.csv`
- `REV12/PLC_Sources/01A_UDT_OpenPoints.scl`
- `REV12/PLC_Sources/02_DB_Global.scl`
- `REV12/PLC_Sources/10O_FB_OpenPoints.scl`
- `REV12/PLC_Sources/11_FB_Application.scl`
- `REV12/HMI/REV12_HMI_Tags.csv`
- `REV12/HMI/REV12_Alarm_List_EN_IT.csv`
- `docs/HMI_MISSING_CONFIG_BACKLOG.md`
- `docs/PLC_IO_LINK_LIST.md`
- `docs/PLC_MOTOR_LIST.md`
- `ROB_REV18_IO-Link_Master_Naming_Diagnostics_CIP_Upgrade.docx`
- `ROB_REV20_AL1403_Master_Port_Allocation_PLC_HMI_Wiring.docx`
- `ROB_REV22_PILZ_Safety_Software_HMI_Diagnostics.docx`
- `ROB_REV23_PILZ_PROFINET_NonSafety_Diagnostics_Interface.docx`

---

**Nota finale per Rob:** questo documento autorizza correzioni documentali e rimozione di visualizzazioni non provate. Non autorizza download, modifiche online, forcing, reset CPU, test su macchina o cambi safety. Tali operazioni richiedono autorizzazione separata sul target esatto.
