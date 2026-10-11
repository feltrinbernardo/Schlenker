# SCHLENKER 36/10 — stato implementazione aggiornamento PLC/HMI per Rob

**Data verifica:** 2026-09-21  
**Documento sorgente:** `SCHLENKER_36_10_ROB_PLC_HMI_IMPLEMENTATION_UPDATE_2026-09-19.docx`  
**Progetto TIA verificato:** `Backup Schlenkers 36-10 190036-7-8v2.12.ap19`  
**Modalità:** offline; nessuna API online, nessun download, nessun comando CPU/HMI e nessun test di movimento.

## Risultato sintetico

Il progetto TIA è stato letto direttamente tramite TIA Portal V19 Openness. Il
contenuto PLC esistente comprende 70 oggetti generabili ed è coerente. Dopo
l'implementazione del coordinamento nastro e dei relativi tag HMI, il rebuild
completo ha prodotto:

- PLC software: **0 errori, 0 warning**;
- hardware PLC: **0 errori, 0 warning**;
- HMI Unified: **0 errori, 0 warning**;
- nuovi tag HMI: **10 creati**, 387 esistenti aggiornati senza cambiare i loro
  indirizzi PLC;
- nuova pagina HMI `conveyor_settings`: **45 oggetti**, nomi univoci, quattro
  parametri modificabili verificati e collegamento dalla pagina `production`;
- archivio pre-modifica: `C:\TIA Projects\Schlenker-Controlled-Archives\pre-rob-update-20260921`.

## Stato richieste del documento

| N. | Area | Stato | Evidenza / azione | Blocco residuo |
|---:|---|---|---|---|
| 1 | IO-Link AL100–AL104 | **WAITING FOR INPUT** | Le strutture PLC, i cinque master logici e la diagnostica fail-safe sono presenti in `DB_Global`, `UDT_OpenPoints`, `FB_OpenPointsSupervisor` e nelle pagine diagnostiche. | Nell'hardware TIA sono configurati **0 master fisici**. Servono modelli/firmware, device name, IP, GSDML/IODD, port mode, process data, polarità e scaling approvati. |
| 2 | Quattro G120C | **WAITING FOR INPUT** | Le interfacce logiche e i quattro `FB_DriveManager` esistono. I falsi feedback sono già isolati fail-safe. | Nell'hardware TIA sono configurati **0 G120C**. Servono order number/firmware, nomi/IP, telegrammi, mapping control/status word, scaling velocità e dati motore. |
| 3 | Sincronismo bottle conveyor | **DONE — SOFTWARE OFFLINE** | Creato `FB_ConveyorSync`; sincronismo selezionabile, velocità base, offset solo positivo, prestart nastro, run-on normale regolabile con default 10 s, annullamento immediato per safety/allarme critico. Creata la pagina HMI `conveyor_settings` con accesso da `production`, parametri, stati live e indicazione `NO DATA` quando la comunicazione non è valida. | Prova funzionale con drive reale e 400 VAC vietata fino al commissioning. La conferma di marcia reale del nastro dipende dal telegramma G120C. |
| 4 | JOG macchina principale | **TO IMPLEMENT / WAITING FOR INPUT** | `FB_JogPendant` è già hold-to-run, forward-only, interbloccato da Manual, safety, pendant, altezza e drive-ready; il JOG invalida il tracking fino al recovery/homing. | Il limite reale 15 Hz e la rampa 3 s devono essere applicati con scaling/rampa del G120C. Mancano frequenza base motore, telegramma e parametri drive approvati; non si converte arbitrariamente Hz in %. |
| 5 | Pendant OMRON A4EG-C000041 | **WAITING FOR INPUT** | L'interfaccia PLC è già separata dal comando ordinario e non contiene reverse o software E-stop. | Servono pin/contact allocation finale e validazione Pilz. Nessuna modifica safety o bypass è stata eseguita. |
| 6 | Transizione Manuale/Automatico | **DONE — SOFTWARE OFFLINE** | `FB_MainState` esce da Manual senza richiedere reset safety se la safety non è intervenuta; gli stati `AutomaticActive`, `ManualActive` e `PendantConnected` sono ora esposti anche come tag HMI. | Validazione operativa finale con selettore fisico MAN/AUT. |
| 7 | Door request / power philosophy | **DONE LOGICA PLC; WAITING SAFETY COMMISSIONING** | `FB_DoorAccess` esegue controlled stop, attende zero speed e conferma 3-phase off prima della richiesta unlock; non autorizza restart automatico. Il Pilz rimane unica autorità safety. | Mappa Pilz, feedback power-off/lock e test safety certificato. |
| 8 | Encoder diretto PLC | **DONE LOGICA; WAITING HARDWARE** | Homing, `HomeValid`, `BottleID`, tracking e perdita validità sono presenti in `FB_HomingManager` e `FB_BottleTracking`. | Configurazione TM Count, canali A/B/Z, scaling, polarità, reference sensor e offset misurati. |
| 9 | Limite commissioning | **DONE / RISPETTATO** | Tutto il lavoro di questa esecuzione è stato offline. Nessun test motore o movimento e nessuna tensione 400 VAC sono stati richiesti o usati. | FAT/SAT solo dopo cablaggio, drive, motori e safety commissionati. |
| 10 | Feedback puntuale | **DONE** | Questo documento classifica ogni punto e identifica blocchi, DB/tag, hardware e attività mancanti. | Aggiornare gli stati dopo l'arrivo dei dati reali e dopo ogni test autorizzato. |

