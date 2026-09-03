#!/usr/bin/env python3
from __future__ import annotations
import json,sys
from pathlib import Path
from validate_occ_art_asset import DEFAULT_CONTRACT,find_root,read_json,validate_manifest
def main():
 root=find_root(Path(__file__).resolve().parent);contract=read_json(DEFAULT_CONTRACT);folder=root/"Worldbuilding/05_美术与音频/正式美术生产/M-A25/manifests";results=[]
 for p in sorted(folder.glob("*.occ-art-manifest-v1.json")):
  errors,report=validate_manifest(read_json(p),contract,root);results.append({"manifest":p.relative_to(root).as_posix(),"asset_id":report.get("asset_id"),"status":"PASS" if not errors else "FAIL","errors":errors,"metrics":report.get("metrics",{})})
 failed=[x for x in results if x["status"]=="FAIL"];summary={"schema":"occ-m-a25-formal-validation-v1","status":"PASS" if len(results)==10 and not failed else "FAIL","manifest_count":len(results),"pass_count":len(results)-len(failed),"fail_count":len(failed),"results":results};out=root/"UnityProject/Artifacts/CombatSemanticPolish10/validation_report_formal.json";out.write_text(json.dumps(summary,ensure_ascii=False,indent=2)+"\n",encoding="utf-8");print(json.dumps({k:summary[k] for k in("status","manifest_count","pass_count","fail_count")}));return 0 if summary["status"]=="PASS" else 1
if __name__=="__main__":sys.exit(main())
