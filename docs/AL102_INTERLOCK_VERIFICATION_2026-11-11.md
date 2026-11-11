# AL102 — verifica interblocco rotazione, 11 novembre 2026

## 1. Risultato e limite della verifica

Nella copia offline esportata il blocco della rotazione è imposto dal software: `AL102InterlockVerified := FALSE` e `RotationInhibited := TRUE` vengono riscritti a ogni ciclo. Non sono una diagnosi misurata di un sensore guasto. Non basta correggere una scritta HMI o impostare un bit a TRUE.

La verifica qui riportata riguarda sorgenti salvati, documenti e registrazioni storiche. L'equivalenza del programma installato nel PLC con questa copia **non è dimostrata**. Eventuali nuove osservazioni online di sola lettura devono essere registrate separatamente con data e target. Nessuna modifica PLC/HMI/hardware, importazione, compilazione della proposta, download o azionamento è attestata da questo rapporto. REV09 non modificata.

## 2. Mappa funzionale corrente e conflitti documentali

L'operatore conferma che tutti i sensori AL102 sono installati e approva la seguente mappa. Questa dichiarazione conferma presenza e funzione; non dimostra indirizzo, polarità o qualità del dato nel programma installato.

| Porta | Funzione confermata | Riscontro / dato ancora da riconciliare |
|---|---|---|
| X1 | Presenza prodotto | Il sorgente nativo storico decodifica invece PN7094 vuoto su X1. |
| X2 | Temperatura | Identità sensore, modulo configurato, indirizzo, scala e qualità da verificare. |
| X3 | Vuoto | Identità sensore, modulo configurato, indirizzo, scala e qualità da verificare. |
| X4 | Antirotazione distributore | Funzione approvata; bit, polarità e diagnostica non dimostrati dalla mappa funzionale. |
| X5 | Livello serbatoio | Il nativo contiene decodifica LR2050; confermare congruenza con installato e scala. |
| X6 | Zero / riferimento macchina | Confermare sorgente e polarità; non sostituisce l'encoder. |
| X7 | Presenza CIP | Nuovo requisito: valutare esclusivamente quando `Machine.CIPActive = TRUE`. Fuori CIP non deve bloccare JOG. |
| X8 | Riserva | Nessun sensore assegnato. |

Il disegno REV D, pagina PDF 28 / SHEET 43, riporta ancora X1 PN7094 vuoto, P2/P3/P4/P6 spare, P5 LR2050 e P7 temperatura prodotto. È documentazione precedente, in conflitto con la mappa corrente; non prova che i nuovi sensori siano assenti. Riscontro: [testo estratto](../runs/2026-11-11-al102-interlock-analysis/REV_D_extracted.txt), righe 5465–5488; [pagina renderizzata](../runs/2026-11-11-al102-interlock-analysis/REV_D_page28.png).

L'export storico `runs/2026-10-14-configure-al102/port-addresses.txt:24` descrive X7 come `X7_ProcessTemperature_TA2405`, modulo `IDS IO-Link 2I`, `Input:370:24`. Non utilizzare questo indirizzo come ingresso booleano CIP senza riconciliazione. Il rapporto `runs/20261109-190311-plc-alarm-diagnosis/report.md:19–22` documenta il 9 novembre un evento di componente mancante per quel modulo, Rack 0 / Slot 1.8: è un fatto storico, non una diagnosi corrente.

Il log `runs/2026-10-14-al102-x2-x5-configure.log:3` propone/genera X4 `SPARE_SIO_DI`, `Input:360:8`, ma termina con errore: la persistenza di quell'assegnazione **non è dimostrata da quel log**. Non è un indirizzo approvato per l'antirotazione. `REV12/Documentation/REV20_Electrical_Workshop_Wiring_Instruction.md:9` mantiene esplicitamente aperta l'approvazione degli indirizzi fisici.

## 3. Catena software realmente documentata

Riferimento nativo più recente esaminato: `runs/2026-11-11-hmi-omron-separation/after/FB_Application.scl`.

| Punto | Evidenza nel file | Effetto |
|---|---|---|
| Interfaccia incompleta | righe 595 e 601 | Flag verifica FALSE e blocco rotazione TRUE costanti. |
| Diagnostica generale | righe 309–332 | Diagnostica master / `IOLinkMaster3OK`; non prova qualità e significato X4/X7. |
| JOG comune | righe 1020–1033 | Consensi manuali e `ProcessInterlockBlocked`; blocco comune a HMI e Omron. |
| Ultima interdizione | righe 1617–1626 | Azzera MainRun e MainSpeedPct dopo i writer e prima della pubblicazione drive. |

