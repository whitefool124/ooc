#!/usr/bin/env python3
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A31/manifests"
VALIDATOR = ROOT / "Tools/OCCArt/validate_occ_art_asset.py"
REPORT = ROOT / "UnityProject/Artifacts/UiChapterMarkers6/validation_report.json"

results = []
for path in sorted(MANIFESTS.glob("marker_*.occ-art-manifest-v1.json")):
    proc = subprocess.run(
        [sys.executable, str(VALIDATOR), str(path), "--root", str(ROOT)],
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    results.append(json.loads(proc.stdout))

proc = subprocess.run(
    [sys.executable, str(VALIDATOR), "--root", str(ROOT), "--audit-contract"],
    capture_output=True,
    text=True,
    encoding="utf-8",
)
contract = json.loads(proc.stdout)
passed = sum(item.get("status") == "PASS" for item in results)
report = {
    "schema": "occ-art-batch-validation-report-v1",
    "batch": "ART-UI-MARKERS-64",
    "status": "PASS" if passed == len(results) == 6 and contract.get("status") == "PASS" else "FAIL",
    "summary": {"passed": passed, "total": len(results), "contract": contract.get("status")},
    "contract_audit": contract,
    "assets": results,
}
REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(report, ensure_ascii=False, indent=2))
raise SystemExit(0 if report["status"] == "PASS" else 1)

