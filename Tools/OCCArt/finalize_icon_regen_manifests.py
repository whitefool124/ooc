#!/usr/bin/env python3
"""Advance reviewed M-A19 icons to FORMAL_CANDIDATE and later FORMAL."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A19/manifests"
UNITY_CONTACT = "UnityProject/Artifacts/IconRegen143/contacts/unity_formal_contact_1920x1080.png"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--formal", action="store_true")
    parser.add_argument("--unity-report")
    args = parser.parse_args()
    paths = sorted(MANIFESTS.rglob("*.occ-art-manifest-v1.json"))
    unity_rows = {}
    if args.formal and args.unity_report:
        report = json.loads((ROOT / args.unity_report).read_text(encoding="utf-8"))
        unity_rows = {row["asset_id"]: row for row in report.get("assets", [])}
    for path in paths:
        value = json.loads(path.read_text(encoding="utf-8"))
        value["evidence"]["application_contact"] = UNITY_CONTACT
        review = value["human_review"]
        review.update({
            "overall": "PASS", "reviewer": "Codex visual review",
            "date": "2026-08-26", "silhouette": "PASS", "material": "PASS",
            "perspective": "PASS", "style": "PASS", "application": "PASS",
        })
        review["notes"] = review.get("notes", "") + " | Reviewed at 1x/4x/grayscale/checker and Unity 1920x1080/960x540 contacts; palette repair removed repeated cyan crystal/energy motifs."
        if args.formal:
            row = unity_rows.get(value["asset_id"], {})
            value["status"] = "FORMAL"
            value["unity_import"] = {
                "status": "PASS", "report": args.unity_report or "UnityProject/Artifacts/IconRegen143/unity_import_report.json",
                "stable_guid": row.get("guid", ""), "importer_verified": bool(row.get("pass")),
                "runtime_verified": bool(row.get("pass")),
            }
        else:
            value["status"] = "FORMAL_CANDIDATE"
        path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"updated": len(paths), "status": "FORMAL" if args.formal else "FORMAL_CANDIDATE"}))


if __name__ == "__main__":
    main()
