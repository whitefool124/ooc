from pathlib import Path
import csv,json,shutil
root=Path('E:/数据库/OCC_Codex');task=root/'Worldbuilding/归档/2026-09-05_行动者定位与敌情避让'
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv';contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
for path in (spec,contract):
    backup=task/'before'/path.name
    if not backup.exists():shutil.copy2(path,backup)
data=json.loads(contract.read_text(encoding='utf-8-sig'))
data['unit_annotation_presentation']={
    'active_footprint':{'reference_cell':64,'size':[56,12],'line_px':2,'corner_px':8,'colors':'friendly cyan; enemy danger','anchor':'current visual foot; hidden outside viewport; one active actor'},
    'intent_placement':{'coordinate_quantum':2,'maximum_candidates':25,'protected':['conservative unit bodies','health/shield','statuses','already placed badges'],'selection':'minimum overlapping area, then shortest distance, then fixed candidate order','viewport':'4px inner margin; fully offscreen actors do not get edge badges','moved_badge':'short orthogonal association line; lazily created and reused','fallback':'keep existing information; report remaining overlap'},
    'review':'static default, crowded, zoom, offscreen and actor transfer checked; live motion, performance and human aesthetic review pending; no asset promotion'
}
contract.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with spec.open(encoding='utf-8-sig',newline='')as f:rows=list(csv.DictReader(f))
fields=list(rows[0]);assert not any(row['ID']=='ART-UNIT-ANNOTATIONS' for row in rows)
rows.append(dict(zip(fields,['ART-UNIT-ANNOTATIONS','表现','当前行动者定位与敌情避让','64px格角标56×12/2px线/8px角；128档倍增；徽记最多25候选','友方冷青敌方红；避让角色血盾状态及徽记；短正交引线','不改变单位大小足底排序和玩法；离屏隐藏；空间不足保留信息并报告；引线按需创建；实机审美待验'])))
with spec.open('w',encoding='utf-8-sig',newline='')as f:
    w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n');w.writeheader();w.writerows(rows)
print('Unit annotation specification synchronized.')
