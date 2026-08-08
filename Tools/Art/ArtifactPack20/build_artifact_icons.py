#!/usr/bin/env python3
"""Build and audit OCC Artifact Pack 20 icons.

Raw AI images are retained as composition/material references.  The automatic
32px guides are never imported.  Unity receives only the individually cleaned,
palette-locked pixel redraws authored below.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


SIZE = (32, 32)
PALETTE = {
    "outline": (18, 22, 24, 255),
    "coal": (34, 40, 43, 255),
    "iron_dark": (55, 64, 68, 255),
    "iron": (91, 103, 107, 255),
    "iron_light": (145, 155, 154, 255),
    "ivory": (215, 205, 177, 255),
    "brass_dark": (99, 70, 31, 255),
    "brass": (177, 130, 54, 255),
    "brass_light": (229, 188, 93, 255),
    "cyan_dark": (5, 109, 122, 255),
    "cyan": (18, 194, 213, 255),
    "heal": (119, 151, 111, 255),
    "rust": (162, 67, 53, 255),
    "risk": (112, 61, 91, 255),
}

ARTIFACTS = [
    ("F-T01", "demolition_canister", "炎脉封装筒", "横卧陶铜双封筒 / 破障火场"),
    ("G-T01", "aegis_fold", "折盾匣", "折叠甲片匣 / 加盾"),
    ("G-T02", "phase_spindle", "移相线轴", "双叉绕线轴 / 位移"),
    ("G-T03", "binding_frame", "缚位框", "方框四角钉 / 束缚"),
    ("G-T04", "survey_lens", "显迹测镜", "手柄圆镜 / 显迹"),
    ("G-T05", "field_siphon", "以太虹吸泵", "手压泵与弯管 / 回魔反噬"),
    ("G-T06", "mending_lattice", "复元编架", "十字编织夹架 / 治疗掉盾"),
    ("G-T07", "cover_stamp", "掩体压模", "厚矩形压模 / 造掩体"),
    ("G-T08", "breach_wedge", "解构楔", "刻纹楔与锤帽 / 近距破障"),
    ("G-T09", "relay_compass", "导位罗盘", "八向罗盘与牵引针 / 推拉"),
    ("G-T10", "reaction_bell", "截击铃", "护框小铃 / 反应截击"),
    ("G-T11", "hazard_condenser", "险地冷凝器", "盘管冷凝壶 / 清危险地形"),
    ("G-T12", "turn_ledger", "行程簿", "扣带账簿与行动刻线 / AP 调度"),
    ("G-T13", "anchor_brace", "定锚支架", "三脚撑架 / 抗推拉反应"),
    ("G-T14", "prism_regulator", "棱返调节器", "框装三棱镜 / 远程反射"),
    ("G-T15", "decoy_lantern", "诱导灯", "四足百叶灯笼 / 诱饵部署"),
    ("G-T16", "shield_balancer", "护盾均衡阀", "双表盘阀体 / 护盾转移"),
    ("G-T17", "seismic_plumb", "震测铅锤", "悬锤与刻度架 / 范围延迟"),
    ("G-T18", "null_veil", "静默幕", "卷起刻纹帘幕 / 净化代价"),
    ("G-T19", "fortune_seal", "冒险封签", "蜡封签牌与断裂护环 / 风险伤害"),
]


def canvas():
    return Image.new("RGBA", SIZE, (0, 0, 0, 0))


def px(name):
    return PALETTE[name]


def line(draw, points, fill="outline", width=1):
    draw.line(points, fill=px(fill), width=width)


def rect(draw, box, fill, outline="outline", width=1):
    draw.rectangle(box, fill=px(fill), outline=px(outline) if outline else None, width=width)


def poly(draw, points, fill, outline="outline", width=1):
    draw.polygon(points, fill=px(fill))
    if outline:
        draw.line(points + [points[0]], fill=px(outline), width=width, joint="curve")


def ellipse(draw, box, fill, outline="outline", width=1):
    draw.ellipse(box, fill=px(fill), outline=px(outline) if outline else None, width=width)


def draw_demolition_canister(d):
    rect(d, (3, 12, 28, 23), "iron_dark", width=2)
    rect(d, (5, 14, 26, 21), "ivory")
    for x in (7, 15, 23): rect(d, (x, 11, x + 2, 24), "brass", width=1)
    rect(d, (1, 15, 4, 20), "brass_dark"); rect(d, (27, 14, 30, 21), "brass_dark")
    line(d, [(11, 17), (20, 17)], "rust", 2); line(d, [(16, 13), (16, 21)], "rust", 2)
    rect(d, (5, 24, 9, 27), "iron_dark"); rect(d, (22, 24, 26, 27), "iron_dark")
    d.point((16, 17), fill=px("brass_light"))


def draw_aegis_fold(d):
    poly(d, [(4, 11), (13, 8), (13, 25), (4, 28)], "iron")
    poly(d, [(10, 9), (20, 6), (20, 24), (10, 27)], "iron_light")
    poly(d, [(17, 8), (27, 10), (27, 26), (17, 24)], "iron_dark")
    rect(d, (8, 12, 23, 23), "brass_dark", width=2)
    line(d, [(10, 19), (15, 23), (22, 14)], "heal", 2)
    rect(d, (12, 24, 21, 28), "coal")


def draw_phase_spindle(d):
    rect(d, (7, 5, 11, 24), "brass"); rect(d, (21, 5, 25, 24), "brass")
    rect(d, (5, 4, 13, 8), "iron"); rect(d, (19, 4, 27, 8), "iron")
    rect(d, (8, 23, 24, 28), "iron_dark")
    ellipse(d, (10, 10, 22, 22), "coal", width=2); ellipse(d, (13, 13, 19, 19), "cyan_dark")
    line(d, [(11, 11), (21, 20), (12, 22), (20, 10)], "cyan", 1)
    line(d, [(15, 5), (15, 9), (17, 5), (17, 9)], "iron_light", 1)


def draw_binding_frame(d):
    rect(d, (5, 5, 27, 27), "iron_dark", width=2); rect(d, (9, 9, 23, 23), "coal", outline=None)
    for x, y in ((3, 3), (24, 3), (3, 24), (24, 24)):
        rect(d, (x, y, x + 5, y + 5), "brass", width=1)
    line(d, [(8, 8), (13, 13), (19, 13), (24, 8)], "cyan", 1)
    line(d, [(8, 24), (13, 19), (19, 19), (24, 24)], "cyan_dark", 1)
    rect(d, (13, 25, 19, 28), "iron_dark")


def draw_survey_lens(d):
    ellipse(d, (3, 3, 23, 23), "brass", width=2); ellipse(d, (7, 7, 19, 19), "cyan_dark", width=1)
    ellipse(d, (10, 9, 16, 15), "cyan", outline=None)
    line(d, [(20, 20), (29, 27)], "outline", 5); line(d, [(20, 20), (28, 27)], "brass", 3)
    for a in (0, 90, 180, 270):
        x = 13 + round(math.cos(math.radians(a)) * 9); y = 13 + round(math.sin(math.radians(a)) * 9)
        d.point((x, y), fill=px("ivory"))
    rect(d, (26, 26, 30, 29), "iron_dark")


def draw_field_siphon(d):
    rect(d, (5, 10, 17, 26), "iron_dark", width=2); rect(d, (8, 13, 14, 23), "ivory")
    rect(d, (7, 6, 15, 10), "brass"); rect(d, (10, 3, 13, 8), "iron_light")
    line(d, [(12, 4), (20, 4), (20, 7)], "brass", 3)
    line(d, [(16, 18), (24, 18), (27, 22), (25, 27), (20, 28)], "outline", 4)
    line(d, [(16, 18), (23, 18), (26, 22), (24, 26), (20, 27)], "cyan_dark", 2)
    rect(d, (4, 25, 18, 28), "brass_dark")


def draw_mending_lattice(d):
    rect(d, (12, 3, 20, 29), "iron_dark", width=2); rect(d, (3, 11, 29, 21), "iron_dark", width=2)
    rect(d, (10, 9, 22, 23), "ivory", width=1)
    for offset in (-5, 0, 5):
        line(d, [(8 + offset, 12), (19 + offset, 22)], "heal", 2)
        line(d, [(8 + offset, 21), (19 + offset, 10)], "brass_light", 1)
    for x, y in ((12, 3), (12, 25), (3, 13), (25, 13)): rect(d, (x, y, x + 4, y + 4), "brass")


def draw_cover_stamp(d):
    rect(d, (10, 3, 22, 8), "iron"); rect(d, (13, 1, 19, 4), "brass_dark")
    poly(d, [(6, 8), (26, 8), (29, 27), (3, 27)], "iron_dark", width=2)
    rect(d, (7, 12, 25, 25), "brass_dark", width=1)
    for y in (13, 18, 23): line(d, [(8, y), (24, y)], "brass_light", 1)
    for x, y0, y1 in ((13, 13, 18), (19, 13, 18), (10, 18, 23), (16, 18, 23), (22, 18, 23)):
        line(d, [(x, y0), (x, y1)], "brass_light", 1)
    rect(d, (4, 27, 28, 29), "outline", outline=None)


def draw_breach_wedge(d):
    poly(d, [(3, 25), (19, 7), (27, 25)], "iron", width=2)
    rect(d, (14, 4, 25, 10), "brass_dark", width=2)
    line(d, [(7, 23), (15, 17), (14, 13), (20, 9)], "cyan", 2)
    line(d, [(5, 26), (27, 26)], "outline", 3)
    rect(d, (8, 26, 23, 29), "coal", outline=None)


def draw_relay_compass(d):
    poly(d, [(16, 3), (25, 7), (29, 16), (25, 25), (16, 29), (7, 25), (3, 16), (7, 7)], "brass", width=2)
    ellipse(d, (8, 8, 24, 24), "coal", width=1)
    poly(d, [(16, 7), (19, 16), (16, 25), (13, 16)], "iron_light")
    poly(d, [(8, 16), (16, 13), (24, 16), (16, 19)], "cyan_dark")
    ellipse(d, (14, 14, 18, 18), "brass_light")
    line(d, [(16, 25), (16, 29), (20, 29)], "iron_light", 2)


def draw_reaction_bell(d):
    line(d, [(5, 28), (5, 7), (27, 7), (27, 28)], "outline", 4)
    line(d, [(6, 27), (6, 8), (26, 8), (26, 27)], "iron", 2)
    rect(d, (13, 4, 19, 9), "brass_dark")
    poly(d, [(10, 13), (22, 13), (25, 23), (7, 23)], "brass", width=2)
    ellipse(d, (13, 21, 19, 27), "brass_light")
    line(d, [(2, 11), (9, 15)], "cyan", 1); rect(d, (3, 27, 9, 29), "coal"); rect(d, (23, 27, 29, 29), "coal")


def draw_hazard_condenser(d):
    ellipse(d, (8, 10, 25, 26), "brass_dark", width=2); rect(d, (10, 13, 23, 23), "iron_dark")
    rect(d, (13, 7, 20, 12), "ivory"); rect(d, (15, 4, 18, 8), "brass")
    line(d, [(14, 6), (8, 5), (5, 8), (8, 11), (12, 9), (9, 6), (5, 12), (9, 15)], "cyan_dark", 2)
    ellipse(d, (4, 24, 28, 29), "iron", width=2)
    line(d, [(11, 17), (22, 17)], "cyan", 1)


def draw_turn_ledger(d):
    poly(d, [(7, 4), (24, 3), (27, 26), (9, 29)], "coal", width=2)
    poly(d, [(9, 6), (22, 5), (24, 24), (11, 27)], "risk", width=1)
    rect(d, (15, 4, 19, 27), "brass_dark")
    rect(d, (14, 14, 20, 18), "brass", width=1)
    for y in (8, 11, 21, 24): line(d, [(20, y), (23, y)], "ivory", 1)
    line(d, [(9, 28), (27, 25)], "brass_light", 1)


def draw_anchor_brace(d):
    ellipse(d, (11, 4, 21, 14), "brass", width=2); ellipse(d, (14, 7, 18, 11), "cyan_dark")
    rect(d, (14, 12, 18, 20), "iron_dark")
    line(d, [(16, 18), (4, 28)], "outline", 5); line(d, [(16, 18), (28, 28)], "outline", 5); line(d, [(16, 18), (16, 29)], "outline", 5)
    line(d, [(16, 18), (5, 27)], "iron", 2); line(d, [(16, 18), (27, 27)], "iron", 2); line(d, [(16, 18), (16, 28)], "iron", 2)
    for x in (2, 13, 25): rect(d, (x, 27, x + 5, 29), "brass_dark")


def draw_prism_regulator(d):
    ellipse(d, (3, 3, 29, 29), "iron_dark", width=2); ellipse(d, (7, 7, 25, 25), "coal", width=1)
    poly(d, [(16, 7), (25, 23), (7, 23)], "cyan_dark", width=2)
    poly(d, [(16, 9), (21, 21), (11, 21)], "cyan", outline=None)
    rect(d, (14, 2, 18, 8), "brass"); rect(d, (14, 24, 18, 30), "brass")
    rect(d, (2, 14, 8, 18), "brass"); rect(d, (24, 14, 30, 18), "brass")


def draw_decoy_lantern(d):
    rect(d, (8, 7, 24, 25), "brass_dark", width=2); poly(d, [(10, 7), (13, 3), (19, 3), (22, 7)], "iron")
    rect(d, (11, 10, 21, 22), "iron_dark")
    for y in (12, 15, 18, 21): line(d, [(11, y), (21, y - 2)], "iron_light", 1)
    rect(d, (14, 12, 18, 20), "cyan_dark", outline=None)
    line(d, [(10, 24), (6, 29)], "outline", 3); line(d, [(22, 24), (26, 29)], "outline", 3)
    rect(d, (5, 28, 10, 30), "coal"); rect(d, (22, 28, 27, 30), "coal")


def draw_shield_balancer(d):
    ellipse(d, (4, 4, 14, 14), "iron", width=2); ellipse(d, (18, 4, 28, 14), "iron", width=2)
    line(d, [(9, 9), (12, 7)], "heal", 1); line(d, [(23, 9), (20, 12)], "heal", 1)
    rect(d, (7, 12, 25, 24), "brass_dark", width=2); rect(d, (13, 14, 19, 22), "iron_dark")
    line(d, [(2, 18), (8, 18)], "iron_light", 4); line(d, [(24, 18), (30, 18)], "iron_light", 4)
    line(d, [(16, 11), (16, 26)], "cyan", 2); line(d, [(11, 27), (21, 27)], "outline", 4)


def draw_seismic_plumb(d):
    line(d, [(5, 28), (5, 4), (27, 4), (27, 28)], "outline", 4)
    line(d, [(6, 27), (6, 5), (26, 5), (26, 27)], "iron", 2)
    line(d, [(16, 5), (16, 14)], "brass_light", 1)
    poly(d, [(16, 12), (22, 22), (16, 27), (10, 22)], "brass", width=2)
    line(d, [(8, 25), (5, 28), (27, 28), (24, 25)], "cyan_dark", 1)
    d.arc((7, 18, 25, 29), 15, 165, fill=px("cyan"), width=1)


def draw_null_veil(d):
    rect(d, (3, 4, 29, 8), "brass", width=2); ellipse(d, (5, 6, 27, 12), "risk", width=1)
    poly(d, [(7, 9), (25, 9), (25, 27), (20, 25), (16, 29), (12, 25), (7, 27)], "coal", width=2)
    line(d, [(10, 13), (15, 17), (12, 21), (18, 25)], "iron", 1)
    line(d, [(22, 12), (18, 16), (22, 20)], "cyan_dark", 1)
    rect(d, (3, 5, 6, 10), "iron_dark"); rect(d, (26, 5, 29, 10), "iron_dark")


def draw_fortune_seal(d):
    poly(d, [(8, 4), (24, 4), (27, 8), (25, 27), (16, 30), (7, 27), (5, 8)], "brass_dark", width=2)
    rect(d, (9, 7, 23, 25), "ivory", width=1)
    ellipse(d, (10, 9, 22, 21), "rust", width=2); ellipse(d, (13, 12, 19, 18), "risk", outline=None)
    d.arc((7, 6, 25, 24), 35, 155, fill=px("brass_light"), width=2)
    d.arc((7, 6, 25, 24), 205, 320, fill=px("brass_light"), width=2)
    line(d, [(8, 20), (11, 18), (9, 16)], "outline", 2); line(d, [(23, 7), (20, 10), (23, 12)], "outline", 2)
    rect(d, (13, 26, 19, 30), "iron_dark")


DRAWERS = {
    "demolition_canister": draw_demolition_canister,
    "aegis_fold": draw_aegis_fold,
    "phase_spindle": draw_phase_spindle,
    "binding_frame": draw_binding_frame,
    "survey_lens": draw_survey_lens,
    "field_siphon": draw_field_siphon,
    "mending_lattice": draw_mending_lattice,
    "cover_stamp": draw_cover_stamp,
    "breach_wedge": draw_breach_wedge,
    "relay_compass": draw_relay_compass,
    "reaction_bell": draw_reaction_bell,
    "hazard_condenser": draw_hazard_condenser,
    "turn_ledger": draw_turn_ledger,
    "anchor_brace": draw_anchor_brace,
    "prism_regulator": draw_prism_regulator,
    "decoy_lantern": draw_decoy_lantern,
    "shield_balancer": draw_shield_balancer,
    "seismic_plumb": draw_seismic_plumb,
    "null_veil": draw_null_veil,
    "fortune_seal": draw_fortune_seal,
}


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def visible_bounds(im):
    return im.getchannel("A").getbbox()


def connected_components(mask):
    width, height = mask.size
    seen = set()
    sizes = []
    pixels = mask.load()
    for y in range(height):
        for x in range(width):
            if not pixels[x, y] or (x, y) in seen:
                continue
            queue = deque([(x, y)]); seen.add((x, y)); total = 0
            while queue:
                px0, py0 = queue.popleft(); total += 1
                for nx, ny in ((px0 - 1, py0), (px0 + 1, py0), (px0, py0 - 1), (px0, py0 + 1)):
                    if 0 <= nx < width and 0 <= ny < height and pixels[nx, ny] and (nx, ny) not in seen:
                        seen.add((nx, ny)); queue.append((nx, ny))
            sizes.append(total)
    return sorted(sizes, reverse=True)


def outline_ratio(im):
    rgba = im.load(); boundary = []
    for y in range(32):
        for x in range(32):
            if rgba[x, y][3] == 0:
                continue
            if any(nx < 0 or nx >= 32 or ny < 0 or ny >= 32 or rgba[nx, ny][3] == 0
                   for nx, ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1))):
                boundary.append(rgba[x, y][:3])
    if not boundary: return 0.0
    return sum(c == PALETTE["outline"][:3] for c in boundary) / len(boundary)


def audit_icon(path, source_path, artifact_id, display_name, semantic):
    im = Image.open(path).convert("RGBA")
    bounds = visible_bounds(im)
    visible = [(x, y, p) for y in range(32) for x in range(32) if (p := im.getpixel((x, y)))[3]]
    colors = sorted({p[:3] for _, _, p in visible})
    alphas = sorted(set(im.getchannel("A").get_flattened_data()))
    failures = []
    if im.size != SIZE: failures.append("size_not_32")
    if not set(alphas).issubset({0, 255}): failures.append("alpha_not_hard")
    if len(colors) > 16: failures.append("palette_over_16")
    if not bounds: failures.append("empty")
    x0, y0, x1, y1 = bounds or (0, 0, 0, 0)
    cx = sum(x for x, _, _ in visible) / len(visible) if visible else 0
    cy = sum(y for _, y, _ in visible) / len(visible) if visible else 0
    center_dx, center_dy = cx - 15.5, cy - 15.5
    # Icons share a lower visual baseline, so vertical mass may sit lower than
    # the geometric cell center while horizontal balance stays tighter.
    if abs(center_dx) > 2.5 or abs(center_dy) > 4.0: failures.append("center_offset")
    bottom_y = y1 - 1
    if bottom_y < 27 or bottom_y > 30: failures.append("baseline_outside_27_30")
    if x0 == 0 or y0 == 0 or x1 == 32 or y1 == 32: failures.append("border_contact")
    components = connected_components(im.getchannel("A"))
    main_ratio = components[0] / sum(components) if components else 0
    if main_ratio < 0.72: failures.append("fragmented_silhouette")
    ratio = outline_ratio(im)
    if ratio < 0.42: failures.append("outline_coverage")
    return {
        "id": artifact_id,
        "slug": path.stem,
        "display_name": display_name,
        "semantic": semantic,
        "formal_path": path.as_posix(),
        "formal_sha256": sha256(path),
        "source_path": source_path.as_posix(),
        "source_sha256": sha256(source_path),
        "size": list(im.size),
        "visible_pixels": len(visible),
        "visible_colors": len(colors),
        "alpha_values": alphas,
        "bounds_xyxy": [x0, y0, x1, y1],
        "center_xy": [round(cx, 3), round(cy, 3)],
        "center_offset_xy": [round(center_dx, 3), round(center_dy, 3)],
        "baseline_bottom_y": bottom_y,
        "outline_boundary_ratio": round(ratio, 4),
        "connected_components": len(components),
        "main_component_ratio": round(main_ratio, 4),
        "result": "PASS" if not failures else "FAIL",
        "failures": failures,
    }


def audit_unity_importer(png_path):
    meta_path = Path(str(png_path) + ".meta")
    required = {
        "texture_type_sprite": r"^\s*textureType:\s*8\s*$",
        "sprite_mode_single": r"^\s*spriteMode:\s*1\s*$",
        "point_filter": r"^\s*filterMode:\s*0\s*$",
        "wrap_u_clamp": r"^\s*wrapU:\s*1\s*$",
        "wrap_v_clamp": r"^\s*wrapV:\s*1\s*$",
        "mipmap_disabled": r"^\s*enableMipMap:\s*0\s*$",
        "ppu_32": r"^\s*spritePixelsToUnits:\s*32\s*$",
        "alpha_transparency": r"^\s*alphaIsTransparency:\s*1\s*$",
    }
    if not meta_path.exists():
        return {"result": "FAIL", "meta_path": meta_path.as_posix(), "missing": ["meta_file"]}
    text = meta_path.read_text(encoding="utf-8")
    missing = [name for name, pattern in required.items() if not re.search(pattern, text, re.MULTILINE)]
    return {"result": "PASS" if not missing else "FAIL", "meta_path": meta_path.as_posix(), "missing": missing}


def make_guide(raw_path, guide_path):
    im = Image.open(raw_path).convert("RGB")
    samples = [im.getpixel((x, y)) for x, y in ((0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1))]
    bg = tuple(sum(p[i] for p in samples) // 4 for i in range(3))
    rgba = Image.new("RGBA", im.size, (0,0,0,0)); out = rgba.load(); src = im.load()
    for y in range(im.height):
        for x in range(im.width):
            color = src[x, y]
            dist = math.sqrt(sum((color[i] - bg[i]) ** 2 for i in range(3)))
            if dist > 45:
                out[x, y] = (*color, 255)
    bbox = rgba.getchannel("A").getbbox()
    if not bbox:
        raise RuntimeError(f"Could not key source: {raw_path}")
    crop = rgba.crop(bbox)
    crop.thumbnail((26, 26), Image.Resampling.LANCZOS)
    guide = Image.new("RGBA", SIZE, (0,0,0,0))
    guide.alpha_composite(crop, ((32-crop.width)//2, 29-crop.height))
    guide = guide.quantize(colors=15, method=Image.Quantize.FASTOCTREE).convert("RGBA")
    a = guide.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    guide.putalpha(a)
    guide_path.parent.mkdir(parents=True, exist_ok=True); guide.save(guide_path)
    return {"background_rgb": list(bg), "source_size": list(im.size), "guide_sha256": sha256(guide_path)}


def contact_sheet(paths, out_path, columns, scale, label_map=None, background=(28,33,36,255)):
    paths = list(paths); font = ImageFont.load_default(); label_h = 14
    cell_w, cell_h = 32 * scale, 32 * scale + label_h
    rows = (len(paths) + columns - 1) // columns
    sheet = Image.new("RGBA", (columns * cell_w, rows * cell_h), background); draw = ImageDraw.Draw(sheet)
    for i, path in enumerate(paths):
        x, y = (i % columns) * cell_w, (i // columns) * cell_h
        tile = Image.new("RGBA", SIZE, (42,48,52,255)); tile.alpha_composite(Image.open(path).convert("RGBA"))
        sheet.alpha_composite(tile.resize((32*scale, 32*scale), Image.Resampling.NEAREST), (x, y))
        label = (label_map or {}).get(path.stem, path.stem)
        draw.text((x+2, y+32*scale+1), label[:24], fill=(225,225,215,255), font=font)
    out_path.parent.mkdir(parents=True, exist_ok=True); sheet.save(out_path)


def raw_contact_sheet(raw_paths, out_path):
    font = ImageFont.load_default(); columns, cell, label_h = 5, 192, 14
    rows = (len(raw_paths)+columns-1)//columns
    sheet = Image.new("RGB", (columns*cell, rows*(cell+label_h)), (30,35,38)); draw = ImageDraw.Draw(sheet)
    for i, path in enumerate(raw_paths):
        im = Image.open(path).convert("RGB"); im.thumbnail((cell, cell), Image.Resampling.LANCZOS)
        x, y = (i%columns)*cell, (i//columns)*(cell+label_h)
        sheet.paste(im, (x+(cell-im.width)//2, y+(cell-im.height)//2))
        draw.text((x+2,y+cell+1), path.stem[:28], fill=(230,230,220), font=font)
    out_path.parent.mkdir(parents=True, exist_ok=True); sheet.save(out_path)


def write_markdown(report, path):
    lines = [
        "# OCC 20 件法宝图标 QA",
        "",
        "- 方法：每件独立 ImageGen 母图 → 自动 32px 引导稿（不导入）→ 逐件像素级语义重绘 → 有限色板/硬 Alpha/轮廓/中心/基线 QA。",
        f"- 图像 QA：{report['passed']}/{report['count']} PASS；失败 {report['failed']}。",
        f"- Unity Importer QA：{report['importer_passed']}/{report['count']} PASS；失败 {report['importer_failed']}。",
        "- Unity 正式目录：`Assets/Game/Resources/Art/FormalArtifactIcons32`；Importer 由 `FormalArtImportPostprocessor` 统一设为 Sprite / Point / Clamp / PPU32 / no mipmap。",
        "",
        "| ID | 文件 | 中文名 | 色数 | 可见像素 | 重心偏移 | 底线 | 轮廓率 | 结果 |",
        "| --- | --- | --- | ---: | ---: | --- | ---: | ---: | --- |",
    ]
    for item in report["assets"]:
        lines.append(f"| {item['id']} | `{item['slug']}.png` | {item['display_name']} | {item['visible_colors']} | {item['visible_pixels']} | {item['center_offset_xy']} | {item['baseline_bottom_y']} | {item['outline_boundary_ratio']:.2f} | {item['result']} |")
    lines += ["", "## 统一门禁", "", "- 32×32 RGBA；Alpha 仅 0/255；最多 16 个可见色；四角透明且不触边。", "- 重心相对 (15.5,15.5) 的水平偏移不超过 2.5 px、垂直偏移不超过 4 px；图标最低像素位于 Y=27–30。", "- 主连通轮廓占比至少 72%；外轮廓深色覆盖率至少 42%；20 个 SHA-256 唯一。", "- Unity Importer：Sprite / Single / Point / Clamp / PPU32 / alphaIsTransparency / no mipmap。", ""]
    path.write_text("\n".join(lines), encoding="utf-8")


def build(project_root):
    raw_root = project_root / "Worldbuilding/05_美术与音频/正式美术生产/ARTIFACT-PACK-20/RawMothers"
    pack_root = raw_root.parent
    guides_root = pack_root / "Guides32_NotForImport"
    qa_root = pack_root / "QA"
    unity_root = project_root / "UnityProject/Assets/Game/Resources/Art/FormalArtifactIcons32"
    unity_root.mkdir(parents=True, exist_ok=True)
    guide_meta = {}; reports = []; icon_paths = []; raw_paths = []
    for artifact_id, slug, display_name, semantic in ARTIFACTS:
        raw_path = raw_root / f"{artifact_id}_{slug}_raw.png"
        if not raw_path.exists(): raise FileNotFoundError(raw_path)
        raw_paths.append(raw_path)
        guide_path = guides_root / f"{slug}_guide32.png"
        guide_meta[slug] = make_guide(raw_path, guide_path)
        icon = canvas(); DRAWERS[slug](ImageDraw.Draw(icon))
        # Final hard-alpha enforcement after the authored cleanup pass.
        icon.putalpha(icon.getchannel("A").point(lambda value: 255 if value else 0))
        icon_path = unity_root / f"{slug}.png"; icon.save(icon_path); icon_paths.append(icon_path)
        reports.append(audit_icon(icon_path, raw_path, artifact_id, display_name, semantic))
    hashes = [r["formal_sha256"] for r in reports]
    if len(set(hashes)) != len(hashes):
        for item in reports: item["failures"].append("duplicate_sha256"); item["result"] = "FAIL"
    # Unity is open during production and imports through FormalArtImportPostprocessor.
    # Audit the generated .meta files without editing them.
    for item, icon_path in zip(reports, icon_paths):
        item["unity_importer"] = audit_unity_importer(icon_path)
    payload = {
        "schema": "occ.artifact-icons.qa.v1",
        "method": "independent_imagegen_mother_to_auto_guide_to_individual_authored_pixel_cleanup",
        "count": len(reports),
        "passed": sum(r["result"] == "PASS" for r in reports),
        "failed": sum(r["result"] != "PASS" for r in reports),
        "importer_passed": sum(r["unity_importer"]["result"] == "PASS" for r in reports),
        "importer_failed": sum(r["unity_importer"]["result"] != "PASS" for r in reports),
        "palette": {k: "#%02X%02X%02X" % v[:3] for k, v in PALETTE.items()},
        "guide_metadata": guide_meta,
        "assets": reports,
    }
    qa_root.mkdir(parents=True, exist_ok=True)
    generation_manifest = {
        "schema": "occ.artifact-icons.image-generation.v1",
        "generator": "built-in imagegen / gpt-image-2",
        "local_workbench_attempt": {
            "service": "http://127.0.0.1:3000/api/generate-json",
            "configured_model": "gpt-image-2",
            "configured_size": "1024x1024",
            "result": "fallback_used_after_two_generation_requests_failed",
            "failure": "https://yunwu.ai/v1/images/generations ETIMEDOUT; no output file created",
        },
        "shared_prompt": (
            "One isolated first-industrial-revolution aether artifact on a perfectly flat #00ff00 "
            "chroma-key background; orthographic front view, centered complete silhouette, crisp "
            "pixel-art-inspired hard forms, brass/wrought iron/fired ceramic/cloth, visible maintainable "
            "aether conduit; no modern weapon, battery, firearm, explosives pack, character, text, logo, "
            "UI, frame, watermark, shadow or background texture. Raw mother reference only."
        ),
        "assets": [
            {
                "id": artifact_id,
                "slug": slug,
                "display_name": display_name,
                "subject_prompt": semantic,
                "raw_path": (raw_root / f"{artifact_id}_{slug}_raw.png").as_posix(),
                "raw_sha256": sha256(raw_root / f"{artifact_id}_{slug}_raw.png"),
            }
            for artifact_id, slug, display_name, semantic in ARTIFACTS
        ],
    }
    (pack_root / "artifact_imagegen_manifest_v1.json").write_text(json.dumps(generation_manifest, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    (pack_root / "artifact_icon_palette_v1.json").write_text(json.dumps(payload["palette"], ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    (qa_root / "artifact_icons_qa_v1.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    write_markdown(payload, qa_root / "artifact_icons_qa_v1.md")
    labels = {slug: artifact_id for artifact_id, slug, _, _ in ARTIFACTS}
    contact_sheet(icon_paths, qa_root / "artifact_icons_contact_6x.png", 5, 6, labels)
    contact_sheet([guides_root/f"{slug}_guide32.png" for _,slug,_,_ in ARTIFACTS], qa_root / "artifact_guides_contact_6x.png", 5, 6, labels)
    raw_contact_sheet(raw_paths, qa_root / "artifact_raw_mothers_contact.png")
    print(json.dumps({"generated": len(icon_paths), "passed": payload["passed"], "failed": payload["failed"], "importer_passed": payload["importer_passed"], "importer_failed": payload["importer_failed"], "unity_root": str(unity_root), "qa_root": str(qa_root)}, ensure_ascii=False))
    return 0 if payload["failed"] == 0 and payload["importer_failed"] == 0 else 2


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[3])
    args = parser.parse_args()
    raise SystemExit(build(args.project_root.resolve()))


if __name__ == "__main__":
    main()
