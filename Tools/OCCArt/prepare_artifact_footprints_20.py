#!/usr/bin/env python3
"""Create M-A22 artifact footprint catalog and manifests before generation."""
from __future__ import annotations
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
M19 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A19/icon_regen_143_catalog.json"
OUT = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A22"
MANIFESTS = OUT / "manifests"
FORMAL = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalInventoryFootprints"

def main() -> None:
    base = [x for x in json.loads(M19.read_text(encoding="utf-8"))["assets"] if x["group"] == "artifact"]
    assets = []
    MANIFESTS.mkdir(parents=True, exist_ok=True)
    for item in base:
        stem = item["stem"]
        px = Image.open(FORMAL / f"{stem}.png").size
        cells = [px[0] // 32, px[1] // 32]
        root = f"UnityProject/Artifacts/ArtifactFootprints20/{stem}"
        value = {**item, "asset_id": f"artifact.footprint.{stem}", "role": "multi_cell_prop_32",
            "logical_cells": cells, "delivery_size": list(px), "palette_max": 12,
            "final_path": f"UnityProject/Assets/Game/Resources/Art/FormalInventoryFootprints/{stem}.png",
            "staging_path": f"UnityProject/Assets/Game/Resources/Art/ValidationArtifactFootprints/{stem}.png",
            "source_path": f"{root}/source.png"}
        assets.append(value)
        manifest = {"schema":"occ-art-manifest-v1","contract_version":1,"asset_id":value["asset_id"],
            "role":"multi_cell_prop_32","status":"QA_PENDING",
            "provenance":{"source_channel":"codex_builtin_imagegen","source_descriptor":"Independent single artifact footprint; no icon stretching or board slicing","source_path":value["source_path"],"source_sha256":"PENDING_GENERATION"},
            "delivery":{"output_path":value["staging_path"],"output_sha256":"PENDING_GENERATION","native_output_path":None,"logical_cells":cells,"palette_max":12,"required_color_families":[]},
            "application":{"runtime_draw_rect":f"{cells[0]}x{cells[1]} inventory cells at 32 pixels per cell","default_integer_scale":2,"minimum_integer_scale":1},
            "evidence":{"one_x":f"{root}/1x.png","four_x":f"{root}/4x.png","grayscale":f"{root}/grayscale.png","checker":f"{root}/checker.png","application_contact":"UnityProject/Artifacts/ArtifactFootprints20/contacts/PENDING.png"},
            "human_review":{"overall":"PENDING","reviewer":"","date":"","silhouette":"PENDING","material":"PENDING","perspective":"PENDING","style":"PENDING","application":"PENDING","notes":item["subject"]},"unity_import":None}
        (MANIFESTS / f"{stem}.occ-art-manifest-v1.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    if len(assets) != 20: raise RuntimeError(f"Expected 20, got {len(assets)}")
    (OUT / "artifact_footprints_20_catalog.json").write_text(json.dumps({"schema":"occ-artifact-footprints-catalog-v1","count":20,"assets":assets}, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    print(json.dumps({"count":len(assets),"sizes":sorted({tuple(x['delivery_size']) for x in assets})}))
if __name__ == "__main__": main()
