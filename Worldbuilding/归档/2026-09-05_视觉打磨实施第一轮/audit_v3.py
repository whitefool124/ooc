from pathlib import Path
import json, importlib.util, copy
root=Path('E:/数据库/OCC_Codex')
task=root/'Worldbuilding/归档/2026-09-05_视觉打磨实施第一轮'
spec=importlib.util.spec_from_file_location('occ_validator',root/'Tools/OCCArt/validate_occ_art_asset.py')
module=importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
contract=module.read_json(root/'Tools/OCCArt/occ_art_contract_v1.json')
contract_errors=module.audit_contract(contract,root)
results=[]
for file in sorted((root/'Worldbuilding/归档/2026-09-04_首次战斗雨后灯庭/美术候选/v3/manifests').glob('*.json')):
 original=module.read_json(file); candidate=copy.deepcopy(original)
 candidate['status']='QA_PENDING'
 candidate['human_review']={'overall':'PENDING','notes':'User chose Feishu rev320 as authoritative; old approval is not current approval.'}
 candidate['unity_import']['importer_verified']=False
 candidate['unity_import']['runtime_verified']=False
 candidate['application']['default_integer_scale']=2
 errors,report=module.validate_manifest(candidate,contract,root)
 results.append({'historical_manifest':file.relative_to(root).as_posix(),'historical_status':original['status'],'current_review_status':'QA_PENDING','approved_this_review':False,'review_method':'Read-only in-memory QA_PENDING validation; historical file unchanged; current overview scale 2, minimum 1.','technical_report':report})
report={'contract_audit':{'status':'FAIL' if contract_errors else 'PASS','errors':contract_errors},'asset_count':len(results),'current_approval_count':0,'assets':results,'limitations':['Historical application screenshot predates current overview layout.','Fresh runtime contact and user aesthetic review are still required.','Machine PASS does not promote an asset.']}
(task/'evidence/v3_current_reaudit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'contract':report['contract_audit'],'assets':[(r['technical_report']['asset_id'],r['technical_report']['status'],r['technical_report']['errors']) for r in results]},ensure_ascii=False))
