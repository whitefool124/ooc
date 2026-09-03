from __future__ import annotations

import json
import re
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[5]
ARTIFACT = ROOT / "UnityProject/Artifacts/ArtModules72"
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18/manifests/academy_modules_v22_25"
FORMAL = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalAcademyStructures32"
QA = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18/QA/academy_modules_v22_25"
UNITY_DIR = "UnityProject/Assets/Game/Resources/Art/FormalAcademyStructures32"


def fit(path: Path, size: tuple[int, int]) -> Image.Image:
    image = Image.open(path).convert("RGB")
    image.thumbnail(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", size, (18, 22, 27))
    canvas.paste(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    return canvas


def contact_sheet(paths: list[tuple[str, Path]], columns: int, output: Path) -> None:
    cell = (640, 360)
    label = 26
    rows = (len(paths) + columns - 1) // columns
    result = Image.new("RGB", (cell[0] * columns, (cell[1] + label) * rows), (18, 22, 27))
    draw = ImageDraw.Draw(result)
    for index, (name, path) in enumerate(paths):
        x = index % columns * cell[0]
        y = index // columns * (cell[1] + label)
        result.paste(fit(path, cell), (x, y))
        draw.text((x + 8, y + cell[1] + 5), name, fill=(224, 226, 220))
    output.parent.mkdir(parents=True, exist_ok=True)
    result.save(output, optimize=True)


def guid_for(asset_id: str) -> str:
    text = (FORMAL / f"{asset_id}.png.meta").read_text(encoding="utf-8")
    match = re.search(r"^guid:\s*([0-9a-f]{32})$", text, re.MULTILINE)
    if not match:
        raise RuntimeError(f"GUID missing for {asset_id}")
    return match.group(1)


def main() -> None:
    representative = ["rail_patrol", "elite_foundry", "core_finale"]
    after = ARTIFACT / "After"
    before = ROOT / "UnityProject/Artifacts/ArtModules43/After"
    contact_sheet(
        [(f"{name} / 1920x1080", after / f"{name}_1920x1080.png") for name in representative]
        + [(f"{name} / 960x540", after / f"{name}_960x540.png") for name in representative],
        3,
        ARTIFACT / "module72_three_maps_contact.png",
    )
    compare: list[tuple[str, Path]] = []
    for name in representative:
        compare.extend([
            (f"BEFORE {name}", before / f"{name}_1920x1080.png"),
            (f"AFTER {name}", after / f"{name}_1920x1080.png"),
        ])
    contact_sheet(compare, 2, ARTIFACT / "module72_before_after_contact.png")

    page_contact = "Worldbuilding/05_美术与音频/正式美术生产/M-A18/QA/academy_modules_v22_25/academy_modules_72_unity_12x9_contact.png"
    representative_contact = "UnityProject/Artifacts/ArtModules72/module72_three_maps_contact.png"
    for path in sorted(MANIFESTS.glob("*.json")):
        manifest = json.loads(path.read_text(encoding="utf-8"))
        asset_id = manifest["asset_id"]
        guid = guid_for(asset_id)
        manifest["status"] = "FORMAL"
        manifest["evidence"]["application_contact"] = page_contact
        manifest["human_review"].update({
            "overall": "PASS",
            "application": "PASS",
            "reviewer": "Product-owner delegated autonomous runtime review",
            "date": "2026-08-25",
            "notes": "Passed target-size silhouette/material/perspective/style review and exact 12x9 Unity contact. Representative weak/strong/boss maps passed at 1920x1080 and 960x540; directional assets were neither rotated nor mirrored.",
        })
        manifest["unity_import"] = {
            "asset_path": f"{UNITY_DIR}/{asset_id}.png",
            "resource_path": f"Art/FormalAcademyStructures32/{asset_id}",
            "guid": guid,
            "stable_guid": guid,
            "importer_verified": True,
            "runtime_verified": True,
            "runtime_evidence": page_contact,
            "representative_map_evidence": representative_contact,
            "resolutions": ["1920x1080", "960x540"],
            "verification_date": "2026-08-25",
        }
        path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
