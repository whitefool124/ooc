"""Run the OCC single art contract against every repository manifest."""

from __future__ import annotations

import json
import sys
from pathlib import Path

from validate_occ_art_asset import DEFAULT_CONTRACT, find_root, read_json, validate_manifest


def main() -> int:
    root = find_root(Path(__file__).resolve().parent)
    contract = read_json(DEFAULT_CONTRACT)
    manifests = sorted(root.glob("Worldbuilding/**/*.occ-art.json"))
    results = []
    for path in manifests:
        manifest = read_json(path)
        errors, report = validate_manifest(manifest, contract, root)
        results.append(
            {
                "manifest": path.relative_to(root).as_posix(),
                "asset_id": report.get("asset_id"),
                "status": "PASS" if not errors else "FAIL",
                "errors": errors,
            }
        )
    failed = [result for result in results if result["status"] == "FAIL"]
    summary = {
        "schema": "occ-art-repository-audit-v1",
        "status": "PASS" if manifests and not failed else "FAIL",
        "manifest_count": len(manifests),
        "pass_count": len(manifests) - len(failed),
        "fail_count": len(failed),
        "results": results,
    }
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0 if summary["status"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
