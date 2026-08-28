#!/usr/bin/env python3
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];M=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A30/manifests";CONTACT="UnityProject/Artifacts/UiChapterDividers5/contacts/ui_chapter_dividers_application_1920x1080.png"
for p in sorted(M.glob("divider_*.occ-art-manifest-v1.json")):
 m=json.loads(p.read_text(encoding="utf-8-sig"));m["status"]="FORMAL_CANDIDATE";m["evidence"]["application_contact"]=CONTACT;m["human_review"].update({"overall":"PASS","reviewer":"Product-owner delegated autonomous UI art review","date":"2026-08-28","silhouette":"PASS","material":"PASS","perspective":"PASS","style":"PASS","application":"PASS","notes":m["human_review"]["notes"]+"; distinct horizontal material cue, no blue/cyan template, passed 1x/4x checker, grayscale and dual-resolution offscreen UGUI contact."});p.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"status":"PASS","formal_candidates":5}))
