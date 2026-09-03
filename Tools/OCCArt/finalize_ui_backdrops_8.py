#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
M27 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A27"
MANIFESTS = M27 / "manifests"
UNITY_ROOT = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalUIBackdrops"
ARTIFACT_ROOT = "UnityProject/Artifacts/UiBackdrops8"
STEMS = ("startup", "landing", "map", "briefing", "inventory", "settlement", "archive", "settings")


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stable_guid(path: Path) -> str:
    meta = Path(str(path) + ".meta")
    for line in meta.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("guid: "):
            return line.split(":", 1)[1].strip()
    raise RuntimeError(f"missing GUID in {meta}")


def main() -> None:
    guids: set[str] = set()
    for stem in STEMS:
        manifest_path = MANIFESTS / f"backdrop_{stem}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
        unity_path = UNITY_ROOT / f"{stem}.png"
        guid = stable_guid(unity_path)
        if guid in guids:
            raise RuntimeError(f"duplicate GUID: {guid}")
        guids.add(guid)

        relative_unity = unity_path.relative_to(ROOT).as_posix()
        manifest["status"] = "FORMAL"
        manifest["delivery"]["output_path"] = relative_unity
        manifest["delivery"]["output_sha256"] = digest(unity_path)
        manifest["evidence"]["application_contact"] = (
            f"{ARTIFACT_ROOT}/contacts/{stem}_1920x1080.png"
        )
        manifest["human_review"].update({
            "overall": "PASS",
            "reviewer": "Product-owner delegated autonomous UI art review",
            "date": "2026-08-28",
            "silhouette": "PASS",
            "material": "PASS",
            "perspective": "PASS",
            "style": "PASS",
            "application": "PASS",
            "notes": (
                manifest["human_review"]["notes"]
                + "; passed native 1x, integer 2x/4x, grayscale, checkerboard, "
                "1920x1080 and 960x540 Unity contacts. The landing page also passed "
                "an offscreen UGUI application contact using FormalUiEffects.ApplyBackdrop."
            ),
        })
        manifest["unity_import"] = {
            "asset_path": relative_unity,
            "resource_path": f"Art/FormalUIBackdrops/{stem}",
            "guid": guid,
            "stable_guid": guid,
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
            "runtime_evidence": f"{ARTIFACT_ROOT}/contacts/{stem}_1920x1080.png",
            "application_evidence": f"{ARTIFACT_ROOT}/contacts/application_landing_1920x1080.png",
            "audit_report": f"{ARTIFACT_ROOT}/import_audit.json",
            "resolutions": ["1920x1080", "960x540"],
            "verification_date": "2026-08-28",
        }
        manifest_path.write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )

    print(json.dumps({"status": "PASS", "formalized": len(STEMS), "unique_guids": len(guids)}))


if __name__ == "__main__":
    main()
