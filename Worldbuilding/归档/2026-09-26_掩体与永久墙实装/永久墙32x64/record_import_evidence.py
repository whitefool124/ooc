"""Record verified Unity GUIDs without promoting artwork past human review."""

import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[2]
WALL_ASSETS = PROJECT / "UnityProject/Assets/Game/Resources/Art/AcademyPermanentWallCandidates20260926"
COVER_ASSETS = PROJECT / "UnityProject/Assets/Game/Resources/Art/AcademyCoverCandidates20260924"
COVER_MANIFESTS = ROOT.parent / "manifests"

VISIBLE_WALLS = {"es_a", "ew_a", "ew_b", "ne_a", "ns_a", "ns_b", "nw_b", "sw_b"}
VISIBLE_COVERS = {
    "academy_light_book_crate_intact", "academy_light_wood_bench_intact",
    "academy_heavy_training_barricade_intact",
}
COVER_NAMES = {
    "book_crate": "academy_light_book_crate_intact",
    "light_cover_intact": "academy_light_planter_cover_intact",
    "training_pad": "academy_light_training_pad_intact",
    "wood_bench": "academy_light_wood_bench_intact",
    "heavy_cover_intact": "academy_heavy_training_barricade_intact",
    "heavy_cover_rubble": "academy_heavy_training_barricade_rubble",
}


def guid_for(asset: Path) -> str:
    text = asset.with_suffix(asset.suffix + ".meta").read_text(encoding="utf-8")
    match = re.search(r"^guid:\s*([0-9a-f]{32})$", text, re.MULTILINE)
    if not match:
        raise ValueError(f"No GUID for {asset}")
    return match.group(1)


def update(path: Path, asset: Path, runtime_verified: bool):
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    settings = data.setdefault("unity_import", {})
    settings["stable_guid"] = guid_for(asset)
    settings["importer_verified"] = True
    settings["runtime_verified"] = runtime_verified
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    walls = ROOT / "connected_family_manifests"
    for manifest in walls.glob("*.occ-art-manifest-v1.json"):
        suffix = manifest.name.split(".occ-art-manifest-v1.json")[0]
        asset = WALL_ASSETS / f"academy_permanent_wall_{suffix}.png"
        update(manifest, asset, suffix in VISIBLE_WALLS)
    for key, name in COVER_NAMES.items():
        update(COVER_MANIFESTS / f"{key}.occ-art-manifest-v1.json",
               COVER_ASSETS / f"{name}.png", name in VISIBLE_COVERS)


if __name__ == "__main__":
    main()
