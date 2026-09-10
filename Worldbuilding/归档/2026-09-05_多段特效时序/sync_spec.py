from pathlib import Path
import csv,json
root=Path('E:/数据库/OCC_Codex')
contract=root/'Tools/OCCArt/occ_art_contract_v1.json'; data=json.loads(contract.read_text(encoding='utf-8-sig'))
data['feedback_presentation']['vfx_sequence']={'input':'FireSpellExecution resolved steps','cast_seconds':0.12,'stage_seconds':0.18,'layers':['Ability','Reaction'],'same_cell_policy':'sequential within ability; reaction cannot replace ability','new_ability_policy':'replace overlapping cell tracks','follow_viewport':True,'cancel_on':['animation_disabled','battle_exit','battle_reset','component_disable','component_destroy'],'zero_strength_status':'emitted status step plus positive rule duration is a real status application'}
contract.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0]); assert not any(r['ID']=='ART-VFX-SEQUENCE' for r in rows)
rows.append(dict(zip(fields,['ART-VFX-SEQUENCE','表现','多段特效与清理','主动起手120ms；每段180ms；能力/即时反馈分层','实际结算步骤门控，同格依次播放','只挂载不提前命中；重叠新能力替换旧链；随视口更新；关闭动画/退出/重开/停用清理；实机待验'])))
with spec.open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n');w.writeheader();w.writerows(rows)
print('VFX sequence mirrors synchronized')
