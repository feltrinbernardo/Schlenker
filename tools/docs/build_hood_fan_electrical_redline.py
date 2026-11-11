from io import BytesIO
from pathlib import Path

from pypdf import PdfReader, PdfWriter
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4, landscape
from reportlab.pdfgen import canvas


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "eletrical drawing" / "SCHLENKER 3610 v05.pdf"
OUTPUT = ROOT / "output" / "pdf" / "SCHLENKER_3610_v06_HOOD_FAN_ELECTRICAL_REDLINE.pdf"


def overlay_page(draw_callback):
    packet = BytesIO()
    c = canvas.Canvas(packet, pagesize=landscape(A4))
    draw_callback(c)
    c.save()
    packet.seek(0)
    return PdfReader(packet).pages[0]


def small_text(c, x, y, text, size=6.5, color=colors.black):
    c.setFillColor(color)
    c.setFont("Helvetica", size)
    c.drawString(x, y, text)


def draw_sheet15(c):
    # Replace only the obsolete hood-fan branch; the vacuum branch is retained.
    c.setFillColor(colors.white)
    c.rect(168, 195, 655, 188, fill=1, stroke=0)
    c.setStrokeColor(colors.red)
    c.setLineWidth(2)
    c.rect(170, 200, 645, 177, fill=0, stroke=1)
    c.setFillColor(colors.red)
    c.setFont("Helvetica-Bold", 9)
    c.drawString(180, 360, "REV B - SUPERSEDES OLD M105 400 VAC / 3-PHASE / 4 kW HOOD-FAN BRANCH")

    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 8)
    c.drawString(180, 344, "TWO COMPLETE HOOD ASSEMBLIES - 230 VAC SINGLE PHASE")

    # Incoming L/N/PE rails.
    rails = [(colors.red, "L"), (colors.blue, "N"), (colors.green, "PE")]
    for idx, (colour, label) in enumerate(rails):
        y = 325 - idx * 14
        c.setStrokeColor(colour)
        c.setLineWidth(1.5)
        c.line(185, y, 790, y)
        small_text(c, 175, y - 2, label, 7, colour)

    for x, title, motor_line, data_line in (
        (285, "M104A - HOOD 1", "Motor: Nidec SIC-67FV-F15B-1", "DC 310 V motor bus | 58 W | 8P | 1350 rpm"),
        (570, "M104B - HOOD 2", "Motor plate: VERIFY ON INSTALLED HOOD", "Same 230 VAC assembly architecture stated by operator"),
    ):
        c.setStrokeColor(colors.darkblue)
        c.setLineWidth(1)
        c.roundRect(x, 230, 205, 62, 5, fill=0, stroke=1)
        c.setFillColor(colors.darkblue)
        c.setFont("Helvetica-Bold", 7.5)
        c.drawCentredString(x + 102.5, 278, title)
        small_text(c, x + 8, 265, "Complete hood input: L + N + PE", 6.5)
        small_text(c, x + 8, 253, "Associated/integrated brushless electronics", 6.5)
        small_text(c, x + 8, 241, motor_line, 6.5)
        small_text(c, x + 8, 231, data_line, 6.2)

        for idx, colour in enumerate((colors.red, colors.blue, colors.green)):
            rail_y = 325 - idx * 14
            c.setStrokeColor(colour)
            c.line(x + 30 + idx * 18, rail_y, x + 30 + idx * 18, 292)

    c.setFillColor(colors.red)
    c.setFont("Helvetica-Bold", 6.8)
    c.drawString(180, 214, "DO NOT CONNECT MOTOR LEADS DIRECTLY TO MAINS, VFD, CONTACTOR OUTPUT OR PLC 24 VDC.")
    small_text(c, 180, 204, "Protection rating, assembly terminals, controller/control interface and Hood 2 nameplate: VERIFY BEFORE CONSTRUCTION.", 6.2)


def draw_sheet31(c):
    # Replace the obsolete K105/DQ11 direct-contactor callout.
    c.setFillColor(colors.white)
    c.rect(600, 180, 225, 205, fill=1, stroke=0)
    c.setStrokeColor(colors.red)
    c.setLineWidth(2)
    c.rect(603, 185, 215, 192, fill=0, stroke=1)
    c.setFillColor(colors.red)
    c.setFont("Helvetica-Bold", 8)
    c.drawString(612, 360, "REV B - K105 3-PHASE CONTACTOR REMOVED")
    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 7)
    c.drawString(612, 340, "HOOD ENABLE INTERFACE - HOLD POINT")
    small_text(c, 612, 325, "%Q4.2 / AirFilterRelayCmd: probable common enable", 6.4)
    small_text(c, 612, 312, "for Hood 1 and Hood 2 - association not yet proven.", 6.4)
    small_text(c, 612, 294, "PLC output may drive only an approved interposing", 6.4)
    small_text(c, 612, 281, "relay/interface matched to the hood controller input.", 6.4)
    small_text(c, 612, 260, "NO direct connection to motor or 230 VAC load.", 6.4, colors.red)
    small_text(c, 612, 240, "Separate clogged-filter inputs retained:", 6.4)
    small_text(c, 612, 227, "Hood 1: %I12.4 | Hood 2: %I12.7", 6.4)
    small_text(c, 612, 204, "Verify terminals, voltage and interface before wiring.", 6.2)


