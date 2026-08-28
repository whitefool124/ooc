from __future__ import annotations

import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]
MANIFEST_DIR = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18/manifests/terrain_tileset_v12"
APPLICATION_CONTACT = (
    "Worldbuilding/05_美术与音频/正式美术生产/M-A18/qa/terrain_tileset_v12/"
    "academy_p0_application_contact_12x9.png"
)
REJECTED = {
    "academy_courtyard_base_b": "Redundant duplicate of base A; variation must return later as an authored multi-cell module, not a random per-cell variant.",
    "academy_aisle_straight": "Rejected in 12x9 review: half-cell connector reads as a drain/UI line instead of a maintained road region.",
    "academy_aisle_corner": "Rejected with the line-road family; replaced by region-based road base and transition autotiles.",
    "academy_aisle_end": "Rejected with the line-road family; road endpoints must be composed from region transition corners.",
    "academy_seal_court_2x2": "Rejected in the default 128px-cell Unity view: the full 2x2 ring dominates the battlefield and reads as a targeting reticle instead of an academy floor seal.",
}


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    for path in sorted(MANIFEST_DIR.glob("*.occ-art-manifest-v1.json")):
        manifest = json.loads(path.read_text(encoding="utf-8"))
        asset_id = manifest["asset_id"]
        source = ROOT / manifest["provenance"]["source_path"]
        output = ROOT / manifest["delivery"]["output_path"]
        manifest["provenance"]["source_channel"] = "hand_pixel"
        manifest["provenance"]["source_descriptor"] = (
            "Explicit hand-authored RLE pixel matrix; generic renderer expands exact pixels only and adds no art structure"
        )
        manifest["provenance"]["source_sha256"] = digest(source)
        manifest["delivery"]["output_sha256"] = digest(output)
        manifest["evidence"]["application_contact"] = APPLICATION_CONTACT
        review = manifest["human_review"]
        if asset_id in REJECTED:
            manifest["status"] = "PROTOTYPE"
            review.update({
                "overall": "FAIL",
                "reviewer": "Codex visual audit",
                "date": "2026-08-24",
                "silhouette": "FAIL",
                "material": "PASS",
                "perspective": "PASS",
                "style": "FAIL",
                "application": "FAIL",
                "notes": REJECTED[asset_id],
            })
        else:
            manifest["status"] = "REVIEW_READY"
            review.update({
                "overall": "PENDING",
                "reviewer": "",
                "date": "",
                "silhouette": "PENDING",
                "material": "PENDING",
                "perspective": "PENDING",
                "style": "PENDING",
                "application": "PENDING",
                "notes": "Machine and Codex preflight passed; product aesthetic review pending before FORMAL_CANDIDATE.",
            })
        path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"{asset_id}: {manifest['status']}")


if __name__ == "__main__":
    main()
