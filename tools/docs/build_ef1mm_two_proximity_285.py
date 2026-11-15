from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4, landscape
from reportlab.lib.colors import HexColor
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
import pymupdf
root=Path('C:/www/Schlenker'); out=root/'output/pdf/SCHLENKER_EF1MM_DUE_INDUTTIVI_285_DENTI_PROPOSTA.pdf'
c=canvas.Canvas(str(out),pagesize=landscape(A4)); W,H=landscape(A4)
navy='#173752'; blue='#177AA6'
def text(x,y,s,size=11,bold=False,color=navy):
 c.setFillColor(HexColor(color));c.setFont('Helvetica-Bold' if bold else 'Helvetica',size);c.drawString(x,y,s)
def rect(x,y,w,h,color):
 c.setFillColor(HexColor(color));c.roundRect(x,y,w,h,5,fill=1,stroke=0)
def para(x,y,w,s,size=11):
 p=Paragraph(s,ParagraphStyle('a',fontName='Helvetica',fontSize=size,leading=size*1.35,textColor=HexColor(navy)));a,h=p.wrap(w,1000);p.drawOn(c,x,y-h);return y-h-12
def head(title,n):
 rect(0,H-65,W,65,navy);text(25,H-29,title,18,True,'#FFFFFF');text(25,H-49,'SCHLENKER 36/10 | Proposta tecnica offline | 13 novembre 2026',10,False,'#FFFFFF');text(25,18,'Nessuna modifica nativa, download o prova fisica eseguita in questa verifica.',9);text(W-60,18,f'{n} / 4',9)
def arrow(x1,x2,y,col):
 c.setStrokeColor(HexColor(col));c.setLineWidth(3);c.line(x1,y,x2,y);c.line(x2-7,y+4,x2,y);c.line(x2-7,y-4,x2,y)
head('DUE SENSORI DENTI -> PILZ EF1MM',1)
rect(28,390,235,112,'#E8F0F5');text(42,480,'SENSORE A - ifm IFS240',13,True);text(42,458,'IFK3004BBPKG/US-104',10);text(42,437,'PNP NO | 24 VDC | 700 Hz',11);text(42,416,'M12: pin 1 L+ / 3 L- / 4 OUT',10)
rect(28,255,235,112,'#E8F0F5');text(42,344,'SENSORE B - Pepperl+Fuchs',12,True);text(42,322,'NBB4-12GM50-E2-V1',11);text(42,301,'PNP NO | 24 VDC | 2550 Hz',11);text(42,280,'Scelta proposta; analisi CCF richiesta',10)
rect(602,217,210,290,'#F6D945');text(617,480,'PILZ PNOZ m EF 1MM',14,True);text(617,459,'772170 - asse 1',11)
for yy,label in [(423,'A12 - impulso sensore A'),(289,'B12 - impulso sensore B'),(245,'S12 - monitor tensione')]:text(617,yy,label,10,True)
arrow(263,600,428,'#222222');text(282,440,'Pin 4 OUT A - nero (*)',11,True)
arrow(263,600,294,'#222222');text(282,306,'Pin 4 OUT B - nero (*)',11,True)
arrow(370,600,250,'#8A542F');text(378,263,'+24 V sensori, dopo protezione',10)
rect(28,144,784,53,'#E8F0F5');text(42,177,'DISTRIBUZIONE ALIMENTAZIONE',11,True);text(42,158,'+24 V protetto -> pin 1 di A e B + S12     |     0 V -> pin 3 di A e B + GND EF1MM',11)
para(30,125,780,'(*) Marrone = L+, blu = L-, nero = OUT: colori convenzionali del cordone M12, da riscontrare sul cavo scelto. Due cavi schermati, tre conduttori attivi per sensore. Schermi secondo Pilz/EMC, non usati come 0 V. Pin 2 non utilizzato. S12 e un ingresso di monitoraggio, non alimenta i sensori.',10)
para(30,70,780,'RO3101 -> CPU Siemens invariato. AL102/X6 zero macchina invariato. X12 Mini-IO non utilizzato da questa proposta: non collegare gli stessi segnali contemporaneamente anche a X12.',10)
c.showPage()
head('POSIZIONAMENTO SUI 285 DENTI',2)
# schematic gear with sensors, no scale
import math
cx,cy,rad=177,339,95
c.setStrokeColor(HexColor('#667989'));c.setLineWidth(8);c.circle(cx,cy,rad,stroke=1,fill=0)
for a in range(0,360,12):
 q=math.radians(a);c.line(cx+rad*math.cos(q),cy+rad*math.sin(q),cx+(rad+10)*math.cos(q),cy+(rad+10)*math.sin(q))
