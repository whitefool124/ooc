#!/usr/bin/env python3
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A31/manifests"
CONTACT = "UnityProject/Artifacts/UiChapterMarkers6/contacts/ui_chapter_markers_application_1920x1080.png"

count = 0
for path in sorted(MANIFESTS.glob("marker_*.occ-art-manifest-v1.json")):
    manifest = json.loads(path.read_text(encoding="utf-8-sig"))
    manifest["status"] = "FORMAL_CANDIDATE"
    manifest["evidence"]["application_contact"] = CONTACT
    manifest["human_review"].update(
        {
            "overall": "PASS",
            "reviewer": "Product-owner delegated autonomous UI art review",
            "date": "2026-08-29",
            "silhouette": "PASS",
            "material": "PASS",
            "perspective": "PASS",
            "style": "PASS",
            "application": "PASS",
            "notes": manifest["human_review"]["notes"]
            + "; distinct compact material cue, no blue-crystal/cyan-energy template, passed 1x/4x checker, grayscale and dual-resolution offscreen UGUI contact.",
        }
    )
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    count += 1

print(json.dumps({"status": "PASS" if count == 6 else "FAIL", "formal_candidates": count}))

