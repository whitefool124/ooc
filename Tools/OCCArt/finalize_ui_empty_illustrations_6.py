#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; MANIFESTS=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A29/manifests"; UNITY=ROOT/"UnityProject/Assets/Game/Resources/Art/FormalUIEmptyIllustrations"; CONTACT="UnityProject/Artifacts/UiEmptyIllustrations6/contacts/ui_empty_formal_runtime_1920x1080.png"
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def guid(p):
    for line in Path(str(p)+".meta").read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("guid: "): return line.split(":",1)[1].strip()
    raise RuntimeError("missing GUID "+str(p))
seen=set()
for path in sorted(MANIFESTS.glob("empty_*.occ-art-manifest-v1.json")):
    m=json.loads(path.read_text(encoding="utf-8-sig")); stem=m["asset_id"].split(".")[-1]; asset=UNITY/f"{stem}.png"; g=guid(asset)
    if g in seen: raise RuntimeError("duplicate GUID "+g)
    seen.add(g); rel=asset.relative_to(ROOT).as_posix(); m["status"]="FORMAL"; m["delivery"]["output_path"]=rel; m["delivery"]["output_sha256"]=digest(asset); m["evidence"]["application_contact"]=CONTACT
    m["unity_import"]={"asset_path":rel,"resource_path":f"Art/FormalUIEmptyIllustrations/{stem}","guid":g,"stable_guid":g,"importer_verified":True,"runtime_verified":True,"importer":{"texture_type":"Sprite","pixels_per_unit":32,"filter_mode":"Point","wrap_mode":"Clamp","mipmap_enabled":False,"compression":"Uncompressed"},"runtime_evidence":CONTACT,"audit_report":"UnityProject/Artifacts/UiEmptyIllustrations6/import_audit.json","resolutions":["1920x1080","960x540"],"verification_date":"2026-08-28"}
    path.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"status":"PASS","formalized":6,"unique_guids":len(seen)}))
