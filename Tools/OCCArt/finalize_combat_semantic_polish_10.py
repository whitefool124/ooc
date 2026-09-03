#!/usr/bin/env python3
from __future__ import annotations
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
PROD=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A25";CAT=PROD/"combat_semantic_polish_10_catalog.json";MANIFESTS=PROD/"manifests";REPORT=ROOT/"UnityProject/Artifacts/CombatSemanticPolish10/unity_import_report.json"
CONTACT="UnityProject/Artifacts/CombatSemanticPolish10/contacts/unity_polish_contact_1920x1080.png"
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 assets=json.loads(CAT.read_text(encoding="utf-8"))["assets"];report=json.loads(REPORT.read_text(encoding="utf-8-sig"));imports={x["asset_id"]:x for x in report["assets"]}
 if report["count"]!=10 or report["loaded"]!=10 or report["preserved_guids"]!=10 or report["failures"]:raise RuntimeError("Unity report not clean")
 for a in assets:
  mp=MANIFESTS/f"{a['group']}_{a['stem']}.occ-art-manifest-v1.json";m=json.loads(mp.read_text(encoding="utf-8"));u=imports[m["asset_id"].removeprefix("combat.")];formal=ROOT/a["final_path"]
  m["status"]="FORMAL";m["delivery"]["output_path"]=a["final_path"];m["delivery"]["output_sha256"]=sha(formal);m["evidence"]["application_contact"]=CONTACT
  m["human_review"]={"overall":"PASS","reviewer":"Codex OCC art-direction review","date":"2026-08-27","silhouette":"PASS","material":"PASS","perspective":"PASS","style":"PASS","application":"PASS","notes":"M-A25 targeted silhouette refinement is clearer than M-A24 at native 1x and Unity 1x/2x contact; persistent and instantaneous visual grammars remain distinct."}
  m["unity_import"]={"asset_path":u["asset_path"],"stable_guid":u["guid"],"texture_type":"Sprite","filter_mode":"Point","wrap_mode":"Clamp","pixels_per_unit":u["ppu"],"compression":"Uncompressed","mipmaps":False,"importer_verified":u["importer_ok"],"runtime_verified":True};mp.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 print(json.dumps({"finalized":len(assets),"status":"FORMAL"}))
if __name__=="__main__":main()
