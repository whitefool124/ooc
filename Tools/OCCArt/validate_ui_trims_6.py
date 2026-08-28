#!/usr/bin/env python3
from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A28/manifests"
VALIDATOR = ROOT / "Tools/OCCArt/validate_occ_art_asset.py"
REPORT = ROOT / "UnityProject/Artifacts/UiTrims6/validation_report.json"


def main() -> int:
    results = []
    for manifest in sorted(MANIFESTS.glob("trim_*.occ-art-manifest-v1.json")):
        process = subprocess.run([sys.executable, str(VALIDATOR), str(manifest), "--root", str(ROOT)], capture_output=True, text=True, encoding="utf-8")
        results.append(json.loads(process.stdout))
    contract_process = subprocess.run([sys.executable, str(VALIDATOR), "--root", str(ROOT), "--audit-contract"], capture_output=True, text=True, encoding="utf-8")
    contract = json.loads(contract_process.stdout)
    passed = sum(result.get("status") == "PASS" for result in results)
    report = {
        "schema": "occ-art-batch-validation-report-v1", "batch": "ART-UI-TRIMS-61",
        "status": "PASS" if passed == len(results) == 6 and contract.get("status") == "PASS" else "FAIL",
        "summary": {"passed": passed, "total": len(results), "contract": contract.get("status")},
        "contract_audit": contract, "assets": results,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0 if report["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
