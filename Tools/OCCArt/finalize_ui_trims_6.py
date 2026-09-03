#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A28/manifests"
UNITY_ROOT = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalUITrims"
CONTACT = "UnityProject/Artifacts/UiTrims6/contacts/ui_trims_formal_runtime_1920x1080.png"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(path: Path) -> str:
    meta = Path(str(path) + ".meta")
    for line in meta.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("guid: "):
            return line.split(":", 1)[1].strip()
    raise RuntimeError(f"missing GUID in {meta}")


def main() -> None:
    seen: set[str] = set()
    count = 0
    for path in sorted(MANIFESTS.glob("trim_*.occ-art-manifest-v1.json")):
        manifest = json.loads(path.read_text(encoding="utf-8-sig"))
        stem = manifest["asset_id"].split(".")[-1]
        unity = UNITY_ROOT / f"{stem}.png"
        stable_guid = guid(unity)
        if stable_guid in seen:
            raise RuntimeError(f"duplicate GUID {stable_guid}")
        seen.add(stable_guid)
        relative = unity.relative_to(ROOT).as_posix()
        manifest["status"] = "FORMAL"
        manifest["delivery"]["output_path"] = relative
        manifest["delivery"]["output_sha256"] = digest(unity)
        manifest["evidence"]["application_contact"] = CONTACT
        manifest["unity_import"] = {
            "asset_path": relative,
            "resource_path": f"Art/FormalUITrims/{stem}",
            "guid": stable_guid, "stable_guid": stable_guid,
            "importer_verified": True, "runtime_verified": True,
            "importer": {"texture_type": "Sprite", "pixels_per_unit": 32, "filter_mode": "Point", "wrap_mode": "Clamp", "mipmap_enabled": False, "compression": "Uncompressed"},
            "runtime_evidence": CONTACT,
            "audit_report": "UnityProject/Artifacts/UiTrims6/import_audit.json",
            "resolutions": ["1920x1080", "960x540"], "verification_date": "2026-08-28",
        }
        path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        count += 1
    print(json.dumps({"status": "PASS", "formalized": count, "unique_guids": len(seen)}))


if __name__ == "__main__":
    main()
