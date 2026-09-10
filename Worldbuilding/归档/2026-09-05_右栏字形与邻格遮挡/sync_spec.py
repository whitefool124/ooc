from pathlib import Path
import csv,json,shutil
root=Path('E:/数据库/OCC_Codex'); task=root/'Worldbuilding/归档/2026-09-05_右栏字形与邻格遮挡'
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'; contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
for path in (spec,contract):
    backup=task/'before'/path.name
    if not backup.exists(): shutil.copy2(path,backup)
data=json.loads(contract.read_text(encoding='utf-8-sig'))
data['typography']['bounded_combat_decision_summary']={
    'rect':[16,40,380,52], 'font_px':24, 'explicit_lines':2, 'max_characters_per_line':15,
    'reference_line_advance_px':26, 'automatic_wrap':False, 'details':'existing hover',
    'measured_reference_bottom_margin':{'1920x1080':6,'960x540':4},
    'review':'14 static glyph cases pass; actual input and full visual approval pending'
}
data['typography']['reading_font_review']={
    'canonical':'FusionPixel12 normal Chinese text', 'observed':'SimHei in combat event log via ConfigureReadingParagraph',
    'status':'conflict reported; awaiting user decision; not silently accepted as an exception'
}
contract.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0]); assert not any(row['ID']=='ART-DECISION-GLYPH' for row in rows)
rows.append(dict(zip(fields,['ART-DECISION-GLYPH','界面','行动摘要实际字形边界','380×52；位置16/40；24px；两行间隔26px；每行最多15字','显式两行；不自动折出第三行；详情悬停','14组双分辨率静态字形检查通过；底部留白1920为6参考px、960为4参考px；实际交互待验'])))
rows.append(dict(zip(fields,['ART-READING-FONT-REVIEW','审核','战斗记录阅读字体冲突','总案FusionPixel12；现有实现SimHei','待用户裁决','当前静态渲染确认；未采纳字体例外；不认证全界面字体一致性'])))
with spec.open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n'); w.writeheader(); w.writerows(rows)
print('Decision glyph contract and unresolved reading-font conflict synchronized.')
