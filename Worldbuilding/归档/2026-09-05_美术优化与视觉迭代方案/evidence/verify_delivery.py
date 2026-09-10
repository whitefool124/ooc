from pathlib import Path
import csv
import difflib
import io
import json
import os
import re
import subprocess
import sys

root = Path('E:/数据库/OCC_Codex')
task = root / 'Worldbuilding/归档/2026-09-05_美术优化与视觉迭代方案'
evidence = task / 'evidence'
final = json.loads((evidence / 'final_master_read.json').read_text(encoding='utf-8-sig'))['data']['document']
initial = json.loads((evidence / 'feishu_master_read.json').read_text(encoding='utf-8-sig'))['data']['document']
master = (root / 'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md').read_text(encoding='utf-8-sig')
assert master.strip() == final['content'].strip()
assert final['revision_id'] == 322
diff = '\n'.join(difflib.unified_diff(initial['content'].splitlines(), final['content'].splitlines(), fromfile='Feishu rev320', tofile='Feishu rev322', lineterm=''))
(evidence / '母版本次局部差异.diff').write_text(diff + '\n', encoding='utf-8')

allowed = {
 'OCC_首次体验战斗规格数据表_v1.0.csv': {'FIRST-B1-WATER','FIRST-B1-LAMP_VINE'},
 'OCC_下一套Build内容表_v1.0.csv': {'ORIGIN-TALENT-01','ORIGIN-SPELL-01','B1','ART-PLAN-20260905'},
 'OCC_美术与界面规格表_v1.0.csv': {'ART-REVIEW-FIRST-B1','ART-PLAN-20260905'}
}
table_results = {}
for filename, allowed_ids in allowed.items():
 def rows(path):
  result = list(csv.DictReader(io.StringIO(path.read_text(encoding='utf-8-sig'))))
  assert all(None not in row for row in result)
  # Existing colour rows omit their final blank field; CSV round-trip pads it.
  result = [{key: (value if value is not None else '') for key,value in row.items()} for row in result]
  assert len({row['ID'] for row in result}) == len(result)
  return {row['ID']:row for row in result}
 before=rows(evidence/'before_sync'/filename); after=rows(root/'Worldbuilding/数据表'/filename)
 assert set(before)<=set(after)
 changed={i for i in after if i not in before or before[i]!=after[i]}
 assert changed == allowed_ids, (filename, changed)
 table_results[filename] = sorted(changed)

plan = task/'OCC_美术优化与视觉迭代方案.md'
broken=[]
for destination in re.findall(r'\]\(([^)]+)\)', plan.read_text(encoding='utf-8')):
 if '://' not in destination and not (plan.parent/destination).exists(): broken.append(destination)
assert not broken, broken
backlog=list(csv.DictReader((task/'迭代执行清单.csv').open(encoding='utf-8-sig',newline='')))
assert len(backlog)==11 and all(None not in row for row in backlog)

audit=subprocess.run([sys.executable,str(root/'Tools/OCCArt/validate_occ_art_asset.py'),'--audit-contract'],cwd=root,capture_output=True,text=True,encoding='utf-8',env={**os.environ,'PYTHONIOENCODING':'utf-8'})
audit_data=json.loads(audit.stdout)
(evidence/'contract_audit_final.json').write_text(json.dumps(audit_data,ensure_ascii=False,indent=2),encoding='utf-8')
assert audit_data['status']=='FAIL' and len(audit_data['errors'])==1 and '2px 细框' in audit_data['errors'][0]
branch=subprocess.check_output(['git','branch','--show-current'],cwd=root,text=True).strip()
assert branch=='codex/art-visual-iteration-20260905'
numstat=subprocess.check_output(['git','diff','--numstat','--','UnityProject','Tools'],cwd=root,text=True,encoding='utf-8',stderr=subprocess.PIPE)
before_numstat=(evidence/'existing_changes_numstat.txt').read_text(encoding='utf-8-sig')
assert numstat.strip()==before_numstat.strip(), 'Unity/Tools tracked diff changed during proposal work'
report={'branch':branch,'master_revision':final['revision_id'],'local_matches_feishu':True,'table_changes_only_expected_ids':table_results,'task_rows':len(backlog),'broken_plan_links':broken,'unity_tools_tracked_diff_unchanged':True,'contract_audit':audit_data,'html_validation':'PASS; see review_validation.json','new_formal_assets':0}
(evidence/'delivery_validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
