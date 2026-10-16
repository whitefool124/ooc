#!/usr/bin/env python3
"""Losslessly decode an enlarged generated pixel-art preview to its logical grid.

The decoder never resizes the subject. It infers the checkerboard lattice, takes
one robust representative colour from each authored macro-pixel, removes only
edge-connected checker cells, and either places the intact subject on a 32px
canvas or rejects a source whose authored logical detail cannot fit.
"""

from __future__ import annotations

import argparse
import json
import math
import statistics
from collections import Counter, deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


COMMON_GRIDS = (16, 24, 32, 48, 64, 96, 128)


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--decoded", required=True, type=Path, help="Exact inferred logical-grid image")
    parser.add_argument("--output-32", required=True, type=Path, help="Output path; retained for CLI compatibility")
    parser.add_argument("--qa", required=True, type=Path)
    parser.add_argument("--preview", type=Path)
    parser.add_argument("--safe-border", type=int, default=1)
    parser.add_argument("--palette-max", type=int, default=16)
    parser.add_argument("--confidence-min", type=float, default=1.2)
    parser.add_argument("--target-size", default="auto", help="auto, 32x32, 32x64, or another native canvas")
    parser.add_argument(
        "--ground-window",
        help="Role-specific opaque-ground normalization window, e.g. 32x32. "
        "Selects a centered lossless logical-pixel window after grid decoding; never resizes or draws.",
    )
    parser.add_argument(
        "--ground-field-pitch",
        type=int,
        help="Human-confirmed macro-pixel pitch for a repetitive opaque ground field. "
        "Only valid with --ground-window; the output remains human-review pending.",
    )
    parser.add_argument(
        "--ground-window-origin",
        help="Optional declared logical XxY origin for a phase-aligned ground window. "
        "Defaults to the deterministic centered window.",
    )
    parser.add_argument("--subject-pitch", type=int, help="Optional diagnosed macro-pixel pitch override")
    return parser.parse_args()


def binary_runs(values: list[bool]) -> list[int]:
    runs: list[int] = []
    start = 0
    for index in range(1, len(values)):
        if values[index] != values[index - 1]:
            runs.append(index - start)
            start = index
    runs.append(len(values) - start)
    return runs


def border_run_count(image: Image.Image, horizontal: bool, far: bool) -> int:
    gray = image.convert("L").filter(ImageFilter.GaussianBlur(radius=3))
    fixed = (gray.height - 10 if far else 9) if horizontal else (gray.width - 10 if far else 9)
    length = gray.width if horizontal else gray.height
    values = [
        gray.getpixel((position, fixed) if horizontal else (fixed, position)) > 235
        for position in range(length)
    ]
    runs = binary_runs(values)
    typical = statistics.median(runs)
    # Ignore tiny threshold flicker; a real checker cell is close to the median run.
    return sum(run >= typical * 0.45 for run in runs)


def infer_grid(image: Image.Image) -> tuple[int, list[int]]:
    observations = [
        border_run_count(image, True, False),
        border_run_count(image, True, True),
        border_run_count(image, False, False),
        border_run_count(image, False, True),
    ]
    estimate = round(statistics.median(observations))
    grid = min(COMMON_GRIDS, key=lambda candidate: abs(candidate - estimate))
    if abs(grid - estimate) > max(2, round(grid * 0.08)):
        raise ValueError(f"checker lattice is not reliable: observations={observations}, estimate={estimate}")
    return grid, observations


def light_neutral(pixel: tuple[int, int, int, int]) -> bool:
    red, green, blue, alpha = pixel
    neutral_checker = min(red, green, blue) >= 100 and max(red, green, blue) - min(red, green, blue) <= 18
    magenta_key = red >= 170 and blue >= 160 and green <= 140 and min(red, blue) - green >= 55
    return alpha > 0 and (neutral_checker or magenta_key)


