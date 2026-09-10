from pathlib import Path
import csv, json, shutil
root = Path('E:/数据库/OCC_Codex')
task = root / 'Worldbuilding/归档/2026-09-05_法宝反馈真实性'
spec = root / 'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
contract = root / 'Tools/OCCArt/occ_art_contract_v1.json'
for file in (spec, contract):
    backup = task / 'before' / file.name
    if not backup.exists(): shutil.copy2(file, backup)
data = json.loads(contract.read_text(encoding='utf-8-sig'))
data['feedback_presentation']['action_contact']['status'] = 'ordinary attack/fire timing and artifact composite feedback code gates pass; runtime and aesthetic review pending'
data['feedback_presentation']['artifact_results'] = {
    'input': 'per-effect resolved feedback with source and affected unit IDs and captured positions',
    'resources': 'actual per-unit health/shield/mana differences; never aggregate ArtifactStep.Applied',
    'shield_semantics': ['absorb damage', 'consume as cost', 'transfer out', 'transfer in', 'restore'],
    'status': 'only actual additions, stronger refreshes and removals; no retired status resurrection',
    'utility': 'accurate placement, readiness and initiative text; no fake cleanse or slow',
    'reactions': 'captured triggered result queue drained once after command resolution',
    'identity': 'resolve captured unit ID before cell occupancy for delayed feedback',
    'snapshot_fallback': 'suppress duplicate resource feedback when explicit effect results exist',
    'visuals': 'reuse approved shield and interact icons; no impact VFX for cost, transfer or utility',
    'review': 'EditMode verified; live flow, timing and aesthetic review pending; no asset promotion'
}
contract.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
with spec.open(encoding='utf-8-sig', newline='') as f: rows = list(csv.DictReader(f))
fields = list(rows[0])
for row in rows:
    if row['ID'] == 'ART-ACTION-CONTACT': row['限制'] = '普通攻击/火术时序与法宝复合反馈EditMode通过；AI和结果页等待有界；关闭动画保留反馈；实机待验'
    if row['ID'] == 'ART-PLAN-20260905': row['限制'] = '布局、真实移动、普通攻击/火术命中时序和法宝复合反馈代码门禁通过；双分辨率实机与审美待完成；完整目标进行中'
assert not any(row['ID'] == 'ART-ARTIFACT-RESULTS' for row in rows)
rows.append(dict(zip(fields, ['ART-ARTIFACT-RESULTS', '表现', '法宝逐项真实反馈', '按结算记录保存来源与目标ID及实际生命/护盾/魔力/状态变化', '护盾承伤、消耗、转出、转入、恢复分开；复用既有图标', '不读取合计量推测个体伤害；不补发重复飘字；零效果不伪造反馈；反应只消费一次；不恢复已删除状态；实机待验'])))
with spec.open('w', encoding='utf-8-sig', newline='') as f:
    writer = csv.DictWriter(f, fieldnames=fields, lineterminator='\n')
    writer.writeheader(); writer.writerows(rows)
print('Artifact feedback specification and machine mirror synchronized.')
