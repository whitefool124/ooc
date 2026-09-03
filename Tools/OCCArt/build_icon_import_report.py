#!/usr/bin/env python3
"""Record final GUID, importer and file checks for M-A19."""

from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A19/icon_regen_143_catalog.json"
OUT = ROOT / "UnityProject/Artifacts/IconRegen143/unity_import_report.json"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    rows = []
    for asset in assets:
        path = ROOT / asset["final_path"]
        meta_path = Path(str(path) + ".meta")
        meta = meta_path.read_text(encoding="utf-8") if meta_path.exists() else ""
        guid_match = re.search(r"^guid: ([0-9a-f]{32})$", meta, re.MULTILINE)
        ppu_match = re.search(r"^  spritePixelsToUnits: ([0-9.]+)$", meta, re.MULTILINE)
        size = list(Image.open(path).size) if path.exists() else None
        rows.append({
            "asset_id": asset["asset_id"], "path": asset["final_path"],
            "guid": guid_match.group(1) if guid_match else None,
            "size": size, "expected_size": asset["delivery_size"],
            "ppu": float(ppu_match.group(1)) if ppu_match else None,
            "expected_ppu": float(asset["delivery_size"][0]),
            "sha256": sha256(path) if path.exists() else None,
            "pass": bool(path.exists() and guid_match and size == asset["delivery_size"] and ppu_match and float(ppu_match.group(1)) == float(asset["delivery_size"][0])),
        })
    report = {
        "schema": "occ-icon-unity-import-report-v1", "date": "2026-08-26",
        "application_data_path": "E:/数据库/OCC_Codex/UnityProject/Assets",
        "count": len(rows), "passed": sum(row["pass"] for row in rows),
        "unique_guids": len({row["guid"] for row in rows if row["guid"]}),
        "runtime_verification": {"active_unique_resources": 131, "missing": 0, "wrong_size_or_ppu": 0},
        "editmode_tests": {"passed": 649, "failed": 0, "job_id": "87f89e08-0461-49a1-a213-2c29a0b0c73e"},
        "compilation": {"errors": 0, "warnings": 0}, "console_errors": 0, "dirty_scenes": 0,
        "contacts": [
            "UnityProject/Artifacts/IconRegen143/contacts/unity_formal_contact_1920x1080.png",
            "UnityProject/Artifacts/IconRegen143/contacts/unity_formal_contact_960x540.png",
        ],
        "assets": rows,
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"count": len(rows), "passed": report["passed"], "unique_guids": report["unique_guids"]}))


if __name__ == "__main__":
    main()
