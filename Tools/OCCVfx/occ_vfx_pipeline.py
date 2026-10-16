"""Offline recipe checks and frozen-reference verification. Does not run Unity."""
from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
from pathlib import Path
import re
import struct
import sys
import uuid

ROOT = Path(__file__).resolve().parents[2]
HOME = ROOT / "Tools/OCCVfx"
REFERENCE = "Tools/OCCVfx/references/sky_furnace_line_weight_20261016.lock.json"
CURRENT_RECIPE = "Tools/OCCVfx/recipes/sky_furnace_line_weight_v4.json"
ROLES = {"magic_seal", "core_magic", "guide", "jet", "absorption", "ember", "shock", "spark", "dust", "ash", "fracture", "rain", "splash"}
PRIMITIVES = {"one_pixel_thick_image", "one_pixel_column", "one_pixel_cube"}


def path(value):
    target = (ROOT / value).resolve()
    if not target.is_relative_to(ROOT):
        raise ValueError("Path leaves main checkout: " + str(value))
    return target


def read(value):
    return json.loads(path(value).read_text(encoding="utf-8-sig"))


def digest(value):
    return hashlib.sha256(path(value).read_bytes()).hexdigest()


def binding(value):
    return {"path": value, "sha256": digest(value)}


def fingerprint(recipe):
    # Evidence and human outcomes refer to a recipe; they must not hash themselves.
    subject = {k: v for k, v in recipe.items() if k not in {"evidence", "status", "review"}}
    return hashlib.sha256(json.dumps(subject, sort_keys=True, ensure_ascii=False, separators=(",", ":"), allow_nan=False).encode()).hexdigest()


def check_bindings(items, errors):
    if not isinstance(items, list) or not items:
        errors.append("Missing bound files")
        return
    for item in items:
        try:
            if not isinstance(item, dict) or not item.get("sha256") or digest(item["path"]) != item["sha256"]:
                errors.append("Missing/stale binding: " + str(item))
        except (KeyError, OSError, ValueError) as exc:
            errors.append("Invalid binding: " + str(exc))


def verify_reference(filename=REFERENCE):
    ref = read(filename)
    errors = []
    if ref.get("schema") != "occ-vfx-reference-lock-v1":
        errors.append("Wrong reference schema")
    check_bindings(ref.get("inputs"), errors)
    check_bindings(ref.get("evidence"), errors)
    snapshots = [{"path": item["snapshot_path"], "sha256": item["sha256"]}
                 for item in ref.get("inputs", []) + ref.get("evidence", []) if item.get("snapshot_path")]
    if len(snapshots) != len(ref.get("inputs", [])) + len(ref.get("evidence", [])):
        errors.append("Frozen reference has incomplete recovery snapshots")
    if snapshots:
        check_bindings(snapshots, errors)
    return {"path": filename, "ok": not errors, "errors": errors,
            "scope": "Frozen local cinematic reference; not formal HD-2D baseline approval"}


def positive(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value) and value > 0


def near(a, b):
    return isinstance(a, (int, float)) and math.isfinite(a) and math.isclose(a, b, abs_tol=1e-6)


def png_size(filename):
    data = path(filename).read_bytes()[:24]
    if len(data) != 24 or data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("Expected PNG: " + filename)
    return struct.unpack(">II", data[16:24])


