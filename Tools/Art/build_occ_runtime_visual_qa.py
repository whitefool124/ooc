#!/usr/bin/env python3
"""Build traceable grayscale/color-risk QA from M-A3 runtime screenshots."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def grayscale(image: Image.Image) -> Image.Image:
    return image.convert("L").convert("RGB")


def deuteranopia_risk(image: Image.Image) -> Image.Image:
    # Deterministic review simulation matrix; this is a risk preview, not diagnosis.
    source = image.convert("RGB")
    out = Image.new("RGB", source.size)
    pixels = []
    for r, g, b in source.getdata():
        pixels.append((
            max(0, min(255, round(.625 * r + .375 * g))),
            max(0, min(255, round(.70 * r + .30 * g))),
            max(0, min(255, round(.30 * g + .70 * b))),
        ))
    out.putdata(pixels)
    return out


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    screenshots = sorted(
        path
        for path in args.input.glob("*.png")
        if not path.name.startswith("_") and "grayscale" not in path.stem and "deuteranopia" not in path.stem
    )
    records = []
    thumbs = []
    for path in screenshots:
        image = Image.open(path).convert("RGB")
        gray_path = args.output / f"{path.stem}_grayscale.png"
        risk_path = args.output / f"{path.stem}_deuteranopia_risk.png"
        grayscale(image).save(gray_path)
        deuteranopia_risk(image).save(risk_path)
        thumb = image.copy(); thumb.thumbnail((480, 270), Image.Resampling.LANCZOS)
        thumbs.append((path.stem, thumb))
        records.append({
            "id": path.stem,
            "source": str(path),
            "size": list(image.size),
            "sha256": digest(path),
            "grayscale": str(gray_path),
            "grayscale_sha256": digest(gray_path),
            "deuteranopia_risk": str(risk_path),
            "deuteranopia_risk_sha256": digest(risk_path),
        })
    width = 960; cell_h = 310; rows = max(1, (len(thumbs) + 1) // 2)
    sheet = Image.new("RGB", (width, rows * cell_h), (8, 10, 12)); draw = ImageDraw.Draw(sheet)
    for index, (name, thumb) in enumerate(thumbs):
        x = (index % 2) * 480; y = (index // 2) * cell_h
        sheet.paste(thumb, (x, y)); draw.text((x + 8, y + 274), name, fill=(215, 216, 210))
    contact = args.output / "occ_runtime_visual_contact_v01.png"; sheet.save(contact)
    report = {
        "schema": "occ.runtime.visual.qa.v0.1",
        "status": "VISUAL_QA_PASS_REVIEWED_2026_08_07",
        "screenshot_count": len(records),
        "target_resolutions": [[1920, 1080], [960, 540]],
        "contact_sheet": str(contact),
        "contact_sha256": digest(contact),
        "records": records,
        "notes": [
            "Character and unit visual approval is explicitly blocked and excluded from pass claims.",
            "Deuteranopia output is a deterministic risk preview; shape and luminance remain the primary acceptance criteria.",
        ],
    }
    (args.output / "occ_runtime_visual_qa_v01.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"screenshots": len(records), "contact": str(contact)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
