from pathlib import Path
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak
from reportlab.lib.styles import getSampleStyleSheet
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
import pymupdf

root=Path(__file__).resolve().parents[2]
out=root/'output/pdf/SCHLENKER_RO3101_CPU_PILZ_ESITO_REV_B.pdf'
s=getSampleStyleSheet()
s['Title'].textColor=colors.HexColor('#17365D')
s['BodyText'].fontSize=10;s['BodyText'].leading=13
story=[]
def p(t,style='BodyText'):
 story.append(Paragraph(t,s[style]));story.append(Spacer(1,6))
def table(rows,widths):
 t=Table([[Paragraph(x,s['BodyText']) for x in r] for r in rows],colWidths=widths)
 t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#DFEAF4')),('GRID',(0,0),(-1,-1),.5,colors.grey),('VALIGN',(0,0),(-1,-1),'TOP'),('TOPPADDING',(0,0),(-1,-1),8),('BOTTOMPADDING',(0,0),(-1,-1),8)]))
 story.append(t);story.append(Spacer(1,12))
p('SCHLENKER 36/10<br/>RO3101 - CPU e PILZ','Title')
p('Esito della verifica documentale | REV B | 12 novembre 2026')
p('<b>Conclusione:</b> la distribuzione elettrica dei segnali HTL e tecnicamente realizzabile. Non e dimostrata l\'idoneita safety dell\'insieme RO3101 + distributore + EF1MM. Non e quindi disponibile un cablaggio finale validato per la funzione motion di sicurezza.')
p('Interfaccia identificata','Heading2')
p('<b>motrona GT222</b>: ingresso A/B/Z HTL single-ended, due uscite con livelli configurabili e separazione galvanica ingresso/uscite e fra uscite. Alimentazione 10-30 VDC. L\'ingresso non e isolato dall\'alimentazione del dispositivo. Le uscite richiedono alimentazione e riferimento propri.')
p('E un candidato elettrico concreto, <b>non una prescrizione di acquisto per una funzione safety</b>. Nei documenti consultati non e stata trovata una certificazione safety o un\'approvazione Pilz per questa combinazione. Non creare un parallelo diretto CPU/PILZ sulla base della precedente REV A.')
table([['Architettura da valutare','Destinazione'],['Unico IFM RO3101, A/B/Z HTL -> ingresso GT222','Distributore di impulsi'],['Uscita 1 HTL A/B/Z -> CPU Siemens','Tracking / conteggio di processo'],['Uscita 2 HTL A/B -> PILZ EF1MM X12','Monitoraggio motion, solo dopo validazione safety']], [280,230])
p('Cavo di ingresso PILZ identificato','Heading2')
p('<b>Pilz MM A MINI-IO CAB99 2.5 m, codice 772201</b>: cavo schermato Mini-IO 8 poli per motion monitoring. E un adattatore di cablaggio, non un isolatore o un convertitore safety. Verificare la corrispondenza dei conduttori con i pin prima della terminazione.')
p('Non sono stati modificati progetto, cablaggio, parametri o stato dei controllori. Nessun download o azionamento.')
story.append(PageBreak())
p('Pin documentati e decisione tecnica','Title')
table([['RO3101 / M12','Segnale','CPU: assegnazione del progetto'],['1 - marrone','UB','+24 VDC protetto'],['3 - blu','GND','0 V'],['2 - bianco','A','DI3 / X11:4 / I10.3'],['5 - grigio','B','DI4 / X11:5 / I10.4'],['4 - nero','Z','DI5 / X11:6 / I10.5']],[130,65,315])
p('I riferimenti CPU provengono dalla documentazione HSC2 del progetto; non sono stati ricontrollati nel TIA o sul campo in questa verifica. I colori devono corrispondere ai pin del cordone effettivo: E360658 da solo non ne identifica il modello.')
p('<b>EF1MM, X12 in modalita HTL A/B senza Z:</b> A = pin 4; B = pin 7; GND = pin 2. Il diagramma Pilz non collega i pin 1, 3, 5, 6 e 8 in questa modalita. Non aggiungere +24 V al pin 1 per supposizione. A12/B12/S12 non sono sostituti automatici di X12. Questi dati di interfaccia non sono uno schema esecutivo del GT222.')
p('Cosa impedisce la chiusura safety','Heading2')
p('1. Non e disponibile il PLr/SIL richiesto per la funzione motion. Il manuale Pilz tratta l\'encoder non sicuro senza requisiti aggiuntivi come architettura condizionale fino a PL c, categoria 1; non significa che il RO3101 o questa macchina abbiano raggiunto quel livello.')
p('2. Mancano evidenze applicabili su guasti del sensore, accoppiamento meccanico, distributore e influenza reciproca dei rami. I segnali complementari generati dal distributore non trasformano il RO3101 in un encoder safety.')
p('3. Restano da verificare HTL/PPR reali, rapporto meccanico, soglie motion e prove di perdita segnale. La marcatura EF1MM 2.4 e la configurazione 2.3 non sono state riconciliate documentalmente in questa verifica.')
p('<b>Decisione:</b> conservare il tracking CPU. Non inserire il GT222 nella catena safety senza verifica della specifica architettura. Se il livello richiesto non e raggiungibile con il RO3101 condiviso, occorre una misura motion separata idonea o un\'architettura con diagnostica aggiuntiva validata. Nessun modello aggiuntivo e prescritto senza questo requisito.')
p('Fonti consultate','Heading2')
for label,url in [ ('IFM RO3101: dati e pinout','https://www.ifm.com/us/en/product/RO3101'),('motrona GT222: prodotto','https://www.motrona.com/en/products/signal-converters/level-converters-signal-splitters/gt222-impulse-distributor-for-incremental-encoder-with-potential-separation-2-outputs.html'),('motrona: manuale GT222, gennaio 2025','https://www.motrona.com/fileadmin/files/bedienungsanleitungen/Gv224_e.pdf'),('Pilz: manuale EF1MM 1003108-EN-11, sezioni encoder HTL e safety','https://www.pilz.com/download/open/PNOZ_m_EF_1MM_Operat_Man_1003108-EN-11.pdf'),('Pilz: cavo 772201','https://www.pilz.com/en-VE/eshop/product/772201')]:
 p(f'<link href="{url}" color="#174F90">{label}</link>')
def footer(c,d):
 c.setFont('Helvetica',8);c.drawString(42,22,'REV B - Esito documentale; non costituisce validazione della funzione safety');c.drawRightString(A4[0]-42,22,str(d.page))
SimpleDocTemplate(str(out),pagesize=A4,rightMargin=42,leftMargin=42,topMargin=35,bottomMargin=38).build(story,onFirstPage=footer,onLaterPages=footer)
doc=pymupdf.open(out)
for i,page in enumerate(doc):page.get_pixmap(matrix=pymupdf.Matrix(1.3,1.3)).save(str(out.with_suffix(''))+f'-{i+1}.png')
print(str(out));print('pages:',len(doc))
