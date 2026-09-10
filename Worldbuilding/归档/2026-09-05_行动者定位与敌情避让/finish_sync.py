from pathlib import Path
import csv,json
root=Path('E:/数据库/OCC_Codex'); task=root/'Worldbuilding/归档/2026-09-05_行动者定位与敌情避让'
contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
data=json.loads(contract.read_text(encoding='utf-8-sig'))
data['unit_annotation_presentation']['intent_placement']['input']='badge raycast, hover and click remain bound to the owning actor cell after displacement'
contract.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with spec.open(encoding='utf-8-sig',newline='')as f:rows=list(csv.DictReader(f))
fields=list(rows[0])
for row in rows:
    if row['ID']=='ART-UNIT-ANNOTATIONS':
        row['限制']='不改大小排序玩法；离屏隐藏；空间不足报告；引线按需创建；位移徽记沿用所属格点击悬停；实机审美待验'
with spec.open('w',encoding='utf-8-sig',newline='')as f:
    w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n');w.writeheader();w.writerows(rows)
document=json.loads((task/'master_final.json').read_text(encoding='utf-8-sig'))['data']['document']
assert (root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md').read_text(encoding='utf-8').rstrip('\n')==document['content'].rstrip('\n')
print('Final master mirror and pointer identity specification verified at revision',document['revision_id'])