Dettaglio letture/scritture del nativo (le righe sono SCL, non numeri network):

| Riferimento | Accesso e dipendenze |
|---|---|
| FB13 25, 69, 74, 122–125 | Dichiarazioni istanza diagnostica AL102, stato comunicazione e temporanei decodifica. |
| FB13 209–220 | Lettura IOL_AL102_P1_RAW, decodifica vuoto e validità da stato master/range: conflitto con nuova funzione X1 presenza prodotto. Stato diagnostico aggiornato successivamente nel ciclo. |
| FB13 222–229 | Lettura IOL_AL102_P5_RAW e conversione livello LR2050; non identifica X4/X7. |
| FB13 309–312, 332 | Chiamata diagnostica con al102~IODevice; scrittura AL102ProfinetOK/retcode e Inp.IOLinkMaster3OK. |
| FB13 595, 601 | Scrittura ciclica incondizionata FALSE / TRUE dei due hold point. |
| FB13 610, 625–626, 636 | Lettura per AL102MappingMissing, richiesta Omron e diagnostica PhysicalCommandsDisabled. |
| FB13 1031–1033 | Scrittura ProcessInterlockBlocked = NOT verifica OR inibizione. |
| FB13 1049, 1052 | Letture ProcessInterlockBlocked per HmiAccepted e OmronAccepted. |
| FB13 1570 | Lettura IOLinkMaster3OK come AL102CommOK al supervisore. |
| FB13 1617–1626 | Lettura dei due hold point nel clamp finale, insieme a SafetyOK, SimulationMode, JOG manuale e altezza. |
| DB_PendantCommissioning 8–9, 20, 31 | Dichiarazioni e inizializzazione RotationInhibited TRUE; verifica Bool inizialmente FALSE. |
| DB_ManualJogSources 10, 23 | Dichiarazione e inizializzazione ProcessInterlockBlocked TRUE. |
| DB_Global salvato, righe 32, 189 | Struttura AL102 dichiarata; OpenPoints.AL102.Required inizializzato TRUE. Non implica mapping X4/X7 compilato. |
| FB_OpenPointsSupervisor, righe 50, 157–172, 255 | Ingresso AL102CommOK; CommunicationOK/Ready da Configured e comunicazione generale, poi consenso READY. Non diagnostica individualmente X4/X7. |

Riferimenti REV12 da non confondere con programma installato: UDT tipi righe 1–49 (porte e stato processo); DB_Global 37–38, 203–209 (dichiarazioni, Required, inizializzazioni); FB_Application 308–332 (chiamata e segnali normalizzati), 582–586 (STOP/JOG/rotazione manuale), 666–669 (CIP), 839–842 (allarmi), 866–871 (stati), 1164 (richiesta produzione), 1240/1472 (bench cap/JOG), 1276 e 1562 (clamp); FB_MainState 20/208 (ingresso/consenso rotazione). FB_AL102ProcessInterlocks 38–42/53–57 (validità), 75/82 (valori), 86–121 (stop/latch/inibizione), 123–142 (CIPStartPermitted/CIPReady), 144–158 (reset). L'inventario allegato contiene ogni riga corrispondente nei 48 file esaminati, incluse dichiarazioni/commenti: non equivale a cross-reference completo di blocchi nativi non esportati o della CPU.

I riferimenti `%X4` ai bit valvole e `%X7` al reset del control word VSD non sono le porte AL102 e non vanno confusi con esse.

In `REV12/PLC_Sources/10P_FB_AL102ProcessInterlocks.scl`, righe 38–42 e 53–57, la validità richiede comunicazione master, hardware verificato, porta configurata, qualità buona e assenza fault dispositivo. `LiveBool` è un valore normalizzato, non una prova di polarità elettrica. La versione REV12 alle righe 113 e seguenti include X7 nel blocco globale: **non soddisfa il nuovo requisito X7 solo durante CIP** e non va importata integralmente.

Per X4, righe 86–90, la perdita durante rotazione richiede stop e memorizza il guasto. Per X7, righe 106–108, la logica storica memorizza la perdita durante CIP dopo presenza riconosciuta. Anche reset, latch, `CIPReady` e transizioni CIP richiedono riconciliazione: non deve rientrare indirettamente un consenso X7 nel JOG fuori CIP.

