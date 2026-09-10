from pathlib import Path
import json,csv,io
root=Path('E:/数据库/OCC_Codex'); task=root/'Worldbuilding/归档/2026-09-05_战斗演出真实性修复'
contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
backup=task/'before/occ_art_contract_v1.json'
assert not backup.exists(); backup.write_bytes(contract.read_bytes())
data=json.loads(contract.read_text(encoding='utf-8-sig'))
data['feedback_presentation']={'source':'resolved_source_id_and_pre_resolution_position','delayed_trigger_recasts':False,'outcome_text_scale':1,'outcome_glyph_alpha':1,'cell_pulse_scale':1,'cell_pulse_edge_reference':2,'floating_text_reference':[240,48],'floating_text_rise_reference':28,'damage_text_reference':[168,104],'damage_text_rise_reference':52,'viewport_margin_reference':8,'position_quantum_reference':2}
contract.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0]); assert not any(r['ID']=='ART-FEEDBACK-READABILITY' for r in rows)
rows.append(dict(zip(fields,['ART-FEEDBACK-READABILITY','表现','结算与飘字可读性','状态240×48上浮28；伤害168×104上浮52；内边距8','结算字形完整不透明，CanvasGroup淡入淡出','文字不缩放；2px位置量化；格框2px贴合64/128格，仅淡出'])))
with spec.open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n');w.writeheader();w.writerows(rows)
print('Feedback presentation contract and CSV synchronized')
