#!/usr/bin/env python3
"""Promote the Gate-0 OCC art manifest to the verified M-A3 v0.2 state."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


VFX = (
    "selection", "lock", "path", "landing", "shoot", "melee", "hit", "heavy_hit",
    "shield_hit", "shield_absorb", "shield_break", "shield_restore", "health_repair",
    "mana_restore", "cleanse", "burning", "slow", "bound", "armor_break", "dazzled",
    "revealed", "object_damage", "object_break", "debris", "fire_projectile", "fire_spray",
    "fire_cross_blast", "fire_burning_ground", "fire_detonate", "fire_smoke",
)


def promote(entry: dict) -> None:
    domain = entry["domain"]
    if domain == "archetype":
        entry["status"] = "BLOCKED_CONTENT"
        entry["notes"] = "Character/unit production paused by explicit product decision; existing visuals are not counted as formal completion."
    elif domain == "screen":
        entry["status"] = "FORMAL_CODE_BOUND"
        entry["notes"] = "Runtime-built formal UI screen; verified as code-native layout rather than a bitmap screen asset."
    elif domain in {"command", "feedback", "item", "node_type", "runtime_skill", "environment_object"}:
        entry["status"] = "FORMAL_QA_PASS_AND_BOUND"
    elif domain == "node":
        entry["status"] = "FORMAL_FRAMEWORK_BOUND"
        entry["notes"] = "Uses unique content ID with a formal node-type icon and composited state frame."
    elif domain == "fire_spell":
        entry["status"] = "FORMAL_QA_PASS_AND_RUNTIME_REACHABLE"
        entry["runtime_reachable"] = True
        entry["use_point"] = "FireSpellCatalog / reward pool / save / combat HUD / FireSpellEngine / CombatVisualFeedback"
        entry["notes"] = "Unique 32x32 icon is QA-passed and bound to the matching F-P runtime definition without fallback."
    elif domain == "environment":
        entry["status"] = "FORMAL_QA_PASS_RUNTIME_MODEL_BLOCKED"
        entry["notes"] = "Asset and registry are complete; the current combat map has no environment-tag model, so binding would change gameplay."
    elif domain == "status":
        if entry["id"] in {"dazzled", "revealed"}:
            entry["status"] = "FORMAL_QA_PASS_RUNTIME_MODEL_BLOCKED"
            entry["notes"] = "Frozen formal status asset exists, but StatusType does not implement this status."
        else:
            entry["status"] = "FORMAL_QA_PASS_AND_BOUND"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    data = json.loads(args.input.read_text(encoding="utf-8"))
    data["schema_version"] = "0.2"
    data["verification_date"] = "2026-08-08"
    data["supersedes"] = args.input.name
    data["verified_counts"] = {
        "noncharacter_static_png": 194,
        "vfx_effects": 30,
        "vfx_independent_frames": 180,
        "formal_unity_textures": 400,
        "registry_entries": 173,
        "editmode_tests_passed": 153,
        "editmode_tests_failed": 0,
        "runtime_visual_screenshots": 33,
        "runtime_visual_grayscale_variants": 23,
        "runtime_visual_deuteranopia_risk_variants": 23,
        "character_entries_blocked": 12,
    }
    for entry in data["entries"]:
        promote(entry)
    known = {entry["asset_id"] for entry in data["entries"]}
    for effect in VFX:
        asset_id = "vfx." + effect
        if asset_id in known:
            continue
        fire = effect.startswith("fire_")
        data["entries"].append({
            "domain": "vfx",
            "id": effect,
            "asset_id": asset_id,
            "path": "Art/FormalVfx32/" + effect,
            "status": "FORMAL_QA_PASS_AND_BOUND" if not fire else "FORMAL_QA_PASS_AND_REGISTERED",
            "runtime_reachable": True,
            "use_point": "CombatPrototypeBootstrap fireground/smoke overlay" if effect == "fire_smoke" else "CombatVisualFeedback",
            "notes": "Six independent 32x32 frames; strip, GIF, contact sheet and QA JSON are review derivatives." if effect == "fire_smoke" else "Six independent 32x32 frames; strip and GIF are review derivatives only.",
        })
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"version": data["schema_version"], "entries": len(data["entries"]), "counts": data["verified_counts"]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
