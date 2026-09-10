from pathlib import Path
import csv, json, shutil
root=Path('E:/数据库/OCC_Codex')
task=root/'Worldbuilding/归档/2026-09-05_双分辨率静态接触'
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
for path in (spec,contract):
    backup=task/'before'/path.name
    if not backup.exists(): shutil.copy2(path,backup)
data=json.loads(contract.read_text(encoding='utf-8-sig'))
data['compact_command_presentation']={
    'reference_font_px':24, 'frame_px':2, 'coordinate_quantum':2,
    'weapon_rows':{'size':[184,54],'positions':[[8,-48],[8,-106]],'icon_px':32},
    'interaction_rows':{'size':[136,54],'name_width':56,'cost_size':[56,32],'detail':'hover'},
    'quickbar':{'size':[76,54],'icon_px':32,'empty':'slot number and 空','occupied':'icon, slot number, remaining charges in separate regions','full_details':'hover'},
    'review':'production UI static renders at 1920x1080 and 960x540; live input, motion, performance and human aesthetic approval remain pending'
}
contract.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0]); assert not any(row['ID']=='ART-COMPACT-COMMANDS' for row in rows)
rows.append(dict(zip(fields,['ART-COMPACT-COMMANDS','界面','底栏指令与快捷槽可读性','武器184×54两行；交互136×54；快捷槽76×54；细框2px','正文24px；32px图标；名称费用分区；空槽槽号与空；占用槽图标槽号次数分区','两档生产UI静态渲染自检通过；82项相关检查通过；不代替实机交互与人工审美；完整目标继续'])))
for row in rows:
    if row['ID']=='ART-PLAN-20260905': row['限制']='布局、真实移动、攻击/火术与法宝反馈代码门禁通过；底栏双分辨率静态接触已修复遮挡；整体实机与审美待完成；完整目标进行中'
with spec.open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n'); w.writeheader(); w.writerows(rows)
print('Compact command specification synchronized.')
