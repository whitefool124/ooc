#!/usr/bin/env python3
"""Create the M-A20 catalog and 32 QA_PENDING manifests before generation."""

from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A20"
MANIFESTS = OUT / "manifests"

ITEMS = [
    ("ACA-EQ-MH01", "学院练习剑", "aca_eq_mh01", "朴素直刃学院练习剑，短十字护手、皮革缠柄、钝圆钢剑尖；冷灰锻铁和棕皮，无晶体无能量光"),
    ("ACA-EQ-MH02", "钩刃长枪", "aca_eq_mh02", "长木枪，窄钢枪尖下带明显后弯钩刃和旧铜箍；长对角线轮廓，无晶体无发光"),
    ("ACA-EQ-MH03", "刻印战锤", "aca_eq_mh03", "双手战锤，宽厚方锤头、单侧破拆喙、锤面深刻几何解构纹与木柄铁箍；沉重锻铁，无晶体"),
    ("ACA-EQ-MH04", "猎团短弓", "aca_eq_mh04", "短而宽的猎团反曲木弓，皮革握把、清楚弓弦与一支短箭；暖木和骨白箭羽，无能量"),
    ("ACA-EQ-MH05", "绞盘重弩", "aca_eq_mh05", "粗重横弩，宽弩臂、方形木托、中央手摇绞盘和已扣弩矢；蓝黑钢与木，无晶体无发光"),
    ("ACA-EQ-MH06", "灰炉导杖", "aca_eq_mh06", "手工灰木导杖，杖首是小型陶瓷灰炉与旧铜风门，内部仅有暗红余烬和一缕橙热刻线；禁止蓝晶"),
    ("ACA-EQ-OH01", "学院圆盾", "aca_eq_oh01", "圆形木芯学院盾，铁边、中央圆盾钉、两道朴素旧铜固定带；无晶体无发光"),
    ("ACA-EQ-OH02", "石闸长盾", "aca_eq_oh02", "高而窄的石闸长盾，层压木芯、厚铁包边、下端方脚和横向承压梁；重型垂直轮廓，无晶体"),
    ("ACA-EQ-OH03", "反握短刃", "aca_eq_oh03", "短宽单刃匕首，反握弯柄、护指环和背部锯齿；深钢与棕皮，无能量"),
    ("ACA-EQ-OH04", "导流副环", "aca_eq_oh04", "可握持的双层旧铜导流环，侧面陶瓷绝缘握片和一段小面积紫罗兰回路刻线；不是宝石戒指"),
    ("ACA-EQ-CH01", "夹棉练习衣", "aca_eq_ch01", "短款夹棉练习上衣，明显菱形绗缝、布腰带和加厚肩片；灰褐粗布，无金属装甲无发光"),
    ("ACA-EQ-CH02", "补强巡行衣", "aca_eq_ch02", "耐磨巡行外套，布料主体上有皮革肩肘补强片、双排扣带和短下摆；深灰粗布与棕皮"),
    ("ACA-EQ-CH03", "塔卫承压带", "aca_eq_ch03", "穿戴式宽厚承压胸带，多层交叉皮带、旧铁胸扣与两侧受力环；不是完整胸甲，灰绿守备点缀，无发光"),
    ("ACA-EQ-CH04", "轻装传令衣", "aca_eq_ch04", "轻薄短款传令衣，单肩短披、斜跨文书带和分叉短下摆；灰蓝布与暖棕皮，无晶体"),
    ("ACA-EQ-CH05", "封存巡检袍", "aca_eq_ch05", "及膝巡检袍，宽领、工具口袋、腰挂封签钳与米白耐污下摆；深炭布和少量暗紫封存缝线，无发光"),
    ("ACA-EQ-HD01", "测距护目镜", "aca_eq_hd01", "双镜片测距护目镜，黄铜可翻镜架、棕皮头带，一枚淡黄色玻璃主镜和一枚烟灰副镜；无蓝晶"),
    ("ACA-EQ-HD02", "低压回路护额", "aca_eq_hd02", "弧形皮革护额，中央小型陶瓷电容片、两侧旧铜回路线，极少量灰绿低压指示；不含水晶"),
    ("ACA-EQ-HN01", "行进握带", "aca_eq_hn01", "一对缠绕手掌与手腕的行进握带，粗布包带、皮革掌垫和拉紧扣；无能量"),
    ("ACA-EQ-HN02", "回授护臂", "aca_eq_hn02", "成对前臂护具，层压皮革、陶瓷护板和旧铜回授扣，一条乳白灰绿防护刻线；无晶体"),
    ("ACA-EQ-LG01", "石路行靴", "aca_eq_lg01", "一双结实行靴，厚分层鞋底、包脚踝皮带与石灰色防滑钉；棕皮和铁灰，无发光"),
    ("ACA-EQ-LG02", "定锚胫甲", "aca_eq_lg02", "一对高胫甲，外侧明显折叠锚钉、厚铁护板和皮革固定带；沉重下压轮廓，无晶体"),
    ("ACA-EQ-BP01", "勘验背架", "aca_eq_bp01", "窄高木质勘验背架，绑有折叠测尺、两个样本盒和卷起的记录布袋；轮廓有横向测尺，不画成普通背包"),
    ("ACA-EQ-BP02", "快挂整备架", "aca_eq_bp02", "宽肩式快速整备背架，三排不同大小的快挂扣、侧置工具筒和下方束带；铁木框架与安全黄小扣，无晶体"),
    ("ACA-EQ-CR01", "学院储能芯", "aca_eq_cr01", "方形陶瓷储能盒，旧铜护角、机械锁扣和中央狭长烟紫玻璃储能管；紫色介质不发大光，禁止蓝晶"),
    ("ACA-EQ-CR02", "余焰回收芯", "aca_eq_cr02", "圆筒形焦黑铜回收芯，散热鳍、回流阀与中央橙红余烬观察窗；火系热色，禁止蓝晶青光"),
    ("ACA-EQ-CR03", "塔心并联芯", "aca_eq_cr03", "三叶并联核心，旧金三臂框架围绕琥珀色多面介质，三条独立导接端；金色命名物，禁止蓝晶"),
    ("ACA-EQ-DG01", "远投定距杖", "aca_eq_dg01", "细长定距导杖，木杆、可滑动黄铜测距环、顶端叉形瞄准片和一枚淡黄玻璃准星；无水晶球"),
    ("ACA-EQ-DG02", "接触耦合环", "aca_eq_dg02", "手掌大小的厚重耦合环，开口 C 形旧铜本体、陶瓷接触垫和机械夹扣；一小段暖白接通刻线，无宝石"),
    ("ACA-EQ-AC01", "余烬珠", "aca_eq_ac01", "挂在短皮绳上的焦陶余烬珠，表面开三道裂隙露出暗橙红余火；不是蓝水晶，不画火球"),
    ("ACA-EQ-AC02", "空槽魔力计", "aca_eq_ac02", "扁平黄铜魔力计，半圆刻度盘、机械指针停在空槽、烟灰玻璃面和小挂环；无发光宝石"),
    ("ACA-EQ-AC03", "贴身守誓牌", "aca_eq_ac03", "可贴身佩戴的矩形旧铁守誓牌，磨圆角、皮绳孔、盾形压印和一条灰绿珐琅；非宗教圣牌，无光晕"),
    ("ACA-EQ-AC04", "灰线行程扣", "aca_eq_ac04", "小型矩形行程扣，黑铁外框、中央灰白滑轨、侧面方向拨片和一枚暗橙热标；不是芯片或水晶"),
]


