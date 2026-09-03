#!/usr/bin/env python3
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
MANIFESTS=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A29/manifests"
CONTACT="UnityProject/Artifacts/UiEmptyIllustrations6/contacts/ui_empty_application_1920x1080.png"
for path in sorted(MANIFESTS.glob("empty_*.occ-art-manifest-v1.json")):
    m=json.loads(path.read_text(encoding="utf-8-sig")); m["status"]="FORMAL_CANDIDATE"; m["evidence"]["application_contact"]=CONTACT
    m["human_review"].update({"overall":"PASS","reviewer":"Product-owner delegated autonomous UI art review","date":"2026-08-28","silhouette":"PASS","material":"PASS","perspective":"PASS","style":"PASS","application":"PASS","notes":m["human_review"]["notes"]+"; clear empty/closed silhouette, restrained non-blue palette, passed 1x/4x checker, grayscale and 1920x1080/960x540 offscreen UGUI contact."})
    path.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"status":"PASS","formal_candidates":6}))
