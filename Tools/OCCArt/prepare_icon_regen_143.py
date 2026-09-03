#!/usr/bin/env python3
"""Prepare the M-A19 icon catalog and one QA_PENDING manifest per source asset."""

from __future__ import annotations

import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "UnityProject/Assets/Game/Resources/Art"
OUT = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A19"
MANIFESTS = OUT / "manifests"

RUNTIME_SUBJECTS = {
    "aether_surge": "青色以太压力表向上冲顶，带三段刻度与过载火花",
    "anchor_seal": "咬合地面的三足定锚印，粗重下压轮廓",
    "arc_bolt": "短促折线电弧贯穿一枚导电螺栓",
    "armor_solvent": "滴管把腐蚀液滴在开裂甲片上",
    "barrier_charge": "蓄能电池向半圆护盾注入青白能量",
    "bastion_pulse": "厚重城垛核心向外发出一圈防护脉冲",
    "breach_shot": "工业枪弹击穿开裂装甲板",
    "cinder_sweep": "宽阔近战横扫弧携带低伏余烬",
    "cryo_pulse": "冷凝线圈放出六角霜晶脉冲",
    "damping_field": "同心抑制环压住中央以太波形",
    "demolition_charge": "带警示扣与旋压阀的学院爆破药包",
    "ember_lance": "细长熔红枪尖带聚焦尾焰",
    "field_repair": "扳手与闭合裂缝的青色维修弧",
    "fire_bolt": "紧凑火弹从短导轨射出，清晰向右上推进",
    "frost_bind": "冰冷束带锁住一只机械靴",
    "hammer_pulse": "工坊锤砸下并产生低矮冲击环",
    "mana_siphon": "虹吸泵从暗色容器抽出青色以太流",
    "overload_needle": "细针刺穿红区仪表盘并迸出火花",
    "phase_step": "两枚错位足印由短程相位轨迹连接",
    "prism_arc": "棱镜将一道青白射线折成三段弧光",
    "rail_burst": "加速导轨发射一枚高亮穿甲弹芯",
    "regenerative_seal": "维修封印闭合一处结构裂纹，非宗教符号",
    "rescue_beam": "定向救援灯向友军轮廓投射稳定青白光束",
    "searing_mark": "烙铁在装甲表面压出灼热靶印",
    "shield_converter": "双向换流阀把能量导入盾片",
    "tether_arc": "两枚绝缘端子间绷紧一条牵引电弧",
    "thermal_purge": "散热阀喷出白汽并清除红色热警示",
}

ITEM_SUBJECTS = {
    "aether_wand": "学院制以太导杖，黄铜套环、青色储能芯和绝缘握柄",
    "arcane_wand": "旧式测术短杖，蓝钢杆体与精密刻度环",
    "demolition_canister": "手提式炎脉封装筒，双旋阀和红色安全箍",
    "fire_bolt_reward": "装在奖励匣中的火弹术式晶片与火红封签",
    "fire_scroll": "近代工程卷轴，火系线路图与金属压角",
    "frost_bind_reward": "装在奖励匣中的冰缚术式晶片与冷凝锁扣",
    "hammer": "短柄学院维护锤，方形钢锤头和绝缘握把",
    "medkit": "学院现场医务箱，白瓷外壳、绿色扣带和药剂槽",
    "rifle": "近代魔法工业步枪，木质枪托、蓝钢机匣与以太导轨",
    "shield": "学院制轻盾，蓝钢面板、黄铜边和青色能量节点",
    "shield_cell": "可更换护盾电池，蓝钢壳体与青色电量窗",
    "wand": "通用学院短导杖，清晰握柄、导线和单枚以太芯",
    "war_hammer": "双手战锤，重型钢锤头、铆钉和以太减震环",
}

