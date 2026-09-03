#!/usr/bin/env python3
from __future__ import annotations
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
PROD=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A25"
MANIFESTS=PROD/"manifests"
ASSETS=[
 ("intent","attack","combat.intent.attack","semantic_icon_16",4,"单一锈红重斩楔与下缘撞点"),
 ("intent","cast","combat.intent.cast","semantic_icon_16",4,"紫罗兰缺口校准环与内向刻点"),
 ("intent","interact_destroy","combat.intent.interact_destroy","semantic_icon_16",4,"锈红短锤击断方块一角"),
 ("status","slow","combat.status.slow","equipment_icon_32",10,"赭黄重块持续压住象牙靴跟"),
 ("status","revealed","combat.status.revealed","equipment_icon_32",10,"暖金测绘括号持续围住象牙人形"),
 ("feedback","bound","combat.feedback.bound","equipment_icon_32",10,"两侧棕色夹具瞬间夹住中心腰线"),
 ("feedback","slow","combat.feedback.slow","equipment_icon_32",10,"足迹拖线撞上赭黄垂直止动条"),
 ("feedback","healing","combat.feedback.healing","equipment_icon_32",10,"灰绿缝线瞬间合拢锈红伤口"),
 ("feedback","shield_restore","combat.feedback.shield_restore","equipment_icon_32",10,"三枚乳白护片向旧金铆点合拢"),
 ("feedback","status_cleared","combat.feedback.status_cleared","equipment_icon_32",10,"象牙扫弧把三枚暗色状态点推出"),
]

def main():
 MANIFESTS.mkdir(parents=True,exist_ok=True);catalog=[]
 for group,stem,asset_id,role,palette,subject in ASSETS:
  size=16 if role=="semantic_icon_16" else 32
  artifact=f"UnityProject/Artifacts/CombatSemanticPolish10/{group}/{stem}"
  staging=f"UnityProject/Assets/Game/Resources/Art/ValidationCombatSemanticPolish/{group}/{stem}.png"
  folder={"intent":"FormalIntentIcons16","status":"FormalStatusIcons32","feedback":"FormalFeedbackIcons32"}[group]
  final=f"UnityProject/Assets/Game/Resources/Art/{folder}/{stem}.png"
  item={"asset_id":asset_id,"group":group,"stem":stem,"subject":subject,"role":role,"delivery_size":[size,size],"palette_max":palette,"source_path":artifact+"/source_v2.png","staging_path":staging,"final_path":final}
  catalog.append(item)
  manifest={"schema":"occ-art-manifest-v1","contract_version":1,"asset_id":asset_id,"role":role,"status":"QA_PENDING","provenance":{"source_channel":"codex_builtin_imagegen","source_descriptor":"Independent M-A25 single-icon refinement; no board slicing","source_path":item["source_path"],"source_sha256":"PENDING_GENERATION"},"delivery":{"output_path":staging,"output_sha256":"PENDING_GENERATION","native_output_path":None,"logical_cells":None,"palette_max":palette,"required_color_families":[]},"application":{"runtime_draw_rect":f"{size}x{size} combat semantic slot","default_integer_scale":2,"minimum_integer_scale":1},"evidence":{"one_x":artifact+"/1x.png","four_x":artifact+"/4x.png","grayscale":artifact+"/grayscale.png","checker":artifact+"/checker.png","application_contact":"UnityProject/Artifacts/CombatSemanticPolish10/contacts/PENDING.png"},"human_review":{"overall":"PENDING","reviewer":"","date":"","silhouette":"PENDING","material":"PENDING","perspective":"PENDING","style":"PENDING","application":"PENDING","notes":subject},"unity_import":None}
  (MANIFESTS/f"{group}_{stem}.occ-art-manifest-v1.json").write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 (PROD/"combat_semantic_polish_10_catalog.json").write_text(json.dumps({"schema":"occ-combat-semantic-polish-catalog-v1","count":len(catalog),"assets":catalog},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 print(json.dumps({"prepared":len(catalog)}))
if __name__=="__main__":main()
