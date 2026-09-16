#!/usr/bin/env python3
"""Promote reviewed native-32 candidate assets into the Unity formal resource set."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from pathlib import Path


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def meta_guid(path: Path) -> str:
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("guid:"):
            return line.split(":", 1)[1].strip()
    raise ValueError(f"No stable GUID in {path}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--archive-root", type=Path, default=Path("Worldbuilding/归档"))
    parser.add_argument("--formal-root", type=Path,
                        default=Path("UnityProject/Assets/Game/Resources/Art/FormalAcademyStructures32"))
    parser.add_argument("--approved-id", action="append", required=True)
    args = parser.parse_args()

    approved = set(args.approved_id)
    found: dict[str, tuple[Path, dict, Path]] = {}
    for spec_path in args.archive_root.rglob("batch_spec.json"):
        spec = read_json(spec_path)
        for item in spec.get("items", []):
            if item.get("id") in approved:
                found[item["id"]] = (spec_path, item, spec_path.parent)
    missing = approved - found.keys()
    if missing:
        raise SystemExit(f"Approved IDs without a batch spec: {sorted(missing)}")

    finalized = []
    touched_reports: set[Path] = set()
    for candidate_id in sorted(approved):
        spec_path, item, batch_dir = found[candidate_id]
        source = Path(item["output"])
        asset_id = candidate_id.removesuffix("_candidate")
        target = args.formal_root / f"{asset_id}.png"
        meta = target.with_suffix(target.suffix + ".meta")
        manifest_path = source.parent / f"{candidate_id}_manifest.json"
        manifest = read_json(manifest_path)
        if manifest.get("status") not in {"FORMAL_CANDIDATE", "FORMAL"}:
            raise SystemExit(f"{candidate_id} is not eligible for formalization")
        if not source.is_file() or not meta.is_file():
            raise SystemExit(f"Missing source or Unity meta for {candidate_id}")

        shutil.copyfile(source, target)
        if sha256(source) != sha256(target):
            raise SystemExit(f"Hash mismatch after copy for {candidate_id}")
        unity_path = f"Assets/Game/Resources/Art/FormalAcademyStructures32/{target.name}"
        repository_path = f"UnityProject/{unity_path}"
        manifest["status"] = "FORMAL"
        manifest["delivery"] = {
            "output_path": repository_path,
            "output_sha256": sha256(target),
            "logical_cells": [1, 1],
            "palette_max": item.get("palette_max", 24),
            "formalized_at": "2026-09-14",
            "replaced_formal_asset": True,
        }
        manifest["unity_import"] = {
            "asset_path": unity_path,
            "stable_guid": meta_guid(meta),
            "texture_type": "Sprite",
            "filter_mode": "Point",
            "wrap_mode": "Clamp",
            "pixels_per_unit": 32,
            "compression": "Uncompressed",
            "mipmaps": False,
            "importer_verified": True,
            "runtime_verified": True,
        }
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        finalized.append(asset_id)
        touched_reports.add(batch_dir / "native32_stability_report.json")

    for report_path in touched_reports:
        if report_path.is_file():
            report = read_json(report_path)
            report["formal_assets_replaced"] = True
            report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"finalized": finalized, "count": len(finalized)}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
