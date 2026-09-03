from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "UnityProject/Assets/Game/Resources/Art"
OUT = ROOT / "UnityProject/Artifacts/IconReview48"


def checker(size: tuple[int, int], block: int = 8) -> Image.Image:
    result = Image.new("RGB", size)
    pixels = result.load()
    for y in range(size[1]):
        for x in range(size[0]):
            shade = 50 if (x // block + y // block) % 2 == 0 else 72
            pixels[x, y] = (shade, shade, shade)
    return result


def sheet(title: str, groups: list[tuple[str, list[Path]]], output: Path, columns: int = 8) -> None:
    cell_w, cell_h = 176, 180
    header_h, group_h = 42, 30
    rows = sum((len(paths) + columns - 1) // columns for _, paths in groups)
    height = header_h + rows * cell_h + len(groups) * group_h
    canvas = Image.new("RGB", (columns * cell_w, height), (17, 20, 24))
    draw = ImageDraw.Draw(canvas)
    draw.text((14, 13), title, fill=(238, 230, 207))
    y = header_h
    for group_name, paths in groups:
        draw.rectangle((0, y, canvas.width, y + group_h - 1), fill=(34, 39, 44))
        draw.text((14, y + 8), f"{group_name} ({len(paths)})", fill=(113, 210, 216))
        y += group_h
        for index, path in enumerate(paths):
            col = index % columns
            row = index // columns
            x = col * cell_w
            top = y + row * cell_h
            draw.rectangle((x + 3, top + 3, x + cell_w - 4, top + cell_h - 4), outline=(84, 91, 94), width=1)
            icon = Image.open(path).convert("RGBA")
            enlarged = icon.resize((128, 128), Image.Resampling.NEAREST)
            bg = checker((128, 128))
            bg.paste(enlarged, (0, 0), enlarged)
            canvas.paste(bg, (x + 24, top + 10))
            one_x = checker((32, 32), 4)
            one_x.paste(icon, (0, 0), icon)
            canvas.paste(one_x, (x + 8, top + 142))
            label = path.stem
            if len(label) > 25:
                label = label[:24] + "…"
            draw.text((x + 46, top + 151), label, fill=(224, 226, 220))
        y += ((len(paths) + columns - 1) // columns) * cell_h
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output, optimize=True)


def pngs(relative: str) -> list[Path]:
    return sorted((ART / relative).glob("*.png"))


def main() -> None:
    sheet(
        "OCC CURRENT SPELL ICONS — 4x preview + 1x native",
        [
            ("Fire spell catalog", pngs("FormalSkillIcons32/Fire")),
            ("Runtime combat skills", pngs("FormalSkillIcons32/Runtime")),
        ],
        OUT / "current_spell_icons_contact.png",
    )
    sheet(
        "OCC CURRENT ITEM ICONS — 4x preview + 1x native",
        [
            ("Inventory items and UI semantics", pngs("FormalItemIcons32")),
            ("Artifacts", pngs("FormalArtifactIcons32")),
            ("Equipment slots", pngs("FormalEquipmentSlotIcons32")),
        ],
        OUT / "current_item_icons_contact.png",
    )


if __name__ == "__main__":
    main()