## 4. Modifiche proposte, non applicate

[Frammenti di proposta non importabile](../runs/2026-11-11-al102-interlock-analysis/AL102_PROPOSAL_NOT_APPLIED.scl.txt).

1. Confermare configurazione corrente e sorgenti cicliche X4/X7; normalizzare valore e validità prima del blocco interlock. Dato perso o scaduto deve diventare non valido, senza conservare l'ultimo TRUE.
2. Integrare selettivamente struttura diagnostica e blocco AL102; sostituire le due costanti soltanto con risultati verificabili. Comunicazione PROFINET OK non equivale a configurazione o sicurezza validata.
3. Mantenere X4 e sicurezza come condizioni comuni. Applicare validità, presenza e latch X7 soltanto con `Machine.CIPActive = TRUE`; fuori CIP stato “non applicabile”, non falsa attestazione di sensore valido.
4. Riconciliare l'ingresso nella sequenza CIP: richiedere X7 valido per entrare in CIP mentre la validità viene calcolata solo in CIP crea una dipendenza circolare. Proposta: ingresso in fase CIP a macchina arrestata, verifica X7, poi consenso ai passi di lavaggio; dettaglio da approvare e provare.
5. Conservare separazione sorgenti HMI/Omron, consensi reali, esclusione altezza/rotazione e clamp finale. Nessun bypass SimulationMode/BenchCommissioningMode.

## 5. Regressioni JOG da controllare prima del rilascio

- `CommonManualPermit` (nativo righe 1020–1029) e clamp finale non includono esplicitamente `EffectiveCmd.Stop`. Proposta: aggiungere lo stop operativo ai consensi e alla revoca finale, collegando anche la richiesta stop AL102. Non confondere STOP operativo e arresto sicuro.
- La separazione HMI/Omron conserva la formula bench `ProductionSpeedBPH / MaxSpeedBPH * 100`, diversa dal ramo storico non simulato con `Par.JogSpeedPct` e limite `MainJogMaxFrequencyHz/MainDriveMaxFrequencyHz`. Va riconciliata con il riferimento installato; nessuna nuova frequenza è ricavata da 3.000 bottiglie/ora.
- `SafetyOK` reale e `HMILinkOK` sono condizioni effettive; non ripristinare alternative simulate per far muovere il carosello. Il trasferimento tracking passa da `JogTrackingValid` e viene ripubblicato fuori simulazione (righe 1065–1083): verificare il confronto con il comportamento installato e la ripartenza.
- Definire e provare riarmo quando ritornano i consensi con pulsante ancora premuto, cambio MANUAL/AUTO, perdita comunicazione e richieste simultanee.

## 6. Cosa manca per un rilascio dimostrato

| Verifica | Stato / evidenza necessaria |
|---|---|
| Presenza sensori e funzioni | Confermata dall'operatore; mappa sopra vincolante. |
| Moduli e indirizzi attuali X4/X7 | Confronto configurazione salvata/installata; byte, bit, tipo porta/PDO e stato qualità. Nessun indirizzo nuovo inventato. |
| Polarità | Correlazione stato fisico / valore letto / qualità, distinguendo healthy X4 ed engaged X7. |
| Perdita segnale | Invalidazione reale su cavo/dispositivo/comunicazione persi; prova controllata separata, non effettuata in questa analisi. |
| PILZ / sicurezza | Percorso e funzione di arresto validati separatamente; nessun ingresso PILZ X4/X7 è dimostrato dalle evidenze citate. `SafetyInterlockValidated` non sostituisce il consenso PILZ dinamico. |
| Software installato | Confronto con copia ed evidenza della versione effettivamente in CPU. Compilazione offline non dimostra equivalenza. |
| Proposta | Revisione, applicazione autorizzata su copia e compilazione ancora necessarie; nessuna abilitazione fisica attestata. |

Inventario dei riferimenti: [CSV](../runs/2026-11-11-al102-interlock-analysis/AL102_reference_inventory.csv), 180 riferimenti su 48 file nel perimetro analizzato; hash iniziali in [source-hashes-before.json](../runs/2026-11-11-al102-interlock-analysis/source-hashes-before.json). Non è una prova di funzionamento online, FAT o validazione di sicurezza.