rect(30,324,38,30,'#E8A23B');rect(287,324,38,30,'#E8A23B');text(30,305,'A',12,True);text(300,305,'B',12,True);text(122,347,'285 DENTI',14,True);text(130,326,'180 gradi',12)
text(35,214,'Vista schematica, non in scala',10)
y=500
for s in ['<b>Proposta meccanica: sensori diametralmente opposti.</b> Con 285 denti uniformi, 180 gradi corrispondono a 142,5 passi: A e B risultano sfasati di mezzo passo dente.', 'Passo angolare = 360/285 = <b>1,263158 gradi</b>. Mezzo passo = <b>0,631579 gradi</b>. Alternativa: separazione (k + 0,5) passi. La distanza in mm richiede il diametro al raggio di lettura: p = pi x D / 285.', '<b>Requisito Pilz:</b> almeno un sensore sempre attivo e sovrapposizione maggiore dell\'1% del periodo. Sequenza attesa PNP/PNP: 10 -> 11 -> 01 -> 11 -> 10. Non accettare intervalli 00 o due segnali identici.', 'Con mezzo passo e forme d\'onda simmetriche, una durata ON >51% del periodo fornisce >1% di sovrapposizione su ciascuna transizione. E un criterio geometrico derivato, non un valore gia misurato: verificare entrambi i canali con oscilloscopio.', '<b>Distanza dal dente:</b> 4 mm e nominale; IFS240 riporta distanza operativa 0...3,24 mm sul bersaglio normalizzato. Denti reali, materiale, fondo cava, eccentricita e vibrazioni modificano la commutazione. Regolare staffe rigide e verificare OFF sulla cava e ON sul dente lungo tutto il giro.']:
 y=para(355,y,455,s,10.5)
