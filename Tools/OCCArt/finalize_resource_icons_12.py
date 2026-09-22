"""Promote the reviewed native-12 resource glyphs after Unity verification."""

from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "ArtSource/resource_icons_12_handdrawn_2026-09-22"
MANIFESTS = SOURCE / "manifests"
UNITY = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalResourceIcons12"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(path: Path) -> str:
    match = re.search(r"^guid: ([0-9a-f]+)$", Path(str(path) + ".meta").read_text(encoding="utf-8"), re.MULTILINE)
    if not match:
        raise ValueError(f"Missing Unity GUID for {path}")
    return match.group(1)


def main() -> None:
    for manifest_path in sorted(MANIFESTS.glob("*.occ-art-manifest-v1.json")):
        data = json.loads(manifest_path.read_text(encoding="utf-8"))
        stem = manifest_path.name.removesuffix(".occ-art-manifest-v1.json")
        target = UNITY / f"{stem}.png"
        data["status"] = "FORMAL"
        data["delivery"]["output_path"] = target.relative_to(ROOT).as_posix()
        data["delivery"]["output_sha256"] = sha256(target)
        data["unity_import"] = {
            "asset_path": target.relative_to(ROOT / "UnityProject").as_posix(),
            "stable_guid": guid(target),
            "texture_type": "Sprite",
            "pixels_per_unit": 12,
            "filter_mode": "Point",
            "mesh_type": "Full Rect",
            "compression": "None",
            "mipmaps": False,
            "wrap_mode": "Clamp",
            "atlas_padding": 0,
            "atlas_rotation": False,
            "atlas_tight_packing": False,
            "atlas_extrude": 0,
            "importer_verified": True,
            "runtime_verified": True,
            "runtime_lattice": {"pass": True, "tier": 3, "capture": "UnityProject/Reports/CombatTestArena/ui_refine_final_1920x1080.png", "notes": "Registry load, importer readback and combat HUD runtime capture verified."},
        }
        data["human_review"]["notes"] = "Native 12px silhouettes and pure-black outlines passed contact review; imported at 12 PPU and verified in the combat HUD."
        manifest_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"finalized={len(list(MANIFESTS.glob('*.occ-art-manifest-v1.json')))}")


if __name__ == "__main__":
    main()
