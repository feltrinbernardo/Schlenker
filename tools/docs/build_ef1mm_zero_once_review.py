from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4,landscape
from reportlab.lib.colors import HexColor,white
import pymupdf
root=Path('C:/www/Schlenker');out=root/'output/pdf/SCHLENKER_EF1MM_ZERO_UN_GIRO_REV_D.pdf';c=canvas.Canvas(str(out),pagesize=landscape(A4));W,H=landscape(A4)
blue=HexColor('#17365D')
def t(x,y,s,z=11):c.setFillColor(blue);c.setFont('Helvetica',z);c.drawString(x,y,s)
def head(title,n):
 c.setFillColor(blue);c.rect(0,H-62,W,62,fill=1,stroke=0);c.setFillColor(white);c.setFont('Helvetica-Bold',17);c.drawString(25,H-28,title);c.setFont('Helvetica',10);c.drawString(25,H-47,'SCHLENKER 36/10 | 12 novembre 2026 | REV D | Esito documentale, nessun download');t(25,18,'Non costituisce schema esecutivo approvato o validazione dello sblocco porte.',9);t(W-60,18,str(n)+'/2',9)
def box(x,y,w,h,title,lines):
 c.setFillColor(HexColor('#edf2f6'));c.setStrokeColor(blue);c.roundRect(x,y,w,h,6,fill=1,stroke=1);t(x+10,y+h-23,title,12)
 for i,s in enumerate(lines):t(x+10,y+h-43-17*i,s,10)
def line(x,y,xx,yy,label):c.setStrokeColor(blue);c.line(x,y,xx,yy);t(x+5,y+8,label,9)
head('Encoder + zero una volta per giro: conclusione',1)
t(25,507,'ESITO: architettura di ingresso prevista; idoneita della funzione porte non dimostrata.',13)
lines=[('Dato fisico confermato dall\'operatore','Un bersaglio al rubinetto n. 1: 1 impulso zero per giro del carosello, 36 rubinetti.'),('Calcolo alla velocita manuale richiesta','3.000 / 36 / 60 = 1,3889 RPM; fZ = 3.000 / 36 / 3.600 = 0,02315 Hz; periodo = 43,2 s.'),('Limite del manuale Pilz, sezione 9.1.8','Per AB/Z >= 1, riferimento minimo per dinamizzazione: 0,010 Hz. Il valore a regime supera tale minimo.'),('Diagnostica di plausibilita, non tempo di arresto','La tolleranza indicata e 7,5 impulsi Z: 7,5 x 43,2 = 324 s, cioe 5 min 24 s, a regime costante.'),('Cosa questo risultato dimostra','Un impulso per giro non esclude la lettura EF1MM. Non prova una reazione ai guasti adeguata per aprire le porte.'),('Cosa impedisce il rilascio esecutivo','Mancano rapporto A/B-Z reale, parametri motion, requisito di reazione/PLr e verifica del ramo condiviso CPU.')]
y=474
for h,b in lines:t(25,y,h,12);t(25,y-21,b,10);y-=57
t(25,116,'Rapporto da impostare: frequenza A/B / frequenza Z, secondo definizione del configuratore.',11)
t(25,95,'Non e 36 per il solo numero di rubinetti. Servono risoluzione encoder effettiva e rapporto meccanico.',10)
t(25,74,'Il tempo 324 s e una conversione della tolleranza del manuale nella condizione indicata, non una prova della macchina.',9)
t(25,54,'Fonte: Pilz 1003108-EN-11, sezioni 6.5 e 9.1.8, pagine 37-38 e 56-58. Manuale valido dalla versione 2.2.',9)
c.linkURL('https://cdn.logic-control.com/docs/pilz-safety/Manuals/PNOZ_m_EF_1MM_Operat_Man_1003108-EN-11.pdf',(25,50,800,69),relative=0)
c.showPage();head('Percorsi fisici necessari - proposta di progetto',2)
box(25,355,270,145,'RO3101 - encoder esistente',['Conservare il tracking CPU.','A -> CPU e ingresso A PILZ proposto.','B -> CPU e ingresso B PILZ proposto.','Z encoder resta distinto dallo zero IM5135.','Derivazione A/B da qualificare in guasto.'])
box(535,374,280,126,'CPU Siemens',['A/B/Z encoder: percorso esistente.','Zero macchina: ricevuto via rete','da AL102/X6.','Nessun consenso CPU al posto di X12.'])
box(25,153,270,168,'IM5135 - zero al rubinetto 1',['Pin 1 L+ / pin 3 L-: alimentazione esistente.','Pin 4 NO: conservare verso AL102/X6.','Pin 2 NC: riferimento proposto per PILZ.','NC: stessa frequenza, livello invertito.','Non e un secondo sensore indipendente.','Serve accesso al pin 2 lato sensore.'])
box(535,153,280,190,'EF1MM - X12 Mini-IO',['Pin 4 A <- RO3101 A','Pin 7 B <- RO3101 B','Pin 3 Z <- IM5135 pin 2 (proposta)','Pin 2 GND <- riferimento 0 V','Pin 1 S <- monitor alimentazione sensore','Pin 5 /A, pin 6 /Z, pin 8 /B: liberi.','Modalita: HTL single Z Freq. Ini pnp','Non collegare il riferimento Z ad A12/B12.'])
line(295,430,535,430,'Tracking conservato')
line(295,367,535,318,'A/B: distribuzione da qualificare')
line(295,247,535,247,'NUOVO cavo schermato al quadro')
t(25,125,'AL102 resta in rete. La rete non sostituisce il nuovo collegamento fisico al PILZ.',12)
t(25,103,'Occorrono: accesso M12 al pin 2, cavo schermato e terminazione Mini-IO X12. Disponibilita in magazzino non verificata.',9)
t(25,84,'Non unire pin 2 e pin 4 IM5135; non ponticellare alimentazioni diverse. Qualificare durata impulsi e comuni.',9)
t(25,65,'Uso NC come riferimento: inferenza dal tipo di segnale PNP ammesso; non approvazione specifica IFM/Pilz della combinazione.',9)
t(25,46,'Fonti: IFM IM5135 (pinout); Pilz EN-11 (X12/modalita). Schema sostituisce la proposta REV C; non autorizza cablaggio.',9)
c.linkURL('https://www.ifm.com/de/en/product/IM5135',(25,42,800,59),relative=0)
c.save();d=pymupdf.open(out)
for i,p in enumerate(d):p.get_pixmap(matrix=pymupdf.Matrix(1.15,1.15)).save(str(out.with_suffix(''))+f'-{i+1}.png')
print(out)
