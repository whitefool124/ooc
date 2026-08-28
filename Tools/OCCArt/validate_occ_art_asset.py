"""Validate an OCC art manifest against the single machine-readable contract.

The validator checks provenance, role-derived dimensions, hard alpha, palette,
transparent safety borders, semantic colour retention, QA evidence and human
review state. It does not draw, repair or promote assets.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path
from typing import Any

from PIL import Image


SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_CONTRACT = SCRIPT_DIR / "occ_art_contract_v1.json"


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("manifest", type=Path, nargs="?")
    parser.add_argument("--contract", type=Path, default=DEFAULT_CONTRACT)
    parser.add_argument("--root", type=Path)
    parser.add_argument("--report", type=Path)
    parser.add_argument("--audit-contract", action="store_true")
    return parser.parse_args()


def find_root(start: Path) -> Path:
    for candidate in (start, *start.parents):
        if (candidate / "AGENTS.md").is_file() and (candidate / "UnityProject").is_dir():
            return candidate
    raise RuntimeError("Could not locate OCC repository root")


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def repo_path(root: Path, value: str | None) -> Path | None:
    if not value:
        return None
    path = Path(value)
    return path if path.is_absolute() else root / path


def color_family(pixel: tuple[int, int, int], family: str) -> bool:
    red, green, blue = pixel
    if family == "blue":
        return blue >= red + 20 and blue >= green + 5
    if family == "cyan":
        return green >= red + 16 and blue >= red + 16
    if family == "red":
        return red >= green + 20 and red >= blue + 20
    if family == "ochre":
        return red >= green + 8 and green >= blue + 5
    if family == "yellow":
        return red >= blue + 20 and green >= blue + 20
    if family == "green":
        return green >= red + 16 and green >= blue + 16
    if family == "purple":
        return red >= green + 12 and blue >= green + 12
    raise ValueError(f"Unknown color family: {family}")


def expected_size(role: dict[str, Any], logical_cells: Any) -> tuple[int, int]:
    if "delivery_size" in role:
        return tuple(role["delivery_size"])
    if role.get("delivery_formula") == "logical_cells_times_32":
        if (
            not isinstance(logical_cells, list)
            or len(logical_cells) != 2
            or not all(isinstance(value, int) and value > 0 for value in logical_cells)
        ):
            raise ValueError("multi-cell role requires two positive integer logical_cells")
        return logical_cells[0] * 32, logical_cells[1] * 32
    raise ValueError("role has no delivery size rule")


def validate_image(
    image_path: Path,
    role: dict[str, Any],
    logical_cells: Any,
    palette_max: int,
    accents: list[dict[str, Any]],
) -> tuple[list[str], dict[str, Any]]:
    errors: list[str] = []
    image = Image.open(image_path).convert("RGBA")
    expected = expected_size(role, logical_cells)
    if image.size != expected:
        errors.append(f"delivery size {image.size} != expected {expected}")

    pixels = list(image.get_flattened_data())
    alpha_values = {pixel[3] for pixel in pixels}
    if not alpha_values.issubset({0, 255}):
        errors.append("alpha is not hard 0/255")
    if role.get("alpha_policy") == "fully_opaque_hard_alpha" and alpha_values != {255}:
        errors.append("role requires every pixel to be fully opaque")
    opaque = [pixel[:3] for pixel in pixels if pixel[3]]
    colors = set(opaque)
    contract_max = int(role["palette_max"])
    if palette_max > contract_max:
        errors.append(f"manifest palette_max {palette_max} exceeds role maximum {contract_max}")
    if len(colors) > palette_max:
        errors.append(f"visible colors {len(colors)} exceed manifest maximum {palette_max}")

    alpha = image.getchannel("A")
    bounds = alpha.getbbox()
    border = int(role.get("transparent_border_min", 0))
    if not bounds:
        errors.append("delivery image has no opaque pixels")
    elif border:
        margins = (bounds[0], bounds[1], image.width - bounds[2], image.height - bounds[3])
        if min(margins) < border:
            errors.append(f"transparent safety border {margins} is smaller than {border}px")

    accent_counts: dict[str, int] = {}
    for accent in accents:
        family = accent.get("family")
        minimum = accent.get("min_opaque_pixels")
        if not isinstance(family, str) or not isinstance(minimum, int) or minimum <= 0:
            errors.append("required_color_families entries need family and positive min_opaque_pixels")
            continue
        try:
            count = sum(1 for pixel in opaque if color_family(pixel, family))
        except ValueError as exception:
            errors.append(str(exception))
            continue
        accent_counts[family] = count
        if count < minimum:
            errors.append(f"required color family {family} has {count}px, needs at least {minimum}px")

    metrics = {
        "size": list(image.size),
        "bounds": list(bounds) if bounds else None,
        "hard_alpha": alpha_values.issubset({0, 255}),
        "fully_opaque": alpha_values == {255},
        "visible_colors": len(colors),
        "accent_pixels": accent_counts,
    }
    return errors, metrics


def audit_contract(contract: dict[str, Any], root: Path) -> list[str]:
    errors: list[str] = []
    canonical = repo_path(root, contract.get("canonical_document"))
    if canonical is None or not canonical.is_file():
        errors.append("canonical art document is missing")
    for check in contract.get("consistency_checks", []):
        path = repo_path(root, check.get("path"))
        if path is None or not path.is_file():
            errors.append(f"consistency file missing: {check.get('path')}")
            continue
        text = path.read_text(encoding="utf-8-sig")
        for required in check.get("required", []):
            if required not in text:
                errors.append(f"{check['path']} missing required contract text: {required}")
        for forbidden in check.get("forbidden", []):
            if forbidden in text:
                errors.append(f"{check['path']} retains forbidden conflicting text: {forbidden}")
    return errors


def validate_manifest(manifest: dict[str, Any], contract: dict[str, Any], root: Path) -> tuple[list[str], dict[str, Any]]:
    errors = audit_contract(contract, root)
    if manifest.get("schema") != "occ-art-manifest-v1":
        errors.append("manifest schema must be occ-art-manifest-v1")
    if manifest.get("contract_version") != contract.get("version"):
        errors.append("manifest contract_version does not match contract")

    status = manifest.get("status")
    if status not in contract.get("statuses", {}):
        errors.append(f"unknown status: {status}")
    role_name = manifest.get("role")
    role = contract.get("roles", {}).get(role_name)
    if role is None:
        errors.append(f"unknown role: {role_name}")
        return errors, {}

    provenance = manifest.get("provenance", {})
    channel = provenance.get("source_channel")
    if channel not in contract.get("allowed_source_channels", []):
        errors.append(f"source channel is not approved: {channel}")
    source_blob = " ".join(str(provenance.get(key, "")) for key in ("source_channel", "source_descriptor", "source_path")).lower()
    for fragment in contract.get("forbidden_source_fragments", []):
        if fragment.lower() in source_blob:
            errors.append(f"forbidden source route found: {fragment}")

    source_path = repo_path(root, provenance.get("source_path"))
    if source_path is None or not source_path.is_file():
        errors.append("source_path is missing")
    elif provenance.get("source_sha256", "").lower() != sha256(source_path):
        errors.append("source_sha256 does not match source file")

    delivery = manifest.get("delivery", {})
    logical_cells = delivery.get("logical_cells")
    fixed_cells = role.get("logical_cells")
    if isinstance(fixed_cells, list) and logical_cells != fixed_cells:
        errors.append(f"logical_cells {logical_cells} != role requirement {fixed_cells}")
    if fixed_cells == "manifest_required" and not logical_cells:
        errors.append("multi-cell role requires logical_cells")
    if fixed_cells is None and logical_cells is not None:
        errors.append("UI role must use logical_cells: null")

    output_path = repo_path(root, delivery.get("output_path"))
    metrics: dict[str, Any] = {}
    if output_path is None or not output_path.is_file():
        errors.append("delivery output_path is missing")
    else:
        if delivery.get("output_sha256", "").lower() != sha256(output_path):
            errors.append("output_sha256 does not match delivery file")
        image_errors, metrics = validate_image(
            output_path,
            role,
            logical_cells,
            int(delivery.get("palette_max", role["palette_max"])),
            delivery.get("required_color_families", []),
        )
        errors.extend(image_errors)

    if "native_size" in role:
        native_path = repo_path(root, delivery.get("native_output_path"))
        if native_path is None or not native_path.is_file():
            errors.append("material pickup requires native_output_path")
        elif output_path is not None and output_path.is_file():
            native = Image.open(native_path).convert("RGBA")
            output = Image.open(output_path).convert("RGBA")
            native_size = tuple(role["native_size"])
            if native.size != native_size:
                errors.append(f"native size {native.size} != expected {native_size}")
            offset_x, offset_y = role["native_embed_offset"]
            crop = output.crop((offset_x, offset_y, offset_x + native.width, offset_y + native.height))
            if crop.tobytes() != native.tobytes():
                errors.append("24px native pickup is not embedded unscaled at the contract offset")

    application = manifest.get("application", {})
    if not str(application.get("runtime_draw_rect", "")).strip():
        errors.append("application.runtime_draw_rect is required")
    for key in ("default_integer_scale", "minimum_integer_scale"):
        value = application.get(key)
        if not isinstance(value, int) or value <= 0:
            errors.append(f"application.{key} must be a positive integer")

    evidence = manifest.get("evidence", {})
    for key in contract.get("required_evidence", []):
        path = repo_path(root, evidence.get(key))
        if path is None or not path.is_file():
            errors.append(f"required evidence missing: {key}")

    review = manifest.get("human_review", {})
    if status in {"FORMAL_CANDIDATE", "FORMAL"}:
        if review.get("overall") != "PASS":
            errors.append("formal candidate requires human_review.overall PASS")
        if not str(review.get("reviewer", "")).strip() or not str(review.get("date", "")).strip():
            errors.append("formal candidate requires reviewer and date")
        for dimension in contract.get("human_review_dimensions", []):
            if review.get(dimension) != "PASS":
                errors.append(f"human review dimension must PASS: {dimension}")

    if status == "FORMAL":
        unity = manifest.get("unity_import") or {}
        if not str(unity.get("stable_guid", "")).strip():
            errors.append("FORMAL requires stable Unity GUID")
        if unity.get("importer_verified") is not True or unity.get("runtime_verified") is not True:
            errors.append("FORMAL requires importer and runtime verification")

    result = {
        "schema": "occ-art-validation-report-v1",
        "asset_id": manifest.get("asset_id"),
        "role": role_name,
        "status": "PASS" if not errors else "FAIL",
        "metrics": metrics,
        "errors": errors,
    }
    return errors, result


def main() -> int:
    args = arguments()
    contract_path = args.contract.resolve()
    contract = read_json(contract_path)
    root = args.root.resolve() if args.root else find_root(contract_path.parent)

    if args.audit_contract:
        errors = audit_contract(contract, root)
        result = {"schema": "occ-art-contract-audit-v1", "status": "PASS" if not errors else "FAIL", "errors": errors}
    else:
        if args.manifest is None:
            raise SystemExit("manifest is required unless --audit-contract is used")
        manifest = read_json(args.manifest.resolve())
        errors, result = validate_manifest(manifest, contract, root)

    rendered = json.dumps(result, ensure_ascii=False, indent=2)
    print(rendered)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(rendered + "\n", encoding="utf-8")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