para(30,180,780,'I due sensori non devono potersi spostare insieme rispetto alla ruota. Proteggere da urti, trucioli e metallo estraneo; evitare accoppiamento elettromagnetico. La disposizione opposta facilita la separazione fisica, ma non elimina i guasti di causa comune. Non specificare un interasse in mm senza geometria reale.',11)
para(30,110,780,'Se la forma dei denti non consente la sovrapposizione, non selezionare diagnostica ridotta per aggirare il problema. Correggere il bersaglio o la geometria e riesaminare la soluzione prima del cablaggio esecutivo.',11)
c.showPage()
head('CONFIGURAZIONE E CONSENSO PORTE',3)
y=510
items=[('Ingresso motion','Due proximity switch PNP/PNP, diagnostica standard; frequenza massima comune 700 Hz (sensore piu lento). 285 impulsi/giro per canale, un dente = un periodo; non 570 e non conteggio x4. Riferimento direttamente sul carosello: conversione meccanica 1:1.'),('Funzione','Usare SSM a bassa soglia per rilevare il sottosoglia. SOS-M, SDI-M e SS2-M non sono utilizzabili con due proximity switch (Pilz p13). Non denominare il consenso come prova di zero matematico.'),('Limite inferiore','Campo monitorabile configurabile: 0,1 Hz...5 kHz. A 285 denti, 0,1 Hz equivale a 0,021053 rpm e 0,126316 gradi/s. Una deriva piu lenta puo essere sotto soglia: l\'ammissibilita deve risultare dalla valutazione del rischio. 0,1 Hz e un candidato di studio, non una taratura rilasciata.'),('Tempi e diagnostica','Pilz indica reazione al superamento 1/f reale + 16 ms, cui aggiungere base, uscite e attuatori. A 0,1 Hz il periodo e 10 s: non promettere una reazione immediata ne identificare 10 s come tempo certo di rilascio. Definire isteresi, frequenza cut-off di plausibilita e tolleranze; sotto il cut-off la plausibilita non viene eseguita.'),('Alimentazione','Attivare monitor S12 sullo stesso ramo protetto che alimenta i sensori. Definire limiti compatibili con alimentatore e ingressi, senza ignorare caduta cavo/uscita. Sensore 10...30 V; EF1MM ingresso HIGH 11...30 V. Tolleranze diagnostiche aggiunte aumentano la reazione.'),('Separazione JOG','Il vecchio CP4 E100_ZS era condiviso con la base JOG. Rimuovere tale riuso nel nuovo progetto: il consenso di arresto serve allo sblocco, non abilita JOG a porte aperte. L\'esclusione della velocita ridotta richiesta non autorizza ad eliminare un interblocco necessario al rischio.'),('Sequenza di accesso','Richiesta sblocco -> arresto e inibizione sicura del riavvio -> consenso SSM valido + diagnostica sana + condizioni safety previste -> sblocco. Uscite a5.O0/O50, O1/O51, O2/O52, O3/O53 restano negate con dati invalidi. Conservare E-stop, EDM a1.I11 e reset; a5.I7 resta sola richiesta operativa.'),('Avviamento / ripristino','Non considerare due ingressi statici come sensori provati. Definire controllo di avviamento/prova dinamica, guasti a 0/24 V, incrocio canali e perdita alimentazione. Un timer non sostituisce il consenso safety. Nessun riavvio alla semplice richiusura delle porte.')]
for title,s in items:y=para(28,y,785,'<b>'+title+':</b> '+s,10)
c.showPage()
head('DISTINTA, EVIDENZE E VALIDAZIONE',4)
y=510
for s in ['<b>Componenti:</b> mantenere EF1MM 772170 e RO3101 collegato alla CPU. Sensore A disponibile: IFS240. Sensore B proposto: Pepperl+Fuchs NBB4-12GM50-E2-V1, PNP NO, 6...36 V, 4 mm, 2550 Hz (scheda 326161-0001). Fabbricante diverso secondo la raccomandazione Pilz; analisi CCF ancora necessaria. Alternativa elettrica: secondo IFS240 con giustificazione CCF. Nessuno dei due e un dispositivo safety autonomo.', '<b>Accessori:</b> due cordoni M12 femmina A-coded schermati con estremita libera e tre fili attivi, lunghezza secondo percorso; due staffe regolabili antirotazione e schermature/serracavi. Riutilizzare quelli disponibili se idonei. Nessun Mini-IO necessario. Cordone candidato: ifm EVC539, 5 m PUR schermato, M12 A-coded 4 poli / fili liberi, IP65/IP67. Acquistare due pezzi solo se lunghezza e lavaggio lo consentono; pin 2 non usato, colore bianco isolato. Non attribuire al cordone IP69K. Resistenze 10 kOhm sono illustrate da Pilz per linee PNP lunghe: non obbligatorie per principio, valutare segnali e cavo.', '<b>Cablaggio preesistente:</b> le foto mostrano quattro fili viola marcati A12/B12/S12/GND dal modulo alla morsettiera omonima; lato campo appariva vuoto. X12 era dichiarato vuoto. Sono evidenze storiche, non misure di continuita attuali. Prima di intervenire, confermare a quadro isolato i quattro percorsi, assenza di ponti/alimentazioni estranee e stato fisico del modulo.', '<b>Livello raggiungibile:</b> manuale EN-11 pp58-59: due proximity, funzione SSM, fino a PL e Cat.4 / SIL CL3 alle condizioni indicate. Non e il risultato automatico della macchina. Mancano PLr/SIL richiesto dalla valutazione rischi, MTTFd sensori, CCF, dati attuatori/serrature e calcolo completo. SOS-M/SS2-M non hanno livello dichiarato per questa combinazione.', '<b>FAT:</b> misurare forme d\'onda in entrambi i sensi e alle velocita minima/massima; provare sensori aperti, bloccati ON/OFF, corto comune, perdita 24 V e GND, perdita canale, distacco staffa e restart. Richiedere accesso in marcia e in deriva lenta; misurare movimento residuo e tempi fino allo sblocco. Validare prevenzione riavvio, E-stop e ritorno EDM. Nessun PASS fisico ricavabile dalla sola simulazione.', '<b>Calcoli:</b> f = 285 x rpm /60. A 3000 bottiglie/ora e 36 valvole: 1,388889 rpm e 6,597222 Hz. Limite teorico sensore 700 Hz = 147,37 rpm: non e una velocita meccanica autorizzata. Verificare frequenza massima reale e risoluzione spaziale (un passo = 1,263158 gradi).']:
 y=para(28,y,785,s,10.2)
text(28,113,'FONTI E STATO DELLA CONSEGNA',11,True)
para(28,100,785,'Pilz 1003108-EN-11: sez. 4.3, 4.5, 6.3, 8, 9.1.9; copia locale tmp/pdfs/ef1mm-review/pilz.pdf. Schede ufficiali: ifm IFS240-01 (23.07.2024), EVC539-00; Pepperl+Fuchs 326161-0001 NBB4-12GM50-E2-V1. IFS240 MTTF non equivale automaticamente a MTTFd. Progetto V11: rapporto 1794461561566, p32. Manuale EN-11 valido da unita 2.2; mantenere aperta la corrispondenza puntuale configurazione 2.3 / marcatura installata 2.4.',9)
para(28,51,785,'Esito: fattibilita di principio documentata, schema proposto consegnato. Programma attivo e copia nativa non modificati. Soglia/tempi, geometria misurata e PLr/SIL sono condizioni di rilascio della funzione porte.',9)
c.save()
p=pymupdf.open(out)
for i,page in enumerate(p):page.get_pixmap(matrix=pymupdf.Matrix(1.1,1.1)).save(str(out.with_suffix(''))+f'-{i+1}.png')
print(out);print('pages',len(p))
