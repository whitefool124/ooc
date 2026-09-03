from __future__ import annotations

import json
import shutil
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]
PACK = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18"
FAMILY = "academy_modules_v22_25"
CATALOG = json.loads((PACK / "tools/module_expansion_72_catalog.json").read_text(encoding="utf-8"))
VALIDATION = ROOT / "UnityProject/Assets/Game/Resources/Art/ValidationAcademyModules72"


def main() -> None:
    VALIDATION.mkdir(parents=True, exist_ok=True)
    for asset in CATALOG:
        asset_id = asset["asset_id"]
        shutil.copy2(PACK / "normalized" / FAMILY / f"{asset_id}.png", VALIDATION / f"{asset_id}.png")
        manifest_path = PACK / "manifests" / FAMILY / f"{asset_id}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        manifest["status"] = "FORMAL_CANDIDATE"
        manifest["human_review"] = {
            "overall": "PASS", "reviewer": "Product-owner delegated autonomous target-size review",
            "date": "2026-08-25", "silhouette": "PASS", "material": "PASS",
            "perspective": "PASS", "style": "PASS", "application": "PENDING",
            "notes": "Target-size 1x/4x contact passed; Unity 12x9 application contact still required before FORMAL.",
        }
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"staged={len(CATALOG)} path={VALIDATION.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