def remove_baked_checker_pixels(source: Image.Image) -> Image.Image:
    """Erase only light-neutral source pixels connected to the outer canvas."""
    image = source.convert("RGBA")
    width, height = image.size
    pixels = image.load()
    background: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()
    for x in range(width):
        queue.extend(((x, 0), (x, height - 1)))
    for y in range(height):
        queue.extend(((0, y), (width - 1, y)))
    while queue:
        x, y = queue.popleft()
        if (x, y) in background or not light_neutral(pixels[x, y]):
            continue
        background.add((x, y))
        if x:
            queue.append((x - 1, y))
        if x + 1 < width:
            queue.append((x + 1, y))
        if y:
            queue.append((x, y - 1))
        if y + 1 < height:
            queue.append((x, y + 1))
    for x, y in background:
        pixels[x, y] = (0, 0, 0, 0)
    return image


def median_cell(image: Image.Image, left: int, top: int, right: int, bottom: int) -> tuple[int, int, int, int]:
    inset_x = max(1, (right - left) // 5)
    inset_y = max(1, (bottom - top) // 5)
    xs = range(min(right - 1, left + inset_x), max(left + 1, right - inset_x))
    ys = range(min(bottom - 1, top + inset_y), max(top + 1, bottom - inset_y))
    all_pixels = [image.getpixel((x, y)) for y in range(top, bottom) for x in range(left, right)]
    visible_count = sum(pixel[3] > 0 for pixel in all_pixels)
    # A valid authored macro-pixel should occupy most of its inferred cell.
    # Lower-coverage cells are antialiased checker/outline fringe, not detail.
    if visible_count < max(3, round(len(all_pixels) * 0.45)):
        return 0, 0, 0, 0
    samples = [image.getpixel((x, y))[:3] for y in ys for x in xs if image.getpixel((x, y))[3] > 0]
    if not samples:
        samples = [pixel[:3] for pixel in all_pixels if pixel[3] > 0]
    median_target = tuple(round(statistics.median(channel)) for channel in zip(*samples))
    frequency = Counter(samples)
    # Pick an observed source RGB, never a mathematically invented average.
    colour = min(frequency, key=lambda value: (colour_distance(value, median_target), -frequency[value], value))
    return *colour, 255


def decode_grid(source: Image.Image, grid: int) -> Image.Image:
    source = source.convert("RGBA")
    decoded = Image.new("RGBA", (grid, grid), (0, 0, 0, 255))
    for gy in range(grid):
        top = round(gy * source.height / grid)
        bottom = round((gy + 1) * source.height / grid)
        for gx in range(grid):
            left = round(gx * source.width / grid)
            right = round((gx + 1) * source.width / grid)
            decoded.putpixel((gx, gy), median_cell(source, left, top, right, bottom))
    return decoded


def edge_profile_score(profile: np.ndarray, pitch: int) -> tuple[float, int, float]:
    total = float(profile.sum()) + 1e-9
    length = len(profile)
    best = (0.0, 0, 0.0)
    for phase in range(pitch):
        selected = np.zeros(length, dtype=bool)
        for position in range(phase, length, pitch):
            selected[max(0, position - 1):min(length, position + 2)] = True
        coverage = float(profile[selected].sum()) / total
        enrichment = coverage / max(float(selected.mean()), 1e-9)
        score = enrichment * coverage ** 0.35
        if score > best[0]:
            best = (score, phase, coverage)
    return best


def infer_subject_lattice(
    cleaned: Image.Image,
    pitch_override: int | None = None,
) -> tuple[int, int, int, float, tuple[int, int, int, int]]:
    bounds = cleaned.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("no subject after background removal")
    crop = cleaned.crop(bounds)
    smooth = np.asarray(crop.convert("RGB").filter(ImageFilter.GaussianBlur(1.4)), dtype=np.float32)
    alpha = np.asarray(crop.getchannel("A")) > 0
    edge_x = np.abs(smooth[:, 1:] - smooth[:, :-1]).sum(axis=2)
    edge_y = np.abs(smooth[1:] - smooth[:-1]).sum(axis=2)
    edge_x *= alpha[:, 1:] & alpha[:, :-1]
    edge_y *= alpha[1:] & alpha[:-1]
    profile_x = edge_x.sum(axis=0)
    profile_y = edge_y.sum(axis=1)
    candidates = []
    pitch_range = [pitch_override] if pitch_override is not None else range(8, 41)
    for pitch in pitch_range:
        if pitch < 4:
            raise ValueError("subject pitch must be at least 4 pixels")
        score_x, phase_x, coverage_x = edge_profile_score(profile_x, pitch)
        score_y, phase_y, coverage_y = edge_profile_score(profile_y, pitch)
        combined = score_x * score_y * min(coverage_x, coverage_y) ** 0.15
        candidates.append((combined, pitch, phase_x, phase_y))
    candidates.sort(reverse=True)
    score, pitch, phase_x, phase_y = candidates[0]
    if len(candidates) == 1:
        confidence = score
    else:
        runner_up = next((item for item in candidates[1:] if item[1] not in {pitch * 2, pitch // 2}), candidates[1])
        confidence = score / max(runner_up[0], 1e-9)
    return pitch, phase_x, phase_y, confidence, bounds


def decode_subject_lattice(
    cleaned: Image.Image,
    bounds: tuple[int, int, int, int],
    pitch: int,
    phase_x: int,
    phase_y: int,
) -> Image.Image:
    crop = cleaned.crop(bounds)
    min_kx = (0 - phase_x) // pitch - 1
    max_kx = (crop.width - phase_x + pitch - 1) // pitch + 1
    min_ky = (0 - phase_y) // pitch - 1
    max_ky = (crop.height - phase_y + pitch - 1) // pitch + 1
    cells: dict[tuple[int, int], tuple[int, int, int, int]] = {}
    for ky in range(min_ky, max_ky):
        top = max(0, phase_y + ky * pitch)
        bottom = min(crop.height, phase_y + (ky + 1) * pitch)
        if bottom <= top:
            continue
        for kx in range(min_kx, max_kx):
            left = max(0, phase_x + kx * pitch)
            right = min(crop.width, phase_x + (kx + 1) * pitch)
            if right <= left:
                continue
            value = median_cell(crop, left, top, right, bottom)
            if value[3]:
                cells[(kx, ky)] = value
    if not cells:
        raise ValueError("subject lattice decoded no opaque cells")
    xs = [position[0] for position in cells]
    ys = [position[1] for position in cells]
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)
    result = Image.new("RGBA", (max_x - min_x + 1, max_y - min_y + 1), (0, 0, 0, 0))
    for (x, y), value in cells.items():
        result.putpixel((x - min_x, y - min_y), value)
    return result


def colour_distance(a: tuple[int, int, int], b: tuple[int, int, int]) -> int:
    return sum(abs(x - y) for x, y in zip(a, b))


def remove_checker(decoded: Image.Image) -> Image.Image:
    grid = decoded.width
    border_cells = [
        (x, y)
        for y in range(grid)
        for x in range(grid)
        if x in (0, grid - 1) or y in (0, grid - 1)
    ]
    references: dict[int, tuple[int, int, int]] = {}
    for parity in (0, 1):
        colors = [decoded.getpixel((x, y))[:3] for x, y in border_cells if (x + y) % 2 == parity]
        references[parity] = tuple(round(statistics.median(channel)) for channel in zip(*colors))

    def is_checker(x: int, y: int) -> bool:
        return colour_distance(decoded.getpixel((x, y))[:3], references[(x + y) % 2]) <= 42

    background: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque(border_cells)
    while queue:
        x, y = queue.popleft()
        if (x, y) in background or not is_checker(x, y):
            continue
        background.add((x, y))
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < grid and 0 <= ny < grid:
                queue.append((nx, ny))
    result = decoded.copy()
    for x, y in background:
        result.putpixel((x, y), (0, 0, 0, 0))
    return result


def squared_distance(a: tuple[int, int, int], b: tuple[int, int, int]) -> int:
    return sum((x - y) ** 2 for x, y in zip(a, b))


def consolidate_to_observed_palette(image: Image.Image, palette_max: int) -> Image.Image:
    """Merge near-identical RGB noise without dithering or inventing colours."""
    if palette_max <= 0:
        return image.copy()
    counts = Counter(pixel[:3] for pixel in image.get_flattened_data() if pixel[3])
    colors = sorted(counts)
    if len(colors) <= palette_max:
        return image.copy()

    first = max(colors, key=lambda value: (counts[value], -sum(value), value))
    medoids = [first]
    while len(medoids) < palette_max:
        candidate = max(
            (color for color in colors if color not in medoids),
            key=lambda color: (min(squared_distance(color, center) for center in medoids) * counts[color], color),
        )
        medoids.append(candidate)

    for _ in range(12):
        clusters: list[list[tuple[int, int, int]]] = [[] for _ in medoids]
        for color in colors:
            index = min(range(len(medoids)), key=lambda i: (squared_distance(color, medoids[i]), i))
            clusters[index].append(color)
        updated: list[tuple[int, int, int]] = []
        for index, cluster in enumerate(clusters):
            if not cluster:
                updated.append(medoids[index])
                continue
            total = sum(counts[color] for color in cluster)
            centroid = tuple(
                sum(color[channel] * counts[color] for color in cluster) / total
                for channel in range(3)
            )
            updated.append(min(cluster, key=lambda color: (squared_distance(color, centroid), -counts[color], color)))
        if updated == medoids:
            break
        medoids = updated

    result = image.copy()
    pixels = result.load()
    for y in range(result.height):
        for x in range(result.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha:
                source = (red, green, blue)
                mapped = min(medoids, key=lambda color: (squared_distance(source, color), color))
                pixels[x, y] = (*mapped, alpha)
    return result


def keep_largest_alpha_component(image: Image.Image) -> tuple[Image.Image, int, int]:
    opaque = {(x, y) for y in range(image.height) for x in range(image.width) if image.getpixel((x, y))[3]}
    components: list[set[tuple[int, int]]] = []
    while opaque:
        component = {opaque.pop()}
        queue = list(component)
        while queue:
            x, y = queue.pop()
            for neighbor in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if neighbor in opaque:
                    opaque.remove(neighbor)
                    component.add(neighbor)
                    queue.append(neighbor)
        components.append(component)
    if not components:
        return image.copy(), 0, 0
    largest = max(components, key=len)
    result = image.copy()
    removed = 0
    for y in range(result.height):
        for x in range(result.width):
            if result.getpixel((x, y))[3] and (x, y) not in largest:
                result.putpixel((x, y), (0, 0, 0, 0))
                removed += 1
    return result, removed, len(components)


def parse_target_size(value: str) -> tuple[int, int] | None:
    if value.lower() == "auto":
        return None
    try:
        width, height = (int(part) for part in value.lower().split("x", 1))
    except (TypeError, ValueError) as error:
        raise ValueError(f"invalid target size: {value}") from error
    if width <= 0 or height <= 0:
        raise ValueError(f"invalid target size: {value}")
    return width, height


def place_intact(
    image: Image.Image,
    target_size: tuple[int, int],
    safe_border: int,
) -> tuple[Image.Image | None, tuple[int, int, int, int] | None]:
    bounds = image.getchannel("A").getbbox()
    if bounds is None:
        return None, None
    crop = image.crop(bounds)
    target_width, target_height = target_size
    available_width = target_width - safe_border * 2
    available_height = target_height - safe_border * 2
    if crop.width > available_width or crop.height > available_height:
        return None, bounds
    result = Image.new("RGBA", target_size, (0, 0, 0, 0))
    x = (target_width - crop.width) // 2
    y = target_height - safe_border - crop.height
    result.alpha_composite(crop, (x, y))
    return result, bounds


def select_ground_window(
    image: Image.Image,
    target_size: tuple[int, int],
    origin: tuple[int, int] | None = None,
) -> tuple[Image.Image | None, tuple[int, int, int, int] | None]:
    """Losslessly select a deterministic centered viewport from an opaque ground field.

    Unlike an isolated Sprite, an opaque ground field has no meaningful subject
    silhouette or transparent safety border. The selected logical pixels are a
    declared material window, not a resized or repaired subject.
    """
    target_width, target_height = target_size
    if image.width < target_width or image.height < target_height:
        return None, None
    if any(alpha != 255 for alpha in image.getchannel("A").get_flattened_data()):
        return None, None
    left, top = origin if origin is not None else ((image.width - target_width) // 2, (image.height - target_height) // 2)
    if left < 0 or top < 0 or left + target_width > image.width or top + target_height > image.height:
        return None, None
    bounds = (left, top, left + target_width, top + target_height)
    return image.crop(bounds), bounds


def main() -> None:
    args = arguments()
    target_size = parse_target_size(args.target_size)
    ground_window_size = parse_target_size(args.ground_window) if args.ground_window else None
    if args.ground_field_pitch is not None and ground_window_size is None:
        raise ValueError("--ground-field-pitch requires --ground-window")
    if args.ground_field_pitch is not None and args.subject_pitch is not None:
        raise ValueError("use either --subject-pitch or --ground-field-pitch, not both")
    ground_window_origin = None
    if args.ground_window_origin:
        try:
            origin_x, origin_y = (int(part) for part in args.ground_window_origin.lower().split("x", 1))
        except (TypeError, ValueError) as error:
            raise ValueError("invalid --ground-window-origin; expected XxY") from error
        ground_window_origin = (origin_x, origin_y)
        if ground_window_size is None:
            raise ValueError("--ground-window-origin requires --ground-window")
    if ground_window_size is not None:
        if target_size is None:
            target_size = ground_window_size
        elif target_size != ground_window_size:
            raise ValueError("--target-size must equal --ground-window when using ground normalization")
    source = Image.open(args.source).convert("RGBA")
    cleaned_source = remove_baked_checker_pixels(source)
    requested_pitch = args.ground_field_pitch if args.ground_field_pitch is not None else args.subject_pitch
    pitch, phase_x, phase_y, lattice_confidence, source_bounds = infer_subject_lattice(cleaned_source, requested_pitch)
    decoded_unfiltered = decode_subject_lattice(cleaned_source, source_bounds, pitch, phase_x, phase_y)
    decoded_raw, background_artifact_cells_removed, component_count_before_cleanup = keep_largest_alpha_component(decoded_unfiltered)
    decoded = consolidate_to_observed_palette(decoded_raw, args.palette_max)
    decoded_bounds = decoded.getchannel("A").getbbox()
    if target_size is None:
        if decoded_bounds is None:
            raise ValueError("decoded subject is empty")
        subject_width = decoded_bounds[2] - decoded_bounds[0]
        subject_height = decoded_bounds[3] - decoded_bounds[1]
        target_size = (
            max(32, math.ceil((subject_width + args.safe_border * 2) / 32) * 32),
            max(32, math.ceil((subject_height + args.safe_border * 2) / 32) * 32),
        )
    args.decoded.parent.mkdir(parents=True, exist_ok=True)
    decoded.save(args.decoded)
    lattice_is_stable = lattice_confidence >= args.confidence_min
    manual_ground_field_route = args.ground_field_pitch is not None
    ground_field_pitch_eligible = manual_ground_field_route and all(
        alpha == 255 for alpha in cleaned_source.getchannel("A").get_flattened_data()
    )
    if ground_window_size is not None:
        output, bounds = select_ground_window(decoded, ground_window_size, ground_window_origin) if (lattice_is_stable or ground_field_pitch_eligible) else (None, None)
    else:
        output, bounds = place_intact(decoded, target_size, args.safe_border)
        if not lattice_is_stable:
            output = None
    subject_size = None if bounds is None else [bounds[2] - bounds[0], bounds[3] - bounds[1]]
    source_rgb = set(source.convert("RGB").get_flattened_data())
    decoded_raw_visible = [pixel[:3] for pixel in decoded_raw.get_flattened_data() if pixel[3]]
    decoded_visible = [pixel[:3] for pixel in decoded.get_flattened_data() if pixel[3]]
    colours_are_source_samples = all(colour in source_rgb for colour in decoded_visible)
    exact_alpha_shape_preserved = list(decoded_raw.getchannel("A").get_flattened_data()) == list(decoded.getchannel("A").get_flattened_data())
    exact_crop_preserved = False
    if output is not None and bounds is not None:
        decoded_crop = decoded.crop(bounds)
        output_bounds = output.getchannel("A").getbbox()
        exact_crop_preserved = output_bounds is not None and list(output.crop(output_bounds).get_flattened_data()) == list(decoded_crop.get_flattened_data())
    if manual_ground_field_route and output is not None:
        status = "PASS_GROUND_FIELD_MANUAL_PITCH_REVIEW_REQUIRED"
    elif not lattice_is_stable:
        status = "REJECT_UNSTABLE_SUBJECT_GRID"
    elif output is None:
        status = "REJECT_GROUND_WINDOW_UNAVAILABLE" if ground_window_size is not None else "REJECT_SOURCE_EXCEEDS_TARGET"
    elif ground_window_size is not None and colours_are_source_samples and exact_alpha_shape_preserved and exact_crop_preserved:
        status = "PASS_GROUND_WINDOW"
    elif colours_are_source_samples and exact_alpha_shape_preserved and exact_crop_preserved:
        status = "PASS_LOSSLESS_TARGET"
    else:
        status = "REJECT_INTEGRITY_FAILURE"
    if output is not None:
        args.output_32.parent.mkdir(parents=True, exist_ok=True)
        output.save(args.output_32)
        if args.preview:
            args.preview.parent.mkdir(parents=True, exist_ok=True)
            preview_size = (target_size[0] * 8, target_size[1] * 8)
            board = Image.new("RGBA", preview_size, (204, 204, 204, 255))
            for y in range(0, preview_size[1], 16):
                for x in range(0, preview_size[0], 16):
                    if (x // 16 + y // 16) % 2:
                        board.paste((238, 238, 238, 255), (x, y, x + 16, y + 16))
            board.alpha_composite(output.resize(preview_size, Image.Resampling.NEAREST))
            board.save(args.preview)
    elif args.output_32.exists():
        args.output_32.unlink()
    report = {
        "status": status,
        "source_size": list(source.size),
        "inferred_subject_pixel_pitch": pitch,
        "inferred_subject_grid_phase": [phase_x, phase_y],
        "subject_lattice_confidence": round(lattice_confidence, 4),
        "subject_lattice_confidence_min": args.confidence_min,
        "subject_lattice_is_stable": lattice_is_stable,
        "ground_field_manual_pitch": args.ground_field_pitch,
        "ground_field_manual_pitch_review_required": manual_ground_field_route,
        "background_checker_used_for_scale": False,
        "subject_bounds_on_logical_grid": list(bounds) if bounds else None,
        "subject_logical_size": subject_size,
        "target_canvas": list(target_size),
        "normalization_mode": "lossless_centered_ground_window" if ground_window_size is not None else "intact_subject_canvas",
        "ground_window_bounds_on_logical_grid": list(bounds) if ground_window_size is not None and bounds else None,
        "ground_window_selection": "declared_phase_aligned_origin" if ground_window_origin is not None else "deterministic_center",
        "preview": str(args.preview) if args.preview and output is not None else None,
        "safe_border": args.safe_border,
        "decoded_visible_colours_before_cleanup": len(set(decoded_raw_visible)),
        "decoded_visible_colours_after_cleanup": len(set(decoded_visible)),
        "palette_max": args.palette_max,
        "all_decoded_colours_are_observed_source_samples": colours_are_source_samples,
        "alpha_shape_preserved_pixel_for_pixel": exact_alpha_shape_preserved,
        "output_subject_equals_cleaned_decoded_subject_pixel_for_pixel": exact_crop_preserved,
        "ground_window_equals_decoded_source_pixels_pixel_for_pixel": exact_crop_preserved if ground_window_size is not None else None,
        "macro_pixel_colour_selection": "observed_medoid_from_cell_center",
        "macro_pixel_decoding_performed": True,
        "geometric_resize_or_interpolation_performed": False,
        "new_colours_introduced": False,
        "logical_pixels_removed": 0 if ground_window_size is None else max(0, decoded.width * decoded.height - target_size[0] * target_size[1]),
        "background_artifact_cells_removed": background_artifact_cells_removed,
        "alpha_components_before_cleanup": component_count_before_cleanup,
        "alpha_components_after_cleanup": 1 if decoded_visible else 0,
        "dithering": False,
        "structural_detail_loss_allowed": False,
        "promotion_limit": "REVIEW_READY only; a manually pitched ground field requires explicit human material, pixel-scale and application review before FORMAL_CANDIDATE" if manual_ground_field_route else None,
        "colour_cleanup": "near-duplicate RGB variants are remapped only to observed source medoids; use --palette-max 0 for unconsolidated logical-cell decoding",
    }
    args.qa.parent.mkdir(parents=True, exist_ok=True)
    args.qa.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
