from pathlib import Path
import importlib.util,json,sys
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'Tools/OCCArt'))
spec=importlib.util.spec_from_file_location('occ_validator',ROOT/'Tools/OCCArt/validate_occ_art_asset.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
contract=module.read_json(ROOT/'Tools/OCCArt/occ_art_contract_v1.json')
results=[]
for path in sorted(OUT.rglob('*_manifest.json')):
 errors,result=module.validate_manifest(module.read_json(path),contract,ROOT)
 results.append({'manifest':path.relative_to(ROOT).as_posix(),'status':'FAIL' if errors else 'PASS','errors':errors,'validator_result':result})
(OUT/'admission_summary.json').write_text(json.dumps(results,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'checked':len(results),'passed':sum(r['status']=='PASS' for r in results),'status':'QA_PENDING'}))
