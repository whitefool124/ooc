from __future__ import annotations

import argparse
import json
import random
from pathlib import Path

from PIL import Image


def main() -> None:
    parser = argparse.ArgumentParser(description="Compare three four-variant 32px terrain families.")
    parser.add_argument("--root", required=True, type=Path)
    parser.add_argument("--qa-dir", required=True, type=Path)
    args = parser.parse_args()
    families = ["large_flags", "cloister_courses", "old_slate"]
    args.qa_dir.mkdir(parents=True, exist_ok=True)

    contact = Image.new("RGBA", (4 * 32, 3 * 32))
    comparison = {}
    for row, family in enumerate(families):
        images = []
        for column, variant in enumerate("abcd"):
            path = args.root / family / f"academy_courtyard_{variant}.png"
            image = Image.open(path).convert("RGBA")
            images.append(image)
            contact.paste(image, (column * 32, row * 32))

        rng = random.Random(20260820 + row)
        board = Image.new("RGBA", (12 * 32, 9 * 32))
        for y in range(9):
            for x in range(12):
                board.paste(images[rng.randrange(4)], (x * 32, y * 32))
        board.resize((768, 576), Image.Resampling.NEAREST).save(args.qa_dir / f"{family}_mixed_12x9_2x.png")
        comparison[family] = {"variants": 4, "board": f"{family}_mixed_12x9_2x.png"}

    contact.resize((512, 384), Image.Resampling.NEAREST).save(args.qa_dir / "three_families_abcd_contact_4x.png")
    (args.qa_dir / "comparison_manifest.json").write_text(json.dumps(comparison, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
