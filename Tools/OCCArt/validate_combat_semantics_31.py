#!/usr/bin/env python3
"""Aggregate formal validation for the 31 M-A24 combat semantic icons."""
from __future__ import annotations

import json
import sys
from pathlib import Path

from validate_occ_art_asset import DEFAULT_CONTRACT, find_root, read_json, validate_manifest


def main() -> int:
    root = find_root(Path(__file__).resolve().parent)
    contract = read_json(DEFAULT_CONTRACT)
    manifest_root = root / "Worldbuilding/05_美术与音频/正式美术生产/M-A24/manifests"
    results = []
    for path in sorted(manifest_root.glob("*.occ-art-manifest-v1.json")):
        errors, report = validate_manifest(read_json(path), contract, root)
        results.append({
            "manifest": path.relative_to(root).as_posix(),
            "asset_id": report.get("asset_id"),
            "status": "PASS" if not errors else "FAIL",
            "errors": errors,
            "metrics": report.get("metrics", {}),
        })
    failed = [result for result in results if result["status"] == "FAIL"]
    summary = {
        "schema": "occ-m-a24-formal-validation-v1",
        "status": "PASS" if len(results) == 31 and not failed else "FAIL",
        "manifest_count": len(results),
        "pass_count": len(results) - len(failed),
        "fail_count": len(failed),
        "results": results,
    }
    output = root / "UnityProject/Artifacts/CombatSemantics31/validation_report_formal.json"
    output.write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: summary[key] for key in ("status", "manifest_count", "pass_count", "fail_count")}))
    return 0 if summary["status"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