SEMANTIC_SUBJECTS = {
    "category_weapon": "交叉的短剑与枪管", "category_armor": "胸甲正面轮廓",
    "category_consumable": "带封口的药瓶", "category_scroll": "卷起的工程图纸",
    "category_artifact": "带机械刻度的遗物核心", "category_material": "三枚堆叠材料锭",
    "category_quest": "带感叹刻记的任务文件夹", "category_container": "带扣小箱",
    "inventory_search": "放大镜", "inventory_filter": "漏斗筛选器",
    "inventory_sort": "上下排序箭头与两条横线", "inventory_autoplace": "方格自动落入背包格",
    "inventory_quickbar": "三格快捷栏并高亮一格", "inventory_use": "手指按下物品按钮",
    "inventory_salvage": "扳手拆解一枚齿轮", "inventory_discard": "物品落入废料桶",
    "inventory_rotate": "方形物品配顺时针转向箭头", "inventory_clear": "扫帚清空方格",
    "inventory_weight": "秤砣", "loot_unknown": "问号封条小箱",
    "loot_searching": "放大镜检查开启一半的箱子", "loot_empty": "打开且空置的小箱",
}

SLOT_SUBJECTS = {
    "main_hand": "右手握短柄武器", "off_hand": "左手持小盾",
    "head": "护额头盔", "chest": "胸甲", "hands": "一双手套",
    "legs": "护胫长裤", "backpack": "带扣背包", "aether_core": "六边以太核心",
    "conduit": "弯曲导管与接头", "accessory_1": "单枚圆形佩饰",
    "accessory_2": "双环佩饰",
}

ARTIFACT_SUBJECTS = {
    "demolition_canister": "炎脉封装筒，双手旋阀、红色安全箍和投放提梁",
    "aegis_fold": "可展开的折盾匣，层叠蓝钢折片与青白护盾芯",
    "phase_spindle": "移相线轴，缠绕发光导线与定距刻度",
    "binding_frame": "四角缚位框，机械扣合束带",
    "survey_lens": "显迹测镜，黄铜支架、蓝色镜片和测绘刻度",
    "field_siphon": "手压式以太虹吸泵，活塞、软管与青色储液窗",
    "mending_lattice": "可折叠复元编架，柔白网格与医疗卡扣",
    "cover_stamp": "重型掩体压模，方形模腔与双握把",
    "breach_wedge": "解构楔与短锤，楔面有几何裂解槽",
    "relay_compass": "导位罗盘，主轴磁针与牵引弧刻度",
    "reaction_bell": "截击铃，铜铃、地钉与触发线圈",
    "hazard_condenser": "险地冷凝器，水雾阀、冷凝鳍片与收集瓶",
    "turn_ledger": "行程簿，硬壳账页、时间刻度与机械盖印器",
    "anchor_brace": "定锚支架，三足锚爪和自动咬合弹簧",
    "prism_regulator": "棱返调节器，多层镜片与入射角校准轮",
    "decoy_lantern": "诱导灯，暖金灯芯、遮光片与短支架",
    "shield_balancer": "护盾均衡阀，双阀表盘和双向青白导流管",
    "seismic_plumb": "震测铅锤，重锤、细线轴与同心波刻度",
    "null_veil": "折叠静默幕，暗紫吸收布与金属收纳杆",
    "fortune_seal": "冒险封签，黑金机械封印片与断裂拉环",
}


def fire_entries() -> list[dict]:
    source = (ROOT / "Worldbuilding/01_游戏策划/OCC_火元素个人术式池_v0.1.md").read_text(encoding="utf-8")
    rows = {}
    for line in source.splitlines():
        match = re.match(r"\| F-P(\d{2}) ([^|]+) \|[^|]*\| ([^|]+) \|", line)
        if match:
            rows[int(match.group(1))] = (match.group(2).strip(), match.group(3).strip())
    if set(rows) != set(range(1, 51)):
        raise RuntimeError(f"Expected 50 fire rows, got {sorted(rows)}")
    return [entry("fire", f"f-p{i:02d}", "equipment_icon_32",
                  f"{rows[i][0]}：{rows[i][1]}", "FormalSkillIcons32/Fire") for i in range(1, 51)]


