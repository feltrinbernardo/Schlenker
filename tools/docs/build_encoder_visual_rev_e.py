from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4,landscape
from reportlab.lib.colors import HexColor
import pymupdf
r=Path('C:/www/Schlenker');o=r/'output/pdf/SCHLENKER_ENCODER_ARCHITETTURA_VISIVA_REV_E.pdf';c=canvas.Canvas(str(o),pagesize=landscape(A4));W,H=landscape(A4);navy='#173752'
def t(x,y,s,z=11,b=False,col=navy):
 c.setFillColor(HexColor(col));c.setFont('Helvetica-Bold' if b else 'Helvetica',z);c.drawString(x,y,s)
def box(x,y,w,h,col):c.setFillColor(HexColor(col));c.roundRect(x,y,w,h,5,fill=1,stroke=0)
def poly(points,col):
 p=c.beginPath();p.moveTo(*points[0]);[p.lineTo(*pt) for pt in points[1:]];p.close();c.setFillColor(HexColor(col));c.drawPath(p,fill=1,stroke=0)
def wire(a,b,y,col,dash=False):
 c.setLineWidth(6);c.setStrokeColor(HexColor('#71808C'));c.line(a,y,b,y);c.setLineWidth(3.5);c.setStrokeColor(HexColor(col));c.line(a,y,b,y)
 d=1 if b>a else -1;poly([(b,y),(b-d*8,y+5),(b-d*8,y-5)],'#71808C')
def head(title,sub,n):
 box(0,H-67,W,67,navy);t(24,H-30,title,19,True,'#FFFFFF');t(24,H-51,sub,10,False,'#FFFFFF');t(26,17,'REV E | 12/11/2026 | Proposta grafica - rilascio esecutivo aperto: vedere pagina 3.',9);t(W-58,17,f'{n} / 3',9)
def encoder():
 box(51,420,105,56,'#85929D');c.setFillColor(HexColor('#B9C5CD'));c.ellipse(34,420,71,476,fill=1,stroke=0);c.setFillColor(HexColor('#465662'));c.ellipse(139,420,172,476,fill=1,stroke=0);box(165,440,29,14,'#BBC5CC')
def cpu():
 poly([(654,418),(681,434),(681,492),(654,480)],'#354956');box(561,418,94,63,'#667987');box(570,454,43,17,'#B5D0CE');box(570,428,15,20,'#008C99')
 for x in [624,635,646]:
  for y in range(426,474,7):box(x,y,4,3,'#C2CAD0')
head('ENCODER -> CPU | FILI E MORSETTI','Stesso modello visivo delle tavole E-stop e porte | Conservazione del ramo tracking',1);encoder();cpu();t(208,463,'IFM RO3101',15,True);t(208,443,'M12 / 5 pin - segnali A, B, Z',10);t(695,463,'CPU',13,True);t(695,444,'1512C-1 PN',10,True)
t(35,393,'LATO ENCODER',13,True);t(574,393,'LATO QUADRO',13,True);box(28,128,245,250,'#EAF0F4');box(569,128,246,250,'#EAF0F4')
rows=[('PIN 1 | MARRONE','UB / alimentazione','+24 VDC PROTETTO','Distributore quadro','#87552F',True),('PIN 3 | BLU','GND / riferimento','0 VDC COMUNE','Riferimento comune con CPU','#007AC6',True),('PIN 2 | BIANCO','Segnale A','DI3 -> CPU X11:4','HSC2 - A / I10.3','#FFFFFF',False),('PIN 5 | GRIGIO','Segnale B','DI4 -> CPU X11:5','HSC2 - B / I10.4','#909090',False),('PIN 4 | NERO','Segnale Z encoder','DI5 -> CPU X11:6','HSC2 - N / I10.5','#191919',False)]
for i,(a,b,d,e,col,rev) in enumerate(rows):
 y=351-i*47;t(42,y,a,13,True);t(42,y-17,b,10);t(583,y,d,12,True);t(583,y-17,e,10);wire(569 if rev else 273,273 if rev else 569,y-4,col)
