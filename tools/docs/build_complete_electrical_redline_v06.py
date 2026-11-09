from io import BytesIO
from pathlib import Path

from pypdf import PdfReader, PdfWriter
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4, landscape
from reportlab.pdfgen import canvas


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "output" / "pdf" / "SCHLENKER_3610_v06_HOOD_FAN_ELECTRICAL_REDLINE.pdf"
OUTPUT = ROOT / "output" / "pdf" / "SCHLENKER_3610_v06_COMPLETE_ELECTRICAL_REDLINE_REV_D.pdf"
PAGE = landscape(A4)
W, H = PAGE
CABINET_PAGE = PAGE
NAVY = colors.HexColor("#17365D")
BLUE = colors.HexColor("#1F77B4")
RED = colors.HexColor("#D62728")
ORANGE = colors.HexColor("#F5A623")
YELLOW = colors.HexColor("#FFD400")
LIGHT = colors.HexColor("#EEF3F8")


def overlay(draw, pagesize=PAGE):
    stream = BytesIO()
    c = canvas.Canvas(stream, pagesize=pagesize)
    draw(c)
    c.save()
    stream.seek(0)
    return PdfReader(stream).pages[0]


def cabinet_layout_pnoz_stamp(c):
    """Replace the obsolete rack thumbnail on original Sheet 3."""
    x, y, w, h = 334, 410, 122, 67
    c.setFillColor(colors.white)
    c.setStrokeColor(RED)
    c.setLineWidth(1)
    c.roundRect(x, y, w, h, 2, fill=1, stroke=1)
    c.setFillColor(RED)
    c.setFont("Helvetica-Bold", 3.8)
    c.drawString(x + 3, y + h - 7, "REV C - COMPLETE PNOZ RACK")

    modules = [
        ("ES", "PN", 11, colors.HexColor("#B6C98A")),
        ("B0", "a1", 17, YELLOW),
        ("1MM", "a3", 19, colors.HexColor("#FFCA28")),
        ("16DI", "a4", 15, YELLOW),
        ("8/4", "a5", 16, YELLOW),
    ]
    mx = x + 3
    my = y + 20
    for name, address, width, fill in modules:
        c.setFillColor(fill)
        c.setStrokeColor(NAVY)
        c.setLineWidth(1)
        c.roundRect(mx, my, width, 31, 1, fill=1, stroke=1)
        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 3.3)
        c.drawCentredString(mx + width / 2, my + 19, name)
        c.setFont("Helvetica", 2.8)
        c.drawCentredString(mx + width / 2, my + 8, address)
        mx += width + 1

    for relay in ("S1", "S1"):
        c.setFillColor(colors.white)
        c.setStrokeColor(colors.black)
        c.rect(mx, my, 9, 31, fill=1, stroke=1)
        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 3)
        c.drawCentredString(mx + 4.5, my + 19, relay)
        mx += 10

    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 3.2)
    c.drawString(x + 3, y + 12, "a3 = PNOZ m EF 1MM 772170 - ENCODER X12")
    c.setFillColor(ORANGE)
    c.setFont("Helvetica-Bold", 2.8)
    c.drawString(x + 3, y + 5, "ORDER: ES - B0 - EF1MM - 16DI - 8DI4DO")

    # Add the missing PROFINET-to-EtherCAT gateway in the ELV device row.
    gx, gy, gw, gh = 508, 421, 40, 47
    c.setFillColor(colors.white)
    c.setStrokeColor(ORANGE)
    c.setLineWidth(1.2)
    c.roundRect(gx, gy, gw, gh, 2, fill=1, stroke=1)
    c.setFillColor(colors.HexColor("#303841"))
    c.roundRect(gx + 5, gy + 12, gw - 10, 25, 1.5, fill=1, stroke=0)
    c.setFillColor(colors.white)
    c.setFont("Helvetica-Bold", 4.1)
    c.drawCentredString(gx + gw / 2, gy + 27, "ANYBUS")
    c.setFont("Helvetica", 3.2)
    c.drawCentredString(gx + gw / 2, gy + 19, "ABC3113-A")
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 3)
    c.drawCentredString(gx + gw / 2, gy + 5, "PROFINET / EtherCAT")


def network_topology_anybus_stamp(c):
    """Insert the missing protocol gateway in original network topology Sheet 40."""
    x, y, w, h = 306, 344, 130, 78
    c.setFillColor(colors.white)
    c.rect(x, y, w, h, fill=1, stroke=0)
    c.setStrokeColor(colors.HexColor("#E600D7"))
    c.setLineWidth(3)
    c.line(x - 10, y + 40, x + 8, y + 40)
    c.line(x + w - 8, y + 40, x + w + 10, y + 40)
    c.setFillColor(colors.HexColor("#303841"))
    c.setStrokeColor(ORANGE)
    c.setLineWidth(1.5)
    c.roundRect(x + 8, y + 17, w - 16, 46, 4, fill=1, stroke=1)
    c.setFillColor(colors.white)
    c.setFont("Helvetica-Bold", 6.2)
    c.drawCentredString(x + w / 2, y + 46, "HMS ANYBUS ABC3113-A")
    c.setFont("Helvetica", 5)
    c.drawCentredString(x + w / 2, y + 32, "PROFINET <-> EtherCAT GATEWAY")
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 4.3)
    c.drawString(x + 2, y + 6, "PROFINET FROM SCALANCE")
    c.drawRightString(x + w - 2, y + 6, "EtherCAT TO SMC EX260")


