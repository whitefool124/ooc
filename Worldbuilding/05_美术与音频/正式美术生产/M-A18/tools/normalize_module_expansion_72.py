from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[5]
PACK = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18"
FAMILY = "academy_modules_v22_25"
CATALOG = json.loads((PACK / "tools/module_expansion_72_catalog.json").read_text(encoding="utf-8"))


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def normalize(source: Path, size: tuple[int, int], colors: int, border: int) -> Image.Image:
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A")
    bbox = alpha.point(lambda value: 255 if value >= 24 else 0).getbbox()
    if bbox is None:
        raise ValueError(f"empty alpha: {source}")
    image = image.crop(bbox)
    maximum = (max(1, size[0] - border * 2), max(1, size[1] - border * 2))
    image.thumbnail(maximum, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    hard_alpha = canvas.getchannel("A").point(lambda value: 255 if value >= 112 else 0)
    rgb = canvas.convert("RGB").quantize(colors=max(2, colors - 1), method=Image.Quantize.FASTOCTREE).convert("RGB")
    result = Image.merge("RGBA", (*rgb.split(), hard_alpha))
    pixels = result.load()
    for y in range(result.height):
        for x in range(result.width):
            if pixels[x, y][3] == 0:
                pixels[x, y] = (0, 0, 0, 0)
    return result


def checker(size: tuple[int, int], cell: int = 4) -> Image.Image:
    out = Image.new("RGBA", size, (0, 0, 0, 255))
    draw = ImageDraw.Draw(out)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            value = 72 if (x // cell + y // cell) % 2 else 132
            draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(value, value, value, 255))
    return out


def main() -> None:
    source_dir = PACK / "source" / FAMILY
    normalized_dir = PACK / "normalized" / FAMILY
    qa_dir = PACK / "QA" / FAMILY
    manifest_dir = PACK / "manifests" / FAMILY
    normalized_dir.mkdir(parents=True, exist_ok=True)
    qa_dir.mkdir(parents=True, exist_ok=True)
    cell_w, cell_h, columns = 160, 176, 8
    rows = (len(CATALOG) + columns - 1) // columns
    contact = Image.new("RGBA", (cell_w * columns, cell_h * rows), (18, 22, 27, 255))
    draw = ImageDraw.Draw(contact)
    for index, asset in enumerate(CATALOG):
        asset_id = asset["asset_id"]
        width, height = asset["cells"][0] * 32, asset["cells"][1] * 32
        colors = 10 if width == height == 32 else 12
        border = 0 if asset["role"] == "modular_structure_32" else 2
        source = source_dir / f"{asset_id}_source.png"
        output = normalized_dir / f"{asset_id}.png"
        image = normalize(source, (width, height), colors, border)
        image.save(output, optimize=True)
        one = qa_dir / f"{asset_id}_1x.png"
        four = qa_dir / f"{asset_id}_4x.png"
        gray_path = qa_dir / f"{asset_id}_grayscale.png"
        checker_path = qa_dir / f"{asset_id}_checker.png"
        image.save(one, optimize=True)
        image.resize((width * 4, height * 4), Image.Resampling.NEAREST).save(four, optimize=True)
        gray = image.convert("L")
        Image.merge("RGBA", (gray, gray, gray, image.getchannel("A"))).save(gray_path, optimize=True)
        board = checker(image.size)
        board.alpha_composite(image)
        board.save(checker_path, optimize=True)

        col, row = index % columns, index // columns
        scale = min(4, max(1, 128 // max(image.size)))
        preview = image.resize((width * scale, height * scale), Image.Resampling.NEAREST)
        x = col * cell_w + (cell_w - preview.width) // 2
        y = row * cell_h + 4 + (132 - preview.height) // 2
        contact.alpha_composite(preview, (x, y))
        draw.text((col * cell_w + 4, row * cell_h + 140), asset_id.replace("academy_", "")[:24], fill=(224, 226, 220, 255))
        draw.text((col * cell_w + 4, row * cell_h + 156), f"{width}x{height} / batch {asset['batch']}", fill=(144, 160, 170, 255))

        manifest_path = manifest_dir / f"{asset_id}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        manifest["provenance"]["source_sha256"] = digest(source)
        manifest["delivery"]["output_sha256"] = digest(output)
        manifest["evidence"] = {
            "one_x": one.relative_to(ROOT).as_posix(),
            "four_x": four.relative_to(ROOT).as_posix(),
            "grayscale": gray_path.relative_to(ROOT).as_posix(),
            "checker": checker_path.relative_to(ROOT).as_posix(),
            "application_contact": None,
        }
        manifest["status"] = "REVIEW_READY"
        manifest["human_review"]["notes"] = "Machine-ready target-size candidate; target-size and Unity application reviews pending."
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    contact.save(qa_dir / "academy_modules_72_target_contact.png", optimize=True)
    print(f"normalized={len(CATALOG)} contact={contact.size}")


if __name__ == "__main__":
    main()
