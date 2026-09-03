#!/usr/bin/env python3
"""Create OCC production manifests and raw folders before image generation."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--batch", required=True)
    parser.add_argument("--catalog", required=True)
    parser.add_argument("--contact", required=True)
    return parser.parse_args()


def main() -> None:
    args = arguments()
    batch_rel = Path("Worldbuilding/05_美术与音频/正式美术生产") / args.batch
    batch = ROOT / batch_rel
    catalog = batch / args.catalog
    manifests = batch / "manifests"
    assets = json.loads(catalog.read_text(encoding="utf-8"))["assets"]
    manifests.mkdir(parents=True, exist_ok=True)

    for asset in assets:
        stem = asset["stem"]
        raw_rel = batch_rel / "raw" / stem / "source.png"
        normalized_rel = batch_rel / "normalized" / f"{stem}.png"
        qa_rel = batch_rel / "QA" / stem
        native_rel = None
        if asset["role"] == "material_pickup_24_to_ui32":
            native_rel = batch_rel / "normalized" / f"{stem}_native24.png"

        manifest = {
            "schema": "occ-art-manifest-v1",
            "contract_version": 1,
            "asset_id": asset["asset_id"],
            "role": asset["role"],
            "status": "QA_PENDING",
            "provenance": {
                "source_channel": "codex_builtin_imagegen",
                "source_descriptor": f"Codex built-in image generation; independent {args.batch} production source; non-Unity",
                "source_path": raw_rel.as_posix(),
                "source_sha256": "PENDING_GENERATION"
            },
            "delivery": {
                "output_path": normalized_rel.as_posix(),
                "output_sha256": "PENDING_NORMALIZATION",
                "native_output_path": native_rel.as_posix() if native_rel else None,
                "logical_cells": asset.get("logical_cells"),
                "palette_max": asset["palette_max"],
                "required_color_families": asset.get("required_color_families", [])
            },
            "application": {
                "runtime_draw_rect": asset["application"],
                "default_integer_scale": 4,
                "minimum_integer_scale": 2
            },
            "evidence": {
                "one_x": (qa_rel / "1x.png").as_posix(),
                "four_x": (qa_rel / "4x.png").as_posix(),
                "grayscale": (qa_rel / "grayscale.png").as_posix(),
                "checker": (qa_rel / "checker.png").as_posix(),
                "application_contact": (batch_rel / "QA" / "contacts" / args.contact).as_posix()
            },
            "human_review": {
                "overall": "PENDING",
                "reviewer": "",
                "date": "",
                "silhouette": "PENDING",
                "material": "PENDING",
                "perspective": "PENDING",
                "style": "PENDING",
                "application": "PENDING",
                "notes": "Production source; product approved the M-A32 family direction, but this individual output still requires review and must not enter Unity."
            },
            "unity_import": None
        }
        path = manifests / f"{stem}.occ-art-manifest-v1.json"
        if path.exists():
            raise FileExistsError(f"refusing to overwrite existing manifest: {path}")
        path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        (batch / "raw" / stem).mkdir(parents=True, exist_ok=True)

    print(json.dumps({"batch": args.batch, "prepared": len(assets), "status": "QA_PENDING"}, ensure_ascii=False))


if __name__ == "__main__":
    main()
