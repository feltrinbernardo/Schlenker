# Pulsantiera Omron — applicazione offline del 11 novembre 2026

## Esito

Applicata la proposta approvata sulla copia separata creata mediante TIA V19 Salva con nome:

`C:\www\Schlenker\runs\2026-11-11-pendant-implementation\Schlenker_Pendant_Offline\Schlenker_Pendant_Offline.ap19`

- PLC software: **0 errori, 0 avvisi**, salvato.
- PLC hardware: **0 errori, 8 avvisi**, nessuna modifica hardware intenzionale.
- Nessun download, ingresso online, comando CPU, forzatura o azionamento.
- HMI non modificata né ricompilata in questa attività PLC.
- Questa copia NON è pronta per abilitare movimenti fisici. Non è una validazione di sicurezza o FAT.

## Modifiche effettivamente importate

1. **FB1 MainState**: rimossa l'eccezione bench precedente al controllo SafetyOK; ingresso altezza da READY subordinato a MANUAL; stati 60/70 abbandonati alla perdita MANUAL verso READY/POWER_UP secondo i prerequisiti già presenti nello stato 50. Interfaccia mantenuta.
2. **FB5 JogPendant**: mantenuta la richiesta senza autoritenuta e i consensi esistenti; durante JOG l'invalidazione tracking prevale su RecoveryComplete simultaneo. Interfaccia mantenuta.
3. **FB13 Application**: richieste JOG/UP/DOWN filtrate in EffectiveCmd; entrambe le chiamate altezza richiedono il consenso comune e lo stato altezza pertinente. Rimossi la forzatura SimulationMode TRUE e i bypass di SafetyOK basati su simulazione. La velocità pendant non deriva più dalla selezione bench/Hz preesistente.
4. **FB28 OpenPointsSupervisor**: la perdita SafetyOK revoca MainRun senza eccezione BenchManualMainJogPermit. La relativa chiamata da FB13 passa FALSE.
5. **Nuovo DB_PendantCommissioning**: diagnostica software separata. I flag di verifica delle sorgenti, abilitatore, selettore, conversione velocità e interblocco AL102 sono mantenuti FALSE ciclicamente. Non sono ingressi fisici inventati e non possono essere dichiarati verificati tramite una semplice scrittura HMI.
6. **Pubblicazione finale FB13**: blocco altezza e rotazione dopo gli arbitrati; consensi reali per i drive; protezione in simulazione delle 41 pubblicazioni dirette preesistenti, senza aggiungere indirizzi/tag fisici. Durante simulazione i telegrammi drive pubblicano il comando OFF1 esistente e riferimento zero; le altre pubblicazioni sono disattivate. Questo non sostituisce PILZ/STO.

## Limite operativo importante

Il nativo esportato non contiene l'equivalente documentato di `AL102Process.RotationInhibited` della REV12. Non è stato importato indiscriminatamente un altro schema di processo. Perciò **RotationInhibited è mantenuto TRUE: questa copia blocca tutta la rotazione del carosello, anche AUTO e homing**, finché non sarà riconciliato l'interblocco AL102 reale.

JOG, UP e DOWN restano disabilitati per le sorgenti fisiche non verificate. I consensi possono essere completati soltanto con una successiva modifica controllata dopo verifica del cablaggio. Non è stata creata una funzione HMI per scavalcarli.

Richiesta registrata: **3000 bottiglie/ora**, corrispondenti cinematicamente a **1,388889 RPM** del carosello a 36 valvole. Il riferimento percentuale validato è zero e la conversione resta non verificata. Nessun Hz operativo è stato inventato.

La prima compilazione segnalava una possibile perdita di precisione sulla divisione costante dei giri carosello. La conversione LREAL_TO_REAL è ora esplicita; la compilazione finale non riporta avvisi software.

## Verifiche e limiti dell'evidenza

- Esportazione fresca dei blocchi dalla copia nativa prima delle modifiche; non usati i vecchi export come base di importazione.
- Riesportati FB1, FB5, FB13 e FB28 dopo importazione: confronto del codice normalizzato corrispondente ai sorgenti revisionati.
- Controllo statico: stessi 41 simboli di pubblicazione, nessuna nuova assegnazione fisica, flag non verificati mantenuti falsi, nessun binding raw JogPB/UP/DOWN residuo nelle chiamate interessate.
- Il successivo FC_SMC_LogicalTest scrive soltanto diagnostica e richieste logiche nel proprio DB; non sovrascrive i comandi fisici. OB1 chiama DB_Application.
- FB_AlarmManager nativo conferma che i timeout altezza sono allarmi critici. Application li legge prima delle chiamate altezza del ciclo corrente: la reazione passa dal ciclo successivo, non è un taglio locale nel FB_HeightMode. Nessun tempo reale di arresto misurato.
- Nessuna prova dinamica eseguita su PLC/PLCSIM o macchina. La compilazione non dimostra il comportamento temporale né il funzionamento del circuito di sicurezza.
- Hash delle esportazioni iniziali invariati. Il rilevamento iniziale dell'intero progetto originale era parziale per file bloccati: 299 file leggibili sono rimasti uguali; il file di sessione `.info` non è più presente dopo Salva con nome. Non si dichiara invariabilità completa del progetto nativo originale. La copia e la sorgente originale hanno percorsi distinti.

## Otto avvisi hardware

Per `al104`, `al102`, `al101`, `al100`, `al103`, `anybus-smc`, `mes-profinet`:

> The device replacement without exchangeable medium function cannot be used for device '[nome]' without configuration of topology.

Per CPU:

> PLC_1 does not contain a configured protection level

Nessuna protezione è stata disabilitata e nessuna topologia modificata per eliminare questi avvisi. Il riepilogo del vecchio helper somma gerarchicamente 16 warning; il conteggio root Siemens è **8 avvisi distinti**, non 16.

## Prima dell'abilitazione fisica

Verificare selettore, contatti UP 7–8 e DOWN 5–6 con comune originale 5–7; distinguere pin/fili da morsetti Omron; verificare separatamente entrambi i canali dell'abilitatore e PILZ. Riconciliare consenso AL102, pendant-connected, validità comunicazione, feedback reale di fermo e drive-ready, finecorsa e sollevamento. Stabilire rapporto meccanico e scala drive, ammissibilità della velocità manuale e comportamento di riarmo dopo ripristino dei consensi. Nessuna nuova logica release-to-rearm è stata aggiunta: da valutare nella validazione prima di abilitare le sorgenti.

## Evidenze locali

Tutte sotto `runs/2026-11-11-pendant-implementation/`:

- `final-software-compile.txt`: compilazione software 0/0 e salvataggio.
- `plc-software-hardware-compile.txt`: software 0/0, hardware 0/8.
- `static-verification.json`, `verify_exports.py`: verifiche delle esportazioni.
- `before/`, `proposed/`, `after/`, `final/`: sorgenti ed evidenze di confronto.
- `copy-saved.jpg`, `final-project.jpg`: copia e risultato visivo della compilazione hardware.
- `prepare_patch.py`: trasformazione circoscritta dei sorgenti esportati.

I file nativi e le evidenze di run sono locali e ignorati da Git. Questo rapporto registra l'esito, non un'approvazione al download.
