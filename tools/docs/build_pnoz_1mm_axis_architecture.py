from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.pagesizes import A3, landscape
from reportlab.lib.utils import ImageReader
from reportlab.pdfgen import canvas


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "output" / "pdf" / "SCHLENKER_PNOZ_m_EF_1MM_AXIS_1_CONNECTION_ARCHITECTURE_REV_B.pdf"
PHOTO = Path(r"C:\www\Schlenker\.codex-remote-attachments\01a0b2a8-9afd-7e40-8df2-f046eda2b7d7\3993f96a-583c-4b1a-a3a9-9f2d0f0d35e5\1-1000158544.jpg")

W, H = landscape(A3)
NAVY = colors.HexColor("#17365D")
BLUE = colors.HexColor("#1F77B4")
GREEN = colors.HexColor("#138A4B")
ORANGE = colors.HexColor("#F5A623")
RED = colors.HexColor("#D62728")
LIGHT = colors.HexColor("#EEF3F8")
YELLOW = colors.HexColor("#FFD400")


def header(c, title, subtitle, page):
    c.setFillColor(NAVY)
    c.rect(0, H - 55, W, 55, fill=1, stroke=0)
    c.setFillColor(colors.white)
    c.setFont("Helvetica-Bold", 18)
    c.drawString(30, H - 32, title)
    c.setFont("Helvetica", 8)
    c.drawString(31, H - 46, subtitle)
    c.drawRightString(W - 30, H - 32, f"SCHLENKER 36/10 | PAGE {page}/2 | 2026-10-30")


def footer(c):
    c.setStrokeColor(colors.HexColor("#AAB7C4"))
    c.line(30, 28, W - 30, 28)
    c.setFillColor(colors.HexColor("#5B6770"))
    c.setFont("Helvetica", 6.5)
    c.drawString(30, 17, "Basis: Pilz PNOZ m EF 1MM, order 772170. Official manual 1003108-EN-16. Wiring must follow the validated PNOZmulti project.")
    c.drawRightString(W - 30, 17, "Axis identity remains a commissioning hold point.")


def arrow(c, x1, y1, x2, y2, color=BLUE, width=2.5):
    c.setStrokeColor(color)
    c.setFillColor(color)
    c.setLineWidth(width)
    c.line(x1, y1, x2, y2)
    import math
    angle = math.atan2(y2 - y1, x2 - x1)
    size = 8
    points = [
        (x2, y2),
        (x2 - size * math.cos(angle - 0.45), y2 - size * math.sin(angle - 0.45)),
        (x2 - size * math.cos(angle + 0.45), y2 - size * math.sin(angle + 0.45)),
    ]
    p = c.beginPath()
    p.moveTo(*points[0])
    p.lineTo(*points[1])
    p.lineTo(*points[2])
    p.close()
    c.drawPath(p, fill=1, stroke=0)


def module_box(c, x, y, w, h, title, code, detail, highlight=False):
    c.setFillColor(YELLOW if highlight else colors.HexColor("#FFE86A"))
    c.setStrokeColor(NAVY if highlight else colors.HexColor("#7B6A00"))
    c.setLineWidth(2 if highlight else 1)
    c.roundRect(x, y, w, h, 5, fill=1, stroke=1)
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 9)
    c.drawCentredString(x + w / 2, y + h - 19, title)
    c.setFont("Helvetica", 7)
    c.drawCentredString(x + w / 2, y + h - 34, code)
    c.setFont("Helvetica", 6.2)
    c.drawCentredString(x + w / 2, y + 12, detail)


