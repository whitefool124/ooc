from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(r"C:/Users/FNHF/.codex/generated_images/01a03335-8dbb-7852-af5d-51ea934172b7")
ASSETS = {
    "academy_ground_macro_earth_3x3": (
        "exec-cbc8cf55-7f9e-4702-84ed-9d7337e72007.png",
        [(116, 84, 62), (132, 94, 69), (148, 106, 78), (163, 121, 89)],
    ),
    "academy_ground_macro_earth_b_3x3": (
        "exec-f1a44639-d865-400e-97b9-06a33227791e.png",
        [(116, 84, 62), (132, 94, 69), (148, 106, 78), (163, 121, 89)],
    ),
    "academy_ground_macro_ruin_3x3": (
        "exec-207f5bdf-ea81-4b0d-98af-eb3a16e0382f.png",
        [(112, 111, 92), (139, 136, 112), (161, 156, 128), (181, 174, 143), (198, 191, 159)],
    ),
    "academy_ground_macro_ruin_b_3x3": (
        "exec-5e139b82-ba4c-4816-89a6-c64ba7ebcd2c.png",
        [(112, 111, 92), (139, 136, 112), (161, 156, 128), (181, 174, 143), (198, 191, 159)],
    ),
}


def normalize(source: Path, palette: list[tuple[int, int, int]]) -> Image.Image:
    raw = Image.open(source).convert("RGB")
    side = min(raw.size)
    crop = ImageOps.fit(raw, (side, side), method=Image.Resampling.LANCZOS)
    pixel = crop.resize((96, 96), Image.Resampling.LANCZOS)
    reduced = pixel.quantize(colors=len(palette), method=Image.Quantize.MEDIANCUT).convert("RGB")
    source_colors = sorted(set(reduced.getdata()), key=lambda color: sum(color))
    mapping = {color: palette[min(index, len(palette) - 1)] for index, color in enumerate(source_colors)}
    normalized = Image.new("RGB", reduced.size)
    normalized.putdata([mapping[color] for color in reduced.getdata()])
    return normalized


def main() -> None:
    source_dir = ROOT / "source" / "terrain_ground_macros_v18"
    normalized_dir = ROOT / "normalized" / "terrain_ground_macros_v18"
    qa_dir = ROOT / "QA" / "terrain_ground_macros_v18"
    for directory in (source_dir, normalized_dir, qa_dir):
        directory.mkdir(parents=True, exist_ok=True)

    contact = Image.new("RGB", (768, 768), (41, 39, 35))
    draw = ImageDraw.Draw(contact)
    for index, (asset_id, (generated_name, palette)) in enumerate(ASSETS.items()):
        generated = GENERATED / generated_name
        source = source_dir / f"{asset_id}_source.png"
        source.write_bytes(generated.read_bytes())
        pixel = normalize(source, palette)
        output = normalized_dir / f"{asset_id}.png"
        pixel.save(output, optimize=True)
        pixel.save(qa_dir / f"{asset_id}_1x.png", optimize=True)
        preview = pixel.resize((384, 384), Image.Resampling.NEAREST)
        preview.save(qa_dir / f"{asset_id}_4x.png", optimize=True)
        pixel.convert("L").save(qa_dir / f"{asset_id}_grayscale.png", optimize=True)
        checker = Image.new("RGB", pixel.size, (194, 194, 194))
        checker.paste(pixel, (0, 0))
        checker.save(qa_dir / f"{asset_id}_checker.png", optimize=True)
        x = (index % 2) * 384
        y = (index // 2) * 384
        contact.paste(preview, (x, y))
        draw.rectangle((x + 6, y + 6, x + 372, y + 27), fill=(41, 39, 35))
        draw.text((x + 10, y + 10), asset_id, fill=(235, 229, 212))
    contact.save(qa_dir / "academy_ground_macros_v18_contact.png", optimize=True)


if __name__ == "__main__":
    main()