def entry(item: tuple[str, str, str, str]) -> dict:
    runtime_id, name, stem, subject = item
    root = f"UnityProject/Artifacts/AcademyEquipment32/{stem}"
    return {"asset_id": f"equipment.academy.{stem}", "runtime_id": runtime_id,
            "name": name, "stem": stem, "role": "equipment_icon_32",
            "delivery_size": [32, 32], "palette_max": 10, "subject": subject,
            "final_path": f"UnityProject/Assets/Game/Resources/Art/FormalAcademyEquipmentIcons32/{stem}.png",
            "staging_path": f"UnityProject/Assets/Game/Resources/Art/ValidationAcademyEquipmentIcons32/{stem}.png",
            "source_path": f"{root}/source.png"}


def manifest(value: dict) -> dict:
    root = f"UnityProject/Artifacts/AcademyEquipment32/{value['stem']}"
    return {"schema": "occ-art-manifest-v1", "contract_version": 1,
            "asset_id": value["asset_id"], "role": "equipment_icon_32", "status": "QA_PENDING",
            "provenance": {"source_channel": "codex_builtin_imagegen", "source_descriptor": "Codex built-in image generation; independent single equipment asset; no board slicing", "source_path": value["source_path"], "source_sha256": "PENDING_GENERATION"},
            "delivery": {"output_path": value["staging_path"], "output_sha256": "PENDING_GENERATION", "native_output_path": None, "logical_cells": None, "palette_max": 10, "required_color_families": []},
            "application": {"runtime_draw_rect": "complete 32x32 equipment content icon slot", "default_integer_scale": 4, "minimum_integer_scale": 2},
            "evidence": {"one_x": f"{root}/1x.png", "four_x": f"{root}/4x.png", "grayscale": f"{root}/grayscale.png", "checker": f"{root}/checker.png", "application_contact": "UnityProject/Artifacts/AcademyEquipment32/contacts/PENDING.png"},
            "human_review": {"overall": "PENDING", "reviewer": "", "date": "", "silhouette": "PENDING", "material": "PENDING", "perspective": "PENDING", "style": "PENDING", "application": "PENDING", "notes": value["subject"]},
            "unity_import": None}


def main() -> None:
    values = [entry(item) for item in ITEMS]
    if len(values) != 32 or len({v["runtime_id"] for v in values}) != 32:
        raise RuntimeError("Expected 32 unique equipment entries")
    MANIFESTS.mkdir(parents=True, exist_ok=True)
    (OUT / "academy_equipment_32_catalog.json").write_text(json.dumps({"schema": "occ-academy-equipment-icon-catalog-v1", "count": 32, "assets": values}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    for value in values:
        (MANIFESTS / f"{value['stem']}.occ-art-manifest-v1.json").write_text(json.dumps(manifest(value), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"count": len(values), "manifest_dir": str(MANIFESTS)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
