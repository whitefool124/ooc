#!/usr/bin/env python3
"""Plan or scaffold a modular OCC ground-theme batch.

This tool creates planning metadata and empty manifest stubs only. It never
generates images, edits existing art, or imports anything into Unity.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
CONTRACT_PATH = ROOT / "Tools/OCCArt/occ_modular_ground_batch_contract_v1.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--theme-id", required=True, help="lowercase snake_case theme id")
    parser.add_argument("--material-family", required=True)
    parser.add_argument("--base-palette", nargs="+", required=True, help="3-6 hex colors")
    parser.add_argument("--wetness", type=int, default=1)
    parser.add_argument("--wear", type=int, default=1)
    parser.add_argument("--motif", default="none")
    parser.add_argument("--edge-profile", default="flush")
    parser.add_argument("--output", type=Path, help="optional scaffold directory")
    return parser.parse_args()


def validate(args: argparse.Namespace, contract: dict) -> None:
    if not re.fullmatch(r"[a-z0-9_]+", args.theme_id):
        raise SystemExit("theme-id must match [a-z0-9_]+")
    allowed = contract["theme_parameters"]
    if args.material_family not in allowed["material_family"]["enum"]:
        raise SystemExit(f"unsupported material-family: {args.material_family}")
    if not 3 <= len(args.base_palette) <= 6:
        raise SystemExit("base-palette requires 3-6 colors")
    for color in args.base_palette:
        if not re.fullmatch(r"#[0-9A-Fa-f]{6}", color):
            raise SystemExit(f"invalid hex color: {color}")
    for name, value in (("wetness", args.wetness), ("wear", args.wear)):
        if not 0 <= value <= 3:
            raise SystemExit(f"{name} must be 0..3")
    for name, value in (("motif", args.motif), ("edge-profile", args.edge_profile)):
        key = "edge_profile" if name == "edge-profile" else name
        if value not in allowed[key]["enum"]:
            raise SystemExit(f"unsupported {name}: {value}")


def build_plan(args: argparse.Namespace, contract: dict) -> dict:
    pieces = []
    variant_sets = {"square": ["a", "b", "c", "d"], "front_face": ["a", "b"]}
    for piece in ["square", "front_face", "north", "east", "south", "west", "nw", "ne", "sw", "se"]:
        variants = variant_sets.get(piece, ["base"])
        for variant in variants:
            pieces.append({
                "runtime_id": f"ground.{args.theme_id}.{piece}.{variant}",
                "native": f"ground_{args.theme_id}_{piece}_{variant}_32.png",
                "manifest": f"ground_{args.theme_id}_{piece}_{variant}.occ-art-manifest-v1.json",
                "status": "PLANNED",
            })
    return {
        "schema": "occ-modular-ground-batch-plan-v1",
        "contract": "Tools/OCCArt/occ_modular_ground_batch_contract_v1.json",
        "delivery": {
            "square_native_px": contract["logical_cell"]["ground_tile_native_px"],
            "front_face_native_px": contract["logical_cell"]["front_face_native_px"],
            "directional_edge_native_px": contract["directional_edge_canvas"]["native_px"],
            "unity_ppu": contract["logical_cell"]["unity_ppu"],
            "squares_per_gameplay_cell": contract["logical_cell"]["ground_tiles_per_gameplay_cell"],
            "canonical_display_scale": contract["logical_cell"]["canonical_display_scale"],
        },
        "theme": {
            "theme_id": args.theme_id,
            "material_family": args.material_family,
            "base_palette": args.base_palette,
            "wetness": args.wetness,
            "wear": args.wear,
            "motif": args.motif,
            "edge_profile": args.edge_profile,
        },
        "pieces": pieces,
        "approval_order": contract["rework_policy"]["approval_order"],
        "status": "PLANNED",
    }


def main() -> None:
    args = parse_args()
    contract = json.loads(CONTRACT_PATH.read_text(encoding="utf-8"))
    validate(args, contract)
    plan = build_plan(args, contract)
    if args.output:
        out = args.output.resolve()
        out.mkdir(parents=True, exist_ok=True)
        (out / "batch_plan.json").write_text(json.dumps(plan, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        (out / "README.md").write_text(
            "# Modular ground batch: " + args.theme_id + "\n\n"
            "This directory is a scaffold. Add independent Codex built-in image sources, decoded "
            "32 PPU native assets (32x32 self-contained squares with a visible 1 px cell border, "
            "32x8 front faces for depth, 32x40 directional board edges), evidence and manifests "
            "before Unity import. No material transitions in this version. Assets carry no scene "
            "lighting: only the fixed upper-left form shading. Use the same native asset only at "
            "integer runtime scales (canonical 6x at 1920x1080); do not create resolution companions. "
            "Verify every square with Tools/OCCArt/verify_ground_tile.py --paving before review.\n",
            encoding="utf-8",
        )
        print(f"scaffolded {len(plan['pieces'])} pieces at {out}")
    else:
        print(json.dumps(plan, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
