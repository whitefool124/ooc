from pathlib import Path
import csv,json,shutil
root=Path('E:/数据库/OCC_Codex'); task=root/'Worldbuilding/归档/2026-09-05_真实移动路径'
contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
for file in (contract,spec): shutil.copy2(file,task/'before'/file.name)
data=json.loads(contract.read_text(encoding='utf-8-sig'))
data['feedback_presentation']['movement']={
 'input':'resolved MovementPath with source, destination and cardinal adjacency',
 'seconds_per_cell':0.08,'minimum_seconds':0.12,'maximum_seconds':0.35,
 'continuous_commands':'preserve current sample and all remaining corners, then append new executed route',
 'unverified_or_stale_path':'align to final resolved state; never invent a path',
 'shared_travel':['unit','health','shield','statuses','intent','facing'],
 'coordinate_quantum_at_64px_cell':2,'unit_sort':'current visual foot Y then X',
 'facing':'three integer UI rectangles; four cardinal rotations; friendly cyan/enemy danger; segment then resolved facing',
 'cancel_on':['animation_disabled','battle_exit','battle_reset','unit_dead','stale_destination','component_disable'],
 'runtime_review':'pending; no gameplay mutation; no art asset promotion'}
contract.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0]); assert not any(r['ID']=='ART-MOVEMENT-PATH' for r in rows)
rows.append(dict(zip(fields,['ART-MOVEMENT-PATH','表现','实际移动路径与信息随行','逐格80ms；120–350ms；64px格位移2px量化','真实路线；连续接续拐点；血盾状态朝向随行；当前足底排序','不可信或过期路径对齐最终状态；无未来路线；不改玩法；双分辨率实机审美待验'])))
with spec.open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n'); w.writeheader(); w.writerows(rows)
print('Movement presentation mirrors synchronized with master section 12.8')
