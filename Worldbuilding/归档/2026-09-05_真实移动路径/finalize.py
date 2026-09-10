from pathlib import Path
import json,csv
root=Path('E:/数据库/OCC_Codex'); task=root/'Worldbuilding/归档/2026-09-05_真实移动路径'
raw=json.loads((task/'master_final.json').read_text(encoding='utf-8-sig')); assert raw['ok']
doc=raw['data']['document']; content=doc['content'].rstrip()+'\n'
assert '12.8 实际移动路径' in content and '下一步复核移动后的攻击' in content
mirror=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'; mirror.write_text(content,encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0])
for row in rows:
 if row['ID']=='ART-PLAN-20260905': row['限制']='VISUAL-INTEGRATION-01布局、反馈、多段时序、真实路径与信息随行代码门禁通过；移动后动作时序及双分辨率实机审美待验；完整目标进行中'
 if row['ID']=='ART-MOVEMENT-PATH': row['限制']='不可信或过期路径对齐最终状态；无未来路线；不改玩法；EditMode通过，双分辨率实机审美待验'
with spec.open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n');w.writeheader();w.writerows(rows)
result={'feishu_revision':doc['revision_id'],'mirror_matches_readback':mirror.read_text(encoding='utf-8')==content,
 'branch':'codex/art-visual-iteration-20260905','main_task':'VISUAL-INTEGRATION-01','status':'in_progress',
 'editmode':{'passed':764,'failed':0,'skipped':0},'new_test_cases':7,'v3_approved':0,
 'runtime_visual_review':'pending','next':'movement/action/popup/camera timing and actual runtime review'}
(task/'final_status.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
