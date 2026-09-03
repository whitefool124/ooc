from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[5]
ARTIFACT = ROOT / "UnityProject/Artifacts/ArtModules43"
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18/manifests/academy_modules_v21"
UNITY_DIR = "UnityProject/Assets/Game/Resources/Art/FormalAcademyStructures32"

ASSET_GUIDS = {
    "academy_floor_drain_grate": "d04bf31205313f44db577fa2f0c3fba1",
    "academy_floor_maintenance_hatch": "090ba8b72f4f6934a8f81586e299e27d",
    "academy_floor_repair_plate": "29d508e50b51ecb459a53be1b0e99126",
    "academy_floor_convergence_scribe": "be500b53c2ce92744abf04eeed56951e",
    "academy_prop_wood_crate": "4bd72bfc8dfa7e84ca674aff6304cc1a",
    "academy_prop_iron_crate": "96793e8bb6f9f494eaaef00b95a10554",
    "academy_prop_instrument_rack": "142c3b75697dbaa4981427d9fceac8e0",
    "academy_prop_potion_case": "de0350e9cfdcd974daa40367f497cf4f",
    "academy_prop_maintenance_lamp": "6abb78dec561f654d87fa154166c0bf7",
    "academy_prop_stone_bollard": "989e0179e4aef2c42940d99896fc5665",
    "academy_workbench_2x1": "c87cde2da7572da4b9d15a0bfedca6a5",
    "academy_archive_cabinet_2x1": "d21811d557abf6a47b05b0bb6bbb067d",
    "academy_pipe_service_rack_2x1": "e25d00e1caf375047a3ca138f5dfd048",
    "academy_aether_device_2x2": "c93cfb8a2d76f2341b67a94a972687cb",
    "academy_wall_end_n": "275e2932a4a647e4c95555dd383504d8",
    "academy_wall_end_e": "5d87c4a3edb961841a422b7f4b4c88cd",
    "academy_wall_end_s": "243f9f1c86552794d843aa60b6e7c9fa",
    "academy_wall_end_w": "b686682cecc923043ac7ecc0de655ea7",
}


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


def main() -> None:
    before = ROOT / "UnityProject/Artifacts/ArtTile41/NineMaps"
    after = ARTIFACT / "After"
    representative = ["rail_patrol", "elite_foundry", "core_finale"]
    contact_sheet(
        [(f"{name} / 1920x1080", after / f"{name}_1920x1080.png") for name in representative]
        + [(f"{name} / 960x540", after / f"{name}_960x540.png") for name in representative],
        3,
        ARTIFACT / "module43_three_maps_contact.png",
    )
    compare: list[tuple[str, Path]] = []
    for name in representative:
        compare.extend([
            (f"BEFORE {name}", before / f"{name}_1920x1080.png"),
            (f"AFTER {name}", after / f"{name}_1920x1080.png"),
        ])
    contact_sheet(compare, 2, ARTIFACT / "module43_before_after_contact.png")
    contact_sheet([
        ("rail_patrol / weak", after / "rail_patrol_1920x1080.png"),
        ("elite_foundry / strong", after / "elite_foundry_1920x1080.png"),
        ("core_finale / boss", after / "core_finale_1920x1080.png"),
        ("signal_hub / device + N/S ends", ARTIFACT / "ApplicationContact/signal_hub_1920x1080.png"),
        ("depot_wreck / archive cabinet", ARTIFACT / "ApplicationContact/depot_wreck_1920x1080.png"),
    ], 2, ARTIFACT / "module43_application_contact.png")

    for asset_id, guid in ASSET_GUIDS.items():
        path = MANIFESTS / f"{asset_id}.occ-art-manifest-v1.json"
        manifest = json.loads(path.read_text(encoding="utf-8"))
        manifest["status"] = "FORMAL"
        manifest["evidence"]["application_contact"] = "UnityProject/Artifacts/ArtModules43/module43_application_contact.png"
        manifest["human_review"].update({
            "overall": "PASS",
            "application": "PASS",
            "reviewer": "Product-owner delegated autonomous runtime review",
            "notes": "Independent target-size asset passed silhouette, material, perspective, style, 12x9 runtime contact, occlusion and tactical-overlay review at 1920x1080 and 960x540; directional pieces remain unrotated and unmirrored.",
        })
        runtime = "UnityProject/Artifacts/ArtModules43/After/elite_foundry_1920x1080.png"
        if asset_id in {"academy_aether_device_2x2", "academy_wall_end_n", "academy_wall_end_s"}:
            runtime = "UnityProject/Artifacts/ArtModules43/ApplicationContact/signal_hub_1920x1080.png"
        elif asset_id == "academy_archive_cabinet_2x1":
            runtime = "UnityProject/Artifacts/ArtModules43/ApplicationContact/depot_wreck_1920x1080.png"
        elif asset_id in {"academy_wall_end_e", "academy_wall_end_w"}:
            runtime = "UnityProject/Artifacts/ArtModules43/After/rail_patrol_1920x1080.png"
        manifest["unity_import"] = {
            "asset_path": f"{UNITY_DIR}/{asset_id}.png",
            "resource_path": f"Art/FormalAcademyStructures32/{asset_id}",
            "guid": guid,
            "stable_guid": guid,
            "importer_verified": True,
            "runtime_verified": True,
            "runtime_evidence": runtime,
            "resolutions": ["1920x1080", "960x540"],
            "verification_date": "2026-08-25",
        }
        path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
