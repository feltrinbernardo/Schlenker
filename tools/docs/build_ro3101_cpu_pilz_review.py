from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.lib.colors import HexColor, white
from reportlab.lib.pagesizes import A4, landscape
import pymupdf

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'output/pdf/SCHLENKER_RO3101_CPU_PILZ_REV_A.pdf'
OUT.parent.mkdir(parents=True, exist_ok=True)
W,H=landscape(A4)
c=canvas.Canvas(str(OUT),pagesize=(W,H))
c.setTitle('Schlenker - IFM RO3101 CPU e PILZ - proposta di derivazione')
navy=HexColor('#17365D'); gray=HexColor('#52616B')
def text(x,y,s,size=10,color=navy,bold=False):
 c.setFillColor(color); c.setFont('Helvetica-Bold' if bold else 'Helvetica',size); c.drawString(x,y,s)
def box(x,y,w,h,title):
 c.setFillColor(HexColor('#EEF3F8')); c.setStrokeColor(navy); c.roundRect(x,y,w,h,6,fill=1,stroke=1); text(x+10,y+h-22,title,12,bold=True)
def header(title,page):
 c.setFillColor(navy);c.rect(0,H-65,W,65,fill=1,stroke=0)
 text(28,H-28,title,18,white,True)
 text(28,H-48,'SCHLENKER 36/10 | 12/11/2026 | REV A | Unico encoder: IFM RO3101',10,white)
 text(28,20,'Proposta documentata - derivazione CPU/PILZ da validare prima del collegamento',9)
 text(W-65,20,f'{page} / 2',9)
header('ENCODER RO3101 - CPU + PILZ',1)
text(28,H-89,'CPU conservata per il tracking. PILZ previsto per monitoraggio motion.',12,bold=True)
text(28,H-108,'Le linee tratteggiate indicano collegamenti proposti, non ancora autorizzati per il cablaggio.',10,HexColor('#A24300'))
box(28,170,200,280,'IFM RO3101 / M12 5 pin')
box(485,170,325,280,'QUADRO / CPU 1512C-1 PN')
rows=[('1 MARRONE','UB / +24 V protetto','#89552C','Distributore +24 VDC'),('3 BLU','GND / 0 V','#1478B5','Barra 0 V comune'),('2 BIANCO','A','#A9A9A9','DI3 -> X11:4 / I10.3'),('5 GRIGIO','B','#68757D','DI4 -> X11:5 / I10.4'),('4 NERO','Z / indice','#202020','DI5 -> X11:6 / I10.5')]
for i,(label,sig,col,dest) in enumerate(rows):
 y=393-i*44
 text(40,y+4,label,11,bold=True);text(40,y-11,sig,9)
 c.setStrokeColor(HexColor(col));c.setLineWidth(3);c.line(228,y,485,y)
 text(497,y+2,dest,11,bold=True)
box(28,49,782,98,'DERIVAZIONE PROPOSTA VERSO PILZ EF1MM - CONNETTORE X12')
text(42,104,'A (bianco) -> X12 pin 4      B (grigio) -> X12 pin 7      GND (blu) -> X12 pin 2',12,bold=True)
c.setDash(5,3);c.setStrokeColor(navy);c.setLineWidth(2);c.line(245,88,791,88);c.setDash()
text(42,70,'HTL single-ended A/B: X12 pin 1, 3, 5, 6, 8 non collegati in questo schema.',10)
text(42,55,'Z nero resta solo alla CPU. Usare i numeri pin del connettore, non una vista geometrica presunta.',9)
c.showPage()
header('CONNESSIONI E VERIFICHE DELLA DERIVAZIONE',2)
items=[
('1. Collegamento esistente CPU','Conservare A/B/Z su DI3/DI4/DI5 come nel PDF HSC2 del progetto. Il presente documento', 'non verifica sul campo quei morsetti e non modifica il software PLC.'),
('2. Collegamento encoder PILZ','Usare un cavo schermato con Mini-IO 8 poli X12: A=4, B=7, GND=2. Non collegare', 'automaticamente gli stessi segnali anche su A12/B12/S12. Non aggiungere ponti a S12.'),
('3. Colori del cavo','1 marrone, 2 bianco, 3 blu, 4 nero, 5 grigio: riferimento M12 IFM. E360658 non identifica', 'da solo il cordone. Verificare continuita colore-pin; il grigio e B, non e lo schermo.'),
('4. Elettrica: verifica ancora necessaria','RO3101 HTL: dato di fabbrica 50 mA. Il carico nominale dei due ingressi appare contenuto;', 'non prova livelli peggiori, fronti, disturbi o comportamento con CPU spenta/guasta.'),
('5. Sicurezza motion','La condivisione di un encoder non crea due misure indipendenti. Non e verificato il PL/SIL', 'richiesto e raggiungibile con RO3101, fissaggio meccanico e diagnostica configurata.'),
('6. Prima di realizzare la derivazione','Confermare idoneita elettrica e influenza reciproca dei ricevitori; definire cavo/adattatore', 'Mini-IO, schermatura e lunghezze. Se il parallelo non e idoneo, serve una soluzione dedicata.'),
('7. Prima del collaudo motion','Verificare HTL e PPR effettivi del RO3101, rapporto meccanico, frequenze e soglie PILZ;', 'provare perdita canali/alimentazione e guasti previsti. Nessun download eseguito.'),
]
y=H-95
for title,a,b in items:
 text(28,y,title,11,bold=True);text(28,y-15,a,10,gray);text(28,y-29,b,10,gray);y-=53
text(28,105,'FONTI E LIMITI',10,bold=True)
text(28,90,'IFM RO3101 datasheet RO3101-02, 13.04.2023, pagina 3: pinout encoder.',9)
text(28,76,'PILZ 1003108-EN-16, pp. 39, 42, 53, 59: pinout, HTL, carico e architettura safety.',9)
text(28,62,'Il manuale EN-16 non chiude la compatibilita dell\'unita marcata 2.4 con configurazione 2.3.',9)
text(28,48,'CPU: SCHLENKER_ENCODER_HSC2_COLLEGAMENTO_5_COLORI.pdf. Nessun SICK previsto.',9)
c.save()
d=pymupdf.open(OUT)
for i,p in enumerate(d):
 p.get_pixmap(matrix=pymupdf.Matrix(1.3,1.3)).save(str(OUT.with_suffix(''))+f'-page{i+1}.png')
print(OUT)
