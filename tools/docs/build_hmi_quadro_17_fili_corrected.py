from io import BytesIO
from pathlib import Path

from pypdf import PdfReader, PdfWriter
from reportlab.lib.colors import HexColor, white
from reportlab.lib.pagesizes import landscape, A4
from reportlab.pdfgen import canvas


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "output" / "pdf" / "SCHLENKER_HMI_QUADRO_CAVO_17_FILI_3D_V3_LED_LOGIC_DRAFT.pdf"
OUTPUT = ROOT / "output" / "pdf" / "SCHLENKER_HMI_QUADRO_CAVO_17_FILI_REV_DEFINITIVA_RELE_STATUS_2026-11-09.pdf"

W, H = landscape(A4)
NAVY = HexColor("#173B55")
BLUE = HexColor("#1689BD")
GREEN = HexColor("#188B4B")
ORANGE = HexColor("#E29400")
LIGHT = HexColor("#EEF3F6")
RED = HexColor("#C9352B")
GRAY = HexColor("#53636F")


def header(c, title, page):
    c.setFillColor(NAVY)
    c.rect(0, H - 48, W, 48, stroke=0, fill=1)
    c.setFillColor(white)
    c.setFont("Helvetica-Bold", 10)
    c.drawString(34, H - 30, "SCHLENKER 3610 - HMI / QUADRO ELETTRICO")
    c.setFont("Helvetica-Bold", 9)
    c.drawRightString(W - 34, H - 30, "REVISIONE CORRETTA 09-11-2026")
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 21)
    c.drawString(34, H - 82, title)
    c.setStrokeColor(HexColor("#AAB7C0"))
    c.line(34, 28, W - 34, 28)
    c.setFillColor(GRAY)
    c.setFont("Helvetica", 7)
    c.drawString(34, 16, "Mappatura consolidata V12 - sostituisce la tabella fili della precedente V3")
    c.drawRightString(W - 34, 16, f"Pagina {page}/4")


def corrected_page_one_overlay():
    packet = BytesIO()
    c = canvas.Canvas(packet, pagesize=landscape(A4))
    c.setFillColor(NAVY)
    c.rect(W - 171, H - 48, 171, 48, stroke=0, fill=1)
    c.setFillColor(GREEN)
    c.roundRect(W - 160, H - 37, 126, 23, 7, stroke=0, fill=1)
    c.setFillColor(white)
    c.setFont("Helvetica-Bold", 8)
    c.drawCentredString(W - 97, H - 29, "REVISIONE CORRETTA")
    c.setFillColor(white)
    c.rect(0, 0, W, 30, stroke=0, fill=1)
    c.setStrokeColor(HexColor("#AAB7C0"))
    c.line(34, 28, W - 34, 28)
    c.setFillColor(GRAY)
    c.setFont("Helvetica", 7)
    c.drawString(34, 16, "Vista architettura: HMI -> cavo 17 fili + PE -> quadro elettrico")
    c.drawRightString(W - 34, 16, "Pagina 1/4")
    c.save()
    packet.seek(0)
    return PdfReader(packet).pages[0]


