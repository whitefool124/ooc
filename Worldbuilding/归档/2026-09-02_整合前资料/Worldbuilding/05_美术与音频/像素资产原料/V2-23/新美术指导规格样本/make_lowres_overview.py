from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent
NAMES = ["occ_fire_canister_v04", "occ_grid_disposer_v04", "occ_recon_beacon_v04"]
board = Image.new("RGB", (1152, 416), (24, 28, 35))
for i, name in enumerate(NAMES):
    asset = Image.open(ROOT / "Items32" / f"{name}.png").convert("RGBA")
    x0 = i * 384
    check = Image.new("RGB", (352, 352), (41, 47, 57))
    draw = ImageDraw.Draw(check)
    for y in range(0, 352, 44):
        for x in range(0, 352, 44):
            if (x // 44 + y // 44) % 2:
                draw.rectangle((x, y, x + 43, y + 43), fill=(53, 60, 73))
    large = asset.resize((352, 352), Image.Resampling.NEAREST)
    check.paste(large, (0, 0), large)
    board.paste(check, (x0 + 16, 0))
    label = ImageDraw.Draw(board)
    label.text((x0 + 16, 366), name, fill=(230, 234, 241))
    label.text((x0 + 16, 385), "16x16 logical -> 2x -> 32x32", fill=(154, 171, 190))
board.save(ROOT / "QA" / "items_lowres_v04_overview.png")
