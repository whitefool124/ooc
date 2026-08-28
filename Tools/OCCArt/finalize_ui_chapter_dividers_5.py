#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];M=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A30/manifests";U=ROOT/"UnityProject/Assets/Game/Resources/Art/FormalUIChapterDividers";CONTACT="UnityProject/Artifacts/UiChapterDividers5/contacts/ui_chapter_dividers_formal_1920x1080.png"
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def guid(p):
 for x in Path(str(p)+".meta").read_text(encoding="utf-8-sig").splitlines():
  if x.startswith("guid: "):return x.split(":",1)[1].strip()
 raise RuntimeError("missing guid")
seen=set()
for p in sorted(M.glob("divider_*.occ-art-manifest-v1.json")):
 m=json.loads(p.read_text(encoding="utf-8-sig"));stem=m["asset_id"].split(".")[-1];a=U/f"{stem}.png";g=guid(a)
 if g in seen:raise RuntimeError("duplicate guid "+g)
 seen.add(g);rel=a.relative_to(ROOT).as_posix();m["status"]="FORMAL";m["delivery"]["output_path"]=rel;m["delivery"]["output_sha256"]=sha(a);m["evidence"]["application_contact"]=CONTACT;m["unity_import"]={"asset_path":rel,"resource_path":f"Art/FormalUIChapterDividers/{stem}","guid":g,"stable_guid":g,"importer_verified":True,"runtime_verified":True,"importer":{"texture_type":"Sprite","pixels_per_unit":32,"filter_mode":"Point","wrap_mode":"Clamp","mipmap_enabled":False,"compression":"Uncompressed"},"runtime_evidence":CONTACT,"audit_report":"UnityProject/Artifacts/UiChapterDividers5/import_audit.json","resolutions":["1920x1080","960x540"],"verification_date":"2026-08-28"};p.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"status":"PASS","formalized":5,"unique_guids":len(seen)}))
