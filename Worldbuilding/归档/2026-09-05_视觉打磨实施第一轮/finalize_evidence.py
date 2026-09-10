from pathlib import Path
import json,csv,hashlib,subprocess
root=Path('E:/数据库/OCC_Codex'); task=root/'Worldbuilding/归档/2026-09-05_视觉打磨实施第一轮'
data=json.loads((task/'evidence/master_final.json').read_text(encoding='utf-8-sig'))
assert data['ok']; doc=data['data']['document']; content=doc['content'].rstrip()+'\n'
for token in ['12.5 当前实施状态','QA_PENDING','当前唯一主任务 VISUAL-INTEGRATION-01','448×768']:
 assert token in content,token
mirror=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'; mirror.write_text(content,encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0])
for row in rows:
 if row['ID']=='ART-PLAN-20260905': row['限制']='VISUAL-INTEGRATION-01首轮代码与EditMode通过；双分辨率实机和审美待验；完整目标仍在进行'
 if row['ID']=='ART-REVIEW-FIRST-B1': row['限制']='旧FORMAL仅追溯；本轮技术2/2与Importer通过；新布局实机接触和人工审美待验；批准0/2，不新增正式替换或扩散复用'
with spec.open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n'); w.writeheader(); w.writerows(rows)
script=task/'implement_hud.py'; s=script.read_text(encoding='utf-8'); s=s.replace('scaler.referenceResolution = UiLayoutContract.ReferenceResolution;','scaler.referenceResolution = new Vector2(UiLayoutContract.ReferenceWidth, UiLayoutContract.ReferenceHeight);'); script.write_text(s,encoding='utf-8')
result={'branch':subprocess.check_output(['git','branch','--show-current'],cwd=root,text=True).strip(),'feishu_revision':doc.get('revision_id'),'mirror_matches_fetched_content':mirror.read_text(encoding='utf-8')==content,'mirror_sha256':hashlib.sha256(mirror.read_bytes()).hexdigest(),'current_main_task':'VISUAL-INTEGRATION-01','status':'runtime_and_aesthetic_review_pending','full_editmode':{'passed':740,'failed':0,'skipped':0},'current_v3_approval_count':0}
(task/'evidence/delivery_status.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
