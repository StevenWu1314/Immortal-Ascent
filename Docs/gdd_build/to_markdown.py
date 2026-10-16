"""Export the built DOCX to a markdown file for AI/tooling consumption.

Kept in the same pipeline as build.py so the .docx and the .md can never drift:
build.py writes the DOCX and then calls export() on that exact file.
"""
import os
import re

from docx import Document
from docx.table import Table
from docx.text.paragraph import Paragraph
from docx.oxml.ns import qn

NOTE_SIZE_PT = 9.5

FRONT_MATTER = """---
title: Immortal Ascent
subtitle: Game Design Document v2 - Rogue Asian Fantasy
version: "2.0 (draft)"
platform: [Windows, macOS]
engine: Unity 2022.3.62f2
genre: turn-based tactical roguelike, room-clear map, permadeath
run_length: 30-45 minutes
realms: [Plains, Desert, Tundra]
floors_per_realm: 2-3
islands_per_floor: 6-8
bosses: 3
win_condition: defeat the Ashen Immortal at the end of the Tundra
meta_progression: blueprint unlocking, persisted in JSON
status: decisions locked; every value marked "proposed" still needs playtesting
conventions: decisions are "Decision"; unverified starting numbers are "proposed"; unresolved items are "Open" and collected in Appendix A; code gaps are "Gap" and collected in Appendix B
---
"""


def _iter_blocks(doc):
    """Yield paragraphs and tables in true document order."""
    for child in doc.element.body.iterchildren():
        if child.tag == qn("w:p"):
            yield Paragraph(child, doc)
        elif child.tag == qn("w:tbl"):
            yield Table(child, doc)


def _cell_text(cell):
    text = " ".join(cell.text.split())
    return text.replace("|", "\\|")


def _is_note(para):
    runs = [r for r in para.runs if r.text.strip()]
    if not runs:
        return False
    for run in runs:
        if not run.italic:
            return False
        size = run.font.size
        if size is None or abs(size.pt - NOTE_SIZE_PT) > 0.01:
            return False
    return True


def _is_header_row(table):
    first = table.rows[0]._tr
    return first.find(qn("w:trPr")) is not None and first.find(
        qn("w:trPr")
    ).find(qn("w:tblHeader")) is not None


def export(docx_path, md_path):
    doc = Document(docx_path)
    blocks = []
    for block in _iter_blocks(doc):
        if isinstance(block, Table):
            rows = [[_cell_text(c) for c in row.cells] for row in block.rows]
            if rows:
                blocks.append(("table", rows, _is_header_row(block)))
            continue

        text = " ".join(block.text.split())
        if not text:
            continue
        style = block.style.name if block.style is not None else "Normal"

        if style == "Title":
            blocks.append(("heading", 1, text))
        elif style.startswith("Heading"):
            level = int(re.sub(r"\D", "", style) or 1) + 1
            blocks.append(("heading", min(level, 6), text))
        elif _is_note(block):
            blocks.append(("note", text))
        elif style == "List Bullet":
            blocks.append(("bullet", text))
        elif re.match(r"^\d+\. ", text):
            blocks.append(("number", text))
        else:
            blocks.append(("para", text))

    out = [FRONT_MATTER.rstrip(), ""]

    def blank():
        if out and out[-1] != "":
            out.append("")

    prev = None
    for block in blocks:
        kind = block[0]
        if kind == "table":
            blank()
            rows, has_header = block[1], block[2]
            header, body = (rows[0], rows[1:]) if has_header else (["Field", "Value"], rows)
            out.append("| " + " | ".join(header) + " |")
            out.append("|" + "|".join([" --- "] * len(header)) + "|")
            for row in body:
                out.append("| " + " | ".join(row) + " |")
            out.append("")
        elif kind == "heading":
            blank()
            out.append("#" * block[1] + " " + block[2])
            out.append("")
        elif kind == "note":
            blank()
            out.append("> " + block[1])
            out.append("")
        elif kind == "bullet":
            if prev != "bullet":
                blank()
            out.append("- " + block[1])
        elif kind == "number":
            if prev != "number":
                blank()
            out.append(block[1])
        else:
            blank()
            out.append(block[1])
            out.append("")
        prev = kind

    with open(md_path, "w", encoding="utf-8") as handle:
        handle.write("\n".join(out).rstrip() + "\n")
    return md_path


if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    src = os.path.abspath(os.path.join(here, "..", "Immortal-Ascent-GDD-v2.docx"))
    dst = os.path.abspath(os.path.join(here, "..", "Immortal-Ascent-GDD-v2.md"))
    print("wrote", export(src, dst))
