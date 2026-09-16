#!/usr/bin/env python3
"""Generate evidence, QA_PENDING manifests and a summary for a native-32 stability batch."""

from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def checker_contact(image: Image.Image) -> Image.Image:
    preview = image.resize((image.width * 4, image.height * 4), Image.Resampling.NEAREST)
    board = Image.new("RGBA", preview.size, (205, 205, 205, 255))
    for y in range(0, board.height, 16):
        for x in range(0, board.width, 16):
            if (x // 16 + y // 16) % 2:
                board.paste((238, 238, 238, 255), (x, y, x + 16, y + 16))
    board.alpha_composite(preview)
    return board


def application_contact(images: list[Image.Image]) -> Image.Image:
    ground_path = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround/academy_test_ground_theme_slate_surface_32.png"
    ground = Image.open(ground_path).convert("RGBA") if ground_path.is_file() else Image.new("RGBA", (32, 32), (70, 75, 73, 255))
    canvas = Image.new("RGBA", (96, 64 * len(images)), (0, 0, 0, 255))
    for index, image in enumerate(images):
        row_y = index * 64
        for y in range(row_y, row_y + 64, 32):
            for x in range(0, 96, 32):
                canvas.alpha_composite(ground, (x, y))
        canvas.alpha_composite(image, (48 - image.width // 2, row_y + 64 - image.height))
    return canvas.resize((canvas.width * 4, canvas.height * 4), Image.Resampling.NEAREST)


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit("usage: register_native32_stability_batch.py BATCH_SPEC.json")
    spec_path = Path(sys.argv[1]).resolve()
    spec = json.loads(spec_path.read_text(encoding="utf-8-sig"))
    summaries = []
    total_attempts = 0
    first_passes = 0
    approved = spec.get("human_review", {}).get("status") == "PASS"
    contact_images: list[Image.Image] = []

    for item in spec["items"]:
        source = ROOT / item["source"]
        output = ROOT / item["output"]
        qa_path = ROOT / item["qa"]
        checker = ROOT / item["checker"]
        contact = ROOT / spec["application_contact"]
        qa = json.loads(qa_path.read_text(encoding="utf-8-sig"))
        attempt_statuses = []
        for attempt_path in item["attempt_qas"]:
            attempt = json.loads((ROOT / attempt_path).read_text(encoding="utf-8-sig"))
            attempt_statuses.append(attempt["status"])
        total_attempts += len(attempt_statuses)
        first_passes += attempt_statuses[0] == "PASS_LOSSLESS_TARGET"

        image = Image.open(output).convert("RGBA")
        stem = output.with_suffix("")
        four_x = Path(str(stem) + "_4x.png")
        grayscale = Path(str(stem) + "_grayscale.png")
        image.resize((image.width * 4, image.height * 4), Image.Resampling.NEAREST).save(four_x)
        alpha = image.getchannel("A")
        gray = image.convert("L")
        Image.merge("RGBA", (gray, gray, gray, alpha)).save(grayscale)
        checker.parent.mkdir(parents=True, exist_ok=True)
        checker_contact(image).save(checker)
        contact_images.append(image.copy())

        manifest = {
            "schema": "occ-art-manifest-v1",
            "contract_version": 1,
            "asset_id": f"stability_{spec['batch_id']}_{item['id']}",
            "role": item["role"],
            "status": "FORMAL_CANDIDATE" if approved else "QA_PENDING",
            "provenance": {
                "source_channel": "codex_builtin_imagegen",
                "source_descriptor": "Independent Codex built-in image generation for native 32 PPU batch stability testing",
                "source_path": item["source"],
                "source_sha256": digest(source),
            },
            "delivery": {
                "output_path": item["output"],
                "output_sha256": digest(output),
                "native_output_path": None,
                "logical_cells": [1, 1],
                "palette_max": item["palette_max"],
                "required_color_families": [],
            },
            "application": {
                "runtime_draw_rect": f"{image.width}x{image.height} visual canvas bottom-centered over one owning 32px cell",
                "default_integer_scale": 2,
                "minimum_integer_scale": 1,
            },
            "evidence": {
                "one_x": item["output"],
                "four_x": relative(four_x),
                "grayscale": relative(grayscale),
                "checker": item["checker"],
                "application_contact": spec["application_contact"],
            },
            "human_review": {
                "overall": "PASS" if approved else "PENDING",
                "reviewer": spec.get("human_review", {}).get("reviewer", "") if approved else "",
                "date": spec.get("human_review", {}).get("date", "") if approved else "",
                "silhouette": "PASS" if approved else "PENDING",
                "material": "PASS" if approved else "PENDING",
                "perspective": "PASS" if approved else "PENDING",
                "style": "PASS" if approved else "PENDING",
                "application": "PASS" if approved else "PENDING",
                "notes": "User approved the batch appearance and unified production process; Unity import remains pending." if approved else "Batch stability sample; no Unity import or formal promotion.",
            },
            "unity_import": None,
        }
        manifest_path = output.parent / f"{item['id']}_manifest.json"
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        summaries.append({
            "id": item["id"],
            "role": item["role"],
            "selected_round": item["selected_round"],
            "attempt_statuses": attempt_statuses,
            "pitch": qa["inferred_subject_pixel_pitch"],
            "confidence": qa["subject_lattice_confidence"],
            "subject_size": qa["subject_logical_size"],
            "canvas": qa["target_canvas"],
            "palette": qa["decoded_visible_colours_after_cleanup"],
            "manifest": relative(manifest_path),
        })

    contact.parent.mkdir(parents=True, exist_ok=True)
    application_contact(contact_images).save(contact)

    report = {
        "schema": "occ-native32-stability-report-v1",
        "batch_id": spec["batch_id"],
        "items": len(summaries),
        "total_generations": total_attempts,
        "first_pass_technical_success": first_passes,
        "first_pass_rate": first_passes / len(summaries),
        "success_with_one_targeted_retry": sum(s["attempt_statuses"][-1] == "PASS_LOSSLESS_TARGET" for s in summaries),
        "success_with_one_targeted_retry_rate": sum(s["attempt_statuses"][-1] == "PASS_LOSSLESS_TARGET" for s in summaries) / len(summaries),
        "items_detail": summaries,
        "application_contact": spec["application_contact"],
        "human_aesthetic_review": spec.get("human_review", {"status": "PENDING"}),
        "formal_assets_replaced": False,
    }
    report_path = spec_path.with_name("batch_stability_report.json")
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
