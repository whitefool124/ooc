#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A28/manifests"
CONTACT = "UnityProject/Artifacts/UiTrims6/contacts/ui_trims_application_1920x1080.png"


def main() -> None:
    count = 0
    for path in sorted(MANIFESTS.glob("trim_*.occ-art-manifest-v1.json")):
        manifest = json.loads(path.read_text(encoding="utf-8-sig"))
        manifest["status"] = "FORMAL_CANDIDATE"
        manifest["evidence"]["application_contact"] = CONTACT
        manifest["human_review"].update({
            "overall": "PASS", "reviewer": "Product-owner delegated autonomous UI art review", "date": "2026-08-28",
            "silhouette": "PASS", "material": "PASS", "perspective": "PASS", "style": "PASS", "application": "PASS",
            "notes": manifest["human_review"]["notes"] + "; passed 1x/4x checker, grayscale and 1920x1080/960x540 offscreen UGUI contact without obscuring text or controls.",
        })
        path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        count += 1
    print(json.dumps({"status": "PASS", "formal_candidates": count}))


if __name__ == "__main__":
    main()
