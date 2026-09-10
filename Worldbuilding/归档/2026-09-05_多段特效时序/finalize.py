from pathlib import Path
import json,csv
root=Path('E:/数据库/OCC_Codex'); task=root/'Worldbuilding/归档/2026-09-05_多段特效时序'
raw=json.loads((task/'master_final.json').read_text(encoding='utf-8-sig')); assert raw['ok']
doc=raw['data']['document']; content=doc['content'].rstrip()+'\n'
assert '下一步依据现有MovementPath' in content and '12.7 多段特效时序' in content
mirror=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'; mirror.write_text(content,encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0])
for row in rows:
 if row['ID']=='ART-PLAN-20260905': row['限制']='VISUAL-INTEGRATION-01布局、反馈、多段时序代码门禁通过；真实移动路径、双分辨率实机与审美待验；完整目标仍在进行'
 if row['ID']=='ART-VFX-SEQUENCE': row['限制']='只挂载不提前命中；重叠新能力替换旧链；随视口更新；关闭动画/退出/重开/停用清理；EditMode通过，实机待验'
with spec.open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n');w.writeheader();w.writerows(rows)
result={'feishu_revision':doc['revision_id'],'mirror_matches_fetched_content':mirror.read_text(encoding='utf-8')==content,'main_task':'VISUAL-INTEGRATION-01','status':'in_progress','goal_turn_classification':'progress','editmode':{'passed':757,'failed':0,'skipped':0},'v3_approval_count':0,'runtime_visual_and_performance_review':'not_verified','next':'consume existing MovementPath and complete runtime review'}
(task/'final_status.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