def add_revision_sheet(writer):
    packet = BytesIO()
    c = canvas.Canvas(packet, pagesize=landscape(A4))
    width, height = landscape(A4)
    c.setFillColor(colors.HexColor("#17365D"))
    c.rect(0, height - 58, width, 58, fill=1, stroke=0)
    c.setFillColor(colors.white)
    c.setFont("Helvetica-Bold", 17)
    c.drawString(30, height - 35, "HOOD FAN ELECTRICAL CORRECTION - REV B")
    c.setFont("Helvetica", 8)
    c.drawRightString(width - 30, height - 35, "SCHLENKER 36/10 | 2026-10-30")

    c.setFillColor(colors.black)
    c.setFont("Helvetica-Bold", 11)
    c.drawString(35, height - 90, "Corrected architecture")
    c.setFont("Helvetica", 8)
    c.drawString(35, height - 108, "The old M105 400 VAC, three-phase, 4 kW branch and K105 contactor are rejected for the photographed hood fan.")
    c.drawString(35, height - 123, "Each complete hood is stated by the operator as 220-230 VAC single-phase. The motor-side plate is DC 310 V brushless.")

    y = height - 170
    for title, detail in (
        ("M104A - HOOD 1", "230 VAC L-N-PE -> associated/integrated electronics -> Nidec SIC-67FV-F15B-1"),
        ("M104B - HOOD 2", "230 VAC L-N-PE -> associated/integrated electronics -> motor plate to verify"),
    ):
        c.setFillColor(colors.HexColor("#EAF2F8"))
        c.roundRect(55, y - 55, width - 110, 48, 6, fill=1, stroke=0)
        c.setFillColor(colors.HexColor("#17365D"))
        c.setFont("Helvetica-Bold", 9)
        c.drawString(70, y - 27, title)
        c.setFillColor(colors.black)
        c.setFont("Helvetica", 8)
        c.drawString(220, y - 27, detail)
        y -= 72

    c.setFillColor(colors.HexColor("#FFF2CC"))
    c.roundRect(55, 105, width - 110, 125, 6, fill=1, stroke=0)
    c.setFillColor(colors.red)
    c.setFont("Helvetica-Bold", 10)
    c.drawString(70, 210, "NOT RELEASED FOR CONSTRUCTION - CLOSE THESE HOLD POINTS")
    c.setFillColor(colors.black)
    c.setFont("Helvetica", 8)
    holds = [
        "Identify each hood's mains input terminals and associated/integrated controller.",
        "Confirm protective device rating, conductor size, isolation method and control input voltage.",
        "Verify the Hood 2 motor nameplate.",
        "Prove whether %Q4.2 is a common enable and select the required interposing interface.",
        "Never apply 230 VAC directly to the motor-interface leads.",
    ]
    for i, hold in enumerate(holds):
        c.drawString(75, 190 - i * 17, f"- {hold}")

    c.setFillColor(colors.grey)
    c.setFont("Helvetica", 7)
    c.drawString(35, 28, "Source drawing retained except for the redlined hood-fan portions on sheets 15 and 31.")
    c.save()
    packet.seek(0)
    writer.add_page(PdfReader(packet).pages[0])


def main():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    reader = PdfReader(SOURCE)
    writer = PdfWriter()

    add_revision_sheet(writer)
    for index, source_page in enumerate(reader.pages):
        page = source_page
        page.transfer_rotation_to_content()
        if index == 11:
            page.merge_page(overlay_page(draw_sheet15))
        elif index == 19:
            page.merge_page(overlay_page(draw_sheet31))
        writer.add_page(page)

    writer.add_metadata(
        {
            "/Title": "SCHLENKER 3610 v06 - Hood Fan Electrical Redline",
            "/Subject": "Correction of hood fan supply and motor architecture",
            "/Author": "Schlenker engineering redline",
        }
    )
    with OUTPUT.open("wb") as stream:
        writer.write(stream)


if __name__ == "__main__":
    main()
