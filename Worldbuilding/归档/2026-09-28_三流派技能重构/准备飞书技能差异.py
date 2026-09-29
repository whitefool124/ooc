"""Read the live Feishu skill tab and prepare exact changed-cell writes."""
import csv
import io
import json
import re
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[3]
url = 'https://jcnlgassfnsm.feishu.cn/sheets/APWssLgD3h4RL7tqxhgcIOa4nTb'
sheet = '07_技能配置'
cmd = ['C:/nvm4w/nodejs/node.exe', 'C:/nvm4w/nodejs/node_modules/@larksuite/cli/scripts/run.js', 'sheets', '+csv-get', '--url', url, '--sheet-name', sheet,
       '--range', 'A1:T135', '--max-chars', '2000000', '--as', 'user']
result = subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8', check=True)
response = json.loads(result.stdout)
assert response['ok'], response
data = response['data']
assert data['actual_range'] == 'A1:T135' and not data['has_more'], data['warning_message']
assert data['row_count'] == 135 and data['col_count'] == 20
annotated = data['annotated_csv']
raw_lines = annotated.splitlines()
row_numbers = [int(re.match(r'^\[row=(\d+)\] ', line).group(1)) for line in raw_lines]
assert row_numbers == list(range(1, 136))
online = list(csv.reader(io.StringIO('\n'.join(re.sub(r'^\[row=\d+\] ', '', x) for x in raw_lines))))
assert all(len(row) == 20 for row in online)
with (root / 'Worldbuilding/数据表/OCC_技能配置表_v1.0.csv').open(encoding='utf-8-sig', newline='') as f:
    local = list(csv.reader(f))
assert len(local) == 135 and online[0] == local[0]
assert [r[0] for r in online] == [r[0] for r in local], 'Online rows have changed identity/order'
columns = data['col_indices']
writes = []
diffs = []
for rowno, (old, new) in enumerate(zip(online, local), 1):
    for colno, (a, b) in enumerate(zip(old, new)):
        if a == b:
            continue
        if not (new[0].startswith('F-P-') or new[0] == 'ARTIFACT-F-T01'):
            raise RuntimeError(f'Non-player row differs: {rowno}, {new[0]}, {columns[colno]}')
        cell = f'{columns[colno]}{rowno}'
        value: str | int = int(b) if columns[colno] in {'F', 'G', 'H', 'I'} and b.isdigit() else b
        writes.append({'sheet_name': sheet, 'range': cell, 'cells': [[{'value': value}]]})
        diffs.append((new[0], cell, a, b))
out = Path(__file__).with_name('飞书技能差异.json')
out.write_text(json.dumps({'url': url, 'revision': data['revision'], 'writes': writes}, ensure_ascii=False, indent=2), encoding='utf-8')
for i in range(0, len(writes), 80):
    Path(__file__).with_name(f'飞书技能写入_{i // 80 + 1}.json').write_text(
        json.dumps(writes[i:i + 80], ensure_ascii=False), encoding='utf-8')
print(json.dumps({'revision': data['revision'], 'cells': len(writes),
                  'rows': len({x[0] for x in diffs}),
                  'columns': {c: sum(1 for x in diffs if x[1].startswith(c)) for c in columns},
                  'output': str(out)}, ensure_ascii=False))