def check_recipe(recipe, stage="design"):
    errors = []
    if recipe.get("schema") != "occ-vfx-recipe-v1":
        errors.append("Wrong recipe schema")
    if not re.fullmatch(r"[a-z][a-z0-9_-]*", str(recipe.get("id", ""))):
        errors.append("Effect id must be a stable lowercase slug")
    if recipe.get("mode") not in {"shared", "story", "roguelike"}:
        errors.append("Missing mode classification")
    if recipe.get("status") not in {"DRAFT", "QA_PENDING"}:
        errors.append("This tool cannot promote FORMAL or APPROVED_BASELINE")
    ref = verify_reference(recipe.get("reference", REFERENCE))
    errors.extend(ref["errors"])
    capture = recipe.get("capture_directory")
    if not capture:
        errors.append("Missing independent capture directory")
    elif recipe.get("capture_mode") == "reference_readonly":
        if recipe.get("id") != read(recipe.get("reference", REFERENCE)).get("recipe_id"):
            errors.append("Only the frozen sample may use its reference capture directory")
    else:
        folder = path(capture)
        if not folder.is_relative_to(path("Worldbuilding/归档")):
            errors.append("Capture outputs must be in the project archive")
        reference = read(recipe.get("reference", REFERENCE))
        if any(path(item[key]).is_relative_to(folder) for item in reference["evidence"] for key in ("path", "snapshot_path")):
            errors.append("Capture directory overlaps frozen evidence; choose a new run directory")
    if not positive(recipe.get("duration_seconds")):
        errors.append("Set effect duration from its actual usage")
    usage = recipe.get("usage")
    if usage not in {"cinematic_study", "gameplay"}:
        errors.append("Select cinematic_study or gameplay usage")
    if usage == "gameplay":
        settlement = recipe.get("settlement_binding")
        if not isinstance(settlement, dict) or not settlement.get("source_event") or not settlement.get("source"):
            errors.append("Gameplay requires a real settlement/action timing binding; do not inherit 16 seconds")
        else:
            check_bindings([settlement["source"]], errors)
    phases = recipe.get("phases", {})
    for name in ("growth", "charge", "burst", "exit"):
        phase = phases.get(name, {})
        if phase.get("enabled") is False:
            if not phase.get("reason"):
                errors.append(name + ": disabled phase needs its scope reason")
            continue
        span = phase.get("seconds") or []
        duration = recipe.get("duration_seconds") if positive(recipe.get("duration_seconds")) else 0
        if phase.get("enabled") is not True or len(span) != 2 or not all(isinstance(x, (int, float)) and not isinstance(x, bool) and math.isfinite(x) for x in span) or not 0 <= span[0] < span[1] <= duration:
            errors.append(name + ": invalid or missing time window")
    growth = recipe.get("growth", {})
    if phases.get("growth", {}).get("enabled") and (growth.get("method") != "attached_pixel_colonies" or growth.get("unlit_coverage") != 0):
        errors.append("Growth must use attached neighbouring texels with no dark backing")
    if phases.get("growth", {}).get("enabled") and not all(positive(growth.get(k)) for k in ("main_roots", "maximum_branches")):
        errors.append("Specify growth roots and branch count")
    if recipe.get("exit", {}).get("method") != "full_source_geometric_fracture":
        errors.append("Exit must fracture the complete source; opacity/coverage erosion is not this production family")
    if recipe.get("exit", {}).get("particle_alpha") != 1 or recipe.get("exit", {}).get("native_size") != 1:
        errors.append("Exit particles must retain opaque native size; runtime readback is required separately")
    optics = recipe.get("optics", {})
    if optics.get("enabled") and not all(positive(optics.get(k)) for k in ("pixel_emission_gain", "contour_energy", "halo_padding_pixels")):
        errors.append("Specify pixel emission and padded soft inner/outer optical glow separately")
    layers = recipe.get("layers", [])
    if not layers:
        errors.append("No effect layers")
    seen = set()
    for layer in layers:
        name = layer.get("id", "")
        if not name or name in seen:
            errors.append("Missing/duplicate layer id: " + name)
        seen.add(name)
        role, primitive = layer.get("role"), layer.get("primitive")
        if role not in ROLES or primitive not in PRIMITIVES:
            errors.append(name + ": unknown role or primitive")
        pitch, size = layer.get("pixel_pitch_m"), layer.get("world_size_m", [])
        if not positive(pitch) or len(size) != 3 or not all(positive(v) for v in size):
            errors.append(name + ": missing world pixel pitch/dimensions")
            continue
        texture = layer.get("texture")
        if primitive == "one_pixel_thick_image":
            if role not in {"magic_seal", "core_magic"} or not texture or not layer.get("art_manifest"):
                errors.append(name + ": paper is only for seals/core, with source and art manifest")
            else:
                width, height = png_size(texture)
                if not path(layer["art_manifest"]).is_file():
                    errors.append(name + ": art manifest missing")
                if not all(near(a, b) for a, b in zip(size, (width*pitch, height*pitch, pitch))):
                    errors.append(name + ": paper dimensions do not match native source pitch")
        elif texture:
            errors.append(name + ": auxiliary geometry must be texture-free")
        if primitive == "one_pixel_cube" and not all(near(v, pitch) for v in size):
            errors.append(name + ": cube must have three one-pixel edges")
        if primitive == "one_pixel_column" and not (near(size[0], pitch) and near(size[2], pitch)):
            errors.append(name + ": column cross section must be one pixel")
        bounds = layer.get("maximum_motion_span_m", [])
        # Primitive dimensions are local axes; motion spans are world XYZ after rotation.
        if len(bounds) != 3 or not all(positive(v) for v in bounds):
            errors.append(name + ": record whole motion bounds separately from single primitive size")
        if not layer.get("scale_basis"):
            errors.append(name + ": explain scale against actor/skill area")
    if not recipe.get("implementation"):
        errors.append("Bind the actual implementation files")
    else:
        check_bindings(recipe["implementation"], errors)
    if stage == "runtime":
        validate_runtime(recipe, errors)
    return {"id": recipe.get("id"), "stage": stage, "ok": not errors, "errors": errors,
            "human_review": recipe.get("review", {}).get("status", "PENDING"), "formal_promotion": False}


