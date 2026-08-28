from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(r"C:/Users/FNHF/.codex/generated_images/01a03335-8dbb-7852-af5d-51ea934172b7")
ASSETS = {
    "academy_ground_macro_court_3x3": ("exec-f50f4411-6196-4795-8f88-6d3442d56e50.png", [(184, 176, 158), (198, 190, 171), (210, 202, 183), (222, 214, 195), (232, 225, 208)]),
    "academy_ground_macro_road_3x3": ("exec-2e05d7e0-b25f-4fbc-bc3f-36fb98ae7811.png", [(82, 82, 79), (99, 97, 91), (116, 112, 103), (132, 126, 115), (148, 140, 126)]),
    "academy_ground_macro_ruin_3x3": ("exec-823082a5-940f-4a3c-a9ff-b9f2726b26b6.png", [(83, 83, 70), (112, 111, 92), (139, 136, 112), (161, 156, 128), (181, 174, 143), (198, 191, 159)]),
    "academy_ground_macro_earth_3x3": ("exec-f2056995-e262-42b5-b8a8-4438a6b14110.png", [(102, 75, 55), (119, 84, 60), (137, 95, 68), (153, 108, 77), (170, 125, 91)]),
    "academy_ground_macro_court_b_3x3": ("exec-9aee8d1f-ca8f-4c3a-baef-c892f1017f89.png", [(184, 176, 158), (198, 190, 171), (210, 202, 183), (222, 214, 195), (232, 225, 208)]),
    "academy_ground_macro_road_b_3x3": ("exec-3d656041-a06d-42f2-874a-367fb02c1252.png", [(82, 82, 79), (99, 97, 91), (116, 112, 103), (132, 126, 115), (148, 140, 126)]),
    "academy_ground_macro_ruin_b_3x3": ("exec-58ee6ff4-202e-4926-aff6-27d90030fb3c.png", [(83, 83, 70), (112, 111, 92), (139, 136, 112), (161, 156, 128), (181, 174, 143), (198, 191, 159)]),
    "academy_ground_macro_earth_b_3x3": ("exec-c439d98e-3e20-481c-a540-ff329e8956fd.png", [(102, 75, 55), (119, 84, 60), (137, 95, 68), (153, 108, 77), (170, 125, 91)]),
}


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    source_dir = ROOT / "source" / "terrain_ground_macros_v16"
    normalized_dir = ROOT / "normalized" / "terrain_ground_macros_v16"
    qa_dir = ROOT / "QA" / "terrain_ground_macros_v16"
    manifest_dir = ROOT / "manifests" / "terrain_ground_macros_v16"
    for directory in (source_dir, normalized_dir, qa_dir, manifest_dir):
        directory.mkdir(parents=True, exist_ok=True)

    rows = (len(ASSETS) + 1) // 2
    contact = Image.new("RGB", (800, rows * 400), (41, 39, 35))
    draw = ImageDraw.Draw(contact)
    for index, (asset_id, (generated_name, palette)) in enumerate(ASSETS.items()):
        generated = GENERATED / generated_name
        source = source_dir / f"{asset_id}_source.png"
        source.write_bytes(generated.read_bytes())
        raw = Image.open(source).convert("RGB")
        side = min(raw.size)
        crop = ImageOps.fit(raw, (side, side), method=Image.Resampling.LANCZOS)
        pixel = crop.resize((96, 96), Image.Resampling.LANCZOS)
        reduced = pixel.quantize(colors=len(palette), method=Image.Quantize.MEDIANCUT).convert("RGB")
        source_colors = sorted(set(reduced.getdata()), key=lambda color: sum(color))
        mapping = {color: palette[min(index, len(palette) - 1)] for index, color in enumerate(source_colors)}
        pixel = Image.new("RGB", reduced.size)
        pixel.putdata([mapping[color] for color in reduced.getdata()])
        output = normalized_dir / f"{asset_id}.png"
        pixel.save(output, optimize=True)
        pixel.resize((384, 384), Image.Resampling.NEAREST).save(qa_dir / f"{asset_id}_4x.png", optimize=True)
        x = (index % 2) * 400 + 8
        y = (index // 2) * 400 + 8
        contact.paste(pixel.resize((384, 384), Image.Resampling.NEAREST), (x, y))
        draw.text((x + 8, y + 8), asset_id.replace("academy_ground_macro_", ""), fill=(26, 24, 22))
        manifest = {
            "schema": "occ-art-manifest-v1",
            "asset_id": asset_id,
            "status": "FORMAL_CANDIDATE",
            "role": "quiet_multi_cell_ground_macro",
            "logical_cells": [3, 3],
            "source": str(source.relative_to(ROOT)).replace("\\", "/"),
            "source_sha256": digest(source),
            "normalized": str(output.relative_to(ROOT)).replace("\\", "/"),
            "normalized_sha256": digest(output),
            "generation_channel": "codex_builtin_imagegen",
            "opaque_color_count": len(set(pixel.getdata())),
            "qa": ["1x", "4x", "grayscale", "application_contact_required", "macro_phase_runtime_sampling"],
            "review": "candidate pending Unity application contact and importer/runtime verification",
        }
        (manifest_dir / f"{asset_id}.occ-art-manifest-v1.json").write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    contact.save(qa_dir / "academy_ground_macros_v16_contact.png", optimize=True)


if __name__ == "__main__":
    main()
