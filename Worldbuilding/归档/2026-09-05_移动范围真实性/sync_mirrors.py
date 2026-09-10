from pathlib import Path
import csv,json
root=Path('E:/数据库/OCC_Codex')
contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
data=json.loads(contract.read_text(encoding='utf-8-sig'))
data['movement_range_presentation']={
    'source':'CombatMovementQuery.FindPath shared with resolver and movement playback path capture',
    'eligibility':['alive hero turn','battle active','sufficient AP','not bound','different unoccupied passable cell','actual weighted path within current budget'],
    'budget':'current movement including slow and existing RainLanternCourt next-move origin bonus',
    'cache':'exact snapshot of state identity, origin, budget, relevant blocked cells, terrain entry costs and live unit occupancy; recheck globals per query',
    'consumers':['move overlay','valid cell count','move target failure reason'],
    'review':'EditMode command equivalence and state changes; runtime GC/frame time and human visual review remain pending; v3 QA_PENDING 0/2'
}
contract.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0])
rows=[r for r in rows if r['ID']!='ART-MOVE-RANGE']
rows.append(dict(zip(fields,['ART-MOVE-RANGE','战场','移动范围真实性','高亮／有效格数／目标失败原因共用真实路径与当前移动预算','仅当前操作范围显著；沿用现有样式','束缚／AP／回合／结局门控；水藤代价、迟缓、出身加成、障碍占位与变化失效；只读缓存；实机性能待验'])))
with spec.open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n');w.writeheader();w.writerows(rows)
print('Movement presentation mirrors synchronized; gameplay CSV and v3 approval unchanged.')
