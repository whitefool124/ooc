"""Native-grid connected 32x64 low-wall family from the approved isolated master."""

from __future__ import annotations

import hashlib
import json
from collections import defaultdict
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps

from author_isolated_master import (
    ROOT, PROJECT, FLOOR, HEAVY, LIGHT, BLACK, IVORY, LIMESTONE,
    ASH, SHADE, JOINT, make_wall, rect,
)

OUT = ROOT / "connected_family"
QA = ROOT / "connected_family_qa"
MANIFESTS = ROOT / "connected_family_manifests"
MAPS = PROJECT / "Worldbuilding/地图配置/战斗地图"
REFERENCE = ROOT / "isolated_masonry_material_reference_source.png"
MASKS = ("island", "N", "E", "S", "W", "NE", "NS", "NW", "ES", "EW", "SW",
         "NES", "NEW", "NSW", "ESW", "NESW")
VARIANTS = ("A", "B")
MAP_IDS = (
    "rain_lantern_court", "first_battle_greenhouse_collection_room",
    "first_b3_rain_prism_court", "first_elite_three_material_pressure",
)


def wall_tile(mask: str, variant: str) -> Image.Image:
    if mask not in MASKS or variant not in VARIANTS:
        raise ValueError((mask, variant))
    n, e, s, w = (direction in mask for direction in "NESW")
    if mask == "island":
        n = e = s = w = False
    image = make_wall()
    d = ImageDraw.Draw(image)

    # Connected sides extend the cap and mass to the shared edge. Exposed
    # corners retain the approved stepped silhouette.
    if not n:
        if w:
            rect(d, (0, 22, 6, 26), LIMESTONE)
        if e:
            rect(d, (25, 22, 31, 26), LIMESTONE)
    else:
        # A north neighbour continues the masonry over the visual overhang.
        # No repeated bright cap is drawn in a vertical run.
        rect(d, (0, 22, 31, 38), ASH)
        rect(d, (2, 23, 12, 25), LIMESTONE)
        rect(d, (18, 23, 28, 24), LIMESTONE)
        rect(d, (13 if variant == "A" else 10, 27,
                 15 if variant == "A" else 12, 37), SHADE)
        rect(d, (1, 37, 30, 38), SHADE)

    if variant == "B":
        # The structural silhouette, contact and edge pixels stay identical.
        # Only the interior ashlar placement changes with map parity.
        rect(d, (3, 43, 9, 47), ASH)
        rect(d, (5, 43, 9, 44), LIMESTONE)
        rect(d, (10, 45, 12, 49), SHADE)
        rect(d, (20, 42, 24, 45), LIMESTONE)
        rect(d, (16, 46, 20, 49), SHADE)
        rect(d, (4, 53, 7, 55), LIMESTONE)
        rect(d, (13, 55, 18, 58), SHADE)
        rect(d, (20, 53, 24, 54), LIMESTONE)

    if s:
        # The foot appears only at an exposed southern end.
        rect(d, (0, 60, 31, 63), ASH)
        rect(d, (3, 60, 9, 61), LIMESTONE)
        rect(d, (13, 60, 20, 61), LIMESTONE)
        rect(d, (26, 60, 29, 61), LIMESTONE)

    # An exposed lateral edge has a single black silhouette pixel. On a
    # connection the edge uses one common stone contact colour, so A/B and
    # unlike masks cannot leave gaps or hard internal outlines.
    if not w:
        for y in range(27 if not n else 22, 64):
            image.putpixel((0, y), BLACK)
    if not e:
        for y in range(27 if not n else 22, 64):
            image.putpixel((31, y), BLACK)
    if not n:
        for x in range(32):
            if image.getpixel((x, 22))[3]:
                image.putpixel((x, 22), BLACK)
    if not s:
        for x in range(32):
            image.putpixel((x, 63), BLACK)

    if w:
        for y in range(22, 64):
            image.putpixel((0, y), LIMESTONE if 23 <= y <= 34 else ASH)
    if e:
        for y in range(22, 64):
            image.putpixel((31, y), LIMESTONE if 23 <= y <= 34 else ASH)
    if n:
        for x in range(32):
            image.putpixel((x, 22), ASH)
    if s:
        for x in range(32):
            image.putpixel((x, 63), ASH)
    return image


