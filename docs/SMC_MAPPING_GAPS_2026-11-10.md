# Verifica PLC → Anybus → SMC — 10 novembre 2026

Verifica offline di disegni, sorgenti ed evidenze del progetto. Nessun download,
azionamento, modifica di indirizzi o configurazione. La lista del proprietario
conferma le funzioni delle undici valvole, non i canali delle bobine.

## Risultato documentale

Il percorso previsto è PLC PROFINET → ABC3113-A → EtherCAT → EX260-SEC1.
Il report TIA del 21 settembre registra SMC_OUTPUT_4B, slot 2, uscita iniziale
128, lunghezza 32 bit. Il GSDML descrive quattro ottetti. Questo non dimostra
il trasferimento interno del gateway né il cablaggio del manifold installato.

Il sorgente esportato dal progetto recuperato scrive
`Y_SMC_EX260_OutputImage := SWAP(DB_Global.Out.ValveBits)`.
L'audit tag storico del 14 ottobre associa quel simbolo a DWord `%QD128`.
La verifica corrente nella copia TIA recuperata conferma direttamente:

- PLC tags → SMC EX260 Outputs: `Y_SMC_EX260_OutputImage`, DWord, `%QD128`.
- Device view → anybus-smc: `SMC_OUTPUT_4B`, rack0, slot2, Q address128…131.

Le schermate `native-smc-output-tag.png` e `native-anybus-output-module.png`
sono conservate in `runs/2026-11-10-smc-mapping-audit/`. È configurazione offline
del progetto: non una lettura della configurazione installata nel gateway.
Nessun parametro è stato modificato e non è stata necessaria ricompilazione.
I tre hash dei file AP19 originali protetti restano invariati.

## Conflitti nei disegni effettivamente controllati

| Documento | Pagina PDF / foglio | Evidenza e limite |
|---|---|---|
|`_collegamento componenti definitivo/SCHLENKER_3610_v06_COMPLETE_ELECTRICAL_REDLINE_REV_D.pdf`|31 / 8001-46|Dieci corpi: EV010, EV011, EV210, EV211, EV212, EV220, EV221, Bottle Wash, EV001 Open/Close, SPARE. Mancano la numerazione attuale 125Y1–11 e la tabella bobina/canale. Chiusura separata e spare sono in conflitto con la conferma attuale.|
|Stesso documento|25 / 8001-40|Architettura ABC3113-A PROFINET da SCALANCE, EtherCAT verso SMC; non è una tabella PDO/bobine.|
|`eletrical drawing/SCHLENKER 3610 v05.pdf`|29 / 8001-46|Stessa rappresentazione storica dei solenoidi.|
|`_collegamento componenti definitivo/REV12_Project_Tree_Architecture_X8_CORRECTED.pdf`|11|Vecchia assegnazione Y1–Y8 ai bit pari, Y9 doppia bobina, Y10 spare. Y6 distributore e Y8 lavaggio: contraddice le funzioni definitive. Non usare come cablaggio approvato.|
|`_collegamento componenti definitivo/REV12_Definitive_PLC_IO_and_Cabinet_Basis.pdf`|1–2, 6|Descrive ancora otto VQC1100N-51, rimanda allo schema pneumatico per le bobine e riporta `%QD128`. Non documenta undici stazioni installate.|

Non è stato trovato uno schema pneumatico aggiornato nelle ricerche eseguite;
questo non prova che non esista altrove. Anche il doppio caricamento a doppia
bobina richiede identificazione fisica: non è stato assegnato per supposizione.

## Percorso software dimostrato e limite degli indirizzi

| Comando ereditato | Bit di ValveBits | Indirizzo PLC dedotto con SWAP DWord e binding `%QD128` |
|---|---:|---|
|ProductInlet / EV010|0|Q128.0|
|ProductClose / EV011|2|Q128.2|
|EV210|4|Q128.4|
|EV211|6|Q128.6|
|EV212|8|Q129.0|
|CapDistributorAir / EV220|10|Q129.2|
|CapChannelAir ritirato|12, FALSE|Q129.4 inattivo|
|ExternalBottleWashPneuEnable|14|Q129.6|
|GateOpen / EV001|16|Q130.0|
|GateClose ritirato|17, FALSE|Q130.1 inattivo|

