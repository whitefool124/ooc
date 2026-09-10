#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PRODUCTION = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A26"
MANIFESTS = PRODUCTION / "manifests"

ELEMENTS = [
    ("fire", "分叉热焰从压缩热核向上张开", "橙红、陶红、暖黄"),
    ("water", "一道受压水束穿过弧形压力环", "深青蓝、灰蓝、象牙"),
    ("wind", "三股气流绕过中央导流片", "浅灰绿、象牙、暗灰"),
    ("earth", "两级承重阶石与底部锚脚", "赭石、砂岩、炭灰"),
    ("lightning", "两枚铜触点之间形成分叉导电路径", "旧金、紫罗兰、象牙"),
    ("ice", "四向霜夹围住不规则冰晶核", "象牙、淡蓝、冷灰"),
    ("light", "机械光阑向外投射三束暖光", "暖白、旧金、炭灰"),
    ("dark", "厚重吸光缺口吞没三枚入射光点", "炭黑、梅紫、暗灰"),
]

RESOURCES = [
    ("action_point", "两段向前推进的节拍楔与一个落点", "冷青、象牙、炭黑"),
    ("aether_load", "承压框架压住一段紫罗兰刻度柱", "紫罗兰、铁灰、象牙"),
    ("charges", "三格由满到空的消耗槽", "灰绿、象牙、炭黑"),
    ("contribution", "三枚学院登记筹码叠成稳定三角", "灰绿、旧金、炭黑"),
    ("core_permit", "两张互锁的黄铜冲孔许可票", "黄铜、象牙、炭黑"),
    ("explored", "展开的路线折页与一个明确终点", "灰蓝、象牙、冷青少量"),
    ("gold", "两枚错叠黄铜硬币，厚边与方形压印", "黄铜、暖黄、棕黑"),
    ("health", "交叉缝合的医务包扎带", "灰绿、象牙、炭黑"),
    ("mana", "有三段刻度的紫罗兰储液容器", "紫罗兰、梅紫、象牙"),
    ("notice", "机械示警片从底座抬起并露出两道警示刻痕", "琥珀、炭黑、象牙"),
    ("operational_aether", "闭合校准回路只有一段冷青正在工作", "铁灰、象牙、冷青少量"),
    ("parts", "垫片、短螺栓与小修补板组成紧凑零件堆", "铁灰、旧金、炭黑"),
    ("risk", "一道锈红裂口劈开厚重危险楔", "锈红、暗陶、象牙"),
    ("shield", "三枚象牙护片向旧金中心合拢", "象牙、旧金、灰绿"),
    ("stage_time", "学院档案进度盘推进到下一枚刻度", "旧金、灰蓝、象牙"),
    ("weight", "宽底铸铁砝码与顶部提环", "铁灰、炭黑、象牙"),
]


def add_asset(group: str, stem: str, subject: str, palette: str) -> dict:
    root = f"UnityProject/Artifacts/ElementResources24/{group}/{stem}"
    final_dir = "FormalElementIcons32" if group == "element" else "FormalResourceIcons32"
    asset = {
        "asset_id": f"ui.{group}.{stem}",
        "group": group,
        "stem": stem,
        "subject": subject,
        "palette": palette,
        "role": "equipment_icon_32",
        "delivery_size": [32, 32],
        "palette_max": 10,
        "source_path": f"{root}/source.png",
        "staging_path": f"UnityProject/Assets/Game/Resources/Art/ValidationElementResources32/{group}/{stem}.png",
        "final_path": f"UnityProject/Assets/Game/Resources/Art/{final_dir}/{stem}.png",
    }
    manifest = {
        "schema": "occ-art-manifest-v1",
        "contract_version": 1,
        "asset_id": asset["asset_id"],
        "role": asset["role"],
        "status": "QA_PENDING",
        "provenance": {
            "source_channel": "codex_builtin_imagegen",
            "source_descriptor": "Independent single 32px element/resource icon; no board slicing",
            "source_path": asset["source_path"],
            "source_sha256": "PENDING_GENERATION",
        },
        "delivery": {
            "output_path": asset["staging_path"],
            "output_sha256": "PENDING_GENERATION",
            "native_output_path": None,
            "logical_cells": None,
            "palette_max": 10,
            "required_color_families": [],
        },
        "application": {
            "runtime_draw_rect": "32x32 element or resource UI slot",
            "default_integer_scale": 1,
            "minimum_integer_scale": 1,
        },
        "evidence": {
            "one_x": f"{root}/1x.png",
            "four_x": f"{root}/4x.png",
            "grayscale": f"{root}/grayscale.png",
            "checker": f"{root}/checker.png",
            "application_contact": "UnityProject/Artifacts/ElementResources24/contacts/PENDING.png",
        },
        "human_review": {
            "overall": "PENDING",
            "reviewer": "",
            "date": "",
            "silhouette": "PENDING",
            "material": "PENDING",
            "perspective": "PENDING",
            "style": "PENDING",
            "application": "PENDING",
            "notes": subject,
        },
        "unity_import": None,
    }
    (MANIFESTS / f"{group}_{stem}.occ-art-manifest-v1.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    return asset


def main() -> None:
    MANIFESTS.mkdir(parents=True, exist_ok=True)
    assets = [add_asset("element", *entry) for entry in ELEMENTS]
    assets += [add_asset("resource", *entry) for entry in RESOURCES]
    if len(assets) != 24:
        raise RuntimeError(len(assets))
    (PRODUCTION / "element_resources_24_catalog.json").write_text(
        json.dumps({"schema": "occ-element-resource-catalog-v1", "count": 24, "assets": assets}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(json.dumps({"count": len(assets), "manifests": len(list(MANIFESTS.glob('*.json')))}))


if __name__ == "__main__":
    main()
