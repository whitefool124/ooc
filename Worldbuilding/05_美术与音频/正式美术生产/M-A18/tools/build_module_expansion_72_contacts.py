from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[5]
PAGE_DIR = ROOT / "UnityProject/Artifacts/ArtModules72/PageContacts"
QA_DIR = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18/QA/academy_modules_v22_25"


def main() -> None:
    pages = [Image.open(PAGE_DIR / f"module72_page_{index:02d}_1920x1080.png").convert("RGB") for index in range(1, 10)]
    canvas = Image.new("RGB", (1920, 1080), (18, 20, 24))
    draw = ImageDraw.Draw(canvas)
    for index, page in enumerate(pages):
        thumb = page.resize((620, 349), Image.Resampling.LANCZOS)
        x = 10 + (index % 3) * 635
        y = 10 + (index // 3) * 355
        canvas.paste(thumb, (x, y))
        draw.rectangle((x, y, x + 619, y + 348), outline=(210, 198, 168), width=2)
        draw.rectangle((x + 5, y + 5, x + 82, y + 28), fill=(20, 23, 28))
        draw.text((x + 11, y + 9), f"PAGE {index + 1:02d}", fill=(238, 229, 207))
    QA_DIR.mkdir(parents=True, exist_ok=True)
    canvas.save(QA_DIR / "academy_modules_72_unity_12x9_contact.png")


if __name__ == "__main__":
    main()