def validate_runtime(recipe, errors):
    evidence = recipe.get("evidence", {})
    if evidence.get("subject_sha256") != fingerprint(recipe):
        errors.append("Runtime evidence belongs to a different recipe; clones cannot inherit it")
    files = evidence.get("files", {})
    for key in ("playback", "geometry", "native_samples", "movie", "comparison_1080", "comparison_540"):
        if key not in files:
            errors.append("Missing runtime evidence: " + key)
    check_bindings(list(files.values()), errors)
    if errors:
        return
    if recipe.get("optics", {}).get("enabled"):
        for key in ("optics_day_before", "optics_day_after", "optics_night_before", "optics_night_after"):
            if key not in files:
                errors.append("Missing optics comparison image: " + key)
        if "optics_readback" not in files:
            errors.append("Missing independent pixel/rim/halo and day/night optics observations")
        else:
            validate_optics(read(files["optics_readback"]["path"]), errors)
    playback, geometry = read(files["playback"]["path"]), read(files["geometry"]["path"])
    if playback.get("status") != "PASS" or not all(playback.get(k) for k in ("normalCompletePlayback", "pauseResume", "reset", "pipelineRestored")) or playback.get("resourceGrowth") != 0:
        errors.append("Playback/cleanup checks failed")
    if not geometry.get("technicalPass") or not geometry.get("auxiliaryTextureFree") or geometry.get("unclassifiedEffectRenderers") != 0:
        errors.append("Runtime geometry/texture contract failed")
    samples = read(files["native_samples"]["path"])
    if evidence.get("adapter") != "sky_furnace_fragment_readback_v1":
        errors.append("Runtime adapter is not implemented; instrument the new effect before claiming PASS")
        return
    limit = recipe.get("effect_motion_limit_m", [])
    if len(limit) != 3 or not all(positive(v) for v in limit):
        errors.append("Missing whole-effect world motion limits")
    elif any(any(actual > bound for actual, bound in zip(s["burst"]["disassemblyPeakSpan"], limit)) for s in samples):
        errors.append("Observed fracture exceeds the recipe world-space motion limits")
    active = [s["burst"]["fragmentReadback"] for s in samples if s["burst"]["fragmentReadback"]["nativeReadbackCount"] > 0]
    if not active or any(r["minAlpha"] != 1 or r["maxAlpha"] != 1 or r["minSize"] != 1 or r["colourOverLifetime"] or r["sizeOverLifetime"] or r["materialAlpha"] != 1 or r["materialEnergy"] <= 0 for r in active):
        errors.append("Native particles fade, resize, or lack emission")
    if len({round(r["materialEnergy"], 6) for r in active}) != 1:
        errors.append("Fracture material energy changes during retirement")
    if any(r["nativeReadbackCount"] != sum(t["liveTexels"] for t in r["tracks"]) for r in active):
        errors.append("Declared source retention differs from native particle count")
    span = evidence.get("retained_source_window", [])
    if len(span) != 2:
        errors.append("Missing retained source observation window")
        return
    trace = [s["burst"]["fragmentReadback"]["tracks"][0] for s in samples if span[0] <= s["time"] <= span[1]]
    if len(trace) < 3 or any(t["liveTexels"] != t["sourceTexels"] or not t["rgba"] for t in trace):
        errors.append("Fracture removes source pixels before visible separation")
    elif len({t["trackedPixel"] for t in trace}) != 1 or len({tuple(t["rgba"]) for t in trace}) != 1 or len({tuple(t["position"]) for t in trace}) < 3 or trace[-1]["maximumMove"] < evidence.get("minimum_visible_move_m", .25):
        errors.append("Same native pixel must retain colour and visibly move across multiple samples")


