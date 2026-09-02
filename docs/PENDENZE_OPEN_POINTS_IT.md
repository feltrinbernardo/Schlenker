# SCHLENKER 36/10 — Pendenze per la chiusura degli Open Points PLC/HMI

## Scopo

Questo documento raccoglie tutte le informazioni, le decisioni e i file tecnici ancora necessari per chiudere gli elementi classificati come `WAITING FOR INPUT` e `TO FIX` nel progetto SCHLENKER 36/10.

Il progetto non deve essere considerato completamente commissionato finché le informazioni elencate di seguito non sono state fornite, integrate e verificate mediante compilazione e test appropriati.

## 1. Elementi `TO FIX`

### 1.1 Protezione hardware della CPU

Sono necessarie le seguenti decisioni:

- definire il livello di protezione da configurare per `PLC_1`;
- definire la politica per la password del display della CPU, attualmente più lunga del limite consentito di otto caratteri;
- definire chi è autorizzato ad accedere, modificare e scaricare il progetto PLC;
- confermare se sono richieste protezioni separate per accesso in lettura, accesso in scrittura e funzioni HMI.

> **Nota di sicurezza:** non inviare password tramite e-mail, chat, documenti di progetto o change-log. La password deve essere inserita direttamente nel TIA Portal o trasferita mediante un canale sicuro approvato.

### 1.2 `Parameter set type_1` del pannello HMI

È necessario scegliere una delle seguenti opzioni:

1. **Il Parameter Set è utilizzato**
   - indicare lo UDT PLC/HMI da utilizzare;
   - indicare la versione corretta dello UDT;
   - indicare il tag PLC di riferimento;
   - specificare la funzione associata, ad esempio ricetta, formato, CIP o parametri macchina.

2. **Il Parameter Set non è utilizzato**
   - autorizzare esplicitamente la sua eliminazione dal progetto HMI.

3. **La funzione non è ancora definita**
   - descrivere lo scopo previsto;
   - mantenere lo stato `WAITING FOR INPUT` fino alla decisione formale.

Gli errori attualmente presenti sono:

- versione del tipo dati PLC/HMI non configurata;
- riferimento al tag PLC non configurato.

### 1.3 Utenti, ruoli e autorizzazioni HMI

Fornire una matrice approvata contenente almeno:

| Ruolo | Funzioni autorizzate | Parametri modificabili | Funzioni vietate |
|---|---|---|---|
| Operator | Da definire | Da definire | Engineering e configurazione protetta |
| Maintenance | Da definire | Da definire | Da definire |
| Supervisor | Da definire | Da definire | Da definire |
| Manufacturer / Engineer | Da definire | Da definire | Da definire |

Confermare inoltre:

- timeout della sessione;
- politica di logout;
- gestione della sessione globale WinCC Unified;
- autorizzazioni per pagine di manutenzione e diagnostica;
- autorizzazioni per parametri di homing, tracking, drive, EV230 e gateway;
- modalità di test offline dei ruoli Runtime.

### 1.4 Allarmi HMI

Definire:

- quali diagnosi devono generare un allarme;
- classe e priorità di ogni allarme;
- quali allarmi richiedono acknowledgement;
- comportamento dopo acknowledgement;
- pagina diagnostica di destinazione;
- testo operatore approvato;
- eventuale ritardo o filtro temporale;
- condizioni di reset e riarmo.

L'acknowledgement non deve eliminare la causa attiva del guasto.

### 1.5 Retention e ricette

Fornire:

- elenco delle variabili realmente retentive;
- comportamento richiesto dopo warm restart, cold restart e power cycle;
- autorità dei dati: PLC, WinCC Unified oppure entrambi;
- elenco delle ricette approvate;
- elenco dei parametri protetti;
- regole per salvataggio, caricamento e modifica delle ricette;
- responsabilità in caso di conflitto tra dati PLC e dati HMI.

Non deve essere dichiarata alcuna persistenza senza evidenza di configurazione e test.

## 2. Elementi `WAITING FOR INPUT`

### 2.1 Quattro azionamenti G120C

Per ciascun azionamento è necessario fornire una riga completa nella seguente matrice:

