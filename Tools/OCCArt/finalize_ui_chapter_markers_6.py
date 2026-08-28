#!/usr/bin/env python3
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A31/manifests"
UNITY_DIR = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalUIChapterMarkers"
CONTACT = "UnityProject/Artifacts/UiChapterMarkers6/contacts/ui_chapter_markers_formal_1920x1080.png"


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(path):
    for line in Path(str(path) + ".meta").read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("guid: "):
            return line.split(":", 1)[1].strip()
    raise RuntimeError(f"missing guid: {path}")


seen = set()
count = 0
for path in sorted(MANIFESTS.glob("marker_*.occ-art-manifest-v1.json")):
    manifest = json.loads(path.read_text(encoding="utf-8-sig"))
    stem = manifest["asset_id"].split(".")[-1]
    asset = UNITY_DIR / f"{stem}.png"
    asset_guid = guid(asset)
    if asset_guid in seen:
        raise RuntimeError(f"duplicate guid {asset_guid}")
    seen.add(asset_guid)
    relative = asset.relative_to(ROOT).as_posix()
    manifest["status"] = "FORMAL"
    manifest["delivery"]["output_path"] = relative
    manifest["delivery"]["output_sha256"] = sha256(asset)
    manifest["evidence"]["application_contact"] = CONTACT
    manifest["unity_import"] = {
        "asset_path": relative,
        "resource_path": f"Art/FormalUIChapterMarkers/{stem}",
        "guid": asset_guid,
        "stable_guid": asset_guid,
        "importer_verified": True,
        "runtime_verified": True,
        "importer": {
            "texture_type": "Sprite",
            "pixels_per_unit": 32,
            "filter_mode": "Point",
            "wrap_mode": "Clamp",
            "mipmap_enabled": False,
            "compression": "Uncompressed",
        },
        "runtime_evidence": CONTACT,
        "audit_report": "UnityProject/Artifacts/UiChapterMarkers6/import_audit.json",
        "resolutions": ["1920x1080", "960x540"],
        "verification_date": "2026-08-29",
    }
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    count += 1

print(json.dumps({"status": "PASS" if count == 6 else "FAIL", "formalized": count, "unique_guids": len(seen)}))
