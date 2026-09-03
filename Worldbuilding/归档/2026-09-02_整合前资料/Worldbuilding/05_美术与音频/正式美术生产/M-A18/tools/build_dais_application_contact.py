from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def main() -> None:
    parser = argparse.ArgumentParser(description="Build a QA-only 12x9 contact for one multi-cell academy dais.")
    parser.add_argument("--base", required=True, type=Path)
    parser.add_argument("--dais", required=True, type=Path)
    parser.add_argument("--unity-art-root", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--floor-output", type=Path)
    args = parser.parse_args()

    base = Image.open(args.base).convert("RGBA")
    dais = Image.open(args.dais).convert("RGBA")
    if base.size != (32, 32) or dais.size != (192, 64):
        raise ValueError(f"unexpected sizes: base={base.size}, dais={dais.size}")
    board = Image.new("RGBA", (384, 288))
    for y in range(9):
        for x in range(12):
            board.alpha_composite(base, (x * 32, y * 32))
    if args.floor_output:
        args.floor_output.parent.mkdir(parents=True, exist_ok=True)
        board.save(args.floor_output, optimize=True)
    board.alpha_composite(dais, (3 * 32, 1 * 32))

    academy = args.unity_art_root / "FormalAcademyCombat32"
    units = args.unity_art_root / "FormalUnits64"
    overlays = args.unity_art_root / "FormalTacticalOverlays32"
    contacts = (
        (academy / "academy_light_planter_intact.png", 1 * 32, 5 * 32),
        (academy / "academy_light_stone_bench_intact.png", 9 * 32, 5 * 32),
        (academy / "academy_heavy_archive_stack_intact.png", 3 * 32, 3 * 32),
        (academy / "academy_heavy_archive_stack_intact.png", 8 * 32, 3 * 32),
        (overlays / "move_range.png", 5 * 32, 5 * 32),
        (units / "hero.png", 5 * 32 - 16, 6 * 32 - 32),
        (units / "shieldguard.png", 4 * 32 - 16, 2 * 32 - 32),
        (units / "pyromancer.png", 8 * 32 - 16, 4 * 32 - 32),
    )
    for path, x, y in contacts:
        if path.is_file():
            board.alpha_composite(Image.open(path).convert("RGBA"), (x, y))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    board.save(args.output, optimize=True)


if __name__ == "__main__":
    main()