## Oggetti aggiunti o estesi

- `FB_ConveyorSync` — coordinamento nastro, prestart e run-on normale;
- `UDT_Command.ConveyorSyncEnable`;
- `UDT_Parameters.ConveyorBaseSpeedPct`;
- `UDT_Parameters.ConveyorSpeedOffsetPct`;
- `UDT_Parameters.ConveyorPrestartTime`;
- `UDT_Parameters.ConveyorRunOnTime`;
- `UDT_Output.ConveyorSyncActive`;
- `UDT_Output.ConveyorRunOnActive`;
- tag HMI per comando, parametri, stati Manual/Automatic e pendant;
- pagina HMI `conveyor_settings` e pulsante `CONVEYOR SETTINGS` su
  `production`, senza sovrapposizioni con oggetti interattivi esistenti.

## Valori iniziali controllati

| Parametro | Valore iniziale | Limite PLC |
|---|---:|---:|
| Conveyor sync | Abilitato | Booleano |
| Base conveyor speed | 50 % | 0–100 % |
| Positive sync offset | 5 % | 0–50 % |
| Conveyor prestart | 1 s | 0–10 s |
| Normal-stop run-on | 10 s | 0–60 s |

Questi valori sono parametri di processo, non parametri safety. Safety loss,
E-stop o allarme critico annullano immediatamente il comando nastro.

## Evidenze di verifica

- audit progetto: `%TEMP%\schlenker-implementation-audit-20260921\project-audit.txt`;
- sorgente PLC generata dal progetto: `%TEMP%\schlenker-implementation-audit-20260921\generated-source.scl`;
- import PLC: `%TEMP%\schlenker-implementation-audit-20260921\import-conveyor-sync.txt`;
- import tag HMI: `%TEMP%\schlenker-implementation-audit-20260921\import-hmi-tags.txt`;
- build e audit pagina nastro: `%TEMP%\schlenker-implementation-audit-20260921\build-conveyor-settings.txt`;
- rebuild finale successivo alla pagina HMI: `%TEMP%\schlenker-implementation-audit-20260921\final-offline-compile-after-hmi-page.txt`;
- baseline Production read-only successiva alla modifica: `%TEMP%\schlenker-production-baseline-after-conveyor-20260921\baseline-manifest.tsv`.

Il controllo geometrico del pulsante nella regione `x=660..935`,
`y=438..478` ha trovato soltanto i due rettangoli di sfondo e il nuovo pulsante;
non risultano sovrapposizioni con altri comandi operatore.

## Prossimo ordine di lavoro

1. Ricevere e approvare dati G120C, AL100–AL104, Pilz e TM Count.
2. Configurare hardware TIA senza inventare indirizzi o telegrammi.
3. Collegare feedback reali `Ready/Running/Fault/ActualSpeed` dei quattro drive.
4. Applicare 15 Hz / 3 s al JOG usando lo scaling e i parametri G120C reali.
5. Compilare offline PLC/hardware/HMI.
6. Chiedere conferma esplicita prima di ogni download.
7. Eseguire test 24 V; eseguire prove motore solo dopo commissioning 400 VAC e safety.