def build_new_pages():
    packet = BytesIO()
    c = canvas.Canvas(packet, pagesize=landscape(A4))

    header(c, "Mappatura definitiva cavo 1...17", 2)
    rows = [
        (1, "HMI L+ + distributore locale +24 VDC", "<-", "+24 VDC protetto quadro", GREEN),
        (2, "HMI M + comune lampade LED X2", "<-", "0 VDC quadro", GREEN),
        (3, "START - contatto NO morsetto 4", "->", "PLC DI20 - START", BLUE),
        (4, "STOP - contatto NO morsetto 4", "->", "PLC DI21 - STOP", BLUE),
        (5, "RESET - contatto NO morsetto 4", "->", "PNOZ I8 - SAFETY RESET", BLUE),
        (6, "DOOR REQUEST - contatto NO morsetto 4", "->", "PNOZ I9 - DOOR REQUEST", BLUE),
        (7, "AUTO/MANUAL - contatto NC morsetto 2", "->", "PLC DI22 - AUTO SELECTED", BLUE),
        (8, "E-STOP CH1 - contatto NC morsetto 1", "<-", "PNOZ T0 - test pulse CH1", ORANGE),
        (9, "E-STOP CH1 - contatto NC morsetto 2", "->", "PNOZ M0 - ingresso CH1", ORANGE),
        (10, "E-STOP CH2 - contatto NC morsetto 1", "<-", "PNOZ T1 - test pulse CH2", ORANGE),
        (11, "E-STOP CH2 - contatto NC morsetto 2", "->", "PNOZ M1 - ingresso CH2", ORANGE),
        (12, "Lampada START - morsetto LED X1", "<-", "Relè K-HMI-STATUS morsetto 14 (NO)", BLUE),
        (13, "Lampada STOP - morsetto LED X1", "<-", "Relè K-HMI-STATUS morsetto 12 (NC)", BLUE),
        (14, "Lampada DOOR REQUEST - LED X1", "<-", "PNOZ M17/M18 - consenso porte", BLUE),
        (15, "RESET - contatto NO morsetto 3", "<-", "+24 VDC protetto - ramo CB21", RED),
        (16, "DOOR REQUEST - contatto NO morsetto 3", "<-", "+24 VDC protetto", RED),
        (17, "SPARE: isolare e numerare", "--", "SPARE: non collegare", GRAY),
    ]
    y = H - 108
    row_h = 25.5
    c.setFont("Helvetica-Bold", 8)
    c.setFillColor(GRAY)
    c.drawString(57, y + 9, "FILO")
    c.drawString(100, y + 9, "LATO PULSANTIERA / HMI")
    c.drawCentredString(425, y + 9, "DIREZIONE")
    c.drawString(470, y + 9, "COLLEGAMENTO NEL QUADRO ELETTRICO")
    y -= 4
    for number, panel, direction, cabinet, color in rows:
        y -= row_h
        c.setFillColor(LIGHT if number % 2 else white)
        c.roundRect(34, y, W - 68, row_h - 2, 4, stroke=0, fill=1)
        c.setFillColor(color)
        c.circle(67, y + 11.5, 10, stroke=0, fill=1)
        c.setFillColor(white)
        c.setFont("Helvetica-Bold", 8)
        c.drawCentredString(67, y + 8.5, str(number))
        c.setFillColor(NAVY)
        c.setFont("Helvetica-Bold", 7.4)
        c.drawString(100, y + 8, panel)
        c.setFillColor(color)
        c.setFont("Helvetica-Bold", 9)
        c.drawCentredString(425, y + 7.5, direction)
        c.setFillColor(NAVY)
        c.setFont("Helvetica", 7.4)
        c.drawString(470, y + 8, cabinet)
    c.setFillColor(GREEN)
    c.roundRect(34, 34, 190, 24, 7, stroke=0, fill=1)
    c.setFillColor(white)
    c.setFont("Helvetica-Bold", 8)
    c.drawCentredString(129, 42, "PE PIASTRA HMI -> BARRA PE QUADRO")
    c.showPage()

    header(c, "Collegamento per componente", 3)
    cards = [
        ("HMI TOUCHSCREEN 24 VDC", ["Filo 1 -> L+", "Filo 2 -> M / 0 V", "PE -> carcassa/piastra -> barra PE"], GREEN),
        ("START + LED", ["+24 locale -> NO morsetto 3", "NO morsetto 4 -> filo 3 -> PLC DI20", "LED X1 -> filo 12 -> K-HMI-STATUS 14 (NO)"], BLUE),
        ("STOP + LED", ["+24 locale -> NO morsetto 3", "NO morsetto 4 -> filo 4 -> PLC DI21", "LED X1 -> filo 13 -> K-HMI-STATUS 12 (NC)"], BLUE),
        ("RESET SAFETY", ["Filo 15 (+24 protetto CB21) -> morsetto 3", "Morsetto 4 -> filo 5 -> PNOZ I8", "Nessun LED installato"], RED),
        ("E-STOP - DUE CANALI", ["CH1: PNOZ T0 -> filo 8 -> NC -> filo 9 -> M0", "CH2: PNOZ T1 -> filo 10 -> NC -> filo 11 -> M1", "Nessun ponte e nessun +24 V comune"], ORANGE),
        ("DOOR REQUEST + LED", ["Filo 16 (+24 protetto) -> NO morsetto 3", "Morsetto 4 -> filo 6 -> PNOZ I9", "LED X1 -> filo 14; LED X2 -> filo 2"], RED),
        ("AUTO / MANUAL", ["+24 locale -> NC morsetto 1", "NC morsetto 2 -> filo 7 -> PLC DI22", "Verificare posizione AUTO con tester"], BLUE),
        ("RISERVA", ["Filo 17 non collegato", "Isolare e numerare entrambe le estremita", "Non riutilizzare senza revisione del disegno"], GRAY),
    ]
    card_w = (W - 92) / 4
    card_h = 174
    for idx, (title, lines, color) in enumerate(cards):
        col = idx % 4
        row = idx // 4
        x = 34 + col * (card_w + 8)
        y = H - 116 - row * (card_h + 16) - card_h
        c.setFillColor(HexColor("#F7FAFC"))
        c.setStrokeColor(color)
        c.setLineWidth(1.4)
        c.roundRect(x, y, card_w, card_h, 9, stroke=1, fill=1)
        c.setFillColor(color)
        c.rect(x, y + card_h - 34, card_w, 34, stroke=0, fill=1)
        c.setFillColor(white)
        c.setFont("Helvetica-Bold", 9)
        c.drawString(x + 11, y + card_h - 22, title)
        c.setFillColor(NAVY)
        c.setFont("Helvetica-Bold", 8)
        ty = y + card_h - 57
        for line in lines:
            c.drawString(x + 11, ty, line)
            ty -= 17
    c.setFillColor(RED)
    c.setFont("Helvetica-Bold", 9)
    c.drawString(34, 44, "CORREZIONE CHIAVE: filo 6 = DOOR REQUEST; E-STOP = fili 8, 9, 10 e 11.")
    c.showPage()

    header(c, "Soluzione definitiva LED START / STOP con un solo rele", 4)
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 12)
    c.drawString(48, H - 118, "K-HMI-STATUS: rele interfaccia 24 VDC, 1 contatto di scambio (1CO / SPDT)")
    c.setFont("Helvetica", 9)
    c.drawString(48, H - 138, "La bobina segue il movimento reale dal feedback G120C. Il contatto commuta automaticamente i due LED.")

    def box(x, y, w, h, title, lines, color):
        c.setFillColor(HexColor("#F7FAFC"))
        c.setStrokeColor(color)
        c.setLineWidth(1.6)
        c.roundRect(x, y, w, h, 9, stroke=1, fill=1)
        c.setFillColor(color)
        c.rect(x, y + h - 32, w, 32, stroke=0, fill=1)
        c.setFillColor(white)
        c.setFont("Helvetica-Bold", 9)
        c.drawString(x + 12, y + h - 21, title)
        c.setFillColor(NAVY)
        c.setFont("Helvetica-Bold", 8)
        ty = y + h - 55
        for line in lines:
            c.drawString(x + 12, ty, line)
            ty -= 17

    box(48, 286, 202, 145, "PLC_1 - USCITA INTEGRATA", [
        "%Q7.7 -> bobina A1",
        "A2 -> 0 VDC quadro",
        "Logica: macchina realmente in movimento",
        "Feedback G120C valido + drive running",
    ], BLUE)
    box(320, 270, 202, 177, "K-HMI-STATUS - 24 VDC 1CO", [
        "Bobina: A1 / A2",
        "Comune contatto 11 -> +24 VDC protetto",
        "NO morsetto 14 -> filo 12 START LED",
        "NC morsetto 12 -> filo 13 STOP LED",
        "Bobina con soppressore DC e polarita verificata",
    ], ORANGE)
    box(592, 286, 202, 145, "PULSANTIERA HMI", [
        "Filo 12 -> START LED X1",
        "Filo 13 -> STOP LED X1",
        "START LED X2 -> filo 2 -> 0 VDC",
        "STOP LED X2 -> filo 2 -> 0 VDC",
    ], GREEN)

    c.setStrokeColor(NAVY)
    c.setLineWidth(2)
    c.line(250, 358, 320, 358)
    c.line(522, 358, 592, 358)
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 9)
    c.drawCentredString(285, 368, "%Q7.7 / 0 V")
    c.drawCentredString(557, 368, "fili 12 / 13")

    c.setFillColor(HexColor("#FFF3CD"))
    c.setStrokeColor(ORANGE)
    c.roundRect(48, 112, W - 96, 116, 9, stroke=1, fill=1)
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 10)
    c.drawString(64, 202, "SEQUENZA ELETTRICA")
    c.setFont("Helvetica", 9)
    notes = [
        "1. Macchina in movimento: %Q7.7 = ON, rele eccitato, 11-14 chiuso -> filo 12 alimentato -> LED START acceso.",
        "2. Macchina ferma: %Q7.7 = OFF, rele diseccitato, 11-12 chiuso -> filo 13 alimentato -> LED STOP acceso.",
        "3. Non servono due uscite PLC e non servono due rele. Serve un solo rele 24 VDC con contatto di scambio.",
        "4. Questa segnalazione non svolge funzioni di sicurezza. Prima dell'energizzazione verificare polarita, continuita e fusibile 24 VDC.",
    ]
    ty = 181
    for line in notes:
        c.drawString(64, ty, line)
        ty -= 21
    c.save()
    packet.seek(0)
    return PdfReader(packet)


def main():
    if not SOURCE.exists():
        raise FileNotFoundError(SOURCE)
    source = PdfReader(str(SOURCE))
    first = source.pages[0]
    first.merge_page(corrected_page_one_overlay())
    new_pages = build_new_pages()
    writer = PdfWriter()
    writer.add_page(first)
    writer.add_page(new_pages.pages[0])
    writer.add_page(new_pages.pages[1])
    writer.add_page(new_pages.pages[2])
    writer.add_metadata({
        "/Title": "Schlenker 3610 - HMI wiring with final START STOP status relay",
        "/Subject": "Final mapping with Q7.7 and one 24 VDC changeover relay for wires 12 and 13",
        "/Author": "FBS Global",
    })
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT.open("wb") as stream:
        writer.write(stream)
    print(OUTPUT)


if __name__ == "__main__":
    main()