box(28,66,W-56,46,'#FFF0CB');t(40,94,'COLORI RO3101: MARRONE / BLU / BIANCO / GRIGIO / NERO',12,True);t(40,77,'Colori dichiarati dall\'operatore; associazione ai pin da documento IFM. Dettagli e limiti a pagina 3.',10)
t(28,47,'Le frecce mostrano origine e destinazione del collegamento. Figure illustrative, non viste di montaggio dei pin.',9);t(28,32,'Il nero Z encoder non deve essere unito al riferimento zero IM5135 destinato al PILZ.',9)
c.showPage();head('SEGNALI -> EF1MM | PERCORSI FISICI','CPU tracking conservato | IM5135 su AL102 conservato | Nuovo collegamento al PILZ proposto',2)
# Recognisable sensor and master
box(36,435,60,47,'#313B43');box(36,423,60,13,'#F18716');box(61,416,12,9,'#A4ADB5');t(110,465,'IM5135 - zero carosello',13,True);t(110,446,'Pin 4 NO -> AL102/X6 -> rete -> CPU',10);t(110,428,'Pin 2 NC -> nuovo cavo verso quadro',10)
# Pilz illustrative module
poly([(650,415),(667,426),(667,493),(650,482)],'#BC8B0A');box(604,415,46,68,'#F6C51B');box(617,431,21,16,'#394B56');t(609,464,'PILZ',9,True);t(681,466,'PNOZ m EF1MM',13,True);t(681,446,'X12 - Mini-IO 8 poli',10);t(681,428,'Pin numerati, non posizione fisica',9)
box(28,134,275,266,'#EAF0F4');box(565,134,250,266,'#F8DD4D')
rows=[('RO3101 | A - BIANCO','Derivazione del segnale encoder A','PIN 4 | A','#FFFFFF'),('RO3101 | B - GRIGIO','Derivazione del segnale encoder B','PIN 7 | B','#909090'),('IM5135 | PIN 2 - NC','Nuovo cavo dal sensore: colore non noto','PIN 3 | Z','#436D80'),('0 V | RIFERIMENTO','Comune dei segnali: colore ramo non noto','PIN 2 | GND','#436D80'),('ALIMENTAZIONE SENSORE','Monitor tensione: colore ramo non noto','PIN 1 | S','#436D80')]
for i,(a,b,d,col) in enumerate(rows):
 y=373-i*49;t(40,y,a,12,True);t(40,y-17,b,9);t(578,y-3,d,14,True);wire(303,565,y-3,col)
box(28,72,W-56,45,'#FFF0CB');t(40,99,'BLU-GRIGIO = SEGNALE DI PROGETTO, NON COLORE DEL FILO',11,True);t(40,82,'Non sono assegnati colori ai conduttori del nuovo Mini-IO. Identificarli con il cavo effettivamente disponibile.',9)
t(28,53,'Pin 5 /A, 6 /Z e 8 /B liberi nella modalita proposta. A12/B12 non sono la destinazione del riferimento Z.',9);t(28,35,'Non unire le uscite IM5135 pin 2 e pin 4; non collegare tra loro alimentazioni distinte.',9)
c.showPage();head('VERIFICHE APERTE | PAGINA SEPARATA','Stato tecnico conservato: il miglioramento grafico non approva il cablaggio',3)
items=[('1. Identita dei cavi e dei morsetti','I cinque colori RO3101 sono dichiarati; la continuita colore-pin del cordone E360658 non e provata.','Le destinazioni CPU sono riprese dal documento HSC2, non ricontrollate oggi nel progetto nativo.'),('2. Nuovo ramo IM5135 verso PILZ','Il sensore raggiunge la CPU via AL102. Serve un nuovo cavo schermato dal pin 2 fisico al quadro.','Pin 2 NC come frequenza e una proposta: verificare accesso al pin, durata impulsi e configurazione reale.'),('3. Distribuzione A/B e alimentazione','Non e convalidata l\'influenza del ramo CPU in guasto o da spento. Il pinout non basta ad approvare il parallelo.','Definire giunzioni, schermatura, comuni e monitor S evitando retroalimentazioni fra sorgenti.'),('4. Configurazione EF1MM e riferimento lento','Modalita proposta: HTL single Z Freq. Ini pnp. Rapporto A/B-Z e soglie da ricavare dai dati reali.','1 impulso/giro a 3.000 bottiglie/ora: 43,2 s. La tolleranza condizionale di 7,5 Z equivale a 324 s.'),('5. Rilascio della funzione porte','324 s e una conversione della tolleranza diagnostica, non il tempo di arresto della macchina.','Restano valutazione della funzione richiesta e prove guasti: questo PDF non autorizza sblocco o download.')]
y=493
for h,a,b in items:t(28,y,h,13,True);t(28,y-21,a,10);t(28,y-38,b,10);y-=75
t(28,102,'FONTI E TRACCIABILITA',11,True)
sources=[('Documento progetto HSC2: SCHLENKER_ENCODER_HSC2_COLLEGAMENTO_5_COLORI.pdf',None),('IFM RO3101 e IM5135: pinout; Pilz 1003108-EN-11: sezioni 6.2, 6.5 e 9.1.8','https://cdn.logic-control.com/docs/pilz-safety/Manuals/PNOZ_m_EF_1MM_Operat_Man_1003108-EN-11.pdf')]
for i,(label,url) in enumerate(sources):
 y=83-i*18;t(28,y,label,9)
 if url:c.linkURL(url,(28,y-3,810,y+12))
t(28,42,'Nessuna modifica PLC, HMI o PILZ. Nessun download o azionamento. REV E sostituisce solo la presentazione grafica.',9)
c.save();d=pymupdf.open(o)
for i,p in enumerate(d):p.get_pixmap(matrix=pymupdf.Matrix(1.3,1.3)).save(str(o.with_suffix(''))+f'-{i+1}.png')
print(o);print(len(d))