def draw_page1(c):
    header(c, "PNOZ m EF 1MM - AXIS 1 CONNECTION ARCHITECTURE", "Where the module belongs and what may be connected", 1)

    # Rack architecture.
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 11)
    c.drawString(35, H - 82, "1. PHYSICAL POSITION IN THE PNOZmulti 2 RACK")
    rack_y = H - 235
    modules = [
        ("PNOZ m ES", "PROFINET", "network interface", 120),
        ("PNOZ m EF 1MM", "772170", "safe motion - 1 axis", 145),
        ("PNOZ m B0", "772100", "base unit", 120),
        ("PNOZ m EF 16DI", "772140", "safe inputs", 130),
        ("PNOZ m EF 8DI4DO", "772142", "safe I/O", 140),
    ]
    x = 42
    for idx, (title, code, detail, width) in enumerate(modules):
        module_box(c, x, rack_y, width, 100, title, code, detail, highlight=(idx == 1))
        if idx < len(modules) - 1:
            c.setStrokeColor(colors.black)
            c.setLineWidth(4)
            c.line(x + width, rack_y + 50, x + width + 13, rack_y + 50)
        x += width + 13
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 7)
    c.drawCentredString(385, rack_y - 15, "Modules exchange safety data through the lateral PNOZmulti bus - no external signal wire is required between modules")

    # Photo evidence.
    if PHOTO.exists():
        img = ImageReader(str(PHOTO))
        c.drawImage(img, W - 230, rack_y - 15, width=165, height=125, preserveAspectRatio=True, anchor="c", mask="auto")
        c.setFont("Helvetica", 6.3)
        c.setFillColor(colors.black)
        c.drawCentredString(W - 147, rack_y - 24, "Installed module photograph: order 772170, version 2.4")

    # Main signal architecture.
    c.setFont("Helvetica-Bold", 11)
    c.drawString(35, H - 295, "2. CORRECT FIELD CONNECTION")

    bx, by, bw, bh = 370, 220, 280, 180
    c.setFillColor(YELLOW)
    c.setStrokeColor(NAVY)
    c.setLineWidth(2)
    c.roundRect(bx, by, bw, bh, 8, fill=1, stroke=1)
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 14)
    c.drawCentredString(bx + bw / 2, by + bh - 28, "PNOZ m EF 1MM")
    c.setFont("Helvetica", 8)
    c.drawCentredString(bx + bw / 2, by + bh - 44, "Safe motion monitoring - AXIS 1")
    c.setFillColor(colors.white)
    c.setStrokeColor(colors.black)
    c.roundRect(bx + bw - 82, by + 65, 58, 60, 4, fill=1, stroke=1)
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 10)
    c.drawCentredString(bx + bw - 53, by + 98, "X12")
    c.setFont("Helvetica", 6.5)
    c.drawCentredString(bx + bw - 53, by + 83, "Mini-IO 8-pin")
    c.drawCentredString(bx + bw - 53, by + 72, "encoder input")
    for i, label in enumerate(("A12", "B12", "S12", "GND")):
        c.setFillColor(colors.white)
        c.circle(bx + 54 + i * 47, by + 28, 10, fill=1, stroke=1)
        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 6.2)
        c.drawCentredString(bx + 54 + i * 47, by + 26, label)

    # Encoder.
    ex, ey = 760, 250
    c.setFillColor(LIGHT)
    c.setStrokeColor(BLUE)
    c.setLineWidth(2)
    c.roundRect(ex, ey, 270, 120, 8, fill=1, stroke=1)
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 12)
    c.drawCentredString(ex + 135, ey + 91, "MACHINE AXIS ENCODER")
    c.setFillColor(colors.black)
    c.setFont("Helvetica", 8)
    c.drawCentredString(ex + 135, ey + 70, "Existing drawing calls DFS60S Pro - verify installed unit")
    c.drawCentredString(ex + 135, ey + 54, "Shielded cable - max 30 m total")
    c.drawCentredString(ex + 135, ey + 38, "Use approved adapter when required")
    c.setFillColor(ORANGE)
    c.setFont("Helvetica-Bold", 8)
    c.drawCentredString(ex + 135, ey + 18, "AXIS IDENTITY NOT YET VALIDATED")
    arrow(c, bx + bw, by + 95, ex, ey + 60, BLUE, 3)
    c.setFillColor(BLUE)
    c.setFont("Helvetica-Bold", 7)
    c.drawCentredString(704, by + 112, "X12 encoder cable")

    # Red exclusions.
    c.setFillColor(colors.HexColor("#FDEDEC"))
    c.setStrokeColor(RED)
    c.setLineWidth(1.5)
    c.roundRect(45, 65, W - 90, 105, 7, fill=1, stroke=1)
    c.setFillColor(RED)
    c.setFont("Helvetica-Bold", 11)
    c.drawString(62, 145, "DO NOT CONNECT TO THIS MODULE")
    exclusions = [
        "Door interlock channels GS1-GS11",
        "Hood 1 FilterClogged %I12.4",
        "Hood 2 FilterClogged %I12.7",
        "Emergency-stop channels or ordinary 24 VDC PLC signals",
    ]
    c.setFont("Helvetica", 8)
    for i, item in enumerate(exclusions):
        c.drawString(70 + (i % 2) * 500, 123 - (i // 2) * 28, f"X  {item}")
    footer(c)


def pin(c, x, y, number, signal, colour):
    c.setFillColor(colour)
    c.circle(x, y, 13, fill=1, stroke=0)
    c.setFillColor(colors.white)
    c.setFont("Helvetica-Bold", 8)
    c.drawCentredString(x, y - 3, str(number))
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 7)
    c.drawCentredString(x, y - 25, signal)


def draw_page2(c):
    header(c, "PNOZ m EF 1MM - TERMINAL AND CABLE DETAIL", "Choose only the connection method configured in PNOZmulti Configurator", 2)

    # Option A: one conductor per row so the cable can be followed terminal-to-terminal.
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 12)
    c.drawString(35, H - 86, "OPTION A - ENCODER TO X12: ONE CONDUCTOR PER ROW")
    c.setFillColor(LIGHT)
    c.setStrokeColor(BLUE)
    c.roundRect(35, H - 382, W - 70, 270, 8, fill=1, stroke=1)

    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 8)
    c.drawString(62, H - 132, "PNOZ X12")
    c.drawString(255, H - 132, "DOCUMENTED DFS60S PRO CONDUCTOR")
    c.drawString(840, H - 132, "ENCODER TERMINAL / FUNCTION")

    rows = [
        (1, "S", "RED", "+U / supply monitor", colors.red),
        (2, "GND", "BLUE", "GND / 0 V", colors.HexColor("#2468C4")),
        (3, "Z", "PURPLE", "Z index", colors.HexColor("#7B2CBF")),
        (4, "A", "WHITE", "A track", colors.white),
        (5, "/A", "BROWN", "/A track", colors.HexColor("#8B5A2B")),
        (6, "/Z", "YELLOW", "/Z index", colors.HexColor("#E2B600")),
        (7, "B", "PINK", "B track", colors.HexColor("#E83E8C")),
        (8, "/B", "BLACK", "/B track", colors.black),
    ]
    y0 = H - 157
    for idx, (number, signal, wire_name, encoder_name, colour) in enumerate(rows):
        y = y0 - idx * 27
        if idx % 2 == 0:
            c.setFillColor(colors.white)
            c.rect(55, y - 11, W - 110, 23, fill=1, stroke=0)
        c.setStrokeColor(colors.HexColor("#CBD5DF"))
        c.setLineWidth(0.5)
        c.line(55, y - 12, W - 55, y - 12)

        c.setFillColor(YELLOW)
        c.setStrokeColor(NAVY)
        c.circle(85, y, 10, fill=1, stroke=1)
        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 7)
        c.drawCentredString(85, y - 2.5, str(number))
        c.drawString(108, y - 2.5, signal)

        # White conductor gets a grey outline so it remains visible.
        c.setStrokeColor(colors.HexColor("#777777") if wire_name == "WHITE" else colour)
        c.setLineWidth(4)
        c.line(190, y, 810, y)
        if wire_name == "WHITE":
            c.setStrokeColor(colors.white)
            c.setLineWidth(2)
            c.line(190, y, 810, y)

        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 7)
        c.drawString(280, y + 4, wire_name)
        c.setFont("Helvetica", 7)
        c.drawString(840, y - 2.5, encoder_name)
        c.setFillColor(colors.white)
        c.setStrokeColor(NAVY)
        c.circle(1090, y, 8, fill=1, stroke=1)

    c.setFillColor(colors.HexColor("#FFF2CC"))
    c.setStrokeColor(ORANGE)
    c.roundRect(55, H - 380, W - 110, 22, 4, fill=1, stroke=1)
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 6.8)
    c.drawCentredString(W / 2, H - 366, "+24 VDC feeds encoder +U; X12 pin 1 S only monitors that supply. 0 V connects encoder GND to X12 pin 2. Connect cable shield to PE at one point.")
    c.setFont("Helvetica", 6)
    c.drawCentredString(W / 2, H - 376, "Wire colours are taken from the existing DFS60S Pro drawing; verify the installed encoder label and continuity before termination.")

    # Option B: separate, orthogonal wiring cards with no crossing conductors.
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 12)
    c.drawString(35, H - 420, "OPTION B - TWO 3-WIRE PROXIMITY SWITCHES: SEPARATE A AND B CABLING")
    c.setFillColor(colors.HexColor("#F7F9FB"))
    c.setStrokeColor(GREEN)
    c.roundRect(35, 92, W - 70, H - 535, 8, fill=1, stroke=1)

    def proximity_card(x, title, signal_terminal):
        y = 130
        w = 535
        h = 245
        c.setFillColor(colors.white)
        c.setStrokeColor(BLUE)
        c.setLineWidth(1.3)
        c.roundRect(x, y, w, h, 7, fill=1, stroke=1)
        c.setFillColor(NAVY)
        c.setFont("Helvetica-Bold", 10)
        c.drawString(x + 15, y + h - 25, title)
        c.setFont("Helvetica", 6.5)
        c.drawRightString(x + w - 15, y + h - 25, "3-wire sensor - PNP/NPN as configured")

        card_rows = [
            ("+24 VDC PELV/SELV", "to sensor supply +", "SENSOR +", colors.red),
            ("0 VDC", "to sensor 0 V and module common", "GND", colors.black),
            ("sensor output", "motion pulse signal", signal_terminal, BLUE),
            ("+24 V monitor branch", "supply supervision only", "S12", ORANGE),
        ]
        ry = y + h - 65
        for index, (source, purpose, terminal, _line_colour) in enumerate(card_rows):
            yy = ry - index * 43
            c.setFillColor(colors.HexColor("#F3F6F9") if index % 2 == 0 else colors.white)
            c.rect(x + 12, yy - 15, w - 24, 32, fill=1, stroke=0)
            c.setFillColor(colors.black)
            c.setFont("Helvetica-Bold", 7)
            c.drawString(x + 22, yy + 2, source)
            c.setFont("Helvetica", 6.5)
            c.drawString(x + 155, yy + 2, purpose)
            c.setStrokeColor(_line_colour)
            c.setLineWidth(3)
            c.line(x + 285, yy, x + 445, yy)
            c.setFillColor(YELLOW)
            c.setStrokeColor(NAVY)
            c.roundRect(x + 440, yy - 11, 70, 22, 4, fill=1, stroke=1)
            c.setFillColor(colors.black)
            c.setFont("Helvetica-Bold", 7)
            c.drawCentredString(x + 475, yy - 2.5, terminal)
        c.setFillColor(colors.HexColor("#FFF2CC"))
        c.setStrokeColor(ORANGE)
        c.roundRect(x + 15, y + 12, w - 30, 27, 4, fill=1, stroke=1)
        c.setFillColor(colors.black)
        c.setFont("Helvetica", 6.2)
        c.drawCentredString(x + w / 2, y + 22, "S12 monitors +24 V only; GND is common 0 V. Do not use two-wire proximity switches.")

    proximity_card(55, "PROXIMITY A -> AXIS 1", "A12")
    proximity_card(600, "PROXIMITY B -> AXIS 1", "B12")

    c.setFillColor(colors.HexColor("#FFF2CC"))
    c.setStrokeColor(ORANGE)
    c.roundRect(55, 45, W - 110, 35, 5, fill=1, stroke=1)
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 7.5)
    c.drawCentredString(W / 2, 58, "INSTALLATION HOLD: confirm the selected sensor method and Axis 1 assignment in the actual PNOZmulti project before wiring or commissioning.")
    footer(c)


def main():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    c = canvas.Canvas(str(OUTPUT), pagesize=landscape(A3))
    c.setTitle("Schlenker PNOZ m EF 1MM Axis 1 Connection Architecture")
    c.setAuthor("Schlenker engineering")
    draw_page1(c)
    c.showPage()
    draw_page2(c)
    c.showPage()
    c.save()


if __name__ == "__main__":
    main()