def mask_at(walls: set[tuple[int, int]], x: int, y: int) -> str:
    mask = "".join(direction for direction, dx, dy in
                   (("N", 0, -1), ("E", 1, 0), ("S", 0, 1), ("W", -1, 0))
                   if (x + dx, y + dy) in walls)
    return mask or "island"


def variant_at(x: int, y: int) -> str:
    return VARIANTS[(x + y) % 2]


def file_hash(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def map_contacts(tiles: dict[tuple[str, str], Image.Image]):
    floor = Image.open(FLOOR).convert("RGBA")
    uses = defaultdict(list)
    seam_pairs = seam_errors = 0
    for name in MAP_IDS:
        data = json.loads((MAPS / f"{name}.occ-map.json").read_text(encoding="utf-8"))
        walls = {(p["x"], p["y"]) for p in data["terrain"] if p["kind"] == "PermanentWall"}
        canvas = Image.new("RGBA", (data["width"] * 32, (data["height"] + 2) * 32))
        for y in range(data["height"] + 2):
            for x in range(data["width"]):
                canvas.alpha_composite(floor, (x * 32, y * 32))
        for x, y in sorted(walls, key=lambda p: (p[1], p[0])):
            mask, variant = mask_at(walls, x, y), variant_at(x, y)
            canvas.alpha_composite(tiles[(mask, variant)], (x * 32, (y + 1) * 32 - 32))
            uses[(mask, variant)].append(f"{name}:{chr(65 + x)}{y + 1}")
            for dx, dy in ((1, 0), (0, 1)):
                if (x + dx, y + dy) not in walls:
                    continue
                neighbor = tiles[(mask_at(walls, x + dx, y + dy), variant_at(x + dx, y + dy))]
                if dx:
                    pairs = ((tiles[(mask, variant)].getpixel((31, i)), neighbor.getpixel((0, i)))
                             for i in range(22, 64))
                else:
                    pairs = ((tiles[(mask, variant)].getpixel((i, 63)), neighbor.getpixel((i, 22)))
                             for i in range(32))
                seam_pairs += 1
                seam_errors += sum(a != b for a, b in pairs)
        canvas.save(QA / f"{name}_wall_contact_1x.png")
        canvas.resize((canvas.width * 6, canvas.height * 6), Image.Resampling.NEAREST).save(
            QA / f"{name}_wall_contact_6x.png")
    if seam_errors:
        raise AssertionError(f"{seam_errors} mismatched edge pixels across {seam_pairs} joins")
    (QA / "map_mask_inventory.json").write_text(
        json.dumps({"uses": {f"{m}_{v}": u for (m, v), u in uses.items()},
                    "connected_pairs": seam_pairs, "mismatched_edge_pixels": seam_errors},
                   ensure_ascii=False, indent=2), encoding="utf-8")
    return uses


def write_manifest(mask: str, variant: str, path: Path, uses: list[str]):
    root = ROOT.relative_to(PROJECT).as_posix()
    q = f"{root}/connected_family_qa/{mask.lower()}_{variant.lower()}"
    output = path.relative_to(PROJECT).as_posix()
    reference = REFERENCE.relative_to(PROJECT).as_posix()
    source = Path(__file__).relative_to(PROJECT).as_posix()
    manifest = {
        "schema": "occ-art-manifest-v1", "contract_version": 1,
        "asset_id": f"occ_academy_permanent_wall_low42_{mask.lower()}_{variant.lower()}_v1",
        "role": "permanent_wall_32x64", "status": "REVIEW_READY",
        "adjacency_mask": mask, "texture_variant": variant,
        "provenance": {
            "source_channel": "manual_pixel_authoring",
            "source_descriptor": "Approved isolated 42px mother and independent built-in warm-stone reference; connected masks authored on native grid under permanent-wall exception, no generated-source crop or resize.",
            "source_path": source, "source_sha256": file_hash(Path(__file__)),
            "generated_reference_path": reference, "generated_reference_sha256": file_hash(REFERENCE),
        },
        "delivery": {"output_path": output, "output_sha256": file_hash(path),
                     "native_output_path": output, "native_ppu": 32,
                     "logical_cells": [1, 1], "palette_max": 6,
                     "required_color_families": []},
        "placement_semantics": {
            "application_scope": "runtime_layout", "mount_type": "structural", "logical_cell": [1, 1],
            "native_anchor": [16, 63], "visible_contact_line": {"axis": "y", "native_px": 63},
            "depth_sort_baseline": 63, "allowed_occlusion_direction": ["front"],
            "screen_axis": "horizontal_vertical_locked", "support_asset": "ground_cell",
            "combined_room_shell": False,
        },
        "display": {"canonical_integer_scale": 6, "canonical_at": [1920, 1080],
                    "screen_px_per_native_px_at_canonical": 6,
                    "integer_only": True, "fractional_scale_forbidden": True,
                    "uniform_pixel_size_with_siblings": True},
        "application": {"runtime_draw_rect": "32x64 transparent visual canvas, 42px visible wall, bottom centred on one 32x32 blocking cell",
                        "default_integer_scale": 6, "minimum_integer_scale": 1,
                        "texture_variant_selection": "A when (x+y)%2==0 else B",
                        "actual_first_stage_uses": uses},
        "evidence": {"one_x": output, "four_x": q + "_4x.png", "six_x": q + "_6x.png",
                     "grayscale": q + "_grayscale.png", "checker": q + "_checker.png",
                     "application_contact_1x": f"{root}/connected_family_qa/first_b3_rain_prism_court_wall_contact_1x.png",
                     "application_contact": f"{root}/connected_family_qa/first_b3_rain_prism_court_wall_contact_6x.png"},
        "human_review": {"overall": "PENDING", "notes": "Isolated master approved by user; connected family aesthetics and runtime contact still pending."},
        "unity_import": {"filter_mode": "Point", "mesh_type": "Full Rect", "compression": "None",
                         "mipmaps": False, "wrap_mode": "Clamp", "stable_guid": "",
                         "importer_verified": False, "runtime_verified": False},
    }
    (MANIFESTS / f"{mask.lower()}_{variant.lower()}.occ-art-manifest-v1.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")


def main():
    for d in (OUT, QA, MANIFESTS):
        d.mkdir(parents=True, exist_ok=True)
    tiles = {}
    for mask in MASKS:
        for variant in VARIANTS:
            image = wall_tile(mask, variant)
            assert image.size == (32, 64)
            assert image.getchannel("A").getbbox() == (0, 22, 32, 64)
            assert len({p for p in image.getdata() if p[3]}) <= 6
            tiles[(mask, variant)] = image
            stem = f"{mask.lower()}_{variant.lower()}"
            image.save(OUT / f"academy_permanent_wall_{stem}_32x64.png")
            image.resize((128, 256), Image.Resampling.NEAREST).save(QA / f"{stem}_4x.png")
            image.resize((192, 384), Image.Resampling.NEAREST).save(QA / f"{stem}_6x.png")
            ImageOps.grayscale(image).save(QA / f"{stem}_grayscale.png")
            check = Image.new("RGBA", (32, 64))
            cd = ImageDraw.Draw(check)
            for y in range(0, 64, 8):
                for x in range(0, 32, 8):
                    rect(cd, (x, y, x + 7, y + 7),
                         (205, 205, 205, 255) if (x // 8 + y // 8) % 2 else (245, 245, 245, 255))
            check.alpha_composite(image)
            check.save(QA / f"{stem}_checker.png")
    uses = map_contacts(tiles)
    for (mask, variant), image in tiles.items():
        path = OUT / f"academy_permanent_wall_{mask.lower()}_{variant.lower()}_32x64.png"
        write_manifest(mask, variant, path, uses.get((mask, variant), []))


if __name__ == "__main__":
    main()
