from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]
PACK = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18"
FAMILY = "academy_modules_v22_25"


def rows(batch: str, names: list[tuple[str, str]], cells: tuple[int, int] = (1, 1), role: str = "single_cell_prop_32"):
    return [{"batch": batch, "asset_id": asset_id, "subject": subject, "cells": list(cells), "role": role}
            for asset_id, subject in names]


ASSETS = (
    rows("44", [
        ("academy_floor_drain_round", "low circular wrought-iron floor drain"),
        ("academy_floor_drain_slot", "low rectangular slotted stone-and-iron drain"),
        ("academy_floor_service_hatch_round", "flush round maintenance hatch with two recessed grips"),
        ("academy_floor_service_hatch_square", "flush square maintenance hatch with four corner bolts"),
        ("academy_floor_repair_stone", "self-contained inset replacement stone repair slab"),
        ("academy_floor_repair_iron", "self-contained flush iron repair plate"),
        ("academy_floor_cable_cap", "flush capped aether cable access plate"),
        ("academy_floor_pipe_socket", "recessed capped pipe socket in a stone collar"),
        ("academy_floor_anchor_plate", "flat forged anchor plate with a central ring folded down"),
        ("academy_floor_inspection_window", "flush dark inspection window protected by two iron bars"),
        ("academy_floor_mortar_inlay", "self-contained pale mortar inlay with a simple converging geometry"),
        ("academy_floor_threshold_studs", "four low flush threshold studs on one stone insert"),
        ("academy_floor_safety_marker", "small flush service marker with restrained safety-yellow corner wedges"),
        ("academy_floor_herb_drain", "low stone drain with a few contained moss pixels inside its rim"),
        ("academy_floor_rain_channel", "short self-contained rain channel segment ending inside the tile"),
        ("academy_floor_conduit_blank", "flush blank conduit cover with a single cold-cyan inactive glass bead"),
    ]),
    rows("45", [
        ("academy_prop_wicker_basket", "low closed wicker supply basket"),
        ("academy_prop_book_crate", "low wooden crate tightly packed with plain books"),
        ("academy_prop_scroll_case", "low strapped wooden scroll case"),
        ("academy_prop_tool_satchel", "low square leather maintenance satchel"),
        ("academy_prop_folding_stool", "compact folded wooden field stool"),
        ("academy_prop_clay_jar", "stout lidded academy clay reagent jar"),
        ("academy_prop_coal_scuttle", "low blackened iron coal scuttle"),
        ("academy_prop_rope_coil", "thick neatly coiled hemp rope with one iron hook"),
        ("academy_prop_specimen_cage", "small empty wooden-and-iron specimen carrier cage"),
        ("academy_prop_practice_shields", "low rack holding two plain wooden practice shields"),
        ("academy_prop_oak_chest", "stout closed oak equipment chest with iron bands"),
        ("academy_prop_iron_locker", "short heavy academy iron locker cabinet"),
        ("academy_prop_stone_planter", "heavy square stone herb planter with restrained foliage"),
        ("academy_prop_reagent_cabinet", "heavy single-cell reagent cabinet with opaque bottle silhouettes"),
        ("academy_prop_field_lectern", "sturdy low academy field lectern with a closed ledger"),
        ("academy_prop_gear_cabinet", "heavy maintenance gear cabinet with one visible iron wheel"),
        ("academy_prop_warding_post", "thick stone-and-copper warding post with a tiny inactive crystal"),
        ("academy_prop_fire_bucket_stand", "sturdy iron stand holding two closed fire buckets"),
        ("academy_prop_medical_chest", "heavy pale-wood medical supply chest with grey-green cloth strap"),
        ("academy_prop_sealed_trunk", "heavy sealed specimen trunk with waxed iron clasps"),
    ]),
    rows("46", [
        ("academy_teaching_desk_2x1", "wide two-cell academy teaching demonstration desk"),
        ("academy_alchemy_bench_2x1", "wide two-cell alchemy preparation bench with secured glassware"),
        ("academy_map_table_2x1", "wide two-cell field map table with a blank rolled plan"),
        ("academy_repair_bench_2x1", "wide two-cell maintenance bench with a vice and hand tools"),
        ("academy_low_bookcase_2x1", "wide low two-cell bookcase with closed cabinet doors"),
        ("academy_specimen_counter_2x1", "wide two-cell specimen counter with lidded trays"),
        ("academy_medical_cot_2x1", "two-cell academy medical cot with folded grey-green blanket"),
        ("academy_supply_rack_2x1", "wide two-cell timber supply rack with strapped crates"),
        ("academy_smithing_table_2x1", "wide two-cell smithing table with a small cold anvil"),
        ("academy_tool_cabinet_2x1", "wide two-cell iron-and-wood tool cabinet"),
        ("academy_timber_barricade_2x1", "wide low two-cell training barricade of joined timber beams"),
        ("academy_stone_balustrade_2x1", "wide two-cell low academy stone balustrade"),
    ], (2, 1), "multi_cell_prop_32")
    + rows("46", [
        ("academy_aether_pump_2x2", "four-cell maintainable stone-and-copper aether circulation pump"),
        ("academy_archive_sorter_2x2", "four-cell manual academy archive sorting machine"),
        ("academy_infirmary_station_2x2", "four-cell academy infirmary treatment station"),
        ("academy_sealing_apparatus_2x2", "four-cell heavy sealing apparatus with a restrained cold-cyan core"),
    ], (2, 2), "multi_cell_prop_32"),
    rows("47", [(f"academy_wall_corner_{d}", f"independent academy wall corner facing {d.upper()}") for d in ("nw", "ne", "se", "sw")], role="modular_structure_32")
    + rows("47", [(f"academy_wall_gate_{d}", f"independent single-cell academy wall gate opening facing {d.upper()}") for d in ("n", "e", "s", "w")], role="modular_structure_32")
    + rows("47", [(f"academy_stair_landing_{d}", f"independent single-cell academy stair landing facing {d.upper()}") for d in ("n", "e", "s", "w")], role="modular_structure_32")
    + rows("47", [(f"academy_wall_buttress_{d}", f"independent academy wall buttress facing {d.upper()}") for d in ("n", "e", "s", "w")], role="modular_structure_32")
    + rows("47", [(f"academy_pipe_terminal_{d}", f"independent capped wall-pipe terminal facing {d.upper()}") for d in ("n", "e", "s", "w")], role="modular_structure_32"),
)
ASSETS = [asset for group in ASSETS for asset in group]


