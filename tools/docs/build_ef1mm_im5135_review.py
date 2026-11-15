from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.lib.colors import HexColor, white
from reportlab.lib.pagesizes import A4, landscape
import pymupdf
root=Path('C:/www/Schlenker'); out=root/'output/pdf/SCHLENKER_EF1MM_IM5135_VERIFICA_REV_C.pdf'
c=canvas.Canvas(str(out),pagesize=landscape(A4)); W,H=landscape(A4)
navy=HexColor('#17365D'); gray=HexColor('#edf2f6')
def text(x,y,s,size=11,color=navy):
 c.setFillColor(color); c.setFont('Helvetica',size); c.drawString(x,y,s)
def header(title,num):
 c.setFillColor(navy);c.rect(0,H-65,W,65,fill=1,stroke=0);text(28,H-30,title,18,white);text(28,H-50,'SCHLENKER 36/10 | Verifica documentale | 12 novembre 2026 | REV C',10,white)
 text(28,18,'Nessun download, modifica del progetto o azionamento eseguito.',9);text(W-65,18,f'{num}/2',9)
def box(x,y,w,h,title,lines):
 c.setFillColor(gray);c.setStrokeColor(navy);c.roundRect(x,y,w,h,7,fill=1,stroke=1);text(x+12,y+h-23,title,13)
 for i,s in enumerate(lines):text(x+12,y+h-45-i*18,s,10)
def arrow(x,y,xx,yy,label):
 c.setStrokeColor(navy);c.setDash(5,3);c.line(x,y,xx,yy);c.setDash();text(x+4,y+8,label,9)
header('RO3101 + IM5135: interfacce e derivazioni',1)
text(28,505,'PIN IDENTIFICATI. DERIVAZIONI NON ANCORA RILASCIATE PER IL CABLAGGIO.',12)
box(28,338,255,145,'IFM RO3101 - encoder', ['A: M12 pin 2, bianco*','B: M12 pin 5, grigio*','Z: M12 pin 4, nero*','UB pin 1 / GND pin 3'])
box(523,360,290,123,'CPU Siemens - ramo esistente',['A -> DI3 / X11:4 / I10.3*','B -> DI4 / X11:5 / I10.4*','Z -> DI5 / X11:6 / I10.5*'])
box(523,175,290,155,'PILZ EF1MM - X12 Mini-IO',['A -> pin 4; B -> pin 7','Riferimento PNP indipendente -> pin 3 (Z)','0 V -> pin 2; monitor +24 V -> pin 1 (S)','Non unire Z del RO3101 e uscita IM5135.','Nessun ponte verso A12/B12/S12.'])
box(28,140,255,166,'IFM IM5135 - zero carosello',['Pin 1: L+; pin 3: L-','Pin 4: PNP NO - uscita selezionata','Pin 2: PNP NC - non unire al pin 4','Presenza metallo: pin 4 attivo.','AL102/X6: destinazione confermata','dall\'operatore; modo porta da leggere.'])
arrow(283,418,523,418,'A / B / Z: percorso documentato*')
arrow(283,355,523,290,'A / B: derivazione da verificare')
arrow(283,230,523,230,'IM5135 pin 4: derivazione da verificare')
text(28,111,'AL102/X6 pin 4: conservare la lettura del sensore in modalita ingresso digitale (SIO).',11)
text(28,90,'Una porta configurata in uscita non puo essere collegata in parallelo all\'uscita del sensore.',11)
text(28,66,'* Riferimenti CPU da documenti del progetto; nessuna nuova verifica TIA. Colori del cordone da verificare sui pin.',9)
text(28,47,'Le linee rappresentano percorsi da valutare, non un cablaggio eseguito. Alimentazioni e schermature da completare.',9)
c.showPage(); header('Esito e condizioni precise da chiudere',2)
rows=[('1. IM5135: punto chiuso','Datasheet IFM, pagina 3: pin 4 normalmente aperto; pin 2 normalmente chiuso.','PNP complementare, 10-36 VDC. Usare il pin 4 come riferimento scelto, non entrambi insieme.'),('2. AL102/X6: compatibilita parziale','AL1403: X6 e porta classe A. Pin 4 C/Q; pin 2 ingresso digitale; pin 1/3 alimentazione sensore.','Manca lettura della configurazione effettiva X6 e del cablaggio del cordone. Non unire due alimentazioni.'),('3. CPU + EF1MM: parallelo non convalidato','La corrente nominale non dimostra l\'assenza di influenza reciproca in guasto o a dispositivo spento.','Servono MLFB CPU, limiti del ramo completo, lunghezze/schermi e comportamento a corto/perdita alimentazione.'),('4. PILZ: architettura prevista, parametri non verificati','Manuale EN-11, sezione 6.5: HTL single Z Freq. Ini pnp; A/B encoder e riferimento PNP separato.','EN-11 valido dalla versione 2.2. Da verificare: parametri reali, rapporto impulsi, soglie, tempi e diagnosi.'),('5. Prima di chiamarlo definitivo','Identificare il cavo Mini-IO e i suoi conduttori; documentare derivazioni e comuni senza retroalimentazione.','Verificare perdita/guasto di ciascun ramo e funzione richiesta per lo sblocco porte. Non usare assenza impulsi da sola.')]
y=505
for title,a,b in rows:
 text(28,y,title,12);text(28,y-19,a,10);text(28,y-35,b,10);y-=73
text(28,124,'Esito: pinout e uscita IM5135 identificati; verifica completa delle derivazioni ancora aperta.',11)
text(28,105,'Non e stato provato che il circuito proposto soddisfi la funzione di sicurezza della macchina.',10)
links=[('IFM IM5135 - schema morsetti','https://www.ifm.com/de/en/product/IM5135'),('IFM AL1403 - dati porte','https://media.ifm.com/dam/cc2d579d-030c-4421-9a6e-b0b7a9c9bffc/Original/AL1403-00_EN-GB.pdf'),('Pilz EF1MM EN-11 - sezioni 6.3, 6.5 e 9.1.8','https://cdn.logic-control.com/docs/pilz-safety/Manuals/PNOZ_m_EF_1MM_Operat_Man_1003108-EN-11.pdf')]
for i,(label,url) in enumerate(links):
 yy=83-i*16;text(28,yy,label,9);c.linkURL(url,(28,yy-2,450,yy+11),relative=0)
c.save();d=pymupdf.open(out)
for i,p in enumerate(d):p.get_pixmap(matrix=pymupdf.Matrix(1.2,1.2)).save(str(out.with_suffix(''))+f'-{i+1}.png')
print(out)

