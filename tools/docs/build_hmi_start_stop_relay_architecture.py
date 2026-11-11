from pathlib import Path

from reportlab.lib.colors import HexColor, white
from reportlab.lib.pagesizes import A4, landscape
from reportlab.pdfgen import canvas


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "output" / "pdf" / "SCHLENKER_HMI_START_STOP_RELE_Q7_7_ARCHITETTURA_DEFINITIVA.pdf"

W, H = landscape(A4)
NAVY = HexColor("#173B55")
BLUE = HexColor("#1689BD")
GREEN = HexColor("#188B4B")
ORANGE = HexColor("#E79A00")
LIGHT = HexColor("#F3F7F9")
GRAY = HexColor("#566875")


def label(c, x, y, text, color=NAVY, size=8, bold=False):
    c.setFillColor(color)
    c.setFont("Helvetica-Bold" if bold else "Helvetica", size)
    c.drawString(x, y, text)


def terminal(c, x, y, number, color=NAVY):
    c.setFillColor(white)
    c.setStrokeColor(color)
    c.setLineWidth(2)
    c.circle(x, y, 13, stroke=1, fill=1)
    c.setFillColor(color)
    c.setFont("Helvetica-Bold", 9)
    c.drawCentredString(x, y - 3, number)


def wire(c, x1, y1, x2, y2, color=NAVY, width=2.4):
    c.setStrokeColor(color)
    c.setLineWidth(width)
    c.line(x1, y1, x2, y2)


