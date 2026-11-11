from io import BytesIO
from pathlib import Path

from pypdf import PdfReader, PdfWriter
from reportlab.lib.colors import HexColor
from reportlab.pdfgen import canvas


ROOT = Path(__file__).resolve().parents[2]
PDF = ROOT / "output" / "pdf" / "REV12_Project_Tree_Architecture.pdf"
OUTPUT = ROOT / "output" / "pdf" / "REV12_Project_Tree_Architecture_X8_CORRECTED.pdf"
PAGE_INDEX = 7


def overlay_page(width: float, height: float):
    stream = BytesIO()
    c = canvas.Canvas(stream, pagesize=(width, height))

    # Preserve the original yellow card and blue border; replace only its stale text.
    c.setFillColor(HexColor("#FFF1B0"))
    c.rect(804, 181, 329, 60, fill=1, stroke=0)

    c.setFillColor(HexColor("#17365D"))
    c.setFont("Helvetica-Bold", 10.5)
    c.drawCentredString(968.5, 222, "P8 - OUTPUT - Stack Light Output Hub")
    c.setFont("Helvetica", 7.5)
    c.drawCentredString(968.5, 207, "DO-HUB-TL100 | IFM AL2330 | IO-Link 8I/8O")
    c.drawCentredString(968.5, 195, "%QB156..%QB163 | GREEN / ORANGE / RED / BUZZER")
    c.save()
    stream.seek(0)
    return PdfReader(stream).pages[0]


def main():
    reader = PdfReader(str(PDF))
    page = reader.pages[PAGE_INDEX]
    text = page.extract_text() or ""
    if "AL102 - IFM AL1403 GRAPHICAL PORT MAP" not in text:
        raise RuntimeError("Expected AL102 graphical port-map page was not found at page 8.")

    page.merge_page(overlay_page(float(page.mediabox.width), float(page.mediabox.height)))
    writer = PdfWriter()
    for current in reader.pages:
        writer.add_page(current)

    temporary = OUTPUT.with_suffix(".tmp.pdf")
    with temporary.open("wb") as stream:
        writer.write(stream)
    temporary.replace(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
