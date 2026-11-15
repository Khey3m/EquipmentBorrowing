"""Build a Word report from supplied screenshots and evidence-scoped captions."""
import argparse
import json
from pathlib import Path
import shutil
from docx import Document
from docx.shared import Inches, Pt, RGBColor

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--copy-to", type=Path, required=True)
args = parser.parse_args()
if args.copy_to.exists():
    parser.error("Choose a new output filename to preserve the previous report.")
folder = root / "docs/lab5/manual-evidence"
data = json.loads((folder / "captions.json").read_text(encoding="utf-8"))
doc = Document()
section = doc.sections[0]
section.top_margin = section.bottom_margin = Inches(.65)
section.left_margin = section.right_margin = Inches(.7)
doc.styles["Normal"].font.name = "Calibri"
doc.styles["Normal"].font.size = Pt(11)
for style in ["Title", "Heading 1", "Heading 2"]:
    doc.styles[style].font.color.rgb = RGBColor.from_string("126A78")
doc.add_heading(data["title"], 0)
doc.add_paragraph("User-supplied screenshots with explanations below each image. "
                  "This report is being assembled as evidence arrives; it does not certify unshown actions.")
for index, entry in enumerate(data["entries"]):
    if index:
        doc.add_page_break()
    doc.add_heading(entry["heading"], 1)
    doc.add_picture(str(folder / entry["image"]), width=Inches(6.5))
    doc.add_paragraph(entry["id"]).runs[0].italic = True
    doc.add_heading("Explanation", 2)
    for text in entry["explanation"]:
        doc.add_paragraph(text)
    doc.add_paragraph("Evidence scope: " + entry["scope"])
output = root / "docs/lab5/DEMO_LAB_5_SCREENSHOT_REPORT.docx"
doc.save(output)
args.copy_to.parent.mkdir(parents=True, exist_ok=True)
shutil.copyfile(output, args.copy_to)
print(f"Report built with {len(data['entries'])} supplied screenshot(s): {args.copy_to}")
