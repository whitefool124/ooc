from pathlib import Path
import csv,json
root=Path('E:/数据库/OCC_Codex')
p=root/'Tools/OCCArt/occ_art_contract_v1.json'
d=json.loads(p.read_text(encoding='utf-8-sig'))
d['terrain_depth_presentation']={
    'lamp_vine':{'source_px':32,'back_rows':22,'front_rows':10,'asset_status':'existing QA_PENDING 0/2 review only',
        'sampling':'partition original UV without changing texture/importer/color; integer display at 64/128 reference cells',
        'depth':'fixed cell ground depth; after actors on same row, before nearer actors; no unit travel inherited',
        'lifecycle':'presented tile controls visibility at existing hit time; hide on burn, reuse on restoration; raycast disabled'},
    'ground_attachments':'catalog marks explicitly; parent to owning cell after floor, before environment and objects; raised structures retain structure layer',
    'review':'empty whole/split production renders identical; motion depth and burn/restore inspected; full dynamic/human review pending; no asset promotion'
}
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with p.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0]);rows=[r for r in rows if r['ID']!='ART-TERRAIN-DEPTH']
rows.append(dict(zip(fields,['ART-TERRAIN-DEPTH','表现','灯藤前沿与地面附属物层级','原图32px按后22行前10行分层；64/128格整数采样','前沿固定地块并按深度遮同格脚部；地面附属物在环境物件之下','空藤合成像素一致；移动不拖地形；命中快照烧毁清理；点击透传；原图GUIDImporter不变；v3仍QA_PENDING0/2；实机审美待验'])))
with p.open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n');w.writeheader();w.writerows(rows)
print('Terrain depth mirrors synchronized; v3 approval and gameplay data unchanged.')
