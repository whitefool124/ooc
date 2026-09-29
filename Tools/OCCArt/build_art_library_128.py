"""Inventory OCC artwork and pack native, unscaled assets at most 128 px per side.

This is an archival export, not a way to turn raw generation output into game art.
Nothing is resized, cropped, recoloured, or imported into Unity.
"""

from __future__ import annotations

import csv
import hashlib
import io
import json
import os
import re
import sys
import zipfile
from collections import Counter
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "ArtLibrary"
ZIP = DEST / "OCC_ArtLibrary_native128_20260929.zip"
CATALOG = DEST / "OCC_ArtLibrary_catalog_20260929.csv"
SUMMARY = DEST / "README.md"
RASTER = {".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tif", ".tiff"}
VISUAL = RASTER | {".svg"}
PRUNE = {".git", "Library", "Temp", "obj", "node_modules", ".vs", "Reference", "Plugins", "_Recovery"}
EXTERNAL = (
    "ArtSource/sephiria_style_study_2026-09-16/refs/",
    "ArtSource/sephiria_style_study_2026-09-16/sephiria_extract/",
)
NOISE = re.compile(r"(?:^|[_./-])(4x|6x|8x|16x|checker|grayscale|contact|preview|source|raw|qa|evidence)(?:[_./-]|$)", re.I)


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def walk_files():
    for base, dirs, files in os.walk(ROOT):
        dirs[:] = sorted(d for d in dirs if d not in PRUNE and d != "ArtLibrary" and not d.startswith("draft_"))
        for name in sorted(files):
            p = Path(base) / name
            if p.suffix.lower() in VISUAL:
                yield p


def manifest_index():
    found = {}
    for base, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d not in PRUNE and d != "ArtLibrary" and not d.startswith("draft_")]
        for name in files:
            if not name.endswith(".occ-art-manifest-v1.json"):
                continue
            path = Path(base) / name
            try:
                data = json.loads(path.read_text(encoding="utf-8"))
            except (OSError, ValueError):
                continue
            if data.get("schema") != "occ-art-manifest-v1":
                continue
            output = (data.get("delivery") or {}).get("output_path")
            if not isinstance(output, str) or not output or output.startswith("repo/"):
                continue
            output = output.replace("\\", "/").lstrip("./")
            found.setdefault(output, []).append((rel(path), data))
    return found


def pick_manifest(entries):
    if not entries:
        return None
    priority = {"FORMAL": 6, "FORMAL_CANDIDATE": 5, "REVIEW_READY": 4, "QA_PENDING": 3, "PROTOTYPE": 2, "CONCEPT": 1}
    return max(entries, key=lambda x: priority.get(x[1].get("status", ""), 0))


def category(path: str) -> str:
    if path.startswith("UnityProject/Assets/Game/Resources/Art/"):
        return "runtime"
    if path.startswith("Worldbuilding/归档/"):
        return "historical"
    if path.startswith("ArtSource/"):
        return "source"
    if path.startswith(("Artifacts/", "UnityProject/Artifacts/")):
        return "production"
    if path.startswith(("UnityProject/Reports/", "outputs/")):
        return "report"
    return "other"


