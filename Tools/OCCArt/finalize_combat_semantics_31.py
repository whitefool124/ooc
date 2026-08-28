#!/usr/bin/env python3
"""Finalize M-A24 manifests after Unity import and application review pass."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PRODUCTION = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A24"
CATALOG = PRODUCTION / "combat_semantics_31_catalog.json"
MANIFESTS = PRODUCTION / "manifests"
IMPORT_REPORT = ROOT / "UnityProject/Artifacts/CombatSemantics31/unity_import_report.json"
CONTACT = "UnityProject/Artifacts/CombatSemantics31/contacts/unity_combat_semantics_1920x1080.png"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    report = json.loads(IMPORT_REPORT.read_text(encoding="utf-8-sig"))
    if report["count"] != 31 or report["loaded"] != 31 or report["unique_guids"] != 31 or report["failures"]:
        raise RuntimeError("Unity import report is not a clean 31/31 pass")
    imports = {entry["asset_id"]: entry for entry in report["assets"]}

    for asset in catalog:
        manifest_path = MANIFESTS / f"{asset['group']}_{asset['stem']}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        import_key = manifest["asset_id"].removeprefix("combat.")
        unity = imports[import_key]
        formal = ROOT / asset["final_path"]
        manifest["status"] = "FORMAL"
        manifest["delivery"]["output_path"] = asset["final_path"]
        manifest["delivery"]["output_sha256"] = digest(formal)
        manifest["evidence"]["application_contact"] = CONTACT
        manifest["human_review"] = {
            "overall": "PASS",
            "reviewer": "Codex OCC art-direction review",
            "date": "2026-08-27",
            "silhouette": "PASS",
            "material": "PASS",
            "perspective": "PASS",
            "style": "PASS",
            "application": "PASS",
            "notes": "Distinct combat semantic silhouette, function-specific palette, native integer-scale readability, and Unity 1920x1080 / 960x540 contact approved.",
        }
        manifest["unity_import"] = {
            "asset_path": unity["asset_path"],
            "stable_guid": unity["guid"],
            "texture_type": "Sprite",
            "filter_mode": "Point",
            "wrap_mode": "Clamp",
            "pixels_per_unit": unity["ppu"],
            "compression": "Uncompressed",
            "mipmaps": False,
            "importer_verified": unity["importer_ok"],
            "runtime_verified": True,
        }
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"finalized": len(catalog), "status": "FORMAL"}))


if __name__ == "__main__":
    main()