def sheet21_stamp(c):
    c.setFillColor(colors.white)
    c.setStrokeColor(RED)
    c.setLineWidth(2)
    c.roundRect(500, 455, 315, 95, 5, fill=1, stroke=1)
    c.setFillColor(RED)
    c.setFont("Helvetica-Bold", 9)
    c.drawString(512, 530, "REV B - SAFE MOTION MODULE ADDED")
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 7.5)
    c.drawString(512, 510, "PNOZ m EF 1MM | ORDER 772170 | AXIS 1")
    c.setFont("Helvetica", 6.5)
    c.drawString(512, 493, "Installed in the PNOZmulti rack and connected by lateral bus.")
    c.drawString(512, 479, "DFS60S Pro encoder connects to X12 - see new Sheet 23A.")
    c.setFillColor(ORANGE)
    c.setFont("Helvetica-Bold", 6.5)
    c.drawString(512, 464, "Validate hardware order and Axis 1 project assignment before operation.")


def sheet31_encoder_stamp(c):
    # Cover the old encoder-to-Siemens DI0...DI5 branch only.
    c.setFillColor(colors.white)
    c.rect(35, 420, 360, 150, fill=1, stroke=0)
    c.setStrokeColor(RED)
    c.setLineWidth(2)
    c.roundRect(42, 432, 340, 125, 5, fill=0, stroke=1)
    c.setFillColor(RED)
    c.setFont("Helvetica-Bold", 9)
    c.drawString(54, 538, "REV B - OLD ENCODER DI0...DI5 WIRING SUPERSEDED")
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 8)
    c.drawString(54, 516, "DFS60S PRO -> PNOZ m EF 1MM X12")
    c.setFont("Helvetica", 6.8)
    c.drawString(54, 496, "Do not parallel encoder tracks directly to the Siemens DI module.")
    c.drawString(54, 481, "Siemens DI0...DI5 allocation requires review before reuse.")
    c.drawString(54, 466, "Complete pin, colour, supply and shield wiring: Sheet 23A.")
    c.setFillColor(ORANGE)
    c.setFont("Helvetica-Bold", 6.5)
    c.drawString(54, 447, "Cabinet wiring by qualified personnel with supply isolated.")


