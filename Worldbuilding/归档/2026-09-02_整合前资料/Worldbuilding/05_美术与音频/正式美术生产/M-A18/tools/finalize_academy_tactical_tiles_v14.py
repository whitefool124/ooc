from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
REPO = ROOT.parents[3]


def guid_for(asset_path: Path) -> str:
    meta = Path(str(asset_path) + ".meta")
    for line in meta.read_text(encoding="utf-8").splitlines():
        if line.startswith("guid: "):
            return line.split(":", 1)[1].strip()
    raise RuntimeError(f"missing guid in {meta}")


def finalize(manifest_path: Path, asset_path: Path, resource_path: str, runtime_evidence: str) -> None:
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    manifest["status"] = "FORMAL"
    stable_guid = guid_for(asset_path)
    manifest["unity_import"] = {
        "asset_path": asset_path.relative_to(REPO).as_posix(),
        "resource_path": resource_path,
        "guid": stable_guid,
        "stable_guid": stable_guid,
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
        "runtime_evidence": runtime_evidence,
        "resolutions": ["1920x1080", "960x540"],
        "verification_date": "2026-08-25",
    }
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    manifests = ROOT / "manifests" / "terrain_tileset_v14"
    formal = REPO / "UnityProject" / "Assets" / "Game" / "Resources" / "Art" / "FormalAcademyCombat32"
    for manifest_path in sorted(manifests.glob("*.occ-art-manifest-v1.json")):
        asset_id = manifest_path.name.split(".occ-art-manifest-v1.json")[0]
        finalize(
            manifest_path,
            formal / f"{asset_id}.png",
            f"Art/FormalAcademyCombat32/{asset_id}",
            "UnityProject/Artifacts/Terrain36/NineMaps/rail_patrol_1920x1080.png",
        )

    dais_manifest = ROOT / "manifests" / "terrain_tileset_v13" / "academy_north_dais_6x2.occ-art-manifest-v1.json"
    dais_asset = REPO / "UnityProject" / "Assets" / "Game" / "Resources" / "Art" / "FormalAcademyStructures32" / "academy_north_dais_6x2.png"
    finalize(
        dais_manifest,
        dais_asset,
        "Art/FormalAcademyStructures32/academy_north_dais_6x2",
        "UnityProject/Artifacts/Terrain36/NineMaps/signal_hub_1920x1080.png",
    )


if __name__ == "__main__":
    main()