def prompt(asset: dict) -> str:
    w, h = asset["cells"]
    size = f"{w * 32}x{h * 32}"
    ground = asset["asset_id"].startswith("academy_floor_")
    view = "strict orthographic top-down, completely flush and low" if ground else "orthographic tactical three-quarter top-down, visible top face, compressed front face, no vanishing point"
    return (
        "Use case: stylized-concept\n"
        f"Asset type: independent OCC tactical-map production source for {asset['asset_id']}\n"
        f"Primary request: one {asset['subject']}; native logical {size} pixel-grid composition; one object only\n"
        "Scene/backdrop: genuinely transparent background\n"
        f"Composition/framing: {view}; centred; at least two logical pixels of transparent safety margin; fixed light from upper-left\n"
        "Style/medium: deliberately coarse hand-clustered pixel art, hard stair-step near-black outline, 5 to 10 discrete flat colours, no anti-aliasing\n"
        "Materials/textures: humble academy stone, worn timber, forged iron, old copper, ceramic or coarse cloth as appropriate; one readable material cue only\n"
        "Color palette: charcoal and warm stone neutrals; cold cyan only for active aether; restrained grey-green for medical; safety yellow only for service warning\n"
        "Constraints: readable at target size; all edges self-contained; no rotation-derived lighting; no dependence on neighbouring tiles\n"
        "Avoid: text, numbers, glyph labels, watermark, sprite sheet, board, multiple objects, grid overlay, UI frame, gradient, bloom, soft shadow, photorealism, medieval ornament, steampunk clutter, sci-fi console, LED array"
    )


def main() -> None:
    manifests = PACK / "manifests" / FAMILY
    sources = PACK / "source" / FAMILY
    manifests.mkdir(parents=True, exist_ok=True)
    sources.mkdir(parents=True, exist_ok=True)
    catalog = []
    for asset in ASSETS:
        asset = dict(asset)
        asset["prompt"] = prompt(asset)
        catalog.append(asset)
        w, h = asset["cells"]
        manifest = {
            "schema": "occ-art-manifest-v1", "contract_version": 1,
            "asset_id": asset["asset_id"], "role": asset["role"], "status": "QA_PENDING",
            "provenance": {"source_channel": "codex_builtin_imagegen", "source_descriptor": f"Independent single-asset generation for batch {asset['batch']}; no board slicing", "source_path": f"Worldbuilding/05_美术与音频/正式美术生产/M-A18/source/{FAMILY}/{asset['asset_id']}_source.png", "source_sha256": None},
            "delivery": {"output_path": f"Worldbuilding/05_美术与音频/正式美术生产/M-A18/normalized/{FAMILY}/{asset['asset_id']}.png", "output_sha256": None, "native_output_path": None, "logical_cells": [w, h], "palette_max": 10 if w == h == 1 else 12, "required_color_families": []},
            "application": {"runtime_draw_rect": "visual-only reusable academy map module; never changes logical cell state", "default_integer_scale": 4, "minimum_integer_scale": 2},
            "evidence": {"one_x": None, "four_x": None, "grayscale": None, "checker": None, "application_contact": None},
            "human_review": {"overall": "PENDING", "reviewer": None, "date": None, "silhouette": "PENDING", "material": "PENDING", "perspective": "PENDING", "style": "PENDING", "application": "PENDING", "notes": "Manifest registered before image generation."},
            "unity_import": None,
        }
        (manifests / f"{asset['asset_id']}.occ-art-manifest-v1.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (PACK / "tools/module_expansion_72_catalog.json").write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"registered={len(catalog)}")


if __name__ == "__main__":
    main()
