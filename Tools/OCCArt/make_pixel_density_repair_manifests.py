#!/usr/bin/env python3
"""Write traceable QA_PENDING manifests for the pixel-density repair sample."""
from hashlib import sha256
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ARCHIVE = ROOT / "Worldbuilding/归档/2026-09-13_像素密度统一修复"
ART = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround"


def digest(path: Path) -> str:
    return sha256(path.read_bytes()).hexdigest().upper()


def write_surface(theme: str) -> None:
    master = ART / f"academy_test_ground_theme_{theme}_surface_64.png"
    low = ART / f"academy_test_ground_theme_{theme}_surface_32.png"
    payload = {
        "schema": "occ-art-manifest-v1",
        "contract_version": 1,
        "asset_id": f"combat_test_arena_theme_{theme}_surface_pixel_density_v2",
        "role": "battlefield_ground_macro_64",
        "status": "QA_PENDING",
        "provenance": {
            "source_channel": "easycli_chatgpt_pro_gpt_image_2_5",
            "source_descriptor": f"像素密度统一修复：{theme}主题安静连续地面独立原料",
            "source_path": f"Worldbuilding/归档/2026-09-13_像素密度统一修复/source/{theme}_surface_v4_1024_source.png",
            "source_sha256": digest(ARCHIVE / "source" / f"{theme}_surface_v4_1024_source.png"),
        },
        "delivery": {
            "output_path": str(master.relative_to(ROOT)).replace("\\", "/"),
            "output_sha256": digest(master),
            "logical_cells": [1, 1],
            "palette_max": 12,
            "low_resolution_companion": {
                "output_path": str(low.relative_to(ROOT)).replace("\\", "/"),
                "output_sha256": digest(low),
                "palette_max": 10,
            },
        },
        "application": {"runtime_draw_rect": "CombatTestArena 每个逻辑格独立拼接", "default_integer_scale": 1, "minimum_integer_scale": 1},
        "evidence": {"one_x": f"Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/{theme}_surface_1x.png", "four_x": f"Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/{theme}_surface_4x.png", "grayscale": f"Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/{theme}_surface_grayscale.png", "checker": f"Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/{theme}_surface_checker.png", "application_contact": "UnityProject/Reports/CombatTestArena/pixel_density_combo_v5_1920x1080.png"},
        "human_review": {"overall": "PENDING", "reviewer": None, "notes": "组合级像素密度样板，待人工审美确认"},
        "unity_import": {"asset_path": str(master.relative_to(ROOT)).replace("\\", "/"), "importer_verified": True, "runtime_verified": True},
    }
    (ARCHIVE / f"{theme}_surface_pixel_density_v2.occ-art-manifest-v1.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_cover() -> None:
    master = ART / "academy_test_heavy_cover_intact_64.png"
    low = ART / "academy_test_heavy_cover_intact_32.png"
    payload = {
        "schema": "occ-art-manifest-v1", "contract_version": 1,
        "asset_id": "academy_heavy_cover_intact_pixel_density_v7",
        "role": "battlefield_single_cell_prop_64", "status": "QA_PENDING",
        "provenance": {
            "source_channel": "easycli_chatgpt_pro_gpt_image_2_5",
            "source_descriptor": "组合级像素密度统一：方形学院工业重掩体独立原料",
            "source_path": "Worldbuilding/归档/2026-09-13_像素密度统一修复/source/heavy_cover_v7_64_source.png",
            "source_sha256": digest(ARCHIVE / "source/heavy_cover_v7_64_source.png"),
            "low_source_path": "Worldbuilding/归档/2026-09-13_像素密度统一修复/source/heavy_cover_v7_32_source.png",
            "low_source_sha256": digest(ARCHIVE / "source/heavy_cover_v7_32_source.png"),
        },
        "delivery": {"output_path": str(master.relative_to(ROOT)).replace("\\", "/"), "output_sha256": digest(master), "logical_cells": [1, 1], "palette_max": 14, "low_resolution_companion": {"output_path": str(low.relative_to(ROOT)).replace("\\", "/"), "output_sha256": digest(low), "palette_max": 10}},
        "application": {"runtime_draw_rect": "CombatTestArena 完整重掩体占一格；主体约45×44，阻挡移动与攻击线", "default_integer_scale": 1, "minimum_integer_scale": 1},
        "evidence": {"one_x": "Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/heavy_cover_1x.png", "four_x": "Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/heavy_cover_4x.png", "grayscale": "Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/heavy_cover_grayscale.png", "checker": "Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/heavy_cover_checker.png", "application_contact": "UnityProject/Reports/CombatTestArena/pixel_density_combo_v5_1920x1080.png"},
        "human_review": {"overall": "PENDING", "reviewer": None, "notes": "组合级像素密度样板，待人工审美确认"},
        "unity_import": {"asset_path": str(master.relative_to(ROOT)).replace("\\", "/"), "importer_verified": True, "runtime_verified": True},
    }
    (ARCHIVE / "heavy_cover_pixel_density_v7.occ-art-manifest-v1.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_crate() -> None:
    master = ART / "academy_test_book_crate_intact_64.png"
    low = ART / "academy_test_book_crate_intact_32.png"
    payload = {
        "schema": "occ-art-manifest-v1", "contract_version": 1,
        "asset_id": "academy_book_crate_intact_pixel_density_v2",
        "role": "battlefield_single_cell_prop_64", "status": "QA_PENDING",
        "provenance": {"source_channel": "easycli_chatgpt_pro_gpt_image_2_5", "source_descriptor": "组合级像素密度统一：低对比学院书箱独立原料", "source_path": "Worldbuilding/归档/2026-09-13_像素密度统一修复/source/book_crate_v2_64_source.png", "source_sha256": digest(ARCHIVE / "source/book_crate_v2_64_source.png"), "low_source_path": "Worldbuilding/归档/2026-09-13_像素密度统一修复/source/book_crate_v2_32_source.png", "low_source_sha256": digest(ARCHIVE / "source/book_crate_v2_32_source.png")},
        "delivery": {"output_path": str(master.relative_to(ROOT)).replace("\\", "/"), "output_sha256": digest(master), "logical_cells": [1, 1], "palette_max": 14, "low_resolution_companion": {"output_path": str(low.relative_to(ROOT)).replace("\\", "/"), "output_sha256": digest(low), "palette_max": 10}},
        "application": {"runtime_draw_rect": "CombatTestArena 轻型书箱占一格；主体约34×33，作为轻掩体", "default_integer_scale": 1, "minimum_integer_scale": 1},
        "evidence": {"one_x": "Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/book_crate_1x.png", "four_x": "Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/book_crate_4x.png", "grayscale": "Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/book_crate_grayscale.png", "checker": "Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence/book_crate_checker.png", "application_contact": "UnityProject/Reports/CombatTestArena/pixel_density_combo_v5_1920x1080.png"},
        "human_review": {"overall": "PENDING", "reviewer": None, "notes": "组合级像素密度样板，待人工审美确认"},
        "unity_import": {"asset_path": str(master.relative_to(ROOT)).replace("\\", "/"), "importer_verified": True, "runtime_verified": True},
    }
    (ARCHIVE / "book_crate_pixel_density_v2.occ-art-manifest-v1.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    write_surface("slate")
    write_surface("earth")
    write_cover()
    write_crate()
    print("wrote 4 QA_PENDING manifests")
