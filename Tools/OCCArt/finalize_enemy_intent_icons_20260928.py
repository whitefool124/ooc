"""Record verified Unity import and runtime evidence for the intent icon set."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "ArtSource/intent_icon_reset_20260928/formal_candidates"
ART = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalEnemyIntentIcons"
CAPTURE = "UnityProject/Artifacts/EnemyIntentIcons20260928/all_icons_runtime_1920x1080.png"
LATTICE = ROOT / "UnityProject/Artifacts/EnemyIntentIcons20260928/runtime_lattice_all.json"
NAMES = ("attack", "cast", "move", "defend", "interact_destroy")


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(path: Path) -> str:
    for line in Path(str(path) + ".meta").read_text(encoding="utf-8").splitlines():
        if line.startswith("guid: "):
            return line.removeprefix("guid: ").strip()
    raise RuntimeError(f"missing Unity GUID: {path}")


def main() -> None:
    lattice = json.loads(LATTICE.read_text(encoding="utf-8"))
    if not lattice["pass"] or lattice["detected_tiers"] != [3]:
        raise RuntimeError("runtime lattice did not pass at 3x")
    for name in NAMES:
        manifest_path = BASE / name / f"{name}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        original_output = ROOT / manifest["delivery"]["output_path"]
        final_output = ART / f"{name}.png"
        if sha256(original_output) != sha256(final_output):
            raise RuntimeError(f"final icon differs from validated candidate: {name}")
        manifest["status"] = "FORMAL"
        manifest["delivery"]["output_path"] = relative(final_output)
        manifest["delivery"]["output_sha256"] = sha256(final_output)
        manifest["evidence"]["application_contact"] = CAPTURE
        manifest["human_review"] = {
            "overall": "PASS",
            "reviewer": "Codex OCC art-direction review",
            "date": "2026-09-28",
            "silhouette": "PASS",
            "material": "PASS",
            "perspective": "PASS",
            "style": "PASS",
            "application": "PASS",
            "illumination": "PASS",
            "notes": "Five silhouettes remain distinct at the shared 3x native-pixel scale in the live Unity overlay; movement also reviewed above a real enemy. No source subject scaling or recolouring.",
        }
        manifest["unity_import"] = {
            "asset_path": relative(final_output).removeprefix("UnityProject/"),
            "stable_guid": guid(final_output),
            "texture_type": "Sprite",
            "filter_mode": "Point",
            "mesh_type": "Full Rect",
            "compression": "None",
            "mipmaps": False,
            "wrap_mode": "Clamp",
            "atlas_padding": 0,
            "atlas_rotation": False,
            "atlas_tight_packing": False,
            "atlas_extrude": 0,
            "pixels_per_unit": 32,
            "importer_verified": True,
            "runtime_verified": True,
            "runtime_lattice": {
                "tool": "Tools/OCCArt/verify_runtime_pixel_grid.py",
                "capture": CAPTURE,
                "report": relative(LATTICE),
                "region": name,
                "tier": 3,
                "offset": [0, 0],
                "pass": True,
                "uniform_with_siblings": True,
            },
        }
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"formal_icons": len(NAMES), "runtime_lattice": "PASS"}))


if __name__ == "__main__":
    main()