def validate_optics(records, errors):
    modes = {r["mode"]: r["optics"] for r in records if r["time"] == 6.6}
    required = {"day_before", "day_glow", "night_rain_before", "night_rain_glow"}
    if not required.issubset(modes):
        errors.append("Missing matched day/night before/after optics captures")
        return
    for before, after in (("day_before", "day_glow"), ("night_rain_before", "night_rain_glow")):
        old, new = modes[before], modes[after]
        if not old["layers"] or len(old["layers"]) != len(new["layers"]) or not any(l["sourceVisible"] for l in old["layers"]):
            errors.append("Optics readback omitted visible source layers")
            continue
        for a, b in zip(old["layers"], new["layers"]):
            if a["sourceVisible"] and (b["pixelEmission"] <= a["pixelEmission"] or not b["haloVisible"]):
                errors.append("Visible paper lacks pixel emission gain or empty-space soft halo")
    for key in ("day_glow", "night_rain_glow"):
        optics = modes[key]
        if optics.get("haloModel") != "continuous_gaussian" or optics.get("hardRimEnergy") != 0 or any(l["rimEmission"] != 0 for l in optics["layers"]):
            errors.append("Optical glow must be soft; hard pixel rim is not approved")
    night = modes["night_rain_glow"]
    if not night["nightRain"] or night["rainParticles"] <= 0 or night["wetFloorSmoothness"] <= modes["day_glow"]["wetFloorSmoothness"]:
        errors.append("Night/rain/wet-ground runtime configuration is missing")
    if any(any(l["haloVisible"] for l in r["optics"]["layers"]) for r in records if r["time"] == 0):
        errors.append("Contour halo appears before any source ink exists")


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    new = sub.add_parser("new");new.add_argument("id");new.add_argument("--output", required=True)
    check = sub.add_parser("check");check.add_argument("recipes", nargs="+");check.add_argument("--stage", choices=("design", "runtime"), default="design")
    lock = sub.add_parser("verify-lock");lock.add_argument("--lock", default=REFERENCE)
    args = parser.parse_args()
    try:
        if args.command == "new":
            if not re.fullmatch(r"[a-z][a-z0-9_-]*", args.id):
                raise ValueError("Use a lowercase effect id")
            recipe = copy.deepcopy(read("Tools/OCCVfx/templates/pixel_spell_v1.json"));recipe["id"] = args.id
            recipe["capture_directory"] = "Worldbuilding/归档/VFX/" + args.id + "_" + uuid.uuid4().hex[:10]
            recipe["capture_mode"] = "new_capture"
            target = path(args.output);target.parent.mkdir(parents=True, exist_ok=True)
            with target.open("x", encoding="utf-8") as stream:
                json.dump(recipe, stream, ensure_ascii=False, indent=2, allow_nan=False);stream.write("\n")
            result = {"ok": True, "status": "DRAFT_CREATED", "path": args.output, "runtime_ready": False}
        elif args.command == "verify-lock":
            result = verify_reference(args.lock)
        else:
            results = []
            seen_ids = set()
            for filename in args.recipes:
                try:
                    result = {"path": filename, **check_recipe(read(filename), args.stage)}
                    if result.get("id") in seen_ids:
                        result["errors"].append("Duplicate effect id in batch")
                        result["ok"] = False
                    seen_ids.add(result.get("id"))
                    results.append(result)
                except (OSError, ValueError, KeyError, TypeError) as exc:
                    results.append({"path": filename, "ok": False, "errors": [str(exc)]})
            result = {"ok": all(r["ok"] for r in results), "results": results}
    except (OSError, ValueError, KeyError, TypeError) as exc:
        result = {"ok": False, "errors": [str(exc)]}
    print(json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False))
    return 0 if result["ok"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