def scale_decision(path: str, suffix: str, width: int | None, height: int | None, manifest):
    if any(path.startswith(prefix) for prefix in EXTERNAL):
        return "EXCLUDED_EXTERNAL_REFERENCE", "third-party reference material"
    if suffix == ".svg":
        return "CATALOG_ONLY", "vector or diagram; no native raster canvas"
    if width is None or height is None:
        return "CATALOG_ONLY", "image could not be decoded"
    if max(width, height) > 128:
        return "CATALOG_ONLY", "native canvas exceeds 128 px; no geometric reduction permitted"
    if suffix not in {".png", ".gif"}:
        return "CATALOG_ONLY", "not a native PNG/GIF asset"

    if manifest:
        data = manifest[1]
        role = str(data.get("role") or "")
        delivery = data.get("delivery") or {}
        ppu = delivery.get("native_ppu")
        if role == "resource_icon_8":
            return "CATALOG_ONLY", "retired 8 px resource role; current role is 12 px"
        if role in {"permanent_wall_32x32", "battlefield_ground_macro_64", "battlefield_floor_tile_64"}:
            return "CATALOG_ONLY", "historical role conflicts with current 32x64 wall or 32x32 ground contract"
        if role.startswith(("battlefield_", "single_cell_prop", "multi_cell_prop", "tactical_unit", "permanent_wall", "floor_tile", "modular_structure")) and ppu not in (None, 32):
            return "CATALOG_ONLY", f"battlefield role records non-current PPU {ppu}"
        return "PACKAGED_MANIFEST", "manifest output; original bytes and native dimensions preserved"

    if path.startswith("UnityProject/Assets/Game/Resources/Art/Formal") and not NOISE.search(path):
        return "PACKAGED_RUNTIME", "current runtime FormalArt source; original bytes and native dimensions preserved"
    parts = {part.lower() for part in path.split("/")}
    experimental_root = path.startswith(("ArtSource/", "Artifacts/", "UnityProject/Artifacts/", "Worldbuilding/实验性内容/"))
    excluded_stage = bool(parts & {"qa", "evidence", "raw", "sources", "source", "contacts", "before_formal", "before_sources", "refs"})
    native_output = (
        path.endswith("/1x.png")
        or bool(parts & {"normalized", "delivery", "formal_candidates", "03_assets"})
        or (path.startswith("Worldbuilding/实验性内容/") and path.endswith("_32.png"))
    )
    if experimental_root and native_output and not excluded_stage and not NOISE.search(path) and min(width, height) >= 12:
        return "PACKAGED_EXPERIMENTAL", "native-size experiment; role and real-world dimensions remain unverified"
    return "CATALOG_ONLY", "no current manifest or formal runtime scale evidence"