def build():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    c = canvas.Canvas(str(OUTPUT), pagesize=landscape(A4))
    c.setTitle("Schlenker HMI START STOP relay architecture Q7.7")
    c.setAuthor("FBS Global")

    c.setFillColor(NAVY)
    c.rect(0, H - 50, W, 50, stroke=0, fill=1)
    c.setFillColor(white)
    c.setFont("Helvetica-Bold", 11)
    c.drawString(34, H - 31, "SCHLENKER 3610 - COLLEGAMENTO LED START / STOP")
    c.setFont("Helvetica-Bold", 9)
    c.drawRightString(W - 34, H - 31, "ARCHITETTURA DEFINITIVA - 09/11/2026")

    label(c, 34, H - 86, "UN SOLO RELE 24 VDC CON CONTATTO DI SCAMBIO", NAVY, 20, True)
    label(c, 34, H - 106, "K-HMI-STATUS - bobina 24 VDC - 1CO / SPDT - contatti 11/12/14", GRAY, 10, True)

    # PLC and relay coil.
    c.setFillColor(LIGHT)
    c.setStrokeColor(BLUE)
    c.setLineWidth(1.8)
    c.roundRect(34, 305, 220, 150, 10, stroke=1, fill=1)
    c.setFillColor(BLUE)
    c.rect(34, 423, 220, 32, stroke=0, fill=1)
    label(c, 48, 434, "PLC_1 - USCITA DIGITALE", white, 10, True)
    label(c, 50, 394, "%Q7.7", NAVY, 15, True)
    label(c, 50, 372, "ON = macchina realmente in movimento", NAVY, 8, True)
    label(c, 50, 353, "Feedback G120C valido + drive running", NAVY, 8)
    label(c, 50, 326, "Uscita standard: non e una funzione safety", GRAY, 7.5)

    c.setFillColor(LIGHT)
    c.setStrokeColor(ORANGE)
    c.roundRect(315, 305, 210, 150, 10, stroke=1, fill=1)
    c.setFillColor(ORANGE)
    c.rect(315, 423, 210, 32, stroke=0, fill=1)
    label(c, 329, 434, "BOBINA K-HMI-STATUS", white, 10, True)
    terminal(c, 352, 380, "A1", ORANGE)
    terminal(c, 488, 380, "A2", ORANGE)
    wire(c, 365, 380, 475, 380, ORANGE, 3)
    label(c, 384, 391, "24 VDC", ORANGE, 9, True)
    label(c, 336, 330, "Relè interfaccia con soppressore DC", NAVY, 8, True)

    wire(c, 254, 380, 339, 380, BLUE, 3)
    label(c, 272, 392, "%Q7.7", BLUE, 9, True)
    wire(c, 501, 380, 566, 380, NAVY, 3)
    terminal(c, 579, 380, "0V", NAVY)
    label(c, 542, 398, "quadro", NAVY, 7.5)

    # Changeover contact and 24 V feed.
    c.setFillColor(LIGHT)
    c.setStrokeColor(ORANGE)
    c.roundRect(34, 125, 360, 135, 10, stroke=1, fill=1)
    c.setFillColor(ORANGE)
    c.rect(34, 228, 360, 32, stroke=0, fill=1)
    label(c, 48, 239, "CONTATTO DI SCAMBIO K-HMI-STATUS", white, 10, True)

    terminal(c, 92, 180, "11", ORANGE)
    terminal(c, 235, 205, "14", GREEN)
    terminal(c, 235, 155, "12", BLUE)
    wire(c, 105, 180, 155, 180, ORANGE, 3)
    wire(c, 155, 180, 218, 200, GREEN, 3)
    c.setFillColor(NAVY)
    c.circle(155, 180, 4, stroke=0, fill=1)
    wire(c, 34, 180, 79, 180, HexColor("#D32F2F"), 3)
    label(c, 45, 196, "+24 VDC PROTETTO", HexColor("#D32F2F"), 8, True)
    label(c, 266, 220, "NO 14 - START", GREEN, 9, True)
    label(c, 266, 170, "NC 12 - STOP", BLUE, 9, True)

    # HMI LEDs.
    c.setFillColor(LIGHT)
    c.setStrokeColor(GREEN)
    c.roundRect(448, 125, 360, 135, 10, stroke=1, fill=1)
    c.setFillColor(GREEN)
    c.rect(448, 228, 360, 32, stroke=0, fill=1)
    label(c, 462, 239, "PULSANTIERA HMI - LED START / STOP", white, 10, True)

    wire(c, 248, 205, 505, 205, GREEN, 3)
    terminal(c, 522, 205, "12", GREEN)
    label(c, 544, 211, "FILO 12 -> START LED X1", NAVY, 9, True)
    wire(c, 248, 155, 505, 155, BLUE, 3)
    terminal(c, 522, 155, "13", BLUE)
    label(c, 544, 161, "FILO 13 -> STOP LED X1", NAVY, 9, True)
    terminal(c, 760, 180, "2", NAVY)
    label(c, 615, 132, "START X2 + STOP X2 -> filo 2 -> 0 VDC", NAVY, 8, True)

    # State table.
    c.setFillColor(HexColor("#FFF3CD"))
    c.setStrokeColor(ORANGE)
    c.roundRect(34, 52, W - 68, 48, 8, stroke=1, fill=1)
    label(c, 50, 80, "MOVIMENTO:", GREEN, 9, True)
    label(c, 120, 80, "%Q7.7 ON -> 11-14 chiuso -> filo 12 alimentato -> LED START acceso", NAVY, 8.5, True)
    label(c, 430, 80, "FERMA:", BLUE, 9, True)
    label(c, 475, 80, "%Q7.7 OFF -> 11-12 chiuso -> filo 13 alimentato -> LED STOP acceso", NAVY, 8.5, True)
    label(c, 50, 62, "Serve 1 solo relè. Verificare polarità bobina, soppressore, protezione 24 VDC e continuità prima dell'energizzazione.", GRAY, 7.5)

    c.setStrokeColor(HexColor("#AAB7C0"))
    c.line(34, 30, W - 34, 30)
    label(c, 34, 17, "FBS Global - schema dedicato fili 12 e 13", GRAY, 7)
    label(c, W - 72, 17, "Pagina 1/1", GRAY, 7)
    c.save()
    print(OUTPUT)


if __name__ == "__main__":
    build()