| Dato richiesto | Main machine | Product pump | Bottle conveyor | Cap distributor |
|---|---|---|---|---|
| Device name | Da fornire | Da fornire | Da fornire | Da fornire |
| Modello / Order Number | Da fornire | Da fornire | Da fornire | Da fornire |
| Firmware | Da fornire | Da fornire | Da fornire | Da fornire |
| Telegramma | Da fornire | Da fornire | Da fornire | Da fornire |
| Indirizzi input/output | Da fornire | Da fornire | Da fornire | Da fornire |
| Control word | Da fornire | Da fornire | Da fornire | Da fornire |
| Status word | Da fornire | Da fornire | Da fornire | Da fornire |
| Speed setpoint scaling | Da fornire | Da fornire | Da fornire | Da fornire |
| Actual speed scaling | Da fornire | Da fornire | Da fornire | Da fornire |
| Ready / Running / Fault / Warning | Da fornire | Da fornire | Da fornire | Da fornire |
| Reset fault | Da fornire | Da fornire | Da fornire | Da fornire |
| Required in Production | Confermare | Confermare | Confermare | Confermare |

Senza questi dati le interfacce rimangono simboliche e non possono produrre un falso stato `READY` o `RUNNING`.

### 2.2 Homing e `HomeValid`

Fornire:

- direzione di ricerca del riferimento;
- velocità di ricerca;
- timeout approvato;
- identificazione del sensore `MachineZero`;
- indirizzo e polarità del sensore;
- scaling e unità dell'encoder;
- procedura per impostare e confermare lo zero;
- criteri di perdita posizione;
- comportamento richiesto dopo E-stop o encoder fault;
- procedura di re-homing;
- eventuali limiti meccanici o zone vietate.

### 2.3 BottleID e tracking

Fornire gli offset reali, nelle unità encoder approvate, per:

1. Infeed;
2. Filler;
3. Transfer;
4. Cap Release;
5. Capper;
6. Outfeed.

Fornire inoltre:

- sensore di creazione BottleID;
- sensori di conferma nelle stazioni;
- tolleranza posizione per ogni stazione;
- criteri di bottiglia persa;
- criteri di bottiglia inattesa;
- timeout o limite per BottleID obsoleti;
- comportamento in caso di incoerenza sensore/tracking;
- condizioni di rimozione del BottleID in outfeed.

### 2.4 Infeed gate

Fornire:

- finestra angolare o di posizione autorizzata;
- indirizzo e polarità del feedback `OPEN`;
- indirizzo e polarità del feedback `CLOSED`;
- timeout di apertura;
- timeout di chiusura;
- condizioni di feedback contraddittorio;
- comportamento richiesto fuori dalla finestra autorizzata;
- procedura di recovery senza riavvio automatico;
- conferma dei segnali DI-031 e DI-032.

Senza parametri approvati l'apertura automatica deve rimanere bloccata.

### 2.5 Cap system

Fornire la mappa elettrica e i parametri reali per:

- cap distributor;
- vibrator;
- elevator demand;
- DI-040 `Cap Present`;
- DI-060 `Hopper Low`;
- DI-061 `Channel Demand`;
- DI-062 `Channel Empty / Missing Caps`;
- timeout;
- condizioni AUTO/MANUAL;
- feedback e reset fault.

### 2.6 EV230 fast cap release

Fornire:

- modulo e canale dell'uscita rapida;
- Technology Object o configurazione `TM Count` applicabile;
- posizione o angolo di attivazione;
- offset protetto;
- tolleranza della finestra di posizione;
- durata approvata dell'impulso;
- unità e scaling dell'encoder;
- polarità sicura dell'uscita;
- criteri di timeout/fault;
- procedura di test fisico;
- conferma che EV230 rimane separata dal manifold SMC.

Fino alla disponibilità e approvazione di questi dati, l'uscita EV230 rimane forzata a `FALSE` e lo stato rimane `NOT COMMISSIONED`.

### 2.7 ABC3113-A / PROFINET

Fornire:

- GSDML corretto e approvato;
- Order Number e firmware dell'ABC3113-A;
- device name PROFINET;
- indirizzo IP approvato;
- input start address;
- input data length;
- output start address;
- output data length;
- byte order;
- bit order;
- status word e fault code;
- file di configurazione del gateway;
- tabella dei dati di processo;
- watchdog e comportamento in caso di perdita comunicazione.