Questi sono otto comandi attivi ereditati, non undici assegnazioni fisiche.
SWAP inverte i byte, non i bit dentro ogni byte; la deduzione sopra non
dimostra che il gateway conservi questo ordine fino alle bobine.
Riferimento Siemens per la semantica: [SWAP](https://docs.tia.siemens.cloud/r/simatic_s7_1200_manual_collection_enus_20/basic-instructions/move-operations/swap-swap-bytes?contentId=75FH3yOsv3P4w7EJzlgHfQ).

Fonti software: `REV12/PLC_Sources/09_FB_SMC_ValveManager.scl`, righe 31–57;
`runs/2026-11-10-tia-recovery-smc/FB_Application_smc_recovery.scl`, righe
1327–1338, 1350–1362 e 1627–1630; audit storico
`runs/2026-10-14-sensor-cross-reference-audit.txt`, riga 17.

## Undici funzioni: cosa manca per ciascuna

Per tutte le righe mancano il collegamento verificato al canale EtherCAT,
la bobina A/B e la corrispondenza alla pneumatica installata.

| Valvola | Funzione confermata | Comando/associazione da risolvere |
|---|---|---|
|125Y1|Infeed Vacuum|La vecchia associazione EV010 è ProductInlet, comandata automaticamente da ProductPumpRun: non prova una funzione vuoto.|
|125Y2|Vacuum|EV011 è ProductClose, complementare alla pompa in determinate fasi e in mutua esclusione con EV010. Associazione al vuoto da dimostrare.|
|125Y3|Drainage|EV210 era Vacuum After Pump: comando di drenaggio e sequenza non riconciliati.|
|125Y4|Return Dummy Bottle CIP|EV211 era Vacuum In Pump: ritorno bottiglia dummy CIP non riconciliato.|
|125Y5|Bypass|EV212 era Vacuum Before Pump: bypass non riconciliato.|
|125Y6|Infeed Product|Nessun collegamento operativo dimostrato dal target logico Y6 alla bobina. Non assegnare automaticamente EV010 o bit10.|
|125Y7|Spray CIP|Nessun comando/canale fisico dimostrato. Il vecchio bit12 CapChannelAir è ritirato.|
|125Y8|Cap Distributor Air|EV220/bit10 è il percorso software candidato per funzione; associazione alla stazione8 e bobina ancora da provare.|
|125Y9|Infeed Gate Open|GateOpen/bit16 candidato; verificare anche il percorso diretto Q4.3. Una sola bobina di apertura, ritorno a molla; nessuna nuova chiusura.|
|125Y10|Cap Opening|Mancano comando operativo, sequenza e canale. Il nuovo campo di sorgente è FALSE e non va confuso con il vecchio filler wash. Una bobina di apertura, ritorno a molla.|
|125Y11|External Washing|Il comando ereditato BottleWash/bit14 richiede conferma che copra davvero questa funzione e questa bobina.|

I campi SMC125.Y8–Y11 aggiunti nel repository sono ancora metadata sorgente,
non nuove uscite del progetto nativo. La matrice Y1–Y7 non dimostra un percorso
alle bobine: non sono state trovate letture di questi target che costruiscano
ValveBits, e l'export nativo recuperato non contiene SMC125/SMC125Matrix.

## Dati precisi necessari per chiudere il mapping

1. **Manifold installato:** codice d'ordine e Wiring Specification Sheet;
   direzione di numerazione dal modulo SI; codice di ogni valvola; bobina A/B;
   numero uscita assegnato; funzione e collegamento dei tubi. Identificare le
   stazioni del doppio caricamento senza alterarne il funzionamento.
2. **Configurazione Anybus effettiva:** export/revisione/firmware; offset e
   lunghezza dati PROFINET; slave EtherCAT e relativa revisione; ESI utilizzato;
   PDO di ricezione, ordine/bit-length/offset di destinazione; eventuali swap.
3. **Confronto end-to-end:** ValveBits → SWAP → QB128–131 → dati gateway → PDO
   → canale EX260 → bobina. Il punto3 non si risolve dai soli nomi HMI.
4. **Comandi e sequenze:** riconciliare i vecchi EV con tutte le nuove funzioni,
   soprattutto Y1–Y7, definire Y10 e chiarire lo scopo del lavaggio Y11.
5. **Percorsi diretti:** l'export nativo scrive ancora tag generati per GateOpen/
   GateClose (righe1594–1595) e altri EV (1600–1606). L'audit storico associa
   il cancello a Q4.3/Q4.4. Verificare quali morsetti sono realmente cablati;
   non sono state eliminate uscite per supposizione.
6. **Diagnostica:** dati EtherCAT/manifold realmente disponibili, watchdog e
   comportamento configurato in perdita comunicazione. La sola salute PROFINET
   non dimostra salute EtherCAT o corretto stato delle valvole.

Il [manuale SMC EX260 EtherCAT](https://static.smc.eu/binaries/content/assets/smc_global/product-documentation/operation-manuals/en/om_ex260_ethercat_en-f.pdf),
pp.19–20 stampate, descrive 32 uscite e cablaggio standard A/B con B saltata
per le monobobina, ma ammette cablaggio personalizzato. Questo rende necessaria
la specifica del manifold effettivo. Il [prodotto HMS ABC3113-A](https://www.hms-networks.com/p/abc3113-a-anybus-communicator-ethercat-maindevice-to-profinet-io-device)
conferma i ruoli EtherCAT MainDevice e PROFINET IO-Device, non la configurazione installata.

## MISSING CONFIG: significato effettivo

PhysicalMapValidated resta FALSE ma non è letto come consenso globale nel
percorso ValveManager esaminato. GatewayConfigured/MappingConfigured sono TRUE
nei default sorgente; SmcEtherCATOK e ManifoldOK restano FALSE. L'uscita usa
ValveBits, con altri consensi di sicurezza/comunicazione/allarmi. Quindi i
pulsanti HMI disabilitati e MISSING CONFIG non equivalgono a un blocco globale
delle uscite PLC. Nessun flag è stato forzato per nascondere il problema.

Questa verifica non autorizza attivazioni: documenta ciò che manca prima di
una successiva prova fisica specificamente autorizzata.

## Preparazione dell'interfaccia logica offline

La richiesta successiva del proprietario autorizza undici comandi logici
indipendenti, senza inventare i canali EX260. Sono stati preparati:

- `REV12/PLC_Sources/10R_SMC_LogicalTest.scl`: nuovo DB e nuova FC che scrivono
  esclusivamente nel DB dell'interfaccia logica. Nessuna scrittura Q, ValveBits
  o comando EV esistente. La chiamata ciclica deve essere integrata nella copia
  nativa e compilata; il solo file sorgente non dimostra questa integrazione.
- `REV12/HMI/SMC_LogicalTest_Tags.csv`: specifica di 67 tag, non un export nativo
  TIA e non prova di importazione.
- `REV12/HMI/SMC_LogicalTest_ChannelAssignment.csv`: undici funzioni esatte,
  canale -1 (non assegnato), bobina non verificata, nessuna uscita abilitata.
- `REV12/HMI/SMC_LogicalTest_Bindings.json`: eventi e stati da applicare agli
  oggetti esistenti, conservando layout, navigazione e reset legacy.

L'accettazione logica richiede pagina attiva, abilitazione esplicita, heartbeat
HMI valido, diagnostica PROFINET reale, Manuale, produzione/run-out/CIP non
attivi, sicurezza, aria, porte e assenza dei blocchi d'allarme. Una sola
richiesta è ammessa. Perdita dei consensi o richieste multiple impongono il
rilascio di tutti i comandi prima del riarmo. Questo è un consenso software
conservativo, non una funzione di sicurezza certificata.

Gli stati sono IDLE, INTERLOCKED, RELEASE, ACCEPTED e MULTIPLE. ACCEPTED
significa richiesta logica accettata, mai posizione della valvola o conferma
della bobina. NO DATA deve dipendere dalla qualità dei tag e dal collegamento
HMI; il valore numerico zero resta uno stato valido. La salute PROFINET non
viene presentata come salute EtherCAT. Enable ON non deve dipendere da Permit,
perché Permit richiede già Enable; ciò creerebbe un blocco circolare.

La revisione indipendente ha verificato i riferimenti ai campi del sorgente
nativo recuperato e l'isolamento delle scritture nel nuovo DB. Il modello
indipendente di transizione ha superato 8192 combinazioni e 11 sequenze di
perdita/ripristino consenso; non sostituisce compilazione TIA o collaudo.

### Anomalie ereditate da non confondere con il nuovo blocco

L'export recuperato forza SimulationMode a TRUE (riga283), rende SMCCommOK
vero anche attraverso la simulazione (339), usa ManualSMCTestActive prima
della sua assegnazione (1016 contro1275) e riscrive ValveBits nel ramo di
simulazione dopo ValveManager (1347–1363). Questo ramo può aggirare i consensi
del manager. Il nuovo blocco non usa questi bypass, ma non corregge né
inibisce globalmente il percorso ereditato. Prima di qualsiasi futura
abilitazione fisica occorre riconciliare anche quel percorso e le uscite
dirette legacy. Nessuna dichiarazione di pronta attivazione deriva dalla
sola preparazione dell'interfaccia logica.

### Esito del primo tentativo nativo (superato dalla ripresa seguente)

La copia TIA corretta è stata selezionata e 306 file sono stati copiati nel
backup precauzionale del run `2026-11-10-smc-logical-test`. Non è un archivio
transazionale con recupero collaudato. L'apertura di Add new external file è
riuscita, ma il percorso digitato tramite Computer Use è stato sostituito da
testo estraneo già presente negli appunti. Ripristino della sessione, nuova
selezione e input tramite tasti non hanno prodotto un valore verificabile.
La finestra è stata annullata e la chiusura verificata.

Nessun sorgente importato, nessun blocco generato, nessuna chiamata ciclica
inserita, nessun collegamento HMI modificato, nessuna nuova compilazione.
I tre hash AP19 originali protetti sono invariati. Evidenze:
`runs/2026-11-10-smc-logical-test/native-attempt-report.txt`,
`import-input-state.png` e `source-after-native-attempt.json`.

Stato: **preparazione sorgenti parziale; integrazione nativa non completata**.
Il problema dei canali sconosciuti è rappresentato esplicitamente dalla
tabella non assegnata; non impedisce la preparazione logica richiesta.
Il blocco attuale all'applicazione è l'input UI non verificabile. Non è stata
dichiarata pronta al collaudo la configurazione né effettuato alcun download.

### Ripresa nativa del 10 novembre 2026

Nella copia `runs/2026-11-10-tia-recovery-smc/recovered-work-copy` sono stati
generati `DB_SMC_LogicalTest` e `FC_SMC_LogicalTest` e aggiunta una sola chiamata
ciclica a fine `FB_Application`, dopo il percorso di uscita preesistente.
La ricompilazione completa del software PLC termina con **0 errori, 0 avvisi**;
la ricompilazione hardware con **0 errori, 8 avvisi**. Il progetto è salvato.
Le evidenze sono `plc-software-rebuild-success.png`, `plc-hardware-rebuild.png`
e `plc-success-report.txt` nel run `2026-11-10-smc-logical-test`.
I tre AP19 originali protetti conservano gli hash iniziali.

Sette avvisi indicano che la sostituzione senza supporto rimovibile richiede
la topologia per al100–al104, anybus-smc e mes-profinet; l'ottavo segnala
`PLC_1 does not contain a configured protection level`. Il testo completo
osservato è conservato in `plc-hardware-warnings-exact.txt`. Nessuna proprietà
hardware o protezione è stata modificata per sopprimere questi avvisi.

L'operatore ha autorizzato esplicitamente Siemens TIA Openness esclusivamente
per applicare i 67 tag e gli 11 controlli HMI sulla copia offline. Un backup
completo precede l'applicazione. Il primo preflight si è fermato prima delle
modifiche perché non trova entrambi gli eventi Loaded/Unloaded attesi.
Questo era un problema di adattamento del preparatore al progetto reale,
non una prova di configurazione fisica mancante. Dopo l'audit sono stati
creati gli eventi mancanti, conservando gli eventi di navigazione esistenti.
I gruppi Y6/Y7 sono stati identificati dalle etichette reali, non dai vecchi
nomi interni: Y6 usa MappingTbc_*_1 e Y7 MappingTbc_*.

L'applicazione HMI è ora salvata: **67 tag collegati al DB logico, 11 comandi
momentanei e diagnostica con controllo qualità**, verificati anche tramite
lettura API successiva. Compilazione WinCC Unified: **0 errori, 0 avvisi**.
Gli eventi Loaded/Unloaded azzerano le nuove richieste e l'abilitazione.
I comandi riutilizzati non attivano più i vecchi tag EV: scrivono soltanto
SMC_Y1_Request…SMC_Y11_Request. Per Y6/Y7/Y10 sono stati aggiunti controlli
logici nelle posizioni confermate della griglia, senza muovere i gruppi.
I tre testi segnaposto ormai obsoleti sono stati svuotati dopo aver verificato
che i rispettivi oggetti fossero già nascosti/disabilitati. La nota generale
mantiene esplicita la verifica fisica pendente. Anche dopo questa pulizia
la compilazione HMI termina con **0 errori, 0 avvisi**, progetto salvato.

Report nativi: `hmi-openness-report.txt`, `hmi-application-final-console.txt`,
`hmi-placeholder-cleanup-report.txt` nel run. I 134 test degli script preparati
sono passati, ma non sostituiscono un test runtime. NO DATA a runtime dipende
dalla qualità/collegamento; il testo statico nell'editor è solo fallback.
ACCEPTED resta accettazione logica, non conferma fisica.

La tabella `REV12/HMI/SMC_LogicalTest_ChannelAssignment.csv` contiene tutte
le 11 identità approvate, alias HMI e campi PLC. Tutti i canali restano -1,
PhysicalVerified e PhysicalOutputsEnabled FALSE per la nuova interfaccia.
Il percorso generale %QD128 → ABC3113-A → EX260 esistente non è stato
riassegnato. Non è quindi pronta l'attivazione pneumatica: restano da
confermare canali/bobine/PDO e da riconciliare il percorso automatico legacy
descritto sopra. Nessun download o azionamento è stato eseguito.
