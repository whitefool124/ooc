"""Draw six independently authored native-pixel frames for the M-A18 fire_cast P0 VFX."""
import json
from pathlib import Path
from PIL import Image

OUT = Path(__file__).resolve().parents[1] / "normalized" / "vfx" / "fire_cast"
QA = Path(__file__).resolve().parents[1] / "QA" / "vfx" / "fire_cast"
PALETTE = {
    "ember": (91, 30, 25, 255),
    "red": (163, 49, 27, 255),
    "orange": (223, 91, 28, 255),
    "gold": (248, 164, 46, 255),
    "core": (255, 238, 174, 255),
}


def px(im, cells, color):
    for x, y in cells:
        if 0 <= x < 32 and 0 <= y < 32:
            im.putpixel((x, y), PALETTE[color])


def rect(im, x0, y0, x1, y1, color):
    px(im, [(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)], color)


def frame(index):
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    # Inward ember hooks: the player reads a controlled cast, never an impact.
    hooks = [
        ([(8, 17), (9, 16), (10, 16), (11, 15), (12, 15)], [(23, 14), (22, 15), (21, 15), (20, 16), (19, 16)]),
        ([(9, 18), (10, 17), (11, 17), (12, 16)], [(22, 13), (21, 14), (20, 14), (19, 15)]),
        ([(10, 19), (11, 18), (12, 17)], [(21, 12), (20, 13), (19, 14)]),
        ([(11, 19), (12, 18), (13, 17)], [(20, 13), (19, 14), (18, 15)]),
        ([(12, 18), (13, 17)], [(19, 14), (18, 15)]),
        ([(13, 17)], [(18, 15)]),
    ][index]
    px(im, hooks[0] + hooks[1], "ember")
    if index >= 1:
        px(im, hooks[0][-2:] + hooks[1][-2:], "red")
    size = [1, 2, 3, 4, 3, 2][index]
    cx, cy = 16, 16
    # A diamond core plus a raised right tongue avoids reading as a UI square.
    outer = [(x, y) for x in range(cx - size, cx + size + 1) for y in range(cy - size, cy + size + 1)
             if abs(x - cx) + abs(y - cy) <= size + 1]
    inner_size = max(0, size - 1)
    inner = [(x, y) for x in range(cx - inner_size, cx + inner_size + 1) for y in range(cy - inner_size, cy + inner_size + 1)
             if abs(x - cx) + abs(y - cy) <= inner_size + 1]
    px(im, outer, "orange")
    px(im, inner, "gold")
    rect(im, 15, 15, 16, 16, "core")
    if index in (2, 3, 4):
        px(im, [(14, 13), (17, 13), (13, 16), (18, 16), (14, 19), (17, 19)], "gold")
    if index == 3:
        px(im, [(12, 16), (13, 15), (18, 15), (19, 16), (15, 12), (16, 12), (17, 11), (17, 10)], "orange")
    return im


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    QA.mkdir(parents=True, exist_ok=True)
    frames = []
    contact = Image.new("RGBA", (32 * 4 * 6, 32 * 4), (25, 25, 25, 255))
    for i in range(6):
        image = frame(i)
        image.save(OUT / f"frame_{i:02d}.png")
        preview = image.resize((128, 128), Image.Resampling.NEAREST)
        preview.save(QA / f"frame_{i:02d}_4x.png")
        contact.alpha_composite(preview, (i * 128, 0))
        alpha = set(image.getchannel("A").getdata())
        colors = {rgb[:3] for rgb in image.getdata() if rgb[3] > 0}
        frames.append({"frame": i, "size": list(image.size), "hardAlpha": alpha.issubset({0, 255}), "colors": len(colors)})
    status = "PASS" if all(f["size"] == [32, 32] and f["hardAlpha"] and f["colors"] <= 12 for f in frames) else "WARN"
    contact.save(QA / "fire_cast_contact_4x.png")
    (QA / "fire_cast_report.json").write_text(json.dumps({"status": status, "frames": frames, "note": "independently drawn native-pixel P0 fire_cast frames"}, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
