"""Prepare exact local state rows for the online state worksheet."""
import csv
import io
import json
import re
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[3]
source = root / 'Worldbuilding/数据表/OCC_状态配置表_v1.0.csv'
with source.open(encoding='utf-8-sig', newline='') as f:
    rows = list(csv.reader(f))
assert len(rows) == 28 and all(len(row) == 12 for row in rows)
assert rows[0][0] == '状态ID'
assert len({row[0] for row in rows[1:]}) == 27
cells = [[{'value': int(value) if column == 11 and value.isdigit() else value}
          for column, value in enumerate(row)] for row in rows[1:]]
out = Path(__file__).with_name('飞书状态写入.json')
out.write_text(json.dumps(cells, ensure_ascii=False), encoding='utf-8')
print(json.dumps({'rows': len(cells), 'columns': len(cells[0]), 'output': str(out)}, ensure_ascii=False))

cmd = ['C:/nvm4w/nodejs/node.exe', 'C:/nvm4w/nodejs/node_modules/@larksuite/cli/scripts/run.js',
       'sheets', '+csv-get', '--url', 'https://jcnlgassfnsm.feishu.cn/sheets/APWssLgD3h4RL7tqxhgcIOa4nTb',
       '--sheet-name', '05_状态配置', '--range', 'A1:L28', '--max-chars', '200000', '--as', 'user']
response = json.loads(subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8', check=True).stdout)
assert response['ok']
data = response['data']
assert data['actual_range'] == 'A1:L28' and not data['has_more']
online = list(csv.reader(io.StringIO('\n'.join(re.sub(r'^\[row=\d+\] ', '', line)
                 for line in data['annotated_csv'].splitlines()))))
assert online == rows, [(i + 1, a, b) for i, (a, b) in enumerate(zip(online, rows)) if a != b]
print(json.dumps({'verified_rows': len(online) - 1, 'revision': data['revision']}, ensure_ascii=False))