### 2.8 EX260-SEC1 / EtherCAT / VQC manifold

Fornire:

- configurazione EtherCAT dell'ABC3113-A verso EX260-SEC1;
- process-data mapping EtherCAT;
- identificazione e firmware dell'EX260-SEC1;
- numero e tipo dei moduli/manifold;
- mappa degli output EX260;
- canali delle dieci stazioni fisiche;
- diagnostica dispositivo/manifold;
- comportamento sicuro in caso di perdita EtherCAT;
- file di configurazione ufficiale o export del configuratore.

L'architettura da mantenere è:

`S7-1512C-1 PN → PROFINET → ABC3113-A → EtherCAT → EX260-SEC1 → VQC manifold`.

### 2.9 Mapping Valve 01–Valve 10

Per ciascuna valvola fornire:

| Campo | Valve 01–Valve 10 |
|---|---|
| Funzione macchina | Da fornire |
| Canale EX260 | Da fornire |
| Posizione EtherCAT | Da fornire |
| Posizione PROFINET | Da fornire |
| Byte/bit PLC | Da fornire |
| Valvola/solenoide fisico | Da fornire |
| Codice dispositivo elettrico | Da fornire |
| Riferimento disegno | Da fornire |
| Revisione mapping | Da fornire |
| Responsabile approvazione | Da fornire |

Fino all'emissione della mappa ufficiale la funzione deve rimanere `TBD / WAITING FOR INPUT`. Non deve essere effettuata alcuna associazione per supposizione.

### 2.10 Pilz, dodici porte e tre zone

Fornire una mappa approvata di byte, bit e polarità per:

- Z1: DR01–DR04;
- Z2: DR05–DR08;
- Z3: DR09–DR12;
- PB-DR-REQ-ALL;
- PB-DR-REQ-Z1;
- PB-DR-REQ-Z2;
- PB-DR-REQ-Z3;
- porta aperta/chiusa;
- porta bloccata/sbloccata;
- discrepancy;
- waiting for standstill;
- standstill failure;
- dump confirmed/not confirmed;
- Safety Reset Required;
- Safety OK;
- comunicazione Pilz;
- DataValid;
- MappingConfigured.

Fornire inoltre:

- protocollo o formato della comunicazione Pilz–Siemens;
- watchdog;
- comportamento in caso di perdita dati;
- criteri di reset e recovery.

Il Pilz rimane l'unica autorità di sicurezza. Il PLC Siemens e l'HMI possono fornire solamente diagnostica e coordinamento.

### 2.11 IO-Link AL100–AL104

Confermare la quantità fisica reale e fornire:

| Master logico | Funzione prevista | Required | Decisione / dati richiesti |
|---|---|---:|---|
| AL100 | Bottle / accumulation sensors | Sì | Device reale, GSDML, porte e process data |
| AL101 | Cap system / air pressure | Sì | Device reale, GSDML, porte e process data |
| AL102 | Process instrumentation | Sì | Device reale, GSDML, porte e process data |
| AL103 | Reserved | Da confermare | Confermare se deve realmente bloccare Production |
| AL104 | Customer CIP provisional | No, attualmente | Confermare se rimane opzionale |

Per ogni porta IO-Link fornire:

- master e numero porta;
- dispositivo collegato;
- IODD applicabile;
- lunghezza e formato dei process data;
- engineering units e scaling;
- stato `Required`;
- diagnostica porta/device;
- comportamento in caso di device assente.

Non aggiungere o rimuovere hardware senza approvazione.

### 2.12 Customer CIP

Confermare la mappa fisica e il comportamento dei seguenti segnali:

Richieste:

- `CIP_REQ_COLD_WATER`;
- `CIP_REQ_HOT_WATER`;
- `CIP_REQ_ACID`;
- `CIP_REQ_CITRA`;
- `CIP_REQ_DISCHARGE`;
- `PRODUCTION_REQ_PRODUCT`.

Feedback:

- `CIP_REMOTE_READY`;
- `CIP_REMOTE_BUSY`;
- `CIP_REMOTE_FAULT`;
- `CIP_REMOTE_REQUEST_ACCEPTED`;
- `CIP_MEDIUM_AVAILABLE`;
- `CIP_DISCHARGE_COMPLETE`.