def add_sheet23a(writer):
    stream = BytesIO()
    c = canvas.Canvas(stream, pagesize=PAGE)
    c.setFillColor(NAVY)
    c.rect(0, H - 50, W, 50, fill=1, stroke=0)
    c.setFillColor(colors.white)
    c.setFont("Helvetica-Bold", 15)
    c.drawString(25, H - 29, "PNOZ m EF 1MM - DFS60S PRO ENCODER WIRING")
    c.setFont("Helvetica", 7)
    c.drawRightString(W - 25, H - 29, "SCHLENKER 36/10 | SHEET 23A | REV B | 2026-10-30")

    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 9)
    c.drawString(28, H - 72, "RACK POSITION")
    labels = [
        ("PNOZ m ES", "PROFINET", 105),
        ("PNOZ m B0", "772100", 105),
        ("PNOZ m EF 1MM", "a3 | 772170", 125),
        ("PNOZ m EF 16DI", "a4 | 772140", 118),
        ("PNOZ m EF 8DI4DO", "a5 | 772142", 125),
    ]
    x = 30
    for idx, (name, code, width) in enumerate(labels):
        c.setFillColor(YELLOW if idx == 2 else colors.HexColor("#FFE86A"))
        c.setStrokeColor(NAVY)
        c.roundRect(x, H - 142, width, 52, 4, fill=1, stroke=1)
        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 6.8)
        c.drawCentredString(x + width / 2, H - 112, name)
        c.setFont("Helvetica", 6)
        c.drawCentredString(x + width / 2, H - 127, code)
        if idx < len(labels) - 1:
            c.setStrokeColor(colors.black)
            c.setLineWidth(3)
            c.line(x + width, H - 116, x + width + 10, H - 116)
        x += width + 10

    c.setFont("Helvetica-Bold", 9)
    c.setFillColor(colors.black)
    c.drawString(28, H - 168, "X12 TERMINAL-TO-TERMINAL CONNECTION")
    c.setFillColor(LIGHT)
    c.setStrokeColor(BLUE)
    c.roundRect(25, 115, W - 50, H - 300, 6, fill=1, stroke=1)
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 7)
    c.drawString(45, H - 190, "PNOZ X12")
    c.drawString(155, H - 190, "DFS60S PRO CONDUCTOR")
    c.drawString(605, H - 190, "ENCODER FUNCTION")

    rows = [
        (1, "S", "RED", "+U / monitored +24 VDC", colors.red),
        (2, "GND", "BLUE", "GND / 0 V", colors.HexColor("#2468C4")),
        (3, "Z", "PURPLE", "Z index", colors.HexColor("#7B2CBF")),
        (4, "A", "WHITE", "A track", colors.white),
        (5, "/A", "BROWN", "/A track", colors.HexColor("#8B5A2B")),
        (6, "/Z", "YELLOW", "/Z index", colors.HexColor("#E2B600")),
        (7, "B", "PINK", "B track", colors.HexColor("#E83E8C")),
        (8, "/B", "BLACK", "/B track", colors.black),
    ]
    y0 = H - 214
    for idx, (pin, track, wire, function, colour) in enumerate(rows):
        y = y0 - idx * 31
        if idx % 2 == 0:
            c.setFillColor(colors.white)
            c.rect(38, y - 12, W - 76, 24, fill=1, stroke=0)
        c.setFillColor(YELLOW)
        c.setStrokeColor(NAVY)
        c.circle(58, y, 9, fill=1, stroke=1)
        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 6.5)
        c.drawCentredString(58, y - 2.5, str(pin))
        c.drawString(79, y - 2.5, track)
        c.setStrokeColor(colors.HexColor("#777777") if wire == "WHITE" else colour)
        c.setLineWidth(4)
        c.line(145, y, 585, y)
        if wire == "WHITE":
            c.setStrokeColor(colors.white)
            c.setLineWidth(2)
            c.line(145, y, 585, y)
        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 6.5)
        c.drawString(165, y + 4, wire)
        c.setFont("Helvetica", 6.5)
        c.drawString(605, y - 2.5, function)

    c.setFillColor(colors.HexColor("#FFF2CC"))
    c.setStrokeColor(ORANGE)
    c.roundRect(38, 102, W - 76, 40, 5, fill=1, stroke=1)
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 6.6)
    c.drawString(50, 130, "+24 VDC PELV/SELV -> encoder +U (red) and X12 pin 1 S. Pin 1 monitors supply; it does not power the encoder.")
    c.drawString(50, 118, "0 VDC -> encoder GND (blue) and X12 pin 2 GND. Connect cable shield to PE at one point only.")
    c.setFillColor(RED)
    c.drawString(50, 106, "Remove the old direct encoder connection to Siemens DI0...DI5. Do not parallel signals without an approved adapter and assessment.")

    c.setStrokeColor(colors.HexColor("#AAB7C4"))
    c.line(25, 85, W - 25, 85)
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 6.5)
    c.drawString(25, 70, "COMMISSIONING HOLD")
    c.setFont("Helvetica", 6.2)
    c.drawString(25, 57, "Verify encoder label/continuity, PNOZmulti Axis 1 assignment, signal type and safety validation before operation.")
    c.drawString(25, 44, "All physical wiring shall be carried out de-energised by qualified electrical and functional-safety personnel.")
    c.setFont("Helvetica", 5.8)
    c.drawRightString(W - 25, 20, "Basis: Pilz manual 1003108-EN-16 | Sheet inserted into complete electrical redline v06")
    c.save()
    stream.seek(0)
    writer.add_page(PdfReader(stream).pages[0])


def main():
    if not SOURCE.exists():
        raise FileNotFoundError(f"Generate the hood-fan v06 redline first: {SOURCE}")
    reader = PdfReader(SOURCE)
    writer = PdfWriter()
    for index, page in enumerate(reader.pages):
        page.transfer_rotation_to_content()
        # Base redline page indices: cover=0, original Sheet 21=16, Sheet 23=18, Sheet 31=20.
        if index == 5:
            page.merge_page(overlay(cabinet_layout_pnoz_stamp, CABINET_PAGE))
        if index == 23:
            page.merge_page(overlay(network_topology_anybus_stamp))
        if index == 16:
            page.merge_page(overlay(sheet21_stamp))
        if index == 20:
            page.merge_page(overlay(sheet31_encoder_stamp))
        writer.add_page(page)
        if index == 18:
            add_sheet23a(writer)
    writer.add_metadata({
        "/Title": "SCHLENKER 3610 v06 - Complete Electrical Redline Rev D",
        "/Subject": "Hood fan, PNOZ m EF 1MM encoder and Anybus gateway corrections",
        "/Author": "Schlenker engineering redline",
    })
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT.open("wb") as handle:
        writer.write(handle)


if __name__ == "__main__":
    main()
