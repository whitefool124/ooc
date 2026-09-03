#!/usr/bin/env python3
from __future__ import annotations
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A24";M=OUT/"manifests"
GROUPS={
"command":[("move","双步前进箭头，冷青动作点"),("attack","两道锈红斩击在目标点交汇"),("skill","单个琥珀回路被手势启动"),("skill_two","两个紫罗兰回路依次启动"),("loot","打开小箱与向下取物手势"),("interact","手指压下旧金机械扣")],
"intent":[("attack","三枚锈红尖锋向中心逼近"),("cast","三点紫罗兰以太向中心聚拢"),("move","一前一后两枚冷青足迹"),("defend","乳白护面闭合在中心"),("interact_destroy","锈红短锤击中方形裂口")],
"status":[("burning","持续燃烧的橙红闭环与中心余烬"),("slow","土黄色重坠压住一只靴底"),("bound","棕绳机械结形成封闭束缚环"),("armor_break","锈红裂缝贯穿断开的胸甲片"),("dazzled","暖金偏光星芒遮住半闭眼睑"),("revealed","暖金测绘轮廓围住显形人影")],
"feedback":[("damage","锈红冲击缺口和向外碎片"),("shield_absorb","乳白冲击波被旧金弧面截住"),("armor_break","甲片在锈红撞点向外碎裂"),("burning","橙红火舌在接触点瞬间窜起"),("bound","棕色索套从四向猛然收紧"),("slow","土黄重线拖住前进足迹"),("healing","灰绿缝合弧合拢一道伤口"),("shield_restore","三枚乳白护片向旧金核心合拢"),("mana_restore","紫罗兰流线注入刻度容器"),("status_cleared","象牙清扫弧带走三枚暗色状态点"),("movement","两段冷青残线跨过中心格"),("object_damaged","石木方块出现锈红单道裂纹"),("object_destroyed","方块向四侧崩解成碎片"),("unit_defeated","暗色人形轮廓倒向下方终点线")],}
def main():
 M.mkdir(parents=True,exist_ok=True);assets=[]
 for group,items in GROUPS.items():
  for stem,subject in items:
   micro=group in("command","intent");size=16 if micro else 32;role="semantic_icon_16" if micro else "equipment_icon_32";final_dir={"command":"FormalCommandIcons16","intent":"FormalIntentIcons16","status":"FormalStatusIcons32","feedback":"FormalFeedbackIcons32"}[group];root=f"UnityProject/Artifacts/CombatSemantics31/{group}/{stem}";v={"asset_id":f"combat.{group}.{stem}","group":group,"stem":stem,"subject":subject,"role":role,"delivery_size":[size,size],"palette_max":4 if micro else 10,"source_path":f"{root}/source.png","staging_path":f"UnityProject/Assets/Game/Resources/Art/ValidationCombatSemantics/{group}/{stem}.png","final_path":f"UnityProject/Assets/Game/Resources/Art/{final_dir}/{stem}.png"};assets.append(v)
   m={"schema":"occ-art-manifest-v1","contract_version":1,"asset_id":v["asset_id"],"role":role,"status":"QA_PENDING","provenance":{"source_channel":"codex_builtin_imagegen","source_descriptor":"Independent single combat semantic icon; no board slicing","source_path":v["source_path"],"source_sha256":"PENDING_GENERATION"},"delivery":{"output_path":v["staging_path"],"output_sha256":"PENDING_GENERATION","native_output_path":None,"logical_cells":None,"palette_max":v["palette_max"],"required_color_families":[]},"application":{"runtime_draw_rect":"16x16 semantic slot" if micro else "32x32 status or feedback slot","default_integer_scale":2,"minimum_integer_scale":1},"evidence":{"one_x":f"{root}/1x.png","four_x":f"{root}/4x.png","grayscale":f"{root}/grayscale.png","checker":f"{root}/checker.png","application_contact":"UnityProject/Artifacts/CombatSemantics31/contacts/PENDING.png"},"human_review":{"overall":"PENDING","reviewer":"","date":"","silhouette":"PENDING","material":"PENDING","perspective":"PENDING","style":"PENDING","application":"PENDING","notes":subject},"unity_import":None};(M/f"{group}_{stem}.occ-art-manifest-v1.json").write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 if len(assets)!=31:raise RuntimeError(len(assets))
 (OUT/"combat_semantics_31_catalog.json").write_text(json.dumps({"schema":"occ-combat-semantics-catalog-v1","count":31,"assets":assets},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 print(json.dumps({"count":len(assets)}))
if __name__=="__main__":main()