Fornire inoltre:

- indirizzi e polarità;
- interfaccia fisica o protocollo;
- timeout;
- sequenza di handshake;
- comportamento in caso di perdita comunicazione durante CIP;
- autorità per il reset del fault.

### 2.13 Pulsanti e selettori locali

Fornire indirizzo, polarità e autorità per:

- Start;
- Stop;
- Reset Alarm;
- Auxiliaries;
- selettore Auto/Manual;
- Pendant Connected.

Confermare:

- priorità dello Stop fisico;
- modalità di rilevamento del comando Start deliberato;
- autorità del selettore MAN/AUT;
- obbligo di `Pendant Connected` per JOG;
- differenza visuale HMI tra ingresso fisico, comando HMI e placeholder.

## 3. File e fonti tecniche accettabili

Le informazioni possono essere fornite mediante:

- disegni elettrici revisionati;
- BOM hardware approvata;
- export della configurazione hardware TIA Portal;
- GSDML ufficiali;
- IODD ufficiali;
- manuali e documentazione ufficiale del costruttore;
- export o file di configurazione Anybus;
- tabella I/O revisionata;
- tabella di mapping PROFINET/EtherCAT;
- mappa Pilz approvata;
- lista dispositivi/porte IO-Link;
- matrice valvole concordata tra tutte le parti;
- specifica funzionale o verbale di commissioning approvato.

Non utilizzare valori dedotti, indirizzi ipotetici o parametri non approvati.

## 4. Ordine minimo raccomandato

Per procedere nel modo più efficace, fornire le informazioni nel seguente ordine:

1. decisione relativa a `Parameter set type_1`;
2. livello di protezione CPU e trattamento sicuro della password display;
3. GSDML e configurazione ABC3113-A;
4. configurazione EtherCAT/EX260-SEC1;
5. dati e telegrammi dei quattro G120C;
6. mappa Pilz delle dodici porte e delle tre zone;
7. mapping ufficiale Valve 01–Valve 10;
8. configurazione IO-Link AL100–AL104;
9. parametri homing e tracking;
10. finestra e feedback dell'infeed gate;
11. configurazione EV230;
12. matrice utenti/ruoli HMI;
13. allarmi, retention e strategia ricette;
14. indirizzi e polarità di Customer CIP e pulsanti locali.

I punti 1 e 2 permettono di eliminare gli errori radice di compilazione hardware/HMI. Gli altri punti permettono di rimuovere progressivamente gli stati `NOT COMMISSIONED`, `MISSING CONFIG`, `DATA INVALID` e `WAITING FOR INPUT`.

## 5. Test e commissioning ancora necessari

Dopo l'integrazione dei dati sarà necessario eseguire:

- PLC full software rebuild;
- hardware compile;
- HMI full rebuild;
- audit delle cross-reference;
- audit dei binding HMI;
- audit dei trigger allarme;
- review delle macchine a stati;
- test PLCSIM, quando compatibile;
- test negativi di SafetyOK, encoder, HomeValid, TrackingValid e comunicazioni;
- test dei quattro drive;
- test Anybus PROFINET, EtherCAT ed EX260;
- test delle dodici porte e delle tre zone;
- test IO-Link;
- test EV230 con una sola attivazione per BottleID;
- test Customer CIP;
- test ruoli e autorizzazioni HMI;
- test warm restart, cold restart e power cycle;
- creazione e verifica di un nuovo archive `.zap19` post-correzione.

Qualsiasi connessione, download, modifica online o test su hardware deve essere eseguito solamente su un impianto di sviluppo/test identificato e con autorizzazione esplicita per l'operazione specifica.

## 6. Criterio di chiusura

Un open point può essere marcato come completato solamente quando:

1. l'informazione richiesta è stata fornita da una fonte approvata;
2. la configurazione è stata integrata senza valori inventati;
3. PLC, hardware e HMI compilano senza errori;
4. la funzione è stata verificata offline e, quando necessario, mediante commissioning autorizzato;
5. le evidenze e i risultati dei test sono stati salvati;
6. il rollback e l'archive finale sono disponibili;
7. la modifica è stata registrata nel change-log del progetto.
