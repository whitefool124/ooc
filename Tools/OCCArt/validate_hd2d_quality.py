"""Fail-closed HD2D evidence gate. Checks records, never judges rendered beauty."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
CONTRACT = ROOT / "Tools/OCCArt/occ_art_contract_v1.json"
REGISTRY = ROOT / "Tools/OCCArt/hd2d_baselines_v1.json"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def review_fingerprint(report):
    """Bind human judgment to the exact technical evidence, not report status."""
    subject = {k: report.get(k) for k in ("policy_version", "contract_sha256", "scene_kind", "baseline_id",
        "changed_files", "inputs", "configuration", "evidence", "runtime", "performance", "blockers")}
    return hashlib.sha256(json.dumps(subject, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()


def bound_file(root, item, errors, label):
    if not isinstance(item, dict) or not isinstance(item.get("path"), str):
        errors.append(f"{label}: missing path/hash binding")
        return None
    path = (root / item["path"]).resolve()
    if not path.is_relative_to(root.resolve()):
        errors.append(f"{label}: file must remain within repository")
        return None
    if not path.is_file() or path.stat().st_size == 0:
        errors.append(f"{label}: missing/empty file {item['path']}")
        return None
    if item.get("sha256") != digest(path):
        errors.append(f"{label}: stale/tampered hash {item['path']}")
    return path


def audit(contract, root):
    errors = []
    policy = contract.get("hd2d_quality_policy", {})
    if policy.get("schema") != "occ-hd2d-quality-policy-v1":
        errors.append("Missing HD2D quality policy")
    for field in ("version", "profiles", "dimensions", "resolutions", "baseline_registry", "validator", "skill", "pixel_density_policy"):
        if not policy.get(field):
            errors.append(f"Policy missing {field}")
    for field in ("baseline_registry", "validator", "skill"):
        if policy.get(field) and not (root / policy[field]).is_file():
            errors.append(f"Broken policy reference: {field}")
    norm = root / contract.get("canonical_document", "")
    if not norm.is_file() or "A.2.4 HD-2D 技术表现与品质门槛" not in norm.read_text(encoding="utf-8-sig"):
        errors.append("Canonical quality section missing")
    return errors


def validate_pixel_density(config, policy, root, input_paths, errors):
    """Fail closed on missing/inconsistent declarations; images still need real review."""
    if not policy.get("pixel_density_policy"):
        return
    path = bound_file(root, config.get("pixel_density_manifest"), errors, "pixel density manifest")
    if not path:
        return
    try:
        manifest = read(path)
    except (OSError, ValueError):
        errors.append("Invalid pixel density manifest JSON")
        return
    if not isinstance(manifest, dict) or manifest.get("schema") != "occ-hd2d-pixel-density-v1":
        errors.append("Unsupported pixel density manifest")
        return
    def positive(value):
        return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value) and value > 0
    target = manifest.get("world_pixel_pitch")
    if not positive(target):
        errors.append("Pixel density target world pitch missing/invalid")
    if manifest.get("all_visible_pixel_assets_reviewed") is not True:
        errors.append("Pixel density inventory completeness not reviewed")
    assets = manifest.get("assets")
    if not isinstance(assets, list) or not assets:
        errors.append("Pixel density inventory missing/empty")
        return
    ids = set()
    for asset in assets:
        if not isinstance(asset, dict):
            errors.append("Invalid pixel density asset record")
            continue
        identity = asset.get("id")
        if not isinstance(identity, str) or not identity.strip() or identity in ids:
            errors.append("Pixel density asset id missing/duplicate")
        ids.add(identity if isinstance(identity, str) else "")
        for field in ("source", "placement"):
            item = asset.get(field)
            bound_file(root, item, errors, f"pixel density {identity} {field}")
            if not isinstance(item, dict) or item.get("path") not in input_paths:
                errors.append(f"Pixel density {identity} {field} not bound as report input")
        if not isinstance(asset.get("density_basis"), str) or not asset["density_basis"].strip():
            errors.append(f"Pixel density {identity} missing source-pitch/scale/UV derivation")
        if asset.get("source_uniformity") != "PASS":
            errors.append(f"Pixel density {identity} source uses mixed/pending pixel pitch")
        pitch = asset.get("effective_world_pitch_xy")
        vfx_exception = asset.get("role") == "pixel_vfx" and policy["pixel_density_policy"].get("vfx_exception", {}).get("allow_different_effect_pixel_pitch") is True
        if vfx_exception:
            if not isinstance(asset.get("world_scale_basis"), str) or not asset["world_scale_basis"].strip():
                errors.append(f"Pixel VFX {identity} missing world-scale derivation")
            bounds = asset.get("actual_motion_bounds")
            if not isinstance(bounds, list) or len(bounds) != 3 or any(not positive(v) for v in bounds):
                errors.append(f"Pixel VFX {identity} motion bounds missing/invalid")
        if not isinstance(pitch, list) or len(pitch) != 2 or any(not positive(v) for v in pitch):
            errors.append(f"Pixel density {identity} effective pitch missing/invalid")
        elif not vfx_exception and positive(target) and any(not math.isclose(v, target, rel_tol=1e-6, abs_tol=0) for v in pitch):
            # Floating-point serialization tolerance, not permission for aesthetic density differences.
            errors.append(f"Pixel density {identity} differs from scene target")
    if not isinstance(manifest.get("reference_asset_id"), str) or manifest["reference_asset_id"] not in ids:
        errors.append("Pixel density reference absent from inventory")
    comparisons = manifest.get("comparisons", [])
    if not isinstance(comparisons, list):
        errors.append("Pixel density comparisons must be a list")
        comparisons = []
    for resolution in policy.get("resolutions", []):
        matches = [x for x in comparisons if isinstance(x, dict) and x.get("resolution") == resolution]
        if len(matches) != 1:
            errors.append(f"Pixel density same-frame comparison missing/duplicate: {resolution}")
            continue
        comparison = matches[0]
        image = bound_file(root, comparison.get("capture"), errors, "pixel density capture")
        if image:
            try:
                with Image.open(image) as capture:
                    if capture.format != "PNG" or list(capture.size) != resolution:
                        errors.append("Pixel density comparison must be actual-resolution PNG")
                    capture.verify()
            except (OSError, ValueError):
                errors.append("Invalid pixel density comparison image")
        for field in ("same_frame_verdict", "projection_verdict", "motion_verdict"):
            if comparison.get(field) != "PASS":
                errors.append(f"Pixel density comparison missing/failed {field}: {resolution}")
        bound_file(root, comparison.get("motion_record"), errors, "pixel density motion record")
        sampled = comparison.get("sampled_asset_ids")
        if not isinstance(sampled, list) or any(not isinstance(x, str) for x in sampled) or set(sampled) != ids or len(sampled) != len(ids):
            errors.append(f"Pixel density comparison does not cover inventory: {resolution}")


def validate_construction(config, policy, root, input_paths, errors):
    """Checks traceability; the bound human review judges the 2D appearance."""
    if not policy.get("construction_policy"):
        return
    path = bound_file(root, config.get("construction_manifest"), errors, "construction manifest")
    if not path:
        return
    try:
        manifest = read(path)
    except (OSError, ValueError):
        errors.append("Invalid construction manifest JSON")
        return
    if not isinstance(manifest, dict) or manifest.get("schema") != "occ-hd2d-construction-v1":
        errors.append("Unsupported construction manifest")
        return
    models = manifest.get("visible_models")
    if not isinstance(models, list):
        errors.append("Construction manifest must declare visible_models (empty for card-only scenes)")
        return
    ids = set()
    for model in models:
        if not isinstance(model, dict):
            errors.append("Invalid visible model record")
            continue
        identity = model.get("id")
        if not isinstance(identity, str) or not identity.strip() or identity in ids:
            errors.append("Missing/duplicate visible model id")
        ids.add(identity if isinstance(identity, str) else "")
        if model.get("role") not in policy["construction_policy"].get("primary_model_scope", []):
            reason = model.get("exception_rationale")
            if not isinstance(reason, str) or not reason.strip():
                errors.append(f"Model outside primary scope lacks justification: {identity}")
        if model.get("style") != "simple_polygonal" or not isinstance(model.get("rationale"), str) or not model["rationale"].strip():
            errors.append(f"Model style/rationale missing: {identity}")
        for key in ("geometry", "material", "pixel_texture"):
            item = model.get(key)
            bound_file(root, item, errors, f"model {identity} {key}")
            if not isinstance(item, dict) or item.get("path") not in input_paths:
                errors.append(f"Model {identity} {key} not bound as report input")


def validate_asset_pixel_density(manifest, contract, root, errors):
    """Formal asset gate shares the scene density checks without requiring full performance approval."""
    application = manifest.get("application", {})
    bindings = application.get("pixel_density_inputs", [])
    if not isinstance(bindings, list):
        bindings = []
        errors.append("Asset pixel density inputs must be a list")
    paths = set()
    for item in bindings:
        bound_file(root, item, errors, "asset pixel density input")
        if isinstance(item, dict) and isinstance(item.get("path"), str):
            paths.add(item["path"])
    binding = application.get("pixel_density_manifest")
    validate_pixel_density({"pixel_density_manifest": binding}, contract["hd2d_quality_policy"], root, paths, errors)
    density_path = bound_file(root, binding, errors, "asset pixel density manifest")
    review = manifest.get("human_review", {})
    if review.get("scene_pixel_size_consistency") != "PASS":
        errors.append("Formal asset scene pixel size consistency not approved")
    if density_path:
        if review.get("reviewed_density_sha256") != digest(density_path):
            errors.append("Formal asset density review stale/missing")
        try:
            record = read(density_path)
            sources = [x.get("source", {}).get("path") for x in record.get("assets", []) if isinstance(x, dict) and isinstance(x.get("source"), dict)]
            if manifest.get("delivery", {}).get("output_path") not in sources:
                errors.append("Formal asset delivery absent from scene density inventory")
        except (OSError, ValueError, AttributeError):
            errors.append("Formal asset density inventory unreadable")


def validate(report, contract, registry, root, contract_path):
    errors = audit(contract, root)
    policy = contract.get("hd2d_quality_policy", {})
    if report.get("schema") != "occ-hd2d-quality-report-v1":
        errors.append("Unsupported report schema")
    if report.get("policy_version") != policy.get("version"):
        errors.append("Policy version changed; revalidate")
    if report.get("contract_sha256") != digest(contract_path):
        errors.append("Contract changed; evidence must be renewed")
    scene = report.get("scene_kind")
    profile = policy.get("profiles", {}).get(scene)
    if not profile:
        return errors + ["Unknown scene_kind"]
    if report.get("status") != "APPROVED_BASELINE":
        errors.append("QA_PENDING/failed candidate cannot satisfy approval gate")
    baselines = registry.get("baselines", {})
    baseline = baselines.get(report.get("baseline_id"), {})
    if baseline.get("scene_kind") != scene or baseline.get("status") != "APPROVED_BASELINE":
        errors.append("Baseline is not approved for this scene")
    bindings = report.get("inputs", [])
    if not isinstance(bindings, list):
        bindings = []
        errors.append("inputs must be a list")
    paths = {x.get("path") for x in bindings if isinstance(x, dict)}
    for path in profile.get("required_inputs", []):
        if path not in paths:
            errors.append(f"Missing required input binding: {path}")
    changed = report.get("changed_files", [])
    if not isinstance(changed, list) or any(not isinstance(x, str) for x in changed):
        changed = []
        errors.append("changed_files must list affected implementation/assets")
    if not changed:
        errors.append("Changed files not declared")
    for path in changed:
        if path not in paths:
            errors.append(f"Changed file not bound: {path}")
    for item in bindings:
        bound_file(root, item, errors, "input")
    config = report.get("configuration", {})
    if config != baseline.get("configuration"):
        errors.append("Configuration differs from approved baseline")
    for key, value in profile.get("locked_parameters", {}).items():
        if config.get(key) != value:
            errors.append(f"Locked profile parameter differs: {key}")
    for key in policy.get("required_configuration", []):
        if key not in config or config[key] is None or config[key] == "" or config[key] == {}:
            errors.append(f"Actual configuration missing: {key}")
    validate_construction(config, policy, root, paths, errors)
    validate_pixel_density(config, policy, root, paths, errors)
    evidence = report.get("evidence", [])
    if not isinstance(evidence, list):
        evidence = []
        errors.append("evidence must be a list")
    seen = set()
    for item in evidence:
        if not isinstance(item, dict):
            errors.append("Invalid evidence record")
            continue
        key = (item.get("case"), str(item.get("resolution")))
        if key in seen:
            errors.append(f"Duplicate evidence case: {key}")
        seen.add(key)
        path = bound_file(root, item, errors, "evidence")
        if item.get("verdict") != "PASS":
            errors.append(f"Evidence case not passed: {key}")
        if path and item.get("case") in profile.get("static_cases", []):
            try:
                with Image.open(path) as capture:
                    if capture.format != "PNG":
                        errors.append(f"Static evidence must be PNG: {key}")
                    if list(capture.size) != item.get("resolution"):
                        errors.append(f"Screenshot dimensions differ from record: {key}")
                    capture.verify()
            except (OSError, ValueError):
                errors.append(f"Invalid/corrupt screenshot: {key}")
        if path and item.get("case") in profile.get("motion_cases", []) and path.suffix.lower() not in (".mp4", ".webm", ".mov", ".gif"):
            errors.append(f"Motion case requires recording: {key}")
    for resolution in policy.get("resolutions", []):
        for case in profile.get("static_cases", []) + profile.get("motion_cases", []):
            if (case, str(resolution)) not in seen:
                errors.append(f"Missing evidence: {case} at {resolution}")
    runtime = report.get("runtime", {})
    for key in ("normal_entry", "console_clear", "interaction_pass", "isolation_pass", "resource_lifetime_pass"):
        if runtime.get(key) is not True:
            errors.append(f"Runtime gate missing/failed: {key}")
    if runtime.get("project_path") != str((root / "UnityProject").resolve()).replace("\\", "/"):
        errors.append("Runtime project is not this main repository")
    bound_file(root, runtime.get("record"), errors, "runtime record")
    performance = report.get("performance", {})
    if not performance.get("device") or not performance.get("quality_tier"):
        errors.append("Performance device/quality tier missing")
    budget_record = baseline.get("performance_budget", {})
    if budget_record.get("device") != performance.get("device") or budget_record.get("quality_tier") != performance.get("quality_tier"):
        errors.append("Performance device/tier differs from approved budget")
    measures = performance.get("measurements", [])
    if not isinstance(measures, list):
        measures = []
        errors.append("performance.measurements must be a list")
    for resolution in policy.get("resolutions", []):
        matches = [m for m in measures if isinstance(m, dict) and m.get("resolution") == resolution]
        if len(matches) != 1:
            errors.append(f"Missing/duplicate performance measurement: {resolution}")
            continue
        measurement = matches[0]
        budget_key = "x".join(map(str, resolution))
        approved_budget = budget_record.get("resolutions", {}).get(budget_key, {})
        for key in ("target_fps", "p95_budget_ms"):
            if approved_budget.get(key) is None or measurement.get(key) != approved_budget.get(key):
                errors.append(f"Performance budget not approved or changed: {key} at {resolution}")
        for key in ("target_fps", "p95_budget_ms", "p95_frame_ms", "cpu_frame_ms", "gpu_frame_ms", "memory_mb", "switch_peak_mb"):
            value = measurement.get(key)
            if not isinstance(value, (float, int)) or isinstance(value, bool) or not 0 < value < float("inf"):
                errors.append(f"Performance missing/invalid: {key}")
        budget = measurement.get("p95_budget_ms")
        actual = measurement.get("p95_frame_ms")
        if isinstance(budget, (int, float)) and isinstance(actual, (int, float)) and actual > budget:
            errors.append("P95 performance budget failed")
    bound_file(root, performance.get("record"), errors, "performance record")
    if report.get("blockers") != []:
        errors.append("Unresolved or undeclared blockers")
    human = report.get("human_review", {})
    if human.get("status") != "APPROVED" or not human.get("reviewer") or not human.get("reviewed_at"):
        errors.append("Human aesthetic approval missing")
    dimensions = human.get("dimensions", {})
    for key in policy.get("dimensions", []):
        if dimensions.get(key) != "PASS":
            errors.append(f"Human dimension unapproved: {key}")
    review_path = bound_file(root, human.get("record"), errors, "human review record")
    if review_path:
        try:
            review = read(review_path)
            if review.get("subject_sha256") != review_fingerprint(report):
                errors.append("Human review refers to stale/different inputs or evidence")
            for key in ("status", "reviewer", "reviewed_at", "dimensions"):
                if review.get(key) != human.get(key):
                    errors.append(f"Human review record differs: {key}")
        except (ValueError, OSError):
            errors.append("Human review record must be JSON binding the exact evidence fingerprint")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path, nargs="?")
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--contract", type=Path, default=CONTRACT)
    parser.add_argument("--registry", type=Path, default=REGISTRY)
    parser.add_argument("--audit-contract", action="store_true")
    parser.add_argument("--also-report", type=Path, action="append", default=[])
    parser.add_argument("--scope", choices=["shared", "desk", "battlefield"], default="shared",
                        help="Shared changes require both approved scene reports; local scope must be explicit")
    args = parser.parse_args()
    try:
        contract = read(args.contract)
        if args.audit_contract:
            errors = audit(contract, args.root)
        else:
            reports = [read(path) for path in [args.report, *args.also_report]]
            errors = []
            for report in reports:
                errors.extend(validate(report, contract, read(args.registry), args.root, args.contract))
            required = {"desk", "battlefield"} if args.scope == "shared" else {args.scope}
            if not required.issubset({r.get("scene_kind") for r in reports}):
                errors.append(f"Change scope {args.scope} requires reports for {sorted(required)}")
    except (OSError, ValueError, TypeError, KeyError, AttributeError) as ex:
        errors = [f"Invalid input: {ex}"]
    print(json.dumps({"ok": not errors, "errors": errors,
        "scope": "record integrity only; visual/human/runtime claims require actual review"}, ensure_ascii=False, indent=2))
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
