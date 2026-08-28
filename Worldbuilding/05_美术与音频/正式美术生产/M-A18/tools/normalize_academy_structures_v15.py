from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(r"C:/Users/FNHF/.codex/generated_images/01a03335-8dbb-7852-af5d-51ea934172b7")
ASSETS = {
    "academy_cloister_wall_4x1": ("exec-aba7b522-6e58-4a00-836b-04c2e0561ae1.png", (128, 32), 9),
    "academy_gate_arch_3x2": ("exec-6dd8ad63-00e4-44c2-9224-c66cb20d7e8f.png", (96, 64), 10),
    "academy_corner_wall_2x2": ("exec-a8d9d8fa-8d72-4fe9-b690-108474087444.png", (64, 64), 9),
    "academy_broken_wall_3x1": ("exec-2493847c-79c6-499f-bd85-177338594e3e.png", (96, 32), 9),
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def normalize(source: Path, target_size: tuple[int, int], colors: int) -> Image.Image:
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 144 else 0)
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError(f"no opaque content: {source}")
    image = image.crop(bbox)
    alpha = alpha.crop(bbox)
    margin = 2
    available = (target_size[0] - margin * 2, target_size[1] - margin * 2)
    scale = min(available[0] / image.width, available[1] / image.height)
    size = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
    image = image.resize(size, Image.Resampling.NEAREST)
    alpha = alpha.resize(size, Image.Resampling.NEAREST)
    rgb = image.convert("RGB").quantize(colors=colors, method=Image.Quantize.MEDIANCUT).convert("RGB")
    normalized = Image.new("RGBA", target_size)
    x = (target_size[0] - size[0]) // 2
    y = target_size[1] - margin - size[1]
    rgb.putalpha(alpha)
    normalized.alpha_composite(rgb, (x, y))
    return normalized


def main() -> None:
    source_dir = ROOT / "source" / "terrain_structures_v15"
    normalized_dir = ROOT / "normalized" / "terrain_structures_v15"
    qa_dir = ROOT / "QA" / "terrain_structures_v15"
    manifest_dir = ROOT / "manifests" / "terrain_structures_v15"
    for directory in (source_dir, normalized_dir, qa_dir, manifest_dir):
        directory.mkdir(parents=True, exist_ok=True)

    products: list[tuple[str, Image.Image]] = []
    for asset_id, (generated_name, target_size, colors) in ASSETS.items():
        generated = GENERATED / generated_name
        source = source_dir / f"{asset_id}_source.png"
        source.write_bytes(generated.read_bytes())
        product = normalize(source, target_size, colors)
        output = normalized_dir / f"{asset_id}.png"
        product.save(output, optimize=True)
        products.append((asset_id, product))
        preview = product.resize((target_size[0] * 4, target_size[1] * 4), Image.Resampling.NEAREST)
        preview.save(qa_dir / f"{asset_id}_4x.png", optimize=True)
        opaque = [pixel for pixel in product.getdata() if pixel[3] == 255]
        manifest = {
            "schema": "occ-art-manifest-v1",
            "asset_id": asset_id,
            "status": "FORMAL_CANDIDATE",
            "role": "multi_cell_academy_structure",
            "logical_cells": [target_size[0] // 32, target_size[1] // 32],
            "source": str(source.relative_to(ROOT)).replace("\\", "/"),
            "source_sha256": sha256(source),
            "normalized": str(output.relative_to(ROOT)).replace("\\", "/"),
            "normalized_sha256": sha256(output),
            "generation_channel": "codex_builtin_imagegen",
            "hard_alpha": sorted(set(product.getchannel("A").getdata())) <= [0, 255],
            "opaque_color_count": len(set(opaque)),
            "qa": ["1x", "4x", "grayscale", "checkerboard", "application_contact_required"],
            "review": "candidate pending Unity application contact and importer/runtime verification",
        }
        (manifest_dir / f"{asset_id}.occ-art-manifest-v1.json").write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    board = Image.new("RGBA", (512, 640), (42, 39, 35, 255))
    draw = ImageDraw.Draw(board)
    y = 16
    for asset_id, product in products:
        checker = Image.new("RGBA", product.size, (226, 220, 204, 255))
        check = ImageDraw.Draw(checker)
        for cy in range(0, product.height, 4):
            for cx in range(0, product.width, 4):
                if (cx // 4 + cy // 4) % 2:
                    check.rectangle((cx, cy, cx + 3, cy + 3), fill=(179, 174, 162, 255))
        checker.alpha_composite(product)
        scaled = checker.resize((product.width * 3, product.height * 3), Image.Resampling.NEAREST)
        board.alpha_composite(scaled, (16, y))
        draw.text((404, y + 8), asset_id.replace("academy_", ""), fill=(235, 230, 215, 255))
        y += scaled.height + 16
    board.save(qa_dir / "academy_structures_v15_contact.png", optimize=True)


if __name__ == "__main__":
    main()