def entry(group: str, stem: str, role: str, subject: str, final_dir: str) -> dict:
    size = 16 if role == "semantic_icon_16" else 32
    palette = 4 if size == 16 else 10
    return {
        "asset_id": f"icon.{group}.{stem}", "group": group, "stem": stem,
        "role": role, "delivery_size": [size, size], "palette_max": palette,
        "subject": subject,
        "final_path": f"UnityProject/Assets/Game/Resources/Art/{final_dir}/{stem}.png",
        "staging_path": f"UnityProject/Assets/Game/Resources/Art/ValidationIconRegen143/{group}/{stem}.png",
    }


def collect() -> list[dict]:
    values = fire_entries()
    values += [entry("runtime", s, "equipment_icon_32", RUNTIME_SUBJECTS[s], "FormalSkillIcons32/Runtime") for s in sorted(RUNTIME_SUBJECTS)]
    values += [entry("item", s, "equipment_icon_32", ITEM_SUBJECTS[s], "FormalItemIcons32") for s in sorted(ITEM_SUBJECTS)]
    values += [entry("semantic", s, "semantic_icon_16", SEMANTIC_SUBJECTS[s], "FormalItemSemanticIcons16") for s in sorted(SEMANTIC_SUBJECTS)]
    values += [entry("artifact", s, "equipment_icon_32", ARTIFACT_SUBJECTS[s], "FormalArtifactIcons32") for s in sorted(ARTIFACT_SUBJECTS)]
    values += [entry("slot", s, "semantic_icon_16", SLOT_SUBJECTS[s], "FormalEquipmentSlotIcons16") for s in sorted(SLOT_SUBJECTS)]
    if len(values) != 143 or len({v["asset_id"] for v in values}) != 143:
        raise RuntimeError(f"Expected 143 unique entries, got {len(values)}")
    return values


def manifest(value: dict) -> dict:
    stem = value["stem"]
    group = value["group"]
    root = f"UnityProject/Artifacts/IconRegen143/{group}/{stem}"
    return {
        "schema": "occ-art-manifest-v1", "contract_version": 1,
        "asset_id": value["asset_id"], "role": value["role"], "status": "QA_PENDING",
        "provenance": {"source_channel": "codex_builtin_imagegen", "source_descriptor": "Codex built-in image generation; independent single asset; no board slicing", "source_path": f"{root}/source.png", "source_sha256": "PENDING_GENERATION"},
        "delivery": {"output_path": value["staging_path"], "output_sha256": "PENDING_GENERATION", "native_output_path": None, "logical_cells": None, "palette_max": value["palette_max"], "required_color_families": []},
        "application": {"runtime_draw_rect": f"complete {value['delivery_size'][0]}x{value['delivery_size'][1]} icon slot", "default_integer_scale": 4, "minimum_integer_scale": 2},
        "evidence": {"one_x": f"{root}/1x.png", "four_x": f"{root}/4x.png", "grayscale": f"{root}/grayscale.png", "checker": f"{root}/checker.png", "application_contact": "UnityProject/Artifacts/IconRegen143/contacts/PENDING.png"},
        "human_review": {"overall": "PENDING", "reviewer": "", "date": "", "silhouette": "PENDING", "material": "PENDING", "perspective": "PENDING", "style": "PENDING", "application": "PENDING", "notes": value["subject"]},
        "unity_import": None,
    }


def main() -> None:
    values = collect()
    MANIFESTS.mkdir(parents=True, exist_ok=True)
    (OUT / "icon_regen_143_catalog.json").write_text(json.dumps({"schema": "occ-icon-regen-catalog-v1", "count": len(values), "assets": values}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    for value in values:
        path = MANIFESTS / value["group"] / f"{value['stem']}.occ-art-manifest-v1.json"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(manifest(value), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"count": len(values), "groups": {g: sum(v["group"] == g for v in values) for g in sorted({v["group"] for v in values})}}, ensure_ascii=False))


if __name__ == "__main__":
    main()
