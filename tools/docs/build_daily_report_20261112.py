from pathlib import Path
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak
from reportlab.lib.styles import getSampleStyleSheet
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
import pymupdf
r=Path('C:/www/Schlenker'); out=r/'output/pdf/SCHLENKER_RAPPORTINO_2026-11-12.pdf'
s=getSampleStyleSheet();s['BodyText'].fontSize=10;s['BodyText'].leading=14;s['BodyText'].spaceAfter=7
s['Title'].textColor=colors.HexColor('#17365D');s['Heading2'].textColor=colors.HexColor('#17365D')
story=[]
def p(t,style='BodyText'):story.append(Paragraph(t,s[style]))
def title(sub):p('SCHLENKER 36/10','Title');p(sub,'Heading2')
def table(rows,widths):
 t=Table([[Paragraph(v,s['BodyText']) for v in row] for row in rows],colWidths=widths,repeatRows=1)
 t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#e5edf5')),('GRID',(0,0),(-1,-1),.4,colors.HexColor('#bac6d0')),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),('TOPPADDING',(0,0),(-1,-1),5),('BOTTOMPADDING',(0,0),(-1,-1),5)]));story.append(t)
title('Rapportino del 12 novembre 2026 - tempi e risultati')
p('Redatto da Rob (assistente AI) su richiesta dell\'operatore. Orari locali Australia/Sydney, UTC+11. Periodo rendicontato: fino all\'ultima attivita tecnica registrata alle 19:03; esclusa la successiva redazione di questo rapporto.')
p('<b>Intervallo documentato: 06:56-19:03, circa 12 ore e 7 minuti.</b> E tempo trascorso, non una misura di lavoro continuo, ore uomo del team o tempo fatturabile. Il registro contiene eventi, non un cronometro delle attivita. Pause, attese e lavoro parallelo non sono separabili con precisione.')
rows=[['Fascia / durata circa','Attivita e risultato documentato'],['06:56-07:27 / 31 min','Copie, inventario PLC/HMI e compilazione registrata senza errori o avvisi. Simulazione funzionale completa non eseguita.'],['07:27-10:02 / 2 h 35 min','Quattro E-stop nella copia offline, intervento dell\'operatore sui collegamenti, prove parziali e revisioni dei PDF.'],['10:02-10:30 / 28 min','Architettura delle undici porte e individuazione delle uscite di sblocco.'],['10:30-12:07 / 1 h 37 min','Encoder verso CPU, revisione collegamenti e lavoro su BottleInfeed. Nessuna attestazione di conteggio collaudato sulla macchina.'],['12:07-14:41 / 2 h 34 min','Prove E-stop/JOG, confronto USB PILZ, acquisizione programma e diagnosi. Validazione complessiva non conclusa.'],['14:41-16:34 / 1 h 53 min','Verifiche EF1MM; conflitto EDM/sblocco identificato; variante offline con EDM a1.I11 e richiesta a5.I7.'],['16:34-18:03 / 1 h 29 min','Ulteriori prove EDM, reset e singoli canali; verifiche residue ancora aperte.'],['18:03-19:03 / 1 h','Analisi RO3101/EF1MM, interfaccia candidata, identificazione IM5135 e PDF. Derivazioni CPU/AL102 non convalidate.']]
table(rows,[142,369]);p('Le fasce sono una ricostruzione cronologica arrotondata e includono eventuali attese e checkpoint. Non misurano il tempo attivo dedicato esclusivamente a ciascun argomento.')
story.append(PageBreak());title('Errori di Rob e tempo perso')
p('<b>Responsabilita del risultato:</b> non ho completato il collegamento EF1MM definitivo e verificato richiesto. La produzione di documenti e prove parziali non equivale alla conclusione dell\'incarico.')
for h,b in [
('Schema elettrico non riconciliato tempestivamente','Non ho confrontato abbastanza presto schema, programma e cablaggio reale. Lo schema e stato consultato successivamente: non e corretto scrivere che non sia mai stato letto. E corretto riconoscere che il conflitto EDM/sblocco avrebbe dovuto essere affrontato prima.'),
('Riferimenti hardware non aderenti alla macchina','Sono stati utilizzati o mantenuti riferimenti a TM Count e SICK senza averli riconciliati con l\'hardware disponibile e con l\'indicazione dell\'operatore sull\'unico encoder RO3101.'),
('Documenti rifatti prima di chiudere i dati','Ho prodotto diverse revisioni dei PDF lasciando aperti punti essenziali. Questo ha generato rilavorazione, ulteriori controlli dell\'operatore e consegne non utilizzabili come cablaggio finale.'),
('Prove frammentate e difficolta operative','Le prove E-stop, EDM e reset sono state riprese piu volte senza un consolidamento sufficientemente efficace. Le difficolta GUI PILZ hanno richiesto anche interventi manuali dell\'operatore.'),
('Percorsi di ricerca non conclusi','Ho esplorato un\'interfaccia aggiuntiva prima di esaurire l\'analisi dei componenti disponibili. Ho continuato a descrivere verifiche mancanti senza portare a termine la soluzione.'),
('Coordinamento insufficiente','Non ho dato al team un percorso abbastanza chiaro e conclusivo. Le spiegazioni delle difficolta incontrate non giustificano il mancato risultato.')]:p('<b>'+h+'.</b> '+b)
p('Separazione del tempo perso','Heading2')
p('<b>Rilavorazione evitabile:</b> revisioni dovute a dati non riconciliati, riferimenti hardware impropri, ripetizioni non consolidate e tentativi di interfaccia non riusciti. <b>Durata esatta: non misurata.</b> Non attribuisco arbitrariamente un numero di ore a queste voci.')
p('<b>Controlli necessari:</b> EDM, singoli canali E-stop, reset, compatibilita elettrica e comportamento in guasto. Non sono inutili in se; e criticabile il modo frammentato e non conclusivo in cui li ho gestiti. Il registro non consente una ripartizione attendibile delle 12 ore e 7 minuti tra lavoro utile, attese e spreco.')
story.append(PageBreak());title('Rettifiche, attribuzioni e conclusione')
p('Acquisto EF1MM','Heading2')
p('<b>Il consiglio di acquistare l\'EF1MM e attribuito a Rob, non all\'operatore.</b> Questa attribuzione e riportata su dichiarazione dell\'operatore, ribadita e recepita nelle rettifiche concordate in conversazione; non e stata recuperata qui la raccomandazione originaria d\'acquisto.')
p('Prima di consigliarlo avrei dovuto verificare l\'abbinamento con il RO3101, i segnali necessari, gli accessori e la configurazione, fornendo un percorso concretamente realizzabile. Non ho dimostrato di aver completato tale verifica preventiva. Alla chiusura del periodo rendicontato l\'integrazione richiesta non e pronta. Non risulta invece dimostrato che il modulo sia inutilizzabile in assoluto.')
p('Credito dell\'idea del sensore zero','Heading2')
p('<b>L\'idea di valutare il sensore zero macchina gia disponibile per il riferimento aggiuntivo dell\'EF1MM e dell\'operatore. Non e un\'idea di Rob.</b> Il mio contributo successivo e stato il confronto documentale e l\'identificazione del pinout IM5135.')
p('Rettifica della voce sui presunti errori dell\'operatore','Heading2')
p('Ritiro l\'inserimento dell\'esasperazione e delle richieste di accelerare nella categoria "errori dell\'operatore". Le richieste sono arrivate dopo ripetute attese e consegne incomplete. Non dispongo di elementi per attribuire all\'operatore errori di cablaggio accertati nelle attivita qui rendicontate. Le proposte discusse non dimostrano che sia stato realizzato un collegamento errato.')
p('Consegna contestata e impatto sul team','Heading2')
p('La schermata del PDF REV C inviata dall\'operatore mostra ancora derivazioni "da verificare" e collegamenti non rilasciati per il cablaggio, a fronte della richiesta di una soluzione definitiva. E una <b>consegna non rispondente al risultato richiesto</b>. Non va presentata come lavoro concluso. L\'operatore segnala attesa prolungata, frustrazione e forte impatto negativo sul team; le ore uomo e il danno economico non sono stati quantificati.')
p('<b>Conclusione:</b> sono state svolte compilazioni, acquisizioni, prove e attivita documentali, ma il risultato operativo atteso e rimasto incompleto. Rob riconosce una gestione inefficiente e frammentata del lavoro e non attribuisce all\'operatore la responsabilita della mancata conclusione.')
p('Fonti e limiti','Heading2')
p('Registro locale logs/change-log/2026-11-12.md, eventi 06:56:21-19:02:58; conversazione e rettifiche dell\'operatore; schermata PDF REV C allegata. I risultati tecnici sono riportati come registrati, non nuovamente collaudati durante la redazione. Questo rapporto non certifica ore lavorate, FAT, sicurezza o autorizzazione al download.')
def footer(c,d):
 c.setStrokeColor(colors.HexColor('#bac6d0'));c.line(42,32,553,32);c.setFont('Helvetica',8);c.setFillColor(colors.HexColor('#536577'));c.drawString(42,20,'FBS / SCHLENKER - Rapportino attivita 12/11/2026');c.drawRightString(553,20,f'Pagina {d.page}')
SimpleDocTemplate(str(out),pagesize=A4,leftMargin=42,rightMargin=42,topMargin=32,bottomMargin=43).build(story,onFirstPage=footer,onLaterPages=footer)
d=pymupdf.open(out)
for i,page in enumerate(d):page.get_pixmap(matrix=pymupdf.Matrix(1.15,1.15)).save(str(out.with_suffix(''))+f'-{i+1}.png')
print('pages',len(d));print(out)
