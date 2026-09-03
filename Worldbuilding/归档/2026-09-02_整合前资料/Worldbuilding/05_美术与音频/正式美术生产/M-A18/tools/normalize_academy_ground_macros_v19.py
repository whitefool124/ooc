from __future__ import annotations

from pathlib import Path
import shutil

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[1]
REPO = Path(__file__).resolve().parents[5]
ASSETS = {
    "academy_ground_macro_earth_3x3": [(128, 94, 72), (138, 101, 77), (149, 110, 84)],
    "academy_ground_macro_earth_b_3x3": [(126, 93, 72), (137, 100, 77), (147, 108, 83)],
    "academy_ground_macro_ruin_3x3": [(126, 124, 103), (146, 142, 117), (164, 159, 131), (181, 175, 145)],
    "academy_ground_macro_ruin_b_3x3": [(123, 122, 102), (143, 140, 116), (160, 156, 130), (177, 172, 144)],
}


def normalize(source: Path, palette: list[tuple[int, int, int]], coarse_ground: bool) -> Image.Image:
    raw = Image.open(source).convert("RGB")
    side = min(raw.size)
    crop = ImageOps.fit(raw, (side, side), method=Image.Resampling.LANCZOS)
    # Earth benefits from a 16px-per-cell material field. Ruin keeps the native
    # 32px-per-cell outline budget so flagstones and cobbles do not become camouflage.
    logical_size = 48 if coarse_ground else 96
    logical = crop.resize((logical_size, logical_size), Image.Resampling.LANCZOS)
    reduced = logical.quantize(colors=len(palette), method=Image.Quantize.MEDIANCUT).convert("RGB")
    colors = sorted(set(reduced.getdata()), key=lambda color: sum(color))
    mapping = {color: palette[min(index, len(palette) - 1)] for index, color in enumerate(colors)}
    recolored = Image.new("RGB", reduced.size)
    recolored.putdata([mapping[color] for color in reduced.getdata()])
    return recolored.resize((96, 96), Image.Resampling.NEAREST)


def main() -> None:
    old_source = ROOT / "source" / "terrain_ground_macros_v18"
    source_dir = ROOT / "source" / "terrain_ground_macros_v19"
    normalized_dir = ROOT / "normalized" / "terrain_ground_macros_v19"
    qa_dir = ROOT / "QA" / "terrain_ground_macros_v19"
    unity_dir = REPO / "UnityProject" / "Assets" / "Game" / "Resources" / "Art" / "FormalAcademyGroundMacros32"
    for directory in (source_dir, normalized_dir, qa_dir, unity_dir):
        directory.mkdir(parents=True, exist_ok=True)

    contact = Image.new("RGB", (768, 812), (41, 39, 35))
    draw = ImageDraw.Draw(contact)
    for index, (asset_id, palette) in enumerate(ASSETS.items()):
        source = source_dir / f"{asset_id}_source.png"
        shutil.copyfile(old_source / f"{asset_id}_source.png", source)
        pixel = normalize(source, palette, asset_id.startswith("academy_ground_macro_earth"))
        output = normalized_dir / f"{asset_id}.png"
        pixel.save(output, optimize=True)
        shutil.copyfile(output, unity_dir / output.name)
        pixel.save(qa_dir / f"{asset_id}_1x.png", optimize=True)
        pixel.resize((384, 384), Image.Resampling.NEAREST).save(
            qa_dir / f"{asset_id}_4x.png", optimize=True)
        pixel.convert("L").save(qa_dir / f"{asset_id}_grayscale.png", optimize=True)
        checker = Image.new("RGB", pixel.size, (194, 194, 194))
        checker.paste(pixel, (0, 0))
        checker.save(qa_dir / f"{asset_id}_checker.png", optimize=True)
        x = (index % 2) * 384
        y = (index // 2) * 406
        contact.paste(pixel.resize((384, 384), Image.Resampling.NEAREST), (x, y + 22))
        draw.text((x + 8, y + 5), asset_id, fill=(235, 229, 212))
    contact.save(qa_dir / "academy_ground_macros_v19_contact.png", optimize=True)


if __name__ == "__main__":
    main()
