from pathlib import Path
import csv,json,shutil
root=Path('E:/数据库/OCC_Codex'); task=root/'Worldbuilding/归档/2026-09-05_动作命中与镜头衔接'
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'; contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
for file in (spec,contract): shutil.copy2(file,task/'before'/file.name)
data=json.loads(contract.read_text(encoding='utf-8-sig'))
data['feedback_presentation']['action_contact']={
 'input':'already resolved action; detached pre-action display snapshot',
 'start':'after current source movement finishes','normal_contact_seconds':0.16,'skill_delivery_seconds':0.18,
 'fire_contact':'per-cell actual FireVfxSequence contact cue; weapon triggers start at weapon contact',
 'result_hold_seconds':0.42,'shared_contact_clock':['unit health/shield/status/death','terrain','fireground','floating feedback'],
 'live_values':['AP costs','mana costs','combat final state'],
 'fence':['next battle input','next AI presentation','outcome overlay'],
 'animation_disabled':'flush result feedback once, release snapshots and fence',
 'reset':'cancel queued callbacks and detached snapshots',
 'slow_frame':'dispatch scheduled timestamps rather than restarting expired effects',
 'camera':'minimum pan at safe edge around current visual position; no overview movement',
 'status':'ordinary attack/fire timing code gates pass; artifact composite feedback and runtime review pending'}
contract.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0]); assert not any(r['ID']=='ART-ACTION-CONTACT' for r in rows)
rows.append(dict(zip(fields,['ART-ACTION-CONTACT','表现','动作命中与镜头衔接','攻击160ms；普通投递180ms；火术按实际序列；命中后保留420ms','结算前显示快照；AP与费用即时；死亡地形飘字同一命中时刻','普通攻击/火术时序EditMode通过；法宝复合反馈待修复；AI和结果页等待有界；关闭动画保留反馈；实机待验'])))
for row in rows:
 if row['ID']=='ART-PLAN-20260905': row['限制']='布局、真实移动、普通攻击和火术命中时序代码门禁通过；法宝复合反馈与双分辨率实机审美待完成；完整目标进行中'
with spec.open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n'); w.writeheader(); w.writerows(rows)
print('Action contact mirrors synchronized; incomplete artifact/runtime gates explicitly retained')
