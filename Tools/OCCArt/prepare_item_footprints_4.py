#!/usr/bin/env python3
"""Create M-A23 four-item footprint catalog and manifests before generation."""
from __future__ import annotations
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A23"; MANIFESTS=OUT/"manifests"
ITEMS=[
 {"runtime_id":"medkit","stem":"medkit","name":"医疗包","subject":"学院现场医务箱，象牙白陶瓷壳、灰绿布扣带、药瓶槽与剪钳","cells":[2,1]},
 {"runtime_id":"shield_cell","stem":"shield_cell","name":"护盾单元","subject":"可更换护幕储能筒，深铁护架、乳白储能介质、灰绿绝缘套和旧金阀帽","cells":[1,2]},
 {"runtime_id":"F-S01","stem":"fire_scroll","name":"火线卷轴","subject":"近代工程火术卷轴，耐热皮纸、陶红线路、橙色封蜡与铜压角","cells":[2,1]},
 {"runtime_id":"aether_core","stem":"aether_core","name":"以太核心","subject":"任务回收用工业核心，深陶瓷六瓣护壳、琥珀金储能窗、锻铁搬运环","cells":[2,2]},]
def main():
 MANIFESTS.mkdir(parents=True,exist_ok=True);assets=[]
 for i in ITEMS:
  w,h=i["cells"];stem=i["stem"];root=f"UnityProject/Artifacts/ItemFootprints4/{stem}";v={**i,"asset_id":f"item.footprint.{stem}","role":"multi_cell_prop_32","logical_cells":[w,h],"delivery_size":[w*32,h*32],"palette_max":12,"source_path":f"{root}/source.png","staging_path":f"UnityProject/Assets/Game/Resources/Art/ValidationItemFootprints/{stem}.png","final_path":f"UnityProject/Assets/Game/Resources/Art/FormalInventoryFootprints/{stem}.png"};assets.append(v)
  m={"schema":"occ-art-manifest-v1","contract_version":1,"asset_id":v["asset_id"],"role":"multi_cell_prop_32","status":"QA_PENDING","provenance":{"source_channel":"codex_builtin_imagegen","source_descriptor":"Independent single inventory footprint; no icon stretching or board slicing","source_path":v["source_path"],"source_sha256":"PENDING_GENERATION"},"delivery":{"output_path":v["staging_path"],"output_sha256":"PENDING_GENERATION","native_output_path":None,"logical_cells":[w,h],"palette_max":12,"required_color_families":[]},"application":{"runtime_draw_rect":f"{w}x{h} inventory cells at 32 pixels per cell","default_integer_scale":2,"minimum_integer_scale":1},"evidence":{"one_x":f"{root}/1x.png","four_x":f"{root}/4x.png","grayscale":f"{root}/grayscale.png","checker":f"{root}/checker.png","application_contact":"UnityProject/Artifacts/ItemFootprints4/contacts/PENDING.png"},"human_review":{"overall":"PENDING","reviewer":"","date":"","silhouette":"PENDING","material":"PENDING","perspective":"PENDING","style":"PENDING","application":"PENDING","notes":i["subject"]},"unity_import":None};(MANIFESTS/f"{stem}.occ-art-manifest-v1.json").write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 (OUT/"item_footprints_4_catalog.json").write_text(json.dumps({"schema":"occ-item-footprints-catalog-v1","count":4,"assets":assets},ensure_ascii=False,indent=2)+"\n",encoding="utf-8");print(json.dumps({"count":len(assets)}))
if __name__=="__main__":main()
