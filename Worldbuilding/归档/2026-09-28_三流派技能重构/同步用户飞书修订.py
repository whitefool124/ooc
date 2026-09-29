"""Apply the user-edited Feishu card copy and resolve the two terminology notes."""
from __future__ import annotations

import csv
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
FOLDER = Path(__file__).parent
SOURCE = ROOT / 'Worldbuilding/数据表/OCC_技能配置表_v1.0.csv'
SNAPSHOT = FOLDER / '飞书文档_用户修改快照.md'
OUTPUT = FOLDER / '三流派技能描述表.md'

with SOURCE.open(encoding='utf-8-sig', newline='') as stream:
    reader = csv.DictReader(stream)
    rows = list(reader)
    fields = reader.fieldnames
assert fields and len(rows) == 134
by_id = {row['技能ID']: row for row in rows}

remote: dict[str, list[str]] = {}
for line in SNAPSHOT.read_text(encoding='utf-8').splitlines():
    if not line.startswith('| '):
        continue
    parts = [part.strip() for part in line.strip('|').split('|')]
    if len(parts) == 6 and parts[0] in by_id:
        assert parts[0] not in remote
        remote[parts[0]] = parts
assert len(remote) == 101

copy_changes = []
for skill_id, parts in remote.items():
    row = by_id[skill_id]
    assert parts[:5] == [row[name] for name in ('技能ID', '名称', 'AP', '魔力', '冷却')]
    if row['卡面文案'] != parts[5]:
        row['卡面文案'] = parts[5]
        copy_changes.append(skill_id)
assert len(copy_changes) == 20, copy_changes

copy_override = {
    'F-P-R23': '选择一个破口，将相邻敌我单位各推开1格。',
    'F-P-U22': '在己方破口或其相邻空格生成8耐久的轻掩体。',
    'F-P-U24': '沿经过己方破口的路径移动最多3格。对路径旁敌人各造成6点武器伤害。',
    'F-P-U26': '移至自身破口。',
    'F-P-M20': '对相邻敌人造成20点伤害；本回合有突进轨迹时，改为对轨迹旁敌人各造成20点伤害。',
    'F-P-M24': '沿本回合突进轨迹退回最多2格。',
    'F-P-U20': '对本回合突进轨迹旁的敌人各造成12点火焰伤害。',
}
for skill_id, copy in copy_override.items():
    by_id[skill_id]['卡面文案'] = copy

for row in rows:
    if not row['技能ID'].startswith('F-P-'):
        continue
    for column in ('目标与范围', '效果链', '结算规则'):
        row[column] = row[column].replace('本回合实际突进路径', '本回合突进轨迹')
        row[column] = row[column].replace('本回合有实际突进路径', '本回合有突进轨迹')
        row[column] = row[column].replace('本回合有突进路径', '本回合有突进轨迹')
        row[column] = row[column].replace('本回合被摧毁物件的原格', '破口')
        row[column] = row[column].replace('本回合己方摧毁物件的原格', '己方破口')

u10 = by_id['F-P-U10']
assert u10['卡面文案'] == '使自身获得护盾效能+4，持续2个自身回合。'
u10['目标与范围'] = '自身'
u10['效果链'] = '对自身施加状态：护盾效能+4，持续2个自身回合'
u10['结算规则'] = '由status_shield_efficiency承载；只加强自身获得的护盾'

u23 = by_id['F-P-U23']
assert u23['卡面文案'] == '沿直线突进，并在停下时对其造成12点武器伤害。'
u23['目标与范围'] = '直线内首个单位或物件；停在其前一格'
u23['效果链'] = '沿直线突进至首个单位或物件前一格；停下时对该目标造成12点武器伤害'

with SOURCE.open('w', encoding='utf-8', newline='') as stream:
    writer = csv.DictWriter(stream, fieldnames=fields, quoting=csv.QUOTE_ALL, lineterminator='\n')
    writer.writeheader()
    writer.writerows(rows)

lines = [
    '# OCC 三流派与混合运转技能描述表', '',
    '本表为技能描述修订版，游戏效果尚未同步实装。个人术式80张：地块破坏回流、突进穿刺、燃烧火场各20张，混合运转20张；另附法宝与出身术式21张。', '',
    '持续增减益使用状态实例；行动条提前／延后即时结算。行动点、魔力、冷却由费用栏显示，描述只写效果。', '',
    '破口：本回合物件被摧毁时占据的原格；己方／自身破口按摧毁来源区分。',
    '突进轨迹：本回合已实际完成的突进路径。',
    '地块：一个战场格；对地块造成伤害时，分别结算该格内可受击的敌我单位与可破坏物。', '',
]
for build in ('地块破坏回流', '突进穿刺', '燃烧火场', '混合运转'):
    lines += [f'## {build}', '', '| ID | 名称 | AP | 魔力 | 冷却 | 描述 |', '|---|---|---:|---:|---|---|']
    for row in rows:
        if row['技能ID'].startswith('F-P-') and row['主构筑'] == build:
            parts = [row[key] for key in ('技能ID', '名称', 'AP', '魔力', '冷却', '卡面文案')]
            lines.append('| ' + ' | '.join(part.replace('|', '\\|') for part in parts) + ' |')
    lines.append('')
others = [row for row in rows if not row['技能ID'].startswith('F-P-') and row['卡面文案'].strip()]
assert len(others) == 21
lines += ['## 法宝与出身术式', '', '| ID | 名称 | AP | 魔力 | 冷却 | 描述 |', '|---|---|---:|---:|---|---|']
for row in others:
    parts = [row[key] for key in ('技能ID', '名称', 'AP', '魔力', '冷却', '卡面文案')]
    lines.append('| ' + ' | '.join(part.replace('|', '\\|') for part in parts) + ' |')
lines += ['', '其余33条基础动作与敌方能力保留现行配置。', '']
OUTPUT.write_text('\n'.join(lines), encoding='utf-8')
print('Feishu copy changes:', len(copy_changes), 'terminology overrides:', len(copy_override))
