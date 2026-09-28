"""Mirror approved personal spell copy and final costs into the Unity source catalogs."""
import csv
import re
from pathlib import Path

root = Path(__file__).resolve().parents[3]
rows = [r for r in csv.DictReader((root / 'Worldbuilding/数据表/OCC_技能配置表_v1.0.csv').open(encoding='utf-8-sig'))
        if r['技能ID'].startswith('F-P-')]
assert len(rows) == 80
combat = root / 'UnityProject/Assets/Game/Runtime/Combat'
path = combat / 'FireSpellCardCopyCatalog.cs'
source = path.read_text(encoding='utf-8-sig')
for row in rows:
    skill_id = row['技能ID']
    copy = row['卡面文案'].replace('\\', '\\\\').replace('"', '\\"')
    pattern = rf'(?m)^            \{{ "{re.escape(skill_id)}", ".*?" \}},$'
    source, count = re.subn(pattern, lambda _: f'            {{ "{skill_id}", "{copy}" }},', source)
    assert count == 1, skill_id
path.write_text(source, encoding='utf-8')

path = combat / 'FireSpellCatalog.cs'
source = path.read_text(encoding='utf-8-sig')
costs = []
for row in rows:
    skill_id = row['技能ID']
    ap, mana = int(row['AP']), int(row['魔力'])
    cooldown = int(re.match(r'\d+', row['冷却']).group())
    costs.append(f'                {{ "{skill_id}", new[] {{ {ap}, {mana}, {cooldown} }} }},')
start = '        private static readonly HashSet<string> ZeroTempoIds ='
assert source.count(start) == 1
cost_block = ('        private static readonly IReadOnlyDictionary<string, int[]> ApprovedCosts = '
              'new Dictionary<string, int[]>(StringComparer.Ordinal)\n        {\n'
              + '\n'.join(costs) + '\n        };\n\n')
existing = re.compile(r'        private static readonly IReadOnlyDictionary<string, int\[]> ApprovedCosts = '
                      r'new Dictionary<string, int\[]>\(StringComparer\.Ordinal\)\n        \{\n.*?\n        \};\n\n', re.S)
if existing.search(source):
    source, count = existing.subn(lambda _: cost_block, source)
    assert count == 1
else:
    source = source.replace(start, cost_block + start)
if 'ApprovedCosts.TryGetValue(id, out int[] approved)) return approved[0]' not in source:
    source = source.replace('            if (ZeroTempoIds.Contains(id)) return 0;\n            // Terminal spells',
                            '            if (ApprovedCosts.TryGetValue(id, out int[] approved)) return approved[0];\n'
                            '            if (ZeroTempoIds.Contains(id)) return 0;\n            // Terminal spells')
if 'ApprovedCosts.TryGetValue(id, out int[] approved)) return approved[1]' not in source:
    source = source.replace('            if (ZeroTempoIds.Contains(id)) return 0;\n            if (authoredCost <= 0)',
                            '            if (ApprovedCosts.TryGetValue(id, out int[] approved)) return approved[1];\n'
                            '            if (ZeroTempoIds.Contains(id)) return 0;\n            if (authoredCost <= 0)')
source = source.replace('public static int BalancedCooldown(string id, int authoredCooldown) => ZeroTempoIds.Contains(id) ? 0 : authoredCooldown;',
                        'public static int BalancedCooldown(string id, int authoredCooldown) => ApprovedCosts.TryGetValue(id, out int[] approved) ? approved[2] : ZeroTempoIds.Contains(id) ? 0 : authoredCooldown;')
path.write_text(source, encoding='utf-8')
print('generated card copy and costs for', len(rows), 'personal spells')