def main():
    DEST.mkdir(exist_ok=True)
    manifests = manifest_index()
    rows = []
    packed = {}
    counts = Counter()
    with zipfile.ZipFile(ZIP, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9, allowZip64=True) as archive:
        for index, path in enumerate(walk_files(), 1):
            source = rel(path)
            suffix = path.suffix.lower()
            width = height = frames = None
            if suffix in RASTER:
                try:
                    with Image.open(path) as img:
                        width, height = img.size
                        frames = getattr(img, "n_frames", 1)
                except (OSError, ValueError):
                    pass
            match = pick_manifest(manifests.get(source))
            decision, reason = scale_decision(source, suffix, width, height, match)
            packed_path = ""
            source_sha = ""
            visible_width = visible_height = ""
            if decision == "PACKAGED_MANIFEST":
                expected = str((match[1].get("delivery") or {}).get("output_sha256") or "").lower()
                actual = hashlib.sha256(path.read_bytes()).hexdigest()
                if expected != actual:
                    if source.startswith("UnityProject/Assets/Game/Resources/Art/Formal"):
                        decision = "PACKAGED_RUNTIME"
                        reason = "formal runtime asset; attached manifest hash is stale"
                    else:
                        decision = "CATALOG_ONLY"
                        reason = "manifest output hash missing or mismatched; provenance unverified"
            if decision.startswith("PACKAGED_"):
                with Image.open(path) as img:
                    rgba = img.convert("RGBA")
                    bounds = rgba.getchannel("A").getbbox()
                    if bounds:
                        visible_width = bounds[2] - bounds[0]
                        visible_height = bounds[3] - bounds[1]
                data = path.read_bytes()
                source_sha = hashlib.sha256(data).hexdigest()
                key = (source_sha, suffix)
                if key in packed:
                    packed_path = packed[key]
                else:
                    group = "experimental" if decision == "PACKAGED_EXPERIMENTAL" else category(source)
                    packed_path = f"assets/{group}/{source_sha[:2]}/{source_sha[:20]}{suffix}"
                    archive.writestr(packed_path, data)
                    packed[key] = packed_path
            row = {
                "source_path": source,
                "category": category(source),
                "source_format": suffix.lstrip("."),
                "source_width_px": width or "",
                "source_height_px": height or "",
                "visible_subject_width_px": visible_width,
                "visible_subject_height_px": visible_height,
                "frames": frames or "",
                "source_bytes": path.stat().st_size,
                "source_sha256_if_packaged": source_sha,
                "manifest_path": match[0] if match else "",
                "asset_id": match[1].get("asset_id", "") if match else "",
                "role": match[1].get("role", "") if match else "",
                "original_status": match[1].get("status", "") if match else "",
                "native_ppu_recorded": (match[1].get("delivery") or {}).get("native_ppu", "") if match else "",
                "disposition": decision,
                "reason": reason,
                "archive_path": packed_path,
            }
            rows.append(row)
            counts[decision] += 1
            if index % 2000 == 0:
                print(f"scanned {index} visuals", flush=True)
        # Historical copies of a verified native file remain represented by the
        # same archived bytes, even if their own folder has no surviving manifest.
        for row in rows:
            if row["archive_path"] or row["source_format"] not in {"png", "gif"}:
                continue
            if not row["source_width_px"] or max(row["source_width_px"], row["source_height_px"]) > 128:
                continue
            if row["disposition"] == "EXCLUDED_EXTERNAL_REFERENCE":
                continue
            source_sha = hashlib.sha256((ROOT / row["source_path"]).read_bytes()).hexdigest()
            archive_path = packed.get((source_sha, "." + row["source_format"]))
            if archive_path:
                counts[row["disposition"]] -= 1
                row["disposition"] = "PACKAGED_IDENTICAL_COPY"
                row["reason"] = "byte-identical to an eligible native asset; shares its archived file"
                row["source_sha256_if_packaged"] = source_sha
                row["archive_path"] = archive_path
                with Image.open(ROOT / row["source_path"]) as img:
                    bounds = img.convert("RGBA").getchannel("A").getbbox()
                    if bounds:
                        row["visible_subject_width_px"] = bounds[2] - bounds[0]
                        row["visible_subject_height_px"] = bounds[3] - bounds[1]
                counts[row["disposition"]] += 1
        catalog_buffer = io.StringIO(newline="")
        writer = csv.DictWriter(catalog_buffer, fieldnames=list(rows[0]), lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)
        catalog_data = catalog_buffer.getvalue().encode("utf-8-sig")
        CATALOG.write_bytes(catalog_data)
        archive.writestr("catalog.csv", catalog_data)
        summary = (
            "# OCC 原生 128 px 美术资产库\n\n"
            "本包逐项保留已具备现行 manifest 或 Unity FormalArt 路径依据、以及明确标为 1×／normalized／delivery／formal_candidates 的实验性原生输出；所有图像画布宽高均不超过 128 px。"
            "没有缩放、裁切、重着色或改变帧。`catalog.csv` 记录扫描到的全部项目视觉文件，包括废案、旧稿与未入包原料。\n\n"
            "战场按 32 PPU（1 px = 3.125 cm，32 px = 1 m）理解。画布尺寸不等于物件的实物尺寸；透明留白不计入主体。"
            "只有记录了实物尺寸和应用接触的物件才能据此断言其现实比例正确。`PACKAGED_EXPERIMENTAL` 是未做物理尺度审核的实验素材，单独存放；历史素材的原状态未提升，包内收录不等于 FORMAL 审美批准。\n\n"
            "大图、放大 QA 图、截图、无比例依据的旧图与外部参考不会通过缩小进入资产目录；详见清单的 `disposition` 与 `reason`。"
            "若要将其变成现行资产，须按 OCC 独立生图原料→像素格诊断→无缩放解码→角色画布→机器及人工审核流程逐项处理。\n\n"
            "扫描排除了 Unity 缓存、插件包、独立第三方参考仓库和临时草稿目录；已知外部参考图仅在清单中登记，不随包复制。\n\n"
            f"扫描文件：{len(rows)}；入包来源：{sum(v for k,v in counts.items() if k.startswith('PACKAGED_'))}；"
            f"独立文件：{len(packed)}。\n"
        )
        SUMMARY.write_text(summary, encoding="utf-8")
        archive.writestr("README.md", summary.encode("utf-8"))
    print(json.dumps({"scanned": len(rows), "decisions": counts, "unique_assets": len(packed), "zip_bytes": ZIP.stat().st_size}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"build failed: {exc}", file=sys.stderr)
        raise
