"""本次母版与结构化状态同步；执行前需保存 final_master_read.json 和 before_sync。"""
from pathlib import Path
import csv
import io
import json

ROOT = Path('E:/数据库/OCC_Codex')
TASK = ROOT / 'Worldbuilding/归档/2026-09-05_美术优化与视觉迭代方案'
response = json.loads((TASK / 'evidence/final_master_read.json').read_text(encoding='utf-8-sig'))
assert response['ok'] is True
doc = response['data']['document']
content = doc['content']
assert 'ART-PLAN-20260905' in content and 'ART-V0-01' in content and 'QA_PENDING' in content
master = ROOT / 'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'
assert (TASK / 'evidence/before_sync' / master.name).exists()
master.write_text(content.rstrip() + '\n', encoding='utf-8')

def update_table(filename, updates, extra=None):
    path = ROOT / 'Worldbuilding/数据表' / filename
    assert (TASK / 'evidence/before_sync' / filename).exists()
    original = path.read_text(encoding='utf-8-sig')
    rows = list(csv.DictReader(io.StringIO(original)))
    fields = list(rows[0])
    found = set()
    for row in rows:
        if row['ID'] in updates:
            row.update(updates[row['ID']])
            found.add(row['ID'])
    assert found == set(updates), (filename, found, set(updates))
    if extra:
        for addition in extra:
            assert set(addition) == set(fields)
            rows = [row for row in rows if row['ID'] != addition['ID']]
            rows.append(addition)
    buf = io.StringIO(newline='')
    writer = csv.DictWriter(buf, fieldnames=fields, lineterminator='\n')
    writer.writeheader(); writer.writerows(rows)
    path.write_text(buf.getvalue(), encoding='utf-8-sig')

update_table('OCC_首次体验战斗规格数据表_v1.0.csv', {
    'FIRST-B1-WATER': {'实现备注': '地表瓦片；2026-09-05用户裁决以飞书rev320为准。现有水面v3待重新审核，当前复审登记QA_PENDING；旧原料、manifest、GUID及运行接触只保留追溯，不构成本轮人工批准；现有Unity文件未替换，复审前不扩散复用'},
    'FIRST-B1-LAMP_VINE': {'实现备注': '灼触与火花不能直接选空格灯藤；2026-09-05用户裁决以飞书rev320为准。现有灯藤v3待重新审核，当前复审登记QA_PENDING；中空树篱与人物站入必须重新审美；旧FORMAL记录仅作历史，现有Unity文件未替换，复审前不扩散复用'}
})
update_table('OCC_下一套Build内容表_v1.0.csv', {
    'ORIGIN-TALENT-01': {'数量或配置': '就地接线；每场第一次移动结束正交邻接轻或重掩体时获得2护盾并恢复1魔力；已由总案10.4.1冻结'},
    'ORIGIN-SPELL-01': {'数量或配置': '借障导流；1AP及1魔力；正交邻接轻或重掩体时对自身施放，获得4护盾且本回合下次移动行动移动力+2；不叠加'},
    'B1': {'数量或配置': '雨后灯庭；正式规则已冻结并接入；奖励三选一仍待冻结；水面及灯藤v3本轮QA_PENDING', '验收结果': '规则按总案10.4.1；奖励未冻结不作完成声明；现有v3需重新人工审核，当前批准0/2'}
}, [{'类别':'美术规划','ID':'ART-PLAN-20260905','名称':'美术优化与视觉迭代方案','数量或配置':'已交付方案、交互评审页和顺序清单；新增正式资产0项','出现位置':'肉鸽首战样板与两模式共用视觉规范','玩家操作':'本次为规划交付，不改游戏操作','验收结果':'证据、文件入口、依赖与验收齐备；下一主任务ART-V0-01复审现有水面和灯藤v3；其余新视觉数值均未采纳'}])
update_table('OCC_美术与界面规格表_v1.0.csv', {}, [
    {'ID':'ART-REVIEW-FIRST-B1','类别':'审核','项目':'雨后灯庭水面与灯藤v3当前复审','规格':'2026-09-05两项QA_PENDING，当前批准0/2','状态色或材质':'以飞书rev320与本轮用户裁决为基准','限制':'旧FORMAL记录仅追溯；现有Unity文件保留；复审前不新增正式替换或扩散复用；五类证据与人工审美需重新核对'},
    {'ID':'ART-PLAN-20260905','类别':'规划','项目':'首战美术与视觉迭代方案交付','规格':'方案与评审页及顺序清单已归档，新增正式资产0项','状态色或材质':'沿用当前暖纸档案方向','限制':'新倍率、演出节奏、工作量与性能预算均为建议；采纳后先更新总案、规格表和机器镜像再实施'}
])
html = TASK / '视觉迭代评审.html'
html.write_text(html.read_text(encoding='utf-8').replace('no-label ', 'no-labels ').replace('board no-label"', 'board no-labels"'), encoding='utf-8')
assert master.read_text(encoding='utf-8').strip() == content.strip()
print(json.dumps({'revision':doc['revision_id'],'local_master_matches_remote':True,'tables_updated':3,'new_formal_assets':0}, ensure_ascii=False))
